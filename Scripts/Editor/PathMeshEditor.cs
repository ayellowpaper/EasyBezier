using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;

namespace EasyBezier
{
    [CustomEditor(typeof(PathMesh))]
    public class PathMeshEditor : Editor
    {
        private PathMesh m_Component;
        private MeshRenderer m_MeshRenderer;
        private MeshFilter m_MeshFilter;

        private VisualElement m_Root;
        private VisualElement m_CountElement;
        private VisualElement m_MeshFittingElement;

        private void OnEnable()
        {
            m_Component = target as PathMesh;
            m_MeshFilter = m_Component.gameObject.GetComponent<MeshFilter>();
            m_MeshRenderer = m_Component.gameObject.GetComponent<MeshRenderer>();
            m_MeshFilter.hideFlags = HideFlags.NotEditable;
            m_MeshRenderer.hideFlags = HideFlags.None;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();

            //var curveMeshField = new PropertyField(serializedObject.FindProperty("m_PathMeshData"));
            //curveMeshField.RegisterCallback<ChangeEvent<object>>(IntermediateMeshChangedCallback);
            var pathMeshField = new CustomIntermediateMeshEditor(serializedObject.FindProperty("m_PathMeshData"));
            pathMeshField.OnChanged += IntermediateMeshChangedCallback;

            var startMeshProperty = serializedObject.FindProperty("m_StartMeshData");
            var startMeshEditor = new CustomCapMeshEditor("Use Start Mesh", startMeshProperty);
            startMeshEditor.OnChanged += IntermediateMeshChangedCallback;

            var endMeshProperty = serializedObject.FindProperty("m_EndMeshData");
            var endMeshEditor = new CustomCapMeshEditor("Use End Mesh", endMeshProperty);
            endMeshEditor.OnChanged += IntermediateMeshChangedCallback;

            var generatedMeshField = new ObjectField("GeneratedMesh");
            generatedMeshField.objectType = typeof(Mesh);
            generatedMeshField.value = m_MeshFilter.sharedMesh;
            generatedMeshField.SetEnabled(false);
            //generatedMeshField.SetEnabled(false);

            var fittingType = new PropertyField(serializedObject.FindProperty("FittingType"));
            fittingType.RegisterCallback<ChangeEvent<string>>(x => { UpdateFittingTypeFields(m_Component.FittingType); m_Component.GenerateMesh(); });

            var countElement = new PropertyField(serializedObject.FindProperty("Count"));
            countElement.RegisterCallback<ChangeEvent<int>>(x => { m_Component.GenerateMesh(); });
            m_CountElement = countElement;

            var meshFitting = new BindingWrapper<float>(new SliderWithField("Mesh Fitting", 0f, 1f), "MeshFitting");
            meshFitting.RegisterValueChangedCallback(x => { m_Component.GenerateMesh(); } );
            m_MeshFittingElement = meshFitting;

            UpdateFittingTypeFields(m_Component.FittingType);

            var materialSetupEditor = new MaterialSetupEditor(this);

            var pathMeshDropdowns = new SubmeshesDropdowns(materialSetupEditor.SubmeshNames, materialSetupEditor.SubmeshIndices, serializedObject.FindProperty("m_PathMeshData").FindPropertyRelative("m_RemappedSubmeshIndices"));
            pathMeshDropdowns.OnAnyDropdownChanged += delegate { m_Component.PathMeshData.SetDirty(); m_Component.GenerateMesh(); };
            pathMeshField.OnMeshChanged += delegate { pathMeshDropdowns.ValidateDropdowns(); };

            var startMeshDropdowns = new SubmeshesDropdowns(materialSetupEditor.SubmeshNames, materialSetupEditor.SubmeshIndices, serializedObject.FindProperty("m_StartMeshData").FindPropertyRelative("m_IntermediateMesh").FindPropertyRelative("m_RemappedSubmeshIndices"));
            startMeshDropdowns.OnAnyDropdownChanged += delegate { m_Component.StartMeshData.IntermediateMesh.SetDirty(); m_Component.GenerateMesh(); };
            startMeshEditor.Content.Add(startMeshDropdowns);
            startMeshEditor.IntermediateMeshEditor.OnMeshChanged += delegate { startMeshDropdowns.ValidateDropdowns(); };

            var endMeshDropdowns = new SubmeshesDropdowns(materialSetupEditor.SubmeshNames, materialSetupEditor.SubmeshIndices, serializedObject.FindProperty("m_EndMeshData").FindPropertyRelative("m_IntermediateMesh").FindPropertyRelative("m_RemappedSubmeshIndices"));
            endMeshDropdowns.OnAnyDropdownChanged += delegate { m_Component.EndMeshData.IntermediateMesh.SetDirty(); m_Component.GenerateMesh(); };
            endMeshEditor.Content.Add(endMeshDropdowns);
            endMeshEditor.IntermediateMeshEditor.OnMeshChanged += delegate { endMeshDropdowns.ValidateDropdowns(); };

            materialSetupEditor.OnSubmeshesChanged += delegate { pathMeshDropdowns.ValidateDropdowns(); startMeshDropdowns.ValidateDropdowns(); endMeshDropdowns.ValidateDropdowns(); };

            m_Root.Add(pathMeshField);
            m_Root.Add(fittingType);
            m_Root.Add(m_MeshFittingElement);
            m_Root.Add(m_CountElement);
            m_Root.Add(pathMeshDropdowns);
            m_Root.Add(generatedMeshField);

            m_Root.Add(startMeshEditor);
            m_Root.Add(endMeshEditor);

            m_Root.Add(materialSetupEditor);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.yellowpaper.easybezier/USS/PathMeshRenderer.uss");
            m_Root.styleSheets.Add(styleSheet);

            return m_Root;
        }

