using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤーの残像（一時強化の発動中に残す、データが欠けたような過去の姿）の設定。
    /// 生成の間隔と上限、寿命、見た目、寿命に応じたフェード・欠損の変化を持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "AfterimageConfig", menuName = "Config/AfterimageConfig")]
    public class AfterimageConfig : ScriptableObject
    {
        [Header("生成")]
        [SerializeField, Tooltip("一時強化（レイジ・カウンターステップ等）の発動中、前回からこの距離[m]進んだら残像を1体残す。0以下なら距離では残さない")]
        private float _spawnDistance = 1.2f;

        [SerializeField, Tooltip("一時強化の発動中、前回からこの秒数たったら残像を1体残す（立ち止まっていても残る）。距離と先に満たした方で残し、両方を数え直す。0以下なら時間では残さない")]
        private float _spawnInterval = 0.3f;

        [SerializeField, Tooltip("1フレームでこの距離[m]以上動いたら、ワープ（ボスウェーブ開始時の移動など）とみなして残像を残さず、その地点から数え直す。0以下ならワープとみなさない")]
        private float _warpDistance = 5f;

        [SerializeField, Tooltip("同時に表示する残像の上限。超えたら一番古いものを消して使い回す")]
        private int _maxCount = 6;

        [Header("寿命")]
        [SerializeField, Tooltip("残像が消えるまでの秒数")]
        private float _lifetime = 0.45f;

        [SerializeField, Tooltip("寿命の進み（0〜1）に対する不透明度の倍率。1で生成時の不透明度、0で見えない")]
        private AnimationCurve _opacityOverLife = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField, Tooltip("寿命の進み（0〜1）に対する欠損の進み。0で生成時の欠損量、1で全部欠ける")]
        private AnimationCurve _dissolveOverLife = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.6f);

        [Header("見た目")]
        [SerializeField, Tooltip("残像の描画に使うマテリアル（App/Afterimage シェーダ）。インスタンスごとの値はプロパティブロックで渡すので複製しない")]
        private Material _material;

        [SerializeField, Tooltip("生成時の見た目")]
        private AfterimageStyle _style = AfterimageStyle.Default;

        public float SpawnDistance => _spawnDistance;
        public float SpawnInterval => _spawnInterval;
        public float WarpDistance => _warpDistance;
        public int MaxCount => _maxCount;
        public float Lifetime => _lifetime;
        public Material Material => _material;
        public AfterimageStyle Style => _style;

        /// <summary>寿命の進み（0〜1）に対する不透明度の倍率</summary>
        public float EvaluateOpacity(float lifeRatio) => Mathf.Clamp01(_opacityOverLife.Evaluate(lifeRatio));

        /// <summary>寿命の進み（0〜1）に対する欠損の進み。生成時の欠損量から1までの補間率</summary>
        public float EvaluateDissolve(float lifeRatio) => Mathf.Clamp01(_dissolveOverLife.Evaluate(lifeRatio));
    }
}
