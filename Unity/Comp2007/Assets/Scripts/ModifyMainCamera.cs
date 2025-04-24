using UnityEngine;

public class ModifyMainCamera : MonoBehaviour
{
    [SerializeField] private float nearClipPlane = 0.01f;
    
    // Remove this field and let WeaponCamera handle layer management
    // [SerializeField] private string weaponLayerName = "Weapon";
    
    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Set near clip plane very close to avoid clipping
            cam.nearClipPlane = nearClipPlane;
            
            // REMOVE the weapon layer exclusion code from here
            // Let WeaponCamera.cs handle this to avoid conflicts
        }
    }
}