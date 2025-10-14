using UnityEngine;

public class PlayerPositionDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebug = true;
    public float debugInterval = 1f; // Show debug every second
    
    private float debugTimer = 0f;
    private Vector3 lastPosition;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
        
        if (enableDebug)
            Debug.Log($"[PlayerDebug] Starting position: {transform.position}");
    }
    
    void Update()
    {
        if (!enableDebug) return;
        
        debugTimer += Time.deltaTime;
        
        if (debugTimer >= debugInterval)
        {
            Vector3 currentPos = transform.position;
            Vector3 movement = currentPos - lastPosition;
            
            string velocityInfo = "";
            if (rb != null)
                velocityInfo = $" | Velocity: {rb.linearVelocity}";
            
            Debug.Log($"[PlayerDebug] Pos: {currentPos} | Movement: {movement}{velocityInfo}");
            
            lastPosition = currentPos;
            debugTimer = 0f;
        }
    }
    
    [ContextMenu("Log Current Position")]
    void LogCurrentPosition()
    {
        Debug.Log($"[PlayerDebug] Current Position: {transform.position}");
        if (rb != null)
            Debug.Log($"[PlayerDebug] Current Velocity: {rb.linearVelocity}");
    }
}