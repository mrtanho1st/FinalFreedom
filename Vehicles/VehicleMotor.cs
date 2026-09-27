using UnityEngine;

namespace FinalFreedom
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleMotor : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public float Length { get; set; }
        public float Direction { get; set; } = 1;
        float stunnedUntil;
        public bool Stunned => Time.time < stunnedUntil;
        void Awake() { Body = GetComponent<Rigidbody>(); }
        public void ResetMotion(Vector3 position, float direction, float initialSpeed)
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            Direction = direction; stunnedUntil = 0;
            Body.isKinematic = false;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.position = position;
            Body.rotation = Quaternion.Euler(0, direction > 0 ? 0 : 180, 0);
            transform.SetPositionAndRotation(position, Body.rotation);
            Body.linearVelocity = new Vector3(0, 0, direction * initialSpeed);
            Body.angularVelocity = Vector3.zero;
            Body.WakeUp();
        }
        public void Stun(float seconds) { stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds); }
        public void Drive(float laneX, float forwardSpeed)
        {
            if (Stunned) return; // Không ghi đè lực va chạm bằng vận tốc AI trong cùng frame.
            var velocity = Body.linearVelocity;
            float gain = GameManager.Instance.Config.laneResponse;
            float targetX = Mathf.Clamp((laneX - Body.position.x) * gain, -8f, 8f);
            var accel = new Vector3(Mathf.Clamp((targetX - velocity.x) * 8f, -40f, 40f), 0,
                Mathf.Clamp((forwardSpeed * Direction - velocity.z) * 3f, -24f, 12f));
            Body.AddForce(accel, ForceMode.Acceleration);
            float wantedYaw = Direction > 0 ? 0 : 180;
            float delta = Mathf.DeltaAngle(Body.rotation.eulerAngles.y, wantedYaw);
            Body.AddTorque(Vector3.up * (delta * 0.6f - Body.angularVelocity.y * 7f), ForceMode.Acceleration);
        }
        public float Sense(float range, out RaycastHit nearest)
        {
            // Ba tia xuất phát ngoài mũi xe: không chạm collider của chính mình.
            nearest = default;
            float best = range;
            Vector3 nose = Body.position + Vector3.up * 0.15f + Vector3.forward * Direction * (Length * 0.5f + 0.15f);
            for (int i = -1; i <= 1; i++)
            {
                Vector3 from = nose + Vector3.right * i * 0.55f;
                if (Physics.Raycast(from, Vector3.forward * Direction, out var hit, range, GameLayers.Sensors, QueryTriggerInteraction.Ignore)
                    && hit.rigidbody != Body && hit.distance < best)
                { best = hit.distance; nearest = hit; }
            }
            return best;
        }
        public bool LaneIsClear(int lane, float reach = 9f)
        {
            var center = new Vector3(GameConfig.LaneX(lane), Body.position.y, Body.position.z + Direction * 2f);
            return !Physics.CheckBox(center, new Vector3(1.2f, 0.6f, reach), Quaternion.identity,
                GameLayers.Sensors, QueryTriggerInteraction.Ignore);
        }
    }
}
