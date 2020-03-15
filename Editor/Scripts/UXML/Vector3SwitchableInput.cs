using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;

namespace EasyBezier.UIElements
{
    public class Vector3SwitchableInput : BaseField<Vector3>
    {
        public new class UxmlFactory : UxmlFactory<Vector3SwitchableInput, UxmlTraits> { }

        public new class UxmlTraits : BaseField<Vector3>.UxmlTraits
        {
            UxmlEnumAttributeDescription<VectorInputType> m_InputType = new UxmlEnumAttributeDescription<VectorInputType>
            {
                name = "input-type",
                defaultValue = VectorInputType.Vector3
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var field = (Vector3SwitchableInput)ve;
                field.InputType = m_InputType.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }

        private VectorInputType m_InputType = VectorInputType.Vector3;
        public VectorInputType InputType {
            get => m_InputType;
            set {
                m_InputType = value;
                UpdateInput();
            }
        }

        public override Vector3 value {
            get => base.value;
            set {
                SetValuesWithoutNotify(value);
                base.value = value;
            }
        }

        private FloatField m_FloatField;
        private Vector2Field m_Vector2Field;
        private Vector3Field m_Vector3Field;

        public Vector3SwitchableInput() : this(null) { }

        public Vector3SwitchableInput(string label) : base(label, null)
        {
            this.AddToClassList("unity-property-field");

            m_FloatField = new FloatField();
            m_FloatField.AddToClassList(BaseField<float>.inputUssClassName);
            m_FloatField.RemoveFromClassList(BaseField<float>.ussClassName);
            m_FloatField.RegisterValueChangedCallback(evt => this.value = new Vector3(evt.newValue, evt.newValue, evt.newValue));

            m_Vector2Field = new Vector2Field();
            m_Vector2Field.AddToClassList(BaseField<Vector2>.inputUssClassName);
            m_Vector2Field.RemoveFromClassList(BaseField<Vector2>.ussClassName);
            m_Vector2Field.RegisterValueChangedCallback(evt => this.value = new Vector3(evt.newValue.x, evt.newValue.y, 1f));

            m_Vector3Field = new Vector3Field();
            m_Vector3Field.AddToClassList(BaseField<Vector3>.inputUssClassName);
            m_Vector3Field.RemoveFromClassList(BaseField<Vector3>.ussClassName);
            m_Vector3Field.RegisterValueChangedCallback(evt => this.value = evt.newValue);

            // TODO: this feels dirty :(
            VisualElement visualInput = this.Query(className: inputUssClassName);
            visualInput.hierarchy.Add(m_FloatField);
            visualInput.hierarchy.Add(m_Vector2Field);
            visualInput.hierarchy.Add(m_Vector3Field);

            UpdateInput();
        }

        void UpdateInput()
        {
            m_FloatField.style.display = m_InputType == VectorInputType.Float ? DisplayStyle.Flex : DisplayStyle.None;
            m_Vector2Field.style.display = m_InputType == VectorInputType.Vector2 ? DisplayStyle.Flex : DisplayStyle.None;
            m_Vector3Field.style.display = m_InputType == VectorInputType.Vector3 ? DisplayStyle.Flex : DisplayStyle.None;
            
        }

        public override void SetValueWithoutNotify(Vector3 newValue)
        {
            base.SetValueWithoutNotify(newValue);
            SetValuesWithoutNotify(newValue);
        }

        private void SetValuesWithoutNotify(Vector3 newValue)
        {
            m_FloatField.SetValueWithoutNotify(newValue.x);
            m_Vector2Field.SetValueWithoutNotify(new Vector2(newValue.x, newValue.y));
            m_Vector3Field.SetValueWithoutNotify(newValue);
        }
    }
}