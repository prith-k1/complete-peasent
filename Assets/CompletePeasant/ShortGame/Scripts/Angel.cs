using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class Angel : Damageable
    {
        public int maxHealth = 30;
        public float attackInterval = 1.5f, triggerX = 46;
        public float arenaLeft = 47, arenaRight = 79;
        public GameObject door;
        public SpriteRenderer sprite, warning;
        public SunOrb orbPrefab;
        public RootBurst rootPrefab;
        public int Health { get; private set; }
        public bool Active { get; private set; }

        private float cooldown = 1, tell, movement, flash, stagger;
        private int attackNumber;
        private bool moving;
        private Vector3 from, to;
        private float targetX, groundHeight;
        private Adventure Game
        {
            get
            {
                return Adventure.Instance;
            }
        }

        private void Start()
        {
            Health = maxHealth;
            door.SetActive(false);
            warning.enabled = false;
            groundHeight = transform.position.y;
            if (Game.Data.bossesDefeated > Game.Stage.data.index)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Game == null || Game.Player == null || Game.ui.IsOpen || Game.Loading || Game.Player.Dead)
                return;
            if (!Active)
            {
                if (Game.Player.transform.position.x < triggerX)
                    return;
                Active = true;
                door.SetActive(true);
                Game.music.clip = Game.Stage.data.bossMusic;
                Game.music.Play();
            }

            float dt = Time.deltaTime;
            flash -= dt;
            sprite.color = flash > 0 ? Color.white : new Color(1, .93f, .86f);
            sprite.flipX = Game.Player.transform.position.x < transform.position.x;
            if (stagger > 0)
            {
                stagger -= dt;
                return;
            }

            if (moving)
            {
                movement += dt;
                bool swoop = Game.Stage.data.bossPattern == 2;
                float t = Mathf.Clamp01(movement / (swoop ? 1.1f : .95f));
                Vector3 position = Vector3.Lerp(from, to, t);
                if (swoop)
                    position.y += Mathf.Sin(t * Mathf.PI) * 4;
                transform.position = position;
                if (t >= 1)
                    moving = false;
                return;
            }

            if (tell > 0)
            {
                tell -= dt;
                if (tell <= 0)
                {
                    warning.enabled = false;
                    PerformAttack();
                }

                return;
            }

            cooldown -= dt;
            if (cooldown <= 0)
            {
                attackNumber++;
                targetX = Mathf.Clamp(Game.Player.transform.position.x, arenaLeft, arenaRight);
                tell = .8f;
                warning.enabled = true;
                cooldown = attackInterval * (Health <= maxHealth / 2 ? .75f : 1);
            }
        }

        private void PerformAttack()
        {
            int pattern = Game.Stage.data.bossPattern;
            float middle = (arenaLeft + arenaRight) * .5f;
            if (pattern == 0)
            {
                StartMove(transform.position.x < middle ? arenaRight - 6 : arenaLeft + 2);
            }
            else if (pattern == 1)
            {
                if (attackNumber % 2 == 1)
                {
                    RootAt(targetX);
                    if (Health <= maxHealth / 2)
                        RootAt(targetX < middle ? targetX + 3 : targetX - 3);
                }
                else
                    ShootFan(1, 4);
            }
            else
            {
                if (attackNumber % 2 == 1)
                    StartMove(transform.position.x < middle ? arenaRight - 7 : arenaLeft + 3);
                else
                    ShootFan(Health <= maxHealth / 2 ? 5 : 3, 6);
            }
        }

        private void StartMove(float x)
        {
            from = transform.position;
            to = new Vector3(Mathf.Clamp(x, arenaLeft, arenaRight), groundHeight, 0);
            movement = 0;
            moving = true;
        }

        private void RootAt(float x)
        {
            if (rootPrefab != null)
                Instantiate(rootPrefab, new Vector3(Mathf.Clamp(x, arenaLeft, arenaRight), 0, 0), Quaternion.identity);
        }

        private void ShootFan(int count, float speed)
        {
            Vector2 aim = (Game.Player.transform.position - transform.position).normalized;
            for (int i = 0; i < count; i++)
            {
                Vector2 heading = Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 20) * aim;
                SunOrb shot = Instantiate(orbPrefab, transform.position + (Vector3)heading * 1.5f, Quaternion.identity);
                shot.direction = heading;
                shot.speed = speed;
            }
        }

        public override void Hit(int damage, Vector2 direction)
        {
            if (!Active || Health <= 0)
                return;
            Knockback(direction);
            Health = Mathf.Max(0, Health - damage);
            flash = .15f;
            if (Health > 0)
                return;
            Active = false;
            door.SetActive(false);
            foreach (SunOrb orb in FindObjectsOfType<SunOrb>())
                Destroy(orb.gameObject);
            foreach (RootBurst root in FindObjectsOfType<RootBurst>())
                Destroy(root.gameObject);
            Game.BossDefeated();
            gameObject.SetActive(false);
        }

        public override void Knockback(Vector2 direction)
        {
            if (!Active || Health <= 0)
                return;
            Vector3 recoil = new Vector3(direction.x * .35f, 0, 0);
            float nextX = Mathf.Clamp(transform.position.x + recoil.x, arenaLeft, arenaRight);
            recoil.x = nextX - transform.position.x;
            transform.position += recoil;
            if (moving)
            {
                from.x = Mathf.Clamp(from.x + recoil.x, arenaLeft, arenaRight);
                to.x = Mathf.Clamp(to.x + recoil.x, arenaLeft, arenaRight);
            }

            stagger = .1f;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Peasant player = other.GetComponent<Peasant>();
            if (Active && player != null)
                player.Hurt(1, transform.position, this);
        }
    }
}
