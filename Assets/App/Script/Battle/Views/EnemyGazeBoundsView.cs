using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 注視系アップグレード（スネークアイズ・メデューサ・ガン飛ばし）で「視線に捉えた」とみなす敵ごとの判定球。
    /// 敵プレハブのルート（EnemyView と同じ GameObject）に付け、形は Inspector の値だけで決める。
    /// 判定そのものは <see cref="EnemyGazeTracker"/> が順番に行う（自分では Update しない）。
    /// ヒットボックスより小さいと視線上の敵を取りこぼすため、迷ったら大きめにする
    /// </summary>
    public class EnemyGazeBoundsView : MonoBehaviour
    {
        // ギズモの色（選択中のみ表示）
        private static readonly Color GizmoColor = new(1f, 0.6f, 0.1f, 0.8f);

        [SerializeField, Tooltip("判定球の中心の高さ[m]。このTransformの位置（足元）から真上へのオフセット。スケールは反映しない")]
        private float _centerHeight = 1.5f;

        [SerializeField, Min(0f), Tooltip("判定球の半径[m]。スケールは反映しない。ヒットボックスを包む大きさにすると取りこぼさない")]
        private float _radius = 1.25f;

        /// <summary>判定球の中心（ワールド座標）</summary>
        public Vector3 Center => transform.position + Vector3.up * _centerHeight;

        /// <summary>判定球の半径[m]</summary>
        public float Radius => _radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(Center, _radius);
        }
    }
}
