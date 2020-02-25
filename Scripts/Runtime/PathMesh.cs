using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(BezierPathComponent.DefaultExecutionOrder + 1)]
    [RequireComponent(typeof(BezierPathComponent))]
    [DisallowMultipleComponent]
    public class PathMesh : MonoBehaviour
    {
        public static string DefaultSubmeshName = "Base";

        [SerializeField]
        private BezierPathComponent m_BezierPathComponent;
        [SerializeField]
        private MeshRenderer m_MeshRenderer;
        [SerializeField]
        private MeshFilter m_MeshFilter;
        public FittingType FittingType = FittingType.Length;
        public int Count = 1;
        [Range(0f, 1f)]
        public float MeshFitting = 0.5f;
        [SerializeField]
        private IntermediateMesh m_PathMeshData = new IntermediateMesh();
        [SerializeField]
        private CapMesh m_StartMeshData = new CapMesh();
        [SerializeField]
        private CapMesh m_EndMeshData = new CapMesh();
        [SerializeField]
        private List<string> m_SubmeshNames = new List<string>() { DefaultSubmeshName };

        public IntermediateMesh PathMeshData { get => m_PathMeshData; }
        public CapMesh StartMeshData { get => m_StartMeshData; }
        public CapMesh EndMeshData { get => m_EndMeshData; }
        public List<string> SubmeshNames { get => m_SubmeshNames; }

        private List<Vector3> m_GeneratedVertices = new List<Vector3>();
        private List<Vector3> m_GeneratedNormals = new List<Vector3>();
        private List<List<Vector2>> m_GeneratedUVs = new List<List<Vector2>>();
        private List<List<int>> m_GeneratedTriangles = new List<List<int>>();
        [SerializeField, HideInInspector]
        private Mesh m_GeneratedMesh;

        [System.Serializable]
        public class CapMesh
        {
            [SerializeField]
            private bool m_IsActive = false;
            [SerializeField]
            private IntermediateMesh m_IntermediateMesh = new IntermediateMesh();
            [SerializeField, Range(0, 1)]
            private float m_EnterPercent = 0f;

            public bool IsActiveAndValid { get => m_IsActive && m_IntermediateMesh.Mesh != null; }
            public bool IsActive { get => m_IsActive; set => m_IsActive = value; }
            public IntermediateMesh IntermediateMesh { get => m_IntermediateMesh; set => m_IntermediateMesh = value; }
            public float EnterPercent { get => m_EnterPercent; set => m_EnterPercent = value; }
        }

        private void OnEnable()
        {
            m_BezierPathComponent = GetComponent<BezierPathComponent>();

            m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshRenderer == null)
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();

            m_MeshFilter = GetComponent<MeshFilter>();
            if (m_MeshFilter == null)
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();

            m_MeshRenderer.enabled = true;
            if (m_GeneratedMesh == null || m_BezierPathComponent.PathChanged)
                GenerateMesh();
        }

        private void OnDisable()
        {
            if (m_MeshRenderer != null)
                m_MeshRenderer.enabled = false;
        }

        public void GenerateMesh()
        {
            m_MeshFilter.sharedMesh = GetMeshForPath();
        }

        public Mesh GetMeshForPath()
        {
            if (m_PathMeshData.Mesh == null)
                return null;

            m_PathMeshData.RebuildDataIfDirty();
            m_StartMeshData.IntermediateMesh.RebuildDataIfDirty();
            m_EndMeshData.IntermediateMesh.RebuildDataIfDirty();

            float startDistance = 0f;
            float endDistance = m_BezierPathComponent.GetLength();
            float startScale = m_BezierPathComponent.GetScaleAtIndex(0).z;
            float endScale = m_BezierPathComponent.GetScaleAtIndex(m_BezierPathComponent.PointCount - 1).z;
            if (m_StartMeshData.IsActiveAndValid)
                startDistance = m_StartMeshData.EnterPercent * m_StartMeshData.IntermediateMesh.Length * startScale;
            if (m_EndMeshData.IsActiveAndValid)
                endDistance -= m_EndMeshData.EnterPercent * m_EndMeshData.IntermediateMesh.Length * endScale;

            int count = FittingType == FittingType.Length ? PathMeshUtility.GetBestFit(m_PathMeshData.Length, (endDistance - startDistance), MeshFitting) : this.Count;
            count = Mathf.Clamp(count, 1, 100);

            if (m_GeneratedMesh == null)
            {
                m_GeneratedMesh = new Mesh();
                m_GeneratedMesh.name = "(Generated)";
                m_GeneratedMesh.MarkDynamic();
            }
            m_GeneratedMesh.Clear();

            m_GeneratedVertices.Clear();
            m_GeneratedNormals.Clear();
            m_GeneratedTriangles.Clear();
            m_GeneratedUVs.Clear();

            m_GeneratedVertices.Capacity = m_PathMeshData.Vertices.Count * count + m_StartMeshData.IntermediateMesh.Vertices.Count + m_EndMeshData.IntermediateMesh.Vertices.Count;
            m_GeneratedNormals.Capacity = m_PathMeshData.Normals.Count * count + m_StartMeshData.IntermediateMesh.Normals.Count + m_EndMeshData.IntermediateMesh.Normals.Count;

            m_GeneratedUVs.Capacity = 8;
            for (int i = 0; i < m_GeneratedUVs.Capacity; i++)
            {
                m_GeneratedUVs.Add(new List<Vector2>(
                    (i < m_PathMeshData.UVs.Count ? m_PathMeshData.UVs[i].Count * count : 0)
                    + (i < m_StartMeshData.IntermediateMesh.UVs.Count ? m_StartMeshData.IntermediateMesh.UVs[i].Count : 0)
                    + (i < m_EndMeshData.IntermediateMesh.UVs.Count ? m_EndMeshData.IntermediateMesh.UVs[i].Count : 0)
                    ));
            }

            for (int i = 0; i < m_SubmeshNames.Count; i++)
            {
                int triCapacity = (i < m_PathMeshData.Triangles.Count ? m_PathMeshData.Triangles[i].Count : 0) * count
                    + (i < m_StartMeshData.IntermediateMesh.Triangles.Count ? m_StartMeshData.IntermediateMesh.Triangles[i].Count : 0)
                    + (i < m_EndMeshData.IntermediateMesh.Triangles.Count ? m_EndMeshData.IntermediateMesh.Triangles[i].Count : 0);
                m_GeneratedTriangles.Add(new List<int>(triCapacity));
            }

            if (m_StartMeshData.IsActiveAndValid)
                Add(m_StartMeshData.IntermediateMesh, startDistance - m_StartMeshData.IntermediateMesh.Length * startScale, startDistance, 1);
            if (m_EndMeshData.IsActiveAndValid)
                Add(m_EndMeshData.IntermediateMesh, endDistance, endDistance + m_EndMeshData.IntermediateMesh.Length * endScale, 1);

            Add(m_PathMeshData, startDistance, endDistance, count);

            // we remove empty submeshes
            for (int i = m_GeneratedTriangles.Count - 1; i >= 0; i--)
                if (m_GeneratedTriangles[i].Count == 0)
                    m_GeneratedTriangles.RemoveAt(i);

            m_GeneratedMesh.subMeshCount = m_GeneratedTriangles.Count;
            m_GeneratedMesh.SetVertices(m_GeneratedVertices);
            m_GeneratedMesh.SetNormals(m_GeneratedNormals);
            for (int i = 0; i < m_GeneratedTriangles.Count; i++)
                m_GeneratedMesh.SetTriangles(m_GeneratedTriangles[i], i);
            for (int i = 0; i < m_GeneratedUVs.Count; i++)
                m_GeneratedMesh.SetUVs(i, m_GeneratedUVs[i]);

            return m_GeneratedMesh;
        }

        private void Add(IntermediateMesh in_MeshData, float in_StartDistance, float in_EndDistance, int in_Count)
        {
            float adjustedLength = (in_EndDistance - in_StartDistance) / in_Count;
            float lengthMultiplier = adjustedLength / in_MeshData.Length;

            int vertCountOffset = m_GeneratedVertices.Count;
            float pathLength = m_BezierPathComponent.GetLength();

            for (int c = 0; c < in_Count; c++)
            {
                for (int i = 0; i < in_MeshData.Vertices.Count; i++)
                {
                    Vector3 vertex = in_MeshData.Vertices[i];
                    float distance = c * adjustedLength + vertex.z * lengthMultiplier + in_StartDistance;
                    float time = m_BezierPathComponent.DistanceToTime(distance);
                    Vector3 positionOffset = m_BezierPathComponent.GetPositionAtTime(time, Space.Self);
                    Quaternion rotation = m_BezierPathComponent.GetRotationAtTime(time, Space.Self);
                    Vector2 scale = m_BezierPathComponent.GetScaleAtTime(time);
                    if (distance < 0f)
                        positionOffset += m_BezierPathComponent.GetTangentAtTime(time, Space.Self).normalized * distance;
                    else if (distance > pathLength)
                        positionOffset += m_BezierPathComponent.GetTangentAtTime(time, Space.Self).normalized * (distance - pathLength);
                    m_GeneratedVertices.Add(positionOffset + rotation * Vector3.Scale(vertex, scale));
                    m_GeneratedNormals.Add(rotation * in_MeshData.Normals[i]);
                }
            }

            for (int i = 0; i < in_MeshData.UVs.Count; i++)
            {
                for (int c = 0; c < in_Count; c++)
                {
                    m_GeneratedUVs[i].AddRange(in_MeshData.UVs[i]);
                }
            }

            for (int c = 0; c < in_Count; c++)
            {
                for (int ti = 0; ti < in_MeshData.Triangles.Count; ti++)
                {
                    var triangles = in_MeshData.Triangles[ti];
                    int triCount = triangles.Count;
                    for (int i = 0; i < triCount; i++)
                    {
                        m_GeneratedTriangles[ti].Add(vertCountOffset + triangles[i] + c * in_MeshData.Vertices.Count);
                    }
                }
            }
        }

        private void Update()
        {
            if (m_BezierPathComponent != null && m_BezierPathComponent.PathChanged)
            {
                GenerateMesh();
            }
        }

#if UNITY_EDITOR
        //private void OnDestroy()
        //{
        //    if (m_MeshRenderer != null)
        //        DestroySafely(m_MeshRenderer);
        //    if (m_MeshFilter != null)
        //        DestroySafely(m_MeshFilter);
        //}

        //private void DestroySafely(UnityEngine.Object in_Object)
        //{
        //    if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
        //        DestroyImmediate(in_Object);
        //}
#endif
    }
}