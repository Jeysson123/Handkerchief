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

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private SettingsManager settingsManager; // Referencia al SettingsManager

    private void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        // Buscar SettingsManager en la escena
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
        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    public void StopBackgroundMusic()
    {
        musicSource.Stop();
    }

    // Actualiza volumen de música y efectos según SettingsManager
    public void UpdateVolumeFromSettings()
    {
        if (settingsManager != null)
        {
            float volume = Mathf.Clamp01(settingsManager.SOUND_LEVEL / 100f);
            musicSource.volume = volume;
            sfxSource.volume = volume;
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
        if (wasPlaying) musicSource.Pause();

        sfxSource.PlayOneShot(clip);

        yield return new WaitForSeconds(clip.length);

        if (wasPlaying) musicSource.UnPause();
    }

    public void PlayTakeFintSound() => PlaySFX(takeFintSound);
    public void PlaySpeedSound() => PlaySFX(speedSound);
    public void PlayWinSound() => PlaySFX(winSound);
    public void PlayLoseSound() => PlaySFX(loseSound);
    public void PlayChooseSound() => PlaySFX(chooseSound);
}
