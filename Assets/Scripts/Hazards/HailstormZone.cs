using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zone where hail falls from above, damaging the player on impact.
/// Spawns hailstones at intervals within the zone bounds.
/// </summary>
public class HailstormZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Size of the hailstorm area (X and Z dimensions)")]
    [SerializeField] private Vector2 zoneSize = new Vector2(10f, 10f);
    
    [Tooltip("Height above zone where hail spawns")]
    [SerializeField] private float spawnHeight = 15f;
    
    [Tooltip("Maximum lifetime of hailstones before despawn (seconds)")]
    [SerializeField] private float hailLifetime = 10f;
    
    [Header("Hail Spawn Settings")]
    [Tooltip("Time between hailstone spawns (seconds)")]
    [SerializeField] private float spawnInterval = 0.2f;
    
    [Tooltip("Number of hailstones to spawn per interval")]
    [SerializeField] private int hailstonesPerSpawn = 1;
    
    [Tooltip("Random offset to spawn timing (0-1)")]
    [SerializeField, Range(0f, 1f)] private float spawnRandomness = 0.3f;
    
    [Tooltip("If true, starts spawning immediately. If false, must call StartHailstorm()")]
    [SerializeField] private bool autoStart = true;
    
    [Header("Hail Properties")]
    [Tooltip("Size of hailstones (scale)")]
    [SerializeField] private Vector2 hailSizeRange = new Vector2(0.2f, 0.5f);
    
    [Tooltip("Fall speed of hailstones")]
    [SerializeField] private float fallSpeed = 10f;
    
    [Tooltip("Randomness in fall direction (0-1)")]
    [SerializeField, Range(0f, 1f)] private float fallRandomness = 0.1f;
    
    [Tooltip("Gravity multiplier for hailstones")]
    [SerializeField] private float gravityMultiplier = 1f;
    
    [Tooltip("Damage dealt per hailstone hit")]
    [SerializeField, Range(1, 5)] private int damagePerHailstone = 1;
    
    [Tooltip("Player tag to detect")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visuals")]
    [Tooltip("Hailstone prefab (sphere with collider). Leave null to use default.")]
    [SerializeField] private GameObject hailstonePrefab;
    
    [Tooltip("Particle effect when hail hits ground")]
    [SerializeField] private ParticleSystem groundImpactEffect;
    
    [Tooltip("Hailstone material")]
    [SerializeField] private Material hailMaterial;
    
    [Tooltip("Ground impact layers")]
    [SerializeField] private LayerMask groundLayers = 1;
    
    [Header("Audio")]
    [Tooltip("Looping ambient sound while hailstorm is active.")]
    [SerializeField] private AudioClip ambientLoopSound;
    [Tooltip("Volume for ambient loop (0-1).")]
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;
    [Tooltip("Spatial blend for ambient. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float ambientSpatialBlend = 0.8f;

    [Tooltip("Sound when hail hits player")]
    [SerializeField] private AudioClip hitPlayerSound;
    
    [Tooltip("Sound when hail hits ground")]
    [SerializeField] private AudioClip hitGroundSound;
    
    // State
    private bool _isActive;
    private float _spawnTimer;
    private List<GameObject> _activeHailstones = new List<GameObject>();
    private AudioSource _audioSource;
    private GameObject _defaultHailstonePrefab;
    
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // Setup ambient loop if provided
        if (ambientLoopSound != null)
        {
            _audioSource.clip = ambientLoopSound;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = ambientVolume;
            _audioSource.spatialBlend = ambientSpatialBlend;

            if (AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    _audioSource.outputAudioMixerGroup = sfxGroup[0];
            }
        }
        
        // Create default hailstone prefab if none provided
        if (hailstonePrefab == null)
        {
            CreateDefaultHailstonePrefab();
        }
        
        if (autoStart)
        {
            StartHailstorm();
        }
    }
    
    private void Update()
    {
        if (!_isActive) return;
        
        // Spawn timer
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnHailstones();
            
            // Reset timer with randomness
            float randomOffset = Random.Range(-spawnRandomness, spawnRandomness) * spawnInterval;
            _spawnTimer = spawnInterval + randomOffset;
        }
        
        // Clean up destroyed hailstones
        _activeHailstones.RemoveAll(h => h == null);
    }
    
    private void SpawnHailstones()
    {
        for (int i = 0; i < hailstonesPerSpawn; i++)
        {
            // Random position within zone
            float randomX = Random.Range(-zoneSize.x / 2f, zoneSize.x / 2f);
            float randomZ = Random.Range(-zoneSize.y / 2f, zoneSize.y / 2f);
            Vector3 spawnPos = transform.position + new Vector3(randomX, spawnHeight, randomZ);
            
            // Spawn hailstone
            GameObject hailstone = Instantiate(hailstonePrefab, spawnPos, Quaternion.identity);
            
            // Random size
            float size = Random.Range(hailSizeRange.x, hailSizeRange.y);
            hailstone.transform.localScale = Vector3.one * size;
            
            // Add hailstone component
            var hailComp = hailstone.GetComponent<Hailstone>();
            if (hailComp == null)
                hailComp = hailstone.AddComponent<Hailstone>();
            
            hailComp.Initialize(this, fallSpeed, fallRandomness, gravityMultiplier, hailLifetime);
            
            _activeHailstones.Add(hailstone);
        }
    }
    
    private void CreateDefaultHailstonePrefab()
    {
        _defaultHailstonePrefab = new GameObject("DefaultHailstone");
        _defaultHailstonePrefab.SetActive(false);
        
        // Add sphere mesh
        var meshFilter = _defaultHailstonePrefab.AddComponent<MeshFilter>();
        meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        
        var meshRenderer = _defaultHailstonePrefab.AddComponent<MeshRenderer>();
        if (hailMaterial != null)
            meshRenderer.material = hailMaterial;
        
        // Add sphere collider (trigger)
        var collider = _defaultHailstonePrefab.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        
        // Add rigidbody
        var rb = _defaultHailstonePrefab.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        hailstonePrefab = _defaultHailstonePrefab;
    }
    
    public void StartHailstorm()
    {
        _isActive = true;
        _spawnTimer = spawnInterval;

        if (ambientLoopSound != null && _audioSource != null && !_audioSource.isPlaying)
            _audioSource.Play();
    }
    
    public void StopHailstorm()
    {
        _isActive = false;
        
        // Destroy all active hailstones
        foreach (var hail in _activeHailstones)
        {
            if (hail != null)
                Destroy(hail);
        }
        _activeHailstones.Clear();

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }
    
    public void OnHailHitPlayer(Vector3 position, PlayerHealthController health)
    {
        if (health == null) return;
        
        health.TryTakeDamage(position, damagePerHailstone);
        
        // Play sound
        if (_audioSource != null && hitPlayerSound != null)
            _audioSource.PlayOneShot(hitPlayerSound);
    }
    
    public void OnHailHitGround(Vector3 position)
    {
        // Spawn impact effect
        if (groundImpactEffect != null)
        {
            Instantiate(groundImpactEffect, position, Quaternion.identity);
        }
        
        // Play sound
        if (_audioSource != null && hitGroundSound != null)
            _audioSource.PlayOneShot(hitGroundSound, 0.3f);
    }
    
    public string PlayerTag => playerTag;
    public LayerMask GroundLayers => groundLayers;
    
    private void OnDrawGizmosSelected()
    {
        // Draw zone bounds
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Vector3 center = transform.position + Vector3.up * spawnHeight / 2f;
        Vector3 size = new Vector3(zoneSize.x, spawnHeight, zoneSize.y);
        Gizmos.DrawCube(center, size);
        
        // Draw zone outline
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
        
        // Draw spawn plane
        Gizmos.color = Color.yellow;
        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeight;
        Gizmos.DrawWireCube(spawnCenter, new Vector3(zoneSize.x, 0.1f, zoneSize.y));
    }
}

