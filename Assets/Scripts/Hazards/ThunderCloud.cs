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
    [Tooltip("Lightning beam renderer (line renderer or mesh)")]
    [SerializeField] private Renderer lightningBeam;
    
    [Tooltip("Particle system for lightning effect")]
    [SerializeField] private ParticleSystem lightningParticles;
    
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
        
        // Auto-find components if not assigned
        if (lightningBeam == null)
            lightningBeam = GetComponentInChildren<Renderer>();
        
        if (lightningParticles == null)
            lightningParticles = GetComponentInChildren<ParticleSystem>();
        
        if (warningLight == null)
            warningLight = GetComponentInChildren<Light>();
        
        // Start in idle state
        _currentState = strikeOnStart ? State.Warning : State.Idle;
        _stateTimer = strikeOnStart ? warningDuration : strikeInterval;
        
        UpdateVisuals();
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
    
    private void EndStrike()
    {
        _currentState = State.Idle;
        _stateTimer = strikeInterval;
    }
    
    private void UpdateVisuals()
    {
        bool showLightning = _currentState == State.Striking;
        bool showWarning = _currentState == State.Warning;
        
        // Lightning beam
        if (lightningBeam != null)
        {
            lightningBeam.enabled = showLightning;
            
            if (showLightning)
            {
                // Scale beam to match strike distance
                float distance = Vector3.Distance(transform.position, _strikePoint);
                Vector3 scale = lightningBeam.transform.localScale;
                scale.y = distance;
                lightningBeam.transform.localScale = scale;
                
                // Position beam midpoint
                lightningBeam.transform.position = (transform.position + _strikePoint) / 2f;
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
                // Position cylinder at center of strike zone (ground + half height)
                Vector3 cylinderCenter = _strikePoint + Vector3.up * (strikeHeight / 2f);
                groundIndicator.transform.position = cylinderCenter;
                
                // Scale to match strike radius and height
                // Cylinder primitive is 2 units tall and 1 unit diameter by default
                float diameter = strikeRadius * 2f;
                float heightScale = strikeHeight / 2f; // Divide by 2 because default cylinder is 2 units tall
                groundIndicator.transform.localScale = new Vector3(diameter, heightScale, diameter);
                
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
