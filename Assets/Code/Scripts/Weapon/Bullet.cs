using UnityEngine;

namespace Scripts.Weapon
{
    public class Bullet : MonoBehaviour
    {
        [Range(0,100)]
        public float velocity = 50f;
        public Rigidbody2D rb;

        void Start()
        {
            // assign forward velocity at bullet creation
            rb.velocity = transform.right * velocity;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Handle collisions with any other objects.
            // IsTrigger must be set on the Collider object 
            // for this to be invoked on object collisions.
            Debug.Log("Bullet Collision: " + collision.name);

            // // This is how/where bullet effects/explosions 
            // // can be invoked any time the bullet hits anything.
            // Instantiate(explosion, transform.position, transform.rotation);

            // destroy the bullet 
            Destroy(this.gameObject);
        }
    }
}