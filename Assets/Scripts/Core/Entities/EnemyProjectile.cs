using UnityEngine;
using DungeonGame.Systems.Managers;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Handles the movement and collision of projectiles fired by ranged enemies.
    /// Needs to be attached to the bullet prefab with a Rigidbody2D (Kinematic or Dynamic with 0 gravity) and Collider2D (IsTrigger).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Tooltip("Speed of the projectile.")]
        public float speed = 5f;
        [Tooltip("Damage dealt to the player on impact.")]
        public float damage = 5f;
        [Tooltip("Time in seconds before the projectile destroys itself to prevent memory leaks.")]
        public float lifetime = 5f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // I disabled gravity since bullets shouldn't be affected in a top-down game
            rb.gravityScale = 0f; 

            // I forced the bullet to the foreground so it doesn't hide behind the floor
            Vector3 pos = transform.position;
            pos.z = -5f;
            transform.position = pos;

            // I also bumped up the sorting order to be extra safe
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10;
            }
        }

        private void Start()
        {
            // I set the bullet to auto-destroy after its lifetime expires to keep the scene clean
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Fires the projectile in the specified normalized direction.
        /// </summary>
        public void Fire(Vector2 direction)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            
            // I set the velocity to fire in the given direction
            rb.linearVelocity = direction * speed;

            // I rotated the sprite to face the travel direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // I dealt damage and destroyed the projectile if it hit the player
            if (collision.CompareTag("Player"))
            {
                Health playerHealth = collision.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
            // I destroyed the bullet if it hit a wall or any non-trigger, non-enemy collider
            else if (!collision.isTrigger && !collision.CompareTag("Enemy") && !collision.CompareTag("Untagged"))
            {
                Destroy(gameObject);
            }
        }
    }
}
