using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Jugador a seguir")]
    public Transform target;          // Transform del jugador seleccionado

    [Header("Configuraci�n de c�mara")]
    public float cameraHeightY = 12f;     // Altura de la c�mara sobre el jugador
    public float cameraDistanceZ = -12f;  // Distancia detr�s del jugador
    public float smoothSpeed = 5f;       // Velocidad de seguimiento suave
    public float cameraLookDownAngle = 17f; // �ngulo de rotaci�n hacia abajo

    void LateUpdate()
    {
        if (target == null) return;

        // Offset din�mico basado en las variables p�blicas
        Vector3 offset = new Vector3(0, cameraHeightY, cameraDistanceZ);

        // Posici�n deseada con offset
        Vector3 desiredPosition = target.position + offset;

        // Movimiento suave de la c�mara
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Mira hacia un punto ligeramente por encima del centro del jugador
        Vector3 lookTarget = target.position + Vector3.up * 1.5f; // centro-cabeza
        transform.LookAt(lookTarget);

        // Aplica un �ngulo de rotaci�n hacia abajo usando la variable p�blica
        transform.rotation = Quaternion.Euler(cameraLookDownAngle, transform.rotation.eulerAngles.y, 0);
    }

    /// <summary>
    /// M�todo p�blico para actualizar el jugador a seguir
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
