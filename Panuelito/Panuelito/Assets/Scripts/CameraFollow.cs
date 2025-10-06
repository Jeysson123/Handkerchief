using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Jugador a seguir")]
    public Transform target;

    [Header("Configuración de cámara")]
    public Vector3 offset = new Vector3(0f, 5f, -7f); // Y aumentado de 3 a 5
    public float smoothSpeed = 10f;
    public float rotationSpeed = 0.2f;
    public float minPitch = -40f;  // Limite hacia abajo
    public float maxPitch = 45f;

    public float yaw = 0f;
    public float pitch = 20f; // Pitch inicial hacia abajo más pronunciado
    private Vector2 lastTouchPos;
    public bool isDragging = false;

    void LateUpdate()
    {
        if (target == null) return;

        // Calcular el centro del jugador usando bounds del Renderer
        Vector3 lookPos = GetTargetCenter(target);

        HandleDragRotation();

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 horizontalOffset = Quaternion.Euler(0, yaw, 0) * new Vector3(offset.x, 0, offset.z);
        Vector3 desiredPosition = target.position + horizontalOffset + Vector3.up * offset.y;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(lookPos);
    }

    private Vector3 GetTargetCenter(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position + Vector3.up * 1.5f;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            bounds.Encapsulate(rend.bounds);
        }

        return bounds.center;
    }

    private void HandleDragRotation()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            lastTouchPos = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastTouchPos;
            lastTouchPos = Input.mousePosition;

            yaw += delta.x * rotationSpeed;
            pitch -= delta.y * rotationSpeed;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
#else
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPos = touch.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.deltaPosition;
                yaw += delta.x * rotationSpeed;
                pitch -= delta.y * rotationSpeed;
                lastTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
#endif
    }

    public void SetTarget(Transform newTarget)
    {
        if(!newTarget) return;
        target = newTarget;
        yaw = target.eulerAngles.y;
    }

    public Vector3 GetCameraForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        return forward.normalized;
    }
}
