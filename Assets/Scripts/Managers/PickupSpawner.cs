using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PickupTypeWeight
{
    public GameObject pickupPrefab;
    [Range(0f, 100f)]
    public float spawnWeight = 50f; // Spawn olma ağırlığı/şansı
}

public class PickupSpawner : MonoBehaviour
{
    [Header("Pickup Types")]
    [SerializeField] private List<PickupTypeWeight> pickupTypes = new List<PickupTypeWeight>();
    
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool preventDuplicateSpawns = true; // Aynı noktada birden fazla pickup olmasın
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private bool checkGroundBeforeSpawn = true;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float groundCheckDistance = 10f;
    [SerializeField] private float minDistanceBetweenPickups = 2f;
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private float effectDuration = 1f;
    
    // Aktif pickup'ları ve kullanılan spawn noktalarını takip et
    private Dictionary<Transform, GameObject> occupiedSpawnPoints = new Dictionary<Transform, GameObject>();
    private List<GameObject> activePickups = new List<GameObject>();
    private float totalWeight = 0f;
    
    private void Start()
    {
        // Spawn point'leri otomatik bul
        if (spawnPoints.Count == 0)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child != transform && child.name.Contains("Pickup"))
                {
                    spawnPoints.Add(child);
                }
            }
        }
        
        // Toplam ağırlığı hesapla
        CalculateTotalWeight();
    }
    
    private void CalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (var pickupType in pickupTypes)
        {
            if (pickupType.pickupPrefab != null)
                totalWeight += pickupType.spawnWeight;
        }
    }
    
    public GameObject SpawnRandomPickup()
    {
        if (pickupTypes.Count == 0)
        {
            Debug.LogWarning("Pickup type listesi boş!");
            return null;
        }
        
        // Random pickup seç
        GameObject selectedPrefab = SelectRandomPickup();
        if (selectedPrefab == null)
        {
            Debug.LogWarning("Pickup seçilemedi!");
            return null;
        }
        
        return SpawnPickup(selectedPrefab);
    }
    
    public GameObject SpawnPickup(GameObject pickupPrefab)
    {
        if (pickupPrefab == null)
        {
            Debug.LogError("Pickup prefab null!");
            return null;
        }
        
        // Uygun spawn noktası bul
        Transform spawnPoint = GetAvailableSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("Uygun spawn noktası bulunamadı!");
            return null;
        }
        
        Vector3 spawnPosition = spawnPoint.position;
        
        // Ground check
        if (checkGroundBeforeSpawn)
        {
            RaycastHit hit;
            if (Physics.Raycast(spawnPosition + Vector3.up * 2f, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
            }
        }
        
        // Spawn effect
        PlaySpawnEffect(spawnPosition);
        
        // Pickup spawn
        GameObject pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        activePickups.Add(pickup);
        
        // Spawn noktasını meşgul olarak işaretle
        if (preventDuplicateSpawns)
        {
            occupiedSpawnPoints[spawnPoint] = pickup;
        }
        
        // Pickup alındığında spawn noktasını boşaltacak listener ekle
        PickupBase pickupBase = pickup.GetComponent<PickupBase>();
        if (pickupBase != null)
        {
            // Pickup alındığında bildirim için event eklenebilir
        }
        
        Debug.Log($"Pickup spawn edildi: {pickup.name} - Pozisyon: {spawnPosition}");
        
        return pickup;
    }
    
    private GameObject SelectRandomPickup()
    {
        if (totalWeight <= 0f)
        {
            CalculateTotalWeight();
            if (totalWeight <= 0f) return null;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var pickupType in pickupTypes)
        {
            if (pickupType.pickupPrefab == null) continue;
            
            currentWeight += pickupType.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return pickupType.pickupPrefab;
            }
        }
        
        // Fallback - ilk geçerli prefab'ı döndür
        foreach (var pickupType in pickupTypes)
        {
            if (pickupType.pickupPrefab != null)
                return pickupType.pickupPrefab;
        }
        
        return null;
    }
    
    private Transform GetAvailableSpawnPoint()
    {
        // Temizlik - yok olmuş pickup'ları listeden çıkar
        CleanupDestroyedPickups();
        
        List<Transform> availablePoints = new List<Transform>();
        
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;
            
            bool isAvailable = true;
            
            // Bu nokta meşgul mü?
            if (preventDuplicateSpawns && occupiedSpawnPoints.ContainsKey(spawnPoint))
            {
                if (occupiedSpawnPoints[spawnPoint] != null)
                {
                    isAvailable = false;
                }
            }
            
            // Yakında başka pickup var mı?
            if (isAvailable && minDistanceBetweenPickups > 0f)
            {
                foreach (var pickup in activePickups)
                {
                    if (pickup != null)
                    {
                        float distance = Vector3.Distance(spawnPoint.position, pickup.transform.position);
                        if (distance < minDistanceBetweenPickups)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                }
            }
            
            if (isAvailable)
            {
                availablePoints.Add(spawnPoint);
            }
        }
        
        if (availablePoints.Count == 0)
            return null;
        
        // Random bir nokta seç
        return availablePoints[Random.Range(0, availablePoints.Count)];
    }
    
    private void CleanupDestroyedPickups()
    {
        // Yok olmuş pickup'ları temizle
        activePickups.RemoveAll(p => p == null);
        
        // Meşgul spawn noktalarını güncelle
        List<Transform> keysToRemove = new List<Transform>();
        foreach (var kvp in occupiedSpawnPoints)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            occupiedSpawnPoints.Remove(key);
        }
    }
    
    private void PlaySpawnEffect(Vector3 position)
    {
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, position);
        }
    }
    
    public void ClearAllPickups()
    {
        foreach (var pickup in activePickups)
        {
            if (pickup != null)
                Destroy(pickup);
        }
        activePickups.Clear();
        occupiedSpawnPoints.Clear();
    }
    
    public void OnPickupCollected(GameObject pickup, Transform spawnPoint)
    {
        // Pickup alındığında çağrılacak
        if (activePickups.Contains(pickup))
        {
            activePickups.Remove(pickup);
        }
        
        if (spawnPoint != null && occupiedSpawnPoints.ContainsKey(spawnPoint))
        {
            occupiedSpawnPoints.Remove(spawnPoint);
        }
    }
    
    // Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                // Boş spawn noktaları
                bool isOccupied = occupiedSpawnPoints.ContainsKey(spawnPoint) && occupiedSpawnPoints[spawnPoint] != null;
                Gizmos.color = isOccupied ? Color.red : Color.green;
                
                Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
                
                // Ground check
                if (checkGroundBeforeSpawn)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(
                        spawnPoint.position + Vector3.up * 2f,
                        spawnPoint.position + Vector3.down * (groundCheckDistance - 2f)
                    );
                }
            }
        }
        
        // Minimum distance
        if (minDistanceBetweenPickups > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, minDistanceBetweenPickups);
                }
            }
        }
    }
}