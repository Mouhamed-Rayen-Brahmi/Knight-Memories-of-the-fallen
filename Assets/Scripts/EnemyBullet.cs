using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 5f;
    public float maxRange = 20f;
    public GameObject hitEffect;

    private Vector3 startPosition;
    private bool hasHit = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        startPosition = transform.position;
        
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) > maxRange)
        {
            DestroyBullet();
        }
    }
    
    private void DestroyBullet(bool showEffect = true)
    {
        if (hasHit) return; 
        
        hasHit = true;
        
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        if (showEffect && hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        
        if (collision.CompareTag("Player"))
        {
            ClearSky.SimplePlayerController playerController = collision.GetComponent<ClearSky.SimplePlayerController>();
            
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
            }
            
            DestroyBullet(true);
        }
        else if (collision.CompareTag("Ground"))
        {
            DestroyBullet(true);
        }
    }
    
    private void OnBecameInvisible()
    {
        if (Vector3.Distance(startPosition, transform.position) > maxRange / 2)
        {
            DestroyBullet(false);
        }
    }
}