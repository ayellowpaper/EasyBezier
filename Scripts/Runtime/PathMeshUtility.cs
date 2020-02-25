using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    public static class PathMeshUtility
    {
        public static int GetBestFit(float in_MeshLength, float in_PathLength, float in_MeshFitting)
        {
            Debug.Assert(in_PathLength > 0f && in_MeshLength > 0f, "Path Length and Mesh Length need to be greater than 0.");
            float val = in_PathLength / in_MeshLength;
            float decimalPart = val % 1f;
            if (decimalPart <= in_MeshFitting)
                return Mathf.CeilToInt(val);
            else
                return Mathf.FloorToInt(val);
        }

        public static int GetBestFitFromToDistance(float in_MeshFitting, float in_MeshLength, float in_StartDistance, float in_EndDistance)
        {
            return GetBestFit(in_EndDistance - in_StartDistance, in_MeshLength, in_MeshFitting);
        }
    }
}