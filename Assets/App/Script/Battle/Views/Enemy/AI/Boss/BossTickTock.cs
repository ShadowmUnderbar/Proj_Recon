using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Views.Enemy.Bullet;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// プレイヤーの上下左右（ワールドの軸）のいずれかについて動く、二人組ボス「TickTock」用のAI。
    /// 台本の配置（CrossFormation）を受けると、プレイヤーからその方向へ一定距離、横へ指定のずれだけ離れた位置へ瞬間移動する。
    /// 行動していない間は、その位置で距離を保ったままプレイヤーを追う。
    /// 行動0（弾幕）: 縦方向（上下）にいれば横へ、横方向（左右）にいれば縦へ、プレイヤーに合わせて軸に沿って動き、
    ///   移動方向と直交する向き（プレイヤーの側）へ弾を連射する。長さは EnemyMasterData の ActiveTime。
    /// 行動1（帯の攻撃）: その場に留まり、プレイヤーの側へ伸びる帯を予兆として出し、予兆が明けた瞬間に帯の中のプレイヤーへ当てる。
    ///   範囲・秒数・ダメージは BossLineStrikeConfig。
    /// 行動2（帯の連続攻撃）: 行動1と同じ帯を、予兆1回のあと同じ向き・同じ位置のまま予兆なしで続けて当てる（×字の配置で使う）。
    ///   回数と間隔は BossLineStrikeConfig の RepeatCount / RepeatInterval。
    /// 行動3（時止め中の予兆）: 行動1と同じ帯の予兆だけを出し、当てずに消す（台本の TimeStopMemory で使う）。
    ///   秒数は BossLineStrikeConfig の MemoryTelegraphSeconds / MemoryIntervalSeconds。
    /// 行動4（時止め明けの攻撃）: 行動1と同じ帯を、短い予兆（ReplayTelegraphSeconds）のあとに当てる。
    /// プレハブでは基底の「行動中は移動を止める」を切っておくこと（弾幕中も動くため）。
    /// </summary>
    public class BossTickTock : BossAIBase
    {
        /// <summary>弾幕の行動番号</summary>
        public const int BarrageActionIndex = 0;

        /// <summary>帯の攻撃の行動番号</summary>
        public const int LineStrikeActionIndex = 1;

        /// <summary>帯の連続攻撃の行動番号（予兆1回のあと続けて当てる）</summary>
        public const int RepeatLineStrikeActionIndex = 2;

        /// <summary>時止め中に予兆だけを見せる行動番号（当てない）</summary>
        public const int MemoryTelegraphActionIndex = 3;

        /// <summary>時止めが明けたあと、短い予兆で帯を当てる行動番号</summary>
        public const int ReplayLineStrikeActionIndex = 4;

        [SerializeField] private BaseBulletView _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        [SerializeField, Min(1f), Tooltip("プレイヤーとの距離（m）。配置のつき直し・追跡でこの距離を保つ")]
        private float _keepDistance = 15f;

        [SerializeField, Min(0.02f), Tooltip("弾幕で弾を撃つ間隔（秒）")]
        private float _fireInterval = 0.2f;

        [Header("帯の攻撃")]
        [SerializeField, Tooltip("帯の攻撃の範囲・秒数・ダメージ・色")]
        private BossLineStrikeConfig _lineStrikeConfig;

        [SerializeField, Tooltip("帯の表示（地面に置く半透明の帯）のプレハブ")]
        private BossLineStrikeView _lineStrikeViewPrefab;

        // 帯の当たり判定の高さ（m）。地面からプレイヤーの頭上までを覆う
        private const float StrikeHitHeight = 4f;

        // 弾幕中に保つ座標（縦方向にいればZ、横方向にいればX）。行動開始時の位置で固定し、軸に沿ってだけ動く
        private float _barrageLine;

        // 次の弾までの残り時間（秒）
        private float _fireTimer;

        // 帯の攻撃の向きと起点（予兆を出した時点で固定する）
        private Vector3 _strikeOrigin;
        private Vector3 _strikeDirection;

        // 連続攻撃の残り回数と、次の攻撃までの残り時間（秒）
        private int _repeatRemaining;
        private float _repeatTimer;

        private BossLineStrikeView _lineStrikeView;
        // 帯は長いので地面・ボス・弾など判定と無関係なコライダーも入る。あふれるとプレイヤーを取りこぼすため余裕を持たせる
        private readonly Collider[] _strikeHits = new Collider[32];

        protected override void GetActionDurations(int actionIndex, out float windup, out float active, out float recovery)
        {
            if (actionIndex == MemoryTelegraphActionIndex && _lineStrikeConfig != null)
            {
                // 攻撃の段階は無く、予兆を消してから次の予兆までの間を硬直にする
                windup = _lineStrikeConfig.MemoryTelegraphSeconds;
                active = 0f;
                recovery = _lineStrikeConfig.MemoryIntervalSeconds;
                return;
            }

            if (IsLineStrike(actionIndex) && _lineStrikeConfig != null)
            {
                windup = actionIndex == ReplayLineStrikeActionIndex
                    ? _lineStrikeConfig.ReplayTelegraphSeconds
                    : _lineStrikeConfig.TelegraphSeconds;
                // 連続攻撃は最後の1回の表示が終わるまでを攻撃の段階にする
                active = actionIndex == RepeatLineStrikeActionIndex
                    ? _lineStrikeConfig.RepeatInterval * (_lineStrikeConfig.RepeatCount - 1) + _lineStrikeConfig.StrikeSeconds
                    : _lineStrikeConfig.StrikeSeconds;
                recovery = _lineStrikeConfig.RecoverySeconds;
                return;
            }

            base.GetActionDurations(actionIndex, out windup, out active, out recovery);
        }

        protected override void OnFormationAssigned(BossFormationSlot slot)
        {
            if (slot == BossFormationSlot.None || PlayerTransform == null)
            {
                return;
            }

            TeleportTo(GetFormationPosition());
            FaceFireDirection();
        }

        protected override void BattleMove()
        {
            if (FormationSlot == BossFormationSlot.None)
            {
                return;
            }

            FaceFireDirection();

            if (!IsActing)
            {
                SetAgentDestination(GetFormationPosition());
                return;
            }

            // 帯の攻撃の間はその場に留まる（予兆と攻撃の範囲を動かさない）
            SetAgentDestination(IsLineStrike(CurrentActionIndex) ? transform.position : GetBarragePosition());
        }

        protected override void OnActionWindup(int actionIndex)
        {
            if (IsLineStrike(actionIndex))
            {
                BeginTelegraph();
                return;
            }

            _barrageLine = FormationSlot.IsVertical() ? transform.position.z : transform.position.x;

            // 攻撃の段階に入った最初のフレームで1発目を撃つ
            _fireTimer = 0f;
        }

        protected override void OnActionActive(int actionIndex)
        {
            // 時止め中の予兆は当てない
            if (!IsLineStrike(actionIndex) || actionIndex == MemoryTelegraphActionIndex)
            {
                return;
            }

            Strike();

            if (actionIndex == RepeatLineStrikeActionIndex && _lineStrikeConfig != null)
            {
                _repeatRemaining = _lineStrikeConfig.RepeatCount - 1;
                _repeatTimer = _lineStrikeConfig.RepeatInterval;
            }
        }

        protected override void OnActionActiveUpdate(int actionIndex, float deltaTime)
        {
            if (actionIndex == RepeatLineStrikeActionIndex)
            {
                UpdateRepeatStrike(deltaTime);
                return;
            }

            if (actionIndex != BarrageActionIndex || FormationSlot == BossFormationSlot.None)
            {
                return;
            }

            _fireTimer -= deltaTime;
            while (_fireTimer <= 0f)
            {
                Fire();
                _fireTimer += _fireInterval;
            }
        }

        protected override void OnActionRecovery(int actionIndex)
        {
            if (IsLineStrike(actionIndex) && _lineStrikeView != null)
            {
                _lineStrikeView.Hide();
            }
        }

        protected override void OnActionFinished(int actionIndex)
        {
            // 予兆の途中で打ち切られた（スタン・撃破・台本の切り替え）ときも帯を残さない
            if (_lineStrikeView != null)
            {
                _lineStrikeView.Hide();
            }
        }

        /// <summary>2回目以降の連続攻撃。予兆なしで、1回目と同じ向き・同じ位置へ間隔ごとに当てる</summary>
        private void UpdateRepeatStrike(float deltaTime)
        {
            if (_lineStrikeConfig == null)
            {
                return;
            }

            _repeatTimer -= deltaTime;
            while (_repeatRemaining > 0 && _repeatTimer <= 0f)
            {
                Strike();
                _repeatRemaining--;
                _repeatTimer += _lineStrikeConfig.RepeatInterval;
            }
        }

        /// <summary>帯を出す行動か（予兆だけの行動も含む。どれもその場に留まる）</summary>
        private static bool IsLineStrike(int actionIndex)
        {
            return actionIndex == LineStrikeActionIndex || actionIndex == RepeatLineStrikeActionIndex
                || actionIndex == MemoryTelegraphActionIndex || actionIndex == ReplayLineStrikeActionIndex;
        }

        private void Fire()
        {
            if (_bulletPrefab == null || _muzzleTransform == null)
            {
                Debug.LogError($"[{nameof(BossTickTock)}] {name}: 弾プレハブまたは銃口が未設定です", this);
                return;
            }

            var pose = new Pose(_muzzleTransform.position, Quaternion.LookRotation(GetFireDirection(), Vector3.up));
            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(EnemyId, pose, EnemyData.CreateBulletData(), -1, PlayerTransform);
        }

        private void BeginTelegraph()
        {
            _strikeOrigin = transform.position;
            _strikeDirection = GetFireDirection();

            if (_lineStrikeConfig == null || _lineStrikeViewPrefab == null)
            {
                Debug.LogError($"[{nameof(BossTickTock)}] {name}: 帯の攻撃の設定または表示のプレハブが未設定です", this);
                return;
            }

            if (_lineStrikeView == null)
            {
                _lineStrikeView = Instantiate(_lineStrikeViewPrefab);
                _lineStrikeView.name = $"{name}_LineStrike";
            }

            _lineStrikeView.Show(_strikeOrigin, _strikeDirection, _lineStrikeConfig.Length, _lineStrikeConfig.Width,
                _lineStrikeConfig.TelegraphColor);
        }

        /// <summary>予兆が明けた瞬間に、帯の中にいるプレイヤーへ1回だけ当てる</summary>
        private void Strike()
        {
            if (_lineStrikeConfig == null)
            {
                return;
            }

            if (_lineStrikeView != null)
            {
                _lineStrikeView.SetColor(_lineStrikeConfig.StrikeColor);
            }

            var length = _lineStrikeConfig.Length;
            var center = _strikeOrigin + _strikeDirection * (length * 0.5f) + Vector3.up * (StrikeHitHeight * 0.5f);
            var halfExtents = new Vector3(_lineStrikeConfig.Width * 0.5f, StrikeHitHeight * 0.5f, length * 0.5f);
            var rotation = Quaternion.LookRotation(_strikeDirection, Vector3.up);

            // ポイント粒子はダメージ対象ではないうえバッファを埋めるため除外する（Rush と同じ）
            var count = Physics.OverlapBoxNonAlloc(center, halfExtents, _strikeHits, rotation,
                Physics.AllLayers & ~LayerConstants.PointParticle);

            if (count == _strikeHits.Length)
            {
                Debug.LogWarning($"[{nameof(BossTickTock)}] {name}: 帯の判定のコライダーがバッファ（{_strikeHits.Length}）を埋めました。プレイヤーを取りこぼしている可能性があります", this);
            }

            var damage = EnemyData.BaseDamage * _lineStrikeConfig.DamageMultiplier;
            var hitPlayerId = int.MinValue;
            for (var i = 0; i < count; i++)
            {
                // 当てるのはプレイヤーだけ。プレイヤーのコライダーが複数あっても1回だけ当てる
                if (!_strikeHits[i].TryGetComponent(out IHitBoxView hitBox) || hitBox.HitBoxType != HitBoxType.Player
                    || hitBox.Id == hitPlayerId)
                {
                    continue;
                }

                hitPlayerId = hitBox.Id;
                hitBox.OnHit(damage, EnemyId, transform.position, out _);
            }
        }

        /// <summary>配置先（プレイヤーからその方向へ一定距離、横へ指定のずれ）</summary>
        private Vector3 GetFormationPosition()
        {
            // 縦方向にいれば横＝X、横方向にいれば横＝Z へずらす
            var lateral = FormationSlot.IsVertical() ? Vector3.right : Vector3.forward;
            return PlayerTransform.position + FormationSlot.ToDirection() * _keepDistance + lateral * FormationLateralOffset;
        }

        /// <summary>弾幕中の移動先（自分の軸の線上で、プレイヤーと並ぶ位置）</summary>
        private Vector3 GetBarragePosition()
        {
            var player = PlayerTransform.position;
            return FormationSlot.IsVertical()
                ? new Vector3(player.x, transform.position.y, _barrageLine)
                : new Vector3(_barrageLine, transform.position.y, player.z);
        }

        /// <summary>弾・帯の向き（移動方向と直交し、プレイヤーの側を向く）</summary>
        private Vector3 GetFireDirection()
        {
            return -FormationSlot.ToDirection();
        }

        private void FaceFireDirection()
        {
            transform.rotation = Quaternion.LookRotation(GetFireDirection(), Vector3.up);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_lineStrikeView != null)
            {
                Destroy(_lineStrikeView.gameObject);
            }
        }
    }
}
