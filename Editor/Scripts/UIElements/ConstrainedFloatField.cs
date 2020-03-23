using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace EasyBezier
{
    public class ConstrainedFloatField : FloatField
    {
        public new class UxmlFactory : UxmlFactory<ConstrainedFloatField, UxmlTraits> { }

        public new class UxmlTraits : FloatField.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value" };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = 10f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var constrainedFloatField = (ConstrainedFloatField)ve;
                base.Init(ve, bag, cc);
                constrainedFloatField.MinValue = m_LowValue.GetValueFromBag(bag, cc);
                constrainedFloatField.MaxValue = m_HighValue.GetValueFromBag(bag, cc);
            }
        }

        private float m_MinValue;
        private float m_MaxValue;

        public float MinValue {
            get => m_MinValue;
            set {
                m_MinValue = value;
                if (value < m_MinValue)
                    this.value = m_MinValue;
            }
        }

        public float MaxValue {
            get => m_MaxValue;
            set {
                m_MaxValue = value;
                if (value > m_MaxValue)
                    this.value = m_MaxValue;
            }
        }

        public override float value {
            get => base.value;
            set {
                base.value = Mathf.Clamp(value, m_MinValue, m_MaxValue);
            }
        }

        public ConstrainedFloatField() : this(null, float.MinValue, float.MaxValue)
        {
        }

        public ConstrainedFloatField(float in_MinValue, float in_MaxValue) : this(null, in_MinValue, in_MaxValue)
        {
        }

        public ConstrainedFloatField(string in_Label) : this(in_Label, float.MinValue, float.MaxValue)
        {
        }

        public ConstrainedFloatField(string in_Label, float in_MinValue, float in_MaxValue) : base(in_Label)
        {
            m_MinValue = in_MinValue;
            m_MaxValue = in_MaxValue;
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            base.SetValueWithoutNotify(Mathf.Clamp(newValue, m_MinValue, m_MaxValue));
        }
    }
}