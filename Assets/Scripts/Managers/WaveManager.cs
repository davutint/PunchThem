using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Wave
{
    public string waveName = "Wave";
    public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
    public int pickupCount = 2;
}

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public int count = 5;
}

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private int currentWaveIndex = 0;
    
    [Header("Spawn Settings")]
    [SerializeField] private float enemySpawnIntervalMin = 1f;
    [SerializeField] private float enemySpawnIntervalMax = 3f;
    [SerializeField] private float pickupSpawnIntervalMin = 5f;
    [SerializeField] private float pickupSpawnIntervalMax = 10f;
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PickupSpawner pickupSpawner;
    
    [Header("Events")]
    public UnityEvent<int> OnWaveStarted = new UnityEvent<int>();
    public UnityEvent<int> OnWaveCompleted = new UnityEvent<int>();
    public UnityEvent OnAllWavesCompleted = new UnityEvent();
    public UnityEvent<int, int> OnEnemyKilled = new UnityEvent<int, int>(); // current, total
    
    // Private variables
    private Wave currentWave;
    public int totalEnemiesInWave = 0;
    private int enemiesKilled = 0;
    private int enemiesSpawned = 0;
    private bool isWaveActive = false;
    private Coroutine enemySpawnCoroutine;
    private Coroutine pickupSpawnCoroutine;
    
    // Public properties
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves.Count;
    public bool IsWaveActive => isWaveActive;
    public int EnemiesRemaining => totalEnemiesInWave - enemiesKilled;
    public static WaveManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Spawner referanslarını kontrol et
        if (enemySpawner == null)
            enemySpawner = GetComponentInChildren<EnemySpawner>();
        if (pickupSpawner == null)
            pickupSpawner = GetComponentInChildren<PickupSpawner>();
            
        // İlk wave'i başlatmak için bekle (GameManager hazır olsun)
        StartCoroutine(DelayedStart());
    }
    
    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        // GameManager otomatik başlatacak
    }
    
    public void StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            Debug.LogWarning("Tüm wave'ler tamamlandı!");
            return;
        }
        
        StartWave(currentWaveIndex);
    }

    public bool IsLastWave()
    {
        if (currentWaveIndex > waves.Count)
        {
            return true;
        }
        else
            return false;
    }
    
    private void StartWave(int waveIndex)
    {
        if (waveIndex >= waves.Count || waveIndex < 0)
        {
            Debug.LogError($"Geçersiz wave index: {waveIndex}");
            return;
        }
        
        currentWave = waves[waveIndex];
        currentWaveIndex = waveIndex;
        isWaveActive = true;
        
        // Wave istatistiklerini sıfırla
        totalEnemiesInWave = 0;
        enemiesKilled = 0;
        enemiesSpawned = 0;
        
        // Toplam düşman sayısını hesapla
        foreach (var enemyInfo in currentWave.enemies)
        {
            totalEnemiesInWave += enemyInfo.count;
        }
        
        Debug.Log($"Wave {CurrentWaveNumber} başladı! Toplam düşman: {totalEnemiesInWave}");
        
        // Event'i tetikle
        OnWaveStarted?.Invoke(CurrentWaveNumber);
        
        // Spawn coroutine'lerini başlat
        if (enemySpawnCoroutine != null)
            StopCoroutine(enemySpawnCoroutine);
        if (pickupSpawnCoroutine != null)
            StopCoroutine(pickupSpawnCoroutine);
            
        enemySpawnCoroutine = StartCoroutine(SpawnEnemies());
        pickupSpawnCoroutine = StartCoroutine(SpawnPickups());
    }
    
    private IEnumerator SpawnEnemies()
    {
        yield return new WaitForSeconds(1f); // Wave başlangıcında kısa bekleme
        
        foreach (var enemyInfo in currentWave.enemies)
        {
            for (int i = 0; i < enemyInfo.count; i++)
            {
                // Spawn enemy
                if (enemySpawner != null && enemyInfo.enemyPrefab != null)
                {
                    enemySpawner.SpawnEnemy(enemyInfo.enemyPrefab);
                    enemiesSpawned++;
                }
                
                // Random interval bekle
                float waitTime = UnityEngine.Random.Range(enemySpawnIntervalMin, enemySpawnIntervalMax);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
    
    private IEnumerator SpawnPickups()
    {
        yield return new WaitForSeconds(2f); // İlk pickup için bekleme
        
        for (int i = 0; i < currentWave.pickupCount; i++)
        {
            // Spawn pickup
            if (pickupSpawner != null)
            {
                pickupSpawner.SpawnRandomPickup();
            }
            
            // Random interval bekle
            float waitTime = UnityEngine.Random.Range(pickupSpawnIntervalMin, pickupSpawnIntervalMax);
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    // Düşman öldüğünde çağrılacak method
    public void OnEnemyDeath()
    {
        if (!isWaveActive) return;
        
        enemiesKilled++;
        OnEnemyKilled?.Invoke(enemiesKilled, totalEnemiesInWave);
        
        Debug.Log($"Düşman öldürüldü: {enemiesKilled}/{totalEnemiesInWave}");
        
        // Tüm düşmanlar öldürüldü mü?
        if (enemiesKilled >= totalEnemiesInWave)
        {
            CompleteWave();
        }
    }
    
    private void CompleteWave()
    {
        isWaveActive = false;
        
        // Spawn coroutine'lerini durdur
        if (enemySpawnCoroutine != null)
        {
            StopCoroutine(enemySpawnCoroutine);
            enemySpawnCoroutine = null;
        }
        if (pickupSpawnCoroutine != null)
        {
            StopCoroutine(pickupSpawnCoroutine);
            pickupSpawnCoroutine = null;
        }
        
        Debug.Log($"Wave {CurrentWaveNumber} tamamlandı!");

      
        // Son wave mi?
        if (currentWaveIndex >= waves.Count - 1)
        {
            Debug.Log("Tüm wave'ler tamamlandı! Oyun kazanıldı!");
            OnAllWavesCompleted?.Invoke();
        }
        else
        {
            OnWaveCompleted?.Invoke(CurrentWaveNumber);
            // Sonraki wave'e geçiş UI'da yapılacak
            currentWaveIndex++;
        }
    }
    
    // Debug için
    public void SkipWave()
    {
        if (isWaveActive)
        {
            CompleteWave();
        }
    }
    
    public Wave GetCurrentWave()
    {
        return currentWave;
    }
    
    public float GetWaveProgress()
    {
        if (totalEnemiesInWave == 0) return 0f;
        return (float)enemiesKilled / totalEnemiesInWave;
    }
}