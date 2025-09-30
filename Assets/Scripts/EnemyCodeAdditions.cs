using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCodeAdditions : MonoBehaviour
{
    // Add this method to your EnemyCode class:
    
    /*
    // Method for sword collider to use
    public void DamageSwordTarget(GameObject target)
    {
        if (isDead) return;
        
        // Check if this player can be damaged (cooldown)
        int playerID = target.GetInstanceID();
        if (!playerContactCooldown.ContainsKey(playerID) || 
            Time.time >= playerContactCooldown[playerID])
        {
            // Apply sword damage to player
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(swordDamage);
                Debug.Log("Enemy sword hit player for " + swordDamage + " damage");
                
                // Set cooldown for this player
                playerContactCooldown[playerID] = Time.time + playerContactDamageCooldown;
            }
            
            // Apply knockback
            Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
                playerRb.AddForce(knockbackDir * 10f, ForceMode2D.Impulse);
            }
        }
    }
    */
    
    // This is a temporary script to provide code examples for adding to EnemyCode.cs
    // Copy the DamageSwordTarget method from the comment above into your EnemyCode.cs
    // Once added to EnemyCode, this script can be deleted
}