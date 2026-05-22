using UnityEngine;
using DungeonGame.Systems.Managers;
using DungeonGame.Systems.Dungeon;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Controls player input mapping (WASD/Arrows), attack action, and damage scaling 
    /// depending on the active level difficulty multiplier.
    /// </summary>
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("The amount of damage dealt to enemies during an attack.")]
        public float attackDamage = 20f;
        [Tooltip("The range of the circle sweep for detecting targets when attacking.")]
        public float attackRange = 2f;

        private CharacterMovement characterMovement;
        private Health health;
        private bool isDead = false;

        private void Awake()
        {
            characterMovement = GetComponent<CharacterMovement>();
            health = GetComponent<Health>();
            health.OnDeath.AddListener(HandleDeath);
        }

        private void Start()
        {
            ConfigureDamage();
        }

        /// <summary>
        /// Recalculates and adjusts the player's attack damage based on the current level index 
        /// and the global difficulty multiplier (higher difficulty decreases player damage).
        /// </summary>
        public void ConfigureDamage()
        {
            if (ScoreManager.Instance == null) return;

            int currentLevel = ScoreManager.Instance.GetCurrentLevel();
            bool isBossLevel = currentLevel % 5 == 0;

            if (isBossLevel)
            {
                // Reset to default on boss level to give the player a fair baseline
                attackDamage = 20f;
                Debug.Log($"[Player Damage] Boss Level {currentLevel} -> Player damage reset to default: {attackDamage}"); // for testing purposes
            }
            else
            {
                float multiplier = 1.0f;
                if (DungeonGenerator.Instance != null)
                {
                    multiplier = DungeonGenerator.Instance.CurrentDifficultyMultiplier;
                }

                // If multiplier > 1 (harder performance), player damage decreases (clamped to min 15)
                // If multiplier < 1 (struggling performance), player damage increases (clamped to max 50)
                attackDamage = Mathf.Clamp(20f / multiplier, 15f, 50f);
                Debug.Log($"[Player Damage] Level {currentLevel} -> Player damage set to: {attackDamage:F1} (Multiplier: {multiplier:F2})"); // for testing purposes
            }
        }

        private void Update()
        {
            if (isDead) return;

            // Gather horizontal and vertical input (WASD, Arrow Keys, or Controller D-pad)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            // Update direction in CharacterMovement component
            characterMovement.SetMovement(new Vector2(moveX, moveY));

            // Check for attack input (Left Mouse Click or Spacebar)
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
            {
                PerformAttack();
            }
        }

        /// <summary>
        /// Executes an attack, triggers animations, and checks for overlapping enemy colliders in the attack range.
        /// </summary>
        private void PerformAttack()
        {
            characterMovement.Attack();

            // Detect all colliders within the attack range circle
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var enemy in hitEnemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    Health enemyHealth = enemy.GetComponent<Health>();
                    if (enemyHealth != null)
                    {
                        // Apply damage to the enemy
                        enemyHealth.TakeDamage(attackDamage);
                    }
                }
            }
        }

        /// <summary>
        /// Callback subscribed to the player's death event. Stops movement and plays the death sequence.
        /// </summary>
        private void HandleDeath()
        {
            if (isDead) return;
            isDead = true;
            characterMovement.Die();
            // Additional game over trigger logic is handled in UIManager (listener on health)
        }

        /// <summary>
        /// Draws the attack range sphere in the Editor viewport when the player object is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
