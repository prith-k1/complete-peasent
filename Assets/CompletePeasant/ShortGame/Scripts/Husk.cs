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
        private float direction = -1;
        private float time;
        private float stagger;
        private Vector2 recoil;
        private enum Behaviour
        {
            Chase,
            Lunge,
            Retreat,
            Wait
        }

        private Behaviour behaviour;
        private float decisionTimer;
        private float windup;
        private void Start()
        {
            home = transform.position;
            health = maxHealth;
        }

        private void Update()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.Player == null || game.ui.IsOpen || game.Loading || game.Player.Dead)
                return;
            if (stagger > 0)
            {
                stagger -= Time.deltaTime;
                Move(recoil.x * Time.deltaTime);
                sprite.color = Color.white;
                return;
            }

            time += Time.deltaTime;
            Vector2 playerPosition = game.Player.transform.position;
            UpdateAggro(playerPosition);
            float moveSpeed = GetMoveSpeed(playerPosition.x);

            if (windup > 0)
                sprite.color = new Color(1, .75f, .35f);
            else
                sprite.color = new Color(.88f, .94f, .86f);

            bool moved = Move(direction * moveSpeed * Time.deltaTime);
            if (!moved && !Aggro)
                direction = -direction;

            if (hops)
            {
                float hopSpeed = 2.3f;
                if (Aggro)
                    hopSpeed = 3.3f;
                Vector3 position = transform.position;
                position.y = home.y + Mathf.Abs(Mathf.Sin(time * hopSpeed)) * 1.5f;
                transform.position = position;
            }

            sprite.flipX = direction < 0;
        }

        private void UpdateAggro(Vector2 playerPosition)
        {
            float distance = Vector2.Distance(transform.position, playerPosition);
            bool withinLeash = Mathf.Abs(playerPosition.x - home.x) <= leashDistance;
            if (!Aggro && withinLeash && distance <= noticeDistance)
            {
                Aggro = true;
                behaviour = Behaviour.Chase;
                decisionTimer = .4f;
            }

            if (Aggro && (!withinLeash || distance > loseDistance))
            {
                Aggro = false;
                windup = 0;
            }
        }

        private float GetMoveSpeed(float playerX)
        {
            if (!Aggro)
            {
                if (Mathf.Abs(transform.position.x - home.x) > patrolDistance)
                    direction = Mathf.Sign(home.x - transform.position.x);
                return speed;
            }

            decisionTimer -= Time.deltaTime;
            if (decisionTimer <= 0)
                ChooseBehaviour(playerX);
            if (behaviour == Behaviour.Chase)
                direction = Mathf.Sign(playerX - transform.position.x);

            float moveSpeed = chaseSpeed;
            if (behaviour == Behaviour.Retreat)
                moveSpeed = speed;
            else if (behaviour == Behaviour.Lunge)
                moveSpeed = chaseSpeed * 1.6f;
            else if (behaviour == Behaviour.Wait)
                moveSpeed = 0;

            if (windup > 0)
            {
                windup -= Time.deltaTime;
                moveSpeed = 0;
            }
            return moveSpeed;
        }

        private void ChooseBehaviour(float playerX)
        {
            behaviour = (Behaviour)Random.Range(0, 4);
            decisionTimer = Random.Range(.6f, 1.2f);
            direction = Mathf.Sign(playerX - transform.position.x);
            if (behaviour == Behaviour.Retreat)
                direction = -direction;
            windup = 0;
            if (behaviour == Behaviour.Lunge)
                windup = .25f;
        }

        private bool Move(float step)
        {
            if (Mathf.Abs(step) < .0001f)
                return true;
            float nextX = transform.position.x + step;
            if (Mathf.Abs(nextX - home.x) > leashDistance)
                return false;
            int ground = LayerMask.GetMask("Ground");
            Vector2 ahead = new Vector2(nextX + Mathf.Sign(step) * .45f, home.y + .5f);
            if (!Physics2D.Raycast(ahead, Vector2.down, 2, ground))
                return false;
            if (Physics2D.Raycast(transform.position, Vector2.right * Mathf.Sign(step), Mathf.Abs(step) + .4f, ground))
                return false;
            transform.position += Vector3.right * step;
            return true;
        }

        public override void Hit(int damage, Vector2 direction)
        {
            if (health <= 0)
                return;
            health -= damage;
            Knockback(direction);
            if (health <= 0)
                gameObject.SetActive(false);
        }

        public override void Knockback(Vector2 push)
        {
            if (!gameObject.activeSelf)
                return;
            float away = push.x;
            if (Mathf.Abs(away) < .01f)
            {
                Adventure game = Adventure.Instance;
                away = direction;
                if (game != null && game.Player != null)
                    away = transform.position.x - game.Player.transform.position.x;
            }

            if (Mathf.Abs(away) < .01f)
                away = 1;
            recoil = new Vector2(Mathf.Sign(away) * knockbackSpeed, 0);
            stagger = .22f;
            windup = 0;
            Aggro = true;
            decisionTimer = 0;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Peasant player = other.GetComponent<Peasant>();
            if (stagger <= 0 && player != null)
                player.Hurt(1, transform.position, this);
        }
    }
}
