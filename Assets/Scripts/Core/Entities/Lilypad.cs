using UnityEngine;
using DungeonGame.Core.Entities;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents a lilypad interactive prop in the Ocean biome.
    /// Stepping onto a lilypad protects the player from water slow effects by incrementing/decrementing LilypadCount.
    /// </summary>
    public class Lilypad : MonoBehaviour
    {
        /// <summary>
        /// Detects when the player steps onto a lilypad, incrementing the player's active lilypad counter.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                PlayerTileEffectController effect = collision.GetComponent<PlayerTileEffectController>();
                if (effect != null)
                {
                    effect.LilypadCount++;
                    Debug.Log($"[Lilypad] Player entered lilypad. Count: {effect.LilypadCount}");
                }
                else
                {
                    Debug.LogError("[Lilypad] PlayerTileEffectController not found on Player!");
                }
            }
        }

        /// <summary>
        /// Detects when the player steps off a lilypad, decrementing their active lilypad counter.
        /// </summary>
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                PlayerTileEffectController effect = collision.GetComponent<PlayerTileEffectController>();
                if (effect != null)
                {
                    effect.LilypadCount--;
                    Debug.Log($"[Lilypad] Player exited lilypad. Count: {effect.LilypadCount}");
                }
            }
        }
    }
}
