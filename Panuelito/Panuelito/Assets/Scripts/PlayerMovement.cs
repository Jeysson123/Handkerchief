using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias externas")]
    public VariableJoystick joystick;
    public Button button1, button2, button3;
    public Button speedButton;
    public Button fintButton;
    public Button takeButton;   // <-- BOTÓN TOMAR
    public Slider speedBar;
    public Image speedFill;

    [Header("Configuración de movimiento")]
    public float baseMoveSpeed = 0f;
    public float maxSpeed = 20f;
    public float speedStep = 1f;
    public float speedDecayRate = 1f;

    [Header("Configuración de finta / tomar")]
    [Range(0f, 2f)]
    public float k = 1f;
    public float fintDuration = 1.2f;
    public float armRaiseAngle = 100f;

    [Header("Pañuelito")]
    public float takeDistance = 2f;           // distancia máxima para tomar

    public float currentSpeed;
    private bool isSpeedButtonPressed = false;

    private HandkerchiefSpawner spawner;
    private Judge judge;
    private DialogAndEffectsManager dialogAndEffectsManager;
    public GameObject currentCharacter;
    private Animator currentAnimator;

    private Transform rightArm;
    private Transform rightHand;
    private Quaternion originalArmRotation;
    private Quaternion targetArmRotation;
    private bool finting = false;
    private float fintTimer = 0f;

    void Start()
    {
        // Buscar spawner (si existe)
        spawner = FindObjectOfType<HandkerchiefSpawner>();
        judge = FindObjectOfType<Judge>();
        dialogAndEffectsManager = FindObjectOfType<DialogAndEffectsManager>();

        // Listeners para selección (SelectCharacter verifica el spawner internamente)
        if (button1 != null) button1.onClick.AddListener(() => SelectCharacter(0));
        if (button2 != null) button2.onClick.AddListener(() => SelectCharacter(1));
        if (button3 != null) button3.onClick.AddListener(() => SelectCharacter(2));

        if (fintButton != null)
            fintButton.onClick.AddListener(MoveRightArm);

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

        // Configurar eventos pointer down/up en el botón de velocidad (si existe)
        if (speedButton != null)
        {
            EventTrigger trigger = speedButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = speedButton.gameObject.AddComponent<EventTrigger>();
            else
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

        // Obtener forward de cámara de forma segura
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

        if (speedBar != null && speedFill != null)
        {
            speedBar.value = currentSpeed;
            UpdateBarColor();
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

        //IA call
        AIController ai = FindObjectOfType<AIController>();

        ai.SelectAICharacter(index); // playerIndex = 0, 1, 2

        judge.ValidateRightPosition(index, dialogAndEffectsManager.numberInDialog); //right number?

        // Verificar que el spawner y la lista existan
        if (spawner == null || spawner.teamAPlayers == null || index >= spawner.teamAPlayers.Count) return;

        currentCharacter = spawner.teamAPlayers[index];

        if (currentCharacter != null)
        {
            currentAnimator = currentCharacter.GetComponentInChildren<Animator>();

            // Buscar huesos (misma ruta que tenías)
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

    private void MoveRightArm()
    {
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
        GameObject hk = (spawner != null) ? spawner.Handkerchief : null;
        Vector3 originalPos = (spawner != null) ? spawner.OriginalHandkerchiefPos : Vector3.zero;

        if (rightArm == null || rightHand == null || hk == null || currentCharacter == null)
        {
            return;
        }

        // Animación del brazo
        MoveRightArm();

        // Distancia horizontal (XZ) desde el jugador al pañuelo
        Vector3 playerPosFlat = new Vector3(currentCharacter.transform.position.x, 0, currentCharacter.transform.position.z);
        Vector3 hkPosFlat = new Vector3(hk.transform.position.x, 0, hk.transform.position.z);
        float distXZ = Vector3.Distance(playerPosFlat, hkPosFlat);

        // Distancia desde posición original del pañuelo
        float distToOriginal = Vector3.Distance(hk.transform.position, originalPos);

        // Margen de tolerancia horizontal (puedes aumentar si es necesario)
        float xzTolerance = 5f;

        // Solo importa la distancia horizontal y que el pañuelo esté en su posición original
        if (distXZ <= xzTolerance && distToOriginal < 0.2f)
        {
            hk.transform.SetParent(rightHand);
            hk.transform.localPosition = Vector3.zero;
            hk.transform.localRotation = Quaternion.identity;
        }
        else
        {
        }
    }




    private void UpdateBarColor()
    {
        if (speedFill == null) return;

        float t = currentSpeed / maxSpeed;
        t = Mathf.Clamp01(t);

        Color newColor = t < 0.33f
            ? Color.Lerp(Color.green, Color.yellow, t / 0.33f)
            : t < 0.66f
                ? Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (t - 0.33f) / 0.33f)
                : Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (t - 0.66f) / 0.34f);

        speedFill.color = newColor;
    }

    public void OnPointerDown(PointerEventData eventData) => isSpeedButtonPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isSpeedButtonPressed = false;
}
