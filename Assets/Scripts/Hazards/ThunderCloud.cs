using UnityEngine;

/// <summary>
/// Dark cloud that periodically shoots lightning bolts downward.
/// Damages player if hit by the lightning strike.
/// </summary>
public class ThunderCloud : MonoBehaviour
{
    [Header("Strike Timing")]
    [Tooltip("Time between lightning strikes (seconds)")]
    [SerializeField] private float strikeInterval = 3f;
    
    [Tooltip("Warning duration before strike (seconds)")]
    [SerializeField] private float warningDuration = 0.5f;
    
    [Tooltip("How long the lightning bolt is visible (seconds)")]
    [SerializeField] private float strikeDuration = 0.2f;
    
    [Tooltip("If true, starts with a strike. If false, waits one interval first.")]
    [SerializeField] private bool strikeOnStart = false;
    
    [Header("Lightning Properties")]
    [Tooltip("Maximum distance the lightning travels downward")]
    [SerializeField] private float strikeRange = 20f;
    
    [Tooltip("Width of the lightning detection (radius)")]
    [SerializeField] private float strikeRadius = 0.5f;
    
    [Tooltip("Height of the strike zone cylinder from ground (0 = only ground level)")]
    [SerializeField] private float strikeHeight = 3f;
    
    [Tooltip("Damage dealt to player hit by lightning")]
    [SerializeField, Range(1, 5)] private int damageAmount = 1;
    
    [Tooltip("Layers the lightning can hit")]
    [SerializeField] private LayerMask hitLayers = -1;
    
    [Header("Visual Feedback")]
    [Tooltip("LineRenderer for procedural lightning bolt")]
    [SerializeField] private LineRenderer lightningLineRenderer;
    
    [Tooltip("Secondary LineRenderer for inner lightning core (brighter, thinner)")]
    [SerializeField] private LineRenderer lightningCoreRenderer;
    
    [Tooltip("Legacy lightning beam renderer (will be replaced by LineRenderer)")]
    [SerializeField] private Renderer lightningBeam;
    
    [Tooltip("Number of segments in lightning bolt (more = more detailed)")]
    [SerializeField, Range(5, 30)] private int lightningSegments = 15;
    
    [Tooltip("Maximum random offset for lightning zigzag")]
    [SerializeField] private float lightningJaggedness = 0.5f;
    
    [Tooltip("Width of the lightning bolt")]
    [SerializeField] private float lightningWidth = 0.15f;
    
    [Tooltip("Number of branch bolts spawned from main bolt")]
    [SerializeField, Range(0, 8)] private int branchCount = 3;
    
    [Tooltip("Minimum branch length")]
    [SerializeField] private float minBranchLength = 0.5f;
    
    [Tooltip("Maximum branch length")]
    [SerializeField] private float maxBranchLength = 2.5f;
    
    [Tooltip("LineRenderers for lightning branches (auto-generated if null)")]
    [SerializeField] private LineRenderer[] branchRenderers;
    
    [Tooltip("Particle system for lightning effect")]
    [SerializeField] private ParticleSystem lightningParticles;
    
    [Tooltip("Particle system for cloud electrification effect")]
    [SerializeField] private ParticleSystem cloudElectricParticles;
    
    [Tooltip("Warning light/glow before strike")]
    [SerializeField] private Light warningLight;
    
    [Tooltip("Cloud renderer to darken during warning")]
    [SerializeField] private Renderer cloudRenderer;
    
    [Tooltip("Ground indicator showing strike area (appears during warning)")]
    [SerializeField] private Renderer groundIndicator;
    
