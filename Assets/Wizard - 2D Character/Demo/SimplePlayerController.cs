using System;
using UnityEngine;

namespace ClearSky
{
    public class SimplePlayerController : MonoBehaviour
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
        private int currentHealth; // Current player health
        private float invincibilityTimer = 0f; // Timer for tracking invincibility
        private bool isInvincible = false; // Flag for invincibility state
        
        // Dictionary to track enemy collisions and their cooldown times
        private System.Collections.Generic.Dictionary<int, float> enemyCollisionCooldown = new System.Collections.Generic.Dictionary<int, float>();
        private float enemyContactDamageCooldown = 1.5f; // Time in seconds before same enemy can damage again


        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            originalScale = transform.localScale; 
            currentHealth = maxHealth; // Initialize health
            alive = true;
        }

        private void Update()
        {
            Restart();
            
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
                    TakeDamage(1);
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
                TakeDamage(1);
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
                TakeDamage(1);
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
        void Run()
        {
            Vector3 moveVelocity = Vector3.zero;
            anim.SetBool("isRun", false);


            if (Input.GetAxisRaw("Horizontal") < 0)
            {
                direction = -1;
                moveVelocity = Vector3.left;

                transform.localScale = new Vector3(originalScale.x * -1, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);

            }
            if (Input.GetAxisRaw("Horizontal") > 0)
            {
                direction = 1;
                moveVelocity = Vector3.right;

                transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);

            }
            transform.position += moveVelocity * movePower * Time.deltaTime;
        }
        void Jump()
        {
            if ((Input.GetButtonDown("Jump") || Input.GetAxisRaw("Vertical") > 0)
            && !anim.GetBool("isJump") && isGrounded)
            {
                isJumping = true;
                anim.SetBool("isJump", true); 
            }

            if (!isJumping)
            {
                return;
            }

            rb.linearVelocity = Vector2.zero;
            Vector2 jumpVelocity = new Vector2(0, jumpPower);
            rb.AddForce(jumpVelocity, ForceMode2D.Impulse);
            isJumping = false;
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
        
        void DealDamageToEnemies()
        {
            Vector2 attackPosition = transform.position;
            attackPosition.x += direction * 0.5f; 
            
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, attackRange);
            
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == gameObject) continue;
                
                if (hitCollider.CompareTag("Enemy"))
                {
                    EnemyCode enemy = hitCollider.GetComponent<EnemyCode>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(attackDamage);
                    }
                }
            }
        }
        
        void OnDrawGizmosSelected()
        {
            Vector2 attackPosition = transform.position;
            attackPosition.x += direction * 0.5f;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition, attackRange);
        }
        public void TakeDamage(int damageAmount)
        {
            // If already invincible or dead, ignore damage
            if (isInvincible || !alive)
                return;
            
            // Apply damage
            currentHealth -= damageAmount;
            
            // Play hurt animation
            if (anim != null)
            {
                anim.SetTrigger("hurt");
            }
            
            // Apply knockback force
            Vector2 knockbackDirection = new Vector2((direction == 1) ? -1 : 1, 0.5f);
            if (rb != null)
            {
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
            
            // Set invincibility
            isInvincible = true;
            invincibilityTimer = invincibilityTime;
            
            // Apply visual feedback for invincibility
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Start with a flash
                spriteRenderer.color = new Color(1, 0.5f, 0.5f, 0.8f);
            }
            
            if (currentHealth <= 0)
            {
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