using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyBezier
{
    public class RotationTool : ManipulationTool
    {
        private Vector3 m_StartDragPosition;
        private Vector3 m_InTangentOffset;
        private Vector3 m_OutTangentOffset;

        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            RotationHandle(in_Editor, in_Index, true, false);
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            RotationHandle(in_Editor, in_Index, false, true);
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            RotationHandle(in_Editor, in_Index, true, true);
        }

        private void RotationHandle(BezierPathComponentEditor in_Editor, int in_Index, bool in_SetInTangent, bool in_SetOutTangent)
        {
            Vector3 pos = in_Editor.Component.GetPositionAtIndex(in_Index);
            int lastHotControl = GUIUtility.hotControl;
            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(in_Editor.ToolRotation, pos);
            if (lastHotControl != GUIUtility.hotControl && lastHotControl == 0)
            {
                m_StartDragPosition = in_Editor.Component.GetPositionAtIndex(in_Index);
                m_InTangentOffset = in_Editor.Component.GetInTangentPositionAtIndex(in_Index) - m_StartDragPosition;
                m_OutTangentOffset = in_Editor.Component.GetOutTangentPositionAtIndex(in_Index) - m_StartDragPosition;
            }
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion rotationalDifference = Quaternion.Inverse(in_Editor.ToolRotation) * newRotation;
                BezierEditorUtility.RecordUndo(in_Editor.Component, UndoStrings.Scale);
                if (in_SetInTangent)
                    in_Editor.Component.SetInTangentPositionAtIndex(in_Index, m_StartDragPosition + rotationalDifference * m_InTangentOffset);
                if (in_SetOutTangent)
                    in_Editor.Component.SetOutTangentPositionAtIndex(in_Index, m_StartDragPosition + rotationalDifference * m_OutTangentOffset);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}