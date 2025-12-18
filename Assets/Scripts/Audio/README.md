# Audio System Integration Guide

## Overview
This audio system is **fully additive** and **scene-reload safe**. It requires zero modifications to existing gameplay scripts.

---

## ✅ What Was Created

### Scripts
- **AudioManager.cs** - Persistent singleton (DontDestroyOnLoad)
- **PlayerAudio.cs** - Hooks into PlayerMotorCC events
- **CollectibleAudio.cs** - Ambient loop + pickup one-shot
- **HazardAudio.cs** - Interval-based hazard loops

### Assets
- **MainAudioMixer.mixer** - AudioMixer with Music/SFX groups

**Location:** `Assets/Scripts/Audio/` and `Assets/Audio/MainAudioMixer.mixer`

---

## 🔧 Setup Instructions

### 1. Create AudioManager GameObject (Required)

**Option A: Add to Bootstrapper (Recommended)**
1. Create prefab: `PF_AudioManager`
2. Add `AudioManager` component
3. Assign `MainAudioMixer.mixer` to the AudioMixer field
4. Assign background music AudioClip if desired
5. Add prefab reference to `Bootstrapper.cs`

**Option B: Manual Scene Setup**
1. Create GameObject: "AudioManager"
2. Add `AudioManager` component
3. Assign AudioMixer and music clips
4. AudioManager will persist via DontDestroyOnLoad

---

### 2. Setup Player Audio

**On Player Prefab (PF_player):**
1. Add `PlayerAudio` component
2. Assign AudioClips:
   - Jump Sound: `jump2.wav`
   - Dash Sound: `Dash.mp3`
   - Footstep Sounds: `WalkingSounds.mp3` (or array of clips)
3. Adjust volumes and step interval as needed

**PlayerAudio will automatically:**
- Subscribe to `PlayerMotorCC.OnJump` event
- Subscribe to `PlayerMotorCC.OnDash` event
- Play footsteps based on speed + grounded state

---

### 3. Setup Collectible Audio (Optional)

**On Collectible Prefabs:**
1. Add `CollectibleAudio` component
2. Assign:
   - Ambient Loop Clip: (sparkle/hum sound)
   - Pickup Sound: (collection sound)
3. Adjust volumes and spatial blend

**CollectibleAudio will:**
- Loop ambient sound while collectible exists
- Stop loop immediately on pickup
- Play pickup sound via AudioManager (survives destruction)

---

### 4. Setup Hazard Audio (Optional)

**On Interval Hazards (HailstormZone, etc.):**
1. Add `HazardAudio` component
2. Assign:
   - Loop Clip: Wind/storm sound
   - Start Stinger: (optional activation sound)
   - Stop Stinger: (optional deactivation sound)
3. Call methods from hazard script:
   ```csharp
   private HazardAudio _hazardAudio;
   
   void Awake() {
       _hazardAudio = GetComponent<HazardAudio>();
   }
   
   void ActivateHazard() {
       // Existing hazard logic...
       _hazardAudio?.StartHazardAudio();
   }
   
   void DeactivateHazard() {
       // Existing hazard logic...
       _hazardAudio?.StopHazardAudio();
   }
   ```

---

## 🎵 AudioMixer Configuration

**MainAudioMixer** has three groups:
- **Master** - Top-level control
- **Music** - Background music routing
- **SFX** - All sound effects (player, collectibles, hazards)

**Exposed Parameters:**
- `Music` - Controls music volume
- `SFX` - Controls SFX volume

**Programmatic Control:**
```csharp
// Set music volume (0-1)
AudioManager.Instance.SetMixerVolume("Music", 0.5f);

// Set SFX volume (0-1)
AudioManager.Instance.SetMixerVolume("SFX", 0.8f);
```

---

## 📝 Usage Examples

### Play One-Shot SFX
```csharp
if (AudioManager.Instance != null)
{
    AudioManager.Instance.PlaySFX(myClip, volume: 0.7f);
}
```

### Play Sound at World Position (Survives Destruction)
```csharp
// Perfect for collectibles or objects that get destroyed
AudioManager.Instance?.PlayAtPosition(
    clip: pickupSound,
    position: transform.position,
    volume: 0.8f,
    spatialBlend: 1f // 0 = 2D, 1 = 3D
);
```

### Control Background Music
```csharp
AudioManager.Instance?.PlayMusic(newMusicClip);
AudioManager.Instance?.StopMusic();
AudioManager.Instance?.PauseMusic();
AudioManager.Instance?.ResumeMusic();
```

