using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class Thorns : MonoBehaviour
    {
        private void OnTriggerStay2D(Collider2D other)
        { Peasant player = other.GetComponent<Peasant>(); if (player != null) player.Hazard(); }
    }
}
