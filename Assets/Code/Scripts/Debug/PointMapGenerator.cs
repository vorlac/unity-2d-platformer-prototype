using UnityEngine;

namespace Scripts.Diagnostics
{
    public class PointMapGenerator : MonoBehaviour
    {
        public bool disabled = true;

        public GameObject pointPrefab;
        public LayerMask collisionMask;

        [Range(0.1f, 1f)]
        public float xInterval = 0.75f;

        [Range(0.1f, 1f)]
        public float yInterval = 0.75f;
        
        [Range(0f, 1f)]
        public float xOffset = 0f;
        
        [Range(0f, 1f)]
        public float YOffset = 0.375f;

        void Start()
        {
            if (disabled)
                return;

            var worldExtRect = Utils.World.GetWorldBounds();
            var worldObjects = Utils.World.GetSceneObjects(collisionMask);

            var worldPoints = new GameObject("WorldPoints");
            for (float y = worldExtRect.Bottom + YOffset; y < worldExtRect.Top; y += yInterval)
            {
                for (float x = worldExtRect.Left + xOffset; x < worldExtRect.Right; x += xInterval)
                {
                    // Create the point with a location of (x, y)
                    var pointObject = Instantiate(pointPrefab, new Vector3(x, y), Quaternion.identity, worldPoints.transform);

                    // Grab the point object's collider to check for possible collisions
                    BoxCollider2D pntCollider = pointObject.GetComponent<BoxCollider2D>();

                    foreach (var worldObject in worldObjects)
                    {
                        // Check if the point collides with any other layerMask objects 
                        BoxCollider2D objCollider = worldObject.GetComponent<BoxCollider2D>();
                        if (objCollider && pntCollider && objCollider.IsTouching(pntCollider))
                        {
                            // Remove any points that collide with existing objects
                            Destroy(pointObject);
                        }
                    }
                }
            }
        }
    }
}