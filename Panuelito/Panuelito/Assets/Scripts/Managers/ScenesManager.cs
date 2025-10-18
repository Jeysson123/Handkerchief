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

    [Header("Restore Cache Popup")]
    public GameObject restorePopupPanel;
    public Button restoreYesButton;
    public Button restoreNoButton;
    public TextMeshProUGUI textCache;
    private AudioManager audioManager;

    private void Awake()
    {
        if (mainPanel == null || settingsPanel == null)
            Debug.LogError("❌ MainPanel o SettingsPanel NO asignados en el Inspector!");

        if (playButton == null)
            Debug.LogError("❌ PlayButton NO asignado en el Inspector!");
        if (settingsButton == null)
            Debug.LogError("❌ SettingsButton NO asignado en el Inspector!");

        if (restorePopupPanel != null)
            restorePopupPanel.SetActive(false);
    }

    private void Start()
    {
        // Bloquear orientación a landscape
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // Opcional: desactivar auto-rotación
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        // 🔹 Asignar listeners
        audioManager = FindObjectOfType<AudioManager>();
        playButton?.onClick.AddListener(PlayGame);
        settingsButton?.onClick.AddListener(ShowSettings);
        restoreYesButton?.onClick.AddListener(RestoreGame);
        restoreNoButton?.onClick.AddListener(SkipRestore);

        // 🔹 Revisar si hay partida guardada
        if (GameCacheManager.Instance != null && GameCacheManager.Instance.HasSavedGame())
        {
            GameCacheManager.Instance.DebugPrintCache();
            GameCacheManager.Instance.LoadSettings();
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            restorePopupPanel?.SetActive(true);
            Debug.Log("[ScenesManager] Popup de restauración mostrado ✅");
        }
        else
        {
            mainPanel?.SetActive(true);
            settingsPanel?.SetActive(false);
            restorePopupPanel?.SetActive(false);
        }
    }

    private void Update()
    {
        Debug.Log($"idioma :: {SettingsManager.Instance.LANGUAGE}");
        if( textCache != null ) textCache.text = SettingsManager.Instance.LANGUAGE.Equals("English") ? "¿Do you want to restore the previous game?" : "¿Deseas restaurar la partida anterior?";

        if (playButton != null)
        {
            var textPlay = playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textPlay != null)
                textPlay.text = SettingsManager.Instance.LANGUAGE.Equals("Spanish") ? "Jugar" : "Play";
        }

        if (settingsButton != null)
        {
            var textSettings = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textSettings != null)
                textSettings.text = SettingsManager.Instance.LANGUAGE.Equals("Spanish") ? "Configuraciones" : "Settings";
        }
    }

    #region Paneles
    public void ShowSettings()
    {
        audioManager.PlayChooseSound();
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        restorePopupPanel?.SetActive(false);
    }

    public void ShowMainMenu()
    {
        mainPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        restorePopupPanel?.SetActive(false);
    }
    #endregion

    #region Cache Restore
    private void RestoreGame()
    {
        Debug.Log("[ScenesManager] Restaurando partida desde caché...");

        if (GameCacheManager.Instance != null)
        {
            var judge = FindObjectOfType<Judge>();
            var spawner = FindObjectOfType<HandkerchiefSpawner>();

            if (judge != null && spawner != null)
            {
                GameCacheManager.Instance.RestoreGame(judge);
                spawner.SpawnAll();
                Debug.Log("[ScenesManager] Partida restaurada correctamente ✅");
            }
            else
            {
                Debug.LogWarning("[ScenesManager] No se encontraron referencias de Judge o Spawner ❌");
            }
        }

        restorePopupPanel?.SetActive(false);
        PlayGame();
    }

    private void SkipRestore()
    {
        Debug.Log("[ScenesManager] Restauración cancelada, volviendo al menú.");
        GameCacheManager.Instance.ClearCache();

        restorePopupPanel?.SetActive(false);
        ShowMainMenu();
    }
    #endregion

    #region Juego
    public void PlayGame()
    {
        audioManager.PlayChooseSound();
        restorePopupPanel?.SetActive(false);
        Debug.Log("[ScenesManager] Popup ocultado antes de cargar la escena ✅");

        if ((SettingsManager.Instance.LANGUAGE.Equals("English")
            && SettingsManager.Instance.CURRENT_MAP.Equals("Parking"))
            || (SettingsManager.Instance.LANGUAGE.Equals("Spanish")
            && SettingsManager.Instance.CURRENT_MAP.Equals("Parqueo")))
        {
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            SceneManager.LoadScene("ProesaScene");
        }


        if ((SettingsManager.Instance.LANGUAGE.Equals("English")
            && SettingsManager.Instance.CURRENT_MAP.Equals("Beach"))
            || (SettingsManager.Instance.LANGUAGE.Equals("Spanish")
            && SettingsManager.Instance.CURRENT_MAP.Equals("Playa")))
        {
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            SceneManager.LoadScene("BeachScene");
        }
    }
    #endregion
}
