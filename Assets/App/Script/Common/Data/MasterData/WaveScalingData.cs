using App.Framework;
using UnityEngine;

namespace App.Common.Data.MasterData
{
    /// <summary>
    /// ウェーブ進行による敵強化の倍率を、ウェーブ帯ごとに切り替えるための1段階ぶんのマスターデータ。
    /// スプレッドシートの WaveScalingData シートからインポートする。
    /// </summary>
    [CreateAssetMenu(fileName = "WaveScalingMasterData", menuName = "MasterData/WaveScalingMasterData")]
    public class WaveScalingMasterData : ScriptableObject
    {
        [SerializeField, ReadOnlyAttribute] private int _id;
        public int Id => _id;

        // この段階の倍率が適用され始めるウェーブ番号（1以上）
        [SerializeField, ReadOnlyAttribute] private int _wave;
        public int Wave => _wave;

        // この段階の攻撃力倍率（1.2で基礎値の1.2倍）
        [SerializeField, ReadOnlyAttribute] private float _atkBuff;
        public float AtkBuff => _atkBuff;

        // この段階のHP倍率（1.2で基礎値の1.2倍）
        [SerializeField, ReadOnlyAttribute] private float _hpBuff;
        public float HpBuff => _hpBuff;
    }
}
