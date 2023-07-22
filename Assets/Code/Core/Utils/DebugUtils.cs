using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Utils
{
    using Core.Geom;

    static class Debug
    {
        public static void DrawText(string text, Vector2 position, Color color, int fontSize = 12)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = fontSize;
            style.normal.textColor = color;
            
            GUIContent content = new GUIContent(text);
            var contentSize = style.CalcSize(content);
            var guiPosition = HandleUtility.WorldToGUIPoint(position);
            var centeredRay = HandleUtility.GUIPointToWorldRay(new Vector2(guiPosition.x - (contentSize.x * 0.5f), guiPosition.y));
            
            Handles.Label(centeredRay.origin, content, style);
        }

        /// <summary>
        /// Draws a line in a specific color and thickness. 
        /// </summary>
        /// <remarks>
        /// This function uses unity gizmos so it can only be called in gizmo functions.
        /// </remarks>
        /// <param name="line"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        public static void DrawLine(Line2 line, Color color, float thickness = 1)
        {
            Utils.Debug.DrawLine(line.Start, line.End, color, thickness);
        }

        /// <summary>
        /// Draws a polyline made up of pts in a specific color and thickness. 
        /// </summary>
        /// <remarks>
        /// This function uses unity gizmos so it can only be called in gizmo functions.
        /// </remarks>
        /// <param name="pts"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        public static void DrawLine(List<Vector3> pts, Color color, float thickness = 1)
        {
            for (int i = 1; i < pts.Count + 1; ++i)
                Utils.Debug.DrawLine(pts[i - 1 % pts.Count], pts[i % pts.Count], color, thickness);
        }

        /// <summary>
        /// Draws a polyline made up of pts in a specific color and thickness. 
        /// </summary>
        /// <remarks>
        /// This function uses unity gizmos so it can only be called in gizmo functions.
        /// </remarks>
        public static void DrawLine(Vector3[] pts, Color color, float thickness = 1)
        {
            for (int i = 1; i < pts.Length + 1; ++i)
                Utils.Debug.DrawLine(pts[i - 1 % pts.Length], pts[i % pts.Length], color, thickness);
        }

        /// <summary>
        /// Draws a line from p1 -> p2 in a specific color and thickness. 
        /// </summary>
        /// <remarks>
        /// This function uses unity gizmos so it can only be called in gizmo functions.
        /// </remarks>
        public static void DrawLine(Vector3 p1, Vector3 p2, Color color, float thickness = 1)
        {
            switch (thickness)
            {
                case 1:
                    UnityEngine.Gizmos.color = color;
                    UnityEngine.Gizmos.DrawLine(p1, p2);
                    break;

                default:
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, color, null, thickness);
                    break;
            }
        }

        /// <summary>
        /// Draws a line in a specific color. 
        /// </summary>
        /// <remarks>
        /// This function doesn't use gizmos so it can be called in any code.
        /// </remarks>
        public static void DrawDebugLine(Line2 line, Color color)
        {
            Utils.Debug.DrawDebugLine(line.Start, line.End, color);
        }

        /// <summary>
        /// Draws a line from p1 -> p2 in a specific color. 
        /// </summary>
        /// <remarks>
        /// This function doesn't use gizmos so it can be called in any code.
        /// </remarks>
        public static void DrawDebugLine(Vector2 p1, Vector2 p2, Color color)
        {
             UnityEngine.Debug.DrawLine(p1, p2, color);
        }

        /// <summary>
        /// Draws a rectangle in a specific color and line thickness.
        /// </summary>
        /// <remarks>
        /// This function can use either gizmos or the standard debug 
        /// functionality (controlled through the 'gizmo' param).
        /// </remarks>
        public static void DrawRectangle(Rect2 rect, Color color, float thickness = 1, bool gizmo = true)
        {
            Vector3 tl = new Vector3(rect.Left, rect.Top);
            Vector3 tr = new Vector3(rect.Right, rect.Top);
            Vector3 bl = new Vector3(rect.Left, rect.Bottom);
            Vector3 br = new Vector3(rect.Right, rect.Bottom);

            if (gizmo)
            {
                Utils.Debug.DrawLine(tl, tr, color, thickness);
                Utils.Debug.DrawLine(tr, br, color, thickness);
                Utils.Debug.DrawLine(br, bl, color, thickness);
                Utils.Debug.DrawLine(bl, tl, color, thickness);
            }
            else
            {
                Utils.Debug.DrawDebugLine(tl, tr, color);
                Utils.Debug.DrawDebugLine(tr, br, color);
                Utils.Debug.DrawDebugLine(br, bl, color);
                Utils.Debug.DrawDebugLine(bl, tl, color);
            }
        }

        /// <summary>
        /// Renders a solid rectangle in a certain color (I don't think this works).
        /// </summary>
        public static void DrawFilledRectangle(Rect2 rect, Color color)
        {
            var position = Rect.MinMaxRect(rect.Left, rect.Bottom, rect.Right, rect.Top);
            
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            GUI.skin.box.normal.background = texture;
            GUI.Box(position, GUIContent.none);
        }

        /// <summary>
        /// Captures the runtime of a function (the action).
        /// </summary>
        /// <example>
        ///   TimeSpan time = DebugUtils.TimeFunction(() => {
        ///      ...
        ///   });
        /// </example>
        /// <param name="action"></param>
        /// <returns></returns>
        public static System.TimeSpan TimeFunction(System.Action action)
        {
            Stopwatch sw = Stopwatch.StartNew();
            
            sw.Start();
            action();
            sw.Stop();
            
            return sw.Elapsed;
        }
    }
}
        