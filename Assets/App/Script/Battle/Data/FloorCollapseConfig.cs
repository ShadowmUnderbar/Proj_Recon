using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ステージ遷移用の床崩壊（床がデータプレートに割れ、沈み・傾き・外へずれて消える）の設定。
    /// 特定のバイオームに依存しないよう、床そのものではなく演出用のプレート床へ差し替えて動かす
    /// </summary>
    [CreateAssetMenu(fileName = "FloorCollapseConfig", menuName = "Config/FloorCollapseConfig")]
    public class FloorCollapseConfig : ScriptableObject
    {
        [SerializeField, Tooltip("ウェーブクリア（ショップを開く時点）で崩壊を再生する。切ると外部から呼んだときだけ再生する")]
        private bool _playOnWaveClear = true;

        [SerializeField, Tooltip("プレートの描画に使うマテリアル（App/FloorCollapse シェーダ）。床ごとの色・テクスチャはプロパティブロックで渡すので複製しない")]
        private Material _plateMaterial;

        [SerializeField, Tooltip("プレートの隙間から見える暗い空間のマテリアル")]
        private Material _voidMaterial;

        [SerializeField, Min(1f), Tooltip("暗い空間を床からどれだけ下に置くか[m]。沈下量より深くする")]
        private float _voidDepth = 40f;

        [SerializeField, Tooltip("再生ごとの見た目と動き")]
        private FloorCollapseSettings _settings = FloorCollapseSettings.Default;

        public bool PlayOnWaveClear => _playOnWaveClear;
        public Material PlateMaterial => _plateMaterial;
        public Material VoidMaterial => _voidMaterial;
        public float VoidDepth => _voidDepth;
        public FloorCollapseSettings Settings => _settings;
    }
}
