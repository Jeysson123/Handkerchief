using UnityEngine;
using System.Collections;

public class CinematicCameraController : MonoBehaviour
{
    [Header("Referencias")]
    private CameraFollow cameraFollow;  // Tu script normal de seguimiento
    public float cinematicDuration = 0.2f;
    public float rotationSpeed = 30f;
    public float zoomDistance = 15f;
    public float slowMotionScale = 0.3f;

    private bool isCinematic = false;
    private Transform focusTarget;
    private Vector3 originalOffset;

    void Start()
    {
        cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
            originalOffset = cameraFollow.offset;
    }

    void LateUpdate()
    {
        if (isCinematic && focusTarget != null)
        {
            // Rota lentamente alrededor del objetivo
            transform.RotateAround(focusTarget.position, Vector3.up, rotationSpeed * Time.unscaledDeltaTime);
            transform.LookAt(focusTarget.position + Vector3.up * 1.5f);
        }
    }

    public void PlayCinematic(Transform focus)
    {
        if (isCinematic) return;
        focusTarget = focus;
        StartCoroutine(CinematicRoutine());
    }

    private IEnumerator CinematicRoutine()
    {
        isCinematic = true;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Ajusta posición inicial de cámara
        Vector3 direction = (transform.position - focusTarget.position).normalized;
        transform.position = focusTarget.position + direction * zoomDistance + Vector3.up * 2f;

        yield return new WaitForSecondsRealtime(cinematicDuration);

        // Restaurar
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isCinematic = false;

        if (cameraFollow != null)
            cameraFollow.enabled = true;
    }
}
