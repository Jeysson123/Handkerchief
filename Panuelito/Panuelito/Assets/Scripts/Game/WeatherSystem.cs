using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeatherSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Light directionalLight;          // Sol o luna
    public ParticleSystem rainEffect;       // Lluvia
    public GameObject cloudPrefab;          // Prefab de nube individual
    public Material daySkybox;              // Cielo de día
    public Material nightSkybox;            // Cielo de noche

    [Header("Audio Manager")]
    public AudioManager audioManager;       // Referencia al AudioManager

    [Header("Configuración del clima")]
    public float weatherChangeInterval = 25f;       // Cada cuánto cambia el clima
    public float dayNightTransitionSpeed = 0.5f;    // Velocidad del cambio día/noche
    public float sunRotationSpeed = 5f;             // Velocidad de rotación del sol

    [Header("Configuración de nubes")]
    public int cloudCount = 15;                     // Cantidad máxima de nubes
    public Vector2 cloudSpawnArea = new Vector2(150, 150); // Área donde aparecen
    public float cloudHeight = 60f;
    public float cloudMoveSpeed = 3f;
    public float cloudFadeSpeed = 0.5f;

    private bool isDay = true;
    private bool isRaining = false;
    private bool hasClouds = false;
    private float timer;

    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        // Buscar componentes en escena
        audioManager = FindObjectOfType<AudioManager>();
        if (directionalLight == null)
            directionalLight = RenderSettings.sun;

        timer = weatherChangeInterval;
        ApplyWeather();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // Cambio aleatorio de clima
            isDay = Random.value > 0.5f;
            isRaining = Random.value > 0.5f;
            hasClouds = Random.value > 0.3f;

            ApplyWeather();
            timer = weatherChangeInterval;
        }

        // Rotar la luz solar (simula el paso del día)
        if (directionalLight != null)
            directionalLight.transform.Rotate(Vector3.right, sunRotationSpeed * Time.deltaTime);

        // Transición de luz y color suave
        if (directionalLight != null)
        {
            float targetIntensity = isDay ? 1.1f : 0.15f;
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, Time.deltaTime * dayNightTransitionSpeed);

            Color targetColor = isDay ? new Color(1f, 0.95f, 0.8f) : new Color(0.25f, 0.3f, 0.5f);
            directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * dayNightTransitionSpeed);
        }

        // Mover las nubes
        if (hasClouds)
            MoveClouds();
    }

    private void ApplyWeather()
    {
        // Cambiar skybox
        RenderSettings.skybox = isDay ? daySkybox : nightSkybox;

        // Control de lluvia
        if (rainEffect != null)
        {
            if (isRaining && !rainEffect.isPlaying)
                rainEffect.Play();
            else if (!isRaining && rainEffect.isPlaying)
                rainEffect.Stop();
        }

        // Control de nubes
        ManageClouds();

        // --- 🎧 Sonidos ---
        if (audioManager != null)
        {
            audioManager.StopWeatherSounds(); // ⛔ No detiene música, solo el clima

            if (isRaining)
            {
                audioManager.PlayRainSound();
                StartCoroutine(PlayThunderRandom());
            }
            else if (!isDay)
            {
                audioManager.PlayNightCricketsSound();
            }
            else if (hasClouds)
            {
                audioManager.PlayWindSound();
            }
        }

        Debug.Log($"🌤️ Clima → {(isDay ? "Día" : "Noche")} | {(isRaining ? "Lluvia" : "Soleado")} | {(hasClouds ? "Nubes" : "Cielo despejado")}");
    }

    // --- ☁️ Gestión de nubes ---
    private void ManageClouds()
    {
        if (!hasClouds)
        {
            // Desaparecer lentamente las nubes
            StartCoroutine(FadeOutClouds());
            return;
        }

        // Si ya hay nubes activas, no crear más
        if (activeClouds.Count >= cloudCount) return;

        // Crear nubes nuevas poco a poco
        StartCoroutine(SpawnClouds());
    }

    private IEnumerator SpawnClouds()
    {
        while (activeClouds.Count < cloudCount)
        {
            Vector3 pos = new Vector3(
                Random.Range(-cloudSpawnArea.x / 2, cloudSpawnArea.x / 2),
                cloudHeight + Random.Range(-5f, 5f),
                Random.Range(-cloudSpawnArea.y / 2, cloudSpawnArea.y / 2)
            );

            GameObject newCloud = Instantiate(cloudPrefab, pos, Quaternion.identity);
            newCloud.transform.localScale *= Random.Range(0.7f, 1.5f);
            activeClouds.Add(newCloud);

            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }

    private IEnumerator FadeOutClouds()
    {
        List<GameObject> toRemove = new List<GameObject>(activeClouds);

        foreach (GameObject cloud in toRemove)
        {
            if (cloud == null) continue;
            float fade = 1f;

            Renderer r = cloud.GetComponent<Renderer>();
            if (r != null && r.material.HasProperty("_Color"))
            {
                Color baseColor = r.material.color;

                while (fade > 0f)
                {
                    fade -= Time.deltaTime * cloudFadeSpeed;
                    Color c = baseColor;
                    c.a = fade;
                    r.material.color = c;
                    yield return null;
                }
            }

            Destroy(cloud);
            activeClouds.Remove(cloud);
        }
    }

    private void MoveClouds()
    {
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud == null) continue;

            cloud.transform.position += new Vector3(cloudMoveSpeed * Time.deltaTime, 0f, 0f);

            if (cloud.transform.position.x > cloudSpawnArea.x / 2)
            {
                cloud.transform.position = new Vector3(
                    -cloudSpawnArea.x / 2,
                    cloudHeight + Random.Range(-5f, 5f),
                    Random.Range(-cloudSpawnArea.y / 2, cloudSpawnArea.y / 2)
                );
            }
        }
    }

    // --- ⚡ Truenos aleatorios ---
    private IEnumerator PlayThunderRandom()
    {
        while (isRaining && audioManager != null)
        {
            yield return new WaitForSeconds(Random.Range(10f, 25f));
            audioManager.PlayThunderSound();
        }
    }
}
