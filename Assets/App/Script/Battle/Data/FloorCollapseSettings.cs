using System;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>床崩壊の進み方。中心から同心円状に広げるか、一方向へ流すか</summary>
    public enum FloorCollapseMode
    {
        Radial = 0,
        Directional = 1,
    }

    /// <summary>
    /// 床崩壊の1回ぶんの見た目と動き。開始時に渡し、再生中は進行度だけを動かす。
    /// プレートの大きさ（分割密度）と床の広さはメッシュの作り直しに関わるので、変えたときだけ作り直す
    /// </summary>
    [Serializable]
    public struct FloorCollapseSettings
    {
        [Header("全体")]
        [Min(0.1f), Tooltip("崩壊の開始から暗転までの秒数")]
        public float Duration;

        [Tooltip("崩壊の進み方")]
        public FloorCollapseMode Mode;

        [Tooltip("一方向に流すときの向き（XZ）。同心円のときは使わない")]
        public Vector2 Direction;

        [Header("分割")]
        [Min(0.5f), Tooltip("プレート1枚の一辺[m]。小さいほど細かく割れる（分割密度）")]
        public float PlateSize;

        [Min(1f), Tooltip("プレートを敷く範囲の一辺[m]。崩壊の中心を真ん中に敷く")]
        public float FieldSize;

        [Min(0.01f), Tooltip("プレートの厚み[m]。傾いたときに断面として見える")]
        public float PlateThickness;

        [Header("動き")]
        [Min(0.1f), Tooltip("1枚のプレートが割れ始めてから消えるまでに、崩壊の波が進む距離[m]。大きいほど同時に動くプレートが増える")]
        public float Stagger;

        [Min(0f), Tooltip("プレートごとの開始のずれ[m]。0で波面が揃い、大きいほど不規則に崩れる")]
        public float Jitter;

        [Range(0f, 0.9f), Tooltip("割れ始めてから動き出すまでの割合（この間は境界に亀裂と発光が走るだけ）")]
        public float CrackPortion;

        [Min(0f), Tooltip("沈下量[m]")]
        public float SinkDepth;

        [Range(0f, 90f), Tooltip("傾斜の角度[度]。外側の縁が下がる向きに傾く")]
        public float TiltAngle;

        [Min(0f), Tooltip("外側へのスライド量[m]")]
        public float SlideDistance;

        [Range(0f, 0.5f), Tooltip("動き出したプレートを縮めて開ける隙間の割合")]
        public float GapRatio;

        [Header("データ片化・暗転")]
        [Range(0f, 1f), Tooltip("動きのどこからデータ片になって消え始めるか（0〜1）")]
        public float DissolveStart;

        [Min(1f), Tooltip("プレート1枚を一辺何個のデータ片に割るか")]
        public float FragmentsPerPlate;

        [Range(0f, 1f), Tooltip("全体の進行度のどこから暗転を始めるか")]
        public float BlackoutStart;

        [Header("発光")]
        [ColorUsage(false, true), Tooltip("境界の亀裂・データ片の切り口の発光色（HDR）")]
        public Color GlowColor;

        [Min(0.001f), Tooltip("境界の発光線の幅[m]")]
        public float LineWidth;

        public static FloorCollapseSettings Default => new()
        {
            Duration = 5f,
            Mode = FloorCollapseMode.Radial,
            Direction = Vector2.up,
            PlateSize = 2.5f,
            FieldSize = 56f,
            PlateThickness = 0.4f,
            Stagger = 10f,
            Jitter = 4f,
            CrackPortion = 0.35f,
            SinkDepth = 8f,
            TiltAngle = 40f,
            SlideDistance = 3f,
            GapRatio = 0.12f,
            DissolveStart = 0.55f,
            FragmentsPerPlate = 5f,
            BlackoutStart = 0.8f,
            GlowColor = new Color(0.4f, 1.8f, 2.4f, 1f),
            LineWidth = 0.06f,
        };

        /// <summary>
        /// 崩壊の中心からいちばん遠いプレートまでの、波が進む距離[m]。
        /// 一方向のときは、正方形の範囲を向きへ射影した幅（斜めの向きでは対角に近づく）
        /// </summary>
        public float MaxTravelDistance
        {
            get
            {
                var half = FieldSize * 0.5f;
                if (Mode == FloorCollapseMode.Radial)
                {
                    return half * Mathf.Sqrt(2f);
                }

                var direction = NormalizedDirection;
                return 2f * half * (Mathf.Abs(direction.x) + Mathf.Abs(direction.y));
            }
        }

        /// <summary>一方向に流すときの向き（正規化済み）。未設定なら +Z</summary>
        public Vector2 NormalizedDirection => Direction.sqrMagnitude > 0f ? Direction.normalized : Vector2.up;
    }
}
