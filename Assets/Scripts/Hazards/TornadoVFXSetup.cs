using UnityEngine;

/// <summary>
/// Sets up a layered tornado particle system with controllable parameters.
/// Three layers: Top (wide circular), Body (cylindrical spiral), Base (ground ring)
/// </summary>
public class TornadoVFXSetup : MonoBehaviour
{
    [Header("Particle System References")]
    public ParticleSystem topLayerParticles;
    public ParticleSystem bodyLayerParticles;
    public ParticleSystem baseLayerParticles;
    
    [Header("Top Layer Settings")]
    public int topParticleCount = 60;
    public float topParticleSize = 0.6f;
    public float topRotationSpeed = 8f;
    public float topRadiusAtBottomHeight = 1f;
    public float topRadiusAtTopHeight = 4f;
    public float topBottomHeight = 10f;
    public float topTopHeight = 15f;
    public Color topColorMin = new Color(0.6f, 0.6f, 0.6f, 0.3f);
    public Color topColorMax = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    
    [Header("Body Layer Settings")]
    public int bodyParticleCount = 100;
    public float bodyParticleSize = 0.5f;
    public float bodyRotationSpeed = 180f;
    public float bodyRadiusAtBottomHeight = 0.5f;
    public float bodyRadiusAtTopHeight = 2f;
    public float bodyBottomHeight = 0f;
    public float bodyTopHeight = 10f;
    public float bodyUpwardSpeed = 2f;
    public Color bodyColorMin = new Color(0.6f, 0.6f, 0.6f, 0.3f);
    public Color bodyColorMax = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    
    [Header("Base Layer Settings")]
    public int baseParticleCount = 40;
    public float baseParticleSize = 0.8f;
    public float baseRotationSpeed = 360f;
    public float baseMinRadius = 0f;
    public float baseMaxRadius = 3f;
    public float baseRadialSpeed = 3f;
    public float baseHeight = 0.1f;
    public Color baseColorMin = new Color(0.6f, 0.6f, 0.6f, 0.3f);
    public Color baseColorMax = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    
    private void Start()
    {
        SetupParticleSystems();
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateParticleSystems();
        }
    }
    
    private void SetupParticleSystems()
    {
        Transform particlesParent = transform.Find("Particles");
        if (particlesParent == null)
        {
            GameObject particlesObj = new GameObject("Particles");
            particlesObj.transform.SetParent(transform);
            particlesObj.transform.localPosition = Vector3.zero;
            particlesParent = particlesObj.transform;
        }
        
        if (topLayerParticles == null)
            topLayerParticles = CreateTopLayer(particlesParent);
        
        if (bodyLayerParticles == null)
            bodyLayerParticles = CreateBodyLayer(particlesParent);
        
        if (baseLayerParticles == null)
            baseLayerParticles = CreateBaseLayer(particlesParent);
        
        UpdateParticleSystems();
    }
    
    private void UpdateParticleSystems()
    {
        if (topLayerParticles != null)
            ConfigureTopLayer(topLayerParticles);
        
        if (bodyLayerParticles != null)
            ConfigureBodyLayer(bodyLayerParticles);
        
        if (baseLayerParticles != null)
            ConfigureBaseLayer(baseLayerParticles);
    }
    
    private ParticleSystem CreateTopLayer(Transform parent)
    {
        GameObject go = new GameObject("PS_TopLayer");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        TornadoConeConstraint constraint = go.AddComponent<TornadoConeConstraint>();
        
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        return ps;
    }
    
    private ParticleSystem CreateBodyLayer(Transform parent)
    {
        GameObject go = new GameObject("PS_BodyLayer");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        return ps;
    }
    
    private ParticleSystem CreateBaseLayer(Transform parent)
    {
        GameObject go = new GameObject("PS_BaseLayer");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        return ps;
    }
    
    private void ConfigureTopLayer(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = topParticleSize;
        main.startColor = new ParticleSystem.MinMaxGradient(topColorMin, topColorMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.maxParticles = topParticleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var emission = ps.emission;
        emission.rateOverTime = topParticleCount / 3f;
        
        // Use box shape to spawn throughout the volume
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        float heightRange = topTopHeight - topBottomHeight;
        float maxRadius = Mathf.Max(topRadiusAtBottomHeight, topRadiusAtTopHeight);
        
        // Box that covers the entire cone area
        shape.scale = new Vector3(maxRadius * 2f, heightRange, maxRadius * 2f);
        shape.position = new Vector3(0f, topBottomHeight + heightRange / 2f, 0f);
        
        // Use velocity to create the cone effect
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;
        velocity.orbitalY = topRotationSpeed;
        
        // Get or add the cone constraint component
        TornadoConeConstraint constraint = ps.GetComponent<TornadoConeConstraint>();
        if (constraint == null)
        {
            constraint = ps.gameObject.AddComponent<TornadoConeConstraint>();
        }
        
        constraint.UpdateParameters(
            topBottomHeight, topTopHeight, 
            topRadiusAtBottomHeight, topRadiusAtTopHeight
        );
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
        
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 1f;
    }
    
    private void ConfigureBodyLayer(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = 0f;
        main.startSize = bodyParticleSize;
        main.startColor = new ParticleSystem.MinMaxGradient(bodyColorMin, bodyColorMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.maxParticles = bodyParticleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var emission = ps.emission;
        emission.rateOverTime = bodyParticleCount / 3f;
        
        // Shape: Box volume to fill the cone
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        float heightRange = bodyTopHeight - bodyBottomHeight;
        float maxRadius = Mathf.Max(bodyRadiusAtBottomHeight, bodyRadiusAtTopHeight);
        
        // Use box to spawn particles, constraint will shape them into cone
        shape.scale = new Vector3(maxRadius * 2f, heightRange, maxRadius * 2f);
        shape.position = new Vector3(0f, bodyBottomHeight + heightRange / 2f, 0f);
        
        // Velocity to make particles spiral and rise
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = bodyUpwardSpeed;
        velocity.z = 0f;
        velocity.orbitalY = bodyRotationSpeed;
        
        // Get or add the cone constraint component
        TornadoConeConstraint constraint = ps.GetComponent<TornadoConeConstraint>();
        if (constraint == null)
        {
            constraint = ps.gameObject.AddComponent<TornadoConeConstraint>();
        }
        
        constraint.UpdateParameters(
            bodyBottomHeight, bodyTopHeight, 
            bodyRadiusAtBottomHeight, bodyRadiusAtTopHeight
        );
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 0.8f;
        noise.separateAxes = true;
        noise.strengthY = 0.3f;
    }
    
    private void ConfigureBaseLayer(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = 0f;
        main.startSize = baseParticleSize;
        main.startColor = new ParticleSystem.MinMaxGradient(baseColorMin, baseColorMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.maxParticles = baseParticleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var emission = ps.emission;
        emission.rateOverTime = baseParticleCount / 2f;
        
        // Shape: Circle ring filling from min to max radius
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = baseMaxRadius;
        // radiusThickness determines the inner hollow part
        // 0 = filled circle, 1 = thin ring
        shape.radiusThickness = baseMinRadius / Mathf.Max(baseMaxRadius, 0.001f);
        shape.position = new Vector3(0f, baseHeight, 0f);
        
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;
        velocity.orbitalY = baseRotationSpeed;
        velocity.radial = baseRadialSpeed;
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(1f, 1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
    }
}

