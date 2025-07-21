using UnityEngine;

public class HealthPickup : PickupBase
{
    [Header("Health Pickup Settings")]
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private bool healPercentage = false; // true ise healAmount yüzde olarak kullanılır
    [SerializeField] private bool fullHeal = false; // true ise tamamen iyileştirir
    
   
    
    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem healingParticles;
    [SerializeField] private float particleDuration = 2f;
    
    private float glowTime = 0f;
    
    protected override void Start()
    {
        base.Start();
        
        // Material property block oluştur
        
        // Varsayılan değerler
        if (string.IsNullOrEmpty(pickupName))
            pickupName = "Health Restore";
        
        // Glow efekti için emission ayarla
        
       
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive) return;
        
        // Glow efekti animasyonu
    }
    
    protected override void ApplyPickupEffect(PlayerCombatSystem player)
    {
        if (player == null) return;
        
        float healValue = healAmount;
        
        if (fullHeal)
        {
            // Tam iyileştirme
            healValue = 9999f; // Player script'i clamp yapacak
        }
        else if (healPercentage)
        {
            // Yüzde bazlı iyileştirme (henüz max health'e erişemiyoruz, 
            // bu yüzden sabit bir değer kullanıyoruz)
            healValue = 100f * (healAmount / 100f); // 100 varsayılan max health
        }
        
        // İyileştirme uygula
        player.Heal(healValue);
        
        // Player'da healing efekti göster
        ShowHealingEffectOnPlayer(player.transform);
    }
    
    protected override string GetPickupMessage()
    {
        if (fullHeal)
            return "Tam İyileşme!";
        else if (healPercentage)
            return $"%{healAmount} Can Yenilendi!";
        else
            return $"+{healAmount} Can";
    }
    
   
   
    private void ShowHealingEffectOnPlayer(Transform playerTransform)
    {
        if (healingParticles == null) return;
        
        // Particle system'i player pozisyonunda oluştur
        ParticleSystem particles = Instantiate(healingParticles, playerTransform.position, Quaternion.identity);
        particles.transform.SetParent(playerTransform);
        particles.transform.localPosition = Vector3.up * 1f; // Player'ın ortasında
        
        // Particle ayarları
        var main = particles.main;
        main.duration = particleDuration;
        main.startLifetime = particleDuration;
        
        // Shape modülünü player etrafında döndür
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Yukarı doğru
        
        particles.Play();
        
        // Particle sistem bitince yok et
        Destroy(particles.gameObject, particleDuration + 0.5f);
    }
    
    
    }

