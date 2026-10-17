using UnityEngine;

namespace FinalFreedom
{
    // Pooled visual ballistics: cones cannot inflict extra collision damage after being knocked away.
    // Positions stay relative to the obstacle so floating-origin shifts also move the complete effect.
    public sealed class TrafficConeImpact : MonoBehaviour
    {
        BoxCollider trigger;
        Transform[] cones;
        Vector3[] restPositions, velocities, spins;
        Quaternion[] restRotations;
        bool[] settled;
        float age;
        public bool Hit { get; private set; }

        public void Initialize(BoxCollider trigger, Transform[] cones)
        {
            this.trigger = trigger; this.cones = cones;
            restPositions = new Vector3[cones.Length]; restRotations = new Quaternion[cones.Length];
            velocities = new Vector3[cones.Length]; spins = new Vector3[cones.Length]; settled = new bool[cones.Length];
            for (int i = 0; i < cones.Length; i++)
            { restPositions[i] = cones[i].localPosition; restRotations[i] = cones[i].localRotation; }
            ResetCones();
        }
        void OnEnable() { if (cones != null) ResetCones(); }
        void OnDisable() { if (cones != null) ResetCones(); }
        void ResetCones()
        {
            Hit = false; age = 0; trigger.enabled = true;
            for (int i = 0; i < cones.Length; i++)
            {
                cones[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
                velocities[i] = spins[i] = Vector3.zero; settled[i] = false;
            }
        }
        void OnTriggerEnter(Collider other)
        {
            var game = GameManager.Instance;
            if (Hit || game == null || !game.IsPlaying || other.attachedRigidbody != game.Player.Motor.Body
                || game.Player.Damage.IsDead || game.Player.BoostActive) return;
            Hit = true; trigger.enabled = false;
            float speed = game.Player.Speed;
            float impactX = transform.InverseTransformPoint(game.Player.transform.position).x;
            for (int i = 0; i < cones.Length; i++)
            {
                float side = Mathf.Clamp(restPositions[i].x - impactX, -1.8f, 1.8f);
                velocities[i] = new Vector3(side * 2.5f, 4.5f + i * 0.65f + Mathf.Min(speed * 0.05f, 2),
                    Mathf.Clamp(speed * 0.7f, 5, 28) + i * 0.7f);
                spins[i] = new Vector3(300 + i * 75, side * 100, -side * 230);
            }
            game.Player.HitTrafficCones();
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (!Hit || game == null || !game.IsPlaying) return;
            float dt = Time.deltaTime;
            age += dt;
            for (int i = 0; i < cones.Length; i++)
            {
                if (settled[i]) continue;
                velocities[i] += Vector3.down * (14 * dt);
                cones[i].localPosition += velocities[i] * dt;
                cones[i].Rotate(spins[i] * dt, Space.Self);
                // Keep rotated mesh bounds above the road and allow a small bounce before settling.
                float bottom = PrefabArt.Measure(cones[i], transform).min.y;
                if (bottom < 0)
                {
                    cones[i].localPosition += Vector3.up * -bottom;
                    if (velocities[i].y < 0) velocities[i].y *= -0.28f;
                    velocities[i].x *= 0.65f; velocities[i].z *= 0.65f; spins[i] *= 0.6f;
                    if (age > 1.5f && velocities[i].y < 0.7f) settled[i] = true;
                }
            }
            if (age > 3.5f) gameObject.SetActive(false);
        }
    }
}
