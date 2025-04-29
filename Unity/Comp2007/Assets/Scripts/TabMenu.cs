using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the tab menu that displays player stats when Tab key is pressed
/// Handles smooth transitions, stats updating, and special states like force-open
/// </summary>
public class TabMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    /// <summary>
    /// Key used to open and close the tab menu
    /// </summary>
    [SerializeField] private KeyCode menuKey = KeyCode.Tab;
    
    /// <summary>
    /// The GameObject containing all menu UI elements
    /// </summary>
    [SerializeField] private GameObject menuPanel;
    
    /// <summary>
    /// Speed of the fade animation when showing/hiding the menu
    /// </summary>
    [SerializeField] private float animationSpeed = 5f;
    
    [Header("Player Information")]
    /// <summary>
    /// Display name of the player shown in the tab menu
    /// </summary>
    [SerializeField] private string playerName = "Player 1";
    
    /// <summary>
    /// Text component for displaying player name
    /// </summary>
    [SerializeField] private TextMeshProUGUI playerNameText;
    
    /// <summary>
    /// Text component for displaying the kill count
    /// </summary>
    [SerializeField] private TextMeshProUGUI killCountText;
    
    /// <summary>
    /// Text component for displaying the total points earned
    /// </summary>
    [SerializeField] private TextMeshProUGUI totalPointsText;
    
    // Internal references
    /// <summary>
    /// Canvas group component used for smooth fade transitions
    /// </summary>
    private CanvasGroup canvasGroup;
    
    /// <summary>
    /// Tracks whether the menu is currently visible
    /// </summary>
    private bool isMenuVisible = false;
    
    /// <summary>
    /// Reference to the point system for retrieving player stats
    /// </summary>
    private PointSystem pointSystem;
    
    /// <summary>
    /// Indicates if the menu is forced to stay open (e.g., when player dies)
    /// </summary>
    private bool isForceOpen = false;

    /// <summary>
    /// Initializes the tab menu, sets up the canvas group, and configures initial visibility
    /// </summary>
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

    /// <summary>
    /// Checks for input to show/hide the tab menu and updates displayed stats
    /// Handles special cases like forced menu visibility during pause
    /// </summary>
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
    /// Shows the tab menu with a smooth fade in animation
    /// Activates the menu panel and updates stats immediately
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
    /// Hides the tab menu with a smooth fade out animation
    /// Disables the menu panel after the animation completes
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
    /// Coroutine that smoothly fades the menu to the target alpha value
    /// </summary>
    /// <param name="targetAlpha">The target alpha value (0 for invisible, 1 for fully visible)</param>
    /// <param name="onComplete">Optional callback to execute when the fade completes</param>
    /// <returns>IEnumerator for coroutine execution</returns>
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
    /// Updates the player stats displayed in the menu from the point system
    /// Updates kill count and total points text elements
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
    /// Sets the player name display in the tab menu
    /// </summary>
    /// <param name="name">The player name to display</param>
    public void SetPlayerName(string name)
    {
        playerName = name;
        
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }
    
    /// <summary>
    /// Forces the menu to open and disables closing via the tab key
    /// Used for end-game scenarios like player death
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
