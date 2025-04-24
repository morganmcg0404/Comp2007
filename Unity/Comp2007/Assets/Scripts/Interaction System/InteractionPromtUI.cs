using UnityEngine;
using TMPro;

/// Manages the UI prompt that appears when player is near interactable objects
/// Handles display, positioning, and text updates for interaction prompts
public class InteractionPromtUI : MonoBehaviour
{
    private Camera _mainCam;                                    // Reference to main camera
    [SerializeField] private GameObject _uiPanel;              // UI panel containing the prompt
    [SerializeField] private TextMeshProUGUI _promptText;      // Text component for displaying prompt

    /// Initializes camera reference and hides UI panel on start
    private void Start()
    {
        _mainCam = Camera.main;
        _uiPanel.SetActive(false);
    }

    /// Updates prompt orientation to face camera each frame after other updates
    /// Ensures prompt is always readable by the player
    void LateUpdate()
    {
        var rotation = _mainCam.transform.rotation;
        transform.LookAt(transform.position + rotation * Vector3.forward, 
                        rotation * Vector3.up);
    }

    /// Tracks whether the prompt is currently being displayed
    public bool IsDisplayed = false;

    /// Shows the UI prompt with specified text
    public void SetUp(string promptText)
    {
        _promptText.text = promptText;
        _uiPanel.SetActive(true);
        IsDisplayed = true;
    }

    /// Hides the UI prompt
    public void Close()
    {
        _uiPanel.SetActive(false);
        IsDisplayed = false;
    }
}