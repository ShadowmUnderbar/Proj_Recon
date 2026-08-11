using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 1回の演出再生リクエスト。
    /// 被写体はワールド座標のスナップショットで渡す（撃破済みの敵でも構図が崩れないようにするため）。
    /// </summary>
    public class StreamerCameraShotRequest
    {
        public StreamerCameraShotRequest(
            StreamerCameraShotData shotData,
            IReadOnlyList<Vector3> subjectPositions,
            string triggerName
        )
        {
            ShotData = shotData;
            SubjectPositions = subjectPositions;
            TriggerName = triggerName;
        }

        public StreamerCameraShotData ShotData { get; }

        /// <summary>被写体のワールド座標一覧。空の場合はプレイヤー前方を被写体として扱う</summary>
        public IReadOnlyList<Vector3> SubjectPositions { get; }

        /// <summary>ログ用のトリガー名</summary>
        public string TriggerName { get; }
    }
}
