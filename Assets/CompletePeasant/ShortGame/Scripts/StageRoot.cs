using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public class StageRoot : MonoBehaviour
    {
        public StageData data;
        public Transform spawn;
        public Transform[] checkpoints;
        public float leftEdge = -14, rightEdge = 84;

        public Vector3 CheckpointPosition(int index)
        {
            if (checkpoints != null && checkpoints.Length > 0)
                return checkpoints[Mathf.Clamp(index, 0, checkpoints.Length - 1)].position;
            return spawn.position;
        }
    }
}
