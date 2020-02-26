using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditorInternal;
using System;

namespace EasyBezier
{
    public partial class BezierPathComponentEditor : Editor
    {
        ReorderableList m_ReorderableList;

        private static readonly float SINGLE_LINE_HEIGHT = EditorGUIUtility.singleLineHeight + 4;
        private static Space g_DisplaySpace = Space.Self;
        private static bool g_ShowSettingsInlined = false;

        private static GUIContent g_LocalSpaceGUIContent;
        private static GUIContent g_WorldSpaceGUIContent;

        private bool m_IsEditingRoll = false;
        private Editor m_SettingsEditor;

        void OnEnableInspector()
        {
            g_LocalSpaceGUIContent = EditorGUIUtility.TrTextContentWithIcon("Local", "Display positions in the inspector in local space.", "ToolHandleLocal");
            g_WorldSpaceGUIContent = EditorGUIUtility.TrTextContentWithIcon("World", "Display positions in the inspector in world space.", "ToolHandleGlobal");

            m_ReorderableList = new ReorderableList(Component.Points as IList, typeof(BezierPoint), false, true, true, true);
            m_ReorderableList.onAddCallback += HandleOnAdd;
            m_ReorderableList.onRemoveCallback += HandleOnRemove;
            m_ReorderableList.drawHeaderCallback += HandleDrawHeader;
            m_ReorderableList.drawElementCallback += HandleDrawElement;
            m_ReorderableList.elementHeightCallback += HandleElementHeight;
            m_ReorderableList.onSelectCallback += HandleSelect;

            SelectPointAtIndex(0, PointType.Point);
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Display Positions in:");
            if (GUILayout.Toggle(g_DisplaySpace == Space.Self, g_LocalSpaceGUIContent, new GUIStyle("ButtonLeft")))
                g_DisplaySpace = Space.Self;
            if (GUILayout.Toggle(g_DisplaySpace == Space.World, g_WorldSpaceGUIContent, new GUIStyle("ButtonRight")))
                g_DisplaySpace = Space.World;
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            bool newLoop = EditorGUILayout.Toggle("Loop", Component.IsLooping);
            if (EditorGUI.EndChangeCheck())
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.ChangeLoop);
                Component.IsLooping = newLoop;
            }

            EditorGUILayout.BeginHorizontal();
            int lastHotControl = GUIUtility.hotControl;
            GUI.SetNextControlName("EditRoll");
            EditorGUI.BeginChangeCheck();
            float newRoll = EditorGUILayout.Slider("Curve Roll", Component.PathRoll, -180f, 180f);
            if (EditorGUI.EndChangeCheck())
            {
                BezierEditorUtility.RecordUndo(Component, "Curve Roll");
                Component.PathRoll = newRoll;
            }

