using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace EasyBezier.UIElements
{
    public class SliderWithField : BaseField<float>
    {
        public new class UxmlFactory : UxmlFactory<SliderWithField, UxmlTraits> { }

        public new class UxmlTraits : BaseField<float>.UxmlTraits
        {
            private UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription
            {
                name = "low-value"
            };

            private UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription
            {
                name = "high-value",
                defaultValue = 10f
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var sliderWithField = (SliderWithField)ve;
                sliderWithField.Slider.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                sliderWithField.Slider.highValue = m_HighValue.GetValueFromBag(bag, cc);
                sliderWithField.FloatField.MinValue = sliderWithField.Slider.lowValue;
                sliderWithField.FloatField.MaxValue = sliderWithField.Slider.highValue;
            }
        }

        public Slider Slider { get; private set; }
        public ConstrainedFloatField FloatField { get; private set; }

        public override float value {
            get => base.value;
            set {
                SetFieldValueWithoutNotify(value);
                SetSliderValueWithoutNotify(value);
                base.value = value;
            }
        }

        public SliderWithField() : this(null, 0f, 1f) { }

        public SliderWithField(string in_Label, float in_Start, float in_End) : base(in_Label, new Slider(in_Start, in_End, SliderDirection.Horizontal))
        {
            EventCallback<ChangeEvent<float>> callback = (evt) => { value = evt.newValue; };
            // TODO: this feels dirty :(
            Slider = this.Query<Slider>(className: inputUssClassName);
            //Slider.Query(className: Slider.inputUssClassName).First().style.marginRight = 4;
            Slider.RegisterValueChangedCallback(callback);
            FloatField = new ConstrainedFloatField(in_Start, in_End);
            FloatField.RegisterValueChangedCallback(callback);

            Slider.style.flexBasis = 0;
            Slider.style.flexGrow = 3;
            Slider.style.flexShrink = 0;
            FloatField.style.flexBasis = new Length(10, LengthUnit.Pixel);
            FloatField.style.flexGrow = 1;
            FloatField.style.flexShrink = 0;

            Slider.RemoveFromClassList(BaseField<float>.ussClassName);
            FloatField.RemoveFromClassList(BaseField<float>.ussClassName);
            this.Add(FloatField);
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            SetFieldValueWithoutNotify(newValue);
            SetSliderValueWithoutNotify(newValue);
            base.SetValueWithoutNotify(newValue);
        }

        private void SetFieldValueWithoutNotify(float in_NewValue)
        {
            FloatField.SetValueWithoutNotify(in_NewValue);
        }

        private void SetSliderValueWithoutNotify(float in_NewValue)
        {
            // I need to update the slider because it won't get updated properly
            System.Reflection.MethodInfo mi = Slider.GetType().BaseType.GetMethod("UpdateDragElementPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, System.Reflection.CallingConventions.Any, new Type[0], null);
            if (mi != null)
            {
                Slider.SetValueWithoutNotify(in_NewValue);
                mi.Invoke(Slider, null);
            }
            else
                Slider.value = in_NewValue;

        }
    }
}