using System.Collections.Generic;
using App.Battle.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.Battle.Views.FloorCollapse
{
    /// <summary>
    /// 床崩壊用のプレート床のメッシュを組む。プレートごとに頂点を分けた（隣と共有しない）厚みのある板を並べ、1枚のメッシュにする。
    /// 各頂点にプレートの中心と乱数を持たせ、沈下・傾斜・スライドはシェーダがプレート単位の剛体変換として行う。
    /// 波が届く距離は再生のたびに <see cref="WriteStartDistances"/> で UV1 へ書き込む（距離の式は <see cref="FloorCollapseWave"/> だけが持つ）。
    /// 元の床のUVや法線は使わない（見た目は真上からの投影で写す）
    /// </summary>
    public static class FloorCollapsePlateMeshBuilder
    {
        // 上面4頂点＋側面4面×4頂点
        private const int VerticesPerPlate = 4 + 4 * 4;
        // 上面2三角形＋側面4面×2三角形
        private const int IndicesPerPlate = (2 + 4 * 2) * 3;

        /// <summary>
        /// 原点を中心に、範囲（FieldSize）へ一辺 PlateSize のプレートを敷き詰める。
        /// 上面は y=0、厚み（PlateThickness）の分だけ下へ側面を張る
        /// </summary>
        /// <returns>敷いたプレートの枚数</returns>
        public static int Build(Mesh mesh, FloorCollapseSettings settings)
        {
            var plateSize = settings.PlateSize;
            var thickness = settings.PlateThickness;
            var platesPerSide = FloorCollapseWave.GetPlatesPerSide(settings);
            var plateCount = platesPerSide * platesPerSide;
            var halfPlate = plateSize * 0.5f;

            var vertices = new List<Vector3>(plateCount * VerticesPerPlate);
            var normals = new List<Vector3>(plateCount * VerticesPerPlate);
            var plateData = new List<Vector4>(plateCount * VerticesPerPlate);
            var indices = new List<int>(plateCount * IndicesPerPlate);

            for (var z = 0; z < platesPerSide; z++)
            {
                for (var x = 0; x < platesPerSide; x++)
                {
                    var localCenter = FloorCollapseWave.PlateLocalCenter(x, z, platesPerSide, plateSize);
                    var center = new Vector3(localCenter.x, 0f, localCenter.y);
                    var data = new Vector4(center.x, center.z,
                        FloorCollapseWave.PlateRandom(x, z, FloorCollapseWave.JitterSeed),
                        FloorCollapseWave.PlateRandom(x, z, FloorCollapseWave.VariationSeed));

                    var minX = center.x - halfPlate;
                    var maxX = center.x + halfPlate;
                    var minZ = center.z - halfPlate;
                    var maxZ = center.z + halfPlate;
                    var bottom = -thickness;

                    // 上面
                    AddQuad(vertices, normals, plateData, indices, data, Vector3.up,
                        new Vector3(minX, 0f, minZ), new Vector3(minX, 0f, maxZ),
                        new Vector3(maxX, 0f, maxZ), new Vector3(maxX, 0f, minZ));

                    // 側面（傾いたときに断面として見える）
                    AddQuad(vertices, normals, plateData, indices, data, Vector3.back,
                        new Vector3(minX, bottom, minZ), new Vector3(minX, 0f, minZ),
                        new Vector3(maxX, 0f, minZ), new Vector3(maxX, bottom, minZ));
                    AddQuad(vertices, normals, plateData, indices, data, Vector3.forward,
                        new Vector3(maxX, bottom, maxZ), new Vector3(maxX, 0f, maxZ),
                        new Vector3(minX, 0f, maxZ), new Vector3(minX, bottom, maxZ));
                    AddQuad(vertices, normals, plateData, indices, data, Vector3.left,
                        new Vector3(minX, bottom, maxZ), new Vector3(minX, 0f, maxZ),
                        new Vector3(minX, 0f, minZ), new Vector3(minX, bottom, minZ));
                    AddQuad(vertices, normals, plateData, indices, data, Vector3.right,
                        new Vector3(maxX, bottom, minZ), new Vector3(maxX, 0f, minZ),
                        new Vector3(maxX, 0f, maxZ), new Vector3(maxX, bottom, maxZ));
                }
            }

            mesh.Clear();
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, plateData);
            mesh.SetTriangles(indices, 0, false);
            return plateCount;
        }

        /// <summary>
        /// 各プレートに波が届く距離[m]を UV1.x へ書き込む。崩壊の中心が変わるので再生のたびに呼ぶ。
        /// プレートの並び（z ごとに x を並べ、1枚あたり <see cref="VerticesPerPlate"/> 頂点）は <see cref="Build"/> と同じ
        /// </summary>
        public static void WriteStartDistances(Mesh mesh, FloorCollapseWave wave, List<Vector2> buffer)
        {
            buffer.Clear();
            var platesPerSide = wave.PlatesPerSide;
            for (var z = 0; z < platesPerSide; z++)
            {
                for (var x = 0; x < platesPerSide; x++)
                {
                    var startDistance = new Vector2(wave.PlateStartDistance(x, z), 0f);
                    for (var i = 0; i < VerticesPerPlate; i++)
                    {
                        buffer.Add(startDistance);
                    }
                }
            }

            mesh.SetUVs(1, buffer);
        }

        /// <summary>時計回り（上から見て）に並べた4点で四角形を足す</summary>
        private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<Vector4> plateData, List<int> indices,
            Vector4 data, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            for (var i = 0; i < 4; i++)
            {
                normals.Add(normal);
                plateData.Add(data);
            }

            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }
    }
}
