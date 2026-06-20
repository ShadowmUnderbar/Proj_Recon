using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerAnimationView
    {
        // ワールド入力をモデルの向き基準でAnimatorのブレンド用パラメータへ反映する
        void SetMoveDirection(Vector2 worldMove);

        // モデルをエイム中心方向へ振り向かせる
        void SetFacingDirection(Vector3 dir);
    }
}
