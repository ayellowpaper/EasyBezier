using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    public static class PathUtility
    {
        public static Vector3 LinearBezier(Vector3 in_Point0, Vector3 in_Point1)
        {
            return in_Point0 + (in_Point1 - in_Point0) / 3f;
        }

        public static void RefitBezier(Vector3 in_Point0, Vector3 in_Point1, Vector3 in_Point2, Vector3 in_Point3, out Vector3 out_Tangent0, out Vector3 out_Tangent1)
        {
            float alpha = 0f;

            float d1 = Mathf.Pow(Vector3.Distance(in_Point1, in_Point0), alpha);
            float d2 = Mathf.Pow(Vector3.Distance(in_Point2, in_Point1), alpha);
            float d3 = Mathf.Pow(Vector3.Distance(in_Point3, in_Point2), alpha);

            // Modify tangent 1
            {
                float a = d1 * d1;
                float b = d2 * d2;
                float c = (2 * d1 * d1) + (3 * d1 * d2) + (d2 * d2);
                float d = 3 * d1 * (d1 + d2);
                out_Tangent0 = (a * in_Point2 - b * in_Point0 + c * in_Point1) / d;
            }
            // Modify tangent 2
            {
                float a = d3 * d3;
                float b = d2 * d2;
                float c = (2 * d3 * d3) + (3 * d3 * d2) + (d2 * d2);
                float d = 3 * d3 * (d3 + d2);
                out_Tangent1 = (a * in_Point1 - b * in_Point3 + c * in_Point2) / d;
            }
        }

        public static Vector3 GetScale(Vector3 in_Scale, VectorInputType in_ScaleInputType)
        {
            if (in_ScaleInputType == VectorInputType.Float)
                return new Vector3(in_Scale.x, in_Scale.x, in_Scale.x);
            else if (in_ScaleInputType == VectorInputType.Vector2)
                return new Vector3(in_Scale.x, in_Scale.y, 1f);

           return in_Scale;
        }
    }
}