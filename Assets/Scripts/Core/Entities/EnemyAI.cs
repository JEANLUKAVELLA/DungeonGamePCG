using UnityEngine;
using DungeonGame.Systems.Managers;
using DungeonGame.Systems.Dungeon;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Defines the biome-specific types of enemies in the game.
    /// </summary>
    public enum EnemyType { Ocean, Hot, Dirt }

    /// <summary>
    /// Handles enemy behaviors, including wandering, chasing the player when detected,
    /// attacking when close, scaling damage values dynamically by level type and difficulty, 
    /// and handling death logic.
    /// </summary>
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [Tooltip("The biome-specific type of this enemy.")]
        public EnemyType enemyType = EnemyType.Dirt;
        [Tooltip("Distance at which the enemy detects the player and starts chasing.")]
        public float detectionRadius = 10f;
        [Tooltip("Distance at which the enemy stops and attacks the player.")]
        public float attackRadius = 1.8f; 
        [Tooltip("Cooldown duration between consecutive attacks in seconds.")]
        public float attackCooldown = 2f;
        [Tooltip("Base attack damage dealt to the player.")]
        public float attackDamage = 10f;
        [Tooltip("Score awarded to the player when this enemy is defeated.")]
        public int scoreValue = 100;

        [Header("Ranged Settings (Hot Biome)")]
        [Tooltip("Prefab for the bullet shot by Hot biome enemies.")]
        public GameObject projectilePrefab;
        [Tooltip("When the player gets closer than this radius, the enemy stops shooting and rushes in for melee.")]
        public float meleeChaseRadius = 4f;
        [Tooltip("Cooldown between projectile shots.")]
        public float shootingCooldown = 1.5f;
        [Tooltip("Damage dealt by projectiles. Usually slightly lower than melee.")]
        public float rangedDamage = 5f;
        
        private float lastShotTime;

        [Header("Wander Settings")]
        [Tooltip("Maximum radius for wandering around the current position.")]
        public float wanderRadius = 5f;
        [Tooltip("Time interval between choosing a new wander target destination.")]
        public float wanderInterval = 3f;
        
        private Vector2 wanderTarget;
        private float nextWanderTime;
        
        private CharacterMovement characterMovement;
        private Health health;
        private Transform playerTarget;
        private float lastAttackTime;
        private bool isDead = false;

        private void Awake()
        {
            characterMovement = GetComponent<CharacterMovement>();
            health = GetComponent<Health>();
            
            health.OnDeath.AddListener(HandleDeath);
            SetNextWanderTarget();
        }

        private void Start()
        {
            // Setting EnemyType in the inspector dropdown, only just to make sure if an enemy wasnt set and this script is acting as my backup clearance
            if (gameObject.name.Contains("Hot") && enemyType != EnemyType.Hot)
            {
                Debug.LogWarning($"[Auto-Fix] Changed {gameObject.name}'s type from {enemyType} to Hot!"); // for testing purposes
                enemyType = EnemyType.Hot;
            }
            else if (gameObject.name.Contains("Ocean") && enemyType != EnemyType.Ocean)
            {
                enemyType = EnemyType.Ocean;
            }
            else if (gameObject.name.Contains("Dirt") && enemyType != EnemyType.Dirt)
            {
                enemyType = EnemyType.Dirt;
            }

            // Finding the player reference in the scene by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }

            // Registering this enemy with the ScoreManager to track remaining enemies
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterEnemy();
            }

            ConfigureStats();
        }

        /// <summary>
        /// Configures speed and attack damage based on the current level index, biome type, and difficulty multiplier.
        /// Boss levels (every 5th level) set damage values to maximum limits.
        /// </summary>
        private void ConfigureStats()
        {
            if (ScoreManager.Instance == null) return;

            int currentLevel = ScoreManager.Instance.GetCurrentLevel();
            bool isBossLevel = currentLevel % 5 == 0;

            float multiplier = 1.0f;
            if (DungeonGenerator.Instance != null)
            {
                multiplier = DungeonGenerator.Instance.CurrentDifficultyMultiplier;
            }

            if (isBossLevel)
            {
                // Setting boss level enemy damage to their highest limit (giving the player more of a challenge)
                attackDamage = enemyType switch
                {
                    EnemyType.Ocean => 8f,
                    EnemyType.Hot => 20f,
                    EnemyType.Dirt => 12f,
                    _ => attackDamage
                };
                Debug.Log($"[Boss Level {currentLevel}] {gameObject.name} ({enemyType}) set to MAXIMUM damage: {attackDamage}"); // for testing purposes
            }
            else
            {
                // Scailing the base damage with the difficulty multiplier and clamped it for non-boss levels
                attackDamage = enemyType switch
                {
                    EnemyType.Ocean => Mathf.Clamp(5f * multiplier, 2f, 8f),
                    EnemyType.Hot => Mathf.Clamp(15f * multiplier, 10f, 20f),
                    EnemyType.Dirt => Mathf.Clamp(8f * multiplier, 5f, 12f),
                    _ => attackDamage
                };
                Debug.Log($"[Level {currentLevel}] {gameObject.name} ({enemyType}) attack damage set to: {attackDamage:F1} (Multiplier: {multiplier:F2})"); // for testing purposes
            }

            // --- SPEED SCALING - here am making a distinct walk difference between the Dirt enemy and Hot & Ocean enemies ---
            if (enemyType == EnemyType.Dirt)
            {
                characterMovement.baseSpeed = Mathf.Clamp(3f * multiplier, 3f, 3.5f);
            }
            else
            {
                characterMovement.baseSpeed = 2f;
            }
            
            // scaled ranged damage for Hot enemies
            rangedDamage = Mathf.Clamp(attackDamage * 0.5f, 5f, 10f);
        }

        private void Update()
        {
            if (isDead) return;

            // I re-acquired the player target if it was lost
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }

            float distanceToPlayer = float.MaxValue;
            bool isPlayerAlive = false;

            if (playerTarget != null)
            {
                Health pHealth = playerTarget.GetComponent<Health>();
                isPlayerAlive = pHealth != null && pHealth.CurrentHealth > 0;
                
                if (isPlayerAlive)
                {
                    distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                }
            }

            // Deciding the current state: Attack, Shoot, Chase, or Wander
            if (playerTarget != null && isPlayerAlive && distanceToPlayer <= attackRadius)
            {
                characterMovement.SetMovement(Vector2.zero);
                
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
            }
            else if (enemyType == EnemyType.Hot && playerTarget != null && isPlayerAlive && distanceToPlayer <= detectionRadius && distanceToPlayer > meleeChaseRadius)
            {
                // Handling ranged attacks for Hot enemies (shooting from afar)
                Vector2 directionToPlayer = (playerTarget.position - transform.position).normalized;
                
                // here i am making sure that bullets cannot be shot if there is a wall between the enemy and the player by using the raycast and a message between the enemy and the wall "Wall"
                RaycastHit2D[] lineOfSightHits = Physics2D.RaycastAll(transform.position, directionToPlayer, distanceToPlayer);
                bool wallBlocks = false;
                foreach (var hit in lineOfSightHits)
                {
                    if (hit.collider.gameObject.name.Contains("Wall"))
                    {
                        wallBlocks = true;
                        break;
                    }
                }
                
                if (!wallBlocks)
                {
                    // For HotEnemy after a confirmed line of sight, the enemy stops moving and starts shooting
                    characterMovement.SetMovement(Vector2.zero); 
                    
                    if (Time.time >= lastShotTime + shootingCooldown)
                    {
                        PerformRangedAttack(directionToPlayer);
                        lastShotTime = Time.time;
                    }
                }
                else
                {
                    characterMovement.SetMovement(directionToPlayer);
                }
            }
            else if (distanceToPlayer <= detectionRadius)
            {
                Vector2 chaseDirection = (playerTarget.position - transform.position).normalized;
                characterMovement.SetMovement(chaseDirection);
            }
            else
            {
                UpdateWander();
            }
        }

        /// <summary>
        /// Moves the enemy towards the current wander target. Chooses a new target when time interval is met.
        /// </summary>
        private void UpdateWander()
        {
            if (Time.time >= nextWanderTime)
            {
                SetNextWanderTarget();
            }

            Vector2 directionToTarget = (wanderTarget - (Vector2)transform.position);
            if (directionToTarget.magnitude < 0.2f)
            {
                characterMovement.SetMovement(Vector2.zero);
            }
            else
            {
                characterMovement.SetMovement(directionToTarget.normalized);
            }
        }

        /// <summary>
        /// Picks a new random position within the wander radius for the next movement cycle.
        /// </summary>
        private void SetNextWanderTarget()
        {
            wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
            nextWanderTime = Time.time + wanderInterval + Random.Range(-1f, 1f);
        }

        /// <summary>
        /// Initiates the attack animation and deals damage to the player if still within attack range.
        /// </summary>
        private void PerformAttack()
        {
            Debug.Log($"{gameObject.name} is attempting an attack on player."); // for testing purposes
            characterMovement.Attack();
            
            // I checked if the player is still in range to take damage
            float currentDistance = Vector2.Distance(transform.position, playerTarget.position);
            if (playerTarget != null && currentDistance <= attackRadius + 0.5f)
            {
                Health playerHealth = playerTarget.GetComponent<Health>();
                if (playerHealth != null)
                {
                    Debug.Log($"{gameObject.name} hit the player!"); // for testing purposes
                    playerHealth.TakeDamage(attackDamage);
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} missed (Player too far: {currentDistance})"); // for testing purposes
            }
        }

        /// <summary>
        /// Fires a projectile towards the player (HotEnemy only).
        /// </summary>
        private void PerformRangedAttack(Vector2 direction)
        {
            if (projectilePrefab == null) return;

            // Spawning the projectile slightly in front of the enemy to avoid immediate collisions with itself
            Vector2 spawnPos = (Vector2)transform.position + (direction * 0.5f);
            GameObject bulletObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            
            EnemyProjectile projectile = bulletObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.damage = rangedDamage;
                projectile.Fire(direction);
            }
        }

        /// <summary>
        /// Handles death logic, giving score, unregistering the enemy, and destroying the GameObject.
        /// </summary>

        // Disabled the enemy collider, destroyed the enemy object after a short delay, and disabled this script to stop all behavior immediately.
        private void HandleDeath()
        {
            if (isDead) return;
            isDead = true;

            characterMovement.Die();
            Collider2D enemyCollider = GetComponent<Collider2D>();
            if (enemyCollider != null) enemyCollider.enabled = false;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(scoreValue);
                ScoreManager.Instance.UnregisterEnemy();
            }
            Destroy(gameObject, 0.5f); 
            this.enabled = false;
        }
        
        /// <summary>
        /// Draws detection and attack bounds in the Unity Editor for debugging and setup.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);

            if (enemyType == EnemyType.Hot)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                Gizmos.DrawWireSphere(transform.position, meleeChaseRadius);
            }
        }
    }
}
