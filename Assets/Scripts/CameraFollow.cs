using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player

    [Header("Camera Settings")]
    public float smoothTime = 0.2f; // How fast the camera catches up
    public Vector2 offset = new Vector2(0f, 1.5f); // Offset above player
    public bool enableDebugLogs = false; // Control debug output

    [Header("Dynamic Player Detection")]
    public bool continuousSearch = true; // Keep searching for player if not found
    public float searchInterval = 0.5f; // How often to search for player

    [Header("Look Ahead")]
    public float lookAheadDistanceX = 2f;
    public float lookAheadReturnSpeed = 2f;
    public float lookAheadMoveSpeed = 3f;

    [Header("Optional Limits")]
    public bool useLimits = false;
    public Vector2 minCameraPos;
    public Vector2 maxCameraPos;

    private Vector3 _currentVelocity;
    private float _lookAheadPosX;
    private float _lastTargetX;
    private float _debugTimer = 0f; // For controlled debug output
    private float _searchTimer = 0f; // For player search timing

    void Start()
    {
        // Don't search immediately, let PlatformGenerator spawn the player first
        if (enableDebugLogs)
            Debug.Log("[CameraFollow] Camera initialized, will search for player...");
    }
    
    void FindPlayer()
    {
        // Try to find player by tag first (most reliable for spawned players)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            _lastTargetX = target.position.x;
            
            // Set initial camera position immediately when player is found
            Vector3 initialPos = target.position + new Vector3(0, offset.y, -10f);
            transform.position = initialPos;
            
            if (enableDebugLogs)
                Debug.Log("[CameraFollow] Found player by tag: " + target.name + " at position: " + target.position);
            return;
        }
        
        // Try to find by SimplePlayerController component
        ClearSky.SimplePlayerController playerController = FindFirstObjectByType<ClearSky.SimplePlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
            _lastTargetX = target.position.x;
            
            // Set initial camera position immediately when player is found
            Vector3 initialPos = target.position + new Vector3(0, offset.y, -10f);
            transform.position = initialPos;
            
            if (enableDebugLogs)
                Debug.Log("[CameraFollow] Found player by component: " + target.name + " at position: " + target.position);
            return;
        }
        
        if (enableDebugLogs && _searchTimer <= 0.1f) // Only log once per search cycle
            Debug.LogWarning("[CameraFollow] Could not find player yet... (will keep searching)");
    }
    
    // Public method for PlatformGenerator to call when player is spawned
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
            _lastTargetX = target.position.x;
            
            // Set initial camera position immediately
            Vector3 initialPos = target.position + new Vector3(0, offset.y, -10f);
            transform.position = initialPos;
            
            if (enableDebugLogs)
                Debug.Log("[CameraFollow] Target set by external script: " + target.name + " at position: " + target.position);
        }
    }

    void LateUpdate()
    {
        // If no target, search for player periodically
        if (target == null)
        {
            if (continuousSearch)
            {
                _searchTimer += Time.deltaTime;
                if (_searchTimer >= searchInterval)
                {
                    FindPlayer();
                    _searchTimer = 0f;
                }
            }
            return;
        }

        // Calculate horizontal movement
        float xMoveDelta = target.position.x - _lastTargetX;

        bool updateLookAhead = Mathf.Abs(xMoveDelta) > 0.01f;

        if (updateLookAhead)
            _lookAheadPosX = Mathf.Lerp(_lookAheadPosX, lookAheadDistanceX * Mathf.Sign(xMoveDelta), Time.deltaTime * lookAheadMoveSpeed);
        else
            _lookAheadPosX = Mathf.Lerp(_lookAheadPosX, 0, Time.deltaTime * lookAheadReturnSpeed);

        Vector3 targetPos = target.position + new Vector3(_lookAheadPosX, offset.y, -10f);

        // Smoothly move the camera
        Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, smoothTime);

        // Clamp if limits are used
        if (useLimits)
        {
            newPos.x = Mathf.Clamp(newPos.x, minCameraPos.x, maxCameraPos.x);
            newPos.y = Mathf.Clamp(newPos.y, minCameraPos.y, maxCameraPos.y);
        }

        transform.position = newPos;
        _lastTargetX = target.position.x;
        
        // Controlled debug output (only every 0.5 seconds)
        if (enableDebugLogs)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= 0.5f)
            {
                Debug.Log($"[CameraFollow] Player: {target.position.x:F2}, Camera: {transform.position.x:F2}, Diff: {Mathf.Abs(target.position.x - transform.position.x):F2}");
                _debugTimer = 0f;
            }
        }
    }
    
    [ContextMenu("Find Player Now")]
    void DebugFindPlayer()
    {
        FindPlayer();
    }
    
    [ContextMenu("Snap to Target")]
    void DebugSnapToTarget()
    {
        if (target != null)
        {
            transform.position = new Vector3(target.position.x + _lookAheadPosX, target.position.y + offset.y, -10f);
            Debug.Log("[CameraFollow] Snapped to target");
        }
    }
    
    [ContextMenu("Toggle Debug Logs")]
    void ToggleDebugLogs()
    {
        enableDebugLogs = !enableDebugLogs;
        Debug.Log("[CameraFollow] Debug logs: " + (enableDebugLogs ? "ENABLED" : "DISABLED"));
    }
    
    [ContextMenu("Force Search for Player")]
    void ForceSearchForPlayer()
    {
        _searchTimer = searchInterval; // Reset timer to trigger immediate search
        FindPlayer();
        Debug.Log("[CameraFollow] Forced player search executed");
    }
}
