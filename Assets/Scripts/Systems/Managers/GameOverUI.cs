using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonGame.Systems.Managers
{
    /// <summary>
    /// Manages the Game Over UI Screen, handling restart commands, 
    /// score stat resets, and exiting the gameplay application.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("The main Game Over canvas overlay panel.")]
        [SerializeField] private GameObject gameOverPanel;
        
        [Header("Buttons")]
        [Tooltip("Button trigger to restart the level.")]
        [SerializeField] private Button restartButton;
        [Tooltip("Button trigger to quit and exit the game.")]
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            // Ensure cursor is visible and unlocked when the scene starts
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Set up button event listeners
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        /// <summary>
        /// Displays the game over panel and unlocks the mouse cursor.
        /// Loads the standalone game over scene if the panel reference is missing.
        /// </summary>
        public void ShowGameOver()
        {
            // If this is an overlay panel (in the same gameplay scene), activate it
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Fallback: load the standalone GameOver scene (assumed build index 2)
                SceneManager.LoadScene(2); 
            }
        }

        /// <summary>
        /// Resets stats and reloads scene 1 (the main gameplay scene). Restores standard timescale.
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("Restarting Game - Loading Gamescene (Index 1)");
            Time.timeScale = 1; // Ensure time isn't frozen from death screen pauses
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetStats();
            }

            SceneManager.LoadScene(1); // Load main gameplay scene
        }

        /// <summary>
        /// Quits the running application or exits editor play mode.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting Game Application...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
