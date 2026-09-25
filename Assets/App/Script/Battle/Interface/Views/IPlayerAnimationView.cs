using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerAnimationView
    {
        // ワールド入力をモデルの向き基準でAnimatorのブレンド用パラメータへ反映する
        void SetMoveDirection(Vector2 worldMove);

        // モデルをエイム中心方向へ振り向かせる
        void SetFacingDirection(Vector3 dir);

        // 死亡アニメを頭から再生する（専用レイヤーを全身に効かせる）
        void PlayDeath();

        // 死亡アニメが最後まで再生されたか。未再生なら true（待たせないため）
        bool IsDeathFinished { get; }

        // 死亡アニメを解除して通常の移動アニメへ戻す（リスタート時に呼ぶ）
        void ResetDeath();
    }
}
