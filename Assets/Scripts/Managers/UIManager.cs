using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;
public enum UIAnimationType
{
    Fade,
    Scale,
    Slide
}

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject waveClearPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject gameOverPanel; // Player ölünce
    
    [Header("Wave Clear UI")]
    [SerializeField] private TextMeshProUGUI waveClearText;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private float waveClearDisplayTime = 3f;
    
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private Slider waveProgressSlider;
    
    [Header("Animation Settings")]
    [SerializeField] private UIAnimationType animationType = UIAnimationType.Scale;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    
    [Header("Slide Animation Settings")]
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, 100f);
    
    [Header("Events")]
    public UnityEvent OnNextWaveClicked = new UnityEvent();
    
    [SerializeField]private Button ContinueGameButton;
    [SerializeField]private Button mainMenuButton;

    // References
    private WaveManager waveManager;
    private GameManager gameManager;
    private CanvasGroup pauseCanvasGroup;
    private CanvasGroup waveClearCanvasGroup;
    private CanvasGroup victoryCanvasGroup;
    private CanvasGroup gameOverCanvasGroup;

    public void Continue()
    {
        gameManager.ResumeGame();
    }

    public void Restart()
    {
        SceneManager.LoadScene(1);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }
    private void Start()
    {
        // Find managers
        waveManager = FindObjectOfType<WaveManager>();
        gameManager = FindObjectOfType<GameManager>();
        ContinueGameButton.onClick.AddListener(Continue);
        mainMenuButton.onClick.AddListener(MainMenu);

        // Setup canvas groups
        SetupCanvasGroups();
        
        // Hide all panels at start
        HideAllPanels();
        
        // Setup button listeners
        if (nextWaveButton != null)
        {
            nextWaveButton.onClick.AddListener(() => {
                OnNextWaveClicked?.Invoke();
                HideWaveClearPanel();
            });
        }
        
        // Subscribe to wave events
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.AddListener(OnWaveStarted);
            waveManager.OnWaveCompleted.AddListener(OnWaveCompleted);
            waveManager.OnAllWavesCompleted.AddListener(OnAllWavesCompleted);
            waveManager.OnEnemyKilled.AddListener(OnEnemyKilled);
        }
    }
    
    private void SetupCanvasGroups()
    {
        // Canvas group ekle (fade animasyonu için)
        if (pauseMenuPanel != null)
            pauseCanvasGroup = GetOrAddCanvasGroup(pauseMenuPanel);
        if (waveClearPanel != null)
            waveClearCanvasGroup = GetOrAddCanvasGroup(waveClearPanel);
        if (victoryPanel != null)
            victoryCanvasGroup = GetOrAddCanvasGroup(victoryPanel);
        if (gameOverPanel != null)
            gameOverCanvasGroup = GetOrAddCanvasGroup(gameOverPanel);
    }
    
    private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();
        return canvasGroup;
    }
    
    private void HideAllPanels()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (waveClearPanel != null) waveClearPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    
    private void Update()
    {
        // ESC for pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager != null)
            {
                if (gameManager.IsPaused)
                    gameManager.ResumeGame();
                else
                    gameManager.PauseGame();
            }
        }
    }
    
    // Wave Events
    private void OnWaveStarted(int waveNumber)
    {
        UpdateWaveNumber(waveNumber);
        
        // WaveManager'dan toplam düşman sayısını al
        int totalEnemies = waveManager.totalEnemiesInWave;
        
        UpdateEnemyCount(0, totalEnemies);
        UpdateWaveProgress(0, totalEnemies);
    }
    
    private void OnWaveCompleted(int waveNumber)
    {
        ShowWaveClearPanel(waveNumber);
    }
    
    private void OnAllWavesCompleted()
    {
        ShowVictoryPanel();
    }
    
    private void OnEnemyKilled(int killed, int total)
    {
        UpdateEnemyCount(killed, total);
        UpdateWaveProgress(killed, total);
    }
    
    // HUD Updates
    private void UpdateWaveNumber(int waveNumber)
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {waveNumber}/{waveManager.TotalWaves}";
        }
    }
    
    private void UpdateEnemyCount(int killed, int total)
    {
        if (enemyCountText != null)
        {
            int remaining = total - killed;
            enemyCountText.text = $"Enemy: {remaining}";
        }
    }
    
    private void UpdateWaveProgress(int killed, int total)
    {
        if (waveProgressSlider != null)
        {
            float progress = total > 0 ? (float)killed / total : 0f;
            waveProgressSlider.DOValue(progress, 0.3f);
        }
    }
    
    // Panel Show/Hide Methods
    public void ShowPauseMenu()
    {
        pauseMenuPanel.SetActive(true);
        pauseCanvasGroup.interactable = true;
        pauseCanvasGroup.alpha = 1;
        //ShowPanel(pauseMenuPanel, pauseCanvasGroup);
    }
    
    public void HidePauseMenu()
    {
        pauseMenuPanel.SetActive(false);
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.alpha = 0;
        //HidePanel(pauseMenuPanel, pauseCanvasGroup);
    }
    
    public void ShowWaveClearPanel(int waveNumber)
    {
        if (waveClearPanel == null) return;
       
        
        if (waveClearText != null)
        {
            waveClearText.text = $"Wave {waveNumber} Cleared!";
        }
        
        ShowPanel(waveClearPanel, waveClearCanvasGroup);
        
        // Auto hide after delay (if no button)
        if (nextWaveButton == null)
        {
            StartCoroutine(AutoHideWaveClear());
        }
    }
    
    private IEnumerator AutoHideWaveClear()
    {
        yield return new WaitForSeconds(waveClearDisplayTime);
        HideWaveClearPanel();
        OnNextWaveClicked?.Invoke();
    }
    
    public void HideWaveClearPanel()
    {
        HidePanel(waveClearPanel, waveClearCanvasGroup);
    }
    
    public void ShowVictoryPanel()
    {
        ShowPanel(victoryPanel, victoryCanvasGroup);
    }
    
    public void ShowGameOverPanel()
    {
        ShowPanel(gameOverPanel, gameOverCanvasGroup);
    }
    
    // Generic animation methods
    private void ShowPanel(GameObject panel, CanvasGroup canvasGroup)
    {
        if (panel == null) return;
        
        panel.SetActive(true);
        
        switch (animationType)
        {
            case UIAnimationType.Fade:
                AnimateFadeIn(canvasGroup);
                break;
                
            case UIAnimationType.Scale:
                AnimateScaleIn(panel.transform);
                break;
                
            case UIAnimationType.Slide:
                AnimateSlideIn(panel.transform);
                break;
        }
    }
    
    private void HidePanel(GameObject panel, CanvasGroup canvasGroup)
    {
        if (panel == null) return;
        
        switch (animationType)
        {
            case UIAnimationType.Fade:
                AnimateFadeOut(canvasGroup, () => panel.SetActive(false));
                break;
                
            case UIAnimationType.Scale:
                AnimateScaleOut(panel.transform, () => panel.SetActive(false));
                break;
                
            case UIAnimationType.Slide:
                AnimateSlideOut(panel.transform, () => panel.SetActive(false));
                break;
        }
    }
    
    // Animation implementations
    private void AnimateFadeIn(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, animationDuration).SetEase(animationEase);
    }
    
    private void AnimateFadeOut(CanvasGroup canvasGroup, System.Action onComplete)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.DOFade(0f, animationDuration)
            .SetEase(animationEase)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    private void AnimateScaleIn(Transform transform)
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, animationDuration).SetEase(animationEase);
    }
    
    private void AnimateScaleOut(Transform transform, System.Action onComplete)
    {
        transform.DOScale(Vector3.zero, animationDuration)
            .SetEase(animationEase)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    private void AnimateSlideIn(Transform transform)
    {
        Vector2 startPos = (Vector2)transform.localPosition - slideOffset;
        transform.localPosition = startPos;
        transform.DOLocalMove(transform.localPosition + (Vector3)slideOffset, animationDuration)
            .SetEase(animationEase);
    }
    
    private void AnimateSlideOut(Transform transform, System.Action onComplete)
    {
        Vector2 endPos = (Vector2)transform.localPosition - slideOffset;
        transform.DOLocalMove(endPos, animationDuration)
            .SetEase(animationEase)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    // Public methods for external calls
    public void OnRestartButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }
    
    public void OnMainMenuButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.LoadMainMenu();
        }
    }
    
    public void OnQuitButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.QuitGame();
        }
    }
}