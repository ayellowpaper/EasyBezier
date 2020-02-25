using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyBezier
{
    public static class MoreHandles
    {
        static Material g_Material = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
        static Vector2 g_MouseStartPosition;
        static bool g_IsDragging = false;

        public const float StartDragDistance = 2f;

        public struct PositionButton
        {
            public bool WasClicked;
            public bool IsHovering;
            public Vector3 Position;

            public PositionButton(bool in_WasClicked, bool in_IsHovering, Vector3 in_Position)
            {
                WasClicked = in_WasClicked;
                IsHovering = in_IsHovering;
                Position = in_Position;
            }
        }

        public static int g_LastHandle = -1;
        public static void ListenForHandleChange()
        {
            g_LastHandle = GUIUtility.hotControl;
        }

        public static bool DidHandleChange()
        {
            return GUIUtility.hotControl != g_LastHandle;
        }

        public static bool WasHandleSelected()
        {
            return GUIUtility.hotControl != g_LastHandle && GUIUtility.hotControl != 0;
        }

        public static bool WasHandleDeselected()
        {
            return GUIUtility.hotControl != g_LastHandle && GUIUtility.hotControl == 0;
        }

        internal static PositionButton SelectableMoveHandle(int in_ControlID, Vector3 in_Position, float in_Size, bool in_Fill = true)
        {
            PositionButton ret = new PositionButton(false, false, in_Position);
            bool wasLastHotControl = GUIUtility.hotControl == in_ControlID;
            Handles.CapFunction capFunction = Handles.RectangleHandleCap;
            if (in_Fill)
                capFunction = Handles.DotHandleCap;
            Vector3 newPosition = Handles.FreeMoveHandle(in_ControlID, in_Position, Quaternion.identity, HandleUtility.GetHandleSize(in_Position) * in_Size, Vector3.one, capFunction);
            if (HandleUtility.nearestControl == in_ControlID)
                ret.IsHovering = true;
            //Vector3 newPosition = Handles.Slider2D(in_ControlID, in_Position, Vector3.zero, -cameraTransform.forward, cameraTransform.up, cameraTransform.right, HandleUtility.GetHandleSize(in_Position) * in_Size, capFunction, Vector2.zero);
            if (!wasLastHotControl && GUIUtility.hotControl == in_ControlID)
            {
                g_MouseStartPosition = Event.current.mousePosition;
            }
            if (wasLastHotControl && GUIUtility.hotControl != in_ControlID)
            {
                if (g_IsDragging == false)
                    ret.WasClicked = true;
                g_IsDragging = false;
            }
            
            if (GUIUtility.hotControl == in_ControlID)
            {
                if (!g_IsDragging && (Event.current.mousePosition - g_MouseStartPosition).magnitude >= StartDragDistance)
                    g_IsDragging = true;

                if (g_IsDragging)
                    ret.Position = newPosition;
            }

            return ret;
        }
    }
}
