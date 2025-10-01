using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Jugador a seguir")]
    public Transform target;          // Transform del jugador seleccionado

    [Header("Configuración de cámara")]
    public float cameraHeightY = 12f;     // Altura de la cámara sobre el jugador
    public float cameraDistanceZ = -12f;  // Distancia detrás del jugador
    public float smoothSpeed = 5f;       // Velocidad de seguimiento suave
    public float cameraLookDownAngle = 17f; // Ángulo de rotación hacia abajo

    void LateUpdate()
    {
        if (target == null) return;

        // Offset dinámico basado en las variables públicas
        Vector3 offset = new Vector3(0, cameraHeightY, cameraDistanceZ);

        // Posición deseada con offset
        Vector3 desiredPosition = target.position + offset;

        // Movimiento suave de la cámara
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Mira hacia un punto ligeramente por encima del centro del jugador
        Vector3 lookTarget = target.position + Vector3.up * 1.5f; // centro-cabeza
        transform.LookAt(lookTarget);

        // Aplica un ángulo de rotación hacia abajo usando la variable pública
        transform.rotation = Quaternion.Euler(cameraLookDownAngle, transform.rotation.eulerAngles.y, 0);
    }

    /// <summary>
    /// Método público para actualizar el jugador a seguir
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
