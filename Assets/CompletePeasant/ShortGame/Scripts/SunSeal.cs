using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class SunSeal : MonoBehaviour
    {
        public GameObject barrier;
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (barrier != null) barrier.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}
