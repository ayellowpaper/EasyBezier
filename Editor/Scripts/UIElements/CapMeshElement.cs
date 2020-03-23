using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System;
using EasyBezier.UIElements;

namespace EasyBezier
{
    public class CapMeshElement : VisualElement
    {
        private static int s_LastGeneratedID = 0;
        public const string s_UssToggleClass = "cap-mesh-element-toggle-";

        SerializedProperty m_Property;
        PathMesh.CapMesh m_Target;

        public string UssToggleClass { get; private set; }

        public IntermediateMeshElement IntermediateMeshElement { get; private set; }

        public event EventHandler OnChanged;

        public CapMeshElement(SerializedProperty in_Property)
        {
            UssToggleClass = s_UssToggleClass + s_LastGeneratedID;
            s_LastGeneratedID++;

            m_Property = in_Property;

            m_Target = in_Property.GetObject<PathMesh.CapMesh>();

            var isActiveProp = in_Property.FindPropertyRelative("m_IsActive");
            var isActiveToggle = new PropertyField(isActiveProp, "Is Active");
            isActiveToggle.RegisterCallback<ChangeEvent<bool>>(evt => { EnableContent(evt.newValue); IsActiveChanged(evt); });
            Add(isActiveToggle);

            IntermediateMeshElement = new IntermediateMeshElement(in_Property.FindPropertyRelative("m_IntermediateMesh"));
            IntermediateMeshElement.OnChanged += (x, y) => { OnChanged?.Invoke(this, y); };
            IntermediateMeshElement.AddToClassList(UssToggleClass);
            Add(IntermediateMeshElement);

            var seperator = new VisualElement();
            seperator.AddToClassList("eb-seperator");
            Add(seperator);

            var enterPercentSlider = new BindingWrapper<float>(new SliderWithField("Enter Percent", 0f, 1f), "m_EnterPercent");
            enterPercentSlider.Child.SetValueWithoutNotify(in_Property.FindPropertyRelative("m_EnterPercent").floatValue);
            enterPercentSlider.RegisterValueChangedCallback(EnterPercentChanged);
            enterPercentSlider.AddToClassList(UssToggleClass);
            Add(enterPercentSlider);

            EnableContent(isActiveProp.boolValue);
        }

        private void EnterPercentChanged(ChangeEvent<float> evt)
        {
            Undo.RecordObject(m_Property.serializedObject.targetObject, "Enter Percent");
            m_Target.EnterPercent = evt.newValue;
            PrefabUtility.RecordPrefabInstancePropertyModifications(m_Property.serializedObject.targetObject);
            HandleOnChanged();
        }

        private void IsActiveChanged(ChangeEvent<bool> evt)
        {
            Undo.RecordObject(m_Property.serializedObject.targetObject, "Is Active");
            m_Target.IsActive = evt.newValue;
            PrefabUtility.RecordPrefabInstancePropertyModifications(m_Property.serializedObject.targetObject);
            HandleOnChanged();
        }

        private void EnableContent(bool in_Show)
        {
            this.Query(className: UssToggleClass).ForEach(x => x.SetEnabled(in_Show));
        }

        private void HandleOnChanged()
        {
            OnChanged?.Invoke(this, new EventArgs());
        }
    }
}