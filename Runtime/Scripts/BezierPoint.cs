using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    [System.Serializable]
    public class BezierPoint
    {
        [SerializeField]
        public Vector3 Position;
        [SerializeField]
        public TangentConnectionType ConnectionType;
        [SerializeField]
        public Tangent InTangent;
        [SerializeField]
        public Tangent OutTangent;
        [SerializeField]
        public Vector3 SmartUpVector = Vector3.up;
        [SerializeField]
        public float Roll = 0f;
        [SerializeField]
        public Vector3 Scale = Vector3.one;

        [System.Serializable]
        public struct Tangent
        {
            public Vector3 Position;
            public CurveType CurveType;

            public Tangent(Vector3 in_Position, CurveType in_InterpolationMode)
            {
                Position = in_Position;
                CurveType = in_InterpolationMode;
            }
        }

        public static readonly Vector3 IN_TANGENT = new Vector3(-1, 0, 0);
        public static readonly Vector3 OUT_TANGENT = new Vector3(1, 0, 0);
        public static readonly CurveType DEFAULT_CURVETYPE = CurveType.Auto;

        public BezierPoint(Vector3 in_Position)
        {
            Position = in_Position;
            ConnectionType = DEFAULT_CURVETYPE.GetPreferredConnectionType();
            InTangent = new Tangent(in_Position + IN_TANGENT, DEFAULT_CURVETYPE);
            OutTangent = new Tangent(in_Position + OUT_TANGENT, DEFAULT_CURVETYPE);
        }

        public Vector3 GetInTangentOffset()
        {
            return InTangent.Position - Position;
        }

        public Vector3 GetOutTangentOffset()
        {
            return OutTangent.Position - Position;
        }
    }
}