using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Judge : MonoBehaviour
{
    private enum Team
    {
        None,
        Player,
        AI
    }

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

    private void Start()
    {
        spawner = FindObjectOfType<HandkerchiefSpawner>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        aIController = FindObjectOfType<AIController>();

        if (spawner != null)
        {
            playerBasePos = spawner.teamAPosition;
            aiBasePos = spawner.teamBPosition;
            RegisterPlayerPositions(spawner.teamAPlayers);
            RegisterPlayerPositions(spawner.teamBPlayers);

            Debug.Log($"[Judge] Bases: playerBase={playerBasePos} | aiBase={aiBasePos} | miembros registrados: {originalPosByTransform.Count}");
        }
        else
        {
            Debug.LogWarning("[Judge] No se encontró HandkerchiefSpawner en la escena.");
        }

        UpdateScoreUI();
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

        // Detectar cambio de portador
        if (holder != prevHolder)
        {
            if (prevHolder != null)
            {
                pickupXZByHolder.Remove(prevHolder);
                progressTowardsBase.Remove(prevHolder);
            }

            if (holder != null)
            {
                // Registrar correctamente si es Player o IA usando root
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
                            Debug.Log($"[Judge] Registrado pickup XZ para {root.name}: {pickupXZByHolder[root].x:F3}, {pickupXZByHolder[root].y:F3}");
                        }
                    }
                    else if (IsTransformInList(root, spawner.teamBPlayers))
                        lastTouched = Team.AI;
                    else
                        lastTouched = Team.None;
                }
            }
            else
            {
                lastTouched = Team.None;
            }

            prevHolder = holder;
        }

        if (holder == null) return;

        bool holderIsTeamA = IsTransformInList(FindRootHolder(holder), spawner.teamAPlayers);
        bool holderIsTeamB = IsTransformInList(FindRootHolder(holder), spawner.teamBPlayers);

        // --- LÓGICA IA ---
        if (holderIsTeamB)
        {
            // Verificar si un jugador intercepta a la IA
            GameObject nearestA = FindNearestInListWithDistance(holder.position, spawner.teamAPlayers, out float nearestADist);
            if (nearestA != null && nearestADist <= interceptDistance)
            {
                AddPointToPlayer($"Jugador ({nearestA.name}) interceptó a la IA.");
                return;
            }


            // Progreso hacia la línea del ia
            if (aIController.currentAICharacter != null)
            {
                Transform iatransform = aIController.currentAICharacter.transform;
                if (iatransform.position.z >= 112.12)
                {
                    AddPointToIA("IA cruzó la línea de puntuación → punto.");
                    return;
                }
            }

            // Verificar si la IA llega a su base
            if (IsWithinBaseArea(holder, Team.AI))
            {
                if (lastTouched == Team.AI || lastTouched == Team.None)
                {
                    AddPointToIA("IA llegó a su base con el pañuelo.");
                }
                else
                {
                    AddPointToPlayer("Jugador tocó el pañuelo antes de que la IA llegara a su base.");
                }
                return;
            }
        }

        // --- LÓGICA JUGADOR ---
        else if (holderIsTeamA)
        {
            GameObject nearestB = FindNearestInListWithDistance(holder.position, spawner.teamBPlayers, out float nearestBDist);
            if (nearestB != null && nearestBDist <= interceptDistance)
            {
                AddPointToIA($"IA ({nearestB.name}) interceptó al jugador.");
                return;
            }

            // Progreso hacia la línea del jugador
            if (playerMovement.currentCharacter != null)
            {
                Transform playerTransform = playerMovement.currentCharacter.transform;
                if (playerTransform.position.z <= -110.8615)
                {
                    AddPointToPlayer("Jugador cruzó la línea de puntuación → punto.");
                    return;
                }
            }

            if (IsWithinBaseArea(FindRootHolder(holder), Team.Player))
            {
                AddPointToPlayer("Jugador llegó a su base con el pañuelo.");
                return;
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
        {
            if (g != null && t == g.transform) return true;
        }
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

    private void AddPointToPlayer(string reason)
    {
        playerScore++;
        Debug.Log($"✅ Punto para JUGADOR → {reason}");
        ResetRound();
        UpdateScoreUI();
    }

    private void AddPointToIA(string reason)
    {
        aiScore++;
        Debug.Log($"✅ Punto para IA → {reason}");
        ResetRound();
        UpdateScoreUI();
    }

    private void ResetRound()
    {
        roundEnded = true;
        lastTouched = Team.None;
        prevHolder = null;
        pickupXZByHolder.Clear();
        progressTowardsBase.Clear();

        if (spawner != null && spawner.Handkerchief != null)
        {
            spawner.Handkerchief.transform.SetParent(null);
            spawner.Handkerchief.transform.position = spawner.OriginalHandkerchiefPos;
            spawner.Handkerchief.transform.rotation = Quaternion.identity;
        }

        Invoke(nameof(NewRound), 1.5f);
    }

    private void NewRound()
    {
        roundEnded = false;
    }

    private void UpdateScoreUI()
    {
        if (playerScoreText != null)
            playerScoreText.text = $"PLAYER : {playerScore}";
        if (aiScoreText != null)
            aiScoreText.text = $"IA : {aiScore}";
    }
}
