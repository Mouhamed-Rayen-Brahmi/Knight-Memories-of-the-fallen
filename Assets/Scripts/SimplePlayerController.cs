using System;
using UnityEngine;

namespace ClearSky
{
    public class SimplePlayerController : MonoBehaviour, IDamageable
    {
        public float movePower = 10f;
        public float jumpPower = 5f;
        public float attackCooldown = 0.5f; 
        public int maxHealth = 3; // Maximum player health
        public float invincibilityTime = 1.5f; // Time of invincibility after getting hit
        public float knockbackForce = 5f; // Force applied when player gets hit
        public float attackDamage = 25f; // Damage dealt by player attacks
        public float attackRange = 1.2f; // Range of player attacks
        
        private Rigidbody2D rb;
        private Animator anim;
        Vector3 movement;
        private int direction = 1;
        bool isJumping = false;
        private bool alive = true;
        public Collider2D Ground;
        private bool isGrounded;
        private Vector3 originalScale;
        private float nextAttackTime = 0f; 
        private int currentHealth; 
        private float invincibilityTimer = 0f;
        private bool isInvincible = false; 
        
        private System.Collections.Generic.Dictionary<int, float> enemyCollisionCooldown = new System.Collections.Generic.Dictionary<int, float>();
        private float enemyContactDamageCooldown = 1.5f; 

        // Start is called before the first frame update
        void Start()
        {
            // Make sure player is tagged properly
            gameObject.tag = "Player";
            // Debug.Log("Player initialized with tag: " + gameObject.tag);
            
            // Initialize components
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            originalScale = transform.localScale; 
            currentHealth = maxHealth; // Initialize health
            alive = true;
            
            // Ensure no rotation on Z axis from the start
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
            
            // Configure rigidbody for proper physics and camera following
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3f; // Reasonable gravity
                rb.linearDamping = 0.5f; // Reduced drag for more responsive movement
                rb.angularDamping = 5f; // Prevent unwanted rotation
            }
            else
            {
                // Debug.LogWarning("Player has no Rigidbody2D - adding one for physics-based movement");
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3f;
                rb.linearDamping = 0.5f; // Reduced drag
                rb.angularDamping = 5f;
            }
            
