using UnityEngine;

namespace FinalFreedom
{
    public sealed class RoadObstacle : MonoBehaviour
    {
        public bool IsPothole { get; set; }
        void OnTriggerEnter(Collider other)
        {
            if (!IsPothole || other.gameObject.layer != GameLayers.Player) return;
            var player = other.GetComponent<PlayerController>();
            if (player != null) player.SlowFromPothole();
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game != null && transform.position.z < game.Player.transform.position.z - 45) gameObject.SetActive(false);
        }
    }
}
