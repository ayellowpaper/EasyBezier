using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static EasyBezier.BezierPoint;

namespace EasyBezier
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(DefaultExecutionOrder)]
    public class BezierPathComponent : MonoBehaviour, IPath
    {
        public const int DefaultExecutionOrder = 1000;

        [SerializeField]
        private bool m_IsLooping = false;
        [SerializeField, Range(-180, 180)]
        private float m_PathRoll = 0;
        [SerializeField]
        private Vector3 m_PathScale = Vector3.one;
        [SerializeField]
        private ScaleInputType m_ScaleInputType = ScaleInputType.Float;
        [SerializeField]
        private List<BezierPoint> m_Points = new List<BezierPoint>(new BezierPoint[] { new BezierPoint(Vector3.zero), new BezierPoint(Vector3.right) });

        public bool PathChanged { get; private set; }
        public delegate void OnPathChangedDelegate(BezierPathComponent in_Sender);
        public event OnPathChangedDelegate OnPathChanged;

        private bool m_IsLengthDirty = true;
        private float m_Length = -1f;
        private List<float> m_Distances = new List<float>();
        private float m_RollAdjustmentPerIndex;

        private bool m_AreCachedWorldPositionsDirty = true;
        private Vector3[] m_CachedWorldPositions;

        public const Space DefaultSpace = Space.World;

        private void Update()
        {
        }

        private void LateUpdate()
        {
            if (PathChanged)
                SetPathChanged(false);
        }

        private void OnDrawGizmos()
        {
            if (!isActiveAndEnabled)
                return;
#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject == this.gameObject)
                return;
#endif
            int pointsPerSegment = 10;
            int points = pointsPerSegment * PointCount + (IsLooping ? 1 : 0);
            float stepSize = 1f / points;

            Gizmos.color = EasyBezierSettings.Instance.Settings.BezierColor;
            Vector3 prevPoint = GetPositionAtTime(0f);
            for (int i = 1; i <= points; i++)
            {
                Vector3 nextPoint = GetPositionAtTime(i * stepSize);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        #region POINTS METHODS
        public Vector3 GetTangentAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 tangent = m_Points[in_Index].OutTangent.Position - m_Points[in_Index].Position;
            return in_Space == Space.World ? transform.TransformVector(tangent) : tangent;
        }

        public Quaternion GetRotationAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            return Quaternion.LookRotation(GetTangentAtIndex(in_Index, in_Space).normalized, GetUpVectorAtIndex(in_Index, in_Space));
        }

        public void SetSmartUpVectorAtIndex(int in_Index, Vector3 in_UpVector, Space in_Space = DefaultSpace)
        {
            BezierPoint point = m_Points[in_Index];
            point.SmartUpVector = in_Space == Space.World ? transform.InverseTransformDirection(in_UpVector) : in_UpVector;
        }

        public Vector3 GetSmartUpVectorAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 upVector = m_Points[in_Index].SmartUpVector;
            return in_Space == Space.World ? transform.TransformDirection(upVector) : upVector;
        }

        public Vector3 GetUpVectorAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 smartUpVector = GetSmartUpVectorAtIndex(in_Index, Space.Self);
            Vector3 tangent = GetTangentAtIndex(in_Index, Space.Self);
            Vector3 upVector = Quaternion.AngleAxis(GetRollAtIndex(in_Index), tangent.normalized) * smartUpVector;
            return in_Space == Space.World ? transform.TransformDirection(upVector) : upVector;
        }

        public Vector3 GetRightVectorAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 rightVector = Vector3.Cross(GetTangentAtIndex(in_Index, Space.Self).normalized, GetUpVectorAtIndex(in_Index, Space.Self));
            return in_Space == Space.World ? transform.TransformDirection(rightVector) : rightVector;
        }

        public Vector3 GetForwardVectorAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 forwardVector = GetTangentAtIndex(in_Index, Space.Self).normalized;
            return in_Space == Space.World ? transform.TransformDirection(forwardVector) : forwardVector;
        }

        public float GetRollAtIndex(int in_Index)
        {
            return m_PathRoll + m_RollAdjustmentPerIndex * in_Index + m_Points[in_Index].Roll;
        }

        public void AddPoint()
        {
            BezierPoint lastPoint = m_Points[m_Points.Count - 1];
            BezierPoint bp = new BezierPoint(lastPoint.Position + (lastPoint.OutTangent.Position - lastPoint.Position).normalized);
            m_Points.Add(bp);
            UpdateAutoAndLinear();
            SetPathChanged(true);
        }

        public void AddPoint(Vector3 in_Position, Space in_Space = DefaultSpace)
        {
            BezierPoint bp = new BezierPoint(in_Space == Space.World ? transform.InverseTransformPoint(in_Position) : in_Position);
            m_Points.Add(bp);
            UpdateAutoAndLinear();
            SetPathChanged(true);
        }

        public int InsertPointAtTime(float in_T)
        {
            Vector3 pos = GetPositionAtTime(in_T, Space.Self);
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);
            BezierPoint bp = new BezierPoint(pos);
            m_Points.Insert(secondIndex, bp);
            UpdateAutoAndLinear();
            SetPathChanged(true);
            return firstIndex;
        }

        public void RemovePoint()
        {
            if (m_Points.Count > 0)
            {
                m_Points.RemoveAt(m_Points.Count - 1);
                SetPathChanged(true);
            }
        }

        public void RemovePointAt(int in_Index)
        {
            if (in_Index >= 0 && in_Index < m_Points.Count)
            {
                m_Points.RemoveAt(in_Index);
                UpdateAutoAndLinear();
                SetPathChanged(true);
            }
        }

        /// <summary>
        /// Set the position of the point at the given index.
        /// </summary>
        public void SetPositionAtIndex(int in_Index, Vector3 in_Position, Space in_Space = DefaultSpace)
        {
            BezierPoint point = m_Points[in_Index];
            Vector3 newPosition = in_Space == Space.World ? transform.InverseTransformPoint(in_Position) : in_Position;
            Vector3 delta = newPosition - point.Position;
            point.Position = newPosition;
            point.InTangent.Position = point.InTangent.Position + delta;
            point.OutTangent.Position = point.OutTangent.Position + delta;
            UpdateAutoAndLinear();
            SetPathChanged(true);
        }

        /// <summary>
        /// Get the position of the point at the given index.
        /// </summary>
        public Vector3 GetPositionAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 point = m_Points[in_Index].Position;
            return in_Space == Space.World ? transform.TransformPoint(point) : point;
        }

        /// <summary>
        /// Set the incomming tangent position at the given index.
        /// </summary>
        public void SetInTangentPositionAtIndex(int in_Index, Vector3 in_Position, Space in_Space = DefaultSpace)
        {
            BezierPoint point = m_Points[in_Index];
            point.InTangent.Position = in_Space == Space.World ? transform.InverseTransformPoint(in_Position) : in_Position;
            point.InTangent.CurveType = CurveType.Free;
            if (point.ConnectionType != TangentConnectionType.Broken)
                UpdateTangentByConnectionType(point, true);
            SetPathChanged(true);
        }

        /// <summary>
        /// Get the incomming tangent position at the given index.
        /// </summary>
        public Vector3 GetInTangentPositionAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 point = m_Points[in_Index].InTangent.Position;
            return in_Space == Space.World ? transform.TransformPoint(point) : point;
        }

        /// <summary>
        /// Set the outgoing tangent position at the given index.
        /// </summary>
        public void SetOutTangentPositionAtIndex(int in_Index, Vector3 in_Position, Space in_Space = DefaultSpace)
        {
            BezierPoint point = m_Points[in_Index];
            point.OutTangent.Position = in_Space == Space.World ? transform.InverseTransformPoint(in_Position) : in_Position;
            point.OutTangent.CurveType = CurveType.Free;
            if (point.ConnectionType != TangentConnectionType.Broken)
                UpdateTangentByConnectionType(point, false);
            SetPathChanged(true);
        }

        /// <summary>
        /// Get the outgoing tangent position at the given index.
        /// </summary>
        public Vector3 GetOutTangentPositionAtIndex(int in_Index, Space in_Space = DefaultSpace)
        {
            Vector3 point = m_Points[in_Index].OutTangent.Position;
            return in_Space == Space.World ? transform.TransformPoint(point) : point;
        }

        public void SetInTangentCurveTypeAtIndex(int in_Index, CurveType in_CurveType)
        {
            BezierPoint point = m_Points[in_Index];
            point.InTangent.CurveType = in_CurveType;
            UpdateAutoAndLinear();
            SetPathChanged(true);
        }

        public CurveType GetInTangentCurveTypeAtIndex(int in_Index)
        {
            return m_Points[in_Index].InTangent.CurveType;
        }

        public void SetOutTangentCurveTypeAtIndex(int in_Index, CurveType in_CurveType)
        {
            BezierPoint point = m_Points[in_Index];
            point.OutTangent.CurveType = in_CurveType;
            UpdateAutoAndLinear();
            SetPathChanged(true);
        }

        public CurveType GetOutTangentCurveTypeAtIndex(int in_Index)
        {
            return m_Points[in_Index].OutTangent.CurveType;
        }

        public void SetTangentConnectionTypeAtIndex(int in_Index, TangentConnectionType in_ConnectionType, bool in_AdjustOutToInTangent = true)
        {
            BezierPoint bp = m_Points[in_Index];
            bp.ConnectionType = in_ConnectionType;
            if (bp.ConnectionType != TangentConnectionType.Broken && bp.InTangent.CurveType == CurveType.Free && bp.InTangent.CurveType == CurveType.Free)
            {
                UpdateTangentByConnectionType(bp, in_AdjustOutToInTangent);
                SetPathChanged(true);
            }
        }

        public TangentConnectionType GetTangentConnectionTypeAtIndex(int in_Index)
        {
            return m_Points[in_Index].ConnectionType;
        }

        public void SetAdjustmentRollAtIndex(int in_Index, float in_NewAngle)
        {
            BezierPoint bp = m_Points[in_Index];
            bp.Roll = in_NewAngle;
            SetPathMetadataChanged(true);
        }

        public float GetAdjustmentRollAtIndex(int in_Index)
        {
            return m_Points[in_Index].Roll;
        }

        public Vector3 GetScaleAtIndex(int in_Index)
        {
            return m_Points[in_Index].Scale;
        }

        public void SetScaleAtIndex(int in_Index, Vector3 in_Scale)
        {
            m_Points[in_Index].Scale = in_Scale;
            SetPathMetadataChanged(true);
        }
        #endregion

        #region Curve Getters
        public Vector3 GetPositionAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);

            BezierPoint p0 = m_Points[firstIndex];
            BezierPoint p1 = m_Points[secondIndex];

            Vector3 pos = Mathf.Pow((1 - in_T), 3) * p0.Position + 3 * Mathf.Pow((1 - in_T), 2) * in_T * p0.OutTangent.Position + 3 * (1 - in_T) * Mathf.Pow(in_T, 2) * p1.InTangent.Position + Mathf.Pow(in_T, 3) * p1.Position;
            return in_Space == Space.World ? transform.TransformPoint(pos) : pos;
        }

        public Vector3 GetPositionAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetPositionAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public Vector3 GetTangentAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);

            BezierPoint p0 = m_Points[firstIndex];
            BezierPoint p1 = m_Points[secondIndex];

            float oneMinusT = 1f - in_T;
            Vector3 tangent =
                3f * oneMinusT * oneMinusT * (p0.OutTangent.Position - p0.Position) +
                6f * oneMinusT * in_T * (p1.InTangent.Position - p0.OutTangent.Position) +
                3f * in_T * in_T * (p1.Position - p1.InTangent.Position);

            return in_Space == Space.World ? transform.TransformVector(tangent) : tangent;
        }

        public Vector3 GetTangentAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetTangentAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public Vector3 GetUpVectorAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            Vector3 pos = GetPositionAtTime(in_T, Space.Self);
            Vector3 tangent = GetTangentAtTime(in_T, Space.Self).normalized;
            float roll = GetRollAtTime(in_T);
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);
            Vector3 upVector = GetSmartUpVector(pos, GetPositionAtIndex(firstIndex, Space.Self), tangent, GetTangentAtIndex(firstIndex, Space.Self).normalized, GetSmartUpVectorAtIndex(firstIndex, Space.Self));
            upVector = Quaternion.AngleAxis(roll, tangent.normalized) * upVector;
            return in_Space == Space.World ? transform.TransformDirection(upVector) : upVector;
        }

        public Vector3 GetUpVectorAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetUpVectorAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public Vector3 GetRightVectorAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            Vector3 rightVector = Vector3.Cross(GetTangentAtTime(in_T, Space.Self).normalized, GetUpVectorAtTime(in_T, Space.Self));
            return in_Space == Space.World ? transform.TransformDirection(rightVector) : rightVector;
        }

        public Vector3 GetRightVectorAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetRightVectorAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public Vector3 GetForwardVectorAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            return GetTangentAtTime(in_T, in_Space).normalized;
        }

        public Vector3 GetForwardVectorAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetForwardVectorAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public Quaternion GetRotationAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            return Quaternion.LookRotation(GetTangentAtTime(in_T, in_Space), GetUpVectorAtTime(in_T, in_Space));
        }

        public Quaternion GetRotationAtDistance(float in_Distance, Space in_Space = DefaultSpace)
        {
            return GetRotationAtTime(DistanceToTime(in_Distance), in_Space);
        }

        public float GetRollAtTime(float in_T)
        {
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);
            return Mathf.Lerp(GetRollAtIndex(firstIndex), GetRollAtIndex(secondIndex) + (secondIndex == 0 ? m_RollAdjustmentPerIndex * m_Points.Count : 0), in_T);
        }

        public float GetRollAtDistance(float in_Distance)
        {
            return GetRollAtTime(DistanceToTime(in_Distance));
        }

        public Vector3 GetScaleAtTime(float in_T)
        {
            RemapTime(ref in_T, out int firstIndex, out int secondIndex);
            return Vector3.Scale(m_PathScale, Vector3.Lerp(GetScaleAtIndex(firstIndex), GetScaleAtIndex(secondIndex), in_T));
        }

        public Vector3 GetScaleAtDistance(float in_Distance)
        {
            return GetScaleAtTime(DistanceToTime(in_Distance));
        }

        public Matrix4x4 GetMatrixAtTime(float in_T, Space in_Space = DefaultSpace)
        {
            return Matrix4x4.TRS(
                GetPositionAtTime(in_T, in_Space),
                GetRotationAtTime(in_T, in_Space),
                GetScaleAtTime(in_T));
        }

        public Matrix4x4 GetMatrixAtDistance(float in_Distance, Space in_Space)
        {
            return GetMatrixAtTime(DistanceToTime(in_Distance), in_Space);
        }
        #endregion

        public void ResetRoll()
        {
            m_PathRoll = 0f;
            foreach (BezierPoint bp in m_Points)
                bp.Roll = 0f;

            SetPathMetadataChanged(true);
        }

        public void ResetScale()
        {
            m_PathScale = Vector3.one;
            foreach (BezierPoint bp in m_Points)
                bp.Scale = Vector3.one;

            SetPathMetadataChanged(true);
        }

        private void RemapTime(ref float in_T, out int out_FirstIndex, out int out_SecondIndex)
        {
            in_T = Mathf.Clamp01(in_T);
            int maxAllowedIndex = AdjustedLastIndex;
            out_FirstIndex = Mathf.FloorToInt(in_T * maxAllowedIndex);
            if (out_FirstIndex == maxAllowedIndex)
                out_FirstIndex -= 1;
            out_SecondIndex = (out_FirstIndex + 1 == m_Points.Count) ? 0 : out_FirstIndex + 1;

            float step = 1f / maxAllowedIndex;
            in_T = (in_T - step * out_FirstIndex) * maxAllowedIndex;
        }

        public float DistanceToTime(float in_Distance)
        {
            if (in_Distance >= GetLength())
                return 1f;

            int bottomIndex = 0;
            int topIndex = m_Distances.Count - 1;
            int midIndex = topIndex / 2;

            while (topIndex - bottomIndex > 1)
            {
                if (in_Distance <= m_Distances[midIndex])
                    topIndex = midIndex;
                else
                    bottomIndex = midIndex;

                midIndex = (bottomIndex + topIndex) / 2;
            }

            float startDistance = m_Distances[bottomIndex];
            float endDistance = m_Distances[topIndex];
            float t = Mathf.InverseLerp(startDistance, endDistance, in_Distance);
            float stepSize = 1f / (m_Distances.Count - 1);
            return bottomIndex * stepSize + t * stepSize;
        }

        public float GetLength()
        {
            if (m_IsLengthDirty)
            {
                m_IsLengthDirty = false;
                int steps = 100;
                float step = 1f / steps;
                m_Distances.Clear();
                m_Distances.Capacity = steps + 1;
                Vector3 prevPos = GetPositionAtTime(0f);
                float totalDistance = 0f;
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 newPos = GetPositionAtTime(i * step);
                    float distance = (newPos - prevPos).magnitude;
                    totalDistance += distance;
                    m_Distances.Add(totalDistance);
                    prevPos = newPos;
                }
                m_Length = totalDistance;
            }

            return m_Length;
        }

        private Vector3[] GetCachedWorldPositions()
        {
            if (m_AreCachedWorldPositionsDirty)
            {
                m_AreCachedWorldPositionsDirty = false;
                int steps = 100;
                float step = 1f / steps;
                m_CachedWorldPositions = new Vector3[steps];
                for (int i = 0; i < steps; i++)
                {
                    m_CachedWorldPositions[i] = GetPositionAtTime(i * step);
                }
            }

            return m_CachedWorldPositions;
        }

        // TODO: This needs a lot of cleanup
        public float FindTimeClosestToLine(Vector3 in_StartPoint, Vector3 in_Direction)
        {
            Vector3[] positions = GetCachedWorldPositions();
            float distance = float.MaxValue;
            int index = -1;
            for (int i = 0; i < positions.Length; i++)
            {
                float newDistance = Vector3.Cross(in_Direction, positions[i] - in_StartPoint).sqrMagnitude;
                if (newDistance < distance)
                {
                    distance = newDistance;
                    index = i;
                }
            }

            float t0 = (float)(index - 1) / positions.Length;
            float t1 = (float)(index + 1) / positions.Length;
            float d0 = Vector3.Cross(in_Direction, GetPositionAtTime(t0) - in_StartPoint).sqrMagnitude;
            float d1 = Vector3.Cross(in_Direction, GetPositionAtTime(t1) - in_StartPoint).sqrMagnitude;

            for (int i = 0; i < 10; i++)
            {
                float newT = (t0 + t1) / 2f;
                Vector3 newPos = GetPositionAtTime(newT);
                float newD = Vector3.Cross(in_Direction, newPos - in_StartPoint).sqrMagnitude;
                if (d0 < d1)
                {
                    d1 = newD;
                    t1 = newT;
                }
                else
                {
                    d0 = newD;
                    t0 = newT;
                }
            }

            float t = (d0 < d1) ? t0 : t1;
            return t;
        }

        private void UpdateAutoAndLinear()
        {
            int lastIndex = AdjustedLastIndex;
            for (int i = 0; i < lastIndex; i++)
            {
                DoAutoAtIndex(i);
                DoLinearAtIndex(i);
            }
        }

        private void UpdateSmartUpVector()
        {
            Vector3 startTangent = GetTangentAtIndex(0, Space.Self).normalized;
            Quaternion rotation = Quaternion.LookRotation(startTangent);
            Vector3 prevUpVector = rotation * Vector3.up;
            SetSmartUpVectorAtIndex(0, prevUpVector, Space.Self);

            for (int i = 1; i < m_Points.Count; i++)
            {
                Vector3 prevPos = GetPositionAtIndex(i - 1, Space.Self);
                Vector3 pos = GetPositionAtIndex(i, Space.Self);
                Vector3 prevTangent = GetTangentAtIndex(i - 1, Space.Self).normalized;
                Vector3 tangent = GetTangentAtIndex(i, Space.Self).normalized;
                Vector3 newUpVector = GetSmartUpVector(pos, prevPos, tangent, prevTangent, prevUpVector);
                SetSmartUpVectorAtIndex(i, newUpVector, Space.Self);
                prevUpVector = newUpVector;
            }

            int lastIndex = m_Points.Count - 1;
            float angle = -Vector3.SignedAngle(GetSmartUpVectorAtIndex(0, Space.Self), GetSmartUpVector(GetPositionAtTime(1f, Space.Self), GetPositionAtIndex(lastIndex, Space.Self), GetTangentAtTime(1f, Space.Self).normalized, GetTangentAtIndex(lastIndex, Space.Self).normalized, GetSmartUpVectorAtIndex(lastIndex, Space.Self)), GetTangentAtTime(1f, Space.Self).normalized);
            m_RollAdjustmentPerIndex = m_IsLooping ? angle / m_Points.Count : 0;
        }

        private Vector3 GetSmartUpVector(Vector3 in_Pos, Vector3 in_PrevPos, Vector3 in_Tangent, Vector3 in_PrevTangent, Vector3 in_PrevUpVector)
        {
            Vector3 reflectionNormal1 = (in_Pos - in_PrevPos).normalized;
            Vector3 reflectedPrevTangent = Vector3.Reflect(in_PrevTangent, reflectionNormal1);
            Vector3 reflectedPrevUpVector = Vector3.Reflect(in_PrevUpVector, reflectionNormal1);
            Vector3 newUpVector = Vector3.Reflect(reflectedPrevUpVector, (in_Tangent - reflectedPrevTangent).normalized);
            return newUpVector;
        }

        private void UpdateTangentByConnectionType(BezierPoint in_Point, bool in_AdjustOutToInTangent)
        {
            Tangent tangentToChange = in_AdjustOutToInTangent ? in_Point.OutTangent : in_Point.InTangent;
            Vector3 diff = in_Point.Position - (in_AdjustOutToInTangent ? in_Point.InTangent : in_Point.OutTangent).Position;

            if (in_Point.ConnectionType == TangentConnectionType.Mirrored)
            {
                tangentToChange.CurveType = CurveType.Free;
                tangentToChange.Position = in_Point.Position + diff;
            }
            if (in_Point.ConnectionType == TangentConnectionType.Connected)
            {
                tangentToChange.CurveType = CurveType.Free;
                float length = (tangentToChange.Position - in_Point.Position).magnitude;
                tangentToChange.Position = in_Point.Position + diff.normalized * length;
            }

            if (in_AdjustOutToInTangent)
                in_Point.OutTangent = tangentToChange;
            else
                in_Point.InTangent = tangentToChange;
        }

        private void DoLinearAtIndex(int in_index)
        {
            int secondIndex = in_index + 1 == m_Points.Count ? 0 : in_index + 1;
            BezierPoint bp0 = m_Points[in_index];
            BezierPoint bp1 = m_Points[secondIndex];
            if (bp0.OutTangent.CurveType == CurveType.Linear)
                bp0.OutTangent.Position = PathUtility.LinearBezier(bp0.Position, bp1.Position);
            if (bp1.InTangent.CurveType == CurveType.Linear)
                bp1.InTangent.Position = PathUtility.LinearBezier(bp1.Position, bp0.Position);
        }

        private void DoAutoAtIndex(int in_index)
        {
            int i1 = in_index;
            int i2 = in_index + 1 < m_Points.Count ? in_index + 1 : 0;

            if (m_Points[i1].OutTangent.CurveType != CurveType.Auto && m_Points[i2].InTangent.CurveType != CurveType.Auto)
                return;

            int i0 = in_index > 0 ? in_index - 1 : m_Points.Count - 1;
            int i3 = i2 + 1 < m_Points.Count ? i2 + 1 : 0;

            Vector3 point1 = m_Points[i1].Position;
            Vector3 point2 = m_Points[i2].Position;
            Vector3 point0 = i1 == 0 ? (m_IsLooping ? m_Points[i0].Position : point1 - 0.001f * (point1 + point2)) : m_Points[i0].Position;
            Vector3 point3 = i2 == m_Points.Count - 1 ? (m_IsLooping ? m_Points[0].Position : point2 + 0.001f * (point1 + point2)) : m_Points[i3].Position;
            Vector3 outTangent;
            Vector3 inTangent;
            PathUtility.RefitBezier(point0, point1, point2, point3, out outTangent, out inTangent);
            {
                BezierPoint point = m_Points[i1];
                if (point.OutTangent.CurveType == CurveType.Auto)
                    point.OutTangent.Position = outTangent;
            }
            {
                BezierPoint point = m_Points[i2];
                if (point.InTangent.CurveType == CurveType.Auto)
                    point.InTangent.Position = inTangent;
            }
        }

        internal void SetPathMetadataChanged(bool in_Value)
        {
            PathChanged = in_Value;
            if (PathChanged == true)
            {
                OnPathChanged?.Invoke(this);
            }
        }

        public void SetPathChanged(bool in_Value)
        {
            PathChanged = in_Value;
            if (PathChanged == true)
            {
                m_IsLengthDirty = true;
                m_AreCachedWorldPositionsDirty = true;
                UpdateSmartUpVector();
                OnPathChanged?.Invoke(this);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            }
        }

        public bool IsLooping {
            get {
                return m_IsLooping;
            }
            set {
                m_IsLooping = value;
                UpdateAutoAndLinear();
                SetPathChanged(true);
            }
        }

        public float PathRoll {
            get {
                return m_PathRoll;
            }
            set {
                m_PathRoll = value;
                SetPathMetadataChanged(true);
            }
        }

        public Vector3 PathScale {
            get {
                return m_PathScale;
            }
            set {
                m_PathScale = value;
                SetPathMetadataChanged(true);
            }
        }

        public ScaleInputType ScaleInputType {
            get {
                return m_ScaleInputType;
            }
            set {
                if (m_ScaleInputType != value)
                {
                    m_ScaleInputType = value;
                    m_PathScale = PathUtility.GetScale(m_PathScale, m_ScaleInputType);
                    foreach (BezierPoint bp in m_Points)
                        bp.Scale = PathUtility.GetScale(bp.Scale, m_ScaleInputType);
                    SetPathMetadataChanged(true);
                }
            }
        }

        private int AdjustedLastIndex { get { return m_IsLooping ? m_Points.Count : m_Points.Count - 1; } }
        public IReadOnlyList<BezierPoint> Points { get => m_Points; }
        public int PointCount { get => m_Points.Count; }
    }
}
