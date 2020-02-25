using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace EasyBezier
{
    public class SliderWithField : BaseField<float>
    {
        public Slider Slider { get; private set; }
        public FloatField FloatField { get; private set; }

        public override float value {
            get => base.value;
            set {
                SetFieldValueWithoutNotify(value);
                SetSliderValueWithoutNotify(value);
                base.value = value;
            }
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            SetFieldValueWithoutNotify(newValue);
            SetSliderValueWithoutNotify(newValue);
            base.SetValueWithoutNotify(newValue);
        }

        public SliderWithField(string in_Label, float in_Start, float in_End) : base(null, null)
        {
            EventCallback<ChangeEvent<float>> callback = (evt) => { value = evt.newValue; evt.StopPropagation(); };
            Slider = new Slider(in_Label, in_Start, in_End, SliderDirection.Horizontal);
            Slider.style.flexGrow = 1f;
            Slider.Query(className: Slider.inputUssClassName).First().style.marginRight = 4;
            Slider.RegisterValueChangedCallback(callback);
            FloatField = new ConstrainedFloatField(in_Start, in_End);
            FloatField.style.width = 50;
            FloatField.RegisterValueChangedCallback(callback);

            // TODO: this feels dirty :(
            this.Remove(this.Query(className:inputUssClassName));

            this.Add(Slider);
            this.Add(FloatField);
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