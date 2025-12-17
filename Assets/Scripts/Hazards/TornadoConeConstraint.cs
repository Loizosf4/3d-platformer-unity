using UnityEngine;

/// <summary>
/// Constrains particles to a cone shape by adjusting their radial position based on height.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class TornadoConeConstraint : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    
    private float bottomHeight = 10f;
    private float topHeight = 15f;
    private float radiusAtBottom = 1f;
    private float radiusAtTop = 4f;
    
    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }
    
    public void UpdateParameters(float bottom, float top, float radiusBottom, float radiusTop)
    {
        bottomHeight = bottom;
        topHeight = top;
        radiusAtBottom = radiusBottom;
        radiusAtTop = radiusTop;
    }
    
    private void LateUpdate()
    {
        if (ps == null) return;
        
        int particleCount = ps.particleCount;
        if (particleCount == 0) return;
        
        if (particles == null || particles.Length < particleCount)
        {
            particles = new ParticleSystem.Particle[particleCount];
        }
        
        ps.GetParticles(particles, particleCount);
        
        float heightRange = topHeight - bottomHeight;
        if (heightRange <= 0.001f) return;
        
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = particles[i].position;
            
            // Calculate what radius this particle should be at based on its height
            float heightNormalized = Mathf.Clamp01((pos.y - bottomHeight) / heightRange);
            float targetRadius = Mathf.Lerp(radiusAtBottom, radiusAtTop, heightNormalized);
            
            // Get current radial distance (XZ plane)
            Vector2 radialPos = new Vector2(pos.x, pos.z);
            float currentRadius = radialPos.magnitude;
            
            // Distribute particles within the cone at this height
            // They should fill from 0 to targetRadius
            if (currentRadius > 0.01f)
            {
                // Keep particles within the cone bounds at their current height
                if (currentRadius > targetRadius)
                {
                    // Particle is outside cone, snap it back
                    Vector2 direction = radialPos.normalized;
                    float clampedRadius = Random.Range(0f, targetRadius);
                    particles[i].position = new Vector3(direction.x * clampedRadius, pos.y, direction.y * clampedRadius);
                }
                // If inside, leave it alone - it's within the cone volume
            }
            else if (targetRadius > 0.01f)
            {
                // Particle is at center, push it out to a random position within the cone at this height
                float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                float randomRadius = Random.Range(0f, targetRadius);
                Vector2 randomPos = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
                
                particles[i].position = new Vector3(randomPos.x, pos.y, randomPos.y);
            }
        }
        
        ps.SetParticles(particles, particleCount);
    }
}
