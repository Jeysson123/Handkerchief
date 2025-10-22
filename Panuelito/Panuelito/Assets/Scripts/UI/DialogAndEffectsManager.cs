using UnityEngine;
using System.Collections;
using TMPro; // TextMeshPro 3D
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class DialogAndEffectsManager : MonoBehaviour
{
    [Header("Referencias")]
    public HandkerchiefSpawner spawner;
    public float effectDuration = 3.5f;

    [Header("Prefabs de efectos")]
    public GameObject[] victoryEffectPrefabs;
    [Range(1f, 4f)] public float effectScaleMultiplier = 4.0f;
    public float effectHeightOffset = 1.5f;

    [Header("Animaciones de celebración")]
    public string[] victoryStates;

    [Header("Cámara")]
    public CameraFollow cameraFollow;
    public float focusDuration = 0.5f;
    public float cameraCloserDistance = 4f;
    public float cameraPitchDuringEffect = 10f;

    [Header("Ajuste cámara desde pañuelo")]
    public float cameraDistanceFromHandkerchief = 1.5f;
    public float cameraHeightFromHandkerchief = 1.2f;

    private bool isPlayingEffect = false;
    private Judge judge;
    private Transform originalTarget;
    private float originalYaw;
    private float originalPitch;
    private Vector3 originalOffset;

    private PlayerMovement playerMovement;
    private AIController aiController;

    // Dialog numbers
    private int[] numbersPosition = { 1, 2, 3 };
    public int numberInDialog = 0;

    [Header("UI de diálogo")]
    public GameObject dialogNumber;      // Objeto padre del texto
    public TextMeshPro textNumber;       // TextMeshPro 3D (no necesita Canvas)
    public GameObject dialogResult;
    public TextMeshPro textResult;
    public GameObject dialogPlus1;

    private void Start()
    {
        judge = FindObjectOfType<Judge>();
        spawner = FindObjectOfType<HandkerchiefSpawner>();
        cameraFollow = FindObjectOfType<CameraFollow>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        aiController = FindObjectOfType<AIController>();

        if (cameraFollow != null)
        {
            originalTarget = cameraFollow.target;
            originalOffset = cameraFollow.offset;
        }
    }

    public void Update()
    {
        if (playerMovement.currentCharacter != null)
        {

            if (playerMovement.currentCharacter.transform.position.x < -89.3604
              || playerMovement.currentCharacter.transform.position.x > 96.98035
              || playerMovement.currentCharacter.transform.position.z > 118.1293
              || playerMovement.currentCharacter.transform.position.z < -119.9638)
            {
                StartCoroutine(RestoreCameraAndResetRound());
            }

        }
    }

    public IEnumerator ShowNumber()
    {
        if (dialogNumber == null || textNumber == null) yield break;

        dialogNumber.SetActive(true);
        textNumber.enabled = true;

        numberInDialog = numbersPosition[Random.Range(0, numbersPosition.Length)];
        textNumber.text = numberInDialog.ToString();

        yield return new WaitForSeconds(2f);

        dialogNumber.SetActive(false);
        textNumber.enabled = false;
    }

    public void StartShowNumber()
    {
        StartCoroutine(ShowNumber());
    }

    // ✅ Nuevo parámetro celebrateFullTeam
    public void ShowVictoryEffect(Transform winner, string teamName, string reason, bool celebrateFullTeam = false, System.Action onComplete = null)
    {
        if (celebrateFullTeam)
        {
            reason = teamName.Equals("IA") ? (SettingsManager.Instance.LANGUAGE.Equals("English")
                ? "Team IA Winners" : "Equipo IA ganadores")
                : (SettingsManager.Instance.LANGUAGE.Equals("English") ? "Team PLAYER Winners" : "Equipo JUGADOR Ganadores");
        }

        if (teamName.Equals("IA"))
        {
            aiController.playSlowMotion = true;
        }

        dialogResult.transform.position = new Vector3(winner.position.x - 15f, winner.position.y + 10f, winner.position.z);
        textResult.gameObject.transform.position = new Vector3(winner.position.x + 27f, winner.position.y - 12f, winner.position.z - 1);
        textResult.text = reason;
        dialogPlus1.transform.position = new Vector3(winner.position.x + 5f, winner.position.y + 10f, winner.position.z);

        if (isPlayingEffect || winner == null) return;
        StartCoroutine(PlayEffectAndRestart(winner, teamName, celebrateFullTeam, onComplete));
    }

    private IEnumerator PlayEffectAndRestart(Transform winner, string teamName, bool celebrateFullTeam, System.Action onComplete)
    {
        isPlayingEffect = true;
        dialogResult.SetActive(true);
        textResult.gameObject.SetActive(true);
        dialogPlus1.SetActive(true);

        if (playerMovement != null) playerMovement.enabled = false;
        if (aiController != null) aiController.enabled = false;

        // ✅ Ocultar todos los personajes menos el ganador (solo si no celebran todo el equipo)
        List<GameObject> hiddenPlayers = new List<GameObject>();
        if (!celebrateFullTeam && spawner != null)
        {
            List<GameObject> allPlayers = new List<GameObject>();
            allPlayers.AddRange(spawner.teamAPlayers);
            allPlayers.AddRange(spawner.teamBPlayers);

            foreach (var p in allPlayers)
            {
                if (p != null && p.transform != winner)
                {
                    Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers)
                        r.enabled = false;
                    hiddenPlayers.Add(p);
                }
            }
        }

        // Guardar cámara
        if (cameraFollow != null)
        {
            originalTarget = cameraFollow.target;
            originalYaw = cameraFollow.yaw;
            originalPitch = cameraFollow.pitch;
            originalOffset = cameraFollow.offset;
            cameraFollow.isDragging = false;
        }

        // Girar al ganador hacia la cámara
        Vector3 camDir = cameraFollow.transform.forward;
        camDir.y = 0;
        if (camDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(-camDir);
            float rotT = 0f;
            while (rotT < 1f)
            {
                rotT += Time.deltaTime * 5f;
                winner.rotation = Quaternion.Slerp(winner.rotation, targetRot, rotT);
                yield return null;
            }
        }

        // Ajuste de cámara
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(winner);

            Vector3 targetOffset;
            float targetYaw;
            float targetPitch;

            if (winner.GetComponent<PlayerMovement>() != null)
            {
                Vector3 forward = winner.forward;
                Vector3 camPos = winner.position + forward * 2f + Vector3.up * 1.2f;
                Vector3 offsetFromTarget = camPos - winner.position;

                Vector3 lookDir = winner.position - camPos;
                Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                Vector3 lookEuler = lookRot.eulerAngles;

                targetYaw = lookEuler.y;
                float rawPitch = lookEuler.x;
                if (rawPitch > 180f) rawPitch -= 360f;
                targetPitch = Mathf.Clamp(rawPitch, -80f, 80f);

                targetOffset = offsetFromTarget;
            }
            else
            {
                targetOffset = originalOffset * cameraCloserDistance;
                targetYaw = 0f;
                targetPitch = cameraPitchDuringEffect;
            }

            Vector3 startOffset = cameraFollow.offset;
            float startYaw = cameraFollow.yaw;
            float startPitch = cameraFollow.pitch;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / focusDuration;
                cameraFollow.offset = Vector3.Lerp(startOffset, targetOffset, t);
                cameraFollow.yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
                cameraFollow.pitch = Mathf.Lerp(startPitch, targetPitch, t);
                yield return null;
            }

            cameraFollow.offset = targetOffset;
            cameraFollow.yaw = targetYaw;
            cameraFollow.pitch = targetPitch;
        }

        // ✅ Animación y efectos de victoria
        if (celebrateFullTeam && spawner != null)
        {
            List<GameObject> teamPlayers = teamName == "Jugador" ? spawner.teamAPlayers : spawner.teamBPlayers;
            foreach (var player in teamPlayers)
            {
                if (player != null)
                {
                    Animator animator = player.GetComponent<Animator>();
                    if (animator != null && victoryStates != null && victoryStates.Length > 0)
                    {
                        int index = Random.Range(0, victoryStates.Length);
                        string stateName = victoryStates[index];
                        animator.Play(stateName, 0, 0f);
                    }

                    // Efectos
                    if (victoryEffectPrefabs != null && victoryEffectPrefabs.Length > 0)
                    {
                        int effectIndex = Random.Range(0, victoryEffectPrefabs.Length);
                        float characterHeight = 2f;
                        Collider col = player.GetComponent<Collider>();
                        if (col != null) characterHeight = col.bounds.size.y;

                        Vector3 effectPos = player.transform.position + Vector3.up * (characterHeight * 0.9f + effectHeightOffset);
                        GameObject effect = Instantiate(victoryEffectPrefabs[effectIndex], effectPos, player.transform.rotation);
                        effect.transform.localScale *= effectScaleMultiplier;
                        Destroy(effect, effectDuration);
                    }
                }
            }
        }
        else
        {
            Animator animator = winner.GetComponent<Animator>();
            if (animator != null && victoryStates != null && victoryStates.Length > 0)
            {
                int index = Random.Range(0, victoryStates.Length);
                string stateName = victoryStates[index];
                animator.Play(stateName, 0, 0f);

                if (victoryEffectPrefabs != null && victoryEffectPrefabs.Length > 0)
                {
                    int effectIndex = Random.Range(0, victoryEffectPrefabs.Length);
                    float characterHeight = 2f;
                    Collider col = winner.GetComponent<Collider>();
                    if (col != null) characterHeight = col.bounds.size.y;

                    Vector3 effectPos = winner.position + Vector3.up * (characterHeight * 0.9f + effectHeightOffset);
                    GameObject effect = Instantiate(victoryEffectPrefabs[effectIndex], effectPos, winner.rotation);
                    effect.transform.localScale *= effectScaleMultiplier;
                    Destroy(effect, effectDuration);
                }
            }
        }

        yield return new WaitForSeconds(effectDuration);

        // ✅ Volver a mostrar los personajes ocultos
        if (!celebrateFullTeam && spawner != null)
        {
            List<GameObject> allPlayers = new List<GameObject>();
            allPlayers.AddRange(spawner.teamAPlayers);
            allPlayers.AddRange(spawner.teamBPlayers);

            foreach (var p in allPlayers)
            {
                if (p != null && hiddenPlayers.Contains(p))
                {
                    Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers)
                        r.enabled = true;
                }
            }
        }

        // ✅ Usar nueva función para restaurar cámara y reiniciar ronda
        yield return StartCoroutine(RestoreCameraAndResetRound());

        isPlayingEffect = false;
        onComplete?.Invoke();

        //Match is ended
        if (celebrateFullTeam)
        {
            GameCacheManager.Instance.SaveEndResult();
            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }
    }

    // 🔹 Nueva función pública para restaurar cámara y reiniciar ronda
    public IEnumerator RestoreCameraAndResetRound()
    {
        // Restaurar cámara
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(originalTarget);

            Vector3 startOffset = cameraFollow.offset;
            float startYaw = cameraFollow.yaw;
            float startPitch = cameraFollow.pitch;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / focusDuration;
                cameraFollow.offset = Vector3.Lerp(startOffset, originalOffset, t);
                cameraFollow.yaw = Mathf.LerpAngle(startYaw, originalYaw, t);
                cameraFollow.pitch = Mathf.Lerp(startPitch, originalPitch, t);
                yield return null;
            }

            cameraFollow.offset = originalOffset;
            cameraFollow.yaw = originalYaw;
            cameraFollow.pitch = originalPitch;
        }

        // Reiniciar ronda
        if (spawner != null)
        {
            foreach (var p in spawner.teamAPlayers)
                if (p != null) Destroy(p);
            spawner.teamAPlayers.Clear();

            foreach (var p in spawner.teamBPlayers)
                if (p != null) Destroy(p);
            spawner.teamBPlayers.Clear();

            if (spawner.Handkerchief != null)
                Destroy(spawner.Handkerchief);

            yield return new WaitForSeconds(0.2f);
            spawner.SpawnAll();

            if (judge != null)
                judge.ReinitializeAfterRespawn();
        }

        if (playerMovement != null) playerMovement.enabled = true;
        if (aiController != null) aiController.enabled = true;

        dialogResult.SetActive(false);
        textResult.gameObject.SetActive(false);
        dialogPlus1.SetActive(false);
        playerMovement.hkTaked = false;
        aiController.randomLine = Random.Range(0, 2);
    }
}
