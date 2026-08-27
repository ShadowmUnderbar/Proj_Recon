using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 回避時跳ね返し攻撃（カウンター）のレイ演出。
    /// 弾の曳光弾（<see cref="BulletTracerView"/>）とは挙動を独立して調整できるよう、
    /// プレハブもスクリプトも専用に分けている。
    /// 生成した時点で最大の長さまで引き切った状態で表示し、伸びる過程は見せない。
    /// 保持時間・収縮速度・太さ倍率はプレハブのインスペクタで調整する。
    /// </summary>
    public class CounterTracerView : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;

        [SerializeField, Tooltip("線を引いてから収縮を始めるまでの保持時間（秒）")]
        private float _holdDuration = 0.05f;

        [SerializeField, Tooltip("発射地点側からラインを収縮させて消す速度（m/秒）。0以下なら収縮せず保持後に消える")]
        private float _retractSpeed = 200f;

        [SerializeField, Tooltip("レイの太さ倍率（呼び出し側が渡す当たり判定サイズに掛ける）")]
        private float _widthMultiplier = 1f;

        /// <summary>
        /// レイを再生する。マテリアルはプレハブのLineRendererに設定したものを使う。
        /// </summary>
        /// <param name="startPos">発射地点</param>
        /// <param name="endPos">着弾地点</param>
        /// <param name="width">基準の太さ（弾の当たり判定サイズ）</param>
        public async UniTask Play(Vector3 startPos, Vector3 endPos, float width)
        {
            var lineWidth = width * _widthMultiplier;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;

            // 発射地点から着弾地点までを一度に引く（伸びる演出は挟まない）
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            // プレハブではLineRendererを無効にしてあり、座標を入れ切ってから表示する。
            // これで生成直後の1フレームにプレハブ既定の短い線が描かれることがない
            _lineRenderer.enabled = true;

            if (_holdDuration > 0f)
            {
                await UniTask.WaitForSeconds(_holdDuration);
            }

            // 収縮速度が未設定なら保持後にそのまま消す
            if (_retractSpeed <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 発射地点側の端点を一定速度で着弾地点へ寄せ、発射地点側から消していく
            var current = startPos;
            while (current != endPos)
            {
                current = Vector3.MoveTowards(current, endPos, _retractSpeed * Time.deltaTime);
                _lineRenderer.SetPosition(0, current);
                await UniTask.Yield();
            }

            Destroy(gameObject);
        }
    }
}
