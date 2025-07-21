using UnityEngine;
using System.Collections;

public class PowerBoostPickup : PickupBase
{
    [Header("Power Boost Settings")]
    [SerializeField] private float chargeSpeedMultiplier = 2f; // 2x hızlı charge
    [SerializeField] private float boostDuration = 15f; // Boost süresi
    [SerializeField] private bool stackable = false; // Birden fazla alınabilir mi
    [SerializeField] private bool refreshDuration = true; // Yeni pickup süreyi yeniler mi
    
    [Header("Visual Customization")]
    [SerializeField] private Material energyMaterial;
    [SerializeField] private float energyIntensity = 3f;
    [SerializeField] private Color energyColor = new Color(1f, 0.5f, 0f, 1f); // Turuncu
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 1.2f);
    
    [Header("Lightning Effect")]
    [SerializeField] private bool enableLightningEffect = true;
    [SerializeField] private LineRenderer lightningPrefab;
    [SerializeField] private int lightningSegments = 10;
    [SerializeField] private float lightningAmplitude = 0.3f;
    [SerializeField] private float lightningFrequency = 0.1f;
    
    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem energyParticles;
    [SerializeField] private ParticleSystem orbitalParticles;
    [SerializeField] private float orbitalRadius = 1.5f;
    
    private float pulseTime = 0f;
    private MaterialPropertyBlock propertyBlock;
    private LineRenderer[] lightningBolts;
    private Coroutine lightningCoroutine;
    
    // Aktif boost takibi için static değişken
    private static PowerBoostPickup activeBoost = null;
    
    protected override void Start()
    {
        base.Start();
        
        // Material property block
        propertyBlock = new MaterialPropertyBlock();
        
        // Varsayılan değerler
        if (string.IsNullOrEmpty(pickupName))
            pickupName = "Güç Artışı";
        
        // Energy efekti setup
        SetupEnergyEffect();
        
        // Lightning efekti setup
        if (enableLightningEffect)
            SetupLightningEffect();
        
        // Particle setup
        SetupParticles();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive) return;
        
        // Pulse efekti
        UpdatePulseEffect();
        
        // Lightning güncelleme
        if (enableLightningEffect)
            UpdateLightning();
        
        // Orbital particles
        UpdateOrbitalParticles();
    }
    
    protected override void ApplyPickupEffect(PlayerCombatSystem player)
    {
        if (player == null) return;
        
        // Stack kontrolü
        if (!stackable && activeBoost != null && activeBoost != this)
        {
            if (!refreshDuration)
            {
                // Stack edilemez ve süre yenilenmez
                Debug.Log("Zaten aktif bir güç artışı var!");
                return;
            }
        }
        
        // Boost uygula
        player.ApplyPowerBoost(chargeSpeedMultiplier, boostDuration);
        
        // Aktif boost olarak işaretle
        activeBoost = this;
        
        // Player'a efekt ekle
        StartCoroutine(AttachBoostEffectToPlayer(player.transform));
        
        // Boost bitince temizle
        StartCoroutine(ClearActiveBoostAfterDuration());
    }
    
    protected override string GetPickupMessage()
    {
        return $"Güç Artışı! {boostDuration} saniye boyunca {chargeSpeedMultiplier}x hızlı şarj!";
    }
    
    private void SetupEnergyEffect()
    {
        if (energyMaterial == null || renderers == null) return;
        
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                // Energy material'i ekle
                Material[] mats = renderer.materials;
                Material[] newMats = new Material[mats.Length + 1];
                
                for (int i = 0; i < mats.Length; i++)
                {
                    newMats[i] = mats[i];
                }
                
                newMats[newMats.Length - 1] = energyMaterial;
                renderer.materials = newMats;
                
                // Emission ayarla
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", energyColor * energyIntensity);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
    
    private void SetupLightningEffect()
    {
        if (lightningPrefab == null) return;
        
        // 3-4 lightning bolt oluştur
        int boltCount = Random.Range(3, 5);
        lightningBolts = new LineRenderer[boltCount];
        
        for (int i = 0; i < boltCount; i++)
        {
            LineRenderer bolt = Instantiate(lightningPrefab, transform);
            bolt.positionCount = lightningSegments;
            bolt.startColor = energyColor;
            bolt.endColor = energyColor * 0.5f;
            bolt.startWidth = 0.05f;
            bolt.endWidth = 0.01f;
            lightningBolts[i] = bolt;
        }
        
        // Lightning animasyon coroutine
        lightningCoroutine = StartCoroutine(AnimateLightning());
    }
    
    private void SetupParticles()
    {
        // Energy particles
        if (energyParticles != null)
        {
            var main = energyParticles.main;
            main.startColor = energyColor;
            main.startSpeed = 2f;
            main.startLifetime = 1f;
            
            var shape = energyParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
        }
        
        // Orbital particles
        if (orbitalParticles != null)
        {
            var main = orbitalParticles.main;
            main.startColor = energyColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            var velocityOverLifetime = orbitalParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.orbitalX = 1f;
        }
    }
    
    private void UpdatePulseEffect()
    {
        pulseTime += Time.deltaTime;
        
        // Scale pulse
        float scaleMultiplier = pulseCurve.Evaluate(Mathf.PingPong(pulseTime * 0.5f, 1f));
        transform.localScale = Vector3.one * scaleMultiplier;
        
        // Emission pulse
        float emissionMultiplier = 0.5f + Mathf.PingPong(pulseTime, 1f) * 0.5f;
        
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", energyColor * (energyIntensity * emissionMultiplier));
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
    
    private void UpdateLightning()
    {
        if (lightningBolts == null) return;
        
        foreach (var bolt in lightningBolts)
        {
            if (bolt == null) continue;
            
            // Random rotation
            bolt.transform.rotation = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );
        }
    }
    
    private void UpdateOrbitalParticles()
    {
        if (orbitalParticles == null) return;
        
        // Particles'ı pickup etrafında döndür
        orbitalParticles.transform.rotation = Quaternion.Euler(0f, Time.time * 50f, 0f);
    }
    
    private IEnumerator AnimateLightning()
    {
        while (true)
        {
            foreach (var bolt in lightningBolts)
            {
                if (bolt == null) continue;
                
                // Lightning pozisyonlarını güncelle
                for (int i = 0; i < lightningSegments; i++)
                {
                    float t = (float)i / (lightningSegments - 1);
                    Vector3 pos = Vector3.Lerp(Vector3.zero, Vector3.up * 2f, t);
                    
                    // Random offset
                    if (i > 0 && i < lightningSegments - 1)
                    {
                        pos += new Vector3(
                            Random.Range(-lightningAmplitude, lightningAmplitude),
                            0f,
                            Random.Range(-lightningAmplitude, lightningAmplitude)
                        );
                    }
                    
                    bolt.SetPosition(i, pos);
                }
                
                // Visibility toggle
                bolt.enabled = Random.Range(0f, 1f) > 0.3f;
            }
            
            yield return new WaitForSeconds(lightningFrequency);
        }
    }
    
    private IEnumerator AttachBoostEffectToPlayer(Transform playerTransform)
    {
        // Player'a enerji efekti ekle
        GameObject boostEffect = new GameObject("PowerBoostEffect");
        boostEffect.transform.SetParent(playerTransform);
        boostEffect.transform.localPosition = Vector3.zero;
        
        // Particle system ekle
        ParticleSystem particles = boostEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = energyColor;
        main.startLifetime = 1f;
        main.startSpeed = 3f;
        main.maxParticles = 50;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.radiusThickness = 0.2f;
        
        var emission = particles.emission;
        emission.rateOverTime = 20f;
        
        // Trail renderer ekle (enerji izleri)
        TrailRenderer trail = boostEffect.AddComponent<TrailRenderer>();
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = energyColor;
        trail.endColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0f);
        trail.startWidth = 0.2f;
        trail.endWidth = 0f;
        trail.time = 0.5f;
        
        // Light ekle
        Light boostLight = boostEffect.AddComponent<Light>();
        boostLight.color = energyColor;
        boostLight.intensity = 2f;
        boostLight.range = 5f;
        
        // Boost süresi boyunca efekti göster
        yield return new WaitForSeconds(boostDuration);
        
        // Fade out
        float fadeTime = 1f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / fadeTime);
            
            var e = particles.emission;
            e.rateOverTime = 20f * t;
            
            boostLight.intensity = 2f * t;
            
            yield return null;
        }
        
        // Temizle
        Destroy(boostEffect);
    }
    
    private IEnumerator ClearActiveBoostAfterDuration()
    {
        yield return new WaitForSeconds(boostDuration);
        
        if (activeBoost == this)
            activeBoost = null;
    }
    
    void OnDestroy()
    {
        // Lightning coroutine'i durdur
        if (lightningCoroutine != null)
            StopCoroutine(lightningCoroutine);
        
        // Aktif boost ise temizle
        if (activeBoost == this)
            activeBoost = null;
    }
    
    // Editor'de görselleştirme
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Yıldırım sembolü
        Gizmos.color = energyColor;
        Vector3 pos = transform.position + Vector3.up * 2f;
        
        // Basit yıldırım çizimi
        Vector3[] lightning = new Vector3[]
        {
            pos + new Vector3(-0.2f, 0.3f, 0f),
            pos + new Vector3(0.1f, 0f, 0f),
            pos + new Vector3(-0.1f, 0f, 0f),
            pos + new Vector3(0.2f, -0.3f, 0f)
        };
        
        for (int i = 0; i < lightning.Length - 1; i++)
        {
            Gizmos.DrawLine(lightning[i], lightning[i + 1]);
        }
    }
}