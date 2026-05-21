using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DungeonGame.Core.Entities;
using DungeonGame.Systems.Managers;

namespace DungeonGame.Systems.Managers
{
    /// <summary>
    /// Singleton Manager that bridges game engines (ScoreManager, player Health) with the canvas UI displays.
    /// Updates sliders, text displays, interaction panels, and game over screens dynamically.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Health UI")]
        [Tooltip("Slider displaying player health capacity.")]
        [SerializeField] private Slider healthSlider;
        [Tooltip("Text element displaying current vs max health values.")]
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Stats UI")]
        [Tooltip("Text element displaying cumulative score.")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [Tooltip("Text element displaying remaining enemy count.")]
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [Tooltip("Text element displaying collected vs total keys on the level.")]
        [SerializeField] private TextMeshProUGUI keyCountText;

        [Header("Game Over UI")]
        [Tooltip("UI panel control overlay script to display on player death.")]
        [SerializeField] private GameOverUI gameOverUI;

        [Header("Interaction UI")]
        [Tooltip("Text element displaying context-sensitive help or status notices.")]
        [SerializeField] private TextMeshProUGUI interactionMessageText;
        [Tooltip("Panel container to enable/disable when showing/hiding context prompts.")]
        [SerializeField] private GameObject interactionPanel;

        private bool playerFound = false;
        private Health currentPlayerHealth;
        private bool scoreManagerConnected = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            TryConnectScoreManager();
            TryFindPlayer();

            // I hid the interaction panel by default
            if (interactionPanel != null) interactionPanel.SetActive(false);

            // I added debug checks to make sure the UI layout references are configured correctly
            if (scoreText == null) Debug.LogWarning("UIManager: Score Text reference is missing!"); // for testing purposes
            if (enemyCountText == null) Debug.LogWarning("UIManager: Enemy Count Text reference is missing!"); // for testing purposes
            if (gameOverUI == null) gameOverUI = GetComponentInChildren<GameOverUI>();
        }

        private void Update()
        {
            // I searched for the player if they were missing (e.g. destroyed or not spawned yet)
            if (!playerFound || currentPlayerHealth == null)
            {
                TryFindPlayer();
            }

            // I kept trying to connect to the ScoreManager if it registered late
            if (!scoreManagerConnected)
            {
                TryConnectScoreManager();
            }
        }

        /// <summary>
        /// Registers listeners to ScoreManager events to update text overlays on UI triggers.
        /// </summary>
        private void TryConnectScoreManager()
        {
            ScoreManager sm = ScoreManager.Instance;
            if (sm != null)
            {
                // I cleaned up any stale listeners first
                sm.OnScoreChanged.RemoveListener(UpdateScoreUI);
                sm.OnEnemyCountChanged.RemoveListener(UpdateEnemyCountUI);
                sm.OnKeysChanged.RemoveListener(UpdateKeyCountUI);

                // I added the active listeners
                sm.OnScoreChanged.AddListener(UpdateScoreUI);
                sm.OnEnemyCountChanged.AddListener(UpdateEnemyCountUI);
                sm.OnKeysChanged.AddListener(UpdateKeyCountUI);
                
                // I initialized the UI text fields with the current values
                UpdateScoreUI(sm.GetScore());
                UpdateEnemyCountUI(sm.GetEnemyCount());
                UpdateKeyCountUI(sm.GetKeysCollected(), sm.GetTotalKeys());
                
                scoreManagerConnected = true;
                Debug.Log("UIManager: Connected to ScoreManager and listening for keys."); // for testing purposes
            }
        }

        /// <summary>
        /// Searches the scene hierarchy for the Player object and hooks onto its Health component events.
        /// </summary>
        private void TryFindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // I cleaned up old listeners since the player reference changed
                    if (currentPlayerHealth != null && currentPlayerHealth != playerHealth)
                    {
                        currentPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
                        currentPlayerHealth.OnDeath.RemoveListener(HandlePlayerDeath);
                    }

                    currentPlayerHealth = playerHealth;
                    
                    // I bound the health bar and death event updates
                    currentPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
                    currentPlayerHealth.OnHealthChanged.AddListener(UpdateHealthUI);

                    currentPlayerHealth.OnDeath.RemoveListener(HandlePlayerDeath);
                    currentPlayerHealth.OnDeath.AddListener(HandlePlayerDeath);
                    
                    UpdateHealthUI(currentPlayerHealth.CurrentHealth, currentPlayerHealth.MaxHealth);
                    playerFound = true;
                    Debug.Log("UIManager: Player found and health bar connected."); // for testing purposes
                }
            }
        }

        /// <summary>
        /// Handles UI behaviors immediately following the player's death event.
        /// </summary>
        private void HandlePlayerDeath()
        {
            Debug.Log("UIManager: Player death detected! Waiting for animation..."); // for testing purposes
            StartCoroutine(DelayedGameOver(2f)); // I added a 2 second delay to let the death animation play out
        }

        /// <summary>
        /// Delay coroutine that reveals the GameOver layout panel.
        /// </summary>
        private IEnumerator DelayedGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (gameOverUI != null)
            {
                gameOverUI.ShowGameOver();
            }
            else
            {
                Debug.Log("UIManager: Loading Gameover scene (Index 2)"); // for testing purposes
                UnityEngine.SceneManagement.SceneManager.LoadScene(2);
            }
        }

        /// <summary>
        /// Displays a notification message using the interaction overlay.
        /// </summary>
        /// <param name="message">The text string to display.</param>
        public void ShowInteractionMessage(string message)
        {
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(true);
                // I forced a scale reset in case of animator conflicts
                interactionPanel.transform.localScale = Vector3.one;
                Debug.Log($"UIManager: Showing interaction panel. Scale is now {interactionPanel.transform.localScale}"); // for testing purposes
            }
            else
            {
                Debug.LogError("UIManager: Interaction Panel is NOT assigned in the Inspector!"); // for testing purposes
            }

            if (interactionMessageText != null)
            {
                interactionMessageText.text = message;
                interactionMessageText.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hides the interaction overlay panel from view.
        /// </summary>
        public void HideInteractionMessage()
        {
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
                Debug.Log("UIManager: Hiding interaction panel."); // for testing purposes
            }
        }

        /// <summary>
        /// Updates the health slider value and the accompanying fraction text.
        /// </summary>
        public void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(currentHealth)} / {maxHealth}";
            }
        }

        /// <summary>
        /// Updates the current score text value.
        /// </summary>
        public void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        /// <summary>
        /// Updates the remaining enemy count text display.
        /// </summary>
        public void UpdateEnemyCountUI(int count)
        {
            if (enemyCountText != null)
            {
                enemyCountText.text = $"Enemies: {count}";
            }
        }

        /// <summary>
        /// Updates the collected key ratio display.
        /// </summary>
        public void UpdateKeyCountUI(int collected, int total)
        {
            if (keyCountText != null)
            {
                keyCountText.text = $"Keys: {collected} / {total}";
                keyCountText.transform.localScale = Vector3.one;
                Debug.Log($"UIManager: Updated key display to 'Keys: {collected} / {total}'. Object: {keyCountText.name}"); // for testing purposes
            }
            else
            {
                Debug.LogError("UIManager: Key Count Text is NOT assigned in the Inspector!"); // for testing purposes
            }
        }
    }
}
