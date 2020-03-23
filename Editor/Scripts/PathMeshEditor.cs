using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using EasyBezier.UIElements;

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
            var pathMeshField = new IntermediateMeshElement(serializedObject.FindProperty("m_PathMeshData"));
            pathMeshField.OnChanged += IntermediateMeshChangedCallback;

            var startMeshProperty = serializedObject.FindProperty("m_StartMeshData");
            var startMeshEditor = new CapMeshElement(startMeshProperty);
            startMeshEditor.OnChanged += IntermediateMeshChangedCallback;

            var endMeshProperty = serializedObject.FindProperty("m_EndMeshData");
            var endMeshEditor = new CapMeshElement(endMeshProperty);
            endMeshEditor.OnChanged += IntermediateMeshChangedCallback;

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
            startMeshEditor.Add(startMeshDropdowns);
            startMeshEditor.IntermediateMeshElement.OnMeshChanged += delegate { startMeshDropdowns.ValidateDropdowns(); };

            var endMeshDropdowns = new SubmeshesDropdowns(materialSetupEditor.SubmeshNames, materialSetupEditor.SubmeshIndices, serializedObject.FindProperty("m_EndMeshData").FindPropertyRelative("m_IntermediateMesh").FindPropertyRelative("m_RemappedSubmeshIndices"));
            endMeshDropdowns.OnAnyDropdownChanged += delegate { m_Component.EndMeshData.IntermediateMesh.SetDirty(); m_Component.GenerateMesh(); };
            endMeshEditor.Add(endMeshDropdowns);
            endMeshEditor.IntermediateMeshElement.OnMeshChanged += delegate { endMeshDropdowns.ValidateDropdowns(); };

            materialSetupEditor.OnSubmeshesChanged += delegate { pathMeshDropdowns.ValidateDropdowns(); startMeshDropdowns.ValidateDropdowns(); endMeshDropdowns.ValidateDropdowns(); };

            var foldoutContainer = new FoldoutContainer("Path");
            m_Root.Add(foldoutContainer);

            var seperator = new VisualElement();
            seperator.AddToClassList("eb-seperator");

            foldoutContainer.Content.Add(pathMeshField);
            foldoutContainer.Content.Add(seperator);
            foldoutContainer.Content.Add(fittingType);
            foldoutContainer.Content.Add(m_MeshFittingElement);
            foldoutContainer.Content.Add(m_CountElement);
            foldoutContainer.Content.Add(pathMeshDropdowns);

            m_Root.Add(new FoldoutContainer("Start", startMeshEditor));
            m_Root.Add(new FoldoutContainer("End", endMeshEditor));

            m_Root.Add(new FoldoutContainer("Materials", materialSetupEditor));
            m_Root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EasyBezierSettings.EditorUSSPath + "/General.uss"));
            m_Root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EasyBezierSettings.EditorUSSPath + "/PathMeshEditor.uss"));

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
    }
}