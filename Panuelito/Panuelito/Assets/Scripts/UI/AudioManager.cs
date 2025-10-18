using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Música de fondo")]
    public AudioClip backgroundMusic;

    [Header("Clips de efectos")]
    public AudioClip takeFintSound;
    public AudioClip speedSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip chooseSound;

    [Header("Clips de clima")]
    public AudioClip rainSound;
    public AudioClip thunderSound;
    public AudioClip nightCricketsSound;
    public AudioClip windSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource weatherSource; // 🔊 Sonidos de clima en loop

    private SettingsManager settingsManager;

    private void Awake()
    {
        // Canales de audio separados
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        weatherSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.3f; // 🎵 Música de fondo suave por defecto

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        weatherSource.loop = true;
        weatherSource.playOnAwake = false;

        settingsManager = FindObjectOfType<SettingsManager>();
    }

    private void Start()
    {
        PlayBackgroundMusic();
        UpdateVolumeFromSettings();
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null) return;
        if (!musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        musicSource.Stop();
    }

    // 🔊 Actualiza volumen de música, efectos y clima
    public void UpdateVolumeFromSettings()
    {
        if (settingsManager != null)
        {
            float volume = Mathf.Clamp01(settingsManager.SOUND_LEVEL / 100f);
            musicSource.volume = volume * 0.3f; // Mantener música de fondo más baja
            sfxSource.volume = volume;
            weatherSource.volume = volume;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(PlaySFXWithMusicPause(clip));
    }

    private IEnumerator PlaySFXWithMusicPause(AudioClip clip)
    {
        bool wasPlaying = musicSource.isPlaying;
        if (wasPlaying) musicSource.volume *= 0.2f; // Bajar música temporalmente

        sfxSource.PlayOneShot(clip);
        yield return new WaitForSeconds(clip.length);

        if (wasPlaying) musicSource.volume /= 0.2f; // Restaurar volumen original
    }

    // --- Métodos de efectos cortos ---
    public void PlayTakeFintSound() => PlaySFX(takeFintSound);
    public void PlaySpeedSound() => PlaySFX(speedSound);
    public void PlayWinSound() => PlaySFX(winSound);
    public void PlayLoseSound() => PlaySFX(loseSound);
    public void PlayChooseSound() => PlaySFX(chooseSound);

    // --- 🎵 Métodos de clima (ambientales) ---
    public void PlayRainSound() => PlayWeatherLoop(rainSound);
    public void PlayNightCricketsSound() => PlayWeatherLoop(nightCricketsSound);
    public void PlayWindSound() => PlayWeatherLoop(windSound);
    public void PlayThunderSound() => PlaySFX(thunderSound); // Truenos cortos

    // --- Control general ---
    public void StopAll()
    {
        // 🔔 Música de fondo no se detiene
        sfxSource.Stop();
        weatherSource.Stop();
    }

    public void StopWeatherSounds()
    {
        weatherSource.Stop();
    }

    // --- 🔁 Manejo del canal ambiental ---
    private void PlayWeatherLoop(AudioClip clip)
    {
        if (clip == null) return;

        if (weatherSource.clip == clip && weatherSource.isPlaying)
            return; // Ya se está reproduciendo este sonido

        weatherSource.clip = clip;
        weatherSource.Play();
    }
}
