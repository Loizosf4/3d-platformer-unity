// ============================================================================
// AUDIO SYSTEM - QUICK REFERENCE CARD
// ============================================================================
// Copy-paste these snippets into your scripts as needed.
// All methods are null-safe and handle missing AudioManager gracefully.
// ============================================================================

// ----------------------------------------------------------------------------
// BASIC ONE-SHOT SFX
// ----------------------------------------------------------------------------
// Use this for any simple sound effect (button clicks, explosions, etc.)
// AudioManager.Instance?.PlaySFX(myAudioClip, volume: 0.7f);

// ----------------------------------------------------------------------------
// SPATIAL SFX (3D World Position)
// ----------------------------------------------------------------------------
// Use this for sounds tied to a specific location (pickups, hazards, etc.)
// Sound will persist even if the GameObject is destroyed
// AudioManager.Instance?.PlayAtPosition(
//     clip: explosionSound,
//     position: transform.position,
//     volume: 1f,
//     spatialBlend: 1f  // 0 = 2D, 1 = 3D
// );

// ----------------------------------------------------------------------------
// BACKGROUND MUSIC CONTROL
// ----------------------------------------------------------------------------
// Play new music (replaces current track)
// AudioManager.Instance?.PlayMusic(newMusicClip, volume: 0.5f);

// Stop music
// AudioManager.Instance?.StopMusic();

// Pause/Resume music
// AudioManager.Instance?.PauseMusic();
// AudioManager.Instance?.ResumeMusic();

// Change music volume
// AudioManager.Instance?.SetMusicVolume(0.3f); // 0-1

// ----------------------------------------------------------------------------
// MIXER VOLUME CONTROL (For Settings Menu)
// ----------------------------------------------------------------------------
// Set music volume via mixer
// AudioManager.Instance?.SetMixerVolume("Music", 0.5f);

// Set SFX volume via mixer
// AudioManager.Instance?.SetMixerVolume("SFX", 0.8f);

// ----------------------------------------------------------------------------
// PLAYER AUDIO INTEGRATION (Already Done in PlayerAudio.cs)
// ----------------------------------------------------------------------------
// PlayerAudio automatically hooks into PlayerMotorCC events:
// - OnJump → Play jump sound
// - OnDash → Play dash sound
// - Step-based footsteps (grounded + moving)
// 
// Just add PlayerAudio component to player prefab and assign clips!

// ----------------------------------------------------------------------------
// COLLECTIBLE AUDIO INTEGRATION
// ----------------------------------------------------------------------------
// Add CollectibleAudio component to collectible prefabs
// Assign ambient loop and pickup sound in Inspector
// The component automatically:
// - Loops ambient sound while alive
// - Stops loop on pickup
// - Plays pickup sound (survives destruction)

// ----------------------------------------------------------------------------
// HAZARD AUDIO INTEGRATION
// ----------------------------------------------------------------------------
// Example: HailstormZone with audio
//
// using UnityEngine;
//
// public class HailstormZoneWithAudio : MonoBehaviour
// {
//     private HazardAudio _hazardAudio;
//
//     private void Awake()
//     {
//         _hazardAudio = GetComponent<HazardAudio>();
//     }
//
//     private void ActivateStorm()
//     {
//         // Your existing hazard activation logic...
//         
//         // Start hazard audio (loop + optional stinger)
//         _hazardAudio?.StartHazardAudio();
//     }
//
//     private void DeactivateStorm()
//     {
//         // Your existing hazard deactivation logic...
//         
//         // Stop hazard audio (loop + optional stinger)
//         _hazardAudio?.StopHazardAudio();
//     }
// }

// ----------------------------------------------------------------------------
// LOOPING AUDIO (Custom Implementation)
// ----------------------------------------------------------------------------
// For custom looping sounds (not hazards or collectibles)
//
// public class CustomLoopingAudio : MonoBehaviour
// {
//     [SerializeField] private AudioClip loopClip;
//     private AudioSource _loopSource;
//
//     private void Start()
//     {
//         _loopSource = gameObject.AddComponent<AudioSource>();
//         _loopSource.clip = loopClip;
//         _loopSource.loop = true;
//         _loopSource.spatialBlend = 1f; // 3D sound
//         
//         // Route to SFX mixer group
//         if (AudioManager.Instance?.audioMixer != null)
//         {
//             var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
//             if (sfxGroup != null && sfxGroup.Length > 0)
//                 _loopSource.outputAudioMixerGroup = sfxGroup[0];
//         }
//         
//         _loopSource.Play();
//     }
//
//     private void OnDestroy()
//     {
//         if (_loopSource != null) _loopSource.Stop();
//     }
// }

// ----------------------------------------------------------------------------
// BOOTSTRAPPER INTEGRATION
// ----------------------------------------------------------------------------
// Add this to Bootstrapper.cs Awake() method:

// [Header("Audio (Optional)")]
// [SerializeField] private GameObject audioManagerPrefab;
//
// private void Awake()
// {
//     // Initialize audio FIRST
//     if (AudioManager.Instance == null && audioManagerPrefab != null)
//         Instantiate(audioManagerPrefab);
//     
//     // ... rest of bootstrapper code
// }

// ----------------------------------------------------------------------------
// SCENE TRANSITION MUSIC EXAMPLE
// ----------------------------------------------------------------------------
// Change music when entering a new scene/area
//
// public class SceneMusicTrigger : MonoBehaviour
// {
//     [SerializeField] private AudioClip newSceneMusic;
//     [SerializeField] private float musicVolume = 0.5f;
//
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             AudioManager.Instance?.PlayMusic(newSceneMusic, musicVolume);
//         }
//     }
// }

// ============================================================================
// TROUBLESHOOTING
// ============================================================================
//
// NO AUDIO PLAYING?
// 1. Check AudioManager exists in scene (via Bootstrapper or manually)
// 2. Verify AudioMixer is assigned to AudioManager
// 3. Check AudioClips are assigned to components
// 4. Verify volumes are > 0
//
// PICKUP SOUND CUTS OFF?
// - Use AudioManager.PlayAtPosition() (not regular PlaySFX)
// - This creates temporary AudioSource that survives destruction
//
// FOOTSTEPS SPAMMING?
// - Adjust stepInterval in PlayerAudio (increase for slower steps)
// - Check minSpeedForFootsteps threshold
//
// DUPLICATE MUSIC ON SCENE LOAD?
// - Ensure only ONE AudioManager exists
// - AudioManager should be in Bootstrapper (DontDestroyOnLoad)
//
// ============================================================================
