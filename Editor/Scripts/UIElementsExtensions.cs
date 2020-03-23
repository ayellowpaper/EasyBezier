using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

namespace EasyBezier
{
    public static class UIElementsExtensions
    {
        public static DisplayStyle FromBool(bool in_ShouldDisplay)
        {
            return in_ShouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static SerializedProperty GetSerializedProperty(this IBindable in_Bindable, SerializedObject in_SerializedObject)
        {
            return in_SerializedObject.FindProperty(in_Bindable.bindingPath);
        }

        public static void AddPropertyManipulator(this VisualElement in_VisualElement, SerializedProperty in_Property, params SerializedProperty[] in_Properties)
        {
            List<SerializedProperty> props = in_Properties.ToList();
            props.Add(in_Property);

            in_VisualElement.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (in_Property.serializedObject.targetObjects.Length == 1 && in_Property.isInstantiatedPrefab && in_Property.prefabOverride)
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(in_Property.serializedObject.targetObject);
                    evt.menu.AppendAction($"Apply to Prefab '{prefab.name}'", action => ApplyPropertyOverrides(props));
                    evt.menu.AppendAction("Revert", action => RevertProperties(props));
                }
            }));
        }

        private static void ApplyPropertyOverrides(IList<SerializedProperty> in_Properties)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(in_Properties[0].serializedObject.targetObject);
            foreach (var property in in_Properties)
            {
                PrefabUtility.ApplyPropertyOverride(property, AssetDatabase.GetAssetPath(prefab), InteractionMode.UserAction);
            }
        }

        private static void RevertProperties(IList<SerializedProperty> in_Properties)
        {
            foreach (var property in in_Properties)
            {
                PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            }
        }
    }
}
