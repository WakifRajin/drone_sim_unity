using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName;

    [Header("Panels")]
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ===== Buttons =====

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game scene name is not set in the inspector.");
        }
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OpenAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    public void CloseAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}