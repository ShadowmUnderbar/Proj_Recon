using System.Collections.Generic;
using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 配信用カメラの演出状態（再生中フラグ・優先度・クールダウン）と、
    /// 複数体同時撃破／同時命中の判定材料を保持する。
    /// </summary>
    public interface IStreamerCameraDataStore
    {
        /// <summary>演出ショットの再生中か</summary>
        ReadOnlyReactiveProperty<bool> IsPlaying { get; }

        /// <summary>経過時間を進める（クールダウンと時間窓の基準）</summary>
        void AddElapsedTime(float deltaTime);

        void RecordKill(int enemyId, Vector3 position);
        void RecordDamage(int enemyId, Vector3 position);

        /// <summary>ルールの条件を満たしていれば、被写体の座標一覧を返す</summary>
        bool TryCollectMultiTargetPositions(
            StreamerCameraMultiTargetRule rule,
            out IReadOnlyList<Vector3> subjectPositions
        );

        /// <summary>指定種別の履歴を空にする（発動直後の連鎖発動を防ぐ）</summary>
        void ClearMultiTargetHistory(StreamerCameraMultiTargetCountType countType);

        /// <summary>クールダウンと優先度から、このショットを今再生できるか判定する</summary>
        bool CanPlay(StreamerCameraShotData shotData);

        /// <summary>再生開始を記録し、クールダウンを開始する</summary>
        void BeginShot(StreamerCameraShotData shotData);

        /// <summary>再生終了を記録する</summary>
        void EndShot();
    }
}
