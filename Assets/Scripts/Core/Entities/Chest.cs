using UnityEngine;
using DungeonGame.Systems.Managers;
using System.Collections;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Represents an interactive treasure chest that grants the player score, heals them, 
    /// triggers opening animations, and displays an on-screen message.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The amount of score added to the player's total when they open this chest.")]
        [SerializeField] private int bonusScore = 100;
        [Tooltip("The amount of health restored to the player when they open this chest.")]
        [SerializeField] private float healAmount = 20f;
        [Tooltip("The keyboard key used to interact with the chest.")]
        [SerializeField] private string interactionKey = "e";
        [Tooltip("Duration in seconds that the reward message is shown on screen.")]
        [SerializeField] private float displayMessageTime = 3f;

        private bool playerInRange = false;
        private bool isOpen = false;
        private Animator animator;
        private Health playerHealth;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            
            // I made sure the chest is rendered above floors but below players
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 5;
            }
        }

        private void Update()
        {
            // I checked if the player is in range, chest is closed, and the interact key was pressed
            if (playerInRange && !isOpen && Input.GetKeyDown(interactionKey))
            {
                OpenChest();
            }
        }

        /// <summary>
        /// Opens the chest, triggering the animation, granting score, healing the player, and showing a notification.
        /// </summary>
        private void OpenChest()
        {
            isOpen = true;

            // I triggered the animation state in the child animator
            if (animator != null)
            {
                animator.SetBool("IsActive", true);
            }

            // I gave the player rewards by adding score to the ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(bonusScore);
            }

            // I healed the player
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
            }

            // I showed a UI message detailing the rewards
            StartCoroutine(ShowRewardMessage());
        }

        /// <summary>
        /// Coroutine that shows chest reward details on screen for a limited time.
        /// </summary>
        private IEnumerator ShowRewardMessage()
        {
            string message = $"Chest Opened!\n+{bonusScore} Score\n+{healAmount} HP";
            UIManager.Instance.ShowInteractionMessage(message);
            
            yield return new WaitForSeconds(displayMessageTime);
            
            UIManager.Instance.HideInteractionMessage();
        }

        /// <summary>
        /// Detects when the player enters the chest's range.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isOpen && other.CompareTag("Player"))
            {
                playerInRange = true;
                playerHealth = other.GetComponent<Health>();
                UIManager.Instance.ShowInteractionMessage("Press E to open chest");
            }
        }

        /// <summary>
        /// Detects when the player exits the chest's range.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                playerHealth = null;
                if (!isOpen)
                {
                    UIManager.Instance.HideInteractionMessage();
                }
            }
        }
    }
}
