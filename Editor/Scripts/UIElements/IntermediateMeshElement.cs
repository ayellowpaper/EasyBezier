using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System;

namespace EasyBezier
{
    public class IntermediateMeshElement : VisualElement
    {
        private IntermediateMesh m_Target;
        private SerializedProperty m_Property;

        public event EventHandler OnChanged;
        public event EventHandler OnMeshChanged;

        public IntermediateMeshElement(SerializedProperty in_Property) : base()
        {
            m_Property = in_Property;
            m_Target = in_Property.GetObject<IntermediateMesh>();

            var originalMeshField = new PropertyField(in_Property.FindPropertyRelative("m_Mesh"));
            originalMeshField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => { m_Property.serializedObject.ApplyModifiedProperties(); m_Target.CheckSubmeshIndices(); m_Property.serializedObject.UpdateIfRequiredOrScript(); FieldChanged(); OnMeshChanged?.Invoke(this, null); });

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
            OnChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}