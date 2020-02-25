using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyBezier
{
    /// <summary>
    /// Contains all settings for Better Editor.
    /// </summary>
    public class EasyBezierSettings : ScriptableObject
    {
        public static EasyBezierSettings Instance {
            get {
                g_Instance = Resources.Load<EasyBezierSettings>("EasyBezierSettings");
                return g_Instance;
            }
        }

        private static EasyBezierSettings g_Instance;

        public CoreSettings Settings;

        public void ResetToDefault()
        {
            Settings = new CoreSettings();
        }

#if UNITY_EDITOR
        public static SerializedObject GetSerializedObject()
        {
            return new SerializedObject(Instance);
        }

        public bool ShouldHideCurveType(CurveType in_CurveType)
        {
            return Settings.HideAutoAndLinear && (in_CurveType == CurveType.Auto || in_CurveType == CurveType.Linear);
        }

        public float GetPointHandleSize()
        {
            return Settings.HandleSize * Settings.PointHandleSize * 0.06f;
        }

        public float GetPointHandleSize(Vector3 in_Position)
        {
            return HandleUtility.GetHandleSize(in_Position) * GetPointHandleSize();
        }

        public float GetTangentHandleSize()
        {
            return Settings.HandleSize * Settings.TangentHandleSize * 0.06f;
        }

        public float GetTangentHandleSize(Vector3 in_Position)
        {
            return HandleUtility.GetHandleSize(in_Position) * GetTangentHandleSize();
        }
#endif
    }

    [System.Serializable]
    public class CoreSettings
    {
        public Color BezierColor = new Color(0, 1f, 0.88f);
#if UNITY_EDITOR
        public Color ControlPointColor = new Color(0, 1f, 0.88f);
        public Color TangentPointColor = new Color(0.31f, 1f, 0.023f);
        public Color TangentLineColor = new Color(0, 1f, 0.88f);
        [Range(1f, 6f)]
        public float PathThickness = 1.3f;
        [Range(0.5f, 2f)]
        public float HandleSize = 1f;
        [Range(0.5f, 2f)]
        public float PointHandleSize = 1f;
        [Range(0.5f, 2f)]
        public float TangentHandleSize = 1f;
        [Range(0.5f, 2f)]
        public float SelectedHandleSize = 1.3f;
        [Range(0.01f, 5f)]
        public float PathScaleGizmoSize = 0.5f;
        [Range(0.01f, 5f)]
        public float UpVectorLength = 0.5f;
        public bool HideAutoAndLinear = false;
        public DisplayMode UpVectorDisplayMode = DisplayMode.WhenEditing;
        public SceneViewGuiAnchor SceneViewGuiAnchor = SceneViewGuiAnchor.TopLeft;
        public Vector2 SceneViewGuiPadding = new Vector2(5f, 5f);
#endif
    }

#if UNITY_EDITOR
    public enum SceneViewGuiAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum DisplayMode
    {
        Always,
        Selected,
        WhenEditing,
        Never
    }

    public static class DisplayModeExtension
    {
        public static bool IsVisible(this DisplayMode in_DisplayMode, bool in_IsSelected, bool in_IsEditing)
        {
            switch (in_DisplayMode)
            {
                case DisplayMode.Always:
                    return true;
                case DisplayMode.Selected:
                    return in_IsSelected;
                case DisplayMode.WhenEditing:
                    return in_IsSelected && in_IsEditing;
                case DisplayMode.Never:
                    return false;
            }

            return false;
        }
    }
#endif
}