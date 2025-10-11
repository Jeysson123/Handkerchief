using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("UI References")]
    public TextMeshProUGUI pointsValueText;
    public TextMeshProUGUI difficultyValueText;
    public TextMeshProUGUI mapValueText;
    public TextMeshProUGUI soundValueText;

    [Header("Buttons")]
    public Button pointsMinusButton;
    public Button pointsPlusButton;
    public Button difficultyPrevButton;
    public Button difficultyNextButton;
    public Button mapPrevButton;
    public Button mapNextButton;
    public Button soundMinusButton;
    public Button soundPlusButton;
    public Button saveButton;

    private int points;
    private int sound;
    private string[] difficulties = { "Easy", "Normal", "Hard" };
    private int difficultyIndex;
    private string[] maps = { "Parking", "Beach" };
    private int mapIndex;

    // Variables públicas globales
    public int POINTS_TO_WIN = 5;
    public string DIFFICULT = "Hard";
    public string CURRENT_MAP = "Beach";
    public int SOUND_LEVEL = 30; // 0 - 100

    private void Awake()
    {
        // Singleton
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
        points = POINTS_TO_WIN;
        sound = SOUND_LEVEL;

        difficultyIndex = System.Array.IndexOf(difficulties, DIFFICULT);
        if (difficultyIndex < 0) difficultyIndex = 0;

        mapIndex = System.Array.IndexOf(maps, CURRENT_MAP);
        if (mapIndex < 0) mapIndex = 0;

        UpdateUI();
        AddListeners();
    }

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
        AddButtonListener(saveButton, SaveSettings);
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void ChangePoints(int delta)
    {
        points = Mathf.Clamp(points + delta, 0, 10);
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

    private void UpdateUI()
    {
        if (pointsValueText != null) pointsValueText.text = points.ToString();
        if (difficultyValueText != null) difficultyValueText.text = difficulties[difficultyIndex];
        if (mapValueText != null) mapValueText.text = maps[mapIndex];
        if (soundValueText != null) soundValueText.text = sound + "%";

        POINTS_TO_WIN = points;
        DIFFICULT = difficulties[difficultyIndex];
        CURRENT_MAP = maps[mapIndex];
        SOUND_LEVEL = sound;
    }

    private void SaveSettings()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        Debug.Log($"💾 Settings saved: Points={points}, Difficulty={difficulties[difficultyIndex]}, Map={maps[mapIndex]}, Sound={sound}%");
    }
}
