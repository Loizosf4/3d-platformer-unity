using UnityEngine;

/// <summary>
/// Scales and offsets the head bone of a character. Attach to the player root or the character model.
/// The script will automatically find the head bone by common naming conventions.
/// </summary>
public class HeadScaler : MonoBehaviour
{
    [Header("Head Scale Settings")]
    [Tooltip("Scale multiplier for the head. 1 = normal, 0.8 = 80% size, 1.5 = 150% size")]
    [Range(0.1f, 2f)]
    [SerializeField] private float headScale = 0.8f;
    
    [Header("Head Offset Settings")]
    [Tooltip("Position offset for the head (local space). Use this to move the head up/down/forward/back.")]
    [SerializeField] private Vector3 headOffset = Vector3.zero;
    
    [Header("Manual Assignment (Optional)")]
    [Tooltip("Manually assign the head bone if auto-detection doesn't work")]
    [SerializeField] private Transform headBone;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    
    // Common head bone names to search for
    private static readonly string[] HeadBoneNames = new string[]
    {
        "Head", "head", "HEAD",
        "mixamorig:Head", "mixamorig:head",
        "Bip001 Head", "Bip01 Head",
        "head_bone", "HeadBone",
        "Bone_Head", "bone_head",
        "Head_jnt", "head_jnt"
    };
    
    private void Start()
    {
        if (headBone == null)
        {
            headBone = FindHeadBone();
        }
        
        if (headBone != null)
        {
            ApplyHeadScale();
            
            if (showDebug)
            {
                Debug.Log($"HeadScaler: Found head bone '{headBone.name}', scaled to {headScale}");
            }
        }
        else
        {
            Debug.LogWarning("HeadScaler: Could not find head bone. Please assign it manually in the Inspector.");
        }
    }
    
    private Transform FindHeadBone()
    {
        // First try exact name matches
        foreach (string boneName in HeadBoneNames)
        {
            Transform bone = FindBoneRecursive(transform, boneName);
            if (bone != null)
            {
                return bone;
            }
        }
        
        // If not found, try partial match (contains "head")
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name.ToLower().Contains("head") && !t.name.ToLower().Contains("headtop"))
            {
                return t;
            }
        }
        
        return null;
    }
    
    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName)
        {
            return parent;
        }
        
        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    private Vector3 _originalLocalPosition;
    private bool _hasOriginalPosition = false;
    
    private void ApplyHeadScale()
    {
        if (headBone != null)
        {
            // Store original position on first apply
            if (!_hasOriginalPosition)
            {
                _originalLocalPosition = headBone.localPosition;
                _hasOriginalPosition = true;
            }
            
            headBone.localScale = Vector3.one * headScale;
            headBone.localPosition = _originalLocalPosition + headOffset;
        }
    }
    
    /// <summary>
    /// Change the head scale at runtime
    /// </summary>
    public void SetHeadScale(float scale)
    {
        headScale = scale;
        ApplyHeadScale();
    }
    
    /// <summary>
    /// Change the head offset at runtime
    /// </summary>
    public void SetHeadOffset(Vector3 offset)
    {
        headOffset = offset;
        ApplyHeadScale();
    }
    
    /// <summary>
    /// Get the current head scale
    /// </summary>
    public float GetHeadScale()
    {
        return headScale;
    }
    
    /// <summary>
    /// Get the current head offset
    /// </summary>
    public Vector3 GetHeadOffset()
    {
        return headOffset;
    }
    
    // Editor helper to preview scale changes
    private void OnValidate()
    {
        if (Application.isPlaying && headBone != null)
        {
            ApplyHeadScale();
        }
    }
}