        private void IntermediateMeshChangedCallback(object sender, EventArgs e)
        {
            m_Component.GenerateMesh();
        }

        private void UpdateFittingTypeFields(FittingType in_FittingType)
        {
            m_CountElement.style.display = in_FittingType == FittingType.Count ? DisplayStyle.Flex : DisplayStyle.None;
            m_MeshFittingElement.style.display = in_FittingType == FittingType.Length ? DisplayStyle.Flex : DisplayStyle.None;
        }

        class MaterialSetupEditor : VisualElement
        {
            public List<string> SubmeshNames { get; private set; } = new List<string>();
            public List<int> SubmeshIndices { get; private set; } = new List<int>();

            public PathMeshEditor ParentEditor { get; private set; }
            public EditableList List { get; private set; }
            public SerializedProperty SubmeshNamesProp { get; private set; }

            public event EventHandler OnSubmeshesChanged;

            public MaterialSetupEditor(PathMeshEditor in_ParentEditor) : base()
            {
                ParentEditor = in_ParentEditor;
                SubmeshNamesProp = ParentEditor.serializedObject.FindProperty("m_SubmeshNames");
                List = new EditableList("Submeshes", SubmeshNamesProp, CreateItem, false);
                List.CanDrag = false;
                List.BindProperty(SubmeshNamesProp);
                List.OnAddCallback += HandleOnItemAdded;
                List.OnRemoveCallback += HandleOnItemRemoved;
                CheckCanAddAndRemove();
                UpdateSubmeshData();

                Add(List);

                //CreateDropdowns(in_ParentEditor.serializedObject.FindProperty("m_PathMeshData").FindPropertyRelative("m_RemappedSubmeshIndices"));
                //CreateDropdowns(in_ParentEditor.serializedObject.FindProperty("m_StartMeshData").FindPropertyRelative("m_IntermediateMesh").FindPropertyRelative("m_RemappedSubmeshIndices"));
                //CreateDropdowns(in_ParentEditor.serializedObject.FindProperty("m_EndMeshData").FindPropertyRelative("m_IntermediateMesh").FindPropertyRelative("m_RemappedSubmeshIndices"));
            }

            private VisualElement CreateItem(EditableList in_List, int in_Index)
            {
                var textField = new TextField();
                textField.value = SubmeshNamesProp.GetArrayElementAtIndex(in_Index).stringValue;
                textField.RegisterCallback<BlurEvent>((e) => {
                    SubmeshNamesProp.GetArrayElementAtIndex(in_Index).stringValue = textField.value;
                    EnsureSubmeshNameUniqueness(in_Index);
                    ParentEditor.serializedObject.ApplyModifiedProperties();
                    in_List.Refresh();
                    UpdateSubmeshData();
                    OnSubmeshesChanged?.Invoke(this, null);
                });
                return textField;
            }

