using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the tab menu that displays player stats when Tab key is pressed
/// </summary>
public class TabMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private KeyCode menuKey = KeyCode.Tab;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private float animationSpeed = 5f;
    
    [Header("Player Information")]
    [SerializeField] private string playerName = "Player 1";
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI totalPointsText;
    
    // Internal references
    private CanvasGroup canvasGroup;
    private bool isMenuVisible = false;
    private PointSystem pointSystem;
    
    // Add this variable to track if the menu is forced open
    private bool isForceOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get or add CanvasGroup component for smooth fade
        canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = menuPanel.AddComponent<CanvasGroup>();
        }
        
        // Initially hide the menu
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Find the point system
        pointSystem = FindAnyObjectByType<PointSystem>();
        
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        // Hide the menu panel at start
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Skip all input processing if game is paused
        if (PauseManager.IsPaused())
            return;
        
        // If force open, keep it open no matter what
        if (isForceOpen)
        {
            if (!isMenuVisible)
            {
                ShowMenu();
            }
            
            // Keep stats updated
            UpdateStats();
            return;
        }
        
        // Normal tab menu behavior
        bool keyPressed = Input.GetKey(menuKey);
        
        // Handle showing/hiding the menu
        if (keyPressed && !isMenuVisible)
        {
            ShowMenu();
        }
        else if (!keyPressed && isMenuVisible)
        {
            HideMenu();
        }
        
        // If menu is active, update the stats
        if (isMenuVisible)
        {
            UpdateStats();
        }
    }
    
    /// <summary>
    /// Shows the tab menu with a smooth fade in
    /// </summary>
    private void ShowMenu()
    {
        isMenuVisible = true;
        
        // Enable the menu panel
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        
        // Start the fade in
        StartCoroutine(FadeMenu(1f));
        
        // Update stats immediately
        UpdateStats();
    }
    
    /// <summary>
    /// Hides the tab menu with a smooth fade out
    /// </summary>
    private void HideMenu()
    {
        isMenuVisible = false;
        
        // Start the fade out
        StartCoroutine(FadeMenu(0f, () => {
            // Disable the menu panel after fade out is complete
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }));
    }
    
    /// <summary>
    /// Fades the menu to the target alpha value
    /// </summary>
    private System.Collections.IEnumerator FadeMenu(float targetAlpha, System.Action onComplete = null)
    {
        if (canvasGroup != null)
        {
            // Set interactable based on whether we're showing or hiding
            canvasGroup.interactable = targetAlpha > 0;
            canvasGroup.blocksRaycasts = targetAlpha > 0;
            
            // Smoothly interpolate the alpha
            while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * animationSpeed);
                yield return null;
            }
            
            // Ensure we reach exactly the target value
            canvasGroup.alpha = targetAlpha;
        }
        
        // Call the completion callback if provided
        if (onComplete != null)
        {
            onComplete();
        }
    }
    
    /// <summary>
    /// Updates the player stats displayed in the menu
    /// </summary>
    private void UpdateStats()
    {
        if (pointSystem != null)
        {
            // Update total points
            if (totalPointsText != null)
            {
                totalPointsText.text = $"{pointSystem.GetCurrentPoints()}";
            }
            
            // Update kill count
            if (killCountText != null)
            {
                killCountText.text = $"{pointSystem.GetKillCount()}";
            }
        }
    }
    
    /// <summary>
    /// Sets the player name display
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
        
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }
    
    /// <summary>
    /// Forces the menu to open and disables closing
    /// </summary>
    public void ForceMenuOpen()
    {
        isForceOpen = true;
        
        // Show the menu (reuse existing code)
        ShowMenu();
        
        // Update stats immediately 
        UpdateStats();
        
        // Add a death message if you want
        if (playerNameText != null)
        {
            playerNameText.text = playerName + " (DEAD)";
            playerNameText.color = Color.red;
        }
    }
}