/// <summary>
/// Individual hailstone behavior - handles falling and collision detection.
/// </summary>
public class Hailstone : MonoBehaviour
{
    private HailstormZone _zone;
    private Vector3 _velocity;
    private float _lifetime;
    private float _age;
    private bool _hasHitPlayer;
    private Rigidbody _rb;
    
    public void Initialize(HailstormZone zone, float fallSpeed, float randomness, float gravityMult, float lifetime)
    {
        _zone = zone;
        _lifetime = lifetime;
        _age = 0f;
        
        // Add random horizontal velocity
        float randomX = Random.Range(-randomness, randomness) * fallSpeed;
        float randomZ = Random.Range(-randomness, randomness) * fallSpeed;
        _velocity = new Vector3(randomX, -fallSpeed, randomZ);
        
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.velocity = _velocity;
            _rb.drag = 0f;
            _rb.angularDrag = 0.05f;
            
            // Apply custom gravity
            Physics.gravity = new Vector3(0, -9.81f * gravityMult, 0);
        }
    }
    
    private void Update()
    {
        _age += Time.deltaTime;
        
        // Destroy if too old
        if (_age >= _lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_zone == null) return;
        
        // Check for player hit
        if (!_hasHitPlayer && other.CompareTag(_zone.PlayerTag))
        {
            var health = other.GetComponentInParent<PlayerHealthController>();
            if (health != null)
            {
                _zone.OnHailHitPlayer(transform.position, health);
                _hasHitPlayer = true;
                Destroy(gameObject);
                return;
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if hit ground
        if (((1 << collision.gameObject.layer) & _zone.GroundLayers) != 0)
        {
            _zone.OnHailHitGround(collision.contacts[0].point);
            Destroy(gameObject);
        }
    }
}
