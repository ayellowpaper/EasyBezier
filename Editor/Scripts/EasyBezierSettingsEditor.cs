using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyBezier
{
    [CustomEditor(typeof(EasyBezierSettings))]
    public class EasyBezierSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty coreProp = serializedObject.FindProperty("Settings");
            foreach (SerializedProperty prop in coreProp)
                EditorGUILayout.PropertyField(prop, false, GUILayout.MaxWidth(460));

            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("BezierColor"), GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("ControlPointColor"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("TangentPointColor"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("TangentLineColor"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("PathThickness"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("HandleSize"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("PointHandleSize"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("TangentHandleSize"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("SelectedHandleSize"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("HideAutoAndLinear"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("ShowUpVectors"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("SceneViewGuiAnchor"), true, GUILayout.MaxWidth(460));
            //EditorGUILayout.PropertyField(coreProp.FindPropertyRelative("SceneViewGuiPadding"), true, GUILayout.MaxWidth(460));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(160)));
            if (GUI.Button(rect, "Reset Settings"))
            {
                Undo.RecordObject(serializedObject.targetObject, "Reset Settings");
                (serializedObject.targetObject as EasyBezierSettings).ResetToDefault();
            }
        }
    }
}