using UnityEngine;
using TMPro;

/// <summary>
/// Handles the UI display of interaction prompts
/// </summary>
public class InteractionPromtUI : MonoBehaviour
{
    [SerializeField] private GameObject _promptPanel;  // The panel containing the prompt
    [SerializeField] private TextMeshProUGUI _promptText;  // The text component to display the prompt
    
    /// <summary>
    /// Gets whether the prompt is currently being displayed
    /// </summary>
    public bool IsDisplayed { get; private set; }

    /// <summary>
    /// Initializes the UI prompt by hiding it on start
    /// </summary>
    private void Start()
    {
        if (_promptPanel != null)
            _promptPanel.SetActive(false);
        
        IsDisplayed = false;
    }

    /// <summary>
    /// Sets up and displays the interaction prompt with the given text
    /// </summary>
    /// <param name="promptText">Text to display in the prompt</param>
    public void SetUp(string promptText)
    {
        if (_promptText != null)
            _promptText.text = promptText;
        
        if (_promptPanel != null && !_promptPanel.activeSelf)
            _promptPanel.SetActive(true);
        
        IsDisplayed = true;
    }

    /// <summary>
    /// Closes/hides the interaction prompt
    /// </summary>
    public void Close()
    {
        if (_promptPanel != null)
            _promptPanel.SetActive(false);
        
        IsDisplayed = false;
    }
}