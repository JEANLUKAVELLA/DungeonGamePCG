using UnityEngine;
using DungeonGame.Systems.Managers;
using DungeonGame.Systems.Dungeon;
using TMPro;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents the exit portal in the dungeon room. 
    /// Verifies that all stage keys are collected and all enemies are defeated before allowing the player to teleport to the next level.
    /// </summary>
    public class ExitTeleporter : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Distance threshold within which the player must stand to interact with the teleporter.")]
        [SerializeField] private float activationRadius = 1.5f;
        [Tooltip("The keyboard key mapped to activate the portal.")]
        [SerializeField] private string interactionKey = "e";

        private bool playerInRange = false;

        private void Awake()
        {
            Debug.Log($"ExitTeleporter: Script active on {gameObject.name}"); // for testing purposes
            
            // I made sure the exit is rendered above floors but underneath characters
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 5;
            }
        }

        private void Update()
        {
            // I triggered the activation attempt when the player is nearby and presses the interaction button
            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                Debug.Log($"ExitTeleporter: Player pressed '{interactionKey}' interaction key."); // for testing purposes
                TryActivateExit();
            }
        }

        /// <summary>
        /// Validates progression criteria (all keys collected and all enemies defeated).
        /// If criteria are met, transitions the level, otherwise displays requirements.
        /// </summary>
        private void TryActivateExit()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("ExitTeleporter: ScoreManager Instance is null!"); // for testing purposes
                return;
            }

            bool allEnemiesDead = ScoreManager.Instance.AllEnemiesDefeated();
            bool allKeysCollected = ScoreManager.Instance.AllKeysCollected();

            Debug.Log($"ExitTeleporter: Checking requirements - Enemies Dead: {allEnemiesDead}, Keys Collected: {allKeysCollected}"); // for testing purposes

            if (allEnemiesDead && allKeysCollected)
            {
                Debug.Log("ExitTeleporter: SUCCESS! Exit activated."); // for testing purposes
                if (DungeonGenerator.Instance != null)
                {
                    DungeonGenerator.Instance.TransitionToNextLevel();
                }
                else
                {
                    UIManager.Instance.ShowInteractionMessage("Exit Activated! Moving to next level...");
                    Debug.LogWarning("ExitTeleporter: DungeonGenerator.Instance is null!"); // for testing purposes
                }
            }
            else
            {
                // I formulated feedback messages for the player
                string message = "";
                if (!allEnemiesDead && !allKeysCollected)
                {
                    message = "Enemies still roaming and keys missing!";
                }
                else if (!allEnemiesDead)
                {
                    message = "Still enemies alive!";
                }
                else if (!allKeysCollected)
                {
                    message = "Not enough key crystals!";
                }
                
                Debug.Log($"ExitTeleporter: FAILED. Message: {message}"); // for testing purposes
                UIManager.Instance.ShowInteractionMessage(message);
            }
        }

        /// <summary>
        /// Sets trigger status and displays help banner when the player walks into the teleporter's collision box.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"ExitTeleporter: Player ENTERED trigger. Setting playerInRange = true."); // for testing purposes
                playerInRange = true;
                UIManager.Instance.ShowInteractionMessage("Press E to exit");
            }
            else
            {
                Debug.Log($"ExitTeleporter: Something other than Player ({other.name}) entered trigger."); // for testing purposes
            }
        }

        /// <summary>
        /// Resets trigger status and hides prompt banner when the player walks away.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"ExitTeleporter: Player EXITED trigger. Setting playerInRange = false."); // for testing purposes
                playerInRange = false;
                UIManager.Instance.HideInteractionMessage();
            }
        }
    }
}
