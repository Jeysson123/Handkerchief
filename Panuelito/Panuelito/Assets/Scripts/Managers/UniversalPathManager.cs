using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UniversalPathManager : MonoBehaviour
{
    [Header("🌍 Configuración General")]
    [Tooltip("Nombre del mapa actual: 'Parqueo' o 'Playa'")]
    public string mapName = "Parqueo";

    [Header("🚗 PARQUEO - Autos")]
    public List<GameObject> carPrefabs;
    public List<Transform> carPathPoints;
    public int numberOfCars = 1;
    public float carSpeed = 5f;
    public float rotationSmoothness = 5f;
    public float respawnDelay = 300f; // 5 minutos

    [Header("⛵ PLAYA - Barcos")]
    public List<GameObject> boatObjects;   // 🔹 Aquí arrastras los barcos ya colocados
    public Transform boatCenterPoint;      // 🔹 Centro del círculo
    public float boatCircleRadius = 20f;
    public float boatRotationSpeed = 10f;

    private List<GameObject> spawnedCars = new List<GameObject>();
    private Dictionary<GameObject, int> carCurrentPoint = new Dictionary<GameObject, int>();
    private float boatAngle;

    void Start()
    {
        if (mapName == "Parqueo") InitializeCars();
        else if (mapName == "Playa") InitializeBoats();
    }

    void Update()
    {
        if (mapName == "Parqueo") UpdateCars();
        else if (mapName == "Playa") UpdateBoats();
    }

    // ==========================================
    // 🚗 AUTOS (PARQUEO)
    // ==========================================
    void InitializeCars()
    {
        if (carPrefabs.Count == 0 || carPathPoints.Count == 0)
        {
            return;
        }

        for (int i = 0; i < numberOfCars; i++)
        {
            SpawnCar();
        }
    }

    void SpawnCar()
    {
        GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
        GameObject car = Instantiate(prefab, carPathPoints[0].position, Quaternion.identity);
        spawnedCars.Add(car);
        carCurrentPoint[car] = 0;
    }

    void UpdateCars()
    {
        List<GameObject> carsToRemove = new List<GameObject>();

        foreach (GameObject car in spawnedCars)
        {
            if (car == null) continue;

            int currentIndex = carCurrentPoint[car];
            Transform target = carPathPoints[currentIndex];

            // Movimiento suave
            Vector3 direction = (target.position - car.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
            car.transform.position = Vector3.MoveTowards(car.transform.position, target.position, carSpeed * Time.deltaTime);

            // Llegó al punto
            if (Vector3.Distance(car.transform.position, target.position) < 0.3f)
            {
                currentIndex++;

                if (currentIndex >= carPathPoints.Count)
                {
                    StartCoroutine(HandleCarRespawn(car));
                    carsToRemove.Add(car);
                    continue;
                }

                carCurrentPoint[car] = currentIndex;
            }
        }

        foreach (var car in carsToRemove)
        {
            spawnedCars.Remove(car);
            carCurrentPoint.Remove(car);
        }
    }

    IEnumerator HandleCarRespawn(GameObject car)
    {
        car.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);
        Destroy(car);
        SpawnCar();
    }

    // ==========================================
    // ⛵ BARCOS (PLAYA)
    // ==========================================
    void InitializeBoats()
    {
        if (boatObjects.Count == 0 || boatCenterPoint == null)
        {
            return;
        }

        // Posiciona los barcos iniciales distribuidos en círculo
        float angleStep = 360f / boatObjects.Count;

        for (int i = 0; i < boatObjects.Count; i++)
        {
            GameObject boat = boatObjects[i];
            if (boat == null) continue;

            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 startPos = boatCenterPoint.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * boatCircleRadius;
            boat.transform.position = startPos;

            // 🔹 Frente del barco siempre hacia afuera
            Vector3 forwardDir = (startPos - boatCenterPoint.position).normalized;
            boat.transform.rotation = Quaternion.LookRotation(forwardDir) * Quaternion.Euler(0, 90, 0); // Ajuste según tu modelo
        }
    }

    void UpdateBoats()
    {
        if (boatCenterPoint == null || boatObjects.Count == 0) return;

        boatAngle += boatRotationSpeed * Time.deltaTime;
        float angleStep = 360f / boatObjects.Count;

        for (int i = 0; i < boatObjects.Count; i++)
        {
            GameObject boat = boatObjects[i];
            if (boat == null) continue;

            float angle = (boatAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 newPos = boatCenterPoint.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * boatCircleRadius;
            boat.transform.position = newPos;

            // 🔹 Frente del barco hacia afuera
            Vector3 forwardDir = (newPos - boatCenterPoint.position).normalized;
            boat.transform.rotation = Quaternion.LookRotation(forwardDir) * Quaternion.Euler(0, 90, 0); // Ajuste según tu modelo
        }
    }

#if UNITY_EDITOR
    // 🟣 Dibuja la ruta del Parqueo
    void OnDrawGizmos()
    {
        if (carPathPoints == null || carPathPoints.Count < 2) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < carPathPoints.Count - 1; i++)
        {
            if (carPathPoints[i] && carPathPoints[i + 1])
                Gizmos.DrawLine(carPathPoints[i].position, carPathPoints[i + 1].position);
        }
    }
#endif
}
