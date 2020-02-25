using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EasyBezier
{
    public class RollTool : ManipulationTool
    {
        public override bool NeedsSelection => false;

        ArcHandle m_ArcHandle;

        public RollTool()
        {
            m_ArcHandle = new ArcHandle();
        }

        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            float roll = in_Editor.Component.GetAdjustmentRollAtIndex(in_Index);
            Vector3 pos = in_Editor.Component.GetPositionAtIndex(in_Index);
            Quaternion orientation = Quaternion.LookRotation(in_Editor.Component.GetSmartUpVectorAtIndex(in_Index), in_Editor.Component.GetTangentAtIndex(in_Index));
            Matrix4x4 handleMatrix = Matrix4x4.TRS(pos, orientation, Vector3.one);
            using (new Handles.DrawingScope(handleMatrix))
            {
                //Handles.Label(new Vector3(0, 0, 0.55f), roll.ToString());
                m_ArcHandle.SetColorWithoutRadiusHandle(Color.red, 0.1f);
                m_ArcHandle.angle = roll;
                m_ArcHandle.radius = 0.4f;
                Handles.color = new Color(0.6f, 0f, 0f, 0.7f);
                float angle = roll > 0f ? -360f + Mathf.Min(roll, 360f) : 360f + Mathf.Max(roll, -360f);
                Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, angle, 0.4f);
                Handles.DrawLine(Vector3.zero, Vector3.forward * 0.4f);
                EditorGUI.BeginChangeCheck();
                Handles.color = Color.white;
                m_ArcHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(in_Editor.Component, UndoStrings.SetRoll);
                    in_Editor.Component.SetAdjustmentRollAtIndex(in_Index, m_ArcHandle.angle);
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }
    }
}