---

## 🔒 Safety Guarantees

### No Breaking Changes
- ✅ Zero modifications to PlayerMotorCC
- ✅ Zero modifications to Collectible
- ✅ Zero modifications to hazard logic
- ✅ All audio components are optional

### Scene Reload Safe
- ✅ AudioManager persists via DontDestroyOnLoad
- ✅ No duplicate AudioSources on scene load
- ✅ Music continues playing across scenes
- ✅ Singleton pattern prevents duplicates

### Graceful Degradation
- ✅ All calls check `if (AudioManager.Instance != null)`
- ✅ Audio components work independently
- ✅ Missing AudioClips are handled safely
- ✅ System continues working if audio is disabled

---

## 🎮 Recommended Audio Clips

Based on existing assets in `Assets/Audio/`:

| Component | AudioClip | Purpose |
|-----------|-----------|---------|
| AudioManager | `MainMenu2.mp3` | Background music |
| PlayerAudio | `jump2.wav` | Jump sound |
| PlayerAudio | `Dash.mp3` | Dash sound |
| PlayerAudio | `WalkingSounds.mp3` | Footsteps (split into array if needed) |
| CollectibleAudio | - | Ambient loop (add custom sparkle) |
| CollectibleAudio | `HealingAcquired.mp3` | Pickup sound |
| HazardAudio (Hailstorm) | `Wind_-_Sound_Effect.mp3` | Loop while active |
| HazardAudio (Thunder) | `LightningSound2.wav` | Stinger sound |
| HazardAudio (Tornado) | `tornado.mp3` | Loop while active |

---

## 🧪 Testing Checklist

### AudioManager
- [ ] AudioManager persists across scene transitions
- [ ] No duplicate AudioManager instances
- [ ] Background music plays on start
- [ ] Background music continues across scenes

### Player Audio
- [ ] Jump sound plays on jump
- [ ] Dash sound plays on dash
- [ ] Footsteps play when moving on ground
- [ ] Footsteps stop when airborne
- [ ] Footsteps stop when standing still

### Collectible Audio
- [ ] Ambient loop plays while collectible exists
- [ ] Ambient loop stops on pickup
- [ ] Pickup sound plays even if object is destroyed
- [ ] Pickup sound routes through SFX mixer group

### Hazard Audio
- [ ] Loop plays when hazard activates
- [ ] Loop stops when hazard deactivates
- [ ] Start/stop stingers play correctly
- [ ] Multiple calls to Start/Stop are safe (idempotent)

---

## 🐛 Troubleshooting

### No Audio Playing
- Check AudioManager exists in scene/bootstrapper
- Verify AudioMixer is assigned
- Check AudioClips are assigned
- Verify volumes are > 0

### Duplicate Audio on Scene Load
- Ensure only ONE AudioManager exists
- AudioManager should be in Bootstrapper or marked DontDestroyOnLoad
- Check for duplicate AudioManager prefabs in scenes

### Pickup Sound Cuts Off
- CollectibleAudio uses `AudioManager.PlayAtPosition` by design
- This creates a temporary AudioSource that survives object destruction
- Verify AudioManager.Instance is not null

### Footsteps Spam/Glitch
- PlayerAudio uses step-based timing (NOT OnControllerColliderHit)
- Adjust `stepInterval` to control step frequency
- Check `minSpeedForFootsteps` threshold
- Verify `enableFootsteps` is checked

---

## 🔄 Future Enhancements (Optional)

If you want to extend the system later:

1. **Volume Settings UI**
   - Create sliders that call `AudioManager.SetMixerVolume()`
   - Save preferences to PlayerPrefs

2. **Dynamic Music Transitions**
   - Add crossfade support to AudioManager
   - Implement music layers for combat/exploration

3. **Randomized SFX**
   - Add pitch variation to one-shots
   - Implement random AudioClip selection

4. **Audio Zones**
   - Trigger ambient sounds on zone entry
   - Fade music based on location

---

## 📄 Summary

This audio system is designed to be:
- **Safe**: No breaking changes, graceful degradation
- **Persistent**: Survives scene loads via DontDestroyOnLoad
- **Modular**: Each component works independently
- **Inspector-Friendly**: All settings exposed in Unity Editor
- **Well-Commented**: Clear documentation in code

All scripts follow Unity best practices and metroidvania audio requirements.
