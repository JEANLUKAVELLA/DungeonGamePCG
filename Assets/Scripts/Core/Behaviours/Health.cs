using UnityEngine;
using UnityEngine.Events;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Manages the health state of any character entity, handling damage, healing,
    /// tracking cumulative damage taken in the current level, and dispatching events on health updates or death.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Tooltip("The maximum health capacity of the character.")]
        [SerializeField] private float maxHealth = 100f;
        
        private float currentHealth;
        
        // Tracks cumulative damage taken specifically during the current dungeon level (for difficulty scaling)
        private float damageTakenThisLevel = 0f;

        /// <summary>
        /// Event triggered when current health changes. Returns (current health, max health).
        /// </summary>
        public UnityEvent<float, float> OnHealthChanged; 
        
        /// <summary>
        /// Event triggered when health drops to zero or below.
        /// </summary>
        public UnityEvent OnDeath;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float DamageTakenThisLevel => damageTakenThisLevel;

        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Applies damage to the entity, checks for death, and fires health events.
        /// </summary>
        /// <param name="amount">The damage value to subtract.</param>
        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            damageTakenThisLevel += amount;
            Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}/{maxHealth}"); // for testing purposes
            
            // Keep health bounds between 0 and maxHealth
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} has died."); // for testing purposes
                Die();
            }
        }

        /// <summary>
        /// Restores health up to the configured maximum value.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Resets the entity's health back to full capacity and clears dead status and level damage history.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            damageTakenThisLevel = 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Internal method that marks the entity as dead and invokes death-related events.
        /// </summary>
        private void Die()
        {
            if (isDead) return;
            isDead = true;
            OnDeath?.Invoke();
        }
    }
}
