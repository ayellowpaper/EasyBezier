using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyBezier
{
    public class BindingWrapper<TValueType> : VisualElement, INotifyValueChanged<TValueType>
    {
        public readonly BaseField<TValueType> Child;

        public BindingWrapper(BaseField<TValueType> in_Field, string in_BindingPath)
        {
            Child = in_Field;
            Child.bindingPath = in_BindingPath;
            this.Add(Child);
        }

        public TValueType value { get => Child.value; set => Child.value = value; }

        public void SetValueWithoutNotify(TValueType newValue)
        {
            Child.SetValueWithoutNotify(newValue);
        }
    }
}
