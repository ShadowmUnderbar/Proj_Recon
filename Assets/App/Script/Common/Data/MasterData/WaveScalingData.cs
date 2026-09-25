using App.Framework;
using UnityEngine;

namespace App.Common.Data.MasterData
{
    /// <summary>
    /// ウェーブ進行による敵強化の増加率を、ウェーブ帯ごとに切り替えるための1段階ぶんのマスターデータ。
    /// スプレッドシートの WaveScalingData シートからインポートする。
    /// </summary>
    [CreateAssetMenu(fileName = "WaveScalingMasterData", menuName = "MasterData/WaveScalingMasterData")]
    public class WaveScalingMasterData : ScriptableObject
    {
        [SerializeField, ReadOnlyAttribute] private int _id;
        public int Id => _id;

        // この段階の増加率が適用され始めるウェーブ番号（1以上）
        [SerializeField, ReadOnlyAttribute] private int _wave;
        public int Wave => _wave;

        // 1ウェーブ進むごとの攻撃力増加率（0.05で+5%/ウェーブ）
        [SerializeField, ReadOnlyAttribute] private float _atkBuff;
        public float AtkBuff => _atkBuff;

        // 1ウェーブ進むごとのHP増加率（0.1で+10%/ウェーブ）
        [SerializeField, ReadOnlyAttribute] private float _hpBuff;
        public float HpBuff => _hpBuff;
    }
}
