using UnityEngine;

namespace FinalFreedom
{
    public sealed class PoliceAI : MonoBehaviour
    {
        public VehicleDamage Damage { get; private set; }
        VehicleMotor motor;
        GameManager game;
        int lane;
        float nextDecision, flawUntil;
        public void Spawn(GameManager manager, int startLane, float z)
        {
            game = manager; lane = startLane; nextDecision = Time.time + 1; flawUntil = 0;
            motor = GetComponent<VehicleMotor>(); Damage = GetComponent<VehicleDamage>();
            Damage.ResetHealth();
            motor.ResetMotion(new Vector3(GameConfig.LaneX(lane), 0.64f, z), 1, game.CruiseSpeed);
            gameObject.SetActive(true);
        }
        void FixedUpdate()
        {
            if (!game.IsPlaying || Damage.IsDead) return;
            if (Time.time >= nextDecision)
            {
                nextDecision = Time.time + Mathf.Lerp(0.65f, 0.25f, game.Difficulty);
                float obstacle = motor.Sense(game.Config.policeSensor, out var hit);
                bool blocked = hit.collider != null && hit.collider.gameObject.layer != GameLayers.Player;
                if (blocked && Time.time >= flawUntil)
                {
                    if (Random.value < game.Config.policeFlawRate) flawUntil = Time.time + 1.2f;
                    else
                    {
                        int first = Random.value < 0.5f ? -1 : 1;
                        for (int i = 0; i < 2; i++)
                        {
                            int candidate = lane + (i == 0 ? first : -first);
                            if (candidate >= 0 && candidate <= 3 && motor.LaneIsClear(candidate)) { lane = candidate; break; }
                        }
                    }
                }
                else if (!blocked && Mathf.Abs(game.Player.transform.position.z - transform.position.z) > 6)
                {
                    int wanted = game.Player.Lane;
                    int candidate = lane + System.Math.Sign(wanted - lane);
                    if (candidate != lane && motor.LaneIsClear(candidate, 5f)) lane = candidate;
                }
            }
            float gap = game.Player.transform.position.z - transform.position.z;
            float speed = game.CruiseSpeed + Mathf.Clamp(gap * 0.3f, -10f, 6f + game.Difficulty * 3f);
            // Lúc nitro người chơi có thể cắt đuôi; không teleport xe vào vùng camera.
            motor.Drive(GameConfig.LaneX(lane), Mathf.Max(8, speed));
            if (gap > 160 || gap < -100 || transform.position.y < -4 || Mathf.Abs(transform.position.x) > 15)
                Damage.ApplyDamage(9999, true);
        }
    }
}
