using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerAimIKView
    {
        // 両手のエイム対象ワールド座標を設定する（毎フレーム更新）
        void SetAimTargets(Vector3 leftTarget, Vector3 rightTarget);
    }
}