            private void HandleOnItemAdded(EditableList in_List, int in_Index)
            {
                if (SubmeshNamesProp.arraySize == 1)
                    SubmeshNamesProp.GetArrayElementAtIndex(in_Index).stringValue = PathMesh.DefaultSubmeshName;
                else
                    EnsureSubmeshNameUniqueness(in_Index);

                CheckCanAddAndRemove();
                UpdateSubmeshData();
                OnSubmeshesChanged?.Invoke(this, null);
            }

            private void HandleOnItemRemoved(EditableList in_List, int in_Index)
            {
                CheckCanAddAndRemove();
                UpdateSubmeshData();
                OnSubmeshesChanged?.Invoke(this, null);
            }

            private void CheckCanAddAndRemove()
            {
                List.CanAdd = SubmeshNamesProp.arraySize < 8;
                List.CanRemove = SubmeshNamesProp.arraySize != 1;
            }

            private void EnsureSubmeshNameUniqueness(int in_Index)
            {
                if (SubmeshNamesProp.arraySize <= 1)
                    return;

                var prop = SubmeshNamesProp.GetArrayElementAtIndex(in_Index);
                string originalName = prop.stringValue;
                List<string> names = new List<string>(SubmeshNamesProp.arraySize - 1);

                for (int i = 0; i < SubmeshNamesProp.arraySize; i++)
                    if (i != in_Index)
                        names.Add(SubmeshNamesProp.GetArrayElementAtIndex(i).stringValue);

                prop.stringValue = ObjectNames.GetUniqueName(names.ToArray(), originalName);
            }

            private void UpdateSubmeshData()
            {
                SubmeshNames.Clear();
                SubmeshIndices.Clear();
                int i = 0;
                foreach (SerializedProperty prop in SubmeshNamesProp)
                {
                    SubmeshIndices.Add(i++);
                    SubmeshNames.Add(prop.stringValue);
                }
            }
        }

        class SubmeshesDropdowns : VisualElement
        {
            private List<BindingWrapper<int>> m_Dropdowns = new List<BindingWrapper<int>>();

            public SerializedProperty Property { get; private set; }
            public List<string> SubmeshNames { get; set; }
            public List<int> SubmeshIndices { get; set; }
            public IReadOnlyList<BindingWrapper<int>> Dropdowns { get => m_Dropdowns; }

            public event EventHandler OnAnyDropdownChanged;

            public SubmeshesDropdowns(List<string> in_SubmeshNames, List<int> in_SubmeshIndices, SerializedProperty in_Property)
            {
                Property = in_Property;
                SubmeshNames = in_SubmeshNames;
                SubmeshIndices = in_SubmeshIndices;

                CreateDropdowns();
            }

            private void CreateDropdowns()
            {
                for (int i = 0; i < Property.arraySize; i++)
                {
                    SerializedProperty prop = Property.GetArrayElementAtIndex(i);
                    var dropdown = new PopupField<int>("Original Submesh " + i, SubmeshIndices, prop.intValue, (x) => { return SubmeshNames[x]; }, (x) => { return SubmeshNames[x]; });
                    var wrapper = new BindingWrapper<int>(dropdown, prop.propertyPath);
                    wrapper.RegisterValueChangedCallback(delegate { OnAnyDropdownChanged?.Invoke(this, null); });
                    m_Dropdowns.Add(wrapper);
                    Add(wrapper);
                }
            }

            public void ValidateDropdowns()
            {
                foreach (var dropdown in m_Dropdowns)
                    dropdown.RemoveFromHierarchy();

                m_Dropdowns.Clear();

                foreach (SerializedProperty prop in Property)
                {
                    if (prop.intValue < 0 || prop.intValue >= SubmeshIndices.Count)
                        prop.intValue = 0;
                }

                CreateDropdowns();
            }
        }

        public class CustomCapMeshEditor : VisualElement
        {
            SerializedProperty m_Property;
            VisualElement m_Header;
            PathMesh.CapMesh m_Target;

            public VisualElement Content { get; private set; }

            public CustomIntermediateMeshEditor IntermediateMeshEditor { get; private set; }

