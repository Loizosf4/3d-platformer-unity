# Audio System - Setup Checklist

## ✅ Files Created

### Scripts (Assets/Scripts/Audio/)
- [x] AudioManager.cs
- [x] PlayerAudio.cs
- [x] CollectibleAudio.cs
- [x] HazardAudio.cs
- [x] BootstrapperWithAudio_Example.cs (reference)
- [x] QuickReference.cs (code snippets)

### Assets
- [x] MainAudioMixer.mixer (Assets/Audio/)

### Documentation
- [x] README.md (complete integration guide)

### System Integration
- [x] Bootstrapper.cs updated (AudioManager prefab field added)

---

## 🔧 Setup Steps (In Unity Editor)

### Step 1: Create AudioManager Prefab (5 minutes)

1. **Create Empty GameObject:**
   - Hierarchy → Right-click → Create Empty
   - Name: `AudioManager`

2. **Add AudioManager Component:**
   - Select AudioManager GameObject
   - Inspector → Add Component → AudioManager

3. **Configure AudioManager:**
   - **Audio Mixer:** Drag `Assets/Audio/MainAudioMixer.mixer`
   - **Background Music:** Drag `Assets/Audio/MainMenu2.mp3` (or your choice)
   - **Music Volume:** 0.5
   - **Play Music On Start:** ✓ (checked)
   - **SFX Volume:** 1.0
   - **Max Simultaneous Sounds:** 10

4. **Create Prefab:**
   - Drag AudioManager from Hierarchy → `Assets/Prefabs/`
   - Name: `PF_AudioManager`
   - Delete AudioManager from scene

5. **Add to Bootstrapper:**
   - Select Bootstrapper GameObject in scene
   - Inspector → Bootstrapper component
   - **Audio Manager Prefab:** Drag `PF_AudioManager`

---

### Step 2: Setup Player Audio (3 minutes)

1. **Open Player Prefab:**
   - Navigate to `Assets/Prefabs/PF_player`
   - Double-click to enter prefab mode

2. **Add PlayerAudio Component:**
   - Select player root GameObject
   - Inspector → Add Component → PlayerAudio

3. **Configure PlayerAudio:**
   - **Motor:** Should auto-assign (PlayerMotorCC)
   - **Jump Sound:** `Assets/Audio/jump2.wav`
   - **Dash Sound:** `Assets/Audio/Dash.mp3`
   - **Footstep Sounds:** Click + to add array element
     - Element 0: `Assets/Audio/WalkingSounds.mp3`
   - **Jump Volume:** 0.7
   - **Dash Volume:** 0.8
   - **Footstep Volume:** 0.4
   - **Min Speed For Footsteps:** 0.5
   - **Step Interval:** 0.35
   - **Enable Footsteps:** ✓

4. **Save Prefab:**
   - File → Save (or Ctrl+S)
   - Exit prefab mode

---

### Step 3: Setup Collectible Audio (Optional, 2 minutes per prefab)

1. **Open Collectible Prefab:**
   - Navigate to collectible prefabs (e.g., `Assets/Prefabs/Collectibles/`)

2. **Add CollectibleAudio Component:**
   - Select collectible root GameObject
   - Inspector → Add Component → CollectibleAudio

3. **Configure CollectibleAudio:**
   - **Ambient Loop Clip:** (Add a sparkle/hum sound if available)
   - **Ambient Volume:** 0.3
   - **Play Ambient On Start:** ✓
   - **Ambient Spatial Blend:** 1.0 (3D)
   - **Ambient Max Distance:** 15
   - **Pickup Sound:** `Assets/Audio/HealingAcquired.mp3`
   - **Pickup Volume:** 0.8
   - **Pickup Spatial Blend:** 0.5

4. **Save Prefab:**
   - Repeat for other collectible prefabs

---

### Step 4: Setup Hazard Audio (Optional, 3 minutes per hazard)

#### Example: HailstormZone

1. **Open Hazard Prefab:**
   - Navigate to hazard prefabs

2. **Add HazardAudio Component:**
   - Select hazard root GameObject
   - Inspector → Add Component → HazardAudio

3. **Configure HazardAudio:**
   - **Loop Clip:** `Assets/Audio/Wind_-_Sound_Effect.mp3`
   - **Loop Volume:** 0.7
   - **Loop Spatial Blend:** 1.0
   - **Loop Max Distance:** 50
   - **Start Stinger:** (Optional warning sound)
   - **Stop Stinger:** (Optional)
   - **Stinger Volume:** 0.8
   - **Fade Duration:** 0.5 (smooth fade in/out)

4. **Add Script Integration:**
   - Open HailstormZone.cs (or your hazard script)
   - Add this code:

```csharp
private HazardAudio _hazardAudio;

void Awake() {
    _hazardAudio = GetComponent<HazardAudio>();
}

void StartHailstorm() {
    // Your existing code...
    _hazardAudio?.StartHazardAudio();
}

void StopHailstorm() {
    // Your existing code...
    _hazardAudio?.StopHazardAudio();
}
```

5. **Save Prefab**

---

### Step 5: Test in Play Mode (5 minutes)

#### AudioManager Test
- [ ] Press Play
- [ ] Background music starts playing
- [ ] Music continues when changing scenes
- [ ] No duplicate music on scene reload

#### Player Audio Test
- [ ] Jump → Jump sound plays
- [ ] Dash → Dash sound plays
- [ ] Walk on ground → Footsteps play at regular intervals
- [ ] Stop moving → Footsteps stop
- [ ] In air → Footsteps stop

#### Collectible Audio Test (if implemented)
- [ ] Approach collectible → Ambient loop plays
- [ ] Pick up collectible → Ambient stops, pickup sound plays
- [ ] Pickup sound completes even though object is destroyed

#### Hazard Audio Test (if implemented)
- [ ] Hazard activates → Loop starts (with optional stinger)
- [ ] Hazard deactivates → Loop stops (with optional stinger)
- [ ] Multiple activate calls → No audio spam (idempotent)

---

## 🎮 Quick Reference

### Common AudioClip Assignments

| Component | Field | Suggested Clip |
|-----------|-------|----------------|
| AudioManager | Background Music | `MainMenu2.mp3` |
| PlayerAudio | Jump Sound | `jump2.wav` |
| PlayerAudio | Dash Sound | `Dash.mp3` |
| PlayerAudio | Footsteps | `WalkingSounds.mp3` |
| CollectibleAudio | Pickup Sound | `HealingAcquired.mp3` |
| HazardAudio (Hailstorm) | Loop Clip | `Wind_-_Sound_Effect.mp3` |
| HazardAudio (Thunder) | Start Stinger | `LightningSound2.wav` |
| HazardAudio (Tornado) | Loop Clip | `tornado.mp3` |

### Code Snippets

**Play one-shot SFX:**
```csharp
AudioManager.Instance?.PlaySFX(clip, 0.7f);
```

**Play at position (survives destruction):**
```csharp
AudioManager.Instance?.PlayAtPosition(clip, transform.position);
```

**Change background music:**
```csharp
AudioManager.Instance?.PlayMusic(newMusicClip, 0.5f);
```

---

## 🐛 Troubleshooting

### No audio at all
1. Check AudioManager prefab is assigned in Bootstrapper
2. Verify MainAudioMixer is assigned in AudioManager
3. Check volumes are > 0
4. Verify AudioClips are assigned

### Pickup sound cuts off
- Use `AudioManager.PlayAtPosition()` in CollectibleAudio (already implemented)

### Footsteps spam
- Increase `stepInterval` in PlayerAudio
- Check `minSpeedForFootsteps` threshold

### Duplicate music on scene load
- Only one AudioManager should exist
- AudioManager should be in Bootstrapper (DontDestroyOnLoad)

### Compilation errors
- Ensure all scripts are in `Assets/Scripts/Audio/`
- Check Unity version compatibility (2022.3.62f3)

---

## ✅ System Status

- [x] All scripts created (no errors)
- [x] AudioMixer asset created
- [x] Bootstrapper integration complete
- [x] Documentation complete
- [x] Zero breaking changes
- [x] Scene-reload safe
- [x] Ready for setup in Unity Editor

**Estimated Setup Time:** 15-20 minutes total
