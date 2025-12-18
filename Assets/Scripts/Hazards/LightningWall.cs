using UnityEngine;

/// <summary>
/// Lightning obstacle that cycles on/off, damaging the player when active.
/// Uses procedural lightning bolts between two endpoints.
/// </summary>
public class LightningWall : MonoBehaviour
{
    [Header("Endpoints")]
    [Tooltip("Distance between the two endpoints")]
    [SerializeField] private float wallDistance = 5f;
    
    [Tooltip("Rotation angle in degrees (Y-axis)")]
    [SerializeField] private float rotationAngle = 0f;
    
    [Tooltip("Show endpoint preview box in editor")]
    [SerializeField] private bool showEndpoints = false;
    
    [Header("Lightning Configuration")]
    [Tooltip("Height of the lightning damage area (automatically spawns bolts to fill this height)")]
    [SerializeField] private float lightningHeight = 2f;
    
    [Tooltip("Spacing between each lightning bolt layer")]
    [SerializeField] private float boltSpacing = 0.5f;
    
    [Tooltip("Number of segments in each lightning bolt (more = more detailed)")]
    [SerializeField, Range(5, 30)] private int lightningSegments = 15;
    
    [Tooltip("Maximum random offset for lightning zigzag")]
    [SerializeField] private float lightningJaggedness = 0.5f;
    
    [Tooltip("Width of the lightning bolt")]
    [SerializeField] private float lightningWidth = 0.15f;
    
    [Tooltip("Number of branch bolts spawned from each main bolt")]
    [SerializeField, Range(0, 5)] private int branchCount = 2;
    
    [Tooltip("Minimum branch length")]
    [SerializeField] private float minBranchLength = 0.3f;
    
    [Tooltip("Maximum branch length")]
    [SerializeField] private float maxBranchLength = 1.5f;
    
