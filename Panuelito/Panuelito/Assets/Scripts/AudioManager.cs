using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("🎵 Música de fondo")]
    public AudioClip backgroundMusic;

    [Header("🎧 Clips de efectos")]
    public AudioClip takeFintSound; // sonido al tomar el pañuelo
    public AudioClip speedSound;    // sonido al correr rápido
    public AudioClip winSound;      // sonido al ganar
    public AudioClip loseSound;     // sonido al perder
    public AudioClip chooseSound;   // sonido al seleccionar jugador o acción

    // AudioSources creados automáticamente
    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // Singleton (solo una instancia)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear AudioSources por código
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Configurar música de fondo
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Configurar efectos de sonido
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    void Start()
    {
        PlayBackgroundMusic();
    }

    // ===========================
    // 🎵 Música de fondo
    // ===========================
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null) return;
        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    public void StopBackgroundMusic()
    {
        musicSource.Stop();
    }

    // ===========================
    // 🔊 Efectos de sonido
    // ===========================
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(PlaySFXWithMusicPause(clip));
    }

    private IEnumerator PlaySFXWithMusicPause(AudioClip clip)
    {
        // Pausar música si está reproduciéndose
        bool wasPlaying = musicSource.isPlaying;
        if (wasPlaying) musicSource.Pause();

        // Reproducir sonido
        sfxSource.PlayOneShot(clip);

        // Esperar a que termine el clip
        yield return new WaitForSeconds(clip.length);

        // Reanudar música si estaba sonando antes
        if (wasPlaying) musicSource.UnPause();
    }

    // ===========================
    // 🔈 Métodos públicos
    // ===========================
    public void PlayTakeFintSound() => PlaySFX(takeFintSound);
    public void PlaySpeedSound() => PlaySFX(speedSound);
    public void PlayWinSound() => PlaySFX(winSound);
    public void PlayLoseSound() => PlaySFX(loseSound);
    public void PlayChooseSound() => PlaySFX(chooseSound);
}
