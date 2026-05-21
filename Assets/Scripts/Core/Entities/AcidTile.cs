using UnityEngine;
using DungeonGame.Core.Entities;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents an acid hazard tile that periodically deals damage to the player when they stand on it.
    /// </summary>
    public class AcidTile : MonoBehaviour
    {
        [Tooltip("Interval (in seconds) between consecutive damage applications.")]
        [SerializeField] private float damageInterval = 1f;
        
        // Tracks the next game time when damage can be dealt to the player
        private float nextDamageTime;

        /// <summary>
        /// Periodic check while the player remains inside the 2D trigger zone of the acid tile.
        /// </summary>
        /// <param name="collision">The Collider2D that entered the trigger.</param>
        private void OnTriggerStay2D(Collider2D collision)
        {
            // Only affect the Player entity
            if (collision.CompareTag("Player"))
            {
                // Verify if the damage interval cooldown has elapsed
                if (Time.time >= nextDamageTime)
                {
                    Health health = collision.GetComponent<Health>();
                    PlayerTileEffectController effectController = collision.GetComponent<PlayerTileEffectController>();
                    
                    if (health != null && effectController != null)
                    {
                        Debug.Log($"[AcidTile] Dealing {effectController.AcidDamage} damage to player");
                        health.TakeDamage(effectController.AcidDamage);
                        
                        // Set the cooldown timer for the next damage tick
                        nextDamageTime = Time.time + damageInterval;
                    }
                    else
                    {
                        Debug.LogError($"[AcidTile] Missing Health ({health != null}) or PlayerTileEffectController ({effectController != null}) on Player!");
                    }
                }
            }
        }
    }
}
