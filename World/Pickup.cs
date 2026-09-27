using UnityEngine;

namespace FinalFreedom
{
    public sealed class Pickup : MonoBehaviour
    {
        public PickupKind Kind { get; private set; }
        Transform model;
        float baseY;
        public void Initialize(PickupKind kind, Transform model) { Kind = kind; this.model = model; }
        public void Spawn(int lane, float z)
        {
            baseY = 1.1f;
            transform.position = new Vector3(GameConfig.LaneX(lane), baseY, z);
            gameObject.SetActive(true);
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying) return;
            var player = game.Player;
            model.Rotate(0, 120 * Time.deltaTime, 0, Space.World);
            model.localPosition = Vector3.up * (Mathf.Sin(Time.time * 4 + transform.position.z) * 0.12f);
            if (Kind == PickupKind.Coin && player.MagnetActive && Vector3.Distance(transform.position, player.transform.position) < 17f)
            {
                // Hút trên cả 4 làn, thống nhất với hệ đường 4 làn của game.
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position + Vector3.up * 0.4f, 35f * Time.deltaTime);
                if (Vector3.Distance(transform.position, player.transform.position + Vector3.up * 0.4f) < 1f) Take();
            }
            if (transform.position.z < player.transform.position.z - 20f) gameObject.SetActive(false);
        }
        void OnTriggerEnter(Collider other) { if (other.gameObject.layer == GameLayers.Player) Take(); }
        void Take()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false); GameManager.Instance.Collect(Kind);
        }
    }
}
