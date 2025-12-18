using UnityEngine;

/// <summary>
/// Place this component in each scene to control what music plays.
/// Automatically changes music when the scene loads.
/// Only one instance should exist per scene.
/// </summary>
public class SceneMusicController : MonoBehaviour
{
    [Header("Scene Music")]
    [Tooltip("Music to play in this scene. Leave empty to stop music.")]
    [SerializeField] private AudioClip sceneMusic;

    [Tooltip("Volume for this scene's music (0-1).")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Tooltip("If true, music starts immediately on scene load. If false, call PlayMusic() manually.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("If true, stops current music before playing new one. If false, crossfades.")]
    [SerializeField] private bool stopCurrentMusic = true;

    private void Start()
    {
        if (playOnStart)
        {
            PlayMusic();
        }
    }

    /// <summary>
    /// Play this scene's music.
    /// </summary>
    public void PlayMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusicController] AudioManager.Instance is null. Cannot play music.");
            return;
        }

        // Stop current music if requested
        if (stopCurrentMusic)
        {
            AudioManager.Instance.StopMusic();
        }

        // Play new music if assigned
        if (sceneMusic != null)
        {
            AudioManager.Instance.PlayMusic(sceneMusic, musicVolume);
        }
        else
        {
            // No music assigned - just stop current music
            AudioManager.Instance.StopMusic();
        }
    }

    /// <summary>
    /// Stop music in this scene.
    /// </summary>
    public void StopMusic()
    {
        AudioManager.Instance?.StopMusic();
    }

    /// <summary>
    /// Change the music volume.
    /// </summary>
    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        AudioManager.Instance?.SetMusicVolume(musicVolume);
    }
}
