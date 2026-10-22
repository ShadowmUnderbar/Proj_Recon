using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.Battle.Views.FloorCollapse
{
    /// <summary>
    /// 床崩壊用のプレート床のメッシュを組む。プレートごとに頂点を分けた（隣と共有しない）厚みのある板を並べ、1枚のメッシュにする。
    /// 各頂点にプレートの中心と乱数を持たせ、沈下・傾斜・スライドはシェーダがプレート単位の剛体変換として行う。
    /// 元の床のUVや法線は使わない（見た目は真上からの投影で写す）
    /// </summary>
    public static class FloorCollapsePlateMeshBuilder
    {
        // 上面4頂点＋側面4面×4頂点
        private const int VerticesPerPlate = 4 + 4 * 4;
        // 上面2三角形＋側面4面×2三角形
        private const int IndicesPerPlate = (2 + 4 * 2) * 3;

        // 乱数の種。プレートの並びが同じなら、毎回同じ崩れ方になる
        private const uint HashSeedA = 0x9E3779B9u;
        private const uint HashSeedB = 0x85EBCA6Bu;

        /// <summary>
        /// 原点を中心に、一辺 <paramref name="fieldSize"/> の範囲へ一辺 <paramref name="plateSize"/> のプレートを敷き詰める。
        /// 上面は y=0、厚みの分だけ下へ側面を張る
        /// </summary>
        /// <returns>敷いたプレートの枚数</returns>
        public static int Build(Mesh mesh, float plateSize, float fieldSize, float thickness)
        {
            var platesPerSide = Mathf.Max(1, Mathf.CeilToInt(fieldSize / plateSize));
            var plateCount = platesPerSide * platesPerSide;
            var half = platesPerSide * plateSize * 0.5f;
            var halfPlate = plateSize * 0.5f;

            var vertices = new List<Vector3>(plateCount * VerticesPerPlate);
            var normals = new List<Vector3>(plateCount * VerticesPerPlate);
            var plateData = new List<Vector4>(plateCount * VerticesPerPlate);
            var indices = new List<int>(plateCount * IndicesPerPlate);

            for (var z = 0; z < platesPerSide; z++)
            {
                for (var x = 0; x < platesPerSide; x++)
                {
                    var center = new Vector3(x * plateSize - half + halfPlate, 0f, z * plateSize - half + halfPlate);
                    var data = new Vector4(center.x, center.z, Hash01(x, z, HashSeedA), Hash01(x, z, HashSeedB));

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

        /// <summary>プレートの並びから0〜1の乱数を作る</summary>
        private static float Hash01(int x, int z, uint seed)
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
