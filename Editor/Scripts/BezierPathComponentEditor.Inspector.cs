using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
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

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.yellowpaper.easybezier/UXML/BezierPathComponentEditor.uxml");
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

            m_PointsLabel = ui.Query<Label>("eb-selected-point-toolbar__label").First();
            UpdatePointsLabel();

            ui.Query<Button>("eb-selected-point-toolbar__prev").First().clicked += delegate { SelectPointAtIndex(m_Selection.Index - 1, m_Selection.PointType); };
            ui.Query<Button>("eb-selected-point-toolbar__next").First().clicked += delegate { SelectPointAtIndex(m_Selection.Index + 1, m_Selection.PointType); };

            m_SelectedPointElement = ui.Query("eb-selected-point").First();
            m_BezierPointEditor = new BezierPointEditor(serializedObject.FindProperty("m_Points").GetArrayElementAtIndex(m_Selection.Index));
            m_SelectedPointElement.Add(m_BezierPointEditor);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.yellowpaper.easybezier/USS/BezierPathComponentEditor.uss");
            root.styleSheets.Add(styleSheet);

            VectorInputType scaleInputType = (VectorInputType)pathScaleInputType.GetSerializedProperty(serializedObject).enumValueIndex;
            pathScale.InputType = scaleInputType;
            m_BezierPointEditor.ScaleField.InputType = scaleInputType;

            OnIsEditingChanged += (sender, isEditing) => { m_SelectedPointElement.style.display = isEditing ? DisplayStyle.Flex : DisplayStyle.None; };
            OnSelectionChanged += delegate {
                UpdatePointsLabel();
                m_BezierPointEditor.RemoveFromHierarchy();
                m_BezierPointEditor = new BezierPointEditor(serializedObject.FindProperty("m_Points").GetArrayElementAtIndex(m_Selection.Index));
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

            public BezierPointEditor(SerializedProperty in_Property)
            {
                var positionField = new Vector3Field("Position");
                positionField.BindProperty(in_Property.FindPropertyRelative("Position"));
                Add(positionField);

                var enumField = new EnumField("ConnectionType");
                enumField.BindProperty(in_Property.FindPropertyRelative("ConnectionType"));
                Add(enumField);

                Add(CreateTangentGUI(in_Property.FindPropertyRelative("InTangent"), "In Tangent"));
                Add(CreateTangentGUI(in_Property.FindPropertyRelative("OutTangent"), "Out Tangent"));

                var roll = new FloatField("Roll");
                roll.BindProperty(in_Property.FindPropertyRelative("Roll"));
                Add(roll);

                ScaleField = new Vector3SwitchableInput("Scale");
                ScaleField.BindProperty(in_Property.FindPropertyRelative("Scale"));
                Add(ScaleField);
            }

            private VisualElement CreateTangentGUI(SerializedProperty in_TangentProperty, string in_Label)
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("eb-tangent");

                var label = new Label(in_Label);
                label.AddToClassList(BaseField<Vector3>.labelUssClassName);
                container.Add(label);

                var positionInput = new Vector3Field();
                positionInput.BindProperty(in_TangentProperty.FindPropertyRelative("Position"));
                positionInput.AddToClassList("eb-tangent__position");
                container.Add(positionInput);

                var curveTypeInput = new EnumField();
                curveTypeInput.BindProperty(in_TangentProperty.FindPropertyRelative("CurveType"));
                curveTypeInput.AddToClassList("eb-tangent__curve-type");
                container.Add(curveTypeInput);
                return container;
            }
        }
    }
}