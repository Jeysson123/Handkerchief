using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("UI Labels")]
    public TextMeshProUGUI pointsValueLabel;
    public TextMeshProUGUI difficultyValueLabel;
    public TextMeshProUGUI mapValueLabel;
    public TextMeshProUGUI soundValueLabel;
    public TextMeshProUGUI languageValueLabel;

    [Header("UI Texts")]
    public TextMeshProUGUI pointsValueText;
    public TextMeshProUGUI difficultyValueText;
    public TextMeshProUGUI mapValueText;
    public TextMeshProUGUI soundValueText;
    public TextMeshProUGUI languageValueText;

    [Header("Buttons")]
    public Button pointsMinusButton;
    public Button pointsPlusButton;
    public Button difficultyPrevButton;
    public Button difficultyNextButton;
    public Button mapPrevButton;
    public Button mapNextButton;
    public Button soundMinusButton;
    public Button soundPlusButton;
    public Button languageMinusButton;
    public Button languagePlusButton;
    public Button saveButton;

    private int points;
    private int sound;

    private string[] difficulties;
    private int difficultyIndex;

    private string[] maps;
    private int mapIndex;

    private string[] languages = { "English", "Spanish" };
    private int languageIndex;

    // Public global variables
    public int POINTS_TO_WIN = 5;
    public string DIFFICULT = "Easy";
    public string CURRENT_MAP = "Parking";
    public int SOUND_LEVEL = 30; // 0 - 100
    public string LANGUAGE = "Spanish";

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeLanguageData();

        points = POINTS_TO_WIN;
        sound = SOUND_LEVEL;

        difficultyIndex = System.Array.IndexOf(difficulties, DIFFICULT);
        if (difficultyIndex < 0) difficultyIndex = 0;

        mapIndex = System.Array.IndexOf(maps, CURRENT_MAP);
        if (mapIndex < 0) mapIndex = 0;

        languageIndex = System.Array.IndexOf(languages, LANGUAGE);
        if (languageIndex < 0) languageIndex = 0;

        UpdateUI();
        AddListeners();
    }

    private void InitializeLanguageData()
    {
        difficulties = LANGUAGE.Equals("Spanish")
            ? new[] { "Facil", "Normal", "Dificil" }
            : new[] { "Easy", "Normal", "Hard" };

        maps = LANGUAGE.Equals("Spanish")
            ? new[] { "Parqueo", "Playa" }
            : new[] { "Parking", "Beach" };
    }

    // Add listeners to buttons
    private void AddListeners()
    {
        AddButtonListener(pointsMinusButton, () => ChangePoints(-1));
        AddButtonListener(pointsPlusButton, () => ChangePoints(1));
        AddButtonListener(difficultyPrevButton, () => ChangeDifficulty(-1));
        AddButtonListener(difficultyNextButton, () => ChangeDifficulty(1));
        AddButtonListener(mapPrevButton, () => ChangeMap(-1));
        AddButtonListener(mapNextButton, () => ChangeMap(1));
        AddButtonListener(soundMinusButton, () => ChangeSound(-5));
        AddButtonListener(soundPlusButton, () => ChangeSound(5));
        AddButtonListener(languageMinusButton, () => ChangeLanguage(-1));
        AddButtonListener(languagePlusButton, () => ChangeLanguage(1));
        AddButtonListener(saveButton, SaveSettings);
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    // Change methods
    private void ChangePoints(int delta)
    {
        // 🔹 Sin límite superior, solo evita que sea negativo
        points = Mathf.Max(points + delta, 0);
        UpdateUI();
    }

    private void ChangeDifficulty(int delta)
    {
        difficultyIndex = (difficultyIndex + delta + difficulties.Length) % difficulties.Length;
        UpdateUI();
    }

    private void ChangeMap(int delta)
    {
        mapIndex = (mapIndex + delta + maps.Length) % maps.Length;
        UpdateUI();
    }

    private void ChangeSound(int delta)
    {
        sound = Mathf.Clamp(sound + delta, 0, 100);
        UpdateUI();
    }

    private void ChangeLanguage(int delta)
    {
        languageIndex = (languageIndex + delta + languages.Length) % languages.Length;
        LANGUAGE = languages[languageIndex];

        InitializeLanguageData();
        UpdateUI();
    }

    // Update UI
    private void UpdateUI()
    {
        if (pointsValueText != null) pointsValueText.text = points.ToString();
        if (difficultyValueText != null) difficultyValueText.text = difficulties[difficultyIndex];
        if (mapValueText != null) mapValueText.text = maps[mapIndex];
        if (soundValueText != null) soundValueText.text = sound + "%";
        if (languageValueText != null) languageValueText.text = LANGUAGE;

        POINTS_TO_WIN = points;
        DIFFICULT = difficulties[difficultyIndex];
        CURRENT_MAP = maps[mapIndex];
        SOUND_LEVEL = sound;

        UpdateUIByLanguage();
    }

    private void UpdateUIByLanguage()
    {
        // Labels
        pointsValueLabel.text = LANGUAGE.Equals("Spanish") ? "Puntos" : "Points";
        difficultyValueLabel.text = LANGUAGE.Equals("Spanish") ? "Dificultad" : "Difficulty";
        mapValueLabel.text = LANGUAGE.Equals("Spanish") ? "Mapa" : "Map";
        soundValueLabel.text = LANGUAGE.Equals("Spanish") ? "Sonido" : "Sound";
        languageValueLabel.text = LANGUAGE.Equals("Spanish") ? "Idioma" : "Language";

        // Button text
        TextMeshProUGUI buttonText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = LANGUAGE.Equals("Spanish") ? "Guardar" : "Save";
    }

    // Save settings
    private void SaveSettings()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        Debug.Log($"💾 Settings saved: Points={points}, Difficulty={difficulties[difficultyIndex]}, Map={maps[mapIndex]}, Sound={sound}%, Language={LANGUAGE}");
    }
}
