using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScenesManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Buttons (Assign in Inspector)")]
    public Button playButton;
    public Button settingsButton;

    private void Awake()
    {
        if (mainPanel == null || settingsPanel == null)
            Debug.LogError("❌ MainPanel o SettingsPanel NO asignados en el Inspector!");

        if (playButton == null)
            Debug.LogError("❌ PlayButton NO asignado en el Inspector!");
        if (settingsButton == null)
            Debug.LogError("❌ SettingsButton NO asignado en el Inspector!");
    }

    private void Start()
    {
        // 🔹 Limpio y directo
        if (playButton != null)
        {
            playButton.onClick.AddListener(() =>
            {
                PlayGame();
            });
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() =>
            {
                ShowSettings();
            });
        }

        ShowMainMenu();
    }


    private void Update()
    {
        TextMeshProUGUI textPlay = playButton.GetComponentInChildren<TextMeshProUGUI>();
        textPlay.text = SettingsManager.Instance.LANGUAGE.Equals("Spanish") ? "Jugar" : "Play";

        TextMeshProUGUI textSettings = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
        textSettings.text = SettingsManager.Instance.LANGUAGE.Equals("Spanish") ? "Configuraciones" : "Settings";
    }

    public void ShowSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        //PROESA 
        if (SettingsManager.Instance.LANGUAGE.Equals("English")
            && SettingsManager.Instance.CURRENT_MAP.Equals("Parking")
            || SettingsManager.Instance.LANGUAGE.Equals("Spanish")
          && SettingsManager.Instance.CURRENT_MAP.Equals("Parqueo"))
        {
            SceneManager.LoadScene("ProesaScene");
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        //PLAYA
    }
}
