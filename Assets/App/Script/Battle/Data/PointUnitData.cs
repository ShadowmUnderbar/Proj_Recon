using System;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ポイント粒子1個分の単位定義（獲得値・色・大きさ）。
    /// 単位が大きいほど色とサイズで目立たせ、遠目でも価値が分かるようにする。
    /// </summary>
    [Serializable]
    public class PointUnitData
    {
        [SerializeField, Tooltip("この粒子1個を回収したときに得られるポイント")]
        private int _value = 1;

        [SerializeField, Tooltip("粒子の色")]
        private Color _color = Color.white;

        [SerializeField, Tooltip("粒子の大きさ（プレハブのスケール倍率）")]
        private float _scale = 1f;

        public int Value => _value;
        public Color Color => _color;
        public float Scale => _scale;
    }
}