    [Tooltip("Lightning color")]
    [SerializeField] private Color lightningColor = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Tooltip("Warning color")]
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0.5f, 1f);
    
    [Header("Audio")]
    [Tooltip("Sound played when lightning strikes")]
    [SerializeField] private AudioClip strikeSound;
    
    [Tooltip("Sound played during warning")]
    [SerializeField] private AudioClip warningSound;
    
    // State
    private enum State { Idle, Warning, Striking }
    private State _currentState = State.Idle;
    private float _stateTimer;
    private AudioSource _audioSource;
    private Material _cloudMaterial;
    private Color _originalCloudColor;
    private Vector3 _strikePoint;
    
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Get cloud material for color changes
        if (cloudRenderer != null && cloudRenderer.material != null)
        {
            _cloudMaterial = cloudRenderer.material;
            _originalCloudColor = _cloudMaterial.color;
        }
        
        // Auto-find or create LineRenderer for lightning
        if (lightningLineRenderer == null)
        {
            GameObject lineObj = new GameObject("LightningBolt");
            lineObj.transform.SetParent(transform);
            lightningLineRenderer = lineObj.AddComponent<LineRenderer>();
            
            // Configure LineRenderer for glowing lightning effect
            Material lightningMat = new Material(Shader.Find("Unlit/Color"));
            lightningMat.color = lightningColor;
            lightningMat.SetColor("_EmissionColor", lightningColor * 2f);
            
            lightningLineRenderer.material = lightningMat;
            lightningLineRenderer.startWidth = lightningWidth;
            lightningLineRenderer.endWidth = lightningWidth * 0.3f;
            lightningLineRenderer.positionCount = lightningSegments;
            lightningLineRenderer.useWorldSpace = true;
            lightningLineRenderer.textureMode = LineTextureMode.Stretch;
            
            // Add glow effect
            lightningLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lightningLineRenderer.receiveShadows = false;
            lightningLineRenderer.enabled = false;
            
            // Create gradient for lightning (bright at start, dimmer at end)
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
            lightningLineRenderer.colorGradient = gradient;
        }
        
        // Setup branch renderers if they exist
        if (branchRenderers != null)
        {
            foreach (var branch in branchRenderers)
            {
                if (branch != null)
                {
                    Material branchMat = new Material(Shader.Find("Unlit/Color"));
                    branchMat.color = lightningColor * 0.7f;
                    branch.material = branchMat;
                    branch.startWidth = lightningWidth * 0.5f;
                    branch.endWidth = lightningWidth * 0.1f;
                    branch.textureMode = LineTextureMode.Stretch;
                    branch.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    branch.receiveShadows = false;
                    branch.enabled = false;
                    
                    Gradient branchGradient = new Gradient();
                    branchGradient.SetKeys(
                        new GradientColorKey[] { 
                            new GradientColorKey(lightningColor, 0f),
                            new GradientColorKey(lightningColor * 0.5f, 1f)
                        },
                        new GradientAlphaKey[] { 
                            new GradientAlphaKey(0.8f, 0f),
                            new GradientAlphaKey(0.3f, 1f)
                        }
                    );
                    branch.colorGradient = branchGradient;
                }
            }
        }
        else
        {
            // Auto-create branch renderers if not assigned
            CreateBranchRenderers();
        }
        
        // Auto-create core renderer for inner glow if not assigned
        if (lightningCoreRenderer == null && lightningLineRenderer != null)
        {
            GameObject coreObj = new GameObject("LightningCore");
            coreObj.transform.SetParent(transform);
            lightningCoreRenderer = coreObj.AddComponent<LineRenderer>();
            
            Material coreMat = new Material(Shader.Find("Unlit/Color"));
            coreMat.color = Color.white;
            
            lightningCoreRenderer.material = coreMat;
            lightningCoreRenderer.startWidth = lightningWidth * 0.3f;
            lightningCoreRenderer.endWidth = lightningWidth * 0.1f;
            lightningCoreRenderer.positionCount = lightningSegments;
            lightningCoreRenderer.useWorldSpace = true;
            lightningCoreRenderer.textureMode = LineTextureMode.Stretch;
            lightningCoreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lightningCoreRenderer.receiveShadows = false;
            lightningCoreRenderer.enabled = false;
            
            // Bright white core
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
            lightningCoreRenderer.colorGradient = coreGradient;
        }
        
        // Auto-find components if not assigned
        if (lightningParticles == null)
            lightningParticles = GetComponentInChildren<ParticleSystem>();
        
        if (warningLight == null)
            warningLight = GetComponentInChildren<Light>();
        
        // Auto-create cloud electric particles if not assigned
        if (cloudElectricParticles == null && cloudRenderer != null)
        {
            GameObject particlesObj = new GameObject("CloudElectricParticles");
            particlesObj.transform.SetParent(transform);
            particlesObj.transform.localPosition = Vector3.zero;
            
            cloudElectricParticles = particlesObj.AddComponent<ParticleSystem>();
            
            var main = cloudElectricParticles.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 0.5f;
            main.startSize = 0.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.5f, 0.8f, 1f, 1f),  // Light blue
                new Color(0.2f, 0.5f, 1f, 1f)   // Darker blue
            );
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = cloudElectricParticles.emission;
            emission.rateOverTime = 20f; // Always emitting
            emission.enabled = true;
            
            var shape = cloudElectricParticles.shape;
            shape.shapeType = ParticleSystemShapeType.MeshRenderer;
            
            // Cast to MeshRenderer if possible
            MeshRenderer meshRenderer = cloudRenderer as MeshRenderer;
            if (meshRenderer != null)
            {
                shape.meshRenderer = meshRenderer;
                shape.meshShapeType = ParticleSystemMeshShapeType.Edge;
            }
            else
            {
                // Fallback to sphere shape if not a mesh renderer
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 2f;
            }
            
            var colorOverLifetime = cloudElectricParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f),      // Light blue at start
                    new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0.5f),    // Medium blue
                    new GradientColorKey(new Color(0.5f, 0.8f, 1f), 1f)       // Light blue at end
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;
            
            var sizeOverLifetime = cloudElectricParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(0.5f, 1.2f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
            
            var renderer = cloudElectricParticles.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", new Color(0.5f, 0.8f, 1f, 1f));
            renderer.material.SetColor("_EmissionColor", new Color(0.5f, 0.8f, 1f, 1f) * 2f);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Auto-start the particles
            cloudElectricParticles.Play();
        }
        
        // Auto-create ground indicator if not assigned
        if (groundIndicator == null)
        {
            GameObject indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicatorObj.name = "GroundIndicator";
            indicatorObj.transform.SetParent(transform);
            
            // Remove collider (we don't need it)
            Collider indicatorCollider = indicatorObj.GetComponent<Collider>();
            if (indicatorCollider != null)
                Destroy(indicatorCollider);
            
            groundIndicator = indicatorObj.GetComponent<Renderer>();
            
            // Create semi-transparent material
            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.SetFloat("_Mode", 3); // Transparent mode
            indicatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            indicatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            indicatorMat.SetInt("_ZWrite", 0);
            indicatorMat.DisableKeyword("_ALPHATEST_ON");
            indicatorMat.EnableKeyword("_ALPHABLEND_ON");
            indicatorMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            indicatorMat.renderQueue = 3000;
            indicatorMat.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0.5f);
            
            groundIndicator.material = indicatorMat;
            groundIndicator.enabled = false;
        }
        
        // Start in idle state
        _currentState = strikeOnStart ? State.Warning : State.Idle;
        _stateTimer = strikeOnStart ? warningDuration : strikeInterval;
        
        UpdateVisuals();
    }
    
    private void CreateBranchRenderers()
    {
        if (branchCount <= 0) return;
        
        branchRenderers = new LineRenderer[branchCount];
        
        for (int i = 0; i < branchCount; i++)
        {
            GameObject branchObj = new GameObject($"LightningBranch_{i}");
            branchObj.transform.SetParent(transform);
            
            LineRenderer branch = branchObj.AddComponent<LineRenderer>();
            Material branchMat = new Material(Shader.Find("Unlit/Color"));
            branchMat.color = lightningColor * 0.7f;
            
            branch.material = branchMat;
            branch.startWidth = lightningWidth * 0.4f;
            branch.endWidth = lightningWidth * 0.05f;
            branch.textureMode = LineTextureMode.Stretch;
            branch.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            branch.receiveShadows = false;
            branch.useWorldSpace = true;
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
            
            branchRenderers[i] = branch;
        }
    }

    private void Update()
    {
        _stateTimer -= Time.deltaTime;
        
        if (_stateTimer <= 0f)
        {
            switch (_currentState)
            {
                case State.Idle:
                    StartWarning();
                    break;
                    
                case State.Warning:
                    ExecuteStrike();
                    break;
                    
                case State.Striking:
                    EndStrike();
                    break;
            }
        }
        
        UpdateVisuals();
    }
    
    private void StartWarning()
    {
        _currentState = State.Warning;
        _stateTimer = warningDuration;
        
        // Calculate strike point for indicator
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, strikeRange, hitLayers))
        {
            _strikePoint = hit.point;
        }
        else
        {
            _strikePoint = origin + direction * strikeRange;
        }
        
        // Play warning sound
        if (_audioSource != null && warningSound != null)
            _audioSource.PlayOneShot(warningSound);
    }
    
    private void ExecuteStrike()
    {
        _currentState = State.Striking;
        _stateTimer = strikeDuration;
        
        // Cast ray downward to find strike point
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, strikeRange, hitLayers))
        {
            _strikePoint = hit.point;
        }
        else
        {
            _strikePoint = origin + direction * strikeRange;
        }
        
        // Generate procedural lightning bolt
        GenerateLightningBolt(origin, _strikePoint);
        
        // Check for player in strike path using SphereCast
        RaycastHit[] hits = Physics.SphereCastAll(origin, strikeRadius, direction, strikeRange, hitLayers);
        
        foreach (var h in hits)
        {
            var health = h.collider.GetComponentInParent<PlayerHealthController>();
            if (health != null)
            {
                // Check if player is within the cylinder height from ground
                float heightFromGround = health.transform.position.y - _strikePoint.y;
                if (heightFromGround >= 0f && heightFromGround <= strikeHeight)
                {
                    health.TryTakeDamage(origin, damageAmount);
                }
                break; // Only hit player once
            }
        }
        
        // Play strike sound
        if (_audioSource != null && strikeSound != null)
            _audioSource.PlayOneShot(strikeSound);
    }
    
    private void GenerateLightningBolt(Vector3 start, Vector3 end)
    {
        if (lightningLineRenderer == null) return;
        
        Vector3[] points = new Vector3[lightningSegments];
        points[0] = start;
        points[lightningSegments - 1] = end;
        
        Vector3 direction = end - start;
        float totalDistance = direction.magnitude;
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.right).normalized;
        if (perpendicular1.magnitude < 0.1f) // Handle parallel case
            perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
        
        // Create jagged lightning path
        for (int i = 1; i < lightningSegments - 1; i++)
        {
            float t = (float)i / (lightningSegments - 1);
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            // Add random offset perpendicular to the main direction
            float maxOffset = lightningJaggedness * (1f - Mathf.Abs(t - 0.5f) * 2f); // More offset in middle
            Vector3 offset = perpendicular1 * Random.Range(-maxOffset, maxOffset) +
                           perpendicular2 * Random.Range(-maxOffset, maxOffset);
            
            points[i] = basePoint + offset;
        }
        
        lightningLineRenderer.positionCount = lightningSegments;
        lightningLineRenderer.SetPositions(points);
        
        // Set same path for core renderer (brighter inner bolt)
        if (lightningCoreRenderer != null)
        {
            lightningCoreRenderer.positionCount = lightningSegments;
            lightningCoreRenderer.SetPositions(points);
        }
        
        // Generate branches
        if (branchRenderers != null && branchCount > 0)
        {
            int branchesGenerated = 0;
            for (int i = 0; i < branchRenderers.Length && branchesGenerated < branchCount; i++)
            {
                if (branchRenderers[i] != null)
                {
                    GenerateLightningBranch(branchRenderers[i], points);
                    branchesGenerated++;
                }
            }
        }
    }
    
    private void GenerateLightningBranch(LineRenderer branchRenderer, Vector3[] mainPoints)
    {
        // Pick a random point from the main bolt (not the endpoints, and not too close to either end)
        int minIndex = Mathf.Max(2, mainPoints.Length / 4);
        int maxIndex = Mathf.Min(mainPoints.Length - 3, mainPoints.Length * 3 / 4);
        int startIndex = Random.Range(minIndex, maxIndex);
        Vector3 branchStart = mainPoints[startIndex];
        
        // Branch extends perpendicular-ish to the main direction with some downward bias
        Vector3 mainDirection = (mainPoints[mainPoints.Length - 1] - mainPoints[0]).normalized;
        
        // Create a random perpendicular direction
        Vector3 randomPerp = Random.onUnitSphere;
        randomPerp = Vector3.ProjectOnPlane(randomPerp, mainDirection).normalized;
        
        // Mix perpendicular with slight downward direction
        Vector3 branchDirection = (randomPerp * 0.7f + Vector3.down * 0.3f).normalized;
        
        float branchLength = Random.Range(minBranchLength, maxBranchLength);
        Vector3 branchEnd = branchStart + branchDirection * branchLength;
        
        // Smaller branches have fewer segments
        int branchSegments = Random.Range(4, 10);
        Vector3[] branchPoints = new Vector3[branchSegments];
        branchPoints[0] = branchStart;
        branchPoints[branchSegments - 1] = branchEnd;
        
        // Create jagged branch with less variation than main bolt
        Vector3 branchDir = branchEnd - branchStart;
        Vector3 branchPerp1 = Vector3.Cross(branchDir, Vector3.up).normalized;
        if (branchPerp1.magnitude < 0.1f)
            branchPerp1 = Vector3.Cross(branchDir, Vector3.right).normalized;
        Vector3 branchPerp2 = Vector3.Cross(branchDir, branchPerp1).normalized;
        
        for (int i = 1; i < branchSegments - 1; i++)
        {
            float t = (float)i / (branchSegments - 1);
            Vector3 basePoint = Vector3.Lerp(branchStart, branchEnd, t);
            
            // Smaller jaggedness for branches
            float maxOffset = lightningJaggedness * 0.3f;
            Vector3 offset = branchPerp1 * Random.Range(-maxOffset, maxOffset) +
                           branchPerp2 * Random.Range(-maxOffset, maxOffset);
            
            branchPoints[i] = basePoint + offset;
        }
        
        branchRenderer.positionCount = branchSegments;
        branchRenderer.SetPositions(branchPoints);
    }
    
    private void EndStrike()
    {
        _currentState = State.Idle;
        _stateTimer = strikeInterval;
    }
    
    private void UpdateVisuals()
    {
        bool showLightning = _currentState == State.Striking;
        bool showWarning = _currentState == State.Warning;
        
        // Lightning LineRenderer with flicker effect
        if (lightningLineRenderer != null)
        {
            lightningLineRenderer.enabled = showLightning;
            
            // Add flicker/pulse during strike for more dynamic effect
            if (showLightning)
            {
                float flicker = Random.Range(0.7f, 1f);
                lightningLineRenderer.widthMultiplier = flicker;
            }
        }
        
        // Core renderer (bright inner bolt)
        if (lightningCoreRenderer != null)
        {
            lightningCoreRenderer.enabled = showLightning;
            
            if (showLightning)
            {
                float flicker = Random.Range(0.8f, 1f);
                lightningCoreRenderer.widthMultiplier = flicker;
            }
        }
        
        // Branch renderers
        if (branchRenderers != null)
        {
            foreach (var branch in branchRenderers)
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
        
        // Legacy lightning beam (disable if using LineRenderer)
        if (lightningBeam != null)
        {
            lightningBeam.enabled = false; // Disabled in favor of procedural lightning
        }
        
        // Particles
        if (lightningParticles != null)
        {
            if (showLightning && !lightningParticles.isPlaying)
                lightningParticles.Play();
            else if (!showLightning && lightningParticles.isPlaying)
                lightningParticles.Stop();
        }
        
        // Cloud electric particles (crackling around cloud edges)
        if (cloudElectricParticles != null)
        {
            // Particles are always on, but increase rate during warning/striking
            if (!cloudElectricParticles.isPlaying)
                cloudElectricParticles.Play();
            
            var emission = cloudElectricParticles.emission;
            
            // Increase particle rate during warning and striking
            if (showWarning)
            {
                emission.rateOverTime = 35f; // More particles during warning
            }
            else if (showLightning)
            {
                emission.rateOverTime = 50f; // Most particles during strike
            }
            else
            {
                emission.rateOverTime = 20f; // Base rate when idle
            }
            
            // Add burst of particles when entering warning or striking state
            if (showWarning && _stateTimer >= warningDuration - Time.deltaTime)
            {
                cloudElectricParticles.Emit(15);
            }
            else if (showLightning && _stateTimer >= strikeDuration - Time.deltaTime)
            {
                cloudElectricParticles.Emit(30);
            }
        }
        
        // Warning light
        if (warningLight != null)
        {
            warningLight.enabled = showWarning;
            if (showWarning)
            {
                // Pulse warning light
                float pulse = Mathf.PingPong(Time.time * 10f, 1f);
                warningLight.intensity = pulse * 3f;
            }
        }
        
        // Ground indicator
        if (groundIndicator != null)
        {
            groundIndicator.enabled = showWarning || showLightning;
            
            if (showWarning || showLightning)
            {
                // Position flat on the ground at strike point
                groundIndicator.transform.position = _strikePoint + Vector3.up * 0.05f; // Slightly above ground to prevent z-fighting
                
                // No rotation needed for sphere
                groundIndicator.transform.rotation = Quaternion.identity;
                
                // Scale to match strike radius as a flat circle (sphere squashed on Y axis)
                float diameter = strikeRadius * 2f;
                groundIndicator.transform.localScale = new Vector3(diameter, 0.05f, diameter); // Very flat Y to make it look like a disc
                
                // Pulse during warning
                if (showWarning)
                {
                    float pulse = Mathf.PingPong(Time.time * 8f, 1f);
                    Color indicatorColor = Color.Lerp(warningColor, Color.red, pulse);
                    indicatorColor.a = 0.5f;
                    
                    if (groundIndicator.material != null)
                        groundIndicator.material.color = indicatorColor;
                }
            }
        }
        
        // Cloud color
        if (_cloudMaterial != null)
        {
            if (showWarning)
            {
                // Flash warning color
                float flash = Mathf.PingPong(Time.time * 8f, 1f);
                _cloudMaterial.color = Color.Lerp(_originalCloudColor, warningColor, flash);
            }
            else
            {
                _cloudMaterial.color = _originalCloudColor;
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw strike range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, strikeRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * strikeRange);
        
        // Draw strike point if striking
        if (_currentState == State.Striking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_strikePoint, strikeRadius);
        }
    }
}
