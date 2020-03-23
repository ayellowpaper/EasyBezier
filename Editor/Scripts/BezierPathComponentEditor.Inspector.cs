using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using EasyBezier.UIElements;

namespace EasyBezier
{
    public partial class BezierPathComponentEditor : Editor
    {
        private static readonly float SINGLE_LINE_HEIGHT = EditorGUIUtility.singleLineHeight + 4;
        private static Space g_DisplaySpace = Space.Self;

        private static GUIContent g_LocalSpaceGUIContent;
        private static GUIContent g_WorldSpaceGUIContent;

        private bool m_IsEditingRoll = false;
        private VisualElement m_SelectedPointElement;
        private BezierPointEditor m_BezierPointEditor;
        private Label m_PointsLabel;

        public event System.Action<BezierPathComponentEditor> OnSelectionChanged;

        void OnEnableInspector()
        {
            g_LocalSpaceGUIContent = EditorGUIUtility.TrTextContentWithIcon("Local", "Display positions in the inspector in local space.", "ToolHandleLocal");
            g_WorldSpaceGUIContent = EditorGUIUtility.TrTextContentWithIcon("World", "Display positions in the inspector in world space.", "ToolHandleGlobal");

            SelectPointAtIndex(0, PointType.Point);
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            var imgui = new IMGUIContainer(delegate
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Display Positions in:");
                if (GUILayout.Toggle(g_DisplaySpace == Space.Self, g_LocalSpaceGUIContent, new GUIStyle("ButtonLeft")))
                    g_DisplaySpace = Space.Self;
                if (GUILayout.Toggle(g_DisplaySpace == Space.World, g_WorldSpaceGUIContent, new GUIStyle("ButtonRight")))
                    g_DisplaySpace = Space.World;
                GUILayout.EndHorizontal();
            });

            root.Add(imgui);

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EasyBezierSettings.EditorUXMLPath + "/BezierPathComponentEditor.uxml");
            VisualElement ui = template.CloneTree();
            root.Add(ui);

            var pathScale = ui.Query<Vector3SwitchableInput>("path-scale").First();
            var pathScaleInputType = ui.Query<EnumField>("path-scale-input-type").First();
            pathScaleInputType.RegisterValueChangedCallback(x => { var inputType = (VectorInputType)x.newValue; pathScale.InputType = inputType; m_BezierPointEditor.ScaleField.InputType = inputType; });

            ui.Query<Button>("reset-path-roll").First().clicked += delegate
            {
                Undo.RecordObject(Component, UndoStrings.ResetRoll);
                Component.ResetRoll();
                PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
            };

            ui.Query<Button>("reset-path-scale").First().clicked += delegate
            {
                Undo.RecordObject(Component, UndoStrings.ResetScale);
                Component.ResetScale();
                PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
            };

            m_PointsLabel = ui.Query<Label>("eb-selected-point-header__label").First();
            UpdatePointsLabel();

            ui.Query<Button>("eb-selected-point-header__prev").First().clicked += delegate { SelectPointAtIndex(m_Selection.Index - 1, m_Selection.PointType); };
            ui.Query<Button>("eb-selected-point-header__next").First().clicked += delegate { SelectPointAtIndex(m_Selection.Index + 1, m_Selection.PointType); };

