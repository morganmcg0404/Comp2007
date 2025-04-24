using UnityEngine;
using TMPro;
using System.Collections;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    
    private float accum = 0f;
    private int frames = 0;
    private float timeleft;
    private bool isVisible = false;
    
    void Awake()
    {
        // Check if we should display FPS based on saved setting
        isVisible = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        
        // Initialize timer
        timeleft = updateInterval;
        
        // Apply initial visibility state
        ApplyVisibility();
    }
    
    void Start()
    {
        // Secondary check to make sure we apply the correct visibility
        ApplyVisibility();
    }
    
    void Update()
    {
        // Only calculate FPS if the counter is visible
        if (!isVisible) 
            return;
            
        // Update FPS counter
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;
        
        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = string.Format("{0:F1} FPS", fps);
            
            if (fpsText != null)
            {
                fpsText.text = format;
                
                // Change color based on FPS
                if (fps < 30)
                    fpsText.color = Color.red;
                else if (fps < 60)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.green;
            }
            
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
    
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyVisibility();
    }
    
    private void ApplyVisibility()
    {
        // Make sure the Text component exists
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogError("FPS Text component is missing. Please assign it in the FPSCounter inspector.");
        }
    }
    
    // This helps verify in the Unity editor if everything is set up correctly
    private void OnValidate()
    {
        if (fpsText == null)
        {
            Debug.LogWarning("FPS Counter is missing its TextMeshPro reference. Please assign it in the inspector.");
        }
    }
}