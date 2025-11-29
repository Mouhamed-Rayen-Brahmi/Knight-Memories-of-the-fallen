using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClearSky;

public class Deadly : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string layerName = LayerMask.LayerToName(collision.collider.gameObject.layer);

        if (layerName == "Player")
        {
            SimplePlayerController playerController = collision.collider.GetComponent<SimplePlayerController>();
            if (playerController != null)
            {
                // Kill player instantly (deal max health as damage)
                playerController.TakeDamage(playerController.maxHealth);
            }
        }
        else if (layerName == "Enemy")
        {
            EnemyCode enemyController = collision.collider.GetComponent<EnemyCode>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(enemyController.maxHealth);
            }
        }
    }
}
