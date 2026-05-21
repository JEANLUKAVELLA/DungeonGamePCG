using UnityEngine;
using DungeonGame.Systems.Managers;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents a collectible key crystal item required to unlock the dungeon exit.
    /// Registers with the ScoreManager on spawn and handles collection by the player.
    /// </summary>
    public class KeyItem : MonoBehaviour
    {
        [Tooltip("The score points awarded when this key is collected.")]
        [SerializeField] private int scoreValue = 50;

        private void Start()
        {
            // Register this key with the ScoreManager to dynamically count level targets
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterKey();
            }
        }

        /// <summary>
        /// Collects the key when colliding with the Player, rewarding points, updating the registry, and deleting the item.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"KeyItem: Collided with {other.name} (Tag: {other.tag})");
            if (other.CompareTag("Player"))
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.CollectKey();
                    ScoreManager.Instance.AddScore(scoreValue);
                }
                
                Debug.Log("KeyItem: Collected by Player!");
                Destroy(gameObject);
            }
        }
    }
}
