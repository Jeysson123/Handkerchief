using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Judge : MonoBehaviour
{
    private enum Team { None, Player, AI }

    [Header("Marcadores UI")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI aiScoreText;

    [Header("Configuración")]
    public float baseTolerance = 1.5f;
    public float extraTolerance = 1.0f;
    public float interceptDistance = 2.5f;

    [Header("Línea de puntuación del jugador (usar XZ)")]
    public Vector3 playerLineTeamA = new Vector3(-35.32088f, 0f, -27.62183f);

    private int playerScore = 0;
    private int aiScore = 0;

    private HandkerchiefSpawner spawner;
    private PlayerMovement playerMovement;
    private AIController aIController;
    private Vector3 playerBasePos;
    private Vector3 aiBasePos;
    private bool roundEnded = false;
    private Team lastTouched = Team.None;
    private Transform prevHolder = null;

    private Dictionary<Transform, Vector3> originalPosByTransform = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector2> pickupXZByHolder = new Dictionary<Transform, Vector2>();
    private Dictionary<Transform, float> progressTowardsBase = new Dictionary<Transform, float>();
    private DialogAndEffectsManager effectsManager;

    private List<GameObject> hiddenPlayers = new List<GameObject>();
    private CinematicCameraController cinematicCamera;
    private bool updateIATextPosition = true;
    private AudioManager audioManager;

    private void Start()
    {
        spawner = FindObjectOfType<HandkerchiefSpawner>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        aIController = FindObjectOfType<AIController>();
        effectsManager = FindObjectOfType<DialogAndEffectsManager>();
        cinematicCamera = FindObjectOfType<CinematicCameraController>();
        audioManager = FindObjectOfType<AudioManager>();

        if (spawner != null)
        {
            playerBasePos = spawner.teamAPosition;
            aiBasePos = spawner.teamBPosition;
            RegisterPlayerPositions(spawner.teamAPlayers);
            RegisterPlayerPositions(spawner.teamBPlayers);
        }

        UpdateScoreUI();
    }

    public void ValidateRightPosition(int selectedIndex, int rightIndex)
    {
        int indexFormated = selectedIndex == 2 ? 3 : selectedIndex + 1;

        if (indexFormated != rightIndex)
        {
            Transform iatransform = aIController.currentAICharacter.transform;
            AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player selected, wrong index → AI point +1."
                : "Jugador selecciono , index equivocado → punto IA +1.", iatransform);
        }
    }

    private void RegisterPlayerPositions(List<GameObject> list)
    {
        if (list == null) return;
        foreach (var g in list)
        {
            if (g != null && g.transform != null && !originalPosByTransform.ContainsKey(g.transform))
                originalPosByTransform[g.transform] = g.transform.position;
        }
    }

    private void Update()
    {
        if (roundEnded || spawner == null || spawner.Handkerchief == null) return;

        Transform hkTransform = spawner.Handkerchief.transform;
        Transform holder = FindRootHolder(hkTransform);

        if (holder != prevHolder)
        {
            if (prevHolder != null)
            {
                pickupXZByHolder.Remove(prevHolder);
                progressTowardsBase.Remove(prevHolder);
            }

            if (holder != null)
            {
                Transform root = FindRootHolder(holder);
                if (root != null)
                {
                    if (IsTransformInList(root, spawner.teamAPlayers))
                    {
                        lastTouched = Team.Player;
                        if (!pickupXZByHolder.ContainsKey(root))
                        {
                            pickupXZByHolder[root] = new Vector2(root.position.x, root.position.z);
                            progressTowardsBase[root] = 0f;
                        }
                    }
                    else if (IsTransformInList(root, spawner.teamBPlayers))
                        lastTouched = Team.AI;
                    else
                        lastTouched = Team.None;
                }
            }
            else lastTouched = Team.None;

            prevHolder = holder;
        }

        if (holder == null) return;

        bool holderIsTeamA = IsTransformInList(holder, spawner.teamAPlayers);
        bool holderIsTeamB = IsTransformInList(holder, spawner.teamBPlayers);

        // --- LÓGICA IA ---
        if (holderIsTeamB)
        {
            GameObject nearestA = FindNearestInListWithDistance(holder.position, spawner.teamAPlayers, out float nearestADist);
            if (nearestA != null && nearestADist <= interceptDistance + 2f)
            {
                cinematicCamera.PlayCinematic(playerMovement.currentCharacter.transform);
            }

            if (nearestA != null && nearestADist <= interceptDistance)
            {
                playerMovement.MoveRightArm();
                HideOtherPlayers(nearestA.transform);
                cinematicCamera.PlayCinematic(playerMovement.currentCharacter.transform);
                AddPointToPlayer(SettingsManager.Instance.LANGUAGE.Equals("English") ? $"Player ({nearestA.name}) intercepted the AI, → point PLAYER +1."
                    : $"Jugador ({nearestA.name}) interceptó a la IA, → punto JUGADOR +1.", nearestA.transform);
                return;
            }

            if (aIController.currentAICharacter != null)
            {
                Transform iatransform = aIController.currentAICharacter.transform;
                if (iatransform.position.z >= 112.12)
                {
                    HideOtherPlayers(iatransform);
                    AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? "IA crossed the score line → point IA +1"
                        : "IA cruzó la línea de puntuación → punto IA +1.", iatransform);
                    return;
                }
            }

            if (IsWithinBaseArea(holder, Team.AI))
            {
                Transform winner = lastTouched == Team.AI || lastTouched == Team.None ? holder : FindNearestInListWithDistance(holder.position, spawner.teamAPlayers, out _).transform;
                HideOtherPlayers(winner);
                if (lastTouched == Team.AI || lastTouched == Team.None)
                {
                    AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? "AI reached its base with the handkerchief, → AI point +1."
                        : "IA llegó a su base con el pañuelo, → punto IA +1.", winner);
                }
                else
                {
                    AddPointToPlayer(SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player touched the handkerchief before the AI ​​reached its base. → PLAYER point +1."
                        : "Jugador tocó el pañuelo antes de que la IA llegara a su base. → punto JUGADOR +1.", winner);
                }
                return;
            }
        }
        // --- LÓGICA JUGADOR ---
        else if (holderIsTeamA)
        {
            GameObject nearestB = FindNearestInListWithDistance(holder.position, spawner.teamBPlayers, out float nearestBDist);

            if (nearestB != null && nearestBDist <= interceptDistance + 2f)
            {
                cinematicCamera.PlayCinematic(aIController.currentAICharacter.transform);
            }

            if (nearestB != null && nearestBDist <= interceptDistance)
            {
                aIController.MoveRightArm();
                HideOtherPlayers(nearestB.transform);
                AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? $"AI ({nearestB.name}) intercepted the player, → AI point +1."
                    : $"IA ({nearestB.name}) interceptó al jugador, → punto IA +1.", nearestB.transform);
                return;
            }

            if (playerMovement.currentCharacter != null)
            {
                Transform playerTransform = playerMovement.currentCharacter.transform;
                if (playerTransform.position.z <= -110.8615)
                {
                    HideOtherPlayers(playerTransform);
                    AddPointToPlayer(SettingsManager.Instance.LANGUAGE.Equals("English") ? $"Player crossed the score line → point PLAYER +1."
                        : "Jugador cruzó la línea de puntuación → punto JUGADOR +1.", playerTransform);
                    return;
                }
            }

            if (IsWithinBaseArea(holder, Team.Player))
            {
                HideOtherPlayers(holder);
                AddPointToPlayer(SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player reached his base with the handkerchief, → point PLAYER +1."
                    : "Jugador llegó a su base con el pañuelo, → punto JUGADOR +1.", holder);
                return;
            }
        }
    }

    private void HideOtherPlayers(Transform winner)
    {
        hiddenPlayers.Clear();

        foreach (var p in spawner.teamAPlayers)
        {
            if (p != null && p.transform != winner)
            {
                foreach (Renderer r in p.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                hiddenPlayers.Add(p);
            }
        }

        foreach (var p in spawner.teamBPlayers)
        {
            if (p != null && p.transform != winner)
            {
                foreach (Renderer r in p.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                hiddenPlayers.Add(p);
            }
        }
    }

    private Transform FindRootHolder(Transform t)
    {
        if (t == null || spawner == null) return null;
        Transform cur = t;
        while (cur != null)
        {
            if (IsTransformInList(cur, spawner.teamAPlayers) || IsTransformInList(cur, spawner.teamBPlayers))
                return cur;
            cur = cur.parent;
        }
        return null;
    }

    private bool IsTransformInList(Transform t, List<GameObject> list)
    {
        if (t == null || list == null) return false;
        foreach (var g in list)
            if (g != null && t == g.transform) return true;
        return false;
    }

    private GameObject FindNearestInListWithDistance(Vector3 pos, List<GameObject> list, out float outDistance)
    {
        outDistance = float.MaxValue;
        GameObject nearest = null;
        if (list == null) return null;
        foreach (var g in list)
        {
            if (g == null) continue;
            float d = Vector3.Distance(pos, g.transform.position);
            if (d < outDistance)
            {
                outDistance = d;
                nearest = g;
            }
        }
        return nearest;
    }

    private bool IsWithinBaseArea(Transform holder, Team team)
    {
        if (holder == null) return false;
        Vector3 basePos = team == Team.Player ? spawner.teamAPosition : spawner.teamBPosition;
        float radius = baseTolerance + extraTolerance;
        Vector2 holderXZ = new Vector2(holder.position.x, holder.position.z);
        Vector2 baseXZ = new Vector2(basePos.x, basePos.z);
        return Vector2.Distance(holderXZ, baseXZ) <= radius;
    }

    // 🔥 PUNTOS CON EFECTOS
    public void AddPointToPlayer(string reason, Transform winner)
    {
        if (roundEnded) return;
        audioManager.PlayWinSound();

        roundEnded = true;
        playerScore++;
        UpdateScoreUI();

        if (effectsManager != null)
        {
            effectsManager.ShowVictoryEffect(winner, "Jugador", reason, playerScore == SettingsManager.Instance.POINTS_TO_WIN, OnEffectComplete);
        }
        else
            OnEffectComplete();

        if (GameCacheManager.Instance != null && spawner != null)
            GameCacheManager.Instance.SaveGame(this, spawner);
    }

    public void AddPointToIA(string reason, Transform winner)
    {
        if (roundEnded) return;
        audioManager.PlayLoseSound();

        roundEnded = true;
        aiScore++;
        UpdateScoreUI();

        if (effectsManager != null)
        {
            effectsManager.ShowVictoryEffect(winner, "IA", reason, aiScore == SettingsManager.Instance.POINTS_TO_WIN, OnEffectComplete);
        }
        else
            OnEffectComplete();

        if (GameCacheManager.Instance != null && spawner != null)
            GameCacheManager.Instance.SaveGame(this, spawner);
    }

    private void OnEffectComplete()
    {
        foreach (var p in hiddenPlayers)
        {
            if (p != null)
            {
                foreach (Renderer r in p.GetComponentsInChildren<Renderer>())
                    r.enabled = true;
            }
        }
        hiddenPlayers.Clear();

        roundEnded = false;
        lastTouched = Team.None;
        prevHolder = null;
        pickupXZByHolder.Clear();
        progressTowardsBase.Clear();
    }

    public void ReinitializeAfterRespawn()
    {
        originalPosByTransform.Clear();
        pickupXZByHolder.Clear();
        progressTowardsBase.Clear();

        if (spawner != null)
        {
            RegisterPlayerPositions(spawner.teamAPlayers);
            RegisterPlayerPositions(spawner.teamBPlayers);
            playerBasePos = spawner.teamAPosition;
            aiBasePos = spawner.teamBPosition;
        }

        roundEnded = false;
        lastTouched = Team.None;
        prevHolder = null;
    }

    private void UpdateScoreUI()
    {
        string labelPlayer = SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player" : "Jugador";
        string labelIA = SettingsManager.Instance.LANGUAGE.Equals("English") ? "IA" : "Maquina";

        //SPECIAL CASE -> ALIGMENT IA
        if (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && updateIATextPosition)
        {
            aiScoreText.transform.position += new Vector3(30f, 0f, 0f);
            updateIATextPosition = false;
        }

        if (playerScoreText != null)
            playerScoreText.text = $"{labelPlayer} : {playerScore}";
        if (aiScoreText != null)
            aiScoreText.text = $"{labelIA} : {aiScore}";
    }

    // ✅ MÉTODOS PARA GameCacheManager
    public int GetPlayerScore() => playerScore;
    public int GetAIScore() => aiScore;
    public void SetScores(int player, int ai)
    {
        playerScore = player;
        aiScore = ai;
        UpdateScoreUI();
    }
}
