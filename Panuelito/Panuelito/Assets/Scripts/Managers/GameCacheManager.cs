using UnityEngine;
using System.Collections.Generic;

public class GameCacheManager : MonoBehaviour
{
    public static GameCacheManager Instance { get; private set; }

    [System.Serializable]
    public class GameData
    {
        public int playerScore;
        public int aiScore;
        public string difficulty;
        public string map;
        public int sound;
        public int pointsToWin;
        public string language;
        public List<string> teamA;
        public List<string> teamB;
    }

    private const string CACHE_KEY = "GameCache";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasSavedGame()
    {
        return PlayerPrefs.HasKey(CACHE_KEY);
    }

    public void SaveGame(Judge judge, HandkerchiefSpawner spawner)
    {
        GameData data = new GameData
        {
            playerScore = judge.GetPlayerScore(),
            aiScore = judge.GetAIScore(),
            difficulty = SettingsManager.Instance.DIFFICULT,
            map = SettingsManager.Instance.CURRENT_MAP,
            sound = SettingsManager.Instance.SOUND_LEVEL,
            pointsToWin = SettingsManager.Instance.POINTS_TO_WIN,
            language = SettingsManager.Instance.LANGUAGE,
            teamA = new List<string>(),
            teamB = new List<string>()
        };
        if (spawner != null)
        {
            foreach (var p in spawner.teamAPlayers)
                if (p != null) data.teamA.Add(p.name.Replace("(Clone)", ""));
            foreach (var p in spawner.teamBPlayers)
                if (p != null) data.teamB.Add(p.name.Replace("(Clone)", ""));
        }
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(CACHE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("💾 Partida guardada en caché");
    }

    public GameData LoadGame()
    {
        if (!HasSavedGame()) return null;
        string json = PlayerPrefs.GetString(CACHE_KEY);
        return JsonUtility.FromJson<GameData>(json);
    }


    public void LoadSettings()
    {
        var data = LoadGame();
        SettingsManager.Instance.LANGUAGE = data.language;
        SettingsManager.Instance.CURRENT_MAP = data.map;
        SettingsManager.Instance.DIFFICULT = data.difficulty;
        SettingsManager.Instance.SOUND_LEVEL = data.sound;
        SettingsManager.Instance.POINTS_TO_WIN = data.pointsToWin;
    }

    public void ClearCache()
    {
        PlayerPrefs.DeleteKey(CACHE_KEY);
        PlayerPrefs.Save();
        Debug.Log("🧹 Caché de partida eliminada");
    }

    // ✅ Restaurar puntuaciones desde el caché Debug.Log($"Language: {data.language}");
    public void RestoreGame(Judge judge)
    {
        var data = LoadGame();
        if (data != null)
        {
            judge.SetScores(data.playerScore, data.aiScore);
            Debug.Log("♻️ Puntuaciones restauradas desde caché");
        }
    }

    // ✅ NUEVO — Imprime todo el contenido del cache en consola
    public void DebugPrintCache()
    {
        var data = LoadGame();
        if (data == null)
        {
            Debug.Log("⚠️ No hay datos en caché para imprimir");
            return;
        }

        Debug.Log("=== 🔹 Game Cache Start 🔹 ===");
        Debug.Log($"Language: {data.language}");
        Debug.Log($"Map: {data.map}");
        Debug.Log($"Difficulty: {data.difficulty}");
        Debug.Log($"Points to Win: {data.pointsToWin}");
        Debug.Log($"Player Score: {data.playerScore}");
        Debug.Log($"AI Score: {data.aiScore}");

        if (data.teamA != null)
        {
            Debug.Log("Team A Players:");
            for (int i = 0; i < data.teamA.Count; i++)
                Debug.Log($"  {i}: {data.teamA[i]}");
        }

        if (data.teamB != null)
        {
            Debug.Log("Team B Players:");
            for (int i = 0; i < data.teamB.Count; i++)
                Debug.Log($"  {i}: {data.teamB[i]}");
        }

        Debug.Log("=== 🔹 Game Cache End 🔹 ===");
    }
}
