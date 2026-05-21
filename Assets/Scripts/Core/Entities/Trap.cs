using UnityEngine;
using System.Collections;
using DungeonGame.Core.Entities;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents a trap hazard (like spikes) that periodicially cycles between active (dangerous, visible) 
    /// and hidden (safe, invisible) states, dealing damage or instant death to the player on contact.
    /// </summary>
    public class Trap : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The damage dealt to the player when the trap is active.")]
        [SerializeField] private float damage = 20f;
        [Tooltip("If true, walking into the active trap instantly kills the player.")]
        [SerializeField] private bool instantDeath = false;
        [Tooltip("How long the trap remains active and dangerous in seconds.")]
        [SerializeField] private float activeDuration = 3f;
        [Tooltip("How long the trap remains hidden and safe in seconds.")]
        [SerializeField] private float hiddenDuration = 3f;
        [Tooltip("Initial delay before starting the active/hidden cycle.")]
        [SerializeField] private float startDelay = 0f;
        [Tooltip("Cooldown period in seconds before the trap can deal damage to the same player again.")]
        [SerializeField] private float damageCooldown = 5f;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Collider2D trapCollider;
        private bool isActive = false;
        private float lastDamageTime;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            trapCollider = GetComponent<Collider2D>();
            animator = GetComponentInChildren<Animator>();

            // Ensure traps are rendered above floor tiles but underneath characters
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 5;
            }
        }

        private void Start()
        {
            Debug.Log($"[Trap] Starting trap cycle on {gameObject.name}");
            StartCoroutine(TrapCycle());
        }

        /// <summary>
        /// Coroutine driving the periodic active/hidden trap lifecycle loop.
        /// </summary>
        private IEnumerator TrapCycle()
        {
            yield return new WaitForSeconds(startDelay);

            while (true)
            {
                // Show Trap (Turn on hazard)
                SetTrapState(true);
                yield return new WaitForSeconds(activeDuration);

                // Hide Trap (Turn off hazard)
                SetTrapState(false);
                yield return new WaitForSeconds(hiddenDuration);
            }
        }

        /// <summary>
        /// Enables or disables the trap hazard state, updating animations, sprite visibility, and collider switches.
        /// </summary>
        private void SetTrapState(bool active)
        {
            isActive = active;
            
            if (animator != null)
            {
                Debug.Log($"[Trap] Setting IsActive to {active} on {gameObject.name}");
                animator.SetBool("IsActive", active);
                // Keep the renderer enabled when using animator; the animation clips handle visibility/visuals
            }
            else if (spriteRenderer != null) 
            {
                spriteRenderer.enabled = active;
            }

            if (trapCollider != null) trapCollider.enabled = active;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActive && other.CompareTag("Player"))
            {
                TryApplyDamage(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isActive && other.CompareTag("Player"))
            {
                TryApplyDamage(other);
            }
        }

        /// <summary>
        /// Checks cooldown/instant death settings and deals damage to the player if eligible.
        /// </summary>
        private void TryApplyDamage(Collider2D other)
        {
            if (Time.time >= lastDamageTime + damageCooldown || instantDeath)
            {
                Health playerHealth = other.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // Deal either instant death (equal to player's current health) or configured damage amount
                    float amount = instantDeath ? playerHealth.CurrentHealth : damage;
                    Debug.Log($"Trap: Dealing {amount} damage to Player. Next damage in {damageCooldown}s");
                    playerHealth.TakeDamage(amount);
                    lastDamageTime = Time.time;
                }
            }
        }
    }
}