            // check if we are dragging the slider
            m_IsEditingRoll = (lastHotControl == 0 && lastHotControl != GUIUtility.hotControl) || (m_IsEditingRoll && GUIUtility.hotControl != 0);
            // check if we are inputting into the field
            if (GUI.GetNameOfFocusedControl() == "EditRoll")
            {
                try
                {
                    System.Reflection.FieldInfo info = typeof(EditorGUI).GetField("s_RecycledEditor", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    m_IsEditingRoll = m_IsEditingRoll || ((TextEditor)info.GetValue(null)).controlID != 0;
                }
                catch { }
            }

            if (GUILayout.Button("Reset All", GUILayout.Width(80)))
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.ResetRoll);
                Component.ResetRoll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = ScaleInputField(Component.PathScale, Component.ScaleInputType);
            if (EditorGUI.EndChangeCheck())
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.SetScale);
                Component.PathScale = newScale;
            }

            EditorGUI.BeginChangeCheck();
            ScaleInputType newScaleInputType = (ScaleInputType) EditorGUILayout.EnumPopup(Component.ScaleInputType, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.SetScaleInputType);
                Component.ScaleInputType = newScaleInputType;
            }

            if (GUILayout.Button("Reset All", GUILayout.Width(80)))
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.ResetScale);
                Component.ResetScale();
            }
            EditorGUILayout.EndHorizontal();

            m_ReorderableList.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(0));
            g_ShowSettingsInlined = EditorGUI.Foldout(rect, g_ShowSettingsInlined, "");
            EditorGUILayout.LabelField("Preferences", EditorStyles.largeLabel, GUILayout.Width(85));
            EditorGUILayoutUtility.HorizontalLine(1f, new Vector2(1f, (EditorGUIUtility.singleLineHeight - 1f) / 2f));
            if (GUILayout.Button("", new GUIStyle("WinBtnMax"), GUILayout.Width(22)))
            {
                g_ShowSettingsInlined = false;
                SettingsService.OpenUserPreferences(EasyBezierSettingsProvider.SETTINGS_PATH);
            }
            EditorGUILayout.EndHorizontal();

            if (g_ShowSettingsInlined)
            {
                if (m_SettingsEditor == null)
                    m_SettingsEditor = Editor.CreateEditor(EasyBezierSettings.Instance);
                EditorGUILayout.Space();
                m_SettingsEditor.OnInspectorGUI();
            }
            EditorGUILayout.Space();

            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
            {
                Component.SetPathChanged(true);
            }
        }

        private static Vector3 ScaleInputField(Vector3 in_Scale, ScaleInputType in_ScaleInputType)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 16f, EditorStyles.numberField);
            return ScaleInputField(rect, in_Scale, in_ScaleInputType);
        }

        private static Vector3 ScaleInputField(Rect in_Rect, Vector3 in_Scale, ScaleInputType in_ScaleInputType)
        {
            Vector3 newScale = in_Scale;
            if (in_ScaleInputType == ScaleInputType.Float)
            {
                float s = EditorGUI.FloatField(in_Rect, "Path Scale", in_Scale.x);
                newScale = new Vector3(s, s, s);
            }
            else if (in_ScaleInputType == ScaleInputType.Vector2)
            {
                newScale = EditorGUI.Vector2Field(in_Rect, "Path Scale", in_Scale);
                newScale.z = 1f;
            }
            else
                newScale = EditorGUI.Vector3Field(in_Rect, "Path Scale", in_Scale);

            return newScale;
        }

        private void HandleOnAdd(ReorderableList list)
        {
            BezierEditorUtility.RecordUndo(Component, UndoStrings.AddPoint);
            Component.AddPoint();
        }

        private void HandleOnRemove(ReorderableList list)
        {
            if (Component.PointCount > 2)
            {
                BezierEditorUtility.RecordUndo(Component, UndoStrings.RemovePoint);
                Component.RemovePointAt(list.index);
            }
        }

        private void HandleDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Points");
        }

        private float HandleElementHeight(int index)
        {
            if (BezierEditorUtility.Booleans[GetFoldoutString(index)])
                return SINGLE_LINE_HEIGHT * 9;
            else
                return SINGLE_LINE_HEIGHT;
        }

        private void HandleDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.x += 10;
            rect.width -= 10;
            rect.height = EditorGUIUtility.singleLineHeight;
            Rect foldoutRect = rect;
            foldoutRect.width = 5;
            bool foldout = BezierEditorUtility.Booleans[GetFoldoutString(index)] = EditorGUI.Foldout(foldoutRect, BezierEditorUtility.Booleans[GetFoldoutString(index)], "Point " + (index + 1));
            if (foldout)
            {
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return EditorGUI.Vector3Field(rect, "Point", Component.GetPositionAtIndex(index, g_DisplaySpace)); }, (pos) => { Component.SetPositionAtIndex(index, pos, g_DisplaySpace); }, UndoStrings.SetPointPosition);
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return EditorGUI.Vector3Field(rect, "In Tangent", Component.GetInTangentPositionAtIndex(index, g_DisplaySpace)); }, (pos) => { Component.SetInTangentPositionAtIndex(index, pos, g_DisplaySpace); }, UndoStrings.SetInTangentPosition);
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return EditorGUI.Vector3Field(rect, "Out Tangent", Component.GetOutTangentPositionAtIndex(index, g_DisplaySpace)); }, (pos) => { Component.SetOutTangentPositionAtIndex(index, pos, g_DisplaySpace); }, UndoStrings.SetOutTangentPosition);
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return (CurveType)EditorGUI.EnumPopup(rect, "In Tangent Curve Type", Component.GetInTangentCurveTypeAtIndex(index)); }, (type) => { Component.SetInTangentCurveTypeAtIndex(index, type); }, UndoStrings.SetInTangentCurveType);
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return (CurveType)EditorGUI.EnumPopup(rect, "Out Tangent Curve Type", Component.GetOutTangentCurveTypeAtIndex(index)); }, (type) => { Component.SetOutTangentCurveTypeAtIndex(index, type); }, UndoStrings.SetOutTangentPosition);
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return (TangentConnectionType)EditorGUI.EnumPopup(rect, "Connected Tangents", Component.GetTangentConnectionTypeAtIndex(index)); }, (val) => { Component.SetTangentConnectionTypeAtIndex(index, val); }, UndoStrings.SetConnectedTangents);
                rect.y += SINGLE_LINE_HEIGHT;
                EditorGUI.BeginChangeCheck();
                ChangeCheckingField(() => { return EditorGUI.FloatField(rect, "Additional Roll", Component.GetAdjustmentRollAtIndex(index)); }, (val) => { Component.SetAdjustmentRollAtIndex(index, val); }, UndoStrings.SetAdjustmentAngle);
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsEditingRoll = true;
                }
                rect.y += SINGLE_LINE_HEIGHT;
                ChangeCheckingField(() => { return ScaleInputField(rect, Component.GetScaleAtIndex(index), Component.ScaleInputType); }, (val) => { Component.SetScaleAtIndex(index, val); }, UndoStrings.SetScale);
            }
        }

        private void HandleSelect(ReorderableList in_List)
        {
            SelectPointAtIndex(in_List.index, PointType.Point);
        }

        private string GetFoldoutString(int in_Index)
        {
            return "Point" + in_Index;
        }

        private void ChangeCheckingField<T>(System.Func<T> in_PropertyFunction, System.Action<T> in_SetterMethod, string in_UndoString)
        {
            EditorGUI.BeginChangeCheck();
            T newValue = in_PropertyFunction();
            if (EditorGUI.EndChangeCheck())
            {
                BezierEditorUtility.RecordUndo(Component, in_UndoString);
                in_SetterMethod(newValue);
            }
        }

        public void SelectPointAtIndex(int in_Index, PointType in_PointType)
        {
            m_Selection = new PointSelection(in_Index, in_PointType);
            if (m_ReorderableList.index != in_Index)
                m_ReorderableList.index = in_Index;

            for (int i = 0; i < Component.PointCount; i++)
                BezierEditorUtility.Booleans[GetFoldoutString(i)] = false;
            BezierEditorUtility.Booleans[GetFoldoutString(in_Index)] = true;

            Tools.hidden = true;
            Repaint();
        }

        public void Hover(int in_Index, PointType in_PointType)
        {
            m_Hovering = new PointSelection(in_Index, in_PointType);
        }

        public void Deselect()
        {
            m_Selection.Index = -1;
            Tools.hidden = false;
        }
    }
}