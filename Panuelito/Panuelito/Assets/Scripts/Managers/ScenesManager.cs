using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ScenesManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject backgroundPanel;

    [Header("Buttons (Assign in Inspector)")]
    public Button playButton;
    public Button settingsButton;

    [Header("Restore Cache Popup")]
    public GameObject restorePopupPanel;
    public Button restoreYesButton;
    public Button restoreNoButton;
    public TextMeshProUGUI textCache;

    private AudioManager audioManager;
    private string endMsg;

    // Lista temporal para objetos del modal
    private List<GameObject> currentModalObjects = new List<GameObject>();

    private void Awake()
    {
        mainPanel = mainPanel ?? GameObject.Find("PanelMain");
        settingsPanel = settingsPanel ?? GameObject.Find("PanelSettings");
        backgroundPanel = backgroundPanel ?? GameObject.Find("PanelBackground");
        restorePopupPanel = restorePopupPanel ?? GameObject.Find("RestorePopupPanel");

        mainPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        backgroundPanel?.SetActive(true);
        restorePopupPanel?.SetActive(false);

        audioManager = FindObjectOfType<AudioManager>();

        if (GameCacheManager.Instance != null && GameCacheManager.Instance.ContainEndMatchResult())
        {
            endMsg = SettingsManager.Instance.LANGUAGE.Equals("English")
                ? "¡Congratulations, thanks for playing!"
                : "¡Felicidades, gracias por jugar!";

            GameCacheManager.Instance.ClearCache("GameCache");
            GameCacheManager.Instance.ClearCache("GameFinished");

            if (!string.IsNullOrEmpty(endMsg))
            {
                StartCoroutine(ShowModalAndLoadScene(endMsg, 3f, "MenuScene"));
            }
            else
            {
                StartCoroutine(LoadSceneAsync("MenuScene"));
            }

            endMsg = string.Empty;
        }
        else
        {
            // Mostrar ads al inicio con delay
            StartCoroutine(ShowAdsWithDelay(1f));
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        Input.multiTouchEnabled = true;

        playButton?.onClick.AddListener(PlayGame);
        settingsButton?.onClick.AddListener(ShowSettings);
        restoreYesButton?.onClick.AddListener(RestoreGame);
        restoreNoButton?.onClick.AddListener(SkipRestore);

        if (GameCacheManager.Instance != null && GameCacheManager.Instance.HasSavedGame("GameCache"))
        {
            GameCacheManager.Instance.LoadSettings();
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            restorePopupPanel?.SetActive(true);
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
        if (textCache != null)
            textCache.text = SettingsManager.Instance.LANGUAGE.Equals("English")
                ? "Do you want to restore the previous game?"
                : "¿Deseas restaurar la partida anterior?";

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

        GameObject modal = GameObject.Find("EndGameModal");
        if (modal != null)
        {
            Destroy(modal, 3f);
            GameObject effect = GameObject.Find("ScreenEffect");
            Destroy(effect, 3f);
        }
    }

    #region Paneles
    public void ShowSettings()
    {
        audioManager?.PlayChooseSound();
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
        if (GameCacheManager.Instance != null)
        {
            var judge = FindObjectOfType<Judge>();
            var spawner = FindObjectOfType<HandkerchiefSpawner>();

            if (judge != null && spawner != null)
            {
                GameCacheManager.Instance.RestoreGame(judge);
                spawner.SpawnAll();
            }
        }

        restorePopupPanel?.SetActive(false);
        PlayGame();
    }

    private void SkipRestore()
    {
        GameCacheManager.Instance.ClearCache("GameCache");
        restorePopupPanel?.SetActive(false);
        ShowMainMenu();
    }
    #endregion

    #region Juego
    public void PlayGame()
    {
        StartCoroutine(ShowAdsWithDelay(1f));

        audioManager?.StopBackgroundMusic();
        audioManager?.PlayChooseSound();
        mainPanel?.SetActive(false);
        restorePopupPanel?.SetActive(false);
        backgroundPanel?.SetActive(false);

        string sceneToLoad = null;

        if ((SettingsManager.Instance.LANGUAGE.Equals("English") && SettingsManager.Instance.CURRENT_MAP.Equals("Parking")) ||
            (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && SettingsManager.Instance.CURRENT_MAP.Equals("Parqueo")))
        {
            sceneToLoad = "ProesaScene";
        }

        if ((SettingsManager.Instance.LANGUAGE.Equals("English") && SettingsManager.Instance.CURRENT_MAP.Equals("Beach")) ||
            (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && SettingsManager.Instance.CURRENT_MAP.Equals("Playa")))
        {
            sceneToLoad = "BeachScene";
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
            StartCoroutine(LoadSceneAsync(sceneToLoad));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;
    }
    #endregion

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuScene")
        {
            mainPanel = mainPanel ?? GameObject.Find("PanelMain");
            settingsPanel = settingsPanel ?? GameObject.Find("PanelSettings");
            backgroundPanel = backgroundPanel ?? GameObject.Find("PanelBackground");
            restorePopupPanel = restorePopupPanel ?? GameObject.Find("RestorePopupPanel");

            mainPanel?.SetActive(true);
            settingsPanel?.SetActive(false);
            backgroundPanel?.SetActive(true);
            restorePopupPanel?.SetActive(false);

            audioManager?.PlayBackgroundMusic();
        }
    }

    #region Modal Dinámico con efecto
    private void ShowEndGameModal(string message, float duration)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Overlay negro transparente
        GameObject overlayGO = new GameObject("ScreenEffect");
        overlayGO.transform.SetParent(canvas.transform);
        Image overlayImage = overlayGO.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        currentModalObjects.Add(overlayGO);

        // Panel blanco con texto
        GameObject panelGO = new GameObject("EndGameModal");
        panelGO.transform.SetParent(canvas.transform);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = Color.white;
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1300, 600);
        panelRect.anchoredPosition = Vector2.zero;
        currentModalObjects.Add(panelGO);

        GameObject textGO = new GameObject("ModalText");
        textGO.transform.SetParent(panelGO.transform);
        TextMeshProUGUI textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = message;
        textTMP.fontSize = 50;
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.color = Color.black;
        textTMP.enableWordWrapping = true;
        textTMP.fontMaterial.EnableKeyword("OUTLINE_ON");
        textTMP.outlineColor = Color.yellow;
        textTMP.outlineWidth = 0.2f;
        RectTransform textRect = textTMP.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        currentModalObjects.Add(textGO);

        // Sonido de victoria
        audioManager?.PlayWinSound();

        // Efecto de confeti
        for (int i = 0; i < 25; i++)
        {
            GameObject confetti = new GameObject("Confetti");
            confetti.transform.SetParent(canvas.transform);
            Image img = confetti.AddComponent<Image>();
            img.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);

            RectTransform r = confetti.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(20, 20);
            r.anchoredPosition = new Vector2(Random.Range(-500, 500), Random.Range(200, 400));

            confetti.AddComponent<MonoBehaviourHelper>().StartCoroutine(FallConfetti(r, duration));
            currentModalObjects.Add(confetti);
        }

        StartCoroutine(HideModalAfterDurationAndClear(duration));
    }

    private IEnumerator ShowModalAndLoadScene(string message, float duration, string sceneName)
    {
        ShowEndGameModal(message, duration);
        yield return new WaitForSeconds(duration);

        foreach (var obj in currentModalObjects)
            if (obj != null) Destroy(obj);
        currentModalObjects.Clear();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;
    }

    private IEnumerator FallConfetti(RectTransform r, float duration)
    {
        float elapsed = 0f;
        Vector2 startPos = r.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(Random.Range(-100, 100), -600);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            r.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            r.Rotate(0, 0, Random.Range(-2f, 2f));
            yield return null;
        }

        if (r != null && r.gameObject != null)
            Destroy(r.gameObject);
    }

    private IEnumerator HideModalAfterDurationAndClear(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Mostrar interstitial justo cuando el modal se cierra
        AdManager adManager = FindObjectOfType<AdManager>();
        if (adManager != null)
        {
            adManager.ShowInterstitial();
        }
        else
        {
            Debug.LogWarning("[ScenesManager] AdManager no encontrado al cerrar modal");
        }

        foreach (var obj in currentModalObjects)
            if (obj != null) Destroy(obj);

        currentModalObjects.Clear();
    }

    private IEnumerator ShowAdsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdManager adManager = FindObjectOfType<AdManager>();
        if (adManager != null)
        {
            adManager.ShowInterstitial();
        }
        else
        {
            Debug.LogWarning("[ScenesManager] AdManager no encontrado");
        }
    }
    #endregion
}

// Helper class para corrutinas fuera de MonoBehaviour
public class MonoBehaviourHelper : MonoBehaviour { }
