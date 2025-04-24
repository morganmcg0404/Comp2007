using UnityEngine;

public class WeaponCamera : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private float nearClipPlane = 0.01f;
    [SerializeField] private float weaponLayerCullingDistance = 5f;
    
    [SerializeField] private string weaponLayerName = "Weapon";
    private int weaponLayer;
    
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (weaponCamera == null)
            weaponCamera = GetComponent<Camera>();
            
        // Get the weapon layer index
        weaponLayer = LayerMask.NameToLayer(weaponLayerName);
        
        // Verify the weapon layer exists
        if (weaponLayer == -1)
        {
            Debug.LogError($"Layer '{weaponLayerName}' doesn't exist! Please add this layer in Project Settings > Tags and Layers");
            return;
        }
            
        // Setup weapon camera
        if (weaponCamera != null)
        {
            // Setup culling mask to ONLY include the weapon layer
            weaponCamera.cullingMask = 1 << weaponLayer;
            
            // Set near clip plane very close
            weaponCamera.nearClipPlane = nearClipPlane;
            
            // Match other camera settings
            weaponCamera.fieldOfView = mainCamera.fieldOfView;
            
            // Make sure this camera renders after the main camera
            weaponCamera.depth = mainCamera.depth + 1;
            
            // Don't clear anything - we want to draw on top of the main camera
            weaponCamera.clearFlags = CameraClearFlags.Depth;
            
            // Apply layer culling distance
            float[] distances = new float[32];
            for (int i = 0; i < 32; i++)
            {
                distances[i] = i == weaponLayer ? weaponLayerCullingDistance : weaponCamera.farClipPlane;
            }
            weaponCamera.layerCullDistances = distances;
        }
        
        // Exclude weapon layer from main camera BUT PRESERVE ALL OTHER LAYERS
        if (mainCamera != null)
        {
            // Store the current culling mask
            int currentCullingMask = mainCamera.cullingMask;
            
            // Remove ONLY the weapon layer from the mask
            currentCullingMask &= ~(1 << weaponLayer);
            
            // Set the updated mask
            mainCamera.cullingMask = currentCullingMask;
        }
    }
    
    public void UpdateFOV(float fov)
    {
        if (weaponCamera != null)
            weaponCamera.fieldOfView = fov;
    }
    
    // For debugging - add this to help troubleshoot
    private void OnGUI()
    {
        // Only show in development builds or editor
        if (Debug.isDebugBuild || Application.isEditor)
        {
            if (weaponLayer == -1)
            {
                GUI.Label(new Rect(10, 10, 300, 20), $"WARNING: Layer '{weaponLayerName}' not found!");
            }
        }
    }
}