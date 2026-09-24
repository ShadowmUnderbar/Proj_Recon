using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// LineRendererに直線を置く。カーブが有効なときは、途中に点を足して線を刻む。
    ///
    /// 曲げているのは頂点シェーダなので、頂点が無い場所は曲がらない。
    /// 2点だけの線は両端が沈んで間が弦のまま残り、放物面から浮いてしまう。
    /// 刻んだあとも直線であることに変わりはないため、カーブ無効時の見た目は変わらない。
    ///
    /// 曲率はシェーダへ配られているグローバル値をそのまま読む。
    /// 設定アセットの値を読むと、実機で曲率を振ったときに刻みが追随せず、
    /// この機能が防ごうとしている「線が浮く」現象がそのまま出る。
    /// カーブを使っていないシーンではグローバル値が0のままなので、2点の直線に落ちる。
    /// </summary>
    public static class CurvedWorldLine
    {
        /// <summary>
        /// 1本の線に置く点の上限。VRで毎フレーム更新するため、際限なく増やさない。
        ///
        /// この上限に当たると許容たわみを超える。当たるのは
        /// 「曲率を下限まで上げた状態」かつ「200m級の線」の組み合わせだけで、
        /// そのときの刻みは200/63=3.2m、たわみは0.47m（許容0.1m）になる。
        /// ただしその条件では線の遠端は数kmの深さへ沈んでいて画面に映らないため、
        /// 見える範囲でのずれは実質もっと小さい。
        /// </summary>
        private const int MaxPositionCount = 64;

        private static readonly int ParamsId = Shader.PropertyToID("_CurvedWorldParams");

        /// <summary>
        /// start から end へ線を引く。曲率に応じて必要なだけ点を刻む。
        /// </summary>
        public static void SetLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
        {
            if (lineRenderer == null)
            {
                return;
            }

            var positionCount = CalculatePositionCount(Vector3.Distance(start, end), MaxSegmentLength());

            // positionCountの代入は内部バッファの作り直しを伴うので、変わったときだけ触る
            if (lineRenderer.positionCount != positionCount)
            {
                lineRenderer.positionCount = positionCount;
            }

            WritePositions(lineRenderer, start, end, positionCount);
        }

        /// <summary>
        /// 点の数を変えずに座標だけ書き直す。
        /// 収縮アニメーションのように毎フレーム長さが変わる用途で使う。
        /// 長さが縮むたびに点の数を減らすと、そのたびに内部バッファが作り直される。
        /// </summary>
        public static void SetLinePositionsKeepingCount(LineRenderer lineRenderer, Vector3 start, Vector3 end)
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
            {
                return;
            }

            WritePositions(lineRenderer, start, end, lineRenderer.positionCount);
        }

        /// <summary>長さを maxSegmentLength 以下に刻むのに要る点の数</summary>
        public static int CalculatePositionCount(float length, float maxSegmentLength)
        {
            if (maxSegmentLength <= 0f || float.IsInfinity(maxSegmentLength) || float.IsNaN(maxSegmentLength))
            {
                return 2;
            }

            var segments = Mathf.CeilToInt(length / maxSegmentLength);
            return Mathf.Clamp(segments + 1, 2, MaxPositionCount);
        }

        /// <summary>
        /// 現在シェーダへ配られている曲率から求めた刻み間隔[m]。
        /// 放物面に弦を張ったときの中央のずれは 曲率 * 長さ^2 / 4 で、距離には依存しない。
        /// これを許容たわみ以下に収める長さを逆算している。
        /// </summary>
        public static float MaxSegmentLength()
        {
            var parameters = Shader.GetGlobalVector(ParamsId);
            var strength = parameters.x;
            var maxSag = parameters.y;

            if (strength <= Mathf.Epsilon || maxSag <= Mathf.Epsilon)
            {
                return float.PositiveInfinity;
            }

            return Mathf.Sqrt(4f * maxSag / strength);
        }

        private static void WritePositions(LineRenderer lineRenderer, Vector3 start, Vector3 end, int positionCount)
        {
            var lastIndex = positionCount - 1;
            for (var i = 0; i <= lastIndex; i++)
            {
                lineRenderer.SetPosition(i, Vector3.Lerp(start, end, (float)i / lastIndex));
            }
        }
    }
}
