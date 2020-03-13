using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static EasyBezier.BezierPathComponentEditor;

namespace EasyBezier
{
    public class FreeMoveTool : ManipulationTool
    {
        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            bool fill = in_Editor.Component.GetInTangentCurveTypeAtIndex(in_Index) != CurveType.Free;
            MoreHandles.PositionButton posButton = DrawSinglePoint(in_Editor, in_Editor.Component.GetInTangentPositionAtIndex(in_Index), GetSelectionSize(in_Editor, in_Index, PointType.InTangent) * EasyBezierSettings.Instance.GetTangentHandleSize(), fill, (pos) => { in_Editor.Component.SetInTangentPositionAtIndex(in_Index, pos); }, UndoStrings.SetInTangentPosition);
            if (posButton.WasClicked)
                in_Editor.SelectPointAtIndex(in_Index, PointType.InTangent);
            if (posButton.IsHovering)
                in_Editor.Hover(in_Index, PointType.InTangent);
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
            bool fill = in_Editor.Component.GetOutTangentCurveTypeAtIndex(in_Index) != CurveType.Free;
            MoreHandles.PositionButton posButton = DrawSinglePoint(in_Editor, in_Editor.Component.GetOutTangentPositionAtIndex(in_Index), GetSelectionSize(in_Editor, in_Index, PointType.OutTangent) * EasyBezierSettings.Instance.GetTangentHandleSize(), fill, (pos) => { in_Editor.Component.SetOutTangentPositionAtIndex(in_Index, pos); }, UndoStrings.SetOutTangentPosition);
            if (posButton.WasClicked)
                in_Editor.SelectPointAtIndex(in_Index, PointType.OutTangent);
            if (posButton.IsHovering)
                in_Editor.Hover(in_Index, PointType.OutTangent);
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            MoreHandles.PositionButton posButton = DrawSinglePoint(in_Editor, in_Editor.Component.GetPositionAtIndex(in_Index), GetSelectionSize(in_Editor, in_Index, PointType.Point) * EasyBezierSettings.Instance.GetPointHandleSize(), true, (pos) => { in_Editor.Component.SetPositionAtIndex(in_Index, pos); }, UndoStrings.SetPointPosition);
            if (posButton.WasClicked)
                in_Editor.SelectPointAtIndex(in_Index, PointType.Point);
            if (posButton.IsHovering)
                in_Editor.Hover(in_Index, PointType.Point);
        }

        public float GetSelectionSize(BezierPathComponentEditor in_Editor, int in_Index, PointType in_PointType)
        {
            bool isSelected = in_Editor.IsSelected(in_Index, in_PointType);
            return (isSelected ? EasyBezierSettings.Instance.Settings.SelectedHandleSize : 1f);
        }

        public MoreHandles.PositionButton DrawSinglePoint(BezierPathComponentEditor in_Editor, Vector3 in_Position, float in_Size, bool in_Fill, System.Action<Vector3> in_Callback, string in_UndoText)
        {
            MoreHandles.PositionButton posButton = new MoreHandles.PositionButton(false, false, in_Position);
            EditorGUI.BeginChangeCheck();
            posButton = MoreHandles.SelectableMoveHandle(GUIUtility.GetControlID(FocusType.Passive), in_Position, in_Size, in_Fill);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.target, in_UndoText);
                in_Callback(posButton.Position);
                PrefabUtility.RecordPrefabInstancePropertyModifications(in_Editor.Component);
            }

            return posButton;
        }
    }
}