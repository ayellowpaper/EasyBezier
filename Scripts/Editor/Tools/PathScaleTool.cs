using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor;

namespace EasyBezier
{
    public class PathScaleTool : ManipulationTool
    {
        public override bool NeedsSelection => false;

        private int m_CachedIndex = -1;
        private Vector3 m_CachedScale = Vector3.one;

        public PathScaleTool()
        {
        }

        public override void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
        }

        public override void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index)
        {
        }

        public override void DoPoint(BezierPathComponentEditor in_Editor, int in_Index)
        {
            Handles.color = Color.red;
            Vector3 pos = in_Editor.Component.GetPositionAtIndex(in_Index);
            Vector3 forwardDir = in_Editor.Component.GetForwardVectorAtIndex(in_Index);
            Vector3 upDir = in_Editor.Component.GetUpVectorAtIndex(in_Index);
            Vector3 scale = in_Editor.Component.GetScaleAtIndex(in_Index);
            float visualScale = EasyBezierSettings.Instance.Settings.PathScaleGizmoSize * 0.5f;
            Handles.DrawWireDisc(pos, forwardDir, scale.x * visualScale);
            EditorGUI.BeginChangeCheck();
            MoreHandles.ListenForHandleChange();
            float newScale = Handles.ScaleSlider(scale.x, pos, upDir * (m_CachedIndex == in_Index ? m_CachedScale : scale).x, Quaternion.LookRotation(upDir), visualScale, 1f);
            if (MoreHandles.WasHandleSelected())
            {
                m_CachedIndex = in_Index;
                m_CachedScale = scale;
            }
            else if (MoreHandles.WasHandleDeselected())
            {
                m_CachedIndex = -1;
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(in_Editor.Component, UndoStrings.SetScale);
                in_Editor.Component.SetScaleAtIndex(in_Index, new Vector3(newScale, newScale, newScale));
                EditorApplication.QueuePlayerLoopUpdate();
            }
            //Handles.CircleHandleCap(-1, pos, Quaternion.LookRotation(forwardDir), scale.x, Event.current.type);
        }
    }
}