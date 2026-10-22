using App.Battle.Data;

namespace App.Battle.Interface
{
    /// <summary>
    /// 床崩壊の見た目。崩壊対象の床を演出用のプレート床へ差し替え、進行度に応じてシェーダで動かす。
    /// コライダーや実際の床の形には触れない
    /// </summary>
    public interface IFloorCollapseView
    {
        /// <summary>崩壊対象の床を隠し、<paramref name="wave"/> の格子でプレート床を敷く。プレートごとに波が届く距離も <paramref name="wave"/> から取る</summary>
        void Begin(FloorCollapseWave wave);

        /// <summary>全体の進行度（0〜1）を反映する。波の前線の位置は Begin で受け取った波から求める</summary>
        void SetProgress(float progress);

        /// <summary>プレート床を片付け、隠していた床を元に戻す</summary>
        void Restore();
    }
}
