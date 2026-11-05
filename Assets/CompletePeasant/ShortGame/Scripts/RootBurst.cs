using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class RootBurst : MonoBehaviour
    {
        public SpriteRenderer sprite;
        public Collider2D hitbox;
        public float windup = .85f, duration = .65f;
        private float time;
        private Vector3 fullScale, fullPosition;

        private void Awake()
        {
            fullScale = sprite.transform.localScale;
            fullPosition = sprite.transform.localPosition;
            hitbox.enabled = false;
            sprite.transform.localScale = new Vector3(fullScale.x, fullScale.y * .05f, fullScale.z);
            sprite.transform.localPosition = new Vector3(fullPosition.x, .06f, fullPosition.z);
            sprite.color = new Color(1, .7f, .2f, .8f);
        }

        private void Update()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.ui.IsOpen || game.Loading || game.Player.Dead) return;
            time += Time.deltaTime;
            if (time >= windup)
            {
                hitbox.enabled = true;
                sprite.transform.localScale = fullScale;
                sprite.transform.localPosition = fullPosition;
                sprite.color = new Color(.65f, .9f, .5f);
            }
            if (time >= windup + duration) Destroy(gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Peasant player = other.GetComponent<Peasant>();
            if (player != null) player.Hurt(1, transform.position);
        }
    }
}