            m_SelectedPointElement = ui.Q("eb-selected-point");
            m_BezierPointEditor = new BezierPointEditor(Component, m_Selection.Index);
            m_BezierPointEditor.AddToClassList("eb-container__content");
            m_SelectedPointElement.Add(m_BezierPointEditor);

            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EasyBezierSettings.EditorUSSPath + "/General.uss"));
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EasyBezierSettings.EditorUSSPath + "/BezierPathComponentEditor.uss"));

            VectorInputType scaleInputType = (VectorInputType)pathScaleInputType.GetSerializedProperty(serializedObject).enumValueIndex;
            pathScale.InputType = scaleInputType;
            m_BezierPointEditor.ScaleField.InputType = scaleInputType;

            OnIsEditingChanged += (sender, isEditing) => { m_SelectedPointElement.style.display = isEditing ? DisplayStyle.Flex : DisplayStyle.None; };
            OnSelectionChanged += delegate {
                UpdatePointsLabel();
                m_BezierPointEditor.RemoveFromHierarchy();
                m_BezierPointEditor = new BezierPointEditor(Component, m_Selection.Index);
                m_BezierPointEditor.ScaleField.InputType = (VectorInputType)pathScaleInputType.GetSerializedProperty(serializedObject).enumValueIndex;
                m_SelectedPointElement.Add(m_BezierPointEditor);
            };

            return root;
        }

        private void UpdatePointsLabel()
        {
            if (m_PointsLabel != null)
                m_PointsLabel.text = $"Point {m_Selection.Index + 1}/{Component.PointCount}";
        }

        public void SelectPointAtIndex(int in_Index, PointType in_PointType)
        {
            if (in_Index < 0 || in_Index >= Component.PointCount)
                return;

            m_Selection = new PointSelection(in_Index, in_PointType);
            Tools.hidden = true;
            OnSelectionChanged?.Invoke(this);
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

        public class BezierPointEditor : VisualElement
        {
            public Vector3SwitchableInput ScaleField { get; private set; }

            private BezierPathComponent m_Component;

            public BezierPointEditor(BezierPathComponent in_Component, int in_Index)
            {
                m_Component = in_Component;
                var positionField = new Vector3Field("Position");
                positionField.AddToClassList("eb-vector-field");
                positionField.BindBackingField(() => m_Component.GetPositionAtIndex(in_Index), x => m_Component.SetPositionAtIndex(in_Index, x), m_Component, UndoStrings.SetPointPosition);
                Add(positionField);

                var enumField = new EnumField("Connection Type");
                enumField.BindBackingField(() => m_Component.GetTangentConnectionTypeAtIndex(in_Index), x => m_Component.SetTangentConnectionTypeAtIndex(in_Index, (TangentConnectionType) x), m_Component, UndoStrings.SetConnectedTangents);
                Add(enumField);

                Add(CreateTangentGUI("In Tangent", () => m_Component.GetInTangentPositionAtIndex(in_Index), x => m_Component.SetInTangentPositionAtIndex(in_Index, x), () => m_Component.GetInTangentCurveTypeAtIndex(in_Index), x => m_Component.SetInTangentCurveTypeAtIndex(in_Index, (CurveType)x), UndoStrings.SetInTangentPosition, UndoStrings.SetInTangentCurveType));
                Add(CreateTangentGUI("Out Tangent", () => m_Component.GetOutTangentPositionAtIndex(in_Index), x => m_Component.SetOutTangentPositionAtIndex(in_Index, x), () => m_Component.GetOutTangentCurveTypeAtIndex(in_Index), x => m_Component.SetOutTangentCurveTypeAtIndex(in_Index, (CurveType)x), UndoStrings.SetOutTangentPosition, UndoStrings.SetOutTangentCurveType));

                var roll = new FloatField("Roll");
                roll.BindBackingField(() => m_Component.GetAdjustmentRollAtIndex(in_Index), x => m_Component.SetAdjustmentRollAtIndex(in_Index, x), m_Component, UndoStrings.SetRoll);
                Add(roll);

                ScaleField = new Vector3SwitchableInput("Scale");
                ScaleField.BindBackingField(() => m_Component.GetScaleAtIndex(in_Index), x => m_Component.SetScaleAtIndex(in_Index, x), m_Component, UndoStrings.SetScale);
                Add(ScaleField);
            }

            private VisualElement CreateTangentGUI(string in_Label, Func<Vector3> in_PositionGetter, Action<Vector3> in_PositionSetter, Func<Enum> in_CurveTypeGetter, Action<Enum> in_CurveTypeSetter, string in_PositionUndoString, string in_CurveTypeUndoString)
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("eb-tangent");

                var label = new Label(in_Label);
                label.AddToClassList(BaseField<Vector3>.labelUssClassName);
                container.Add(label);

                var positionInput = new Vector3Field();
                positionInput.BindBackingField(in_PositionGetter, in_PositionSetter, m_Component, in_PositionUndoString);
                positionInput.AddToClassList("eb-tangent__position");
                positionInput.AddToClassList("eb-vector-field");
                container.Add(positionInput);

                var curveTypeInput = new EnumField();
                curveTypeInput.BindBackingField(in_CurveTypeGetter, in_CurveTypeSetter, m_Component, in_CurveTypeUndoString);
                curveTypeInput.AddToClassList("eb-tangent__curve-type");
                container.Add(curveTypeInput);
                return container;
            }
        }
    }
}