            public event EventHandler OnChanged;

            public CustomCapMeshEditor(string in_Name, SerializedProperty in_Property)
            {
                m_Property = in_Property;
                m_Header = new VisualElement();
                Content = new VisualElement();

                m_Target = in_Property.GetObject<PathMesh.CapMesh>();

                var isActiveProp = in_Property.FindPropertyRelative("m_IsActive");
                var toggle = new PropertyField(isActiveProp, in_Name);
                toggle.RegisterCallback<ChangeEvent<bool>>(evt => { Content.SetEnabled(evt.newValue); IsActiveChanged(evt); });
                Content.SetEnabled(isActiveProp.boolValue);

                Foldout foldout = new Foldout();
                foldout.value = BezierEditorUtility.Booleans[in_Property.propertyPath];
                foldout.RegisterValueChangedCallback(x => { BezierEditorUtility.Booleans[in_Property.propertyPath] = x.newValue; ShowContent(x.newValue); });
                ShowContent(foldout.value);

                m_Header.Add(foldout);
                m_Header.Add(toggle);

                IntermediateMeshEditor = new CustomIntermediateMeshEditor(in_Property.FindPropertyRelative("m_IntermediateMesh"));
                IntermediateMeshEditor.OnChanged += (x, y) => { OnChanged?.Invoke(this, y); };
                Content.Add(IntermediateMeshEditor);
                var enterPercentSlider = new BindingWrapper<float>(new SliderWithField("Enter Percent", 0f, 1f), "m_EnterPercent");
                enterPercentSlider.RegisterValueChangedCallback(EnterPercentChanged);
                Content.Add(enterPercentSlider);

                AddToClassList("use-mesh");
                m_Header.AddToClassList("use-mesh-header");
                Content.AddToClassList("use-mesh-content");
                Add(m_Header);
                Add(Content);
            }

            private void EnterPercentChanged(ChangeEvent<float> evt)
            {
                Undo.RecordObject(m_Property.serializedObject.targetObject, "Enter Percent");
                m_Target.EnterPercent = evt.newValue;
                HandleOnChanged();
            }

            private void IsActiveChanged(ChangeEvent<bool> evt)
            {
                Undo.RecordObject(m_Property.serializedObject.targetObject, "Is Active");
                m_Target.IsActive = evt.newValue;
                HandleOnChanged();
            }

            private void ShowContent(bool in_Show)
            {
                Content.style.display = in_Show == true ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void HandleOnChanged()
            {
                OnChanged?.Invoke(this, new EventArgs());
            }
        }

        public class CustomIntermediateMeshEditor : VisualElement
        {
            private IntermediateMesh m_Target;
            private SerializedProperty m_Property;

            public event EventHandler OnChanged;
            public event EventHandler OnMeshChanged;

            public CustomIntermediateMeshEditor(SerializedProperty in_Property) : base()
            {
                m_Property = in_Property;
                m_Target = in_Property.GetObject<IntermediateMesh>();

                var originalMeshField = new ObjectField("Mesh");
                originalMeshField.objectType = typeof(Mesh);
                var meshProp = in_Property.FindPropertyRelative("m_Mesh");
                originalMeshField.value = meshProp.objectReferenceValue;
                originalMeshField.RegisterValueChangedCallback((x) => { meshProp.objectReferenceValue = x.newValue; m_Property.serializedObject.ApplyModifiedProperties(); m_Target.CheckIndices(); m_Property.serializedObject.UpdateIfRequiredOrScript(); FieldChanged(); OnMeshChanged?.Invoke(this, null); });

                var axisField = new PropertyField(in_Property.FindPropertyRelative("m_ForwardAxis"));
                axisField.RegisterCallback<ChangeEvent<string>>(x => { FieldChanged(); });

                var flipAxisField = new PropertyField(in_Property.FindPropertyRelative("m_FlipMesh"));
                flipAxisField.RegisterCallback<ChangeEvent<bool>>(delegate { FieldChanged(); });

                var container = new VisualElement();
                container.Add(axisField);
                container.Add(flipAxisField);

                Add(originalMeshField);
                Add(container);
            }

            private void FieldChanged()
            {
                m_Target.SetDirty();
                OnChanged?.Invoke(this, new EventArgs());
            }
        }
    }
}