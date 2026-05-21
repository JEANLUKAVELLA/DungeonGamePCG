using UnityEngine;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Base class for character movement (both Player and Enemies) in a top-down 2D space.
    /// Handles Rigidbody2D velocity updates, facing directions, and animator state updates.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMovement : MonoBehaviour
    {
        [Tooltip("The base movement speed of the character.")]
        public float baseSpeed = 5f;
        
        // The current movement speed, which can be modified dynamically (e.g., mud slow, powerups)
        private float currentSpeed;
        
        protected Rigidbody2D rb;
        protected Animator animator;
        protected Vector2 movement;
        
        // Pre-cached Animator parameter hashes for performance optimization
        protected readonly int SpeedHash = Animator.StringToHash("Speed");
        protected readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        protected readonly int IsDeadHash = Animator.StringToHash("IsDead");

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            currentSpeed = baseSpeed;
            
            // Ensure gravity doesn't affect top-down movement
            rb.gravityScale = 0f;
            // Prevent physics collisions from rotating the character sprite
            rb.freezeRotation = true;

            // Ensure characters (Player/Enemies) are always rendered on top of props and floor tiles
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10;
            }
        }

        protected virtual void Update()
        {
            UpdateAnimation();
            UpdateFacingDirection();
        }

        protected virtual void FixedUpdate()
        {
            // Apply velocity directly to Rigidbody2D for smooth physics-based movement
            rb.velocity = movement * currentSpeed;
        }

        /// <summary>
        /// Sets the movement input direction vector.
        /// </summary>
        /// <param name="direction">The raw direction vector (usually from input or AI pathing).</param>
        public void SetMovement(Vector2 direction)
        {
            movement = direction.normalized;
        }

        /// <summary>
        /// Dynamically adjusts the character's movement speed.
        /// </summary>
        /// <param name="newSpeed">The new speed value to apply.</param>
        public void SetSpeed(float newSpeed)
        {
            currentSpeed = newSpeed;
        }

        /// <summary>
        /// Resets the current movement speed back to its configured base speed.
        /// </summary>
        public void ResetSpeed()
        {
            currentSpeed = baseSpeed;
        }

        /// <summary>
        /// Triggers the attack animation.
        /// </summary>
        public void Attack()
        {
            if (animator != null)
            {
                animator.SetTrigger(IsAttackingHash);
            }
        }

        /// <summary>
        /// Handles death logic, triggering death animations, disabling colliders/movement, and graying out the sprite.
        /// </summary>
        public void Die()
        {
            if (animator != null)
            {
                animator.SetBool(IsDeadHash, true);
                animator.SetFloat(SpeedHash, 0f);
                
                // Only set trigger if it exists in the controller to avoid warnings
                if (HasParameter(animator, "Die"))
                {
                    animator.SetTrigger("Die"); 
                }
            }

            // Fallback visual feedback if no animations exist on the controller
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.gray; // Fade to gray to indicate death
            }

            SetMovement(Vector2.zero);
            rb.velocity = Vector2.zero;
            
            // Disable collider so it doesn't block the player or other entities
            Collider2D characterCollider = GetComponent<Collider2D>();
            if (characterCollider != null) characterCollider.enabled = false;

            this.enabled = false;
        }

        /// <summary>
        /// Updates the Speed parameter in the Animator controller based on current movement magnitude.
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, movement.magnitude);
            }
        }

        /// <summary>
        /// Flips the character localScale along the X-axis depending on horizontal movement direction.
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (movement.x > 0.01f)
            {
                // Move Right -> Face Right
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (movement.x < -0.01f)
            {
                // Move Left -> Face Left (Flip X)
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        /// <summary>
        /// Helper utility to verify if a specific parameter name exists within an Animator Controller.
        /// </summary>
        private bool HasParameter(Animator animatorController, string paramName)
        {
            foreach (AnimatorControllerParameter param in animatorController.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }
    }
}
