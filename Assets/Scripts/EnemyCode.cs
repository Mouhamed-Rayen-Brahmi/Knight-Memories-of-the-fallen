using System.Collections;
using UnityEngine;

public class EnemyCode : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int damageAmount = 1;
    public float invincibilityTime = 0.5f; 
    
    [Header("Attack Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootingInterval = 2f;
    public float bulletSpeed = 10f;
    public float detectionRange = 10f;
    
    [Header("Death Settings")]
    public float deathDelay = 1f;
    public GameObject deathEffect;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public bool canMove = true;
    public bool facingRight = true;
    
    private Transform player;
    private Animator animator; 
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool isDead = false;
    private float lastShootTime;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        lastShootTime = Time.time;
    }

    void Update()
    {
        if (isDead) return;
        
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            
            if (spriteRenderer != null)
            {
                float flashValue = Mathf.PingPong(Time.time * 20, 1);
                spriteRenderer.color = new Color(1, flashValue, flashValue, 1);
            }
            
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
        
        if (player == null)
        {
            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                player = GameObject.FindGameObjectWithTag("Player").transform;
            }
            return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRange)
        {
            FacePlayer();
            
            if (Time.time >= lastShootTime + shootingInterval)
            {
                Shoot();
                lastShootTime = Time.time;
            }
            
            if (canMove)
            {
                MoveTowardsPlayer();
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
        }
    }
    
    void FacePlayer()
    {
        if (player.position.x > transform.position.x && !facingRight)
        {
            Flip();
        }
        else if (player.position.x < transform.position.x && facingRight)
        {
            Flip();
        }
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(direction.x * moveSpeed * Time.deltaTime, 0, 0);
        }
        
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
        }
    }
    
    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet prefab not assigned to " + gameObject.name);
            return;
        }
        
        // Make sure we have a player reference
        if (player == null)
        {
            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                player = GameObject.FindGameObjectWithTag("Player").transform;
            }
            else
            {
                return; // No player to shoot at
            }
        }
        
        // Get shoot position
        Vector3 shootPosition = (firePoint != null) ? firePoint.position : transform.position;
        
        // Calculate direction to player
        Vector2 direction = (player.position - shootPosition).normalized;
        
        // Create bullet with correct rotation to face player
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject bullet = Instantiate(bulletPrefab, shootPosition, Quaternion.Euler(0, 0, angle));
        
        // Set up rigidbody
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null)
        {
            bulletRb = bullet.AddComponent<Rigidbody2D>();
            bulletRb.gravityScale = 0; // No gravity for bullet
        }
        
        // Set up bullet script
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<EnemyBullet>();
            bulletScript.damage = damageAmount;
        }
        
        // Add collider if missing
        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (bulletCol == null)
        {
            CircleCollider2D circleCol = bullet.AddComponent<CircleCollider2D>();
            circleCol.isTrigger = true;
            circleCol.radius = 0.1f;
        }
        
        // Apply velocity directly toward player
        bulletRb.linearVelocity = direction * bulletSpeed;
        
        // Set bullet properties
        if (bulletScript != null)
        {
            bulletScript.damage = damageAmount;
            bulletScript.maxRange = detectionRange * 1.5f; // Set max range slightly beyond detection range
        }
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
        
        // Note: Don't need to Destroy here as the bullet handles its own destruction
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible) return;
        
        currentHealth -= damage;
        
        // Make enemy invincible for a short time
        isInvincible = true;
        invincibilityTimer = invincibilityTime;
        
        StartCoroutine(FlashRed());
        
        if (animator != null)
        {
            animator.SetTrigger("hurt");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        // Check if already marked as dead to prevent double processing
        if (isDead) return;
        
        // Mark as dead
        isDead = true;
        
        // Disable any running coroutines to prevent interference
        StopAllCoroutines();
        
        // Play death animation if animator exists
        if (animator != null)
        {
            animator.SetTrigger("die");
        }
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.simulated = false; // Disable physics simulation
        }
        
        // Disable all colliders on this object and its children
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            col.enabled = false;
        }
        
        // Spawn death effect if available
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Make sure we can't shoot anymore
        canMove = false;
        
        // Immediately disable all scripts to prevent any further functionality
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this) // Don't disable this script yet
            {
                script.enabled = false;
            }
        }
        
        // Disable any renderers to make enemy visually disappear
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Destroy the gameObject after the delay
        Destroy(gameObject, deathDelay);
        
        // Disable this script to prevent any further updates
        enabled = false;
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
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}
