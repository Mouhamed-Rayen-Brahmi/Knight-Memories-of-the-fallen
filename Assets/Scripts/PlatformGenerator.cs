using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Platform Generation Settings")]
    [SerializeField] private int numberOfPlatforms = 20;
    [SerializeField] private float worldWidth = 50f;
    [SerializeField] private float worldHeight = 20f;
    [SerializeField] private float groundY = 0f;
    
    [Header("Platform Constraints - Metroidvania Style")]
    [SerializeField] private float maxJumpHeight = 3f;         // Max Y distance player can jump UP
    [SerializeField] private float maxJumpDistance = 5f;       // Max X distance player can jump across gaps
    [SerializeField] private float maxFallDistance = 8f;       // Max Y distance player can safely fall
    [SerializeField] private float minPlatformSpacing = 4f;    // Increased from 2f to 4f for better spacing
    [SerializeField] private Vector2 platformSize = new Vector2(3f, 0.5f); // Wider platforms for walking
    [SerializeField] private int platformBlocksPerPlatform = 3; // Number of blocks to spawn per platform
    
    [Header("Platform Prefabs")]
    [SerializeField] private List<GameObject> platformPrefabs = new List<GameObject>();
    [SerializeField] private GameObject groundPrefab;
    
    [Header("Player & Enemy Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private bool spawnPlayer = true;
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] [Range(0f, 1f)] private float enemySpawnChance = 0.3f; // 30% chance per platform
    [SerializeField] private int maxEnemiesPerPlatform = 2;
    [SerializeField] private int maxTotalEnemies = 15;
    
    [Header("Starting Position")]
    [SerializeField] private Vector2 startingPosition = new Vector2(0f, 2f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool generateOnStart = true;
    
    // Private variables
    private List<Vector2> placedPlatforms = new List<Vector2>();
    private List<GameObject> instantiatedPlatforms = new List<GameObject>();
    private List<GameObject> instantiatedEnemies = new List<GameObject>();
    private GameObject spawnedPlayer;
    private Queue<Vector2> platformQueue = new Queue<Vector2>();
    private List<GameObject> groundSegments = new List<GameObject>(); // Track ground segments
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateWorld();
        }
    }
    
    [ContextMenu("Generate New World")]
    public void GenerateWorld()
    {
        Debug.Log("=== STARTING WORLD GENERATION ===");
        
        // Validate settings before generation
        if (!ValidateGenerationSettings())
        {
            Debug.LogError("❌ World generation aborted due to invalid settings!");
            return;
        }
        
        // Clear existing platforms and entities
        ClearExistingWorld();
        
        // Reset tracking lists
        placedPlatforms.Clear();
        platformQueue.Clear();
        
        Debug.Log("🌍 Generating ground...");
        // Generate ground
        GenerateGround();
        
        Debug.Log("🏗️ Generating platforms...");
        // Start with initial platform
        Vector2 firstPlatform = startingPosition;
        PlacePlatform(firstPlatform);
        platformQueue.Enqueue(firstPlatform);
        
        // Generate remaining platforms
        GeneratePlatforms();
        
        Debug.Log("👤 Spawning player...");
        // Spawn player on ground
        if (spawnPlayer)
        {
            SpawnPlayer();
        }
        else
        {
            Debug.Log("Player spawning is disabled");
        }
        
        Debug.Log("👹 Spawning enemies...");
        // Spawn enemies on platforms
        if (spawnEnemies)
        {
            SpawnEnemies();
        }
        
        Debug.Log($"Generated {placedPlatforms.Count} platforms and {instantiatedEnemies.Count} enemies for Knightmare: Memories of the Fallen");
    }
    
    private void GenerateGround()
    {
        if (groundPrefab == null)
        {
            Debug.LogWarning("Ground prefab not assigned!");
            return;
        }
        
        // Clear previous ground segments
        groundSegments.Clear();
        
        // Create ground segments across the world width
        float groundSegmentWidth = 10f; // Adjust based on your ground prefab size
        int numGroundSegments = Mathf.CeilToInt(worldWidth / groundSegmentWidth);
        
        for (int i = 0; i < numGroundSegments; i++)
        {
            Vector3 groundPosition = new Vector3(
                (i * groundSegmentWidth) - (worldWidth * 0.5f),
                groundY,
                0f
            );
            
            GameObject ground = Instantiate(groundPrefab, groundPosition, Quaternion.identity, transform);
            ground.name = $"Ground_Segment_{i}";
            instantiatedPlatforms.Add(ground);
            
            // Track ground segments for player spawning
            groundSegments.Add(ground);
        }
        
        Debug.Log($"Generated {numGroundSegments} ground segments");
    }
    
    private void GeneratePlatforms()
    {
        int attemptCount = 0;
        int maxAttempts = numberOfPlatforms * 5; // Prevent infinite loops
        
        while (placedPlatforms.Count < numberOfPlatforms && attemptCount < maxAttempts)
        {
            attemptCount++;
            
            Vector2 candidatePosition = Vector2.zero;
            bool validPosition = false;
            
            // Try to place platform reachable from existing platforms
            if (platformQueue.Count > 0)
            {
                Vector2 basePosition = platformQueue.Dequeue();
                candidatePosition = GenerateReachablePosition(basePosition);
                validPosition = IsValidPlatformPosition(candidatePosition);
            }
            
            // If no valid reachable position found, try random placement
            if (!validPosition)
            {
                for (int i = 0; i < 10; i++) // Limited random attempts
                {
                    candidatePosition = GenerateRandomPosition();
                    if (IsValidPlatformPosition(candidatePosition) && IsReachableFromAnyPlatform(candidatePosition))
                    {
                        validPosition = true;
                        break;
                    }
                }
            }
            
            // Place platform if valid position found
            if (validPosition)
            {
                PlacePlatform(candidatePosition);
                
                // Add to queue for future platform generation (with probability)
                if (Random.value < 0.7f) // 70% chance to use this platform as base for others
                {
                    platformQueue.Enqueue(candidatePosition);
                }
            }
        }
        
        Debug.Log($"Platform generation completed. Placed: {placedPlatforms.Count}, Attempts: {attemptCount}");
    }
    
    private Vector2 GenerateReachablePosition(Vector2 basePosition)
    {
        // For Metroidvania-style games, generate positions based on jump mechanics
        Vector2 newPosition = basePosition;
        
        // Choose movement type based on probability
        float movementType = Random.value;
        
        if (movementType < 0.4f) // 40% - Horizontal jump (gap crossing)
        {
            float jumpDirection = Random.value < 0.5f ? -1f : 1f;
            // Better spacing: 3-4.5 units apart for comfortable jumps
            float jumpDistance = Random.Range(3f, maxJumpDistance * 0.9f);
            newPosition.x += jumpDirection * jumpDistance;
            
            // Small vertical variation for realistic jumps
            newPosition.y += Random.Range(-0.5f, 1f);
        }
        else if (movementType < 0.7f) // 30% - Upward jump (climbing)
        {
            // Climbing platforms should have comfortable vertical spacing
            newPosition.y += Random.Range(1.5f, maxJumpHeight * 0.9f);
            // Ensure horizontal spacing for approach
            newPosition.x += Random.Range(-3f, 3f);
        }
        else // 30% - Downward placement (falling/dropping)
        {
            newPosition.y -= Random.Range(2f, maxFallDistance * 0.8f);
            // Ensure horizontal spacing when dropping down
            newPosition.x += Random.Range(-3f, 3f);
        }
        
        // Clamp to world bounds
        newPosition.x = Mathf.Clamp(newPosition.x, -worldWidth * 0.5f, worldWidth * 0.5f);
        newPosition.y = Mathf.Clamp(newPosition.y, groundY + 1f, worldHeight);
        
        return newPosition;
    }
    
    private Vector2 GenerateRandomPosition()
    {
        float randomX = Random.Range(-worldWidth * 0.5f, worldWidth * 0.5f);
        float randomY = Random.Range(groundY + 1f, worldHeight);
        
        return new Vector2(randomX, randomY);
    }
    
    private bool IsValidPlatformPosition(Vector2 position)
    {
        // Check if position is within world bounds
        if (position.x < -worldWidth * 0.5f || position.x > worldWidth * 0.5f)
            return false;
        
        if (position.y <= groundY || position.y > worldHeight)
            return false;
        
        // Check distance from existing platforms
        foreach (Vector2 existingPlatform in placedPlatforms)
        {
            float distance = Vector2.Distance(position, existingPlatform);
            if (distance < minPlatformSpacing)
                return false;
            
            // Check for overlap considering the 3-block platform width
            float actualPlatformWidth = platformSize.x * platformBlocksPerPlatform;
            if (Mathf.Abs(position.x - existingPlatform.x) < actualPlatformWidth &&
                Mathf.Abs(position.y - existingPlatform.y) < platformSize.y)
                return false;
        }
        
        return true;
    }
    
    private bool IsReachableFromAnyPlatform(Vector2 targetPosition)
    {
        // Check if the target position is reachable from any existing platform using Metroidvania mechanics
        foreach (Vector2 existingPlatform in placedPlatforms)
        {
            if (IsMetroidvaniaReachable(existingPlatform, targetPosition))
            {
                return true;
            }
        }
        
        // Also check reachability from ground level
        Vector2 groundPosition = new Vector2(targetPosition.x, groundY);
        return IsMetroidvaniaReachable(groundPosition, targetPosition);
    }
    
    private bool IsMetroidvaniaReachable(Vector2 from, Vector2 to)
    {
        float horizontalDistance = Mathf.Abs(to.x - from.x);
        float verticalDistance = to.y - from.y; // Positive = going up, Negative = going down
        
        // Can jump horizontally across gaps
        if (Mathf.Abs(verticalDistance) <= 1f && horizontalDistance <= maxJumpDistance)
        {
            return true;
        }
        
        // Can jump upward
        if (verticalDistance > 0 && verticalDistance <= maxJumpHeight && horizontalDistance <= maxJumpDistance)
        {
            return true;
        }
        
        // Can fall/drop downward
        if (verticalDistance < 0 && Mathf.Abs(verticalDistance) <= maxFallDistance && horizontalDistance <= maxJumpDistance)
        {
            return true;
        }
        
        return false;
    }
    
    private void PlacePlatform(Vector2 position)
    {
        if (platformPrefabs.Count == 0)
        {
            Debug.LogWarning("No platform prefabs assigned!");
            return;
        }
        
        // Add to tracking list
        placedPlatforms.Add(position);
        
        // Choose random platform prefab
        GameObject prefabToUse = platformPrefabs[Random.Range(0, platformPrefabs.Count)];
        
        // Calculate the actual platform size from the prefab
        float blockWidth = 1f; // Default, will be updated from prefab
        SpriteRenderer prefabRenderer = prefabToUse.GetComponent<SpriteRenderer>();
        if (prefabRenderer != null)
        {
            blockWidth = prefabRenderer.bounds.size.x;
        }
        
        // Calculate starting position for multiple blocks
        float totalWidth = blockWidth * platformBlocksPerPlatform;
        float startX = position.x - (totalWidth / 2f) + (blockWidth / 2f);
        
        // Instantiate multiple blocks to form one platform
        for (int i = 0; i < platformBlocksPerPlatform; i++)
        {
            Vector3 blockPosition = new Vector3(startX + (i * blockWidth), position.y, 0f);
            GameObject platformBlock = Instantiate(prefabToUse, blockPosition, Quaternion.identity, transform);
            platformBlock.name = $"Platform_{placedPlatforms.Count}_Block_{i + 1}";
            
            instantiatedPlatforms.Add(platformBlock);
        }
    }
    
    private void ClearExistingWorld()
    {
        // Destroy all previously instantiated platforms
        foreach (GameObject platform in instantiatedPlatforms)
        {
            if (platform != null)
            {
                if (Application.isPlaying)
                    Destroy(platform);
                else
                    DestroyImmediate(platform);
            }
        }
        
        // Destroy all previously instantiated enemies
        foreach (GameObject enemy in instantiatedEnemies)
        {
            if (enemy != null)
            {
                if (Application.isPlaying)
                    Destroy(enemy);
                else
                    DestroyImmediate(enemy);
            }
        }
        
        // Destroy previously spawned player
        if (spawnedPlayer != null)
        {
            if (Application.isPlaying)
                Destroy(spawnedPlayer);
            else
                DestroyImmediate(spawnedPlayer);
            spawnedPlayer = null;
        }
        
        instantiatedPlatforms.Clear();
        instantiatedEnemies.Clear();
    }
    
    private void SpawnPlayer()
    {
        Debug.Log("=== PLAYER SPAWNING DEBUG ===");
        Debug.Log($"spawnPlayer = {spawnPlayer}");
        Debug.Log($"playerPrefab = {(playerPrefab != null ? playerPrefab.name : "NULL")}");
        
        if (!spawnPlayer)
        {
            Debug.Log("Player spawning is disabled. Skipping.");
            return;
        }
        
        // Strategy 1: Try to use assigned player prefab
        if (playerPrefab != null)
        {
            if (TrySpawnPlayerWithPrefab())
            {
                Debug.Log("✅ Successfully spawned player using assigned prefab");
                return;
            }
        }
        
        // Strategy 2: Find existing player in scene
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            if (TryRepositionExistingPlayer(existingPlayer))
            {
                Debug.Log("✅ Successfully repositioned existing player");
                return;
            }
        }
        
        // Strategy 3: Try to find player prefab in Resources folder
        if (TrySpawnPlayerFromResources())
        {
            Debug.Log("✅ Successfully spawned player from Resources");
            return;
        }
        
        // Strategy 4: Create a simple player GameObject as fallback
        if (TryCreateFallbackPlayer())
        {
            Debug.Log("⚠️ Created fallback player GameObject");
            return;
        }
        
        Debug.LogError("❌ FAILED to spawn player with all strategies!");
    }
    
    private bool TrySpawnPlayerWithPrefab()
    {
        try
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition();
            Debug.Log($"Attempting to spawn player at position: {spawnPosition}");
            
            spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            
            if (spawnedPlayer != null)
            {
                SetupSpawnedPlayer(spawnedPlayer, "SpawnedPlayer");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to instantiate player prefab: {e.Message}");
        }
        
        return false;
    }
    
    private bool TryRepositionExistingPlayer(GameObject existingPlayer)
    {
        try
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition();
            Debug.Log($"Moving existing player {existingPlayer.name} to position: {spawnPosition}");
            
            existingPlayer.transform.position = spawnPosition;
            spawnedPlayer = existingPlayer;
            
            // Ensure proper setup
            SetupSpawnedPlayer(spawnedPlayer, null); // Don't change name
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to reposition existing player: {e.Message}");
        }
        
        return false;
    }
    
    private bool TrySpawnPlayerFromResources()
    {
        try
        {
            // Try common player prefab names in Resources folder
            string[] commonNames = { "Player", "PlayerPrefab", "Wizard", "Character" };
            
            foreach (string name in commonNames)
            {
                GameObject playerResource = Resources.Load<GameObject>(name);
                if (playerResource != null)
                {
                    Vector3 spawnPosition = GetPlayerSpawnPosition();
                    Debug.Log($"Found player resource: {name}, spawning at {spawnPosition}");
                    
                    spawnedPlayer = Instantiate(playerResource, spawnPosition, Quaternion.identity);
                    
                    if (spawnedPlayer != null)
                    {
                        SetupSpawnedPlayer(spawnedPlayer, "ResourcePlayer");
                        return true;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn player from Resources: {e.Message}");
        }
        
        return false;
    }
    
    private bool TryCreateFallbackPlayer()
    {
        try
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition();
            Debug.Log($"Creating fallback player at position: {spawnPosition}");
            
            // Create a simple player GameObject
            spawnedPlayer = new GameObject("FallbackPlayer");
            spawnedPlayer.transform.position = spawnPosition;
            
            // Add basic components
            spawnedPlayer.tag = "Player";
            spawnedPlayer.layer = 0; // Default layer
            
            // Add SpriteRenderer for visibility
            SpriteRenderer sr = spawnedPlayer.AddComponent<SpriteRenderer>();
            sr.color = Color.blue; // Make it visible
            sr.sortingOrder = 10;
            
            // Add collider
            BoxCollider2D collider = spawnedPlayer.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1f);
            
            // Add rigidbody
            Rigidbody2D rb = spawnedPlayer.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            
            SetupSpawnedPlayer(spawnedPlayer, null); // Don't change name
            
            Debug.LogWarning("Created a basic fallback player. Please assign a proper player prefab!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create fallback player: {e.Message}");
        }
        
        return false;
    }
    
    private Vector3 GetPlayerSpawnPosition()
    {
        // Use actual ground segment positions if available
        if (groundSegments.Count > 0)
        {
            // Choose a random or specific ground segment
            GameObject spawnGroundSegment = groundSegments[groundSegments.Count / 2]; // Middle ground segment
            
            if (spawnGroundSegment != null)
            {
                // Get the top of the ground segment
                Renderer renderer = spawnGroundSegment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float groundTop = renderer.bounds.max.y;
                    Vector3 groundCenter = spawnGroundSegment.transform.position;
                    return new Vector3(groundCenter.x, groundTop + 1f, 0f); // Spawn 1 unit above ground
                }
                else
                {
                    // Fallback to ground segment position + offset
                    return spawnGroundSegment.transform.position + Vector3.up * 2f;
                }
            }
        }
        
        // Fallback positions if no ground segments available
        Vector3[] spawnPositions = {
            new Vector3(0f, groundY + 2f, 0f),           // On ground, center
            new Vector3(startingPosition.x, startingPosition.y + 1f, 0f), // Above starting platform
            new Vector3(0f, groundY + 5f, 0f),           // Higher up
            new Vector3(-5f, groundY + 2f, 0f),          // Left side
            new Vector3(5f, groundY + 2f, 0f),           // Right side
        };
        
        Debug.LogWarning("No ground segments found, using fallback spawn position");
        return spawnPositions[0];
    }
    
    private void SetupSpawnedPlayer(GameObject player, string newName)
    {
        if (player == null) return;
        
        // Set name if provided
        if (!string.IsNullOrEmpty(newName))
        {
            player.name = newName;
        }
        
        // Ensure proper tag
        if (!player.CompareTag("Player"))
        {
            player.tag = "Player";
            Debug.Log($"Set player tag to 'Player' for {player.name}");
        }
        
        // Ensure player has required components
        if (player.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            Debug.Log($"Added Rigidbody2D to {player.name}");
        }
        
        if (player.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1f);
            Debug.Log($"Added BoxCollider2D to {player.name}");
        }
        
        // Set as child of this generator for organization
        player.transform.SetParent(transform);
        
        Debug.Log($"Player setup complete: {player.name} at position {player.transform.position}");
        
        // ✅ NOTIFY CAMERA ABOUT SPAWNED PLAYER
        NotifyCameraOfPlayer(player);
    }
    
    // New method to notify camera about spawned player
    private void NotifyCameraOfPlayer(GameObject player)
    {
        // Find CameraFollow script in the scene
        CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();
        
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(player.transform);
            Debug.Log($"[PlatformGenerator] Notified camera to follow player: {player.name}");
        }
        else
        {
            Debug.LogWarning("[PlatformGenerator] Could not find CameraFollow script in scene!");
        }
    }
    
    private void SpawnEnemies()
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned! Cannot spawn enemies.");
            return;
        }
        
        int totalEnemiesSpawned = 0;
        
        // Create a list of suitable platforms for enemy spawning
        List<Vector2> suitablePlatforms = new List<Vector2>();
        
        // Add all platforms except the starting one (give player some space)
        foreach (Vector2 platform in placedPlatforms)
        {
            float distanceFromStart = Vector2.Distance(platform, startingPosition);
            if (distanceFromStart > 3f) // Don't spawn enemies too close to start
            {
                suitablePlatforms.Add(platform);
            }
        }
        
        // Spawn enemies on platforms
        foreach (Vector2 platformPos in suitablePlatforms)
        {
            if (totalEnemiesSpawned >= maxTotalEnemies)
                break;
            
            // Check if we should spawn an enemy on this platform
            if (Random.value <= enemySpawnChance)
            {
                int enemiesToSpawn = Random.Range(1, maxEnemiesPerPlatform + 1);
                
                for (int i = 0; i < enemiesToSpawn && totalEnemiesSpawned < maxTotalEnemies; i++)
                {
                    SpawnEnemyOnPlatform(platformPos);
                    totalEnemiesSpawned++;
                }
            }
        }
        
        Debug.Log($"Spawned {totalEnemiesSpawned} enemies across {suitablePlatforms.Count} platforms");
    }
    
    private void SpawnEnemyOnPlatform(Vector2 platformPosition)
    {
        // Choose random enemy prefab
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        
        // Add some random offset on the platform
        float xOffset = Random.Range(-platformSize.x * 0.3f, platformSize.x * 0.3f);
        Vector3 enemySpawnPosition = new Vector3(
            platformPosition.x + xOffset, 
            platformPosition.y + platformSize.y * 0.5f + 0.5f, // Spawn above platform
            0f
        );
        
        // Instantiate enemy
        GameObject enemyInstance = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity, transform);
        enemyInstance.name = $"Enemy_{instantiatedEnemies.Count + 1}";
        
        // Ensure enemy has proper physics setup to allow falling
        Rigidbody2D rb = enemyInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Don't override gravity if already set, just ensure it's enabled
            if (rb.gravityScale == 0)
            {
                rb.gravityScale = 1f;
            }
            // Ensure rotation is frozen but allow falling
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Debug.Log($"Configured Rigidbody2D for {enemyInstance.name} - Gravity: {rb.gravityScale}, can fall from platforms");
        }
        else
        {
            // Add Rigidbody2D if missing
            rb = enemyInstance.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Debug.Log($"Added Rigidbody2D to {enemyInstance.name} - Enemy can now fall from platforms");
        }
        
        instantiatedEnemies.Add(enemyInstance);
    }
    
    // Public utility methods
    public List<Vector2> GetPlatformPositions()
    {
        return new List<Vector2>(placedPlatforms);
    }
    
    public Vector2 GetRandomPlatformPosition()
    {
        if (placedPlatforms.Count == 0)
            return startingPosition;
        
        return placedPlatforms[Random.Range(0, placedPlatforms.Count)];
    }
    
    public Vector2 GetNearestPlatform(Vector2 position)
    {
        if (placedPlatforms.Count == 0)
            return startingPosition;
        
        Vector2 nearest = placedPlatforms[0];
        float nearestDistance = Vector2.Distance(position, nearest);
        
        foreach (Vector2 platform in placedPlatforms)
        {
            float distance = Vector2.Distance(position, platform);
            if (distance < nearestDistance)
            {
                nearest = platform;
                nearestDistance = distance;
            }
        }
        
        return nearest;
    }
    
    // Get all enemy positions
    public List<Vector3> GetEnemyPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (GameObject enemy in instantiatedEnemies)
        {
            if (enemy != null)
            {
                positions.Add(enemy.transform.position);
            }
        }
        return positions;
    }
    
    // Get total enemy count
    public int GetEnemyCount()
    {
        return instantiatedEnemies.Count;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw world bounds
        Gizmos.color = Color.yellow;
        Vector3 worldCenter = new Vector3(0, worldHeight * 0.5f, 0);
        Vector3 worldSize = new Vector3(worldWidth, worldHeight, 1);
        Gizmos.DrawWireCube(worldCenter, worldSize);
        
        // Draw ground line
        Gizmos.color = Color.green;
        Vector3 groundStart = new Vector3(-worldWidth * 0.5f, groundY, 0);
        Vector3 groundEnd = new Vector3(worldWidth * 0.5f, groundY, 0);
        Gizmos.DrawLine(groundStart, groundEnd);
        
        // Draw placed platforms
        Gizmos.color = Color.blue;
        foreach (Vector2 platform in placedPlatforms)
        {
            Vector3 platformPos = new Vector3(platform.x, platform.y, 0);
            Gizmos.DrawWireCube(platformPos, new Vector3(platformSize.x, platformSize.y, 0.1f));
        }
        
        // Draw starting position
        Gizmos.color = Color.red;
        Vector3 startPos = new Vector3(startingPosition.x, startingPosition.y, 0);
        Gizmos.DrawWireSphere(startPos, 0.5f);
        
        // Draw player spawn position (on ground)
        Gizmos.color = Color.blue;
        Vector3 playerSpawnPos = GetPlayerSpawnPosition();
        Gizmos.DrawWireCube(playerSpawnPos, Vector3.one);
        
        // Draw enemy positions
        Gizmos.color = Color.red;
        foreach (GameObject enemy in instantiatedEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, 0.3f);
            }
        }
        
        // Draw reachability radius from starting position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(startPos, new Vector3(maxJumpDistance * 2, maxJumpHeight * 2, 0.1f));
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw connections between reachable platforms
        Gizmos.color = Color.magenta;
        for (int i = 0; i < placedPlatforms.Count; i++)
        {
            Vector2 currentPlatform = placedPlatforms[i];
            
            for (int j = i + 1; j < placedPlatforms.Count; j++)
            {
                Vector2 otherPlatform = placedPlatforms[j];
                
                if (IsMetroidvaniaReachable(currentPlatform, otherPlatform))
                {
                    Vector3 start = new Vector3(currentPlatform.x, currentPlatform.y, 0);
                    Vector3 end = new Vector3(otherPlatform.x, otherPlatform.y, 0);
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
    
    // Validation method for inspector
    void OnValidate()
    {
        numberOfPlatforms = Mathf.Max(1, numberOfPlatforms);
        worldWidth = Mathf.Max(5f, worldWidth);
        worldHeight = Mathf.Max(5f, worldHeight);
        maxJumpHeight = Mathf.Max(1f, maxJumpHeight);
        maxJumpDistance = Mathf.Max(1f, maxJumpDistance);
        maxFallDistance = Mathf.Max(1f, maxFallDistance);
        minPlatformSpacing = Mathf.Max(0.5f, minPlatformSpacing);
        platformSize.x = Mathf.Max(0.1f, platformSize.x);
        platformSize.y = Mathf.Max(0.1f, platformSize.y);
        
        // Enemy spawning validation
        enemySpawnChance = Mathf.Clamp01(enemySpawnChance);
        maxEnemiesPerPlatform = Mathf.Max(1, maxEnemiesPerPlatform);
        maxTotalEnemies = Mathf.Max(1, maxTotalEnemies);
    }
    
    // Comprehensive validation for generation settings
    private bool ValidateGenerationSettings()
    {
        bool isValid = true;
        
        Debug.Log("🔍 Validating generation settings...");
        
        // Check platform prefabs
        if (platformPrefabs.Count == 0)
        {
            Debug.LogError("❌ No platform prefabs assigned! Please assign at least one platform prefab.");
            isValid = false;
        }
        else
        {
            Debug.Log($"✅ {platformPrefabs.Count} platform prefab(s) assigned");
        }
        
        // Check ground prefab
        if (groundPrefab == null)
        {
            Debug.LogWarning("⚠️ No ground prefab assigned. Ground generation will be skipped.");
        }
        else
        {
            Debug.Log("✅ Ground prefab assigned");
        }
        
        // Check player settings
        if (spawnPlayer && playerPrefab == null)
        {
            Debug.LogWarning("⚠️ Player spawning enabled but no player prefab assigned. Will try fallback methods.");
        }
        else if (spawnPlayer)
        {
            Debug.Log("✅ Player prefab assigned for spawning");
        }
        
        // Check enemy settings
        if (spawnEnemies && enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("⚠️ Enemy spawning enabled but no enemy prefabs assigned. Enemy spawning will be skipped.");
        }
        else if (spawnEnemies)
        {
            Debug.Log($"✅ {enemyPrefabs.Count} enemy prefab(s) assigned");
        }
        
        // Check world size
        if (numberOfPlatforms < 1)
        {
            Debug.LogError("❌ Number of platforms must be at least 1!");
            isValid = false;
        }
        
        if (worldWidth < 5f || worldHeight < 5f)
        {
            Debug.LogError("❌ World dimensions too small! Minimum 5x5 units required.");
            isValid = false;
        }
        
        return isValid;
    }
    
    // Context menu methods for easier testing
    [ContextMenu("Clear World")]
    public void ClearWorldOnly()
    {
        Debug.Log("🧹 Clearing existing world...");
        ClearExistingWorld();
        Debug.Log("✅ World cleared successfully!");
    }
    
    [ContextMenu("Spawn Player Only")]
    public void SpawnPlayerOnly()
    {
        Debug.Log("👤 Attempting to spawn player only...");
        SpawnPlayer();
    }
    
    [ContextMenu("Spawn Enemies Only")]
    public void SpawnEnemiesOnly()
    {
        Debug.Log("👹 Attempting to spawn enemies only...");
        SpawnEnemies();
    }
    
    [ContextMenu("Validate Settings")]
    public void ValidateSettingsManual()
    {
        Debug.Log("🔍 Manual settings validation requested...");
        bool valid = ValidateGenerationSettings();
        
        if (valid)
        {
            Debug.Log("✅ All settings are valid! Ready for world generation.");
        }
        else
        {
            Debug.Log("❌ Some settings need attention before generation.");
        }
    }
    
    [ContextMenu("Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== PLATFORM GENERATOR WORKS ===");
       
    }
}