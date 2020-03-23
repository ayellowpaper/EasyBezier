using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace EasyBezier
{
    [System.Serializable]
    public class IntermediateMesh
    {
        [SerializeField]
        private Axis m_ForwardAxis = Axis.Z;
        [SerializeField]
        private Mesh m_Mesh;
        [SerializeField]
        private bool m_FlipMesh;
        [SerializeField]
        private int[] m_RemappedSubmeshIndices = new int[] { 0 };

        public Axis ForwardAxis { get => m_ForwardAxis; set { m_ForwardAxis = value; IsIntermediateMeshDataDirty = true; } }
        public Mesh Mesh {
            get => m_Mesh;
            set {
                m_Mesh = value;
                IsIntermediateMeshDataDirty = true;
                CheckSubmeshIndices();
            }
        }
        public bool FlipMesh { get => m_FlipMesh; set { m_FlipMesh = value; IsIntermediateMeshDataDirty = true; } }

        public int[] RemappedSubmeshIndices { get => m_RemappedSubmeshIndices; }
        public float Length { get; private set; }
        public bool IsIntermediateMeshDataDirty { get; private set; } = true;
        public IReadOnlyList<Vector3> Vertices { get => m_Vertices; }
        public IReadOnlyList<Vector3> Normals { get => m_Normals; }
        public IReadOnlyList<IReadOnlyList<Vector2>> UVs { get => m_UVs; }
        public IReadOnlyList<IReadOnlyList<int>> Triangles { get => m_Triangles; }

        private List<Vector3> m_Vertices = new List<Vector3>();
        private List<Vector3> m_Normals = new List<Vector3>();
        private List<List<Vector2>> m_UVs = new List<List<Vector2>>(8);
        private List<List<int>> m_Triangles = new List<List<int>>();

        internal void SetDirty()
        {
            IsIntermediateMeshDataDirty = true;
        }

        void InvalidateData()
        {
            m_Vertices.Clear();
            m_Normals.Clear();
            m_UVs.Clear();
            m_Triangles.Clear();
        }

        public void CheckSubmeshIndices()
        {
            if (m_Mesh == null)
                m_RemappedSubmeshIndices = new int[] { 0 };
            else
            {
                if (m_Mesh.subMeshCount != m_RemappedSubmeshIndices.Length)
                {
                    var newIndices = new int[m_Mesh.subMeshCount];
                    int max = Mathf.Min(m_Mesh.subMeshCount, m_RemappedSubmeshIndices.Length);
                    for (int i = 0; i < max; i++)
                        newIndices[i] = m_RemappedSubmeshIndices[i];
                    for (int i = max; i < newIndices.Length; i++)
                        newIndices[i] = 0;
                    m_RemappedSubmeshIndices = newIndices;
                }
            }
        }

        private int GetRemappedIndex(int in_Index)
        {
            if (in_Index >= 0 && in_Index < m_RemappedSubmeshIndices.Length)
                return m_RemappedSubmeshIndices[in_Index];

            return in_Index;
        }

        public void RebuildDataIfDirty()
        {
            // specifically check for uvs and triangles, because unity can't restore those fields after recompile
            if ((IsIntermediateMeshDataDirty || m_UVs.Count == 0 || m_Triangles.Count == 0))
            {
                if (m_Mesh != null)
                {
                    m_Mesh.GetNormals(m_Normals);

                    int submeshCount = m_RemappedSubmeshIndices.Max() + 1;
                    m_Triangles = new List<List<int>>(submeshCount);
                    for (int i = 0; i < m_Triangles.Capacity; i++)
                        m_Triangles.Add(new List<int>());
                    for (int i = 0; i < m_Mesh.subMeshCount; i++)
                    {
                        List<int> triangles = new List<int>();
                        m_Mesh.GetTriangles(triangles, i);
                        m_Triangles[GetRemappedIndex(i)].AddRange(triangles);
                    }

                    m_UVs.Clear();
                    m_UVs.Capacity = 8;
                    for (int i = 0; i < m_UVs.Capacity; i++)
                    {
                        List<Vector2> uvs = new List<Vector2>();
                        m_Mesh.GetUVs(i, uvs);
                        m_UVs.Add(uvs);
                    }

                    Bounds bounds = m_Mesh.bounds;
                    Length = Utility.GetComponentByAxis(bounds.size, m_ForwardAxis);
                    m_Mesh.GetVertices(m_Vertices);
                    float offset = Utility.GetComponentByAxis(bounds.center, m_ForwardAxis);
                    offset = Length / 2f - offset;
                    for (int i = 0; i < m_Vertices.Count; i++)
                    {
                        Vector3 vertex = m_Vertices[i];
                        vertex = Utility.SwitchComponentsByAxis(vertex, m_ForwardAxis);
                        vertex.z += offset;
                        if (m_FlipMesh)
                            vertex.z = -vertex.z + Length;
                        m_Vertices[i] = vertex;
                    }
                    for (int i = 0; i < m_Normals.Count; i++)
                    {
                        Vector3 normal = Utility.SwitchComponentsByAxis(m_Normals[i], m_ForwardAxis);
                        if (m_FlipMesh)
                            normal.z *= -1;
                        m_Normals[i] = normal;
                    }

                    if (m_FlipMesh)
                    {
                        foreach (List<int> triangles in m_Triangles)
                        {
                            int triCount = triangles.Count / 3;
                            for (int i = 0; i < triCount; i++)
                            {
                                int ii = i * 3;
                                int ii0 = triangles[ii];
                                int ii1 = triangles[ii + 1];
                                triangles[ii] = ii1;
                                triangles[ii + 1] = ii0;
                            }
                        }
                    }
                }
                else
                {
                    InvalidateData();
                }
                IsIntermediateMeshDataDirty = false;
            }
        }
    }
}