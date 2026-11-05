using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public abstract class Damageable : MonoBehaviour
    {
        public abstract void Hit(int damage, Vector2 direction);
        public virtual void Knockback(Vector2 direction) { }
    }
}
