using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySwordCollider : MonoBehaviour
{
    // Changed to SerializeField to fix serialization issues in Unity
    [SerializeField] private BoxCollider2D swordCollider;
    
    // Reference to parent - not serialized
    private EnemyCode enemyParent;
    
    private void Awake()
    {
        // Set tag
        gameObject.tag = "EnemySword";
        
        // Get reference to parent enemy
        UpdateParentReference();
        
        // Setup collider if needed
        if (swordCollider == null)
        {
            swordCollider = GetComponent<BoxCollider2D>();
            if (swordCollider == null)
            {
                swordCollider = gameObject.AddComponent<BoxCollider2D>();
                swordCollider.size = new Vector2(1.2f, 0.5f);
                swordCollider.offset = new Vector2(0.5f, 0);
                swordCollider.isTrigger = true;
                swordCollider.enabled = false; // Start disabled
            }
        }
        
        // Make sure collider is set up as trigger
        if (swordCollider != null)
        {
            swordCollider.isTrigger = true;
            
            // Log to confirm setup
            Debug.Log("EnemySwordCollider initialized with collider: " + 
                     (swordCollider != null ? "YES" : "NO") + 
                     ", Parent found: " + (enemyParent != null ? "YES" : "NO"));
        }
    }
    
    // Helper method to update parent reference
    private void UpdateParentReference()
    {
        if (enemyParent == null)
        {
            enemyParent = GetComponentInParent<EnemyCode>();
            if (enemyParent == null && transform.parent != null)
            {
                enemyParent = transform.parent.GetComponent<EnemyCode>();
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Make sure we have debug info
        Debug.Log("EnemySwordCollider: OnTriggerEnter2D called with " + 
                  (collision != null ? collision.gameObject.name : "null"));
        
        // Ensure we have valid references
        if (swordCollider == null) 
        {
            swordCollider = GetComponent<BoxCollider2D>();
            if (swordCollider == null) 
            {
                Debug.LogError("No sword collider found!");
                return;
            }
        }
        
        // Ensure collider is enabled
        if (!swordCollider.enabled)
        {
            Debug.Log("Sword collider is disabled");
            return;
        }
        
        // Update parent reference if needed
        UpdateParentReference();
        
        // Validate parent state
        if (enemyParent == null)
        {
            Debug.LogError("No enemy parent found!");
            return;
        }
        
        if (enemyParent.isDead)
        {
            Debug.Log("Enemy is dead, can't attack");
            return;
        }
        
        if (!enemyParent.isAttacking)
        {
            Debug.Log("Enemy is not in attack state");
            return;
        }
        
        // Check for valid collision
        if (collision == null) return;
        
        // Debug collision info
        Debug.Log("Sword collider hit object: " + collision.gameObject.name + " with tag: " + collision.tag);
        
        // Direct player detection - try different methods to find the player
        bool isPlayerHit = false;
        ClearSky.SimplePlayerController playerController = null;
        
        // Method 1: Check by tag
        if (collision.CompareTag("Player"))
        {
            isPlayerHit = true;
            playerController = collision.GetComponent<ClearSky.SimplePlayerController>();
        }
        
        // Method 2: Check by component
        if (!isPlayerHit)
        {
            playerController = collision.GetComponent<ClearSky.SimplePlayerController>();
            if (playerController != null)
            {
                isPlayerHit = true;
            }
        }
        
        // Method 3: Check parent
        if (!isPlayerHit && collision.transform.parent != null)
        {
            if (collision.transform.parent.CompareTag("Player"))
            {
                isPlayerHit = true;
                playerController = collision.transform.parent.GetComponent<ClearSky.SimplePlayerController>();
            }
        }
        
        if (!isPlayerHit)
        {
            Debug.Log("Not a player collision");
            return;
        }
        
        // At this point we know it's the player
        Debug.Log("CONFIRMED: Sword hit the player!");
        
        // Try to apply damage
        bool damageApplied = false;
        
        // Try to get IDamageable implementation
        IDamageable playerDamageable = null;
        if (playerController != null)
        {
            playerDamageable = playerController as IDamageable;
        }
        else
        {
            playerDamageable = collision.GetComponent<IDamageable>();
            if (playerDamageable == null && collision.transform.parent != null)
            {
                playerDamageable = collision.transform.parent.GetComponent<IDamageable>();
            }
        }
        
        // Apply damage if found
        if (playerDamageable != null)
        {
            Debug.Log("Applying " + enemyParent.swordDamage + " damage to player directly");
            playerDamageable.TakeDamage(enemyParent.swordDamage);
            damageApplied = true;
        }
        
        // Fallback - use the parent's method for damage application
        if (collision.gameObject != null)
        {
            Debug.Log("Applying damage via parent's DamageSwordTarget method");
            enemyParent.DamageSwordTarget(collision.gameObject);
            damageApplied = true;
        }
        
        // Verify damage was applied
        if (!damageApplied)
        {
            Debug.LogError("Failed to apply damage to player!");
        }
        
        // Visual feedback
        if (collision.transform != null)
        {
            StartCoroutine(ShowHitEffect(collision.transform.position));
        }
    }
    
    // Visual feedback for hits
    private IEnumerator ShowHitEffect(Vector3 hitPosition)
    {
        // Create a temporary hit effect - moved outside try/catch for yield compatibility
        GameObject hitEffect = new GameObject("SwordHitEffect");
        if (hitEffect == null) yield break;
        
        hitEffect.transform.position = hitPosition;
        
        // Visual representation
        SpriteRenderer effectRenderer = hitEffect.AddComponent<SpriteRenderer>();
        if (effectRenderer == null)
        {
            Destroy(hitEffect);
            yield break;
        }
        
        // White flash effect
        effectRenderer.color = Color.white;
        hitEffect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Fade out
        float duration = 0.3f;
        float timer = 0f;
        
        while (timer < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);
            effectRenderer.color = new Color(1, 1, 1, alpha);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Clean up
        Destroy(hitEffect);
    }
    
    // Add extra safeguards when the object is enabled
    private void OnEnable()
    {
        // Re-initialize references if needed
        UpdateParentReference();
        
        if (swordCollider == null)
        {
            swordCollider = GetComponent<BoxCollider2D>();
        }
    }
    
    // Add safeguard for Unity Editor errors
    private void OnValidate()
    {
        // This is called in the editor, helps with serialization issues
        if (swordCollider == null)
        {
            swordCollider = GetComponent<BoxCollider2D>();
        }
    }
    
    // When object is reactivated in the scene
    private void Reset()
    {
        // This helps when the component is reset in the inspector
        swordCollider = GetComponent<BoxCollider2D>();
        UpdateParentReference();
    }
}