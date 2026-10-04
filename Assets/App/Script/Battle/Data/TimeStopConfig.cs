using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ボスによる時止めの演出の設定。時止めの始まり・終わりそのものはボスの台本が決める。
    /// </summary>
    [CreateAssetMenu(fileName = "TimeStopConfig", menuName = "Config/TimeStopConfig")]
    public class TimeStopConfig : ScriptableObject
    {
        [SerializeField, Tooltip("時止めの間にかけるセピア調（プレイヤー側の色を変える）。未指定ならかけない")]
        private SepiaTonePreset _sepiaTonePreset;

        public SepiaTonePreset SepiaTonePreset => _sepiaTonePreset;
    }
}