            // Ensure we have a proper collider for collision detection
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                // Debug.LogWarning("Player has no collider - adding BoxCollider2D");
                BoxCollider2D newCollider = gameObject.AddComponent<BoxCollider2D>();
                newCollider.size = new Vector2(0.8f, 1.8f);
                newCollider.offset = new Vector2(0, 0);
            }
        }

        private void Update()
        {
            Restart();
            
            // Prevent rotation on Z axis
            if (transform.rotation.eulerAngles.z != 0)
            {
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
            }
            
            // Handle invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                // Flash the player sprite to indicate invincibility
                float flashValue = Mathf.PingPong(Time.time * 10, 1);
                
                // Check if SpriteRenderer exists before accessing it
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1, 1, 1, flashValue);
                }
                
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                    
                    // Check if SpriteRenderer exists before accessing it
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = Color.white; // Reset color
                    }
                }
            }
            
            // Clean up expired enemy collision cooldowns
            System.Collections.Generic.List<int> expiredEnemies = new System.Collections.Generic.List<int>();
            foreach (var enemy in enemyCollisionCooldown)
            {
                if (Time.time > enemy.Value)
                {
                    expiredEnemies.Add(enemy.Key);
                }
            }
            
            foreach (int enemyId in expiredEnemies)
            {
                enemyCollisionCooldown.Remove(enemyId);
            }
            
            if (alive)
            {
                // Debug hurt for testing
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    TakeDamage(1f);
                }
                Attack();
                Jump();
                Run();
            }
        }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            anim.SetBool("isJump", false);
        }
        
        // Check for enemy collisions
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Get unique ID for this enemy to track cooldown
            int enemyInstanceId = collision.gameObject.GetInstanceID();
            
            // Check if we've recently taken damage from this enemy
            if (!enemyCollisionCooldown.ContainsKey(enemyInstanceId))
            {
                // Add this enemy to cooldown dictionary
                enemyCollisionCooldown[enemyInstanceId] = Time.time + enemyContactDamageCooldown;
                
                // Default damage amount is 1
                TakeDamage(1f);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Handle continuous enemy collision with cooldown
        if (collision.gameObject.CompareTag("Enemy"))
        {
            int enemyInstanceId = collision.gameObject.GetInstanceID();
            
            // Check if cooldown has expired for this enemy
            if (!enemyCollisionCooldown.ContainsKey(enemyInstanceId))
            {
                // Add this enemy to cooldown dictionary
                enemyCollisionCooldown[enemyInstanceId] = Time.time + enemyContactDamageCooldown;
                
                // Apply damage
                TakeDamage(1f);
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
        
        // When no longer in contact with an enemy, remove from cooldown dictionary
        if (collision.gameObject.CompareTag("Enemy"))
        {
            int enemyInstanceId = collision.gameObject.GetInstanceID();
            if (enemyCollisionCooldown.ContainsKey(enemyInstanceId))
            {
                enemyCollisionCooldown.Remove(enemyInstanceId);
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            anim.SetBool("isJump", false);
        }
    }
    
    private void FixedUpdate()
    {
        // Also enforce Z rotation constraint in physics updates
        if (transform.rotation.eulerAngles.z != 0)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
        }
        
        // If using a Rigidbody2D, also constrain its rotation
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
    
        void Run()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            anim.SetBool("isRun", false);

            if (horizontalInput < 0)
            {
                direction = -1;
                transform.localScale = new Vector3(originalScale.x * -1, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);
            }
            else if (horizontalInput > 0)
            {
                direction = 1;
                transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);
            }

            // Use physics-based movement for better camera following
            if (rb != null)
            {
                // Keep current Y velocity, only change X velocity
                Vector2 velocity = new Vector2(horizontalInput * movePower, rb.linearVelocity.y);
                rb.linearVelocity = velocity;
            }
            else
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3f;
                rb.linearDamping = 0.5f; // Consistent with Start method
                rb.angularDamping = 5f;
            }
        }
        void Jump()
        {
            if ((Input.GetButtonDown("Jump") || Input.GetAxisRaw("Vertical") > 0)
            && !anim.GetBool("isJump") && isGrounded)
            {
                isJumping = true;
                anim.SetBool("isJump", true); 
                
                // Apply jump force immediately, preserving horizontal velocity
                if (rb != null)
                {
                    Vector2 currentVelocity = rb.linearVelocity;
                    rb.linearVelocity = new Vector2(currentVelocity.x, jumpPower);
                }
                
                isJumping = false; // Reset immediately since we applied the force
            }
        }
        void Attack()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= nextAttackTime)
            {
                anim.SetTrigger("attack");
                
                nextAttackTime = Time.time + attackCooldown;
                
                DealDamageToEnemies();
            }
        }
        // Debug.Log
        void DealDamageToEnemies()
        {
            // Calculate attack position in front of the player
            Vector2 attackPosition = transform.position;
            attackPosition.x += direction * 0.8f; // Increased offset to reach enemies better
            
            // Debug message for attack
            // Debug.Log("Player attacking at position: " + attackPosition + " with range: " + attackRange);
            
            // Get all colliders in attack range
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, attackRange);
            
            bool hitAnything = false;
            
            foreach (Collider2D hitCollider in hitColliders)
            {
                // Skip self-collision
                if (hitCollider.gameObject == gameObject) continue;
                
                // Debug.Log("Hit object: " + hitCollider.gameObject.name + " with tag: " + hitCollider.tag);
                
                // Check for enemy tag or enemy sword tag
                if (hitCollider.CompareTag("Enemy") || hitCollider.transform.CompareTag("Enemy"))
                {
                    // Try to get enemy component directly
                    EnemyCode enemy = hitCollider.GetComponent<EnemyCode>();
                    
                    // If not found, try parent
                    if (enemy == null && hitCollider.transform.parent != null)
                    {
                        enemy = hitCollider.transform.parent.GetComponent<EnemyCode>();
                    }
                    
                    // Apply damage if enemy found
                    if (enemy != null)
                    {
                        // Debug.Log("Player hit enemy for " + attackDamage + " damage");
                        enemy.TakeDamage(attackDamage);
                        hitAnything = true;
                        
                        // Apply knockback to enemy if it has Rigidbody2D
                        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                        if (enemyRb != null)
                        {
                            Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                            enemyRb.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Hit enemy collider but couldn't find EnemyCode component!");
                    }
                }
            }
            
            if (!hitAnything)
            {
                Debug.Log("Player attack didn't hit any enemies");
            }
        }
        
        void OnDrawGizmosSelected()
        {
            Vector2 attackPosition = transform.position;
            attackPosition.x += direction * 0.5f;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition, attackRange);
        }
        // Implement IDamageable interface
        public void TakeDamage(float damage)
        {
            // Debug.Log("Player.TakeDamage called with damage: " + damage);
            
            // If already invincible or dead, ignore damage
            if (isInvincible)
            {
                // Debug.Log("Player is invincible - damage ignored");
                return;
            }
            
            if (!alive)
            {
                // Debug.Log("Player is not alive - damage ignored");
                return;
            }
            
            // Apply damage (convert float to int if needed)
            int damageAmount = Mathf.RoundToInt(damage);
            currentHealth -= damageAmount;
            
            // Debug.Log("Player took " + damageAmount + " damage! Current health: " + currentHealth);
            
            // Play hurt animation
            if (anim != null)
            {
                anim.SetTrigger("hurt");
                // Debug.Log("Player hurt animation triggered");
            }
            else
            {
                // Debug.LogError("Player has no animator component!");
            }
            
            // Apply knockback force
            Vector2 knockbackDirection = new Vector2((direction == 1) ? -1 : 1, 0.5f);
            if (rb != null)
            {
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                // Debug.Log("Applied knockback force to player");
            }
            
            // Set invincibility
            isInvincible = true;
            invincibilityTimer = invincibilityTime;
            // Debug.Log("Player is now invincible for " + invincibilityTime + " seconds");
            
            // Apply visual feedback for invincibility
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Start with a flash
                spriteRenderer.color = new Color(1, 0.5f, 0.5f, 0.8f);
            }
            
            if (currentHealth <= 0)
            {
                // Debug.Log("Player health reached zero - calling Die()");
                Die();
            }
        }
        
        void Die()
        {
            if (alive)
            {
                anim.SetTrigger("die");
                alive = false;
                
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0;
                GetComponent<Collider2D>().enabled = false;
            }
        }
        void Restart()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                anim.SetTrigger("idle");
                alive = true;
                currentHealth = maxHealth;
                isInvincible = false;
                GetComponent<SpriteRenderer>().color = Color.white;
                GetComponent<Collider2D>().enabled = true;
                rb.gravityScale = 1;
            }
        }
    }
}