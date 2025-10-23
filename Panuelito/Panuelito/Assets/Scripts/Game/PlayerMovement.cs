using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class PlayerMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias externas")]
    public VariableJoystick joystick;
    public Button button1, button2, button3;
    public Button speedButton;
    public Button fintButton;
    public Button takeButton;
    public Slider speedBar;
    public Image speedFill;

    [Header("Configuración de movimiento")]
    public float baseMoveSpeed = 0f;
    public float maxSpeed = 20f;
    public float speedStep = 1f;
    public float speedDecayRate = 3f;

    [Header("Configuración de finta / tomar")]
    [Range(0f, 2f)] public float k = 1f;
    public float fintDuration = 1.2f;
    public float armRaiseAngle = 100f;

    [Header("Pañuelito")]
    public float takeDistance = 2f;

    public float currentSpeed;
    private bool isSpeedButtonPressed = false;

    private HandkerchiefSpawner spawner;
    private Judge judge;
    private DialogAndEffectsManager dialogAndEffectsManager;
    private AIController aiController;
    public GameObject currentCharacter;
    public Animator currentAnimator;

    private Transform rightArm;
    private Transform rightHand;
    private Quaternion originalArmRotation;
    private Quaternion targetArmRotation;
    private bool finting = false;
    private float fintTimer = 0f;

    private CinematicCameraController cinematicCamera;
    public bool hkTaked;
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

    // 🔹 Nuevas rotaciones originales
    private Quaternion torsoOriginal, headOriginal;

    void Start()
    {
        spawner = FindObjectOfType<HandkerchiefSpawner>();
        judge = FindObjectOfType<Judge>();
        dialogAndEffectsManager = FindObjectOfType<DialogAndEffectsManager>();
        cinematicCamera = FindObjectOfType<CinematicCameraController>();
        aiController = FindObjectOfType<AIController>();
        audioManager = FindObjectOfType<AudioManager>();

        if (button1 != null) button1.onClick.AddListener(() => SelectCharacter(0));
        if (button2 != null) button2.onClick.AddListener(() => SelectCharacter(1));
        if (button3 != null) button3.onClick.AddListener(() => SelectCharacter(2));

        if (fintButton != null)
            fintButton.onClick.AddListener(HandleFint);

        if (takeButton != null)
            takeButton.onClick.AddListener(TakeHandkerchief);

        currentSpeed = baseMoveSpeed;
        if (speedBar != null)
        {
            speedBar.minValue = baseMoveSpeed;
            speedBar.maxValue = maxSpeed;
            speedBar.value = currentSpeed;
            speedBar.direction = Slider.Direction.BottomToTop;
        }

        if (speedButton != null)
        {
            EventTrigger trigger = speedButton.GetComponent<EventTrigger>() ?? speedButton.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => OnPointerDown(data as PointerEventData));
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => OnPointerUp(data as PointerEventData));
            trigger.triggers.Add(pointerUp);
        }
    }

    void Update()
    {
        if (currentCharacter == null || joystick == null) return;

        if (!hkTaked && !aiController.returningToBase)
        {
            if (currentCharacter.transform.position.z > -15.6)
            {
                judge.AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player crossed the line without a handkerchief, → point IA +1."
                    : $"Jugador cruzo linea sin panuelo, → punto IA +1.", aiController.currentAICharacter.transform);
            }
        }
        else if (hkTaked)
        {
            if (currentCharacter.transform.position.z > -15.6)
            {
                judge.AddPointToIA(SettingsManager.Instance.LANGUAGE.Equals("English") ? "Player crossed the line with a handkerchief towards the wrong base, → point IA +1."
                    : $"Jugador cruzo linea con panuelo hacia base equivocada, → punto IA +1.", aiController.currentAICharacter.transform);
            }
        }

        Vector3 camForward;
        CameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (camFollow != null)
            camForward = camFollow.GetCameraForward();
        else if (Camera.main != null)
            camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        else
            camForward = Vector3.forward;

        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 move = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        Vector3 moveDir = camForward * move.z + camRight * move.x;

        if (moveDir.magnitude > 0.01f)
        {
            currentCharacter.transform.Translate(moveDir.normalized * currentSpeed * Time.deltaTime, Space.World);
            currentCharacter.transform.rotation = Quaternion.Slerp(
                currentCharacter.transform.rotation,
                Quaternion.LookRotation(moveDir),
                Time.deltaTime * 10f
            );
        }

        HandleSpeed();

        if (currentAnimator != null)
            currentAnimator.SetFloat("speed", currentSpeed);

        if (currentSpeed >= 10f) SimulateRunning();

        if (speedBar != null && speedFill != null)
        {
            speedBar.value = currentSpeed;
            UpdateBarColor();
        }

        GameObject hk = (spawner != null) ? spawner.Handkerchief : null;
        Vector3 originalPos = (spawner != null) ? spawner.OriginalHandkerchiefPos : Vector3.zero;
        Vector3 playerPosFlat = new Vector3(currentCharacter.transform.position.x, 0, currentCharacter.transform.position.z);
        Vector3 hkPosFlat = new Vector3(hk.transform.position.x, 0, hk.transform.position.z);
        float distXZ = Vector3.Distance(playerPosFlat, hkPosFlat);

        float distToOriginal = Vector3.Distance(hk.transform.position, originalPos);
        float xzTolerance = 2f;

        if (distXZ <= xzTolerance && distToOriginal < 0.2f)
        {
            currentSpeed = 0;
            currentAnimator.SetFloat("speed", currentSpeed);
        }

        if (currentSpeed < 10f && currentAnimator != null && !currentAnimator.enabled)
        {
            currentAnimator.enabled = true;

            if (leftThigh != null) leftThigh.localRotation = leftThighOriginal;
            if (rightThigh != null) rightThigh.localRotation = rightThighOriginal;
            if (leftLeg != null) leftLeg.localRotation = leftLegOriginal;
            if (rightLeg != null) rightLeg.localRotation = rightLegOriginal;
            if (leftArmRun != null) leftArmRun.localRotation = leftArmOriginal;
            if (rightArmRun != null) rightArmRun.localRotation = rightArmOriginal;
            if (leftForearm != null) leftForearm.localRotation = leftForearmOriginal;
            if (rightForearm != null) rightForearm.localRotation = rightForearmOriginal;
            if (torso != null) torso.localRotation = torsoOriginal;
            if (head != null) head.localRotation = headOriginal;
        }
    }

    void LateUpdate()
    {
        if (rightArm != null && finting)
        {
            fintTimer += Time.deltaTime;
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

    public void HandleFint()
    {
        MoveRightArm();
        aiController.IAReaction();
    }

    private void HandleSpeed()
    {
        if (isSpeedButtonPressed)
            currentSpeed += speedStep * Time.deltaTime * 20f;
        else
            currentSpeed -= speedDecayRate * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, baseMoveSpeed, maxSpeed);
        if (speedBar != null) speedBar.value = currentSpeed;
    }

    private void SelectCharacter(int index)
    {
        audioManager.PlayChooseSound();

        AIController ai = FindObjectOfType<AIController>();
        ai.SelectAICharacter(index);

        judge.ValidateRightPosition(index, dialogAndEffectsManager.numberInDialog);

        if (spawner == null || spawner.teamAPlayers == null || index >= spawner.teamAPlayers.Count) return;

        currentCharacter = spawner.teamAPlayers[index];

        if (currentCharacter != null)
        {
            currentAnimator = currentCharacter.GetComponentInChildren<Animator>();
            rightArm = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r");
            rightHand = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r/forearm_stretch.r/hand.r");

            if (rightArm != null)
                originalArmRotation = rightArm.localRotation;

            if (button1 != null) button1.interactable = (index == 0);
            if (button2 != null) button2.interactable = (index == 1);
            if (button3 != null) button3.interactable = (index == 2);

            CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null) cam.SetTarget(currentCharacter.transform);
        }
    }

    public void MoveRightArm()
    {
        audioManager.PlayTakeFintSound();
        if (rightArm == null) return;

        finting = true;
        fintTimer = 0f;

        Vector3 euler = rightArm.localEulerAngles;
        float targetX = euler.x - armRaiseAngle * k;
        float targetY = euler.y;
        float targetZ = euler.z;

        targetArmRotation = Quaternion.Euler(targetX, targetY, targetZ);
        originalArmRotation = rightArm.localRotation;
    }

    private void TakeHandkerchief()
    {
        audioManager.PlayTakeFintSound();

        GameObject hk = (spawner != null) ? spawner.Handkerchief : null;
        Vector3 originalPos = (spawner != null) ? spawner.OriginalHandkerchiefPos : Vector3.zero;

        if (rightArm == null || rightHand == null || hk == null || currentCharacter == null) return;

        MoveRightArm();

        Vector3 playerPosFlat = new Vector3(currentCharacter.transform.position.x, 0, currentCharacter.transform.position.z);
        Vector3 hkPosFlat = new Vector3(hk.transform.position.x, 0, hk.transform.position.z);
        float distXZ = Vector3.Distance(playerPosFlat, hkPosFlat);
        float distToOriginal = Vector3.Distance(hk.transform.position, originalPos);
        float xzTolerance = 5f;

        if (distXZ <= xzTolerance && distToOriginal < 0.2f)
        {
            hk.transform.SetParent(rightHand);
            hk.transform.localPosition = Vector3.zero;
            hk.transform.localRotation = Quaternion.identity;
            cinematicCamera.PlayCinematic(currentCharacter.transform);
            hkTaked = true;
        }
    }

    private void UpdateBarColor()
    {
        if (speedFill == null) return;

        float t = Mathf.Clamp01(currentSpeed / maxSpeed);
        Color newColor = t < 0.33f
            ? Color.Lerp(Color.green, Color.yellow, t / 0.33f)
            : t < 0.66f
                ? Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (t - 0.33f) / 0.33f)
                : Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (t - 0.66f) / 0.34f);

        speedFill.color = newColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isSpeedButtonPressed = true;
        audioManager.PlaySpeedSound();
    }

    public void OnPointerUp(PointerEventData eventData) => isSpeedButtonPressed = false;

    public void SimulateRunning()
    {
        if (currentCharacter == null) return;

        // Desactivar el Animator si está activo
        if (currentAnimator != null && currentAnimator.enabled)
            currentAnimator.enabled = false;

        // Cachear huesos si aún no se hizo
        if (leftThigh == null)
        {
            leftThigh = currentCharacter.transform.Find("root/root.x/thigh_stretch.l");
            rightThigh = currentCharacter.transform.Find("root/root.x/thigh_stretch.r");
            leftLeg = currentCharacter.transform.Find("root/root.x/thigh_stretch.l/leg_stretch.l");
            rightLeg = currentCharacter.transform.Find("root/root.x/thigh_stretch.r/leg_stretch.r");
            leftArmRun = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.l/arm_stretch.l");
            rightArmRun = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r");
            leftForearm = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.l/arm_stretch.l/forearm.l");
            rightForearm = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/shoulder.r/arm_stretch.r/forearm.r");

            // Nuevas referencias: torso y cabeza
            torso = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x");
            head = currentCharacter.transform.Find("root/root.x/spine_01.x/spine_02.x/spine_03.x/neck.x/head.x");

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

        // Tiempo del ciclo de carrera
        runCycleTime += Time.deltaTime * 6f;

        // Parámetros de movimiento
        float thighForward = 60f;
        float rightThighForward = 70f;
        float thighBackward = 10f;
        float kneeFlex = 90f;
        float armForward = 70f;  // Hacia adelante
        float armBackward = -50f; // Hacia atrás
        float forearmFlex = 60f;  // Flexión del codo

        // Cálculo de ciclo
        float cycle = Mathf.Sin(runCycleTime);
        float oppositeCycle = Mathf.Sin(runCycleTime + Mathf.PI);

        // Movimiento de piernas
        float leftThighAngle = (cycle > 0 ? cycle * thighForward : -cycle * thighBackward);
        float rightThighAngle = (oppositeCycle > 0 ? -oppositeCycle * rightThighForward : oppositeCycle * thighBackward);
        float leftKneeAngle = (cycle > 0 ? -cycle * kneeFlex : 0);
        float rightKneeAngle = (oppositeCycle > 0 ? oppositeCycle * kneeFlex : 0);

        if (leftThigh != null) leftThigh.localRotation = leftThighOriginal * Quaternion.Euler(0, 0, leftThighAngle);
        if (rightThigh != null) rightThigh.localRotation = rightThighOriginal * Quaternion.Euler(0, 0, rightThighAngle);
        if (leftLeg != null) leftLeg.localRotation = leftLegOriginal * Quaternion.Euler(0, 0, leftKneeAngle);
        if (rightLeg != null) rightLeg.localRotation = rightLegOriginal * Quaternion.Euler(0, 0, rightKneeAngle);

        // Movimiento de brazos
        float leftArmAngle = Mathf.Lerp(armBackward, armForward, (oppositeCycle + 1f) / 2f);
        float rightArmAngle = Mathf.Lerp(armBackward, armForward, (cycle + 1f) / 2f);

        if (leftArmRun != null) leftArmRun.localRotation = leftArmOriginal * Quaternion.Euler(0, 0, leftArmAngle);
        if (rightArmRun != null) rightArmRun.localRotation = rightArmOriginal * Quaternion.Euler(0, 0, -rightArmAngle);

        // Movimiento del antebrazo
        float leftForearmAngle = (oppositeCycle > 0 ? -oppositeCycle * forearmFlex : 0);
        float rightForearmAngle = (cycle > 0 ? -cycle * forearmFlex : 0);

        if (leftForearm != null) leftForearm.localRotation = leftForearmOriginal * Quaternion.Euler(leftForearmAngle, 0, 0);
        if (rightForearm != null) rightForearm.localRotation = rightForearmOriginal * Quaternion.Euler(rightForearmAngle, 0, 0);

        // Movimiento torso y cabeza
        float torsoTilt = 5f * cycle;   // ligero movimiento del torso
        float headTilt = 15f * cycle;   // movimiento más pronunciado de la cabeza

        if (torso != null) torso.localRotation = torsoOriginal * Quaternion.Euler(torsoTilt, 0, 0);
        if (head != null) head.localRotation = headOriginal * Quaternion.Euler(headTilt, 0, 0);
    }

}
