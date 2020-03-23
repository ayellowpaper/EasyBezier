using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace EasyBezier
{
    public static class CustomBackingFieldBindingExtensions
    {
        public static void BindBackingField<T>(this BaseField<T> in_BoundElement, Func<T> in_Getter, Action<T> in_Setter, UnityEngine.Object in_UndoObject = null, string in_UndoString = "Inspector")
        {
            in_BoundElement.binding = new CustomBackingFieldBinding<T>(in_BoundElement, in_Getter, in_Setter, in_UndoObject, in_UndoString);
        }
    }

    public class CustomBackingFieldBinding<T> : IBinding
    {

        private BaseField<T> m_BoundElement;
        private Func<T> m_Getter;
        private Action<T> m_Setter;
        private UnityEngine.Object m_UndoObject;
        private string m_UndoString;

        private T m_PreviousValue;

        public CustomBackingFieldBinding(BaseField<T> in_BoundElement, Func<T> in_Getter, Action<T> in_Setter, UnityEngine.Object in_UndoObject = null, string in_UndoString = "Inspector")
        {
            m_BoundElement = in_BoundElement;
            m_Getter = in_Getter;
            m_Setter = in_Setter;
            m_PreviousValue = in_Getter();
            m_UndoObject = in_UndoObject;
            m_UndoString = in_UndoString;

            if (in_BoundElement is EnumField)
            {
                (in_BoundElement as EnumField).Init(m_PreviousValue as Enum);
            }
            else
            {
                in_BoundElement.SetValueWithoutNotify(m_PreviousValue);
            }
        }

        public void PreUpdate()
        {
        }

        public void Release()
        {
        }

        public void Update()
        {
            T newBackingValue = m_Getter();

            if (!EqualityComparer<T>.Default.Equals(newBackingValue, m_PreviousValue))
            {
                //Debug.Log("backing field changed " + newBackingValue);
                m_BoundElement.SetValueWithoutNotify(newBackingValue);
                m_PreviousValue = newBackingValue;
            }
            else if (!EqualityComparer<T>.Default.Equals(m_BoundElement.value, m_PreviousValue))
            {
                bool recordUndo = m_UndoObject != null && !string.IsNullOrEmpty(m_UndoString);
                if (recordUndo)
                    Undo.RecordObject(m_UndoObject, m_UndoString);
                m_Setter(m_BoundElement.value);
                if (recordUndo)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(m_UndoObject);

                m_PreviousValue = m_BoundElement.value;
            }
        }
    }
}
