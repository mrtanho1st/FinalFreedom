using UnityEngine;

namespace FinalFreedom
{
    public sealed class RoadObstacle : MonoBehaviour
    {
        void Update()
        {
            var game = GameManager.Instance;
            if (game != null && transform.position.z < game.Player.transform.position.z - 45) gameObject.SetActive(false);
        }
    }
}
