using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerAimIKView
    {
        // 両手のエイム対象ワールド座標を設定する（毎フレーム更新）
        void SetAimTargets(Vector3 leftTarget, Vector3 rightTarget);

        // エイムIKの有効・無効を切り替える。
        // 無効にするとウェイトが0へフェードし、腕が再生中のアニメの姿勢に従う（死亡アニメ用）
        void SetEnable(bool enable);
    }
}
