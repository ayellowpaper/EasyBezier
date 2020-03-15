using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditorInternal;

namespace EasyBezier
{
    /// <summary>
    /// The settings provider responsible for drawing the settings.
    /// </summary>
    class EasyBezierSettingsProvider : SettingsProvider
    {
        public const string SETTINGS_PATH = "Preferences/Easy Bezier";

        private SerializedObject m_Settings;
        private Editor m_Editor;

        public EasyBezierSettingsProvider(string path, SettingsScope scopes = SettingsScope.Project, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        /// <summary>
        /// Initialize the settings in OnActivate
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_Settings = EasyBezierSettings.GetSerializedObject();
            m_Editor = Editor.CreateEditor(m_Settings.targetObject);
        }

        /// <summary>
        /// Add the entry to the settings.
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettings()
        {
            var provider = new EasyBezierSettingsProvider(SETTINGS_PATH, SettingsScope.User);
            return provider;
        }

        /// <summary>
        /// Draw all the nice stuff in UI
        /// </summary>
        /// <param name="searchContext"></param>
        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            EditorGUILayout.Space();
            EditorGUI.indentLevel = 1;

            m_Editor.OnInspectorGUI();
        }
    }
}