using System;
using UnityEngine;

namespace Core.world
{
    using Core.Spatial;
    using Core.Geom;
    using Core.Tree;


    public class SpatialIndex : MonoBehaviour
    {
        public delegate int Del(int asdf);

        public static int DelTest(int asdf) { return asdf; }
        /// <summary>
        /// The real spatial index. This RTree will store 
        /// references to all scene/layer objects in a way 
        /// that they will be quickly accessed by location
        /// </summary>
        private RTree<SpatialObject, Rect2> rtree = new RTree<SpatialObject, Rect2>();

        /// <summary>
        /// The layer mask that defines which layers to 
        /// use for the spatial index. Only objects in 
        /// those layers will be added to the spatial index.
        /// </summary>
        public LayerMask dataLayers;

        /// <summary>
        /// The elapsed time since the tree was updated
        /// </summary>
        public float lastUpdateElapsedTime = 0f;

        private void Awake()
        {
            BuildLevelDataTree();
        }

        private void BuildLevelDataTree()
        {
            Del test = DelTest;
            var val = test(1234);

            if (rtree.Count > 0)
            {
                rtree.Clear();
            }

            // convert the layer mask info to a list of layer IDs
            var layerIDs = Utils.World.GetLayerIDs(dataLayers);
            if (layerIDs.Count > 0)
            {
                // fetch all game objects in any of the layers
                var layerObjects = Utils.World.GetLayerObjects(layerIDs);
                foreach (var obj in layerObjects)
                {
                    var objCollider = obj.GetComponent<BoxCollider2D>();
                    if (objCollider != null)
                    {
                        Rect2 objRect = Rect2.FromBounds(objCollider.bounds);
                        SpatialObject levelObj = new SpatialObject(obj, objRect);
                        rtree.Insert(levelObj, objRect, objRect);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            TimeSpan time = Utils.Debug.TimeFunction(() =>
            {
                BuildLevelDataTree();
            });

            var perSum = rtree.GetPerimiterSum();
            var treeRectInfo = rtree.GetAllTreeRectangles();
            treeRectInfo.Sort((a, b) => -b.Item2.CompareTo(a.Item2));

            foreach (var rectInfo in treeRectInfo)
            {
                var rect = rectInfo.Item1;
                var depth = rectInfo.Item2;
                var isleaf = rectInfo.Item3;

                Vector3 tl = new Vector3(rect.Left, rect.Top);
                Vector3 tr = new Vector3(rect.Right, rect.Top);
                Vector3 bl = new Vector3(rect.Left, rect.Bottom);
                Vector3 br = new Vector3(rect.Right, rect.Bottom);

                Color color = Color.white;
                float thickness = 3;

                switch (depth)
                {
                    case 0:
                        color = Color.white;
                        break;
                    case 1:
                        color = Color.yellow;
                        break;
                    case 2:
                        color = Color.green;
                        break;
                    case 3:
                        color = Color.cyan;
                        break;
                    case 4:
                        color = Color.blue;
                        break;
                    case 5:
                        color = Color.magenta;
                        break;
                    case 6:
                        color = Color.white;
                        break;
                    default:
                        color = Color.black;
                        break;
                }

                Utils.Debug.DrawRectangle(rect, color, thickness);
            }
        }
    }
}