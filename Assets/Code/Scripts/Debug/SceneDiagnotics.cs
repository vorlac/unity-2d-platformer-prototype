using Core.Geom;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Diagnostics
{
    public class SceneDiagnotics : MonoBehaviour
    {
        public Vector3 mousePositionWorld = Vector3.zero;
        
        public bool enableDistLine = false;
        public Color distLineColor = Color.gray;
        public float distance = 0f;

        public bool enableMouseHighlight = false;
        public LayerMask mouseHighlightLayers = 1;
        public Color highlightColor = Color.cyan;
        [Range(1, 10)]
        public float highlightWidth = 5f;

        public bool enableOutlines = false;
        public LayerMask objectOutlineLayers = 1;
        public Color outlineColor = Color.white;
        [Range(1,10)]
        public float outlineWidth = 5f;

        public bool enableLabels = false;
        public LayerMask objectLabelLayers = 1;
        public Color labelColor = Color.yellow;
        [Range(1, 48)]
        public int labelFontSize = 12;

        private void OnDrawGizmos()
        {
            var mousePt = Utils.World.GetMousePointWorldCoords();
            distance = Vector3.Distance(this.transform.position, mousePositionWorld);

            if (enableMouseHighlight)
            {
                // draw objects that intersect with the mouse in mouseIntersectionLayers
                var objects = Utils.World.GetSceneObjects(mouseHighlightLayers);
                foreach (var obj in objects)
                {
                    var pts = Utils.Object.GetBoxColliderVertices(obj);
                    if (pts != null && Utils.Object.PointIntersectsObject(obj, mousePt))
                        Utils.Debug.DrawLine(pts, highlightColor, highlightWidth);
                }
            }

            if (enableOutlines)
            {
                // draw outline for objects in layers defined by objectOutlineLayers
                var outlineObjects = Utils.World.GetSceneObjects(objectOutlineLayers);
                foreach (var obj in outlineObjects)
                {
                    // draw rectangle (with rotation)
                    var pts = Utils.Object.GetBoxColliderVertices(obj);
                    if (pts != null)
                        Utils.Debug.DrawLine(pts, outlineColor, outlineWidth);
                }
            }

            if (enableLabels)
            {
                // draw text label for objects in layers defined by objectLabelLayers
                var labelObjects = Utils.World.GetSceneObjects(objectLabelLayers);
                foreach (var obj in labelObjects)
                {
                    var rect = Utils.Object.GetBoundingRect(obj);
                    // draw object name label
                    Utils.Debug.DrawText(obj.name, rect.GetPoint(Rect2.Location.BottomCenter), labelColor, labelFontSize);
                }
            }

            if (enableDistLine)
            {
                // draw line from debug object transform location and populate distance
                Utils.Debug.DrawLine(this.transform.position, mousePositionWorld, distLineColor);
                // draw cross at mouse point (horizontal line)
                Utils.Debug.DrawLine(new Line2(mousePositionWorld - (Vector3.left * 0.25f), mousePositionWorld - (Vector3.right * 0.25f)), Color.white);
                // draw cross at mouse point (vertical line)
                Utils.Debug.DrawLine(new Line2(mousePositionWorld - (Vector3.up * 0.25f), mousePositionWorld - (Vector3.down * 0.25f)), Color.white);
            }
        }
    }
}