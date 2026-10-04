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
    /// 以後はプレイヤーがどう動いても位置関係と距離が崩れないよう、経路探索ではなく毎フレーム配置先へ直接追従する（FollowTo）。
    /// 行動0（弾幕）: 配置の方向へ一定距離を保ったままプレイヤーと並ぶ位置（横のずれなし）を追い、
    ///   移動方向と直交する向き（プレイヤーの側）へ弾を連射する。長さは EnemyMasterData の ActiveTime。
    /// 行動1（帯の攻撃）: その場に留まり、プレイヤーの側へ伸びる帯を予兆として出し、予兆が明けた瞬間に帯の中のプレイヤーへ当てる。
    ///   範囲・秒数・ダメージは BossLineStrikeConfig。
    /// 行動2（帯の連続攻撃）: 行動1と同じ帯を、予兆1回のあと同じ向き・同じ位置のまま予兆なしで続けて当てる（×字の配置で使う）。
    ///   回数と間隔は BossLineStrikeConfig の RepeatCount / RepeatInterval。
    /// 行動3（回りこみ連射）: 命令された向きへプレイヤーを中心に90度回りこみながら、プレイヤーへ向けて弾を連射する。
    ///   撃った弾はその場に止めておき、時止めの解除で動き出す（台本の TimeStopOrbit で、時止め中にだけ使う）。
    /// 行動4（回りこみ）: 行動3と同じく回りこむだけで撃たない（行動3の相方）。
    ///   どちらも回りこみ終えたら、回りこんだ先の配置（上→右など）につき直す。秒数・間隔は Inspector の値。
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

        /// <summary>回りこみながら連射する行動番号（時止め中に使う。撃った弾は時止めの解除まで止まる）</summary>
        public const int OrbitBarrageActionIndex = 3;

        /// <summary>回りこむだけの行動番号（回りこみ連射の相方）</summary>
        public const int OrbitActionIndex = 4;

        [SerializeField] private BaseBulletView _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        [SerializeField, Min(1f), Tooltip("プレイヤーとの距離（m）。配置のつき直し・追従でこの距離を保つ")]
        private float _keepDistance = 15f;

        [SerializeField, Min(0.02f), Tooltip("弾幕で弾を撃つ間隔（秒）")]
        private float _fireInterval = 0.2f;

        [Header("回りこみ（時止め）")]
        [SerializeField, Min(0.1f), Tooltip("プレイヤーを中心に90度回りこむのに掛ける秒数")]
        private float _orbitSeconds = 1f;

        [SerializeField, Min(0.02f), Tooltip("回りこみ連射で弾を撃つ間隔（秒）")]
        private float _orbitFireInterval = 0.15f;

        [Header("帯の攻撃")]
        [SerializeField, Tooltip("帯の攻撃の範囲・秒数・ダメージ・色")]
        private BossLineStrikeConfig _lineStrikeConfig;

        [SerializeField, Tooltip("帯の表示（地面に置く半透明の帯）のプレハブ")]
        private BossLineStrikeView _lineStrikeViewPrefab;

        // 帯の当たり判定の高さ（m）。地面からプレイヤーの頭上までを覆う
        private const float StrikeHitHeight = 4f;

        // 回りこむ角度（度）
        private const float OrbitAngle = 90f;

        // 次の弾までの残り時間（秒）
        private float _fireTimer;

        // 回りこみの起点の向き（プレイヤーから見た方向）と、回りこみ始めてからの秒数
        private Vector3 _orbitStartDirection;
        private float _orbitElapsed;

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
            if (IsOrbit(actionIndex))
            {
                // 回りこんでいる間をまるごと攻撃の段階にする（時止めの長さ＝回りこみの秒数）
                windup = 0f;
                active = _orbitSeconds;
                recovery = 0f;
                return;
            }

            if (IsLineStrike(actionIndex) && _lineStrikeConfig != null)
            {
                windup = _lineStrikeConfig.TelegraphSeconds;
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
            if (FormationSlot == BossFormationSlot.None || PlayerTransform == null)
            {
                return;
            }

            // 回りこみの移動と向きは OnActionActiveUpdate で行う
            if (IsActing && IsOrbit(CurrentActionIndex))
            {
                return;
            }

            FaceFireDirection();

            // 帯の攻撃の間はその場に留まる（予兆と攻撃の範囲を動かさない）
            if (IsActing && IsLineStrike(CurrentActionIndex))
            {
                return;
            }

            // 弾幕中はプレイヤーと並ぶ（横のずれなし）。どちらもプレイヤーの動きに遅れず距離を保つ
            FollowTo(GetFormationPosition(IsActing ? 0f : FormationLateralOffset));
        }

        protected override void OnActionWindup(int actionIndex)
        {
            if (IsLineStrike(actionIndex))
            {
                BeginTelegraph();
                return;
            }

            if (IsOrbit(actionIndex))
            {
                BeginOrbit();
            }

            // 攻撃の段階に入った最初のフレームで1発目を撃つ
            _fireTimer = 0f;
        }

        protected override void OnActionActive(int actionIndex)
        {
            if (!IsLineStrike(actionIndex))
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

            if (IsOrbit(actionIndex))
            {
                UpdateOrbit(actionIndex, deltaTime);
                return;
            }

            if (actionIndex != BarrageActionIndex || FormationSlot == BossFormationSlot.None)
            {
                return;
            }

            _fireTimer -= deltaTime;
            while (_fireTimer <= 0f)
            {
                Fire(GetFireDirection(), false);
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

            // 回りこんだ先の配置につき直す（以後の追従・弾幕・帯の向きがその配置になる）。
            // 最後のフレームの端数で回りきれなかった分もここで詰める。途中で打ち切られたときも回りこんだ先へ移す（撃破されたときは動かさない）
            if (IsOrbit(actionIndex) && FormationSlot != BossFormationSlot.None && PlayerTransform != null
                && State.Value != EnemyAIState.Dead)
            {
                SetFormation(FormationSlot.Turn90(CurrentTurnDirection), 0f);
            }
        }

        /// <summary>回りこみの起点を決める（配置の方向から回り始める。横のずれは無くす）</summary>
        private void BeginOrbit()
        {
            _orbitElapsed = 0f;
            if (FormationSlot != BossFormationSlot.None || PlayerTransform == null)
            {
                _orbitStartDirection = FormationSlot.ToDirection();
                return;
            }

            // 配置の指定が無ければ、今いる方向から回り始める
            var offset = transform.position - PlayerTransform.position;
            offset.y = 0f;
            _orbitStartDirection = offset.sqrMagnitude > 0f ? offset.normalized : Vector3.forward;
        }

        /// <summary>プレイヤーを中心に、一定距離を保ったまま回りこむ。回りこみ連射ならプレイヤーへ向けて撃つ</summary>
        private void UpdateOrbit(int actionIndex, float deltaTime)
        {
            if (PlayerTransform == null || _orbitStartDirection == Vector3.zero)
            {
                return;
            }

            _orbitElapsed += deltaTime;
            var progress = Mathf.Clamp01(_orbitElapsed / _orbitSeconds);
            var angle = OrbitAngle * progress * CurrentTurnDirection.ToSign();
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * _orbitStartDirection;

            FollowTo(PlayerTransform.position + direction * _keepDistance);
            transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);

            if (actionIndex != OrbitBarrageActionIndex)
            {
                return;
            }

            _fireTimer -= deltaTime;
            while (_fireTimer <= 0f)
            {
                Fire(-direction, true);
                _fireTimer += _orbitFireInterval;
            }
        }

        private static bool IsOrbit(int actionIndex)
        {
            return actionIndex == OrbitBarrageActionIndex || actionIndex == OrbitActionIndex;
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

        /// <summary>帯を出す行動か（どれもその場に留まる）</summary>
        private static bool IsLineStrike(int actionIndex)
        {
            return actionIndex == LineStrikeActionIndex || actionIndex == RepeatLineStrikeActionIndex;
        }

        /// <summary>弾を撃つ。isHeld なら撃った弾をその場に止めておく（時止めの解除で BulletStoreView が一括で動かす）</summary>
        private void Fire(Vector3 direction, bool isHeld)
        {
            if (_bulletPrefab == null || _muzzleTransform == null)
            {
                Debug.LogError($"[{nameof(BossTickTock)}] {name}: 弾プレハブまたは銃口が未設定です", this);
                return;
            }

            var pose = new Pose(_muzzleTransform.position, Quaternion.LookRotation(direction, Vector3.up));
            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(EnemyId, pose, EnemyData.CreateBulletData(), -1, PlayerTransform);

            // 時止め中に生まれた弾は BulletStoreView の一括停止に間に合わないため、自分で止める
            if (isHeld)
            {
                bullet.SetPause(true);
            }
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
            return GetFormationPosition(FormationLateralOffset);
        }

        /// <summary>配置先（プレイヤーからその方向へ一定距離、横へ lateralOffset のずれ）</summary>
        private Vector3 GetFormationPosition(float lateralOffset)
        {
            // 縦方向にいれば横＝X、横方向にいれば横＝Z へずらす
            var lateral = FormationSlot.IsVertical() ? Vector3.right : Vector3.forward;
            return PlayerTransform.position + FormationSlot.ToDirection() * _keepDistance + lateral * lateralOffset;
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
