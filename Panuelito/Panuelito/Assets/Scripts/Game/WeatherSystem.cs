using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeatherSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Light directionalLight;
    public ParticleSystem rainEffect;
    public GameObject cloudPrefab;
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Audio Manager")]
    public AudioManager audioManager;

    [Header("Configuración del clima")]
    public float weatherChangeInterval = 120f;
    public float dayNightTransitionSpeed = 0.5f;
    public float sunRotationSpeed = 5f;

    [Header("Configuración de nubes")]
    public int cloudCount = 15;
    public Vector2 cloudSpawnArea = new Vector2(150, 150);
    public float cloudHeight = 60f;
    public float cloudMoveSpeed = 3f;
    public float cloudFadeSpeed = 0.5f;

    [Header("Iluminación mínima de noche")]
    [Range(0.2f, 1f)]
    public float minNightIntensity = 0.45f;
    public Color ambientNightColor = new Color(0.25f, 0.3f, 0.4f);
    public Color ambientDayColor = new Color(1f, 0.98f, 0.9f);

    private bool isDay = true;
    private bool isRaining = false;
    private bool hasClouds = false;
    private float timer;

    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();
        if (directionalLight == null)
            directionalLight = RenderSettings.sun;

        timer = weatherChangeInterval;
        ApplyWeather();

        // Asegura un inicio equilibrado
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientDayColor;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            isDay = Random.value > 0.5f;
            isRaining = Random.value > 0.5f;
            hasClouds = Random.value > 0.3f;

            ApplyWeather();
            timer = weatherChangeInterval;
        }

        if (directionalLight != null)
            directionalLight.transform.Rotate(Vector3.right, sunRotationSpeed * Time.deltaTime);

        if (directionalLight != null)
        {
            float targetIntensity = isDay ? 1.2f : minNightIntensity;
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, Time.deltaTime * dayNightTransitionSpeed);

            Color targetColor = isDay
                ? new Color(1f, 0.97f, 0.9f)
                : new Color(0.4f, 0.5f, 0.7f);
            directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * dayNightTransitionSpeed);

            RenderSettings.ambientLight = Color.Lerp(
                RenderSettings.ambientLight,
                isDay ? ambientDayColor : ambientNightColor,
                Time.deltaTime * dayNightTransitionSpeed
            );
        }

        if (hasClouds)
            MoveClouds();
    }

    private void ApplyWeather()
    {
        RenderSettings.skybox = isDay ? daySkybox : nightSkybox;

        if (rainEffect != null)
        {
            if (isRaining && !rainEffect.isPlaying)
                rainEffect.Play();
            else if (!isRaining && rainEffect.isPlaying)
                rainEffect.Stop();
        }

        ManageClouds();

        if (audioManager != null)
            StartCoroutine(SwapWeatherSound());

        Debug.Log($"🌤️ Clima → {(isDay ? "Día" : "Noche")} | {(isRaining ? "Lluvia" : "Soleado")} | {(hasClouds ? "Nubes" : "Despejado")}");
    }

    private IEnumerator SwapWeatherSound()
    {
        audioManager.StopWeatherSounds();
        yield return new WaitForSeconds(0.1f);

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

    private void ManageClouds()
    {
        if (!hasClouds)
        {
            StartCoroutine(FadeOutClouds());
            return;
        }

        if (activeClouds.Count >= cloudCount) return;
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

    private IEnumerator PlayThunderRandom()
    {
        while (isRaining && audioManager != null)
        {
            yield return new WaitForSeconds(Random.Range(10f, 25f));
            audioManager.PlayThunderSound();
        }
    }
}
