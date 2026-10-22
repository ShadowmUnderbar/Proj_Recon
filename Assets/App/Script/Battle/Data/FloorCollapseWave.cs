using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 床崩壊の波の計算。波がどこまで進んだか（前線）と、各プレートに波が届く距離を、ここだけで決める。
    /// シェーダ（App/FloorCollapse）は距離の式を持たず、ここで求めた値（プレートごとの開始距離・前線の位置）を受け取って使う。
    /// 敵・弾・粒子を消す判定（<see cref="IsCollapsedAt"/>）も同じ値を使うので、床が落ちる位置と物が消える位置がずれない。
    /// 1回の再生（中心と設定）ごとに作る
    /// </summary>
    public sealed class FloorCollapseWave
    {
        // プレートの乱数の種。プレートの並びが同じなら、毎回同じ崩れ方になる
        public const uint JitterSeed = 0x9E3779B9u;
        public const uint VariationSeed = 0x85EBCA6Bu;

        private readonly Vector3 _origin;
        private readonly FloorCollapseSettings _settings;
        private readonly Vector2 _direction;
        private readonly float _maxTravel;
        private readonly float _halfField;

        /// <summary>プレートの格子の中心（ワールドのXZ。Yは崩壊の中心の高さ）。境目が毎回同じワールド位置に来るよう格子へ揃える</summary>
        public Vector3 GridCenter { get; }

        /// <summary>一辺あたりのプレート枚数</summary>
        public int PlatesPerSide { get; }

        public FloorCollapseSettings Settings => _settings;

        /// <summary>崩壊の中心（ワールド）</summary>
        public Vector3 Origin => _origin;

        public FloorCollapseWave(Vector3 origin, FloorCollapseSettings settings)
        {
            _origin = origin;
            _settings = settings;
            _direction = settings.NormalizedDirection;
            _maxTravel = settings.MaxTravelDistance;

            PlatesPerSide = GetPlatesPerSide(settings);
            _halfField = PlatesPerSide * settings.PlateSize * 0.5f;
            var plateSize = settings.PlateSize;
            GridCenter = new Vector3(
                Mathf.Round(origin.x / plateSize) * plateSize,
                origin.y,
                Mathf.Round(origin.z / plateSize) * plateSize);
        }

        /// <summary>範囲（FieldSize）を覆うのに要る、一辺あたりのプレート枚数</summary>
        public static int GetPlatesPerSide(FloorCollapseSettings settings)
        {
            return Mathf.Max(1, Mathf.CeilToInt(settings.FieldSize / settings.PlateSize));
        }

        /// <summary>格子の中心から見た、プレート (x, z) の中心（ローカルのXZ）</summary>
        public static Vector2 PlateLocalCenter(int x, int z, int platesPerSide, float plateSize)
        {
            var half = platesPerSide * plateSize * 0.5f;
            var halfPlate = plateSize * 0.5f;
            return new Vector2(x * plateSize - half + halfPlate, z * plateSize - half + halfPlate);
        }

        /// <summary>
        /// 全体の進行度（0〜1）に対する、波の前線の位置[m]。
        /// いちばん遠いプレートが動き切ったときに、ちょうど暗転の開始（BlackoutStart）になるよう進める
        /// </summary>
        public float Front(float progress)
        {
            var travel = _maxTravel + _settings.Stagger + _settings.Jitter;
            return progress / Mathf.Max(_settings.BlackoutStart, Mathf.Epsilon) * travel;
        }

        /// <summary>
        /// プレート (x, z) に波が届く距離[m]。中心からの距離（一方向なら向きに沿った距離）に、プレートごとの開始のずれを足す
        /// </summary>
        public float PlateStartDistance(int x, int z)
        {
            var center = PlateLocalCenter(x, z, PlatesPerSide, _settings.PlateSize);
            var world = new Vector3(GridCenter.x + center.x, 0f, GridCenter.z + center.y);
            return Distance(world) + PlateRandom(x, z, JitterSeed) * _settings.Jitter;
        }

        /// <summary>
        /// <paramref name="worldPosition"/> の真下の床が落ち始めているか（亀裂だけの区間を過ぎたか）。
        /// プレートの範囲の外は、開始のずれを平均（ずれ幅の半分）で見る
        /// </summary>
        public bool IsCollapsedAt(Vector3 worldPosition, float progress)
        {
            var startDistance = TryGetPlate(worldPosition, out var x, out var z)
                ? PlateStartDistance(x, z)
                : Distance(worldPosition) + _settings.Jitter * 0.5f;
            var plateProgress = (Front(progress) - startDistance) / Mathf.Max(_settings.Stagger, Mathf.Epsilon);
            return plateProgress >= _settings.CrackPortion;
        }

        /// <summary>崩壊の中心からの距離[m]。一方向のときは、範囲の向きの逆側の端を0とする向きに沿った距離</summary>
        public float Distance(Vector3 worldPosition)
        {
            var fromOrigin = new Vector2(worldPosition.x - _origin.x, worldPosition.z - _origin.z);
            if (_settings.Mode == FloorCollapseMode.Radial)
            {
                return fromOrigin.magnitude;
            }

            return Vector2.Dot(fromOrigin, _direction) + _maxTravel * 0.5f;
        }

        /// <summary>ワールド位置の真下にあるプレートの並び。範囲の外なら false</summary>
        private bool TryGetPlate(Vector3 worldPosition, out int x, out int z)
        {
            var plateSize = _settings.PlateSize;
            x = Mathf.FloorToInt((worldPosition.x - GridCenter.x + _halfField) / plateSize);
            z = Mathf.FloorToInt((worldPosition.z - GridCenter.z + _halfField) / plateSize);
            return x >= 0 && x < PlatesPerSide && z >= 0 && z < PlatesPerSide;
        }

        /// <summary>プレートの並びから0〜1の乱数を作る（メッシュ生成と判定で同じ値を使う）</summary>
        public static float PlateRandom(int x, int z, uint seed)
        {
            var h = (uint)x * 0x27D4EB2Du ^ (uint)z * 0x165667B1u ^ seed;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}
