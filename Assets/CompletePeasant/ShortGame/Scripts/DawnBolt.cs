using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class DawnBolt : MonoBehaviour
    {
        public Vector2 direction = Vector2.right;
        public float speed = 14;
        public int damage = 10;
        private float life = 2.5f;
        private bool spent;
        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.Player == null || game.ui.IsOpen || game.Loading || game.Player.Dead)
            { body.velocity = Vector2.zero; return; }
            body.velocity = direction * speed;
            life -= Time.deltaTime;
            if (life <= 0) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (spent || other.GetComponentInParent<Peasant>() != null) return;
            SunSeal seal = other.GetComponentInParent<SunSeal>();
            Damageable target = other.GetComponentInParent<Damageable>();
            if (seal != null) seal.Open();
            else if (target != null) target.Hit(damage, direction);
            else if (other.gameObject.layer != 8) return;
            spent = true;
            Destroy(gameObject);
        }
    }
}
