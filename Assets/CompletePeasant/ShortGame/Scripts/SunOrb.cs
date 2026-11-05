using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class SunOrb : MonoBehaviour
    {
        public Vector2 direction;
        public float speed = 6;
        public bool Reflected { get; private set; }
        private float life = 4;
        private bool spent;

        private void Update()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.ui.IsOpen || game.Loading || game.Player.Dead) return;
            transform.position += (Vector3)direction * speed * Time.deltaTime;
            life -= Time.deltaTime;
            if (life <= 0) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (spent) return;
            Peasant player = other.GetComponent<Peasant>();
            if (player != null)
            {
                if (Reflected) return;
                if (player.TryParry(transform.position))
                {
                    Reflected = true; speed *= 1.5f; life = 4;
                    direction = -direction;
                    SpriteRenderer art = GetComponentInChildren<SpriteRenderer>();
                    if (art != null) art.color = new Color(1, .85f, .35f);
                    return;
                }
                player.Hurt(1, transform.position);
                spent = true; Destroy(gameObject);
                return;
            }
            Damageable target = other.GetComponentInParent<Damageable>();
            if (Reflected && target != null)
            {
                spent = true; Destroy(gameObject);
            }
            else if (other.gameObject.layer == 8) { spent = true; Destroy(gameObject); }
        }
    }
}
