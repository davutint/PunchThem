using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool useRandomSpawnPoint = true;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeightOffset = 0.1f;
    [SerializeField] private bool checkGroundBeforeSpawn = true;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float groundCheckDistance = 10f;
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float effectDuration = 1f;
    
    // Spawn edilen düşmanları takip et
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int lastUsedSpawnPoint = -1;
    
    private void Start()
    {
        // Spawn point'leri otomatik bul
        if (spawnPoints.Count == 0)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child != transform && child.name.Contains("Spawn"))
                {
                    spawnPoints.Add(child);
                }
            }
            
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("Hiç spawn point bulunamadı! Manuel olarak ekleyin.");
            }
        }
    }
    
    public GameObject SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab null!");
            return null;
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Spawn point yok!");
            return null;
        }
        
        // Spawn pozisyonu seç
        Transform spawnPoint = GetSpawnPoint();
        Vector3 spawnPosition = spawnPoint.position;
        
        // Ground check
        if (checkGroundBeforeSpawn)
        {
            RaycastHit hit;
            if (Physics.Raycast(spawnPosition + Vector3.up, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
            }
        }
        
        // Spawn effect
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Enemy spawn
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.rotation);
        activeEnemies.Add(enemy);
        
        // Enemy'ye WaveManager referansı ekle (ölünce haber vermesi için)
        EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
        if (notifier == null)
        {
            notifier = enemy.AddComponent<EnemyDeathNotifier>();
        }
        
        // EnemyHealthSystem varsa death event'ine bağlan
        EnemyHealthSystem healthSystem = enemy.GetComponent<EnemyHealthSystem>();
        if (healthSystem != null)
        {
            // EnemyHealthSystem'e ölüm callback'i eklenebilir
        }
        
        Debug.Log($"Düşman spawn edildi: {enemy.name} - Pozisyon: {spawnPosition}");
        
        return enemy;
    }
    
    private Transform GetSpawnPoint()
    {
        if (useRandomSpawnPoint)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            // Aynı spawn point'i üst üste kullanma
            if (spawnPoints.Count > 1 && randomIndex == lastUsedSpawnPoint)
            {
                randomIndex = (randomIndex + 1) % spawnPoints.Count;
            }
            lastUsedSpawnPoint = randomIndex;
            return spawnPoints[randomIndex];
        }
        else
        {
            // Sırayla kullan
            lastUsedSpawnPoint = (lastUsedSpawnPoint + 1) % spawnPoints.Count;
            return spawnPoints[lastUsedSpawnPoint];
        }
    }
    
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
    }
    
    public int GetActiveEnemyCount()
    {
        // Null olanları temizle
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }
    
    // Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                // Spawn noktası
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                
                // Yön göstergesi
                Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 1f);
                
                // Ground check
                if (checkGroundBeforeSpawn)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(
                        spawnPoint.position + Vector3.up,
                        spawnPoint.position + Vector3.down * groundCheckDistance
                    );
                }
            }
        }
    }
}

// Düşman öldüğünde WaveManager'a haber verecek component
public class EnemyDeathNotifier : MonoBehaviour
{
    private WaveManager waveManager;
    
    private void Start()
    {
        waveManager = FindObjectOfType<WaveManager>();
    }
    
    // Bu methodu düşman öldüğünde çağır
    public void NotifyDeath()
    {
        if (waveManager != null)
        {
            waveManager.OnEnemyDeath();
        }
        
        // Kendini yok et
        Destroy(gameObject, 0.1f);
    }
    
    private void OnDestroy()
    {
        // Eğer sahne kapanmıyorsa ve waveManager hala varsa
        if (waveManager != null && gameObject.scene.isLoaded)
        {
            // Otomatik bildir (güvenlik için)
            // waveManager.OnEnemyDeath();
        }
    }
}