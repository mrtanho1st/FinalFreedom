using UnityEngine;

namespace FinalFreedom
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleMotor : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public float Length { get; set; }
        public float Direction { get; set; } = 1f;

        [Header("Hồi góc xe")]

        [Tooltip("Tốc độ xoay về hướng ban đầu, đơn vị độ/giây.")]
        [SerializeField, Min(1f)]
        private float rotationReturnSpeed = 40f;

        [Tooltip("Sai lệch nhỏ hơn ngưỡng này thì ngừng chỉnh góc.")]
        [SerializeField, Range(0.01f, 2f)]
        private float rotationDeadZone = 0.1f;

        private float stunnedUntil;

        public bool Stunned => Time.time < stunnedUntil;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        public void ResetMotion(
            Vector3 position,
            float direction,
            float initialSpeed)
        {
            if (Body == null)
                Body = GetComponent<Rigidbody>();

            Direction = direction;
            stunnedUntil = 0f;

            Body.isKinematic = false;

            // Cho phép xoay Y, khóa nghiêng/lật khi xe đang hoạt động.
            Body.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            Quaternion rotation = Quaternion.Euler(
                0f,
                Direction > 0f ? 0f : 180f,
                0f
            );

            Body.position = position;
            Body.rotation = rotation;

            transform.SetPositionAndRotation(position, rotation);

            Body.linearVelocity = new Vector3(
                0f,
                0f,
                Direction * initialSpeed
            );

            Body.angularVelocity = Vector3.zero;
            Body.WakeUp();
        }

        public void Stun(float seconds)
        {
            stunnedUntil = Mathf.Max(
                stunnedUntil,
                Time.time + seconds
            );
        }

        // Gọi hàm này từ FixedUpdate của controller.
        public void Drive(float laneX, float forwardSpeed)
        {
            if (Body == null || Body.isKinematic || Stunned)
                return;

            // ===== DI CHUYỂN =====

            Vector3 velocity = Body.linearVelocity;
            float gain = GameManager.Instance.Config.laneResponse;

            float targetX = Mathf.Clamp(
                (laneX - Body.position.x) * gain,
                -8f,
                8f
            );

            Vector3 acceleration = new Vector3(
                Mathf.Clamp(
                    (targetX - velocity.x) * 8f,
                    -40f,
                    40f
                ),
                0f,
                Mathf.Clamp(
                    (forwardSpeed * Direction - velocity.z) * 3f,
                    -24f,
                    12f
                )
            );

            Body.AddForce(acceleration, ForceMode.Acceleration);

            // ===== HỒI GÓC =====

            RestoreHeading();
        }

        private void RestoreHeading()
        {
            float wantedYaw = Direction > 0f ? 0f : 180f;

            Quaternion currentRotation = Body.rotation;
            float currentYaw = currentRotation.eulerAngles.y;

            // Tính sai lệch theo đường xoay ngắn nhất,
            // xử lý đúng cả trường hợp 359° -> 0°.
            float deltaYaw = Mathf.DeltaAngle(
                currentYaw,
                wantedYaw
            );

            // Loại bỏ vận tốc xoay Y còn dư để nó không cộng
            // thêm vào góc xoay do MoveRotation điều khiển.
            // Trong thời gian Stunned, Drive đã return nên
            // xe vẫn được xoay tự do do va chạm.
            Vector3 angularVelocity = Body.angularVelocity;

            if (angularVelocity.y != 0f)
            {
                angularVelocity.y = 0f;
                Body.angularVelocity = angularVelocity;
            }

            // Xe đã gần thẳng: không ghi rotation mỗi bước nữa.
            if (Mathf.Abs(deltaYaw) <= rotationDeadZone)
                return;

            // Tốc độ cố định theo độ/giây.
            float maxStep =
                rotationReturnSpeed * Time.fixedDeltaTime;

            // Không xoay vượt quá góc đích.
            float yawStep = Mathf.Clamp(
                deltaYaw,
                -maxStep,
                maxStep
            );

            // Chỉ bổ sung chuyển động xoay quanh trục Y thế giới.
            Quaternion nextRotation =
                Quaternion.AngleAxis(yawStep, Vector3.up)
                * currentRotation;

            Body.MoveRotation(nextRotation);
        }

        public float Sense(float range, out RaycastHit nearest)
        {
            nearest = default;
            float best = range;

            Vector3 nose =
                Body.position
                + Vector3.up * 0.15f
                + Vector3.forward * Direction
                * (Length * 0.5f + 0.15f);

            for (int i = -1; i <= 1; i++)
            {
                Vector3 from =
                    nose + Vector3.right * i * 0.55f;

                if (Physics.Raycast(
                        from,
                        Vector3.forward * Direction,
                        out RaycastHit hit,
                        range,
                        GameLayers.Sensors,
                        QueryTriggerInteraction.Ignore)
                    && hit.rigidbody != Body
                    && hit.distance < best)
                {
                    best = hit.distance;
                    nearest = hit;
                }
            }

            return best;
        }

        public bool LaneIsClear(int lane, float reach = 9f)
        {
            Vector3 center = new Vector3(
                GameConfig.LaneX(lane),
                Body.position.y,
                Body.position.z + Direction * 2f
            );

            return !Physics.CheckBox(
                center,
                new Vector3(1.2f, 0.6f, reach),
                Quaternion.identity,
                GameLayers.Sensors,
                QueryTriggerInteraction.Ignore
            );
        }
    }
}