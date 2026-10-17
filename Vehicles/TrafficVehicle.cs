using UnityEngine;

namespace FinalFreedom
{
    public sealed class TrafficVehicle : MonoBehaviour
    {
        public int Lane { get; private set; }
        VehicleMotor motor;
        VehicleDamage damage;
        VehicleVisual visual;
        float cruise, nextSense, sensedSpeed;
        public void Spawn(int lane, float z, float normalSpeed)
        {
            motor = GetComponent<VehicleMotor>(); damage = GetComponent<VehicleDamage>(); visual = GetComponent<VehicleVisual>();
            Lane = lane; cruise = sensedSpeed = normalSpeed; nextSense = 0;
            damage.ResetHealth();
            motor.ResetMotion(new Vector3(GameConfig.LaneX(lane), 0.64f, z), lane < 2 ? -1 : 1, normalSpeed);
            visual.SetBrake(false); gameObject.SetActive(true);
        }
        void FixedUpdate()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying || damage.IsDead) return;
            if (Time.time >= nextSense)
            {
                nextSense = Time.time + 0.1f;
                float speed = Mathf.Abs(motor.Body.linearVelocity.z);
                // Cones are triggers so the player can knock them away. Include them only
                // in civilian sensing; the Sensors mask still excludes pickups/scenery.
                // Look far enough ahead to stop at high speed without a solid collider.
                const float braking = 12f, clearance = 0.7f, reaction = 0.5f;
                float range = Mathf.Max(12f, speed * speed / (2f * braking) + speed * reaction + clearance);
                float distance = motor.Sense(range, out var hit, QueryTriggerInteraction.Collide);
                // SafeDistance = 10 m; khoảng trống 0.7 m bảo vệ ở vận tốc rất thấp.
                sensedSpeed = cruise * Mathf.Clamp01((distance - 0.7f) / 10f);
                if (hit.collider != null && hit.collider.TryGetComponent<TrafficConeImpact>(out _))
                {
                    float stoppingSpace = Mathf.Max(0, distance - clearance - speed * reaction);
                    sensedSpeed = Mathf.Min(sensedSpeed, Mathf.Sqrt(2f * braking * stoppingSpace));
                }
                visual.SetBrake(sensedSpeed < cruise * 0.85f);
            }
            motor.Drive(GameConfig.LaneX(Lane), sensedSpeed);
            float delta = transform.position.z - game.Player.transform.position.z;
            if (delta < -55 || delta > 330 || transform.position.y < -5 || Mathf.Abs(transform.position.x) > 16)
                gameObject.SetActive(false);
        }
    }
}
