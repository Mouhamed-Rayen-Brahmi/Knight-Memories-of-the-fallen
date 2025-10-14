using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
}

public class EnemyCode : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int bodyDamage = 10;        // Damage when player touches the enemy body
    public int swordDamage = 20;       // Damage when player touches the sword
    public float invincibilityTime = 0.5f;
    
    [Header("Attack Settings")]
    public float attackInterval = 2f;  // Minimum time between attacks
    public float attackRange = 1.5f;   // Distance at which enemy will attack
    public float detectionRange = 10f; // Distance at which enemy will detect player
    public Transform attackPoint;      // Position of the sword tip for collision detection
    public LayerMask playerLayer;      // Layer containing the player
    
    [Header("Death Settings")]
    public float deathDelay = 3f;
    public AnimationClip deathEffect;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public bool canMove = true;
    
    [Header("Colliders")]
    public BoxCollider2D bodyCollider;  // Main body collider
    public BoxCollider2D swordCollider; // Sword collider for specific sword damage
    
    // Animation state names directly from the animator
    private const string STATE_IDLE = "Idle";
    private const string STATE_WALK = "Walk";
    private const string STATE_ATTACK = "Attack with sword";
    private const string STATE_HURT = "Get Hit";
    private const string STATE_DEATH = "Death Skeleton with sword";
    private const string STATE_ROTTEN = "Death Skeleton rotten with sword";
    
    // Component references
    private Transform player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    
    // State tracking - made public for sword collider script access
    [HideInInspector] public bool isDead = false;
    private bool isInvincible = false;
    [HideInInspector] public bool isAttacking = false;
    private bool facingRight = true;
    
    // Timers
    private float lastAttackTime;
    private float invincibilityTimer = 0f;
    
    // Position tracking
    private Vector3 startPosition;
    private float groundY;
    
    // Animation
    private string currentState;
    
    // Damage contact tracking
    private Dictionary<int, float> playerContactCooldown = new Dictionary<int, float>();
    private float playerContactDamageCooldown = 1.0f;

    private void Awake()
    {
        // Setup colliders if not already set in inspector
        if (bodyCollider == null)
            bodyCollider = GetComponent<BoxCollider2D>();
            
        // Create sword collider if not set
        if (swordCollider == null && attackPoint != null)
        {
            GameObject swordObj = attackPoint.gameObject;
            swordCollider = swordObj.GetComponent<BoxCollider2D>();
            if (swordCollider == null)
            {
                swordCollider = swordObj.AddComponent<BoxCollider2D>();
                swordCollider.size = new Vector2(1.2f, 0.5f);
                swordCollider.offset = new Vector2(0.5f, 0);
                swordCollider.isTrigger = true;
            }
        }
    }
    
    void Start()
    {
        // Check if this enemy was previously marked as dead
        if (isDead)
        {
            Debug.Log("Enemy was already dead - destroying immediately");
            Destroy(gameObject);
            return;
        }
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Store initial position
        startPosition = transform.position;
        groundY = transform.position.y;
        
        // Find player reference
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        // Get component references
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        lastAttackTime = Time.time;
        
        // Setup rigidbody
        if (rb != null)
        {
            rb.gravityScale = 5f;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Setup sword collider
        SetupSwordCollider();
        
        // Set initial animation state
        ChangeAnimationState(STATE_IDLE);
        
        // Disable sword collider by default
        if (swordCollider != null)
        {
            swordCollider.enabled = false;
        }
        
        // Add tag for easier identification
        gameObject.tag = "Enemy";
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Handle invincibility
        HandleInvincibility();
        
        // Make sure we have a player reference
        if (player == null)
        {
            TryFindPlayer();
            if (player == null) return;
        }
        
        // Ground checking and position fixing
        FixPositionAndRotation();
        
        // Main AI logic
        if (!isAttacking)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                FacePlayer();
                
                if (distanceToPlayer <= attackRange)
                {
                    StopMovement();
                    
                    // Attack when cooldown is over
                    if (Time.time >= lastAttackTime + attackInterval)
                    {
                        Attack();
                        lastAttackTime = Time.time;
                    }
                }
                else if (canMove)
                {
                    // Move towards player when in detection range but outside attack range
                    MoveTowardsPlayer();
                }
            }
            else
            {
                // Outside detection range, return to idle
                StopMovement();
                ChangeAnimationState(STATE_IDLE);
            }
        }
    }
    
    private void SetupSwordCollider()
    {
        if (attackPoint == null)
        {
            // Create attack point if it doesn't exist
            GameObject swordObj = new GameObject("SwordCollider");
            swordObj.transform.parent = transform;
            swordObj.transform.localPosition = new Vector3(1.2f, 0.3f, 0); // Position in front of enemy
            
            // Tag for identification
            swordObj.tag = "EnemySword";
            
            // Create collider
            swordCollider = swordObj.AddComponent<BoxCollider2D>();
            swordCollider.size = new Vector2(1.2f, 0.5f);
            swordCollider.isTrigger = true;
            swordCollider.enabled = false;
            
            // Add sword controller script
            swordObj.AddComponent<EnemySwordCollider>();
            
            // Store reference
            attackPoint = swordObj.transform;
        }
        else
        {
            // Setup existing attack point
            GameObject swordObj = attackPoint.gameObject;
            swordObj.tag = "EnemySword";
            
            // Setup collider
            swordCollider = swordObj.GetComponent<BoxCollider2D>();
            if (swordCollider == null)
            {
                swordCollider = swordObj.AddComponent<BoxCollider2D>();
                swordCollider.size = new Vector2(1.2f, 0.5f);
                swordCollider.isTrigger = true;
                swordCollider.enabled = false;
            }
            
            // Add sword controller if needed
            if (swordObj.GetComponent<EnemySwordCollider>() == null)
            {
                swordObj.AddComponent<EnemySwordCollider>();
            }
        }
    }
    
    private void TryFindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    private void HandleInvincibility()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            
            // Visual feedback for invincibility
            if (spriteRenderer != null)
            {
                float flashValue = Mathf.PingPong(Time.time * 20, 1);
                spriteRenderer.color = new Color(1, flashValue, flashValue, 1);
            }
            
            // Check if invincibility is over
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
    }
    
    private void FixPositionAndRotation()
    {
        // Fix rotation issues
        Vector3 currentRotation = transform.rotation.eulerAngles;
        if (currentRotation.z != 0)
        {
            transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        }
        
        // Prevent falling through the ground
        if (transform.position.y < groundY - 1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
        
        // Prevent teleporting
        if (rb != null && rb.linearVelocity.magnitude > moveSpeed * 2)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, moveSpeed);
        }
    }
    
    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    
    void FacePlayer()
    {
        if (player == null) return;
        
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != facingRight)
        {
            Flip();
        }
    }
    
    void Flip()
    {
        // Remember current position
        Vector3 currentPosition = transform.position;
        
        // Switch facing direction
        facingRight = !facingRight;
        
        // Rotate the character instead of scaling
        transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
        
        // Make sure Z rotation is 0
        Vector3 currentRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        
        // Update sword position
        if (attackPoint != null)
        {
            Vector3 attackPos = attackPoint.localPosition;
            attackPos.x = Mathf.Abs(attackPos.x) * (facingRight ? 1 : -1);
            attackPoint.localPosition = attackPos;
        }
        
        // Prevent position change from flipping
        transform.position = currentPosition;
    }
    
    void MoveTowardsPlayer()
    {
        if (player == null) return;
        
        // Store starting position to detect teleporting
        Vector3 startPosition = transform.position;
        
        // Direction to player (only X axis)
        Vector2 direction = new Vector2(
            player.position.x - transform.position.x,
            0
        ).normalized;
        
        // Move with physics
        if (rb != null)
        {
            float desiredVelocity = Mathf.Clamp(direction.x * moveSpeed, -moveSpeed, moveSpeed);
            float currentYVelocity = rb.linearVelocity.y;
            float smoothedXVelocity = Mathf.Lerp(rb.linearVelocity.x, desiredVelocity, 0.3f);
            rb.linearVelocity = new Vector2(smoothedXVelocity, currentYVelocity);
        }
        else
        {
            // Fallback direct transform movement
            float maxMovePerFrame = moveSpeed * Time.deltaTime;
            float actualMove = Mathf.Clamp(direction.x * moveSpeed * Time.deltaTime, -maxMovePerFrame, maxMovePerFrame);
            transform.position += new Vector3(actualMove, 0, 0);
        }
        
        // Play walking animation
        ChangeAnimationState(STATE_WALK);
    }
    
    void Attack()
    {
        if (isDead || player == null) return;
        
        isAttacking = true;
        
        // Change animation
        ChangeAnimationState(STATE_ATTACK);
        
        // Trigger attack sequence
        StartCoroutine(AttackSequence());
    }
    
    IEnumerator AttackSequence()
    {
        // Wind-up phase
        yield return new WaitForSeconds(0.3f);
        
        if (isDead || player == null)
        {
            isAttacking = false;
            yield break;
        }
        
        // Enable sword collider during active attack frames
        if (swordCollider != null)
        {
            swordCollider.enabled = true;
            Debug.Log("Sword collider ENABLED - ready to hit player");
            
            // Make sure the sword knows it's active
            EnemySwordCollider swordScript = null;
            if (attackPoint != null)
            {
                swordScript = attackPoint.GetComponent<EnemySwordCollider>();
                if (swordScript != null)
                {
                    Debug.Log("Found EnemySwordCollider script on attack point");
                }
            }
        }
        else
        {
            Debug.LogError("No sword collider reference found during attack!");
        }
        
        // Active attack duration
        yield return new WaitForSeconds(0.4f);
        
        // Attempt to directly check for player collision at attack point
        if (attackPoint != null && player != null)
        {
            float distToPlayer = Vector2.Distance(attackPoint.position, player.position);
            Debug.Log("Distance from sword to player: " + distToPlayer);
            
            if (distToPlayer < 2.0f)
            {
                // Direct hit detection
                Debug.Log("Player in sword range - attempting direct hit");
                
                // Try to find player
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    IDamageable playerDamageable = playerObj.GetComponent<IDamageable>();
                    if (playerDamageable != null)
                    {
                        Debug.Log("Direct hit detected - applying damage");
                        playerDamageable.TakeDamage(swordDamage);
                    }
                }
            }
        }
        
        // Disable sword collider
        if (swordCollider != null)
        {
            swordCollider.enabled = false;
            Debug.Log("Sword collider DISABLED");
        }
        
        // Recovery phase
        yield return new WaitForSeconds(0.3f);
        
        // Reset attack state
        isAttacking = false;
        
        // Return to idle
        ChangeAnimationState(STATE_IDLE);
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible) return;
        
        currentHealth -= damage;
        
        // Activate invincibility
        isInvincible = true;
        invincibilityTimer = invincibilityTime;
        
        // Stop ongoing actions
        StopAllCoroutines();
        isAttacking = false;
        
        // Visual feedback
        StartCoroutine(FlashRed());
        
        // Play hurt animation
        ChangeAnimationState(STATE_HURT);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Return to idle after hurt animation
            StartCoroutine(ReturnToIdleAfterHurt());
        }
    }
    
    // Method for sword collider to use
    public void DamageSwordTarget(GameObject target)
    {
        if (isDead)
        {
            Debug.Log("Enemy is dead, can't damage player");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError("DamageSwordTarget called with null target!");
            return;
        }
        
        Debug.Log("DamageSwordTarget called on " + target.name);
        
        // Check if this player can be damaged (cooldown)
        int playerID = target.GetInstanceID();
        bool onCooldown = playerContactCooldown.ContainsKey(playerID) && 
                         Time.time < playerContactCooldown[playerID];
        
        if (onCooldown)
        {
            Debug.Log("Player damage on cooldown - skipping");
            return;
        }
        
        // Try to apply damage using multiple methods
        bool damageApplied = false;
        
        // Method 1: Direct component
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log("Applying sword damage directly to IDamageable: " + swordDamage);
            damageable.TakeDamage(swordDamage);
            damageApplied = true;
        }
        
        // Method 2: Try parent object
        if (!damageApplied && target.transform.parent != null)
        {
            damageable = target.transform.parent.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log("Applying sword damage to parent's IDamageable: " + swordDamage);
                damageable.TakeDamage(swordDamage);
                damageApplied = true;
            }
        }
        
        // Method 3: Try specific player controller
        if (!damageApplied)
        {
            ClearSky.SimplePlayerController playerController = target.GetComponent<ClearSky.SimplePlayerController>();
            if (playerController == null && target.transform.parent != null)
            {
                playerController = target.transform.parent.GetComponent<ClearSky.SimplePlayerController>();
            }
            
            if (playerController != null)
            {
                Debug.Log("Found player controller - applying damage: " + swordDamage);
                playerController.TakeDamage(swordDamage);
                damageApplied = true;
            }
        }
        
        if (damageApplied)
        {
            Debug.Log("Successfully applied sword damage to player!");
            
            // Set cooldown for this player
            playerContactCooldown[playerID] = Time.time + playerContactDamageCooldown;
            
            // Apply knockback
            Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
            if (playerRb == null && target.transform.parent != null)
            {
                playerRb = target.transform.parent.GetComponent<Rigidbody2D>();
            }
            
            if (playerRb != null)
            {
                Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
                playerRb.AddForce(knockbackDir * 10f, ForceMode2D.Impulse);
            }
        }
        else
        {
            // Last resort - try to find the player GameObject directly
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                IDamageable playerDamageable = playerObj.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    Debug.Log("Last resort - applying damage to found player: " + swordDamage);
                    playerDamageable.TakeDamage(swordDamage);
                    
                    // Set cooldown
                    playerContactCooldown[playerObj.GetInstanceID()] = Time.time + playerContactDamageCooldown;
                }
            }
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Enemy died - destroying in " + deathDelay + " seconds");
        
        // Stop all activities
        StopAllCoroutines();
        
        // Play death animation
        ChangeAnimationState(STATE_DEATH);
        
        // Disable physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.simulated = false;
        }
        
        // Disable colliders
        DisableAllColliders();
        
        // Play death effect animation if available
        if (deathEffect != null)
        {
            // Create a GameObject for the death effect at the enemy's position
            GameObject deathEffectObj = new GameObject("DeathEffect");
            deathEffectObj.transform.position = transform.position;
            deathEffectObj.transform.rotation = transform.rotation;
            
            // Add sprite renderer component
            SpriteRenderer newRenderer = deathEffectObj.AddComponent<SpriteRenderer>();
            
            // Copy sprite renderer settings from the enemy if available
            SpriteRenderer enemyRenderer = GetComponent<SpriteRenderer>();
            if (enemyRenderer != null)
            {
                newRenderer.sortingLayerName = enemyRenderer.sortingLayerName;
                newRenderer.sortingOrder = enemyRenderer.sortingOrder + 1; // Render on top
                newRenderer.material = enemyRenderer.material;
            }
            
            // Add Animation component and play the death animation
            Animation animationComponent = deathEffectObj.AddComponent<Animation>();
            animationComponent.AddClip(deathEffect, "DeathEffect");
            animationComponent.clip = deathEffect;
            animationComponent.playAutomatically = true;
            animationComponent.wrapMode = WrapMode.Once;
            
            // Play the animation
            animationComponent.Play("DeathEffect");
            
            // Destroy the death effect after the animation finishes
            float animationLength = deathEffect.length;
            Destroy(deathEffectObj, animationLength + 0.1f);
        }
        
        // Play rotten animation after death, then destroy
        StartCoroutine(DeathSequence());
        
        // Make sure this object can't be reactivated
        gameObject.SetActive(true); // Ensure it's active for the animation
        
        // Cancel any previous destroy calls to avoid conflicts
        CancelInvoke("DestroyEnemy");
        
        // Use Invoke to ensure destruction happens even if coroutines are interrupted
        Invoke("DestroyEnemy", deathDelay);
    }
    
    // Separate method to ensure destruction happens
    private void DestroyEnemy()
    {
        Debug.Log("Permanently destroying enemy: " + gameObject.name);
        Destroy(gameObject);
    }
    
    private void DisableAllColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }
    
    IEnumerator PlayRottenAnimation()
    {
        // Wait for death animation to complete
        yield return new WaitForSeconds(0.9f);
        
        if (animator != null)
        {
            ChangeAnimationState(STATE_ROTTEN);
        }
    }
    
    IEnumerator DeathSequence()
    {
        // Wait for death animation to complete
        yield return new WaitForSeconds(0.9f);
        
        // Play rotten animation
        if (animator != null)
        {
            ChangeAnimationState(STATE_ROTTEN);
        }
        
        // Wait for rotten animation to play for a bit
        yield return new WaitForSeconds(deathDelay - 1.0f);
        
        // Double check destruction in case Invoke fails
        if (gameObject != null)
        {
            Debug.Log("Ensuring enemy destruction via coroutine");
            Destroy(gameObject);
        }
    }
    
    IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }
    
    IEnumerator ReturnToIdleAfterHurt()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (!isDead)
        {
            ChangeAnimationState(STATE_IDLE);
        }
    }
    
    // Handle object re-activation
    private void OnEnable()
    {
        // If this enemy was previously killed, make sure it stays dead
        if (isDead)
        {
            Debug.Log("Preventing re-activation of dead enemy");
            
            // Ensure destruction
            DestroyEnemy();
        }
    }
    
    private void ChangeAnimationState(string newState)
    {
        // Don't change if already in this state
        if (currentState == newState) return;
        
        // Safety check
        if (animator == null || string.IsNullOrEmpty(newState)) return;
        
        try
        {
            // Play the animation
            animator.Play(newState);
            
            // Store current state
            currentState = newState;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error changing animation state: " + e.Message);
        }
    }
    
    // Handle collisions for body damage
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if this player can be damaged (cooldown)
            int playerID = collision.gameObject.GetInstanceID();
            if (!playerContactCooldown.ContainsKey(playerID) || 
                Time.time >= playerContactCooldown[playerID])
            {
                // Apply body damage to player
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(bodyDamage);
                    Debug.Log("Enemy body hit player for " + bodyDamage + " damage");
                    
                    // Set cooldown for this player
                    playerContactCooldown[playerID] = Time.time + playerContactDamageCooldown;
                }
            }
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Simple visualization for sword position
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, attackPoint.position);
            Gizmos.DrawSphere(attackPoint.position, 0.1f);
        }
    }
}