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
    public Animator currentAnimator;
    private Transform rightArm;
    private Transform rightHand;
    private Quaternion originalArmRotation;
    private Quaternion targetArmRotation;
    private bool finting = false;
    private float fintTimer = 0f;
    private float baseSpeed;
    private float currentSpeed;
    private float speedFactor;
    public float originalSpeed = 0f;

    private bool isActing = false;
    public bool returningToBase = false;
    private Vector3 aiOriginalPos;
    private CinematicCameraController cinematicCamera;
    public bool playSlowMotion = true;
    private PlayerMovement playerMovement;
    private Judge judge;
    public int randomLine;
    private AudioManager audioManager;

    // === Simulación de correr ===
    private Transform leftThigh, rightThigh;
    private Transform leftLeg, rightLeg;
    private Transform leftArmRun, rightArmRun;
    private Transform leftForearm, rightForearm;

    // 🔹 Nuevas referencias
    private Transform torso, head;

    private float runCycleTime;

    // Variables para guardar rotación inicial
    private Quaternion leftThighOriginal, rightThighOriginal;
    private Quaternion leftLegOriginal, rightLegOriginal;
    private Quaternion leftArmOriginal, rightArmOriginal;
    private Quaternion leftForearmOriginal, rightForearmOriginal;
    private Quaternion torsoOriginal, headOriginal;

    void Start()
    {
        cinematicCamera = FindObjectOfType<CinematicCameraController>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        judge = FindObjectOfType<Judge>();
        randomLine = Random.Range(0, 2);
        audioManager = FindObjectOfType<AudioManager>();

        if (spawner == null)
            spawner = FindObjectOfType<HandkerchiefSpawner>();
    }

    void Update()
    {
        if (currentAICharacter == null || spawner == null || spawner.Handkerchief == null) return;

        MoveTowardsTarget();
        HandleFinting();
        if (currentSpeed >= 10f) SimulateRunning();

        // NUEVO: habilitar Animator si velocidad baja
        if (currentSpeed < 10f && currentAnimator != null && !currentAnimator.enabled)
        {
            currentAnimator.enabled = true;
        }
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
            if ((SettingsManager.Instance.LANGUAGE.Equals("English") && SettingsManager.Instance.DIFFICULT.Equals("Hard"))
                 || (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && SettingsManager.Instance.DIFFICULT.Equals("Dificil")))
            {
                currentSpeed = 20f;
            }
            else
            {
                baseSpeed = 10f;
                speedFactor = Random.Range(minSpeedFactor, maxSpeedFactor);
                currentSpeed = baseSpeed * speedFactor;
            }

            //store true SPEED
            originalSpeed = currentSpeed;
        }
    }

    private void MoveTowardsTarget()
    {
        if (currentAICharacter == null || spawner.Handkerchief == null) return;

        if ((SettingsManager.Instance.LANGUAGE.Equals("English") && !SettingsManager.Instance.DIFFICULT.Equals("Hard"))
               || (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && !SettingsManager.Instance.DIFFICULT.Equals("Dificil")))
        {
            if (!returningToBase && !playerMovement.hkTaked)
            {
                if (currentAICharacter.transform.position.z < -15.6)
                {
                    judge.AddPointToPlayer($"IA cruzo linea sin panuelo, → punto JUGADOR +1.", playerMovement.currentCharacter.transform);
                }
            }
            else if (returningToBase)
            {
                if (currentAICharacter.transform.position.z < -15.6)
                {
                    judge.AddPointToPlayer($"IA cruzo linea con panuelo hacia base equivocada, → punto JUGADOR +1.", playerMovement.currentCharacter.transform);
                }
            }
        }

        Vector3 targetPos;

        if (returningToBase)
        {
            targetPos = aiOriginalPos;
            if (playSlowMotion)
            {
                cinematicCamera.PlayCinematic(currentAICharacter.transform);
                audioManager.PlayTakeFintSound();
                playSlowMotion = false;
            }
            currentSpeed = originalSpeed;
            if (currentAnimator != null)
                currentAnimator.SetFloat("speed", currentSpeed);
        }
        else
        {
            float distToOriginal = Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos);
            bool handkerchiefAvailable = distToOriginal < 0.2f;

            if (handkerchiefAvailable)
            {
                targetPos = spawner.Handkerchief.transform.position;

                if ((SettingsManager.Instance.LANGUAGE.Equals("English") && !SettingsManager.Instance.DIFFICULT.Equals("Hard"))
                || (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && !SettingsManager.Instance.DIFFICULT.Equals("Dificil")))
                {
                    if (randomLine > 0)
                    {
                        targetPos.z -= 20;
                    }
                    else
                    {
                        Vector3 aiPosFlat = new Vector3(currentAICharacter.transform.position.x, 0, currentAICharacter.transform.position.z);
                        Vector3 hkPosFlat = new Vector3(spawner.Handkerchief.transform.position.x, 0, spawner.Handkerchief.transform.position.z);
                        float distXZ = Vector3.Distance(aiPosFlat, hkPosFlat);
                        float xzTolerance = 2f;

                        if (!returningToBase
                            && spawner.Handkerchief.transform.parent == null
                            && distXZ <= xzTolerance
                            && Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos) < 0.2f)
                        {
                            if (currentAnimator != null)
                            {
                                currentSpeed = 0;
                                currentAnimator.SetFloat("speed", currentSpeed);
                                StartCoroutine(PerformRandomAction());
                            }
                        }
                    }
                }
            }
            else
            {
                Transform parent = spawner.Handkerchief.transform.parent;
                if (parent != null && parent != rightHand)
                    targetPos = parent.position;
                else
                    targetPos = spawner.Handkerchief.transform.position;
            }
        }

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
            returningToBase = false;
            if (currentAnimator != null)
                currentAnimator.SetFloat("speed", 0f);
        }
    }

    public void IAReaction()
    {
        Vector3 playerPosFlat = new Vector3(currentAICharacter.transform.position.x, 0, currentAICharacter.transform.position.z);
        Vector3 hkPosFlat = new Vector3(spawner.Handkerchief.transform.position.x, 0, spawner.Handkerchief.transform.position.z);
        float distXZ = Vector3.Distance(playerPosFlat, hkPosFlat);
        float xzTolerance = 5f;

        if (!returningToBase && distXZ <= xzTolerance
            && Vector3.Distance(spawner.Handkerchief.transform.position, spawner.OriginalHandkerchiefPos) < 0.2f)
        {
            Vector3 targetPos = spawner.Handkerchief.transform.position;

            if ((SettingsManager.Instance.LANGUAGE.Equals("English") && !SettingsManager.Instance.DIFFICULT.Equals("Hard"))
                || (SettingsManager.Instance.LANGUAGE.Equals("Spanish") && !SettingsManager.Instance.DIFFICULT.Equals("Dificil")))
            {
                int randomLineTemp = Random.Range(0, 2);

                if (randomLineTemp > 0)
                {
                    targetPos.z -= 20;
                }
                else
                {
                    StartCoroutine(PerformRandomAction());
                }
            }
        }
    }

    private IEnumerator PerformRandomAction()
    {
        isActing = true;
        float delay = Random.Range(actionCooldownMin, actionCooldownMax);
        yield return new WaitForSeconds(delay);

        float actionRoll = Random.value;
        if (actionRoll < 0.5f)
            MoveRightArm();
        else
            TakeHandkerchief();

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

            returningToBase = true;
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

    public void SimulateRunning()
    {
        if (currentAICharacter == null) return;

        // Desactivar el Animator si está activo
        if (currentAnimator != null && currentAnimator.enabled)
            currentAnimator.enabled = false;

        if (leftThigh == null)
        {
            leftThigh = currentAICharacter.transform.Find("root/root.x/thigh_stretch.l");
            rightThigh = currentAICharacter.transform.Find("root/root.x/thigh_stretch.r");
            leftLeg = currentAICharacter.transform.Find("root/root.x/thigh_stretch.l/leg_stretch.l");
            rightLeg = currentAICharacter.transform.Find("root/root.x/thigh_stretch.r/leg_stretch.r");
            leftArmRun = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.l/arm_stretch.l");
            rightArmRun = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r");
            leftForearm = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.l/arm_stretch.l/forearm.l");
            rightForearm = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r/forearm.r");

            torso = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x");
            head = currentAICharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/neck.x/head.x");

            if (leftThigh != null) leftThighOriginal = leftThigh.localRotation;
            if (rightThigh != null) rightThighOriginal = rightThigh.localRotation;
            if (leftLeg != null) leftLegOriginal = leftLeg.localRotation;
            if (rightLeg != null) rightLegOriginal = rightLeg.localRotation;
            if (leftArmRun != null) leftArmOriginal = leftArmRun.localRotation;
            if (rightArmRun != null) rightArmOriginal = rightArmRun.localRotation;
            if (leftForearm != null) leftForearmOriginal = leftForearm.localRotation;
            if (rightForearm != null) rightForearmOriginal = rightForearm.localRotation;
            if (torso != null) torsoOriginal = torso.localRotation;
            if (head != null) headOriginal = head.localRotation;
        }

        runCycleTime += Time.deltaTime * 6f;

        float thighForward = 60f;
        float rightThighForward = 70f;
        float thighBackward = 10f;
        float kneeFlex = 90f;
        float armForward = 70f;
        float armBackward = -50f;
        float forearmFlex = 60f;

        float cycle = Mathf.Sin(runCycleTime);
        float oppositeCycle = Mathf.Sin(runCycleTime + Mathf.PI);

        float leftThighAngle = (cycle > 0 ? cycle * thighForward : -cycle * thighBackward);
        float rightThighAngle = (oppositeCycle > 0 ? -oppositeCycle * rightThighForward : oppositeCycle * thighBackward);
        float leftKneeAngle = (cycle > 0 ? -cycle * kneeFlex : 0);
        float rightKneeAngle = (oppositeCycle > 0 ? oppositeCycle * kneeFlex : 0);

        if (leftThigh != null) leftThigh.localRotation = leftThighOriginal * Quaternion.Euler(0, 0, leftThighAngle);
        if (rightThigh != null) rightThigh.localRotation = rightThighOriginal * Quaternion.Euler(0, 0, rightThighAngle);
        if (leftLeg != null) leftLeg.localRotation = leftLegOriginal * Quaternion.Euler(0, 0, leftKneeAngle);
        if (rightLeg != null) rightLeg.localRotation = rightLegOriginal * Quaternion.Euler(0, 0, rightKneeAngle);

        float leftArmAngle = Mathf.Lerp(armBackward, armForward, (oppositeCycle + 1f) / 2f);
        float rightArmAngle = Mathf.Lerp(armBackward, armForward, (cycle + 1f) / 2f);

        if (leftArmRun != null) leftArmRun.localRotation = leftArmOriginal * Quaternion.Euler(0, 0, leftArmAngle);
        if (rightArmRun != null) rightArmRun.localRotation = rightArmOriginal * Quaternion.Euler(0, 0, -rightArmAngle);

        float leftForearmAngle = (oppositeCycle > 0 ? -oppositeCycle * forearmFlex : 0);
        float rightForearmAngle = (cycle > 0 ? -cycle * forearmFlex : 0);

        if (leftForearm != null) leftForearm.localRotation = leftForearmOriginal * Quaternion.Euler(leftForearmAngle, 0, 0);
        if (rightForearm != null) rightForearm.localRotation = rightForearmOriginal * Quaternion.Euler(rightForearmAngle, 0, 0);

        float torsoTilt = 5f * cycle;
        float headTilt = 15f * cycle;

        if (torso != null) torso.localRotation = torsoOriginal * Quaternion.Euler(torsoTilt, 0, 0);
        if (head != null) head.localRotation = headOriginal * Quaternion.Euler(headTilt, 0, 0);
    }
}
