using UnityEngine;

/// <summary>
/// Hollow Knight-style camera follow system with smooth movement, dead zones, and responsive fall catch-up
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform to follow")]
    public Transform target;

    [Header("Camera Smoothing")]
    [Tooltip("Horizontal smooth time (lower = faster, more responsive)")]
    [Range(0.05f, 0.5f)]
    public float smoothTimeX = 0.15f;
    
    [Tooltip("Vertical smooth time when following normally")]
    [Range(0.05f, 0.5f)]
    public float smoothTimeY = 0.2f;
    
    [Tooltip("Vertical smooth time when player falls (much faster catch-up)")]
    [Range(0.01f, 0.1f)]
    public float fallCatchupSpeed = 0.03f;
    
    [Tooltip("Distance below camera before fast catch-up activates")]
    [Range(1f, 5f)]
    public float fallCatchupThreshold = 2.5f;

    [Header("Camera Offset")]
    [Tooltip("Offset from player position")]
    public Vector2 offset = new Vector2(0f, 1.5f);

    [Header("Dead Zone (Hollow Knight Style)")]
    [Tooltip("Dead zone size - camera only moves when player leaves this area")]
    public Vector2 deadZoneSize = new Vector2(1.5f, 2f);
    
    [Tooltip("Enable dead zone for smoother camera")]
    public bool useDeadZone = true;

    [Header("Look Ahead")]
    [Tooltip("How far ahead camera looks when player moves")]
    [Range(0f, 4f)]
    public float lookAheadDistanceX = 2.5f;
    
    [Tooltip("Speed of look-ahead return to center")]
    [Range(1f, 10f)]
    public float lookAheadReturnSpeed = 4f;
    
    [Tooltip("Speed of look-ahead movement")]
    [Range(1f, 10f)]
    public float lookAheadMoveSpeed = 5f;
    
    [Tooltip("Minimum movement speed to trigger look-ahead")]
    [Range(0.1f, 2f)]
    public float lookAheadThreshold = 0.5f;

    [Header("Player Detection")]
    [Tooltip("Keep searching for player if not found")]
    public bool continuousSearch = true;
    
    [Tooltip("How often to search for player (seconds)")]
    [Range(0.1f, 2f)]
    public float searchInterval = 0.5f;

    [Header("Camera Limits")]
    [Tooltip("Enable camera boundaries")]
    public bool useLimits = false;
    
    [Tooltip("Minimum camera position")]
    public Vector2 minCameraPos;
    
    [Tooltip("Maximum camera position")]
    public Vector2 maxCameraPos;

    // Private variables
    private Vector3 _currentVelocity;
    private float _lookAheadPosX;
    private float _lastTargetX;
    private float _lastTargetY;
    private float _searchTimer = 0f;
    private Vector3 _targetPosition;
    private bool _isInitialized = false;

    void Start()
    {
        // Initialize camera position when player is found
        FindPlayer();
    }

    void FindPlayer()
    {
        // Try to find player by tag first (most reliable for spawned players)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            SetTarget(playerObj.transform);
            return;
        }

        // Try to find by SimplePlayerController component
        ClearSky.SimplePlayerController playerController = FindFirstObjectByType<ClearSky.SimplePlayerController>();
        if (playerController != null)
        {
            SetTarget(playerController.transform);
        }
    }

    /// <summary>
    /// Public method for PlatformGenerator to call when player is spawned
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
            _lastTargetX = target.position.x;
            _lastTargetY = target.position.y;

            // Set initial camera position immediately
            Vector3 initialPos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, -10f);
            transform.position = initialPos;
            _targetPosition = initialPos;
            _isInitialized = true;
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

        // Calculate movement deltas
        float xMoveDelta = target.position.x - _lastTargetX;
        float yMoveDelta = target.position.y - _lastTargetY;
        
        // Calculate target position
        Vector3 desiredPos = CalculateDesiredPosition(xMoveDelta, yMoveDelta);
        
        // Apply dead zone if enabled
        if (useDeadZone && _isInitialized)
        {
            desiredPos = ApplyDeadZone(desiredPos);
        }
        
        // Calculate smooth movement
        Vector3 newPos = SmoothCameraMovement(desiredPos, yMoveDelta);
        
        // Apply limits if enabled
        if (useLimits)
        {
            newPos.x = Mathf.Clamp(newPos.x, minCameraPos.x, maxCameraPos.x);
            newPos.y = Mathf.Clamp(newPos.y, minCameraPos.y, maxCameraPos.y);
        }

        // Update camera position
        transform.position = newPos;
        
        // Store last positions
        _lastTargetX = target.position.x;
        _lastTargetY = target.position.y;
    }

    Vector3 CalculateDesiredPosition(float xDelta, float yDelta)
    {
        // Update look-ahead based on horizontal movement
        bool isMoving = Mathf.Abs(xDelta) > lookAheadThreshold * Time.deltaTime;
        
        if (isMoving)
        {
            // Smoothly move look-ahead in direction of movement
            float targetLookAhead = lookAheadDistanceX * Mathf.Sign(xDelta);
            _lookAheadPosX = Mathf.Lerp(_lookAheadPosX, targetLookAhead, Time.deltaTime * lookAheadMoveSpeed);
        }
        else
        {
            // Return look-ahead to center
            _lookAheadPosX = Mathf.Lerp(_lookAheadPosX, 0f, Time.deltaTime * lookAheadReturnSpeed);
        }

        // Calculate desired position with offset and look-ahead
        float desiredX = target.position.x + offset.x + _lookAheadPosX;
        float desiredY = target.position.y + offset.y;
        
        return new Vector3(desiredX, desiredY, -10f);
    }

    Vector3 ApplyDeadZone(Vector3 desiredPos)
    {
        Vector3 currentPos = transform.position;
        Vector3 playerWorldPos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, -10f);
        
        // Calculate distance from camera center to player
        Vector2 distance = new Vector2(
            playerWorldPos.x - currentPos.x,
            playerWorldPos.y - currentPos.y
        );
        
        // Only move if player is outside dead zone
        if (Mathf.Abs(distance.x) > deadZoneSize.x * 0.5f)
        {
            // Player is outside horizontal dead zone
            desiredPos.x = playerWorldPos.x - Mathf.Sign(distance.x) * deadZoneSize.x * 0.5f;
        }
        else
        {
            // Keep camera X position within dead zone
            desiredPos.x = currentPos.x;
        }
        
        if (Mathf.Abs(distance.y) > deadZoneSize.y * 0.5f)
        {
            // Player is outside vertical dead zone
            desiredPos.y = playerWorldPos.y - Mathf.Sign(distance.y) * deadZoneSize.y * 0.5f;
        }
        else
        {
            // Keep camera Y position within dead zone
            desiredPos.y = currentPos.y;
        }
        
        return desiredPos;
    }

    Vector3 SmoothCameraMovement(Vector3 desiredPos, float yDelta)
    {
        Vector3 currentPos = transform.position;
        
        // Determine vertical smooth time based on fall state
        float verticalSmoothTime = smoothTimeY;
        float yDistance = desiredPos.y - currentPos.y;
        
        // Fast catch-up when player falls below camera
        if (yDistance < -fallCatchupThreshold)
        {
            verticalSmoothTime = fallCatchupSpeed;
        }
        // Also catch up faster when player jumps high
        else if (yDistance > fallCatchupThreshold * 0.5f && yDelta > 0)
        {
            verticalSmoothTime = smoothTimeY * 0.7f; // Slightly faster when jumping up
        }
        
        // Smooth damp for both axes
        float newX = Mathf.SmoothDamp(currentPos.x, desiredPos.x, ref _currentVelocity.x, smoothTimeX);
        float newY = Mathf.SmoothDamp(currentPos.y, desiredPos.y, ref _currentVelocity.y, verticalSmoothTime);
        
        return new Vector3(newX, newY, desiredPos.z);
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw dead zone
        if (useDeadZone)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 center = transform.position;
            Gizmos.DrawCube(center, new Vector3(deadZoneSize.x, deadZoneSize.y, 0.1f));
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(deadZoneSize.x, deadZoneSize.y, 0.1f));
        }
        
        // Draw look-ahead indicator
        Gizmos.color = Color.cyan;
        Vector3 lookAheadPos = transform.position + new Vector3(_lookAheadPosX, 0, 0);
        Gizmos.DrawLine(transform.position, lookAheadPos);
        Gizmos.DrawSphere(lookAheadPos, 0.2f);
    }
}
