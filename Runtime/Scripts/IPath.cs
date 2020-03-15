using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    public interface IPath
    {
        float DistanceToTime(float in_Distance);
        Vector3 GetPositionAtTime(float in_T, Space in_Space);
        Vector3 GetPositionAtDistance(float in_Distance, Space in_Space);
        Vector3 GetTangentAtTime(float in_T, Space in_Space);
        Vector3 GetTangentAtDistance(float in_Distance, Space in_Space);
        Vector3 GetUpVectorAtTime(float in_T, Space in_Space);
        Vector3 GetUpVectorAtDistance(float in_Distance, Space in_Space);
        Vector3 GetRightVectorAtTime(float in_T, Space in_Space);
        Vector3 GetRightVectorAtDistance(float in_Distance, Space in_Space);
        Vector3 GetForwardVectorAtTime(float in_T, Space in_Space);
        Vector3 GetForwardVectorAtDistance(float in_Distance, Space in_Space);
        Quaternion GetRotationAtTime(float in_T, Space in_Space);
        Quaternion GetRotationAtDistance(float in_Distance, Space in_Space);
        float GetRollAtTime(float in_T);
        float GetRollAtDistance(float in_Distance);
        Vector3 GetScaleAtTime(float in_T);
        Vector3 GetScaleAtDistance(float in_Distance);
        Matrix4x4 GetMatrixAtTime(float in_T, Space in_Space);
        Matrix4x4 GetMatrixAtDistance(float in_Distance, Space in_Space);
    }
}