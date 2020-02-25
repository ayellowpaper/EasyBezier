using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyBezier
{
    public static class EditorGUILayoutUtility
    {
        public static readonly Color DEFAULT_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Vector2 DEFAULT_LINE_MARGIN = new Vector2(2f, 2f);

        public const float DEFAULT_LINE_HEIGHT = 1f;

        public static void HorizontalLine(Color color, float height, Vector2 margin, params GUILayoutOption[] options)
        {
            Rect reservedSpace = EditorGUILayout.GetControlRect(false, height + margin.y * 2, options);
            reservedSpace.y += margin.y;
            reservedSpace.height = height;
            EditorGUI.DrawRect(reservedSpace, color);
        }

        public static void HorizontalLine(Color color, float height, params GUILayoutOption[] options) => HorizontalLine(color, height, DEFAULT_LINE_MARGIN, options);
        public static void HorizontalLine(Color color, Vector2 margin, params GUILayoutOption[] options) => HorizontalLine(color, DEFAULT_LINE_HEIGHT, margin, options);
        public static void HorizontalLine(float height, Vector2 margin, params GUILayoutOption[] options) => HorizontalLine(DEFAULT_COLOR, height, margin, options);

        public static void HorizontalLine(Color color, params GUILayoutOption[] options) => HorizontalLine(color, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN, options);
        public static void HorizontalLine(float height, params GUILayoutOption[] options) => HorizontalLine(DEFAULT_COLOR, height, DEFAULT_LINE_MARGIN, options);
        public static void HorizontalLine(Vector2 margin, params GUILayoutOption[] options) => HorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, margin, options);

        public static void HorizontalLine(params GUILayoutOption[] options) => HorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN, options);
    }
}