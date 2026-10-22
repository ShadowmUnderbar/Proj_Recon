using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.MasterData;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 取得済みバフの実行時状態（発動条件の進行・残り効果時間）を管理する。
    /// レイジ（被弾で効果時間が延びる OnDamaged 条件のバフ）だけは、何レベル持っていても
    /// 残り時間1本・効果1つの状態として持つ（被弾・撃破などの獲得元はどれもこの残り時間を増やす）
    /// </summary>
    public class BuffStateDataStore : IBuffStateDataStore, IRunResettable, ITickable
    {
        private class BuffState
        {
            public BuffMasterData Master;

            // HitCount条件: 次の発動に向けたヒット蓄積数
            public int HitCount;

            // HitCount / AfterDodge条件: 残り効果時間（秒）。0以下なら非アクティブ
            public float RemainingTime;

            // HpBelow条件: 条件成立中フラグ
            public bool IsConditionActive;

            // HitDifferentEnemy条件: 現在の累積効果倍率（初期値1=補正なし。上限は Master.EffectValue）
            public float StackMultiplier = 1f;

            // HitDifferentEnemy条件: 直前に命中した敵ID（-1=未命中）。同一敵の連続命中でリセット判定に使う
            public int LastHitEnemyId = -1;

            // KillWithDifferentForm条件: 次の撃破まで適用する倍率（1=補正なし）
            public float KillFormMultiplier = 1f;

            // KillWithDifferentForm条件: 直前の撃破フォーム（null=未撃破）
            public ShotType? LastKillShotType;

            // KillWithDifferentForm条件: 2つ前の撃破フォーム（null=未撃破）
            public ShotType? PreviousKillShotType;

            public bool IsActive => Master.ConditionType switch
            {
                BuffConditionType.HitCount => RemainingTime > 0f,
                BuffConditionType.HpBelow => IsConditionActive,
                BuffConditionType.HitDifferentEnemy => StackMultiplier > 1f,
                BuffConditionType.AfterDodge => RemainingTime > 0f,
                BuffConditionType.KillWithDifferentForm => KillFormMultiplier > 1f,
                // HP減少割合に比例する効果は常時発動（軽減量の算出側でHP割合を参照する）
                BuffConditionType.HpLossScaling => true,
                _ => false
            };

            // 効果時間の間だけ効く（時間で切れる）条件か
            public bool IsTimed => Master.ConditionType is BuffConditionType.HitCount or BuffConditionType.AfterDodge;

            // アクティブ時に適用する効果倍率。スタック型は累積倍率、それ以外はマスターの固定倍率
            public float EffectMultiplier => Master.ConditionType switch
            {
                BuffConditionType.HitDifferentEnemy => StackMultiplier,
                BuffConditionType.KillWithDifferentForm => KillFormMultiplier,
                _ => Master.EffectValue
            };
        }

        // レイジか。被弾で効果時間が延びる条件（OnDamaged）を持つバフをレイジとして扱う
        private static bool IsRageBuff(BuffMasterData buff) => buff.ConditionType == BuffConditionType.OnDamaged;

        // 被ダメージ軽減率の上限（軽減しきってダメージが完全に無効化されるのを防ぐ）
        private const float MaxDamageReductionRate = 0.9f;

        // レイジ以外のバフ
        private readonly List<BuffState> _buffStates = new();

        // 所持しているレイジ（レベルごとのバフ）。効果と被弾時の獲得倍率は最高レベルのものを使う
        private readonly List<BuffMasterData> _ownedRages = new();

        // レイジ未所持のまま獲得したとき（ソシオパス等）に効果に使うレイジ
        private BuffMasterData _rageWhenNotOwned;

        // レイジの残り効果時間（秒）。0以下なら非発動
        private float _rageRemainingTime;

        private float _currentHealthRatio = 1f;

        private readonly IOverclockDataStore _overclockDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        [Inject]
        public BuffStateDataStore(IOverclockDataStore overclockDataStore, IWaveManagerDataStore waveManagerDataStore)
        {
            _overclockDataStore = overclockDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Tick()
        {
            // オーバークロック中は効果時間を減らさない（付与・蓄積は通常どおり受け付ける）
            if (_overclockDataStore.IsActive.CurrentValue)
            {
                return;
            }

            foreach (var state in _buffStates)
            {
                if (state.RemainingTime > 0f)
                {
                    state.RemainingTime -= Time.deltaTime;
                }
            }

            // レイジはウェーブ間ポーズ（ショップ等）の間も残り時間を減らさない
            if (_rageRemainingTime > 0f && !_waveManagerDataStore.IsWavePause.Value)
            {
                _rageRemainingTime -= Time.deltaTime;
            }
        }

        public void AddBuff(BuffMasterData buffData)
        {
            // レイジはレベルを記録するだけ（残り時間は全レベルで共通の1本）
            if (IsRageBuff(buffData))
            {
                if (_ownedRages.All(rage => rage.Id != buffData.Id))
                {
                    _ownedRages.Add(buffData);
                }

                return;
            }

            if (_buffStates.Any(state => state.Master.Id == buffData.Id))
            {
                return;
            }

            var state = new BuffState { Master = buffData };

            // 取得時点のHP割合で条件を初期判定する
            if (buffData.ConditionType == BuffConditionType.HpBelow)
            {
                state.IsConditionActive = _currentHealthRatio <= buffData.ConditionValue;
            }

            _buffStates.Add(state);
        }

        public void NotifyHit(int damagedId)
        {
            foreach (var state in _buffStates)
            {
                switch (state.Master.ConditionType)
                {
                    case BuffConditionType.HitCount:
                        state.HitCount++;
                        if (state.HitCount < state.Master.ConditionValue)
                        {
                            break;
                        }

                        // 発動: カウンタをリセットし、効果時間をリフレッシュ（スタックはしない）
                        state.HitCount = 0;
                        state.RemainingTime = state.Master.Duration;
                        break;

                    case BuffConditionType.HitDifferentEnemy:
                        if (damagedId == state.LastHitEnemyId)
                        {
                            // 同一敵への連続命中 → 積み上げた倍率をリセット
                            state.StackMultiplier = 1f;
                        }
                        else
                        {
                            // 直前と異なる敵への命中 → 増分(ConditionValue)を加算し上限(EffectValue)でクランプ
                            state.StackMultiplier = Mathf.Min(
                                state.StackMultiplier + state.Master.ConditionValue,
                                state.Master.EffectValue);
                        }

                        state.LastHitEnemyId = damagedId;
                        break;
                }
            }
        }

        public void NotifyKill(ShotType? shotType)
        {
            // 射撃以外（回避の突進など）の撃破はフォーム履歴に含めない
            if (!shotType.HasValue)
            {
                return;
            }

            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.KillWithDifferentForm)
                {
                    continue;
                }

                // 直前の撃破と異なるフォームか（履歴が無い初回は成立させない）
                var isDifferentFromLast = state.LastKillShotType.HasValue &&
                                          state.LastKillShotType.Value != shotType.Value;

                // 2つ前の撃破とも異なるフォームか
                var isDifferentFromPrevious = state.PreviousKillShotType.HasValue &&
                                              state.PreviousKillShotType.Value != shotType.Value;

                if (!isDifferentFromLast)
                {
                    // 直前と同じフォーム（または初回）なら補正なしに戻す
                    state.KillFormMultiplier = 1f;
                }
                else
                {
                    // 2つ前とも異なれば上位倍率(ConditionValue)、直前のみ異なれば通常倍率(EffectValue)
                    state.KillFormMultiplier = isDifferentFromPrevious
                        ? Mathf.Max(state.Master.ConditionValue, state.Master.EffectValue)
                        : state.Master.EffectValue;
                }

                state.PreviousKillShotType = state.LastKillShotType;
                state.LastKillShotType = shotType;
            }
        }

        public void NotifyDamageTaken(float damage)
        {
            // 被弾でレイジを得るのはレイジのアップグレードの効果（ソシオパス等で得ただけなら延びない）
            var highestRage = GetHighestOwnedRage();
            if (highestRage == null)
            {
                return;
            }

            // 被弾ダメージ量 × 最高レベルの倍率(ConditionValue) だけ効果時間を得る（被弾のたびに累積）
            AddRageTime(damage * highestRage.ConditionValue);
        }

        public void NotifyDodge()
        {
            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.AfterDodge)
                {
                    continue;
                }

                // 回避のたびに効果時間をリフレッシュ（スタックはしない）
                state.RemainingTime = state.Master.Duration;
            }
        }

        public void SetHealthRatio(float healthRatio)
        {
            _currentHealthRatio = healthRatio;

            foreach (var state in _buffStates)
            {
                if (state.Master.ConditionType != BuffConditionType.HpBelow)
                {
                    continue;
                }

                state.IsConditionActive = healthRatio <= state.Master.ConditionValue;
            }
        }

        public float CalcMultiply(BuffEffectType effectType)
        {
            var result = 1f;
            foreach (var state in _buffStates)
            {
                if (state.Master.EffectType != effectType || !state.IsActive)
                {
                    continue;
                }

                result *= state.EffectMultiplier;
            }

            // レイジは何レベル持っていても効果は1つ（重ねない）
            var rage = GetHighestOwnedRage() ?? _rageWhenNotOwned;
            if (rage != null && rage.EffectType == effectType && IsRageActive)
            {
                result *= rage.EffectValue;
            }

            return result;
        }

        public float CalcDamageTakenMultiply()
        {
            var result = 1f;
            foreach (var state in _buffStates)
            {
                if (state.Master.EffectType != BuffEffectType.DamageReduction || !state.IsActive)
                {
                    continue;
                }

                if (state.Master.ConditionType != BuffConditionType.HpLossScaling)
                {
                    continue;
                }

                // HP減少割合 × EffectValue を軽減率とする（上限 MaxDamageReductionRate でクランプ）
                // 例: EffectValue=1.0 でHPが半分まで減っていれば軽減率0.5＝被ダメージ半減
                var lossRatio = Mathf.Clamp01(1f - _currentHealthRatio);
                var reduction = Mathf.Clamp(lossRatio * state.Master.EffectValue, 0f, MaxDamageReductionRate);
                result *= 1f - reduction;
            }

            return result;
        }

        public bool IsRageActive => _rageRemainingTime > 0f;

        public bool IsTimedBuffActive
        {
            get
            {
                if (IsRageActive)
                {
                    return true;
                }

                foreach (var state in _buffStates)
                {
                    if (state.IsTimed && state.IsActive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Tickで0をわずかに下回ることがあるので0で止める
        public float RageRemainingTime => Mathf.Max(_rageRemainingTime, 0f);

        public void GainRage(float seconds, BuffMasterData rageWhenNotOwned)
        {
            if (_ownedRages.Count == 0)
            {
                // レイジ以外のバフを渡されると効果を誤って掛けるので受け付けない
                if (rageWhenNotOwned == null || !IsRageBuff(rageWhenNotOwned))
                {
                    Debug.LogWarning($"[BuffStateDataStore] レイジ未所持で、レイジとして発動できるバフ \"{rageWhenNotOwned?.Id}\" が渡されませんでした");
                    return;
                }

                _rageWhenNotOwned = rageWhenNotOwned;
            }

            AddRageTime(seconds);
        }

        // 所持しているレイジのうち最高レベル（被弾時の獲得倍率が最大）のもの。未所持なら null
        private BuffMasterData GetHighestOwnedRage()
        {
            BuffMasterData highest = null;
            foreach (var rage in _ownedRages)
            {
                if (highest == null || rage.ConditionValue > highest.ConditionValue)
                {
                    highest = rage;
                }
            }

            return highest;
        }

        // 発動中なら残り時間に足し、切れていれば（Tickで0をわずかに下回ることがある）0から発動させる
        private void AddRageTime(float seconds)
        {
            _rageRemainingTime = Mathf.Max(_rageRemainingTime, 0f) + seconds;
        }

        public void ResetRun()
        {
            _buffStates.Clear();
            _ownedRages.Clear();
            _rageWhenNotOwned = null;
            _rageRemainingTime = 0f;
            _currentHealthRatio = 1f;
        }
    }
}
