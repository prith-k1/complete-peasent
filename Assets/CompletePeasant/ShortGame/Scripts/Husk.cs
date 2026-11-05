using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class Husk : Damageable
    {
        public int maxHealth = 10;
        public float speed = 1.5f, patrolDistance = 2.5f;
        public float noticeDistance = 8, loseDistance = 12, chaseSpeed = 3.2f, leashDistance = 12;
        public float knockbackSpeed = 8;
        public bool hops;
        public SpriteRenderer sprite;
        public bool Aggro { get; private set; }
        private int health;
        private Vector3 home;
        private float direction = -1, time;
        private float stagger;
        private Vector2 recoil;
        private enum Behaviour { Chase, Lunge, Retreat, Wait }
        private Behaviour behaviour;
        private float decisionTimer, windup;

        private void Start() { home = transform.position; health = maxHealth; }
        private void Update()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.Player == null || game.ui.IsOpen || game.Loading || game.Player.Dead) return;
            if (stagger > 0)
            {
                stagger -= Time.deltaTime;
                Move(recoil.x * Time.deltaTime);
                sprite.color = Color.white;
                return;
            }
            time += Time.deltaTime;
            Vector2 playerPosition = game.Player.transform.position;
            float distance = Vector2.Distance(transform.position, playerPosition);
            bool withinLeash = Mathf.Abs(playerPosition.x - home.x) <= leashDistance;
            if (!Aggro && withinLeash && distance <= noticeDistance)
            { Aggro = true; behaviour = Behaviour.Chase; decisionTimer = .4f; }
            if (Aggro && (!withinLeash || distance > loseDistance))
            { Aggro = false; windup = 0; }

            float moveSpeed = speed;
            if (Aggro)
            {
                decisionTimer -= Time.deltaTime;
                if (decisionTimer <= 0) ChooseBehaviour(playerPosition.x);
                if (behaviour == Behaviour.Chase) direction = Mathf.Sign(playerPosition.x - transform.position.x);
                if (behaviour == Behaviour.Lunge)
                    moveSpeed = chaseSpeed * 1.6f;
                else if (behaviour == Behaviour.Wait)
                    moveSpeed = 0;
                else if (behaviour != Behaviour.Retreat)
                    moveSpeed = chaseSpeed;
                if (windup > 0) { windup -= Time.deltaTime; moveSpeed = 0; }
            }
            else if (Mathf.Abs(transform.position.x - home.x) > patrolDistance)
                direction = Mathf.Sign(home.x - transform.position.x);
            sprite.color = windup > 0 ? new Color(1, .75f, .35f) : new Color(.88f, .94f, .86f);
            if (!Move(direction * moveSpeed * Time.deltaTime) && !Aggro) direction = -direction;
            if (hops)
            {
                Vector3 position = transform.position;
                position.y = home.y + Mathf.Abs(Mathf.Sin(time * (Aggro ? 3.3f : 2.3f))) * 1.5f;
                transform.position = position;
            }
            sprite.flipX = direction < 0;
        }

        private void ChooseBehaviour(float playerX)
        {
            behaviour = (Behaviour)Random.Range(0, 4);
            decisionTimer = Random.Range(.6f, 1.2f);
            direction = Mathf.Sign(playerX - transform.position.x);
            if (behaviour == Behaviour.Retreat) direction = -direction;
            windup = behaviour == Behaviour.Lunge ? .25f : 0;
        }

        private bool Move(float step)
        {
            if (Mathf.Abs(step) < .0001f) return true;
            float nextX = transform.position.x + step;
            if (Mathf.Abs(nextX - home.x) > leashDistance) return false;
            int ground = 1 << 8;
            Vector2 ahead = new Vector2(nextX + Mathf.Sign(step) * .45f, home.y + .5f);
            if (!Physics2D.Raycast(ahead, Vector2.down, 2, ground)) return false;
            if (Physics2D.Raycast(transform.position, Vector2.right * Mathf.Sign(step), Mathf.Abs(step) + .4f, ground)) return false;
            transform.position += Vector3.right * step;
            return true;
        }

        public override void Hit(int damage, Vector2 direction)
        {
            if (health <= 0) return;
            health -= damage;
            Knockback(direction);
            if (health <= 0) gameObject.SetActive(false);
        }
        public override void Knockback(Vector2 push)
        {
            if (!gameObject.activeSelf) return;
            float away = push.x;
            if (Mathf.Abs(away) < .01f)
            {
                Adventure game = Adventure.Instance;
                away = direction;
                if (game != null && game.Player != null)
                    away = transform.position.x - game.Player.transform.position.x;
            }
            recoil = Vector2.right * (Mathf.Abs(away) < .01f ? 1 : Mathf.Sign(away)) * knockbackSpeed;
            stagger = .22f; windup = 0;
            Aggro = true; decisionTimer = 0;
        }
        private void OnTriggerStay2D(Collider2D other)
        { Peasant player = other.GetComponent<Peasant>(); if (stagger <= 0 && player != null) player.Hurt(1, transform.position, this); }
    }
}
