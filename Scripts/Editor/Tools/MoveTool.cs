using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyBezier
{
    public class MoveTool : ManipulationTool
    {
        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            Vector3 pos = in_Editor.Component.GetInTangentPositionAtIndex(in_Index);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pos, in_Editor.ToolRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.Component, UndoStrings.SetInTangentPosition);
                in_Editor.Component.SetInTangentPositionAtIndex(in_Index, newPosition);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            Vector3 pos = in_Editor.Component.GetOutTangentPositionAtIndex(in_Index);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pos, in_Editor.ToolRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.Component, UndoStrings.SetOutTangentPosition);
                in_Editor.Component.SetOutTangentPositionAtIndex(in_Index, newPosition);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            Vector3 pos = in_Editor.Component.GetPositionAtIndex(in_Index);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                if (HandleUtility.DistanceToCircle(pos, EasyBezierSettings.Instance.GetPointHandleSize(pos)) <= BezierPathComponentEditor.PickDistance)
                    in_Editor.ShowPointContextMenu(in_Index);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pos, in_Editor.ToolRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.Component, UndoStrings.SetPointPosition);
                in_Editor.Component.SetPositionAtIndex(in_Index, newPosition);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}