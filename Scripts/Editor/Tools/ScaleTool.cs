using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyBezier
{
    public class ScaleTool : ManipulationTool
    {
        private Space m_Space;
        private Vector3 m_StartDragPosition;
        private Vector3 m_InTangentOffset;
        private Vector3 m_OutTangentOffset;

        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            ScaleHandle(in_Editor, in_Index, true, false);
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            ScaleHandle(in_Editor, in_Index, false, true);
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            ScaleHandle(in_Editor, in_Index, true, true);
        }

        private void ScaleHandle(BezierPathComponentEditor in_Editor, int in_Index, bool in_SetInTangent, bool in_SetOutTangent)
        {
            Vector3 pos = in_Editor.Component.GetPositionAtIndex(in_Index);
            int lastHotControl = GUIUtility.hotControl;
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = Handles.ScaleHandle(Vector3.one, pos, in_Editor.ToolRotation, HandleUtility.GetHandleSize(pos));
            if (lastHotControl != GUIUtility.hotControl && lastHotControl == 0)
            {
                m_Space = in_Editor.ToolSpace;
                m_StartDragPosition = in_Editor.Component.GetPositionAtIndex(in_Index, m_Space);
                m_InTangentOffset = in_Editor.Component.GetInTangentPositionAtIndex(in_Index, m_Space) - m_StartDragPosition;
                m_OutTangentOffset = in_Editor.Component.GetOutTangentPositionAtIndex(in_Index, m_Space) - m_StartDragPosition;
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.Component, UndoStrings.Scale);
                if (in_SetInTangent)
                    in_Editor.Component.SetInTangentPositionAtIndex(in_Index, m_StartDragPosition + Vector3.Scale(m_InTangentOffset, newScale), m_Space);
                if (in_SetOutTangent)
                    in_Editor.Component.SetOutTangentPositionAtIndex(in_Index, m_StartDragPosition + Vector3.Scale(m_OutTangentOffset, newScale), m_Space);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}