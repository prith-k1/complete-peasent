using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class FollowPlayer : MonoBehaviour
    {
        private Vector3 velocity;
        private Camera view;
        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            Adventure game = Adventure.Instance;
            if (game == null || game.Player == null || game.Stage == null)
                return;
            float half = view.orthographicSize * view.aspect;
            float x = Mathf.Clamp(game.Player.transform.position.x + 1.5f, game.Stage.leftEdge + half, game.Stage.rightEdge - half);
            Vector3 target = new Vector3(x, Mathf.Clamp(game.Player.transform.position.y + 2, 3.8f, 8), -10);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 0.18f);
        }
    }
}
