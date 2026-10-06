using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// テキストを時間で横に流すための計算（MonoBehaviour から切り離した部分）。
    /// 同じ内容を2周ぶん並べたテキストを1周の幅だけずらし続け、1周したら先頭に戻して少し止める
    /// </summary>
    public class TagTickerScroller
    {
        private readonly float _speed;
        private readonly float _loopPause;

        private float _cycleWidth;
        private float _pauseRemaining;

        /// <summary>現在のずらし量（0 以上 1周の幅未満）</summary>
        public float Offset { get; private set; }

        /// <param name="speed">流れる速さ[幅/秒]</param>
        /// <param name="loopPause">先頭で止まる時間[秒]</param>
        public TagTickerScroller(float speed, float loopPause)
        {
            _speed = Mathf.Max(0f, speed);
            _loopPause = Mathf.Max(0f, loopPause);
        }

        /// <summary>1周の幅を設定し、先頭から止まった状態でやり直す</summary>
        public void Reset(float cycleWidth)
        {
            _cycleWidth = Mathf.Max(0f, cycleWidth);
            Offset = 0f;
            _pauseRemaining = _loopPause;
        }

        public void Advance(float deltaTime)
        {
            if (_cycleWidth <= 0f || deltaTime <= 0f)
            {
                return;
            }

            if (_pauseRemaining > 0f)
            {
                _pauseRemaining -= deltaTime;
                if (_pauseRemaining > 0f)
                {
                    return;
                }

                // 止まっている時間を使い切った残りぶんだけ進める
                deltaTime = -_pauseRemaining;
                _pauseRemaining = 0f;
            }

            Offset += _speed * deltaTime;
            if (Offset >= _cycleWidth)
            {
                // 2周目の先頭が1周目の先頭と同じ位置に来たので、見た目を変えずに先頭へ戻して止める
                Offset = 0f;
                _pauseRemaining = _loopPause;
            }
        }

        /// <summary>
        /// 表示範囲 [min, max] の端に近い文字ほど薄くする係数。範囲外は 0、端から fadeWidth 以上内側は 1
        /// </summary>
        public static float EdgeAlpha(float x, float min, float max, float fadeWidth)
        {
            if (x < min || x > max)
            {
                return 0f;
            }

            if (fadeWidth <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(Mathf.Min(x - min, max - x) / fadeWidth);
        }
    }
}
