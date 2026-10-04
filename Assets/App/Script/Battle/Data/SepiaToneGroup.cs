using System;

namespace App.Battle.Data
{
    /// <summary>
    /// セピア調をかける対象のまとまり。プリセットでは複数を組み合わせて指定する。
    /// 並び（ビット位置）はシェーダへ渡す強さベクトルの成分順（x:背景 y:敵 z:プレイヤー w:UI）と一致させている。
    /// </summary>
    [Flags]
    public enum SepiaToneGroup
    {
        None = 0,
        Background = 1 << 0,
        Enemy = 1 << 1,
        Player = 1 << 2,
        UI = 1 << 3,
    }

    /// <summary>
    /// セピア調のグループと、Renderer の renderingLayerMask のビットとの対応。
    /// シェーダは renderingLayerMask を見て、その Renderer がどのグループに属するかを判断する。
    /// </summary>
    public static class SepiaToneRenderingLayer
    {
        /// <summary>扱うグループの数。シェーダへ渡すベクトルの成分数と同じ</summary>
        public const int GroupCount = 4;

        /// <summary>
        /// 背景グループに使う renderingLayerMask のビット位置。以降のグループは1つずつずらして使う。
        /// URP のライトレイヤー・デカールレイヤー（下位8ビット）と重ならない位置を選んでいる
        /// </summary>
        public const int FirstLayerIndex = 8;

        /// <summary>グループ（単独）の成分番号。0:背景 1:敵 2:プレイヤー 3:UI。単独でなければ -1</summary>
        public static int ComponentIndex(SepiaToneGroup group)
        {
            return group switch
            {
                SepiaToneGroup.Background => 0,
                SepiaToneGroup.Enemy => 1,
                SepiaToneGroup.Player => 2,
                SepiaToneGroup.UI => 3,
                _ => -1,
            };
        }

        /// <summary>成分番号に対応する renderingLayerMask のビット値</summary>
        public static uint LayerBit(int componentIndex)
        {
            return 1u << (FirstLayerIndex + componentIndex);
        }

        /// <summary>セピア用に使うビットをすべて立てたマスク。グループを付け替える前に既存のビットを消すのに使う</summary>
        public static uint AllLayerBits
        {
            get
            {
                var mask = 0u;
                for (var i = 0; i < GroupCount; i++)
                {
                    mask |= LayerBit(i);
                }

                return mask;
            }
        }

        /// <summary>グループ（組み合わせ可）に対応する renderingLayerMask のビットを立てたマスク</summary>
        public static uint LayerMask(SepiaToneGroup groups)
        {
            var mask = 0u;
            for (var i = 0; i < GroupCount; i++)
            {
                if (((int)groups & (1 << i)) != 0)
                {
                    mask |= LayerBit(i);
                }
            }

            return mask;
        }
    }
}
