using UnityEngine;

namespace Core.Spatial
{
    using Core.Geom;

    public class SpatialObject : ISpatial
    {
        public GameObject GameObject { get; } = null;
        public Rect2 BoundingBox { get; } = Rect2.Empty;

        public SpatialObject(GameObject obj, Rect2 rect)
        {
            this.BoundingBox = rect;
            this.GameObject = obj;
        }
    }
}

