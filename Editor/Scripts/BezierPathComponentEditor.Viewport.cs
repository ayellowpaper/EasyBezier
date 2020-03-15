using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.ShortcutManagement;

namespace EasyBezier
{
    [CustomEditor(typeof(BezierPathComponent))]
    public partial class BezierPathComponentEditor : Editor
    {
        public const float SceneViewWindowWidth = 210;
        public const float SceneViewWindowHeight = 26;
        public const float ExtraTopRightPadding = 80f;

        public const float PickDistance = 5f;
        public const float SnapToPathDistance = 0.3f;
        public const KeyCode CreatePointKey = KeyCode.C;

        static BezierPathComponentEditor s_Instance;
        static ToolContent[] s_ToolContents;
        static ToolContent[] s_MetaToolContents;

        public BezierPathComponent Component { get; private set; }
        private Event m_Event;

        private bool m_IsEditing = true;
        private PointSelection m_Hovering;
        private PointSelection m_Selection;
        private bool m_IsAdding;

        private float m_SceneViewHudWidth;
        private float m_SceneViewHudHeight;

        static string[] s_CurveTypeNames;
        static CurveType[] s_CurveTypes;
        static System.Reflection.PropertyInfo s_IsViewToolActive;

        private ManipulationTool m_ActiveTool;
        internal FreeMoveTool m_FreeMoveTool = new FreeMoveTool();
        internal MoveTool m_MoveTool = new MoveTool();
        internal RotationTool m_RotationTool = new RotationTool();
        internal ScaleTool m_ScaleTool = new ScaleTool();
        internal RollTool m_RollTool = new RollTool();
        internal PathScaleTool m_PathScaleTool = new PathScaleTool();

        internal Dictionary<Tool, ManipulationTool> m_ToolMappings;

        public event Action<BezierPathComponentEditor, bool> OnIsEditingChanged;

        class ToolContent
        {
            public ManipulationTool Tool;
            public GUIContent OnContent;
            public GUIContent OffContent;

            public ToolContent(ManipulationTool in_Tool, GUIContent in_OnContent, GUIContent in_OffContent)
            {
                Tool = in_Tool;
                OnContent = in_OnContent;
                OffContent = in_OffContent;
            }

            public GUIContent GetContent(ManipulationTool in_ActiveTool)
            {
                return in_ActiveTool == Tool ? OnContent : OffContent;
            }
        }

        public struct PointSelection
        {
            public int Index;
            public PointType PointType;

            public PointSelection(int in_Index, PointType in_PointType)
            {
                Index = in_Index;
                PointType = in_PointType;
            }
        }

        static BezierPathComponentEditor()
        {
            s_CurveTypeNames = System.Enum.GetNames(typeof(CurveType));
            List<CurveType> curveTypes = new List<CurveType>(s_CurveTypeNames.Length);
            foreach (CurveType curveType in System.Enum.GetValues(typeof(CurveType)))
                curveTypes.Add(curveType);
            s_CurveTypes = curveTypes.ToArray();

            s_IsViewToolActive = typeof(Tools).GetProperty("viewToolActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        }

        private void OnEnable()
        {
            s_Instance = this;
            Component = (BezierPathComponent)target;
            m_Hovering = new PointSelection(-1, PointType.Point);
            m_Selection = new PointSelection(-1, PointType.Point);
            m_ActiveTool = m_MoveTool;
            OnEnableInspector();

            s_ToolContents = new ToolContent[]
            {
                new ToolContent(m_MoveTool, EditorGUIUtility.IconContent("MoveTool On"), EditorGUIUtility.TrIconContent("MoveTool", "Move Tool")),
                new ToolContent(m_RotationTool, EditorGUIUtility.IconContent("RotateTool On"), EditorGUIUtility.TrIconContent("RotateTool", "Rotate Tool")),
                new ToolContent(m_ScaleTool, EditorGUIUtility.IconContent("ScaleTool On"), EditorGUIUtility.TrIconContent("ScaleTool", "Scale Tool")),
            };

            s_MetaToolContents = new ToolContent[]
            {
                new ToolContent(m_RollTool, EditorGUIUtility.IconContent("RotateTool On"), EditorGUIUtility.TrIconContent("RotateTool", "Rotate Tool")),
                new ToolContent(m_PathScaleTool, EditorGUIUtility.IconContent("ScaleTool On"), EditorGUIUtility.TrIconContent("ScaleTool", "Scale Tool")),
            };

            m_ToolMappings = new Dictionary<Tool, ManipulationTool>()
            {
                {Tool.Move, m_MoveTool },
                {Tool.Rotate, m_RotationTool },
                {Tool.Scale, m_ScaleTool },
            };
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        [Shortcut("Easy Bezier/Edit Path Roll", typeof(SceneView), KeyCode.E, ShortcutModifiers.Shift)]
        static void EditPathRollShortcut(ShortcutArguments in_Args)
        {
            s_Instance.SetActiveTool(s_Instance.m_RollTool);
            
        }

        [Shortcut("Easy Bezier/Edit Path Scale", typeof(SceneView), KeyCode.R, ShortcutModifiers.Shift)]
        static void EditPathScaleShortcut(ShortcutArguments in_Args)
        {
            s_Instance.SetActiveTool(s_Instance.m_PathScaleTool);
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawGizmo(BezierPathComponent in_Component, GizmoType in_GizmoType)
        {
            bool isSelected = Selection.activeGameObject == in_Component.gameObject;
            bool isEditingRoll = s_Instance != null ? s_Instance.m_IsEditingRoll : false;
            if (EasyBezierSettings.Instance.Settings.UpVectorDisplayMode.IsVisible(isSelected, isEditingRoll))
                ShowUpVectors(in_Component);
        }

        public static void ShowUpVectors(BezierPathComponent in_Component)
        {
            int iterationCount = in_Component.PointCount * 20;
            for (int i = 0; i <= iterationCount; i++)
            {
                float time = (float)i / iterationCount;
                Vector3 pos = in_Component.GetPositionAtTime(time);
                Vector3 up = in_Component.GetUpVectorAtTime(time);
                Handles.color = Color.red;
                Handles.DrawLine(pos, pos + up * EasyBezierSettings.Instance.Settings.UpVectorLength);
            }
        }

        public static void ShowPathScale(BezierPathComponent in_Component)
        {
            float scaleMultiplier = EasyBezierSettings.Instance.Settings.PathScaleGizmoSize * 0.5f;
            int iterationCount = in_Component.PointCount * 20;
            for (int i = 0; i <= iterationCount; i++)
            {
                float time = (float)i / iterationCount;
                Vector3 pos = in_Component.GetPositionAtTime(time);
                Vector3 forward = in_Component.GetForwardVectorAtTime(time);
                Vector3 scale = in_Component.GetScaleAtTime(time);
                Handles.color = Color.red;
                Handles.DrawWireDisc(pos, forward, scaleMultiplier * scale.x);
            }
        }

        private void OnSceneGUI()
        {
            if (!Component.enabled)
                return;

            m_Event = Event.current;

            UpdateActiveToolByUnityTool();

            if (m_Event.type == EventType.KeyDown && m_Event.keyCode == KeyCode.Tab)
            {
                m_Event.Use();
                IsEditing = !IsEditing;
            }

            DrawSceneViewGUI();
            DrawBezierCurve();
            if (m_IsEditing)
                DrawBezierPoints();

            Tools.hidden = m_IsEditing;

            if (!m_IsEditing)
                return;

            if (m_Selection.Index >= 0)
            {
                if (m_Event.type == EventType.KeyDown && m_Event.keyCode == KeyCode.Delete)
                {
                    if (m_Selection.PointType == PointType.Point)
                    {
                        Undo.RecordObject(Component, UndoStrings.RemovePoint);
                        Component.RemovePointAt(m_Selection.Index);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
                        SelectPointAtIndex(m_Selection.Index - 1 >= 0 ? m_Selection.Index - 1 : 0, PointType.Point);
                    }
                    m_Event.Use();
                }
            }

            if (m_Event.type == EventType.MouseDown && m_Event.button == 1 && m_Hovering.Index != -1)
            {
                m_Event.Use();
                ShowContextMenu(m_Hovering.Index, m_Hovering.PointType);
            }

            if (m_Event.type == EventType.KeyDown && m_Event.keyCode == CreatePointKey)
            {
                m_Event.Use();
                m_IsAdding = true;
            }
            if (m_Event.type == EventType.KeyUp && m_Event.keyCode == CreatePointKey)
            {
                m_Event.Use();
                m_IsAdding = false;
            }

            if (m_IsAdding)
            {
                Handles.color = Color.blue;
                Ray ray = HandleUtility.GUIPointToWorldRay(m_Event.mousePosition);

                bool insert = true;
                float t = Component.FindTimeClosestToLine(ray.origin, ray.direction);
                Vector3 newPoint = Component.GetPositionAtTime(t);
                if (Vector3.Cross(ray.direction, newPoint - ray.origin).magnitude > SnapToPathDistance)
                {
                    Plane plane = new Plane(-SceneView.lastActiveSceneView.camera.transform.forward, Component.GetPositionAtIndex(Component.PointCount - 1));
                    if (plane.Raycast(ray, out float enter))
                    {
                        newPoint = ray.GetPoint(enter);
                        insert = false;
                    }
                }

                Handles.CapFunction capFunction = Handles.DotHandleCap;
                capFunction(-1, newPoint, Quaternion.identity, EasyBezierSettings.Instance.GetPointHandleSize(newPoint), m_Event.type);

                if (m_Event.type == EventType.MouseDown && m_Event.button == 0)
                {
                    Undo.RecordObject(Component, UndoStrings.AddPoint);
                    if (insert)
                        Component.InsertPointAtTime(t);
                    else
                        Component.AddPoint(newPoint);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
                }
                //Handles.FreeMoveHandle(GUIUtility.GetControlID(FocusType.Passive), newPoint, Quaternion.identity, HandleUtility.GetHandleSize(newPoint) * 0.09f, Vector3.one, Handles.DotHandleCap);
            }
        }

        private void UpdateActiveToolByUnityTool()
        {
            if (m_ToolMappings.TryGetValue(Tools.current, out ManipulationTool tool) && tool != m_ActiveTool)
                SetActiveTool(tool);
        }

        private void SetActiveTool(ManipulationTool in_Tool)
        {
            m_ActiveTool = in_Tool;
            if (m_ToolMappings.ContainsValue(in_Tool))
                Tools.current = m_ToolMappings.First(x => x.Value == in_Tool).Key;
            else
                Tools.current = Tool.None;
        }

        private void DrawSceneViewGUI()
        {
            Rect sceneViewRect = SceneView.lastActiveSceneView.camera.pixelRect;
            SceneViewGuiAnchor anchor = EasyBezierSettings.Instance.Settings.SceneViewGuiAnchor;
            Vector2 padding = EasyBezierSettings.Instance.Settings.SceneViewGuiPadding;
            float y = anchor == SceneViewGuiAnchor.TopLeft || anchor == SceneViewGuiAnchor.TopRight || anchor == SceneViewGuiAnchor.TopCenter ? padding.y : sceneViewRect.height - padding.y - m_SceneViewHudHeight;
            float x = 0;
            switch (anchor)
            {
                case SceneViewGuiAnchor.TopLeft:
                case SceneViewGuiAnchor.BottomLeft:
                    x = padding.x;
                    break;
                case SceneViewGuiAnchor.TopCenter:
                case SceneViewGuiAnchor.BottomCenter:
                    x = (sceneViewRect.width - m_SceneViewHudWidth) / 2f;
                    break;
                case SceneViewGuiAnchor.TopRight:
                case SceneViewGuiAnchor.BottomRight:
                    x = sceneViewRect.width - padding.x - m_SceneViewHudWidth - (anchor == SceneViewGuiAnchor.TopRight ? ExtraTopRightPadding : 0);
                    break;
            }
            Rect windowRect = new Rect(x, y, m_SceneViewHudWidth, m_SceneViewHudHeight);
            Handles.BeginGUI();
            GUILayout.BeginArea(windowRect);

            Rect currentRect = new Rect(0, 0, 30, 24f);

            IsEditing = EditorGUI.Toggle(currentRect, IsEditing, (GUIStyle)"button");
            GUI.DrawTexture(currentRect, EditorGUIUtility.FindTexture("Prefab Icon"), ScaleMode.ScaleToFit);

            if (m_IsEditing)
            {
                currentRect = NextRect(currentRect, 100f);
                DoToolContents(currentRect, s_ToolContents);
                currentRect = NextRect(currentRect, 66f);
                DoToolContents(currentRect, s_MetaToolContents);
            }

            if (m_Event.type == EventType.Layout)
            {
                m_SceneViewHudWidth = currentRect.x + currentRect.width;
                m_SceneViewHudHeight = currentRect.y + currentRect.height;
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DoToolContents(Rect in_Rect, ToolContent[] in_ToolContents)
        {
            GUIContent[] contents = new GUIContent[in_ToolContents.Length];
            int selectedIndex = -1;
            for (int i = 0; i < in_ToolContents.Length; i++)
            {
                contents[i] = in_ToolContents[i].GetContent(m_ActiveTool);
                if (m_ActiveTool == in_ToolContents[i].Tool)
                    selectedIndex = i;
            }
            EditorGUI.BeginChangeCheck();
            int newIndex = GUI.Toolbar(in_Rect, selectedIndex, contents);
            if (EditorGUI.EndChangeCheck())
            {
                SetActiveTool(in_ToolContents[newIndex].Tool);
            }
        }

        private Rect NextRect(Rect in_Rect, float in_Width)
        {
            return new Rect(in_Rect.x + in_Rect.width + 4f, in_Rect.y, in_Width, in_Rect.height);
        }

        public void DrawUpVectorHandles()
        {
            int iterationCount = Component.PointCount * 20;
            for (int i = 0; i <= iterationCount; i++)
            {
                float time = (float)i / iterationCount;
                Vector3 pos = Component.GetPositionAtTime(time);
                Vector3 up = Component.GetUpVectorAtTime(time);
                Handles.color = Color.red;
                Handles.DrawLine(pos, pos + up * 0.6f);
            }
        }

        /// <summary>
        /// Draw all the bezier points.
        /// </summary>
        public void DrawBezierPoints()
        {
            m_Hovering.Index = -1;

            for (int i = 0; i < Component.PointCount; i++)
            {
                DrawBezierPoint(i);
            }
        }

        /// <summary>
        /// Draw a bezier point with it's tangent.
        /// </summary>
        private void DrawBezierPoint(int in_Index)
        {
            Vector3 position = Component.GetPositionAtIndex(in_Index);
            bool isPointSelected = m_Selection.Index == in_Index && m_Selection.PointType == PointType.Point;
            bool isInTangentSelected = m_Selection.Index == in_Index && m_Selection.PointType == PointType.InTangent;
            bool isOutTangentSelected = m_Selection.Index == in_Index && m_Selection.PointType == PointType.OutTangent;

            Handles.color = isPointSelected ? Handles.selectedColor : EasyBezierSettings.Instance.Settings.ControlPointColor;
            m_FreeMoveTool.DoPoint(this, in_Index);
            if (!m_ActiveTool.NeedsSelection || isPointSelected)
                m_ActiveTool.DoPoint(this, in_Index);

            if (in_Index != 0 || Component.IsLooping)
            {
                if (!EasyBezierSettings.Instance.ShouldHideCurveType(Component.GetInTangentCurveTypeAtIndex(in_Index)))
                {
                    Handles.color = isInTangentSelected ? Handles.selectedColor : EasyBezierSettings.Instance.Settings.TangentPointColor;
                    m_FreeMoveTool.DoInTangent(this, in_Index);
                    if (!m_ActiveTool.NeedsSelection || isInTangentSelected)
                        m_ActiveTool.DoInTangent(this, in_Index);
                    Handles.color = isInTangentSelected ? Handles.selectedColor : EasyBezierSettings.Instance.Settings.TangentLineColor;
                    Handles.DrawLine(position, Component.GetInTangentPositionAtIndex(in_Index));
                }
            }

            if (in_Index != Component.PointCount - 1 || Component.IsLooping)
            {
                if (!EasyBezierSettings.Instance.ShouldHideCurveType(Component.GetOutTangentCurveTypeAtIndex(in_Index)))
                {
                    Handles.color = isOutTangentSelected ? Handles.selectedColor : EasyBezierSettings.Instance.Settings.TangentPointColor;
                    m_FreeMoveTool.DoOutTangent(this, in_Index);
                    if (!m_ActiveTool.NeedsSelection || isOutTangentSelected)
                        m_ActiveTool.DoOutTangent(this, in_Index);
                    Handles.color = isOutTangentSelected ? Handles.selectedColor : EasyBezierSettings.Instance.Settings.TangentLineColor;
                    Handles.DrawLine(position, Component.GetOutTangentPositionAtIndex(in_Index));
                }
            }
        }

        public Handles.CapFunction GetCapFunction(int in_Index, PointType in_PointType)
        {
            if (in_PointType == PointType.Point)
                return Handles.DotHandleCap;

            CurveType curveType = in_PointType == PointType.InTangent ? Component.GetInTangentCurveTypeAtIndex(in_Index) : Component.GetOutTangentCurveTypeAtIndex(in_Index);
            if (curveType == CurveType.Free)
                return Handles.RectangleHandleCap;
            else
                return Handles.DotHandleCap;
        }

        /// <summary>
        /// Draw the bezier curve.
        /// </summary>
        public void DrawBezierCurve()
        {
            DrawBezierCurve(Component);
        }

        public static void Pickable(int in_ControlID, BezierPathComponent in_Component)
        {
            Event e = Event.current;
            EventType eventType = e.type;

            switch (eventType)
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == in_ControlID && e.button == 0)
                    {
                        e.Use();
                        Selection.activeGameObject = in_Component.gameObject;
                    }
                    break;
                case EventType.MouseUp:
                    break;
                case EventType.MouseMove:
                    break;
                case EventType.MouseDrag:
                    break;
                case EventType.KeyDown:
                    break;
                case EventType.KeyUp:
                    break;
                case EventType.ScrollWheel:
                    break;
                case EventType.Repaint:
                    if (HandleUtility.nearestControl == in_ControlID)
                    {
                        Handles.color = Handles.preselectionColor;
                    }
                    break;
                case EventType.Layout:
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    float t = in_Component.FindTimeClosestToLine(ray.origin, ray.direction);
                    Vector3 pointOnLine = in_Component.GetPositionAtTime(t);
                    HandleUtility.AddControl(in_ControlID, HandleUtility.DistancePointLine(pointOnLine, ray.origin, ray.direction * Mathf.Infinity));
                    Debug.Log(HandleUtility.DistancePointLine(pointOnLine, ray.origin, ray.direction * Mathf.Infinity));
                    break;
                case EventType.DragUpdated:
                    break;
                case EventType.DragPerform:
                    break;
                case EventType.DragExited:
                    break;
            }
        }

        /// <summary>
        /// Draw the supplied bezier curve. This is just static for gizmo drawing.
        /// </summary>
        public static void DrawBezierCurve(BezierPathComponent in_Component)
        {
            int pointCount = in_Component.PointCount;
            Handles.color = EasyBezierSettings.Instance.Settings.BezierColor;
            for (int i = 0; i < pointCount - 1; i++)
                Handles.DrawBezier(in_Component.GetPositionAtIndex(i), in_Component.GetPositionAtIndex(i + 1), in_Component.GetOutTangentPositionAtIndex(i), in_Component.GetInTangentPositionAtIndex(i + 1), Handles.color, null, EasyBezierSettings.Instance.Settings.PathThickness);

            if (in_Component.IsLooping)
                Handles.DrawBezier(in_Component.GetPositionAtIndex(pointCount - 1), in_Component.GetPositionAtIndex(0), in_Component.GetOutTangentPositionAtIndex(pointCount - 1), in_Component.GetInTangentPositionAtIndex(0), Handles.color, null, EasyBezierSettings.Instance.Settings.PathThickness);
        }

        struct ChangeCurveTypeWrapper
        {
            public int Index;
            public CurveType CurveType;
            public PointType PointType;

            public ChangeCurveTypeWrapper(int in_Index, CurveType in_CurveType, PointType in_PointType)
            {
                Index = in_Index;
                CurveType = in_CurveType;
                PointType = in_PointType;
            }
        }

        public void ShowContextMenu(int in_Index, PointType in_PointType)
        {
            if (in_PointType == PointType.Point)
                ShowPointContextMenu(in_Index);
            else
                ShowTangentContextMenu(in_Index, in_PointType == PointType.InTangent);
        }

        public void ShowPointContextMenu(int in_Index)
        {
            BezierPoint bp = Component.Points[in_Index];

            int bothTangentsTypeValue = bp.InTangent.CurveType == bp.OutTangent.CurveType ? (int)bp.InTangent.CurveType : -1;

            GenericMenu menu = new GenericMenu();
            if (bp.InTangent.CurveType != bp.OutTangent.CurveType)
                menu.AddItem(new GUIContent("Custom"), true, null);

            for (int i = 0; i < s_CurveTypeNames.Length; i++)
            {
                menu.AddItem(new GUIContent($"{s_CurveTypeNames[i]}"), (int)s_CurveTypes[i] == bothTangentsTypeValue, _ContextChangeTangentCurveType, new ChangeCurveTypeWrapper(in_Index, s_CurveTypes[i], PointType.Point));
            }

            if (bp.InTangent.CurveType == CurveType.Free && bp.InTangent.CurveType == CurveType.Free)
            {
                menu.AddSeparator("");
                foreach (TangentConnectionType i in System.Enum.GetValues(typeof(TangentConnectionType)))
                {
                    menu.AddItem(new GUIContent(i.ToString()), bp.ConnectionType == i, _ContextSetConnectionTypeAtIndex, new System.Tuple<int, TangentConnectionType, bool>(in_Index, i, true));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove"), false, _ContextRemoveAtIndex, in_Index);
            menu.ShowAsContext();
        }

        private void ShowTangentContextMenu(int in_Index, bool in_IsInTangent)
        {
            BezierPoint bp = Component.Points[in_Index];
            CurveType curveType = (in_IsInTangent ? bp.InTangent : bp.OutTangent).CurveType;

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < s_CurveTypeNames.Length; i++)
                menu.AddItem(new GUIContent(s_CurveTypeNames[i]), curveType == s_CurveTypes[i], _ContextChangeTangentCurveType, new ChangeCurveTypeWrapper(in_Index, s_CurveTypes[i], in_IsInTangent ? PointType.InTangent : PointType.OutTangent));

            if (bp.InTangent.CurveType == CurveType.Free && bp.InTangent.CurveType == CurveType.Free)
            {
                menu.AddSeparator("");
                foreach (TangentConnectionType i in System.Enum.GetValues(typeof(TangentConnectionType)))
                {
                    menu.AddItem(new GUIContent(i.ToString()), bp.ConnectionType == i, _ContextSetConnectionTypeAtIndex, new System.Tuple<int, TangentConnectionType, bool>(in_Index, i, in_IsInTangent));
                }
            }

            menu.ShowAsContext();
        }

        private void _ContextChangeTangentCurveType(object in_Wrapper)
        {
            ChangeCurveTypeWrapper wrapper = (ChangeCurveTypeWrapper)in_Wrapper;

            Undo.RecordObject(Component, UndoStrings.SetCurveType);
            switch (wrapper.PointType)
            {
                case PointType.Point:
                    Component.SetInTangentCurveTypeAtIndex(wrapper.Index, wrapper.CurveType);
                    Component.SetOutTangentCurveTypeAtIndex(wrapper.Index, wrapper.CurveType);
                    break;
                case PointType.InTangent:
                    Component.SetInTangentCurveTypeAtIndex(wrapper.Index, wrapper.CurveType);
                    break;
                case PointType.OutTangent:
                    Component.SetOutTangentCurveTypeAtIndex(wrapper.Index, wrapper.CurveType);
                    break;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
        }

        private void _ContextRemoveAtIndex(object in_Index)
        {
            Undo.RecordObject(Component, UndoStrings.RemovePoint);
            Component.RemovePointAt((int) in_Index);
            PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
        }

        private void _ContextSetConnectionTypeAtIndex(object in_Index)
        {
            Tuple<int, TangentConnectionType, bool> tuple = (Tuple<int, TangentConnectionType, bool>)in_Index;
            Undo.RecordObject(Component, UndoStrings.SetConnectedTangents);
            Component.SetTangentConnectionTypeAtIndex(tuple.Item1, tuple.Item2, tuple.Item3);
            PrefabUtility.RecordPrefabInstancePropertyModifications(Component);
        }

        public bool IsSelected(int in_Index)
        {
            return m_Selection.Index == in_Index;
        }

        public bool IsSelected(int in_Index, PointType in_PointType)
        {
            return m_Selection.Index == in_Index && m_Selection.PointType == in_PointType;
        }

        public bool IsEditing {
            get => m_IsEditing;
            set {
                if (m_IsEditing != value)
                {
                    m_IsEditing = value;
                    OnIsEditingChanged?.Invoke(this, m_IsEditing);
                }
            }
        }

        public Quaternion ToolRotation {
            get {
                return Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : Component.transform.rotation;
            }
        }

        public Space ToolSpace {
            get {
                return Tools.pivotRotation == PivotRotation.Global ? Space.World : Space.Self;
            }
        }

        public bool IsClickAllowed {
            get {
                return s_IsViewToolActive != null ? (bool) s_IsViewToolActive.GetValue(null) : false;
            }
        }
    }
}