using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    [ExecuteAlways]
    public class PathAttachment : MonoBehaviour
    {
        public BezierPathComponent PathComponent;

        public MeasurementUnit AttachmentType;
        public float AttachedDistance = 0f;
        [Range(0f, 1f)]
        public float AttachedTime = 0f;
        public bool CopyRotation = false;
        public bool CopyScale = false;
        public Vector3 ScaleAdjustment = Vector3.one;

        private void Update()
        {
            if (PathComponent != null)
            {
                float time = AttachmentType == MeasurementUnit.Distance ? PathComponent.DistanceToTime(AttachedDistance) : AttachedTime;
                transform.position = PathComponent.GetPositionAtTime(time);
                if (CopyRotation)
                    transform.rotation = PathComponent.GetRotationAtTime(time);
                if (CopyScale)
                    transform.localScale = Vector3.Scale(PathComponent.GetScaleAtTime(time), ScaleAdjustment);
            }
        }
    }
}