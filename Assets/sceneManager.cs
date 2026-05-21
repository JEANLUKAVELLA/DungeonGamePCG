using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the Main Menu UI, including playing the game (loading the next scene) and quitting the application.
/// </summary>
public class sceneManager : MonoBehaviour
{
    // MAIN MENU
    [Header("Main Menu UI")]
    [Tooltip("Button to start the game by loading the first gameplay scene.")]
    public Button playButton;
    [Tooltip("Button to quit the game application.")]
    public Button quitButton;

    [Tooltip("The main menu panel GameObject containing UI elements.")]
    public GameObject mainMenu;
    [Tooltip("The parent canvas for the start menu to be destroyed on play.")]
    public GameObject startMenuCanvas;

    private void Start()
    {
        SetupMenuButtons();
    }

    /// <summary>
    /// Configures the click listeners for play and quit buttons, and activates the main menu panel.
    /// </summary>
    void SetupMenuButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    /// <summary>
    /// Callback triggered when the play button is clicked. Destroys the main menu canvas and loads the next scene in the build index.
    /// </summary>
    void OnPlayClicked()
    {
        Debug.Log("Game State: Playing"); // Testing

        if (startMenuCanvas != null)
            Destroy(startMenuCanvas);

        // Load the gameplay scene, which is expected to be next in the build settings
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Quits the application. Exits play mode if running inside the Unity Editor, otherwise closes the standalone application.
    /// </summary>
    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
