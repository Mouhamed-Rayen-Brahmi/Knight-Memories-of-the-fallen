using System;
using UnityEngine;

namespace ClearSky
{
    public class SimplePlayerController : MonoBehaviour, IDamageable
    {
        public float movePower = 10f;
        public float jumpPower = 12f;
        public float attackCooldown = 0.5f;

        [Header("Jump Settings")]
        public float coyoteTime = 0.15f;
        public float jumpBufferTime = 0.15f;
        public float jumpCutMultiplier = 0.5f;
        public int maxJumps = 2; // Maximum number of jumps allowed

        public int maxHealth = 3;
        public float invincibilityTime = 1.5f;
        public float knockbackForce = 5f;
        public float attackDamage = 25f;
        public float attackRange = 1.2f;

        private Rigidbody2D rb;
        private Animator anim;
        Vector3 movement;
        private int direction = 1;
        bool isJumping = false;
        private bool alive = true;
        private bool isGrounded;
        private Vector3 originalScale;
        private float nextAttackTime = 0f;
        private int currentHealth;
        private float invincibilityTimer = 0f;
        private bool isInvincible = false;

        private System.Collections.Generic.Dictionary<int, float> enemyCollisionCooldown = new System.Collections.Generic.Dictionary<int, float>();
        private float enemyContactDamageCooldown = 1.5f;

        private float coyoteCounter = 0f;
        private float jumpBufferCounter = 0f;

        // Double jump system
        private int jumpsRemaining = 2;
        private bool isMidAirJump = false;

        // Forced fall system
        private bool forcedFall = false;
        private float fallGravityScale = 5f;

        private float lastYPosition = 0f;
        private float stuckTimer = 0f;
        private float stuckThreshold = 0.1f;
        private float stuckDuration = 1f;

        private float horizontalInput;

        void Start()
        {
            gameObject.tag = "Player";

            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            originalScale = transform.localScale;
            currentHealth = maxHealth;
            alive = true;
            jumpsRemaining = maxJumps;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 5f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D newCollider = gameObject.AddComponent<BoxCollider2D>();
                newCollider.size = new Vector2(0.8f, 1.8f);
                newCollider.offset = new Vector2(0, 0);
            }
        }

        private void Update()
        {
            Restart();

            if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
            if (isGrounded)
            {
                coyoteCounter = coyoteTime;
                jumpsRemaining = maxJumps; // Reset jumps when grounded
                isMidAirJump = false;
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }

            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    float flashValue = Mathf.PingPong(Time.time * 10, 1);
                    spriteRenderer.color = new Color(1, 1, 1, flashValue);
                }

                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                    if (spriteRenderer != null)
                        spriteRenderer.color = Color.white;
                }
            }

            if (alive)
            {
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    TakeDamage(1f);

                Attack();
                Jump();
                Run();
            }

            // Reset gravity when grounded
            if (isGrounded && rb.gravityScale != 3f)
            {
                rb.gravityScale = 3f;
                forcedFall = false;
            }
        }

        private void FixedUpdate()
        {
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                Vector2 velocity = new Vector2(horizontalInput * movePower, rb.linearVelocity.y);
                rb.linearVelocity = velocity;
            }
        }

        void Run()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            anim.SetBool("isRun", false);

            if (horizontalInput < 0)
            {
                direction = -1;
                transform.localScale = new Vector3(originalScale.x * -1, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump")) anim.SetBool("isRun", true);
            }
            else if (horizontalInput > 0)
            {
                direction = 1;
                transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
                if (!anim.GetBool("isJump")) anim.SetBool("isRun", true);
            }
        }

        void Jump()
        {
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W))
                jumpBufferCounter = jumpBufferTime;

            // First jump (from ground or coyote time)
            if (jumpBufferCounter > 0f && coyoteCounter > 0f && jumpsRemaining == maxJumps)
            {
                PerformJump(false);
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }
            // Mid-air jump (double jump)
            else if (jumpBufferCounter > 0f && jumpsRemaining > 0 && !isGrounded)
            {
                PerformJump(true);
                jumpBufferCounter = 0f;
            }

            // Jump cut (shorter jump when releasing button)
            if ((Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Z) || Input.GetKeyUp(KeyCode.W)) && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        void PerformJump(bool isMidAir)
        {
            isJumping = true;
            jumpsRemaining--;
            
            if (isMidAir)
            {
                // Mid-air jump animation
                anim.SetBool("isMidAir", true);
                isMidAirJump = true;
            }
            else
            {
                // Regular jump animation
                anim.SetBool("isJump", true);
            }
            
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
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
            attackPosition.x += direction * 0.8f;
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, attackRange);

            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == gameObject) continue;

                if (hitCollider.CompareTag("Enemy"))
                {
                    EnemyCode enemy = hitCollider.GetComponent<EnemyCode>();
                    if (enemy == null && hitCollider.transform.parent != null)
                        enemy = hitCollider.transform.parent.GetComponent<EnemyCode>();

                    if (enemy != null)
                    {
                        enemy.TakeDamage(attackDamage);
                        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                        if (enemyRb != null)
                        {
                            Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                            enemyRb.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                forcedFall = false;
                jumpsRemaining = maxJumps; // Reset jumps on landing
                anim.SetBool("isJump", false);
                anim.SetBool("isMidAir", false);
                isMidAirJump = false;
                rb.gravityScale = 3f;
                return;
            }

            // Cancel jump on collision with ANYTHING midair
            if (!isGrounded && (anim.GetBool("isJump") || anim.GetBool("isMidAir")))
                CancelJumpAndForceFall();

            // Enemy collision logic
            if (collision.gameObject.CompareTag("Enemy"))
            {
                int id = collision.gameObject.GetInstanceID();
                if (!enemyCollisionCooldown.ContainsKey(id))
                {
                    enemyCollisionCooldown[id] = Time.time + enemyContactDamageCooldown;
                    TakeDamage(1f);
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!isGrounded && (anim.GetBool("isJump") || anim.GetBool("isMidAir")) && !collision.gameObject.CompareTag("Ground"))
                CancelJumpAndForceFall();
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
                isGrounded = false;
        }

        private void CancelJumpAndForceFall()
        {
            if (forcedFall || rb == null) return;
            forcedFall = true;

            anim.SetBool("isJump", false);
            anim.SetBool("isMidAir", false);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -8f);
            rb.gravityScale = fallGravityScale;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 attackPosition = transform.position;
            attackPosition.x += direction * 0.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition, attackRange);
        }

        public void TakeDamage(float damage)
        {
            if (isInvincible || !alive) return;

            currentHealth -= Mathf.RoundToInt(damage);
            anim.SetTrigger("hurt");

            Vector2 knockbackDirection = new Vector2((direction == 1) ? -1 : 1, 0.5f);
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

            isInvincible = true;
            invincibilityTimer = invincibilityTime;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(1, 0.5f, 0.5f, 0.8f);

            if (currentHealth <= 0)
                Die();
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
                jumpsRemaining = maxJumps;
                isMidAirJump = false;
                GetComponent<SpriteRenderer>().color = Color.white;
                GetComponent<Collider2D>().enabled = true;
                rb.gravityScale = 3f;
                anim.SetBool("isJump", false);
                anim.SetBool("isMidAir", false);
            }
        }
    }
}