using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 回避の移動が終わった瞬間の情報。
    /// 回避時跳ね返し攻撃は、この終了地点を頂点・この方向を中心軸として扇形範囲を張る。
    /// </summary>
    public readonly struct DodgeEndData
    {
        public DodgeEndData(Vector3 endPosition, Vector3 direction)
        {
            EndPosition = endPosition;
            Direction = direction;
        }

        /// <summary>回避終了地点</summary>
        public Vector3 EndPosition { get; }

        /// <summary>回避方向（水平・正規化済み）</summary>
        public Vector3 Direction { get; }
    }
}
