using UnityEngine;
using System.Collections;

namespace Scripts.Controller
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RaycastController : MonoBehaviour
    {
        #region internal types
        public struct RaycastOrigins
        {
            public Vector2 topLeft;
            public Vector2 topRight;
            public Vector2 botLeft;
            public Vector2 botRight;
        }
        #endregion

        public int horizontalRayCount = 4;
        public int verticalRayCount = 4;
        public float horizontalRaySpacing;
        public float verticalRaySpacing;

        public LayerMask collisionMask;
        public BoxCollider2D boxCollider;
        public RaycastOrigins raycastOrigins;

        public const float skinWidth = .015f;
        const float distBetweenRays = .15f;

        public virtual void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        public virtual void Start()
        {
            CalculateRaySpacing();
        }

        public void UpdateRaycastOrigins()
        {
            Bounds bounds = boxCollider.bounds;
            bounds.Expand(skinWidth * -2);

            raycastOrigins.botLeft = new Vector2(bounds.min.x, bounds.min.y);
            raycastOrigins.botRight = new Vector2(bounds.max.x, bounds.min.y);
            raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
            raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        }

        public void CalculateRaySpacing()
        {
            Bounds bounds = boxCollider.bounds;
            bounds.Expand(skinWidth * -2);

            float boundsWidth = bounds.size.x;
            float boundsHeight = bounds.size.y;

            horizontalRayCount = Mathf.RoundToInt(boundsHeight / distBetweenRays);
            verticalRayCount = Mathf.RoundToInt(boundsWidth / distBetweenRays);

            horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
            verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
        }
    }
}