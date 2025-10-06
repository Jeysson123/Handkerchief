using UnityEngine;
using System.Collections;
using TMPro; // TextMeshPro 3D

public class DialogAndEffectsManager : MonoBehaviour
{
    [Header("Referencias")]
    public HandkerchiefSpawner spawner;
    public float effectDuration = 3.5f;

    [Header("Prefabs de efectos")]
    public GameObject[] victoryEffectPrefabs;
    [Range(1f, 4f)] public float effectScaleMultiplier = 3.0f;
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

    public IEnumerator ShowNumber()
    {
        if (dialogNumber == null || textNumber == null) yield break;

        dialogNumber.SetActive(true);
        textNumber.enabled = true;  // muestra el texto

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

    public void ShowVictoryEffect(Transform winner, string teamName, System.Action onComplete = null)
    {
        if (isPlayingEffect || winner == null) return;
        StartCoroutine(PlayEffectAndRestart(winner, teamName, onComplete));
    }

    private IEnumerator PlayEffectAndRestart(Transform winner, string teamName, System.Action onComplete)
    {
        isPlayingEffect = true;

        if (playerMovement != null) playerMovement.enabled = false;
        if (aiController != null) aiController.enabled = false;

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

        // Animación y efectos de victoria
        Animator animator = winner.GetComponent<Animator>();
        if (animator != null && victoryStates != null && victoryStates.Length > 0)
        {
            int index = Random.Range(0, victoryStates.Length);
            string stateName = victoryStates[index];
            animator.Play(stateName, 0, 0f);

            if (victoryEffectPrefabs != null && victoryEffectPrefabs.Length > 0)
            {
                int effectIndex = Random.Range(0, victoryEffectPrefabs.Length);
                Vector3 effectPos = winner.position + Vector3.up * effectHeightOffset;
                GameObject effect = Instantiate(victoryEffectPrefabs[effectIndex], effectPos, winner.rotation);
                effect.transform.localScale *= effectScaleMultiplier;
                Destroy(effect, effectDuration);
            }
        }

        yield return new WaitForSeconds(effectDuration);

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

        isPlayingEffect = false;
        onComplete?.Invoke();
    }
}
