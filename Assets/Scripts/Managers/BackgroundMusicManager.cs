using UnityEngine;

/// <summary>
/// Verwaltet die Hintergrundmusik für das Spiel
/// </summary>
public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private void Awake()
    {
        // Erstelle AudioSource falls nicht vorhanden
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Konfiguriere AudioSource - Stelle sicher, dass alle Werte korrekt sind
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.pitch = 1.0f; // ✅ Stelle sicher, dass Pitch normal ist (1.0 = normale Geschwindigkeit)
        audioSource.priority = 0; // Niedrigste Priorität, damit Sound-Effekte nicht unterbrochen werden
        audioSource.spatialBlend = 0f; // 2D Sound (nicht 3D)
        audioSource.bypassEffects = false;
        audioSource.bypassListenerEffects = false;
        audioSource.bypassReverbZones = false;
        
        // Lade Musik aus Resources falls nicht im Inspector zugewiesen
        if (backgroundMusic == null)
        {
            backgroundMusic = Resources.Load<AudioClip>("Audio/Clash of Clans_ Main Theme");
            if (backgroundMusic == null)
            {
                Debug.LogWarning("[BackgroundMusicManager] Hintergrundmusik nicht gefunden. Bitte im Inspector zuweisen oder in Resources/Audio/ ablegen.");
            }
        }
    }

    private void Start()
    {
        if (playOnStart && backgroundMusic != null)
        {
            PlayBackgroundMusic();
        }
    }

    /// <summary>
    /// Startet die Hintergrundmusik
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (audioSource == null || backgroundMusic == null)
        {
            Debug.LogWarning("[BackgroundMusicManager] AudioSource oder Musik fehlt!");
            return;
        }

        // ✅ Stelle sicher, dass Pitch korrekt ist (1.0 = normale Geschwindigkeit)
        audioSource.pitch = 1.0f;
        audioSource.clip = backgroundMusic;
        audioSource.Play();
        Debug.Log($"[BackgroundMusicManager] Hintergrundmusik gestartet: {backgroundMusic.name} (Pitch: {audioSource.pitch})");
    }

    /// <summary>
    /// Stoppt die Hintergrundmusik
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[BackgroundMusicManager] Hintergrundmusik gestoppt.");
        }
    }

    /// <summary>
    /// Setzt die Lautstärke der Hintergrundmusik
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    /// <summary>
    /// Pausiert die Hintergrundmusik
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// Setzt die Hintergrundmusik fort
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
}
