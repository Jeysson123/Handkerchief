using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias externas")]
    public VariableJoystick joystick;   // Joystick UI para mover personajes
    public Button button1;              // Botón para seleccionar jugador en posición 0
    public Button button2;              // Botón para seleccionar jugador en posición 1
    public Button button3;              // Botón para seleccionar jugador en posición 2

    [Header("Configuración de movimiento")]
    public float moveSpeed = 5f;        // Velocidad de movimiento

    private HandkerchiefSpawner spawner;
    private GameObject currentCharacter; // Personaje actualmente seleccionado

    void Start()
    {
        // 🔎 Busca el Spawner en la escena
        spawner = FindObjectOfType<HandkerchiefSpawner>();

        if (spawner == null)
        {
            Debug.LogError("❌ No se encontró el HandkerchiefSpawner en la escena.");
            return;
        }

        // 🟡 Asigna los listeners de los botones a sus posiciones
        if (button1 != null) button1.onClick.AddListener(() => SelectCharacter(0));
        if (button2 != null) button2.onClick.AddListener(() => SelectCharacter(1));
        if (button3 != null) button3.onClick.AddListener(() => SelectCharacter(2));
    }

    void Update()
    {
        if (currentCharacter == null || joystick == null) return;

        // 🎮 Movimiento con joystick solo del jugador seleccionado
        Vector3 move = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        currentCharacter.transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Selecciona un personaje de Team A por índice y hace que la cámara lo siga
    /// </summary>
    private void SelectCharacter(int index)
    {
        if (spawner.teamAPlayers == null || index >= spawner.teamAPlayers.Count)
        {
            Debug.LogWarning($"❌ No hay jugador en el índice {index}");
            return;
        }

        currentCharacter = spawner.teamAPlayers[index];
        Debug.Log($"✅ Jugador seleccionado en posición {index}: {currentCharacter.name}");

        // 🔹 Hacer que la cámara siga al jugador seleccionado
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(currentCharacter.transform);
        }
        else
        {
            Debug.LogWarning("❌ No se encontró el script CameraFollow en Main Camera.");
        }
    }
}