    [Tooltip("Lightning color")]
    [SerializeField] private Color lightningColor = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Header("Timing")]
    [Tooltip("How long the lightning stays ON (seconds). Set to 0 for always on.")]
    [SerializeField] private float onDuration = 2f;
    
    [Tooltip("How long the lightning stays OFF (seconds). Set to 0 for always on.")]
    [SerializeField] private float offDuration = 2f;
    
    [Tooltip("If true, lightning starts in the ON state")]
    [SerializeField] private bool startActive = true;
    
    [Header("Damage")]
    [Tooltip("Damage dealt per hit")]
    [SerializeField] private int damageAmount = 1;
    
    [Tooltip("Force to push player back when touching lightning")]
    [SerializeField] private float pushbackForce = 50f;
    
    [Header("Visual Feedback")]
    [Tooltip("Particle system for lightning effect")]
    [SerializeField] private ParticleSystem lightningParticles;
    
    [Tooltip("Editor guide showing lightning area (only visible in editor)")]
    [SerializeField] private Renderer editorGuide;
    
    [Tooltip("Collider that damages player when lightning is active")]
    [SerializeField] private Collider damageCollider;
    
    [Tooltip("Solid collider that blocks player movement when lightning is active")]
    [SerializeField] private Collider blockingCollider;

    [Header("Audio")]
    [Tooltip("Looping sound played while lightning is active.")]
    [SerializeField] private AudioClip lightningLoopSound;
    
    [Tooltip("Sound played when lightning turns on.")]
    [SerializeField] private AudioClip activationSound;
    
    [Tooltip("Sound played when lightning turns off.")]
    [SerializeField] private AudioClip deactivationSound;
    
    [Tooltip("Volume for lightning loop (0-1).")]
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.7f;
    
    [Tooltip("Volume for activation/deactivation sounds (0-1).")]
    [SerializeField, Range(0f, 1f)] private float stingerVolume = 0.8f;
    
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    
    [Tooltip("Max distance for 3D audio.")]
    [SerializeField] private float audioMaxDistance = 30f;
    
    // Runtime data
    private LineRenderer[] _lightningBolts;
    private LineRenderer[] _lightningCores;
    private LineRenderer[][] _branchRenderers;
    private bool _isActive;
    private float _timer;
    private bool _alwaysOn;
    private float _regenerateTimer;
    private const float RegenerateInterval = 0.05f; // Regenerate bolts every 50ms for animation
    private int _calculatedBoltCount; // Auto-calculated based on height
    
    // Endpoint transforms for rotation
    private Transform _leftEndpoint;
    private Transform _rightEndpoint;
    
    // Audio
    private AudioSource _loopSource;

    private void Start()
    {
        // Setup audio loop
        if (lightningLoopSound != null)
        {
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.clip = lightningLoopSound;
            _loopSource.loop = true;
            _loopSource.playOnAwake = false;
            _loopSource.volume = loopVolume;
            _loopSource.spatialBlend = spatialBlend;
            _loopSource.maxDistance = audioMaxDistance;
        }
        
        // Create endpoint transforms if they don't exist
        CreateEndpoints();
        
        // Check if always on mode
        _alwaysOn = (onDuration <= 0f && offDuration <= 0f) || offDuration <= 0f;
        
        _isActive = startActive || _alwaysOn;
        _timer = _isActive ? onDuration : offDuration;
        
        // Don't auto-find colliders - they must be assigned in prefab
        if (damageCollider == null)
            Debug.LogError("LightningWall: damageCollider not assigned!");
        
        if (blockingCollider == null)
            Debug.LogError("LightningWall: blockingCollider not assigned!");
        
        if (lightningParticles == null)
            lightningParticles = GetComponentInChildren<ParticleSystem>();
        
        Debug.Log($"LightningWall Start: damageCollider={damageCollider?.gameObject.name}, isTrigger={(damageCollider as BoxCollider)?.isTrigger}, blockingCollider={blockingCollider?.gameObject.name}, blockingIsTrigger={(blockingCollider as BoxCollider)?.isTrigger}");
        
        // Update colliders to fit lightning area (after finding them)
        UpdateColliders();
        
        // Create lightning bolt renderers
        CreateLightningRenderers();
        
        // Hide editor guide during play
        if (editorGuide != null)
            editorGuide.enabled = false;
        
        // Set initial state
        UpdateVisuals();
    }

    private void CreateEndpoints()
    {
        // Find or create left endpoint
        Transform existingLeft = transform.Find("LeftEndpoint");
        if (existingLeft == null)
        {
            GameObject leftObj = new GameObject("LeftEndpoint");
            _leftEndpoint = leftObj.transform;
            _leftEndpoint.SetParent(transform);
        }
        else
        {
            _leftEndpoint = existingLeft;
        }
        
        // Find or create right endpoint
        Transform existingRight = transform.Find("RightEndpoint");
        if (existingRight == null)
        {
            GameObject rightObj = new GameObject("RightEndpoint");
            _rightEndpoint = rightObj.transform;
            _rightEndpoint.SetParent(transform);
        }
        else
        {
            _rightEndpoint = existingRight;
        }
        
        // Update positions
        UpdateEndpoints();
    }
    
    private void UpdateEndpoints()
    {
        if (_leftEndpoint == null || _rightEndpoint == null)
            return;
        
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
        
        // Calculate local positions and apply rotation
        Vector3 leftLocal = Vector3.left * (wallDistance / 2f);
        Vector3 rightLocal = Vector3.right * (wallDistance / 2f);
        
        Vector3 rotatedLeft = rotation * leftLocal;
        Vector3 rotatedRight = rotation * rightLocal;
        
        // Set endpoint positions
        _leftEndpoint.localPosition = rotatedLeft;
        _rightEndpoint.localPosition = rotatedRight;
    }
    
    private void UpdateColliders()
    {
        // Use the specified lightning height for colliders
        float totalHeight = Mathf.Max(2f, lightningHeight);
        
        // Calculate depth based on wall distance (proportional scaling)
        float colliderDepth = Mathf.Max(0.5f, wallDistance * 0.2f);
        
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
        
        // Update damage collider - trigger zone that detects player contact
        if (damageCollider != null)
        {
            BoxCollider boxCollider = damageCollider as BoxCollider;
            if (boxCollider != null)
            {
                // Reset transform scale to (1,1,1) so collider.size is accurate
                damageCollider.transform.localScale = Vector3.one;
                
                // Size scales with both wallDistance (X) and lightningHeight (Y), depth is proportional
                boxCollider.size = new Vector3(wallDistance, totalHeight, colliderDepth);
                boxCollider.center = Vector3.zero;
                boxCollider.isTrigger = true; // Ensure it's a trigger
                
                // Apply rotation
                damageCollider.transform.localRotation = rotation;
            }
        }
        
        // Update blocking collider - slightly thinner than damage trigger
        if (blockingCollider != null)
        {
            BoxCollider boxCollider = blockingCollider as BoxCollider;
            if (boxCollider != null)
            {
                // Reset transform scale to (1,1,1) so collider.size is accurate
                blockingCollider.transform.localScale = Vector3.one;
                
                // Slightly thinner than damage collider
                boxCollider.size = new Vector3(wallDistance, totalHeight, colliderDepth * 0.5f);
                boxCollider.center = Vector3.zero;
                boxCollider.isTrigger = false; // Ensure it's solid
                
                // Apply rotation
                blockingCollider.transform.localRotation = rotation;
            }
        }
        
        // Update editor guide to match lightning dimensions
        if (editorGuide != null && editorGuide.transform != null)
        {
            editorGuide.transform.localScale = new Vector3(1, 1, 0.1f);
            editorGuide.transform.localPosition = Vector3.zero;
            editorGuide.transform.localRotation = rotation;
            
            // Only show in editor, not during play
            if (!Application.isPlaying)
                editorGuide.enabled = true;
        }
    }
    
    private void CreateLightningRenderers()
    {
        // Calculate how many bolts needed to fill the height
        _calculatedBoltCount = Mathf.Max(1, Mathf.CeilToInt(lightningHeight / boltSpacing));
        
        _lightningBolts = new LineRenderer[_calculatedBoltCount];
        _lightningCores = new LineRenderer[_calculatedBoltCount];
        _branchRenderers = new LineRenderer[_calculatedBoltCount][];
        
        for (int i = 0; i < _calculatedBoltCount; i++)
        {
            // Create main bolt
            GameObject boltObj = new GameObject($"LightningBolt_{i}");
            boltObj.transform.SetParent(transform);
            LineRenderer bolt = boltObj.AddComponent<LineRenderer>();
            
            Material lightningMat = new Material(Shader.Find("Unlit/Color"));
            lightningMat.color = lightningColor;
            
            bolt.material = lightningMat;
            bolt.startWidth = lightningWidth;
            bolt.endWidth = lightningWidth * 0.3f;
            bolt.positionCount = lightningSegments;
            bolt.useWorldSpace = true;
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bolt.receiveShadows = false;
            bolt.enabled = false;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(lightningColor, 0.3f),
                    new GradientColorKey(lightningColor * 0.7f, 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0.8f, 1f)
                }
            );
            bolt.colorGradient = gradient;
            
            _lightningBolts[i] = bolt;
            
            // Create core (bright inner bolt)
            GameObject coreObj = new GameObject($"LightningCore_{i}");
            coreObj.transform.SetParent(transform);
            LineRenderer core = coreObj.AddComponent<LineRenderer>();
            
            Material coreMat = new Material(Shader.Find("Unlit/Color"));
            coreMat.color = Color.white;
            
            core.material = coreMat;
            core.startWidth = lightningWidth * 0.3f;
            core.endWidth = lightningWidth * 0.1f;
            core.positionCount = lightningSegments;
            core.useWorldSpace = true;
            core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            core.receiveShadows = false;
            core.enabled = false;
            
            Gradient coreGradient = new Gradient();
            coreGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(lightningColor, 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            core.colorGradient = coreGradient;
            
            _lightningCores[i] = core;
            
            // Create branches
            if (branchCount > 0)
            {
                _branchRenderers[i] = new LineRenderer[branchCount];
                
                for (int b = 0; b < branchCount; b++)
                {
                    GameObject branchObj = new GameObject($"LightningBranch_{i}_{b}");
                    branchObj.transform.SetParent(transform);
                    LineRenderer branch = branchObj.AddComponent<LineRenderer>();
                    
                    Material branchMat = new Material(Shader.Find("Unlit/Color"));
                    branchMat.color = lightningColor * 0.7f;
                    
                    branch.material = branchMat;
                    branch.startWidth = lightningWidth * 0.4f;
                    branch.endWidth = lightningWidth * 0.05f;
                    branch.useWorldSpace = true;
                    branch.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    branch.receiveShadows = false;
                    branch.enabled = false;
                    
                    Gradient branchGradient = new Gradient();
                    branchGradient.SetKeys(
                        new GradientColorKey[] { 
                            new GradientColorKey(lightningColor * 0.9f, 0f),
                            new GradientColorKey(lightningColor * 0.6f, 0.5f),
                            new GradientColorKey(lightningColor * 0.3f, 1f)
                        },
                        new GradientAlphaKey[] { 
                            new GradientAlphaKey(0.9f, 0f),
                            new GradientAlphaKey(0.6f, 0.5f),
                            new GradientAlphaKey(0.2f, 1f)
                        }
                    );
                    branch.colorGradient = branchGradient;
                    
                    _branchRenderers[i][b] = branch;
                }
            }
        }
    }

    private void Update()
    {
        // Always on mode - no cycling
        if (_alwaysOn)
        {
            _isActive = true;
        }
        else
        {
            // Cycle timing
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                // Switch state
                _isActive = !_isActive;
                
                // Reset timer
                _timer = _isActive ? onDuration : offDuration;
            }
        }
        
        // Regenerate lightning bolts periodically for animation
        if (_isActive)
        {
            _regenerateTimer -= Time.deltaTime;
            if (_regenerateTimer <= 0f)
            {
                _regenerateTimer = RegenerateInterval;
                GenerateAllLightningBolts();
            }
        }
        
        UpdateVisuals();
    }
    
    private void GenerateAllLightningBolts()
    {
        if (_leftEndpoint == null || _rightEndpoint == null)
            return;
        
        // Get world positions from endpoint transforms
        Vector3 startPos = _leftEndpoint.position;
        Vector3 endPos = _rightEndpoint.position;
        
        // Generate each bolt with Y offset to fill the height
        for (int i = 0; i < _calculatedBoltCount; i++)
        {
            // Center the bolts vertically around transform position
            float yOffset = (i - (_calculatedBoltCount - 1) / 2f) * boltSpacing;
            Vector3 offsetStart = startPos + Vector3.up * yOffset;
            Vector3 offsetEnd = endPos + Vector3.up * yOffset;
            
            GenerateLightningBolt(_lightningBolts[i], _lightningCores[i], offsetStart, offsetEnd, i);
        }
    }
    
    private void GenerateLightningBolt(LineRenderer bolt, LineRenderer core, Vector3 start, Vector3 end, int boltIndex)
    {
        if (bolt == null) return;
        
        Vector3[] points = new Vector3[lightningSegments];
        points[0] = start;
        points[lightningSegments - 1] = end;
        
        Vector3 direction = end - start;
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular1.magnitude < 0.1f)
            perpendicular1 = Vector3.Cross(direction, Vector3.forward).normalized;
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
        
        // Create jagged lightning path
        for (int i = 1; i < lightningSegments - 1; i++)
        {
            float t = (float)i / (lightningSegments - 1);
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            // Add random offset perpendicular to the main direction
            float maxOffset = lightningJaggedness * (1f - Mathf.Abs(t - 0.5f) * 2f);
            Vector3 offset = perpendicular1 * Random.Range(-maxOffset, maxOffset) +
                           perpendicular2 * Random.Range(-maxOffset, maxOffset);
            
            points[i] = basePoint + offset;
        }
        
        bolt.positionCount = lightningSegments;
        bolt.SetPositions(points);
        
        // Set same path for core
        if (core != null)
        {
            core.positionCount = lightningSegments;
            core.SetPositions(points);
        }
        
        // Generate branches
        if (_branchRenderers != null && boltIndex < _branchRenderers.Length && _branchRenderers[boltIndex] != null)
        {
            foreach (var branch in _branchRenderers[boltIndex])
            {
                if (branch != null)
                    GenerateLightningBranch(branch, points);
            }
        }
    }
    
    private void GenerateLightningBranch(LineRenderer branchRenderer, Vector3[] mainPoints)
    {
        int minIndex = Mathf.Max(2, mainPoints.Length / 4);
        int maxIndex = Mathf.Min(mainPoints.Length - 3, mainPoints.Length * 3 / 4);
        int startIndex = Random.Range(minIndex, maxIndex);
        Vector3 branchStart = mainPoints[startIndex];
        
        Vector3 mainDirection = (mainPoints[mainPoints.Length - 1] - mainPoints[0]).normalized;
        Vector3 randomPerp = Random.onUnitSphere;
        randomPerp = Vector3.ProjectOnPlane(randomPerp, mainDirection).normalized;
        
        Vector3 branchDirection = randomPerp;
        float branchLength = Random.Range(minBranchLength, maxBranchLength);
        Vector3 branchEnd = branchStart + branchDirection * branchLength;
        
        int branchSegments = Random.Range(4, 10);
        Vector3[] branchPoints = new Vector3[branchSegments];
        branchPoints[0] = branchStart;
        branchPoints[branchSegments - 1] = branchEnd;
        
        Vector3 branchDir = branchEnd - branchStart;
        Vector3 branchPerp1 = Vector3.Cross(branchDir, Vector3.up).normalized;
        if (branchPerp1.magnitude < 0.1f)
            branchPerp1 = Vector3.Cross(branchDir, Vector3.forward).normalized;
        Vector3 branchPerp2 = Vector3.Cross(branchDir, branchPerp1).normalized;
        
        for (int i = 1; i < branchSegments - 1; i++)
        {
            float t = (float)i / (branchSegments - 1);
            Vector3 basePoint = Vector3.Lerp(branchStart, branchEnd, t);
            
            float maxOffset = lightningJaggedness * 0.3f;
            Vector3 offset = branchPerp1 * Random.Range(-maxOffset, maxOffset) +
                           branchPerp2 * Random.Range(-maxOffset, maxOffset);
            
            branchPoints[i] = basePoint + offset;
        }
        
        branchRenderer.positionCount = branchSegments;
        branchRenderer.SetPositions(branchPoints);
    }

    private void UpdateVisuals()
    {
        bool showLightning = _isActive;
        bool wasActive = _lightningBolts != null && _lightningBolts.Length > 0 && _lightningBolts[0] != null && _lightningBolts[0].enabled;
        
        // Handle audio state changes
        if (showLightning && !wasActive)
        {
            // Lightning turning ON
            if (_loopSource != null && !_loopSource.isPlaying)
            {
                _loopSource.Play();
            }
            
            // Play activation stinger
            if (activationSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAtPosition(activationSound, transform.position, stingerVolume, spatialBlend);
            }
        }
        else if (!showLightning && wasActive)
        {
            // Lightning turning OFF
            if (_loopSource != null && _loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
            
            // Play deactivation stinger
            if (deactivationSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAtPosition(deactivationSound, transform.position, stingerVolume, spatialBlend);
            }
        }
        
        // Lightning bolts with flicker
        if (_lightningBolts != null)
        {
            foreach (var bolt in _lightningBolts)
            {
                if (bolt != null)
                {
                    bolt.enabled = showLightning;
                    if (showLightning)
                    {
                        float flicker = Random.Range(0.7f, 1f);
                        bolt.widthMultiplier = flicker;
                    }
                }
            }
        }
        
        // Cores
        if (_lightningCores != null)
        {
            foreach (var core in _lightningCores)
            {
                if (core != null)
                {
                    core.enabled = showLightning;
                    if (showLightning)
                    {
                        float flicker = Random.Range(0.8f, 1f);
                        core.widthMultiplier = flicker;
                    }
                }
            }
        }
        
        // Branches
        if (_branchRenderers != null)
        {
            foreach (var branches in _branchRenderers)
            {
                if (branches != null)
                {
                    foreach (var branch in branches)
                    {
                        if (branch != null)
                        {
                            branch.enabled = showLightning;
                            if (showLightning)
                            {
                                float flicker = Random.Range(0.6f, 1f);
                                branch.widthMultiplier = flicker;
                            }
                        }
                    }
                }
            }
        }
        
        // Particles
        if (lightningParticles != null)
        {
            if (showLightning && !lightningParticles.isPlaying)
                lightningParticles.Play();
            else if (!showLightning && lightningParticles.isPlaying)
                lightningParticles.Stop();
        }
        
        // Colliders - only enable damage trigger
        if (damageCollider != null)
            damageCollider.enabled = _isActive;
        
        // Keep blocking collider disabled - it prevents damage trigger from working
        // Instead we use strong pushback force from the damage trigger
        if (blockingCollider != null)
            blockingCollider.enabled = false;
    }

    /// <summary>
    /// Public properties for LightningBeamDamage script
    /// </summary>
    public bool IsActive => _isActive;
    public int DamageAmount => damageAmount;
    public float PushbackForce => pushbackForce;

    public Vector3 GetClosestWallPosition(Vector3 playerPos)
    {
        if (_leftEndpoint == null || _rightEndpoint == null)
        {
            // Fallback to calculated positions
            Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
            Vector3 leftPos = transform.position + rotation * (Vector3.left * (wallDistance / 2f));
            Vector3 rightPos = transform.position + rotation * (Vector3.right * (wallDistance / 2f));
            
            float distLeft = Vector3.Distance(playerPos, leftPos);
            float distRight = Vector3.Distance(playerPos, rightPos);
            
            return distLeft < distRight ? leftPos : rightPos;
        }
        
        // Return closest endpoint position using transforms
        Vector3 leftPosition = _leftEndpoint.position;
        Vector3 rightPosition = _rightEndpoint.position;
        
        float distLeft2 = Vector3.Distance(playerPos, leftPosition);
        float distRight2 = Vector3.Distance(playerPos, rightPosition);
        
        return distLeft2 < distRight2 ? leftPosition : rightPosition;
    }
    
    /// <summary>
    /// Get the wall's perpendicular direction (the normal facing forward/backward)
    /// </summary>
    public Vector3 GetWallNormal()
    {
        // The wall spans left-right (X-axis), so the normal is the rotated forward direction (Z-axis)
        Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
        return rotation * Vector3.forward;
    }

    private void OnDrawGizmos()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);
        
        // Calculate endpoint positions with rotation
        Vector3 leftLocal = Vector3.left * (wallDistance / 2f);
        Vector3 rightLocal = Vector3.right * (wallDistance / 2f);
        
        Vector3 leftPos = transform.position + rotation * leftLocal;
        Vector3 rightPos = transform.position + rotation * rightLocal;
        
        // Draw line between endpoints
        Gizmos.color = Application.isPlaying && _isActive ? Color.cyan : Color.gray;
        Gizmos.DrawLine(leftPos, rightPos);
        
        // Draw endpoint indicators
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftPos, 0.2f);
        Gizmos.DrawWireSphere(rightPos, 0.2f);
        
        // Draw rotated height area using matrix transformation
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(wallDistance, lightningHeight, 0.5f));
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    private void OnValidate()
    {
        // Ensure minimum values
        lightningHeight = Mathf.Max(0.5f, lightningHeight);
        boltSpacing = Mathf.Max(0.1f, boltSpacing);
        wallDistance = Mathf.Max(1f, wallDistance);
        
        // Update endpoints when rotation or distance changes
        if (_leftEndpoint != null && _rightEndpoint != null)
        {
            UpdateEndpoints();
        }
        
        // Update colliders when values change
        UpdateColliders();
    }
    
    /// <summary>
    /// Force the lightning to a specific state (useful for scripted events)
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Reset the cycle timer
    /// </summary>
    public void ResetTimer()
    {
        _timer = _isActive ? onDuration : offDuration;
    }
}
