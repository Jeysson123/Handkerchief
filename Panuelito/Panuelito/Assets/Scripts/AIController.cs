using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("Referencias externas")]
    public HandkerchiefSpawner spawner; // referencia al spawner (igual que PlayerMovement)
    public float minSpeedFactor = 0.8f; // velocidad mínima relativa
    public float maxSpeedFactor = 1.2f; // velocidad máxima relativa
    public float actionCooldownMin = 0.5f;
    public float actionCooldownMax = 2f;

    public GameObject currentAICharacter;
    private Animator currentAnimator;
    private Transform rightArm;
    private Transform rightHand;
    private Quaternion originalArmRotation;
    private Quaternion targetArmRotation;
    private bool finting = false;
    private float fintTimer = 0f;
    private float baseSpeed;
    private float currentSpeed;
    private float speedFactor;

    private bool isActing = false;
    public bool returningToBase = false;
    private Vector3 aiOriginalPos;
    private CinematicCameraController cinematicCamera;
    public bool playSlowMotion = true;
    private PlayerMovement playerMovement;
    private Judge judge;
    public int randomLine;

    void Start()
    {
        cinematicCamera = FindObjectOfType<CinematicCameraController>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        judge = FindObjectOfType<Judge>();
        randomLine = Random.Range(0, 2);

        if (spawner == null)
            spawner = FindObjectOfType<HandkerchiefSpawner>();
    }

    void Update()
    {
        if (currentAICharacter == null || spawner == null || spawner.Handkerchief == null) return;

        MoveTowardsTarget();
        HandleFinting();
    }

    public void SelectAICharacter(int playerIndex)
    {
        if (spawner == null || spawner.teamBPlayers == null || playerIndex >= spawner.teamBPlayers.Count) return;

        currentAICharacter = spawner.teamBPlayers[playerIndex];
        if (currentAICharacter != null)
        {
            currentAnimator = currentAICharacter.GetComponentInChildren<Animator>();

            rightArm = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r");
            rightHand = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r/forearm_stretch.r/hand.r");

            if (rightArm != null)
                originalArmRotation = rightArm.localRotation;

            // Guardar posición original
            aiOriginalPos = currentAICharacter.transform.position;

            // Aleatorizar velocidad
            baseSpeed = 10f;
            speedFactor = Random.Range(minSpeedFactor, maxSpeedFactor);
            currentSpeed = baseSpeed * speedFactor;
        }
    }

    private void MoveTowardsTarget()
    {
        if (currentAICharacter == null || spawner.Handkerchief == null) return;

        if (!returningToBase && !playerMovement.hkTaked)
        {
            //pass line without take HK
            if (currentAICharacter.transform.position.z < -15.6)
            {
                judge.AddPointToPlayer($"IA cruzo linea sin panuelo, → punto JUGADOR +1.", playerMovement.currentCharacter.transform);

            }
        }
        else if (returningToBase)
        {
            //pass line now with HK to enemy base
            if (currentAICharacter.transform.position.z < -15.6)
            {
                judge.AddPointToPlayer($"IA cruzo linea con panuelo hacia base equivocada, → punto JUGADOR +1.", playerMovement.currentCharacter.transform);

            }
        }

        Vector3 targetPos;

        if (returningToBase)
        {
            // IA regresando a su posición original
            targetPos = aiOriginalPos;
            if (playSlowMotion)
            {
                cinematicCamera.PlayCinematic(currentAICharacter.transform); //play slow motion
                playSlowMotion = false;
            }

        }
        else
        {
            // Verificar si el pañuelo sigue en su posición original
            float distToOriginal = Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos);
            bool handkerchiefAvailable = distToOriginal < 0.2f;

            if (handkerchiefAvailable)
            {
                targetPos = spawner.Handkerchief.transform.position; // pañuelo disponible
                //ramdon logic go to HK or pass line
                if (randomLine > 0)
                {
                    targetPos.z -= 20;
                }
            }
            else
            {
                Transform parent = spawner.Handkerchief.transform.parent;
                if (parent != null && parent != rightHand)
                    targetPos = parent.position; // jugador humano que lo tomó
                else
                    targetPos = spawner.Handkerchief.transform.position; // fallback
            }
        }

        // Movimiento hacia el objetivo
        Vector3 direction = targetPos - currentAICharacter.transform.position;
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            currentAICharacter.transform.Translate(direction.normalized * currentSpeed * Time.deltaTime, Space.World);
            currentAICharacter.transform.rotation = Quaternion.Slerp(
                currentAICharacter.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 10f
            );

            if (currentAnimator != null)
                currentAnimator.SetFloat("speed", currentSpeed);
        }
        else if (!returningToBase && Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos) < 0.2f && !isActing)
        {
            StartCoroutine(PerformRandomAction());
        }
        else if (returningToBase)
        {
            // Llegó a base
            returningToBase = false;
            if (currentAnimator != null)
                currentAnimator.SetFloat("speed", 0f);
        }
    }

    private IEnumerator PerformRandomAction()
    {
        isActing = true;
        float delay = Random.Range(actionCooldownMin, actionCooldownMax);
        yield return new WaitForSeconds(delay);

        float actionRoll = Random.value;
        if (actionRoll < 0.5f)
            MoveRightArm(); // finta
        else
            TakeHandkerchief(); // intentar tomar

        isActing = false;
    }

    public void MoveRightArm()
    {
        if (rightArm == null) return;

        finting = true;
        fintTimer = 0f;

        Vector3 euler = rightArm.localEulerAngles;
        float armRaiseAngle = 100f;
        float k = 1f;

        float targetX = euler.x - armRaiseAngle * k;
        targetArmRotation = Quaternion.Euler(targetX, euler.y, euler.z);
        originalArmRotation = rightArm.localRotation;
    }

    private void TakeHandkerchief()
    {
        if (rightArm == null || rightHand == null || spawner.Handkerchief == null || currentAICharacter == null) return;

        Vector3 playerPosFlat = new Vector3(currentAICharacter.transform.position.x, 0, currentAICharacter.transform.position.z);
        Vector3 hkPosFlat = new Vector3(spawner.Handkerchief.transform.position.x, 0, spawner.Handkerchief.transform.position.z);
        float distXZ = Vector3.Distance(playerPosFlat, hkPosFlat);
        float xzTolerance = 5f;

        MoveRightArm();

        if (distXZ <= xzTolerance && Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos) < 0.2f)
        {
            spawner.Handkerchief.transform.SetParent(rightHand);
            spawner.Handkerchief.transform.localPosition = Vector3.zero;
            spawner.Handkerchief.transform.localRotation = Quaternion.identity;

            returningToBase = true; // IA vuelve a su base
        }
        else
        {
        }
    }

    private void HandleFinting()
    {
        if (rightArm != null && finting)
        {
            fintTimer += Time.deltaTime;
            float fintDuration = 1.2f;
            float t = fintTimer / fintDuration;

            if (t <= 1f)
            {
                float smoothT = Mathf.SmoothStep(0f, 1f, t <= 0.5f ? t * 2f : (1f - t) * 2f);
                rightArm.localRotation = Quaternion.Slerp(originalArmRotation, targetArmRotation, smoothT);
            }
            else
            {
                rightArm.localRotation = originalArmRotation;
                finting = false;
            }
        }
    }
}
