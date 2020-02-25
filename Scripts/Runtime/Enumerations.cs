using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    public enum PointType
    {
        Point,
        InTangent,
        OutTangent
    }

    public enum ScaleInputType
    {
        Float,
        Vector2,
        Vector3
    }

    public enum TangentConnectionType
    {
        Broken,
        Connected,
        Mirrored
    }

    public enum CurveType
    {
        Auto,
        Linear,
        Free
    }

    public enum Axis
    {
        X,
        Y,
        Z
    }

    public enum FittingType
    {
        Length,
        Count
    }

    public enum MeasurementUnit
    {
        Time,
        Distance
    }

    public static class EnumerationExtensions
    {
        public static TangentConnectionType GetPreferredConnectionType(this CurveType in_CurveType, TangentConnectionType in_CurrentConnectionType = TangentConnectionType.Connected)
        {
            switch (in_CurveType)
            {
                case CurveType.Auto:
                    return TangentConnectionType.Connected;
                case CurveType.Linear:
                    return TangentConnectionType.Broken;
                case CurveType.Free:
                    return in_CurrentConnectionType;
            }

            return in_CurrentConnectionType;
        }
    }
}