using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ウイルス（敵への継続ダメージのデバフ）の実行時状態。
    /// 敵ごとに威力・最長効果時間・残り効果時間と1秒周期の経過時間を持ち、
    /// 1秒経つごとに「威力÷最長効果時間」のダメージを発生させる（1以下なら整数に四捨五入）。
    /// 重複時は威力を加算し、新しい効果時間が残り効果時間より長いときだけ時間を置き換える。
    /// 1秒周期の経過時間は重複しても引き継ぐ（付け直しのたびに周期が戻ってダメージが出なくなるのを防ぐ）。
    /// 威力だけを加算して時間に触れない付与（AddPower）と、残り効果時間をそのフレームだけ止める指定（HoldRemainingTime）もできる
    /// </summary>
    public class VirusDataStore : IVirusDataStore, IRunResettable, IDisposable
    {
        // ダメージを与える間隔（秒）
        private const float TickInterval = 1f;

        // 浮動小数の誤差で最後の1回を取りこぼさないための許容誤差（秒）
        private const float Epsilon = 1e-4f;

        // 毎秒ダメージがこの値以下なら整数に四捨五入する
        private const float RoundThreshold = 1f;

        private class VirusState
        {
            public float Power;
            public float MaxDuration;
            public float RemainingTime;

            // 直前のダメージからの経過時間（秒）
            public float TickTimer;
        }

        private readonly Dictionary<int, VirusState> _viruses = new();

        // 効果時間が切れた敵Idの一時バッファ（呼び出しごとに詰め直す）
        private readonly List<int> _expiredEnemyIds = new();

        // 次の Advance で残り効果時間を減らさない敵Id（Advance のたびに空にする）
        private readonly HashSet<int> _heldEnemyIds = new();

        private readonly Subject<VirusApplyResult> _onApplied = new();

        public Observable<VirusApplyResult> OnApplied => _onApplied;

        public void Apply(int enemyId, float power, float duration)
        {
            if (power <= 0f || duration <= 0f)
            {
                return;
            }

            if (!_viruses.TryGetValue(enemyId, out var state))
            {
                state = new VirusState
                {
                    Power = power,
                    MaxDuration = duration,
                    RemainingTime = duration
                };
                _viruses.Add(enemyId, state);
                _onApplied.OnNext(new VirusApplyResult(enemyId, power, duration, true, default, ToStatus(state), false,
                    CalcDamagePerSecond(state.Power, state.MaxDuration)));
                return;
            }

            var before = ToStatus(state);

            state.Power += power;

            var isDurationReplaced = duration > state.RemainingTime;
            if (isDurationReplaced)
            {
                state.MaxDuration = duration;
                state.RemainingTime = duration;
            }

            _onApplied.OnNext(new VirusApplyResult(enemyId, power, duration, false, before, ToStatus(state), isDurationReplaced,
                CalcDamagePerSecond(state.Power, state.MaxDuration)));
        }

        public void AddPower(int enemyId, float power, float durationIfNew)
        {
            if (!_viruses.TryGetValue(enemyId, out var state))
            {
                Apply(enemyId, power, durationIfNew);
                return;
            }

            if (power <= 0f)
            {
                return;
            }

            var before = ToStatus(state);

            state.Power += power;

            _onApplied.OnNext(new VirusApplyResult(enemyId, power, 0f, false, before, ToStatus(state), false,
                CalcDamagePerSecond(state.Power, state.MaxDuration)));
        }

        public void HoldRemainingTime(int enemyId)
        {
            if (_viruses.ContainsKey(enemyId))
            {
                _heldEnemyIds.Add(enemyId);
            }
        }

        public bool TryGetVirus(int enemyId, out VirusStatus status)
        {
            if (!_viruses.TryGetValue(enemyId, out var state))
            {
                status = default;
                return false;
            }

            status = ToStatus(state);
            return true;
        }

        private static VirusStatus ToStatus(VirusState state)
        {
            return new VirusStatus(state.Power, state.MaxDuration, state.RemainingTime);
        }

        public void Advance(float deltaTime, List<VirusDamage> results)
        {
            results.Clear();

            if (_viruses.Count == 0 || deltaTime <= 0f)
            {
                _heldEnemyIds.Clear();
                return;
            }

            _expiredEnemyIds.Clear();

            foreach (var pair in _viruses)
            {
                var enemyId = pair.Key;
                var state = pair.Value;

                // 残り時間を止めている敵は、1秒周期だけ進めて残り時間は減らさない（切れない）
                var isHeld = _heldEnemyIds.Contains(enemyId);

                // 残り時間を超えて進めない（切れる瞬間に最後の1回が届くようにする）
                var step = isHeld ? deltaTime : Math.Min(deltaTime, state.RemainingTime);
                state.TickTimer += step;
                if (!isHeld)
                {
                    state.RemainingTime -= step;
                }

                while (state.TickTimer >= TickInterval - Epsilon)
                {
                    state.TickTimer -= TickInterval;

                    var damage = CalcDamagePerSecond(state.Power, state.MaxDuration);
                    if (damage > 0f)
                    {
                        results.Add(new VirusDamage(enemyId, damage));
                    }
                }

                if (!isHeld && state.RemainingTime <= Epsilon)
                {
                    _expiredEnemyIds.Add(enemyId);
                }
            }

            _heldEnemyIds.Clear();

            foreach (var enemyId in _expiredEnemyIds)
            {
                _viruses.Remove(enemyId);
            }
        }

        public void Remove(int enemyId)
        {
            _viruses.Remove(enemyId);
        }

        public void ResetRun()
        {
            _viruses.Clear();
            _heldEnemyIds.Clear();
        }

        public void Dispose()
        {
            _onApplied.Dispose();
        }

        /// <summary>
        /// 1秒ごとのダメージ＝威力÷最長効果時間。1以下なら整数に四捨五入する（0.5は1、0.5未満は0＝ダメージなし）
        /// </summary>
        public static float CalcDamagePerSecond(float power, float maxDuration)
        {
            if (power <= 0f || maxDuration <= 0f)
            {
                return 0f;
            }

            var damage = power / maxDuration;
            if (damage <= RoundThreshold)
            {
                damage = (float)Math.Round(damage, MidpointRounding.AwayFromZero);
            }

            return damage;
        }
    }
}
