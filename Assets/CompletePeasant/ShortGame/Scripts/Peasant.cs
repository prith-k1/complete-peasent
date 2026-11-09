using System.Collections.Generic;
using UnityEngine;

namespace CompletePeasant.ShortGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class Peasant : MonoBehaviour
    {
        public float speed = 7, jumpSpeed = 14, dashSpeed = 18;
        public int maxHealth = 6;
        public LayerMask groundMask, enemyMask;
        public Animator animator;
        public SpriteRenderer sprite;
        public SpriteRenderer slash, parryFlash;
        public DawnBolt sunbreakPrefab;
        public float damageIFrames = 1.1f, parryWindow = 0.18f;
        public int Health { get; private set; }
        public bool Dead { get; private set; }
        public bool Grounded { get; private set; }

        public bool Dashing
        {
            get
            {
                return dashTimer > 0;
            }
        }

        public bool Parrying
        {
            get
            {
                return parryTimer > 0;
            }
        }

        private Rigidbody2D body;
        private BoxCollider2D box;
        private float horizontal, facing = 1;
        private float coyote, jumpBuffer, dashTimer, dashCooldown;
        private float attackCooldown, hitDelay = -1, invincible, knockback;
        private float parryTimer, parryCooldown, flashTimer, slashTimer;
        private float sunbreakCooldown;
        private bool downSlash, airJumpUsed;
        private Adventure Game
        {
            get
            {
                return Adventure.Instance;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
        }

        public void Heal(int amount)
        {
            if (!Dead)
                Health = Mathf.Min(maxHealth, Health + amount);
        }

        public void Restore()
        {
            Dead = false;
            Health = maxHealth;
            body.simulated = true;
            body.gravityScale = 3.5f;
            body.velocity = Vector2.zero;
            dashTimer = 0;
            dashCooldown = 0;
            attackCooldown = 0;
            hitDelay = -1;
            knockback = 0;
            invincible = 1;
            horizontal = 0;
            coyote = 0;
            jumpBuffer = 0;
            parryTimer = 0;
            parryCooldown = 0;
            flashTimer = 0;
            slashTimer = 0;
            downSlash = false;
            sunbreakCooldown = 0;
            airJumpUsed = false;
            if (slash != null)
                slash.enabled = false;
            if (parryFlash != null)
                parryFlash.enabled = false;
            animator.ResetTrigger("Attack");
        }

        private void Update()
        {
            if (Game == null || Game.ui.IsOpen || Game.Loading || Dead)
                return;
            float dt = Time.deltaTime;
            horizontal = Input.GetAxisRaw("Horizontal");

            Grounded = body.velocity.y <= .1f &&
                Physics2D.BoxCast(transform.position, box.size * new Vector2(0.88f, 0.96f),
                    0, Vector2.down, 0.12f, groundMask);
            coyote = Grounded ? 0.12f : coyote - dt;
            if (Grounded)
                airJumpUsed = false;

            invincible -= dt;
            knockback -= dt;
            dashCooldown -= dt;
            attackCooldown -= dt;
            parryTimer -= dt;
            parryCooldown -= dt;
            flashTimer -= dt;
            slashTimer -= dt;
            sunbreakCooldown -= dt;

            if (horizontal != 0 && !Dashing && !Parrying && knockback <= 0)
                facing = Mathf.Sign(horizontal);
            sprite.flipX = facing < 0;
            if (flashTimer > 0)
                sprite.color = new Color(1, .85f, .35f);
            else if (invincible > 0 && Mathf.Sin(Time.time * 35) > 0)
                sprite.color = new Color(1, 1, 1, 0.45f);
            else
                sprite.color = Color.white;
            if (slash != null)
                slash.enabled = slashTimer > 0;
            if (parryFlash != null)
            {
                parryFlash.enabled = Parrying || flashTimer > 0;
                parryFlash.color = flashTimer > 0 ? new Color(1, .85f, .35f) : new Color(.65f, .9f, 1, .8f);
            }

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z))
                jumpBuffer = 0.12f;
            else
                jumpBuffer -= dt;
            if (jumpBuffer > 0)
                Jump();
            if ((Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Z)) && body.velocity.y > 0 && knockback <= 0)
                body.velocity = new Vector2(body.velocity.x, body.velocity.y * 0.5f);
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.K))
                Dash();
            if (Input.GetKeyDown(KeyCode.F))
                CastSunbreak();
            if (Input.GetMouseButtonDown(1))
                Parry();
            if (Input.GetMouseButtonDown(0))
                Attack(Input.GetAxisRaw("Vertical") < -0.1f);
            if (hitDelay >= 0)
            {
                hitDelay -= dt;
                if (hitDelay <= 0)
                {
                    Strike();
                    hitDelay = -1;
                }
            }

            Animate();
            if (transform.position.y < -7)
                Hazard();
        }

        private void FixedUpdate()
        {
            if (Game == null || Dead || Game.Loading)
                return;
            if (Dashing)
            {
                body.gravityScale = 0;
                body.velocity = new Vector2(facing * dashSpeed, 0);
                dashTimer = Mathf.Max(0, dashTimer - Time.fixedDeltaTime);
            }
            else
            {
                body.gravityScale = 3.5f;
                if (knockback <= 0)
                    body.velocity = new Vector2(horizontal * speed, body.velocity.y);
            }
        }

        private void Animate()
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetFloat("VerticalSpeed", body.velocity.y);
            animator.SetBool("Grounded", Grounded);
            animator.SetBool("Dashing", Dashing);
        }

        public bool Jump()
        {
            if (Game == null || Dead || Game.Loading || Game.ui.IsOpen || Dashing || knockback > 0)
                return false;
            bool fromGround = Grounded || coyote > 0;
            if (!fromGround && (airJumpUsed || !Game.Data.HasSkill(Skill.AshWings)))
                return false;
            if (!fromGround)
            {
                airJumpUsed = true;
                flashTimer = .15f;
            }

            body.velocity = new Vector2(body.velocity.x, jumpSpeed);
            Grounded = false;
            coyote = 0;
            jumpBuffer = 0;
            animator.Play("Jump", 0, 0);
            return true;
        }

        public bool Dash()
        {
            if (Game == null || Dead || Game.Loading || Game.ui.IsOpen ||
                knockback > 0 || Parrying || dashCooldown > 0 ||
                !Game.Data.HasSkill(Skill.EmberDash))
                return false;
            dashTimer = .17f;
            dashCooldown = .65f;
            hitDelay = -1;
            return true;
        }

        public void CastSunbreak()
        {
            if (Game == null || Dead || Game.Loading || Game.ui.IsOpen ||
                knockback > 0 || Parrying || Dashing || sunbreakCooldown > 0 ||
                !Game.Data.HasSkill(Skill.Sunbreak) || sunbreakPrefab == null)
                return;
            DawnBolt bolt = Instantiate(sunbreakPrefab, transform.position + Vector3.right * facing * .5f, Quaternion.identity);
            bolt.direction = Vector2.right * facing;
            sunbreakCooldown = .9f;
            hitDelay = -1;
            flashTimer = .15f;
            attackCooldown = Mathf.Max(attackCooldown, .2f);
            Game.Sound(Game.attackSound);
        }

        public void Attack(bool downward = false)
        {
            if (Dead || Game.Loading || Game.ui.IsOpen || attackCooldown > 0 || knockback > 0 || Parrying)
                return;
            dashTimer = 0;
            downSlash = downward && !Grounded;
            attackCooldown = 0.42f;
            hitDelay = 0.12f;
            slashTimer = 0;
            if (slash != null)
                slash.enabled = false;
            if (downSlash)
            {
                animator.ResetTrigger("Attack");
                animator.Play(body.velocity.y > 0 ? "Jump" : "Fall", 0, 0);
            }
            else
                animator.SetTrigger("Attack");
            Game.Sound(Game.attackSound);
        }

        private void Strike()
        {
            HashSet<Damageable> struck = new HashSet<Damageable>();
            Vector2 direction = downSlash ? Vector2.down : Vector2.right * facing;
            Vector2 center = (Vector2)transform.position + direction * 1.1f;
            if (slash != null && downSlash)
            {
                slash.transform.localPosition = direction * 1.1f;
                slash.transform.localRotation = Quaternion.Euler(0, 0, -90);
                slash.enabled = true;
                slashTimer = .16f;
            }

            foreach (Collider2D collider in Physics2D.OverlapBoxAll(
                center, downSlash ? new Vector2(1.6f, 1.8f) : new Vector2(1.7f, 1.8f),
                0, enemyMask))
            {
                Damageable target = collider.GetComponentInParent<Damageable>();
                if (target != null && struck.Add(target))
                    target.Hit(5, direction);
            }

            if (downSlash && struck.Count > 0)
            {
                body.velocity = new Vector2(body.velocity.x, jumpSpeed * .75f);
                invincible = Mathf.Max(invincible, .15f);
            }
        }

        public void Parry()
        {
            if (Dead || Game.Loading || Game.ui.IsOpen || knockback > 0 || Dashing || parryCooldown > 0)
                return;
            parryTimer = parryWindow;
            parryCooldown = .65f;
            hitDelay = -1;
            attackCooldown = Mathf.Max(attackCooldown, .25f);
        }

        public bool TryParry(Vector2 source)
        {
            if (!Parrying || Dead || Game.Loading || Game.ui.IsOpen || (source.x - transform.position.x) * facing < -.2f)
                return false;
            parryTimer = 0;
            flashTimer = .22f;
            invincible = Mathf.Max(invincible, .35f);
            Game.Sound(Game.restSound);
            return true;
        }

        public void Hurt(int damage, Vector2 source, Damageable attacker = null)
        {
            if (Dead || Dashing || Game.Loading || Game.ui.IsOpen)
                return;
            if (TryParry(source))
            {
                if (attacker != null)
                    attacker.Knockback((source - (Vector2)transform.position).normalized);
                return;
            }

            if (invincible > 0)
                return;
            Health = Mathf.Max(0, Health - damage);
            invincible = damageIFrames;
            knockback = 0.2f;
            hitDelay = -1;
            parryTimer = 0;
            float distanceX = transform.position.x - source.x;
            float away = Mathf.Abs(distanceX) < .01f ? -facing : Mathf.Sign(distanceX);
            body.velocity = new Vector2(away * 7, 5);
            Game.Sound(Game.hurtSound);
            if (Health == 0)
            {
                Dead = true;
                horizontal = 0;
                body.velocity = Vector2.zero;
                body.simulated = false;
                if (slash != null)
                    slash.enabled = false;
                if (parryFlash != null)
                    parryFlash.enabled = false;
                animator.SetFloat("Speed", 0);
                animator.SetBool("Dashing", false);
                Game.StartCoroutine(Game.Respawn());
            }
        }

        public void Hazard()
        {
            if (Dead || Game.Loading || Game.ui.IsOpen)
                return;
            invincible = 0;
            dashTimer = 0;
            parryTimer = 0;
            Hurt(maxHealth, transform.position + Vector3.right);
        }
    }
}
