using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    WaveCompleted,
    Victory,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private GameState currentState = GameState.Playing;
    [SerializeField] private bool startFirstWaveAutomatically = true;
    [SerializeField] private float waveStartDelay = 2f;
    
    [Header("Scene Management")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int currentSceneIndex = -1;
    
    [Header("References")]
    private WaveManager waveManager;
    private UIManager uiManager;
    private PlayerCombatSystem playerCombat;
    
    // Properties
    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused;
    public bool IsPlaying => currentState == GameState.Playing;
    
    // Singleton pattern (opsiyonel)
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameManager>();
            return instance;
        }
    }
    
    private void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Get current scene index
        if (currentSceneIndex == -1)
            currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
    }
    
    private void Start()
    {
        // Find references
        waveManager = FindObjectOfType<WaveManager>();
        uiManager = FindObjectOfType<UIManager>();
        playerCombat = FindObjectOfType<PlayerCombatSystem>();
        
        // Subscribe to events
        if (waveManager != null)
        {
            waveManager.OnWaveCompleted.AddListener(OnWaveCompleted);
            waveManager.OnAllWavesCompleted.AddListener(OnVictory);
        }
        
        if (uiManager != null)
        {
            uiManager.OnNextWaveClicked.AddListener(StartNextWave);
        }
        
        // Start first wave
        if (startFirstWaveAutomatically)
        {
            StartCoroutine(StartFirstWaveDelayed());
        }
    }
    
    private IEnumerator StartFirstWaveDelayed()
    {
        yield return new WaitForSeconds(waveStartDelay);
        StartNextWave();
    }
    
    // State Management
    private void SetGameState(GameState newState)
    {
        currentState = newState;
        
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
                
            case GameState.WaveCompleted:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
                
            case GameState.Victory:
            case GameState.GameOver:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
    
    // Game Flow Methods
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            if (uiManager != null)
                uiManager.ShowPauseMenu();
        }
    }
    
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            if (uiManager != null)
                uiManager.HidePauseMenu();
        }
    }
    
    public void StartNextWave()
    {
        if (waveManager != null && !waveManager.IsWaveActive)
        {
            SetGameState(GameState.Playing);
            waveManager.StartNextWave();
        }
    }
    
    // Event Handlers
    private void OnWaveCompleted(int waveNumber)
    {
        SetGameState(GameState.WaveCompleted);
        // UI Manager otomatik olarak wave clear panel'i gösterecek
    }
    
    private void OnVictory()
    {
        SetGameState(GameState.Victory);
        // UI Manager otomatik olarak victory panel'i gösterecek
    }
    
    public void OnPlayerDeath()
    {
        SetGameState(GameState.GameOver);
        if (uiManager != null)
            uiManager.ShowGameOverPanel();
    }
    
    // Scene Management
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(currentSceneIndex);
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Main Menu scene adı ayarlanmamış!");
            SceneManager.LoadScene(0); // İlk sahneyi yükle
        }
    }
    
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextSceneIndex = currentSceneIndex + 1;
        
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("Son level'desiniz!");
            LoadMainMenu();
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("Oyundan çıkılıyor...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // Debug Methods
    [ContextMenu("Win Game")]
    private void DebugWinGame()
    {
        OnVictory();
    }
    
    [ContextMenu("Lose Game")]
    private void DebugLoseGame()
    {
        OnPlayerDeath();
    }
    
    [ContextMenu("Skip Current Wave")]
    private void DebugSkipWave()
    {
        if (waveManager != null)
            waveManager.SkipWave();
    }
    
    private void OnDestroy()
    {
        // Cleanup
        Time.timeScale = 1f;
    }
}