using System;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>複数体同時発生を数える対象の種別</summary>
    public enum StreamerCameraMultiTargetCountType
    {
        /// <summary>撃破数で数える（マルチキル）</summary>
        Kill,

        /// <summary>命中した敵の体数で数える（複数体同時ヒット）</summary>
        Damage,
    }

    /// <summary>
    /// 「一定時間内に○体を倒した／当てた」ときにカットインを入れる条件。
    /// 体数・時間窓・対象種別をデータで指定できるようにしている。
    /// </summary>
    [Serializable]
    public class StreamerCameraMultiTargetRule
    {
        [SerializeField, Tooltip("識別用の名前（ログ・クールダウン用）")]
        private string _ruleName = "MultiKill";

        [SerializeField, Tooltip("撃破/命中の対象種別")]
        private StreamerCameraMultiTargetCountType _countType = StreamerCameraMultiTargetCountType.Kill;

        [SerializeField, Tooltip("この体数に達したら発動する")]
        private int _targetCount = 3;

        [SerializeField, Tooltip("体数を数える時間窓（秒）")]
        private float _windowSeconds = 1.0f;

        [SerializeField, Tooltip("発動するショット。未設定のルールは無視される")]
        private StreamerCameraShotData _shotData;

        public string RuleName => _ruleName;
        public StreamerCameraMultiTargetCountType CountType => _countType;
        public int TargetCount => Mathf.Max(2, _targetCount);
        public float WindowSeconds => Mathf.Max(0.01f, _windowSeconds);
        public StreamerCameraShotData ShotData => _shotData;

        public bool IsValid => _shotData != null;
    }
}
