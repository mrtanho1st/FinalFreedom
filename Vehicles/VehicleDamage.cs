using System;
using UnityEngine;

namespace FinalFreedom
{
    [RequireComponent(typeof(Rigidbody), typeof(VehicleMotor))]
    public sealed class VehicleDamage : MonoBehaviour
    {
        public float MaxHP { get; private set; }
        public float HP { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasShield { get; set; }
        public bool Invulnerable { get; set; }
        public bool IsPlayer { get; set; }
        public event Action<VehicleDamage> Destroyed;
        public float Fraction => MaxHP > 0 ? HP / MaxHP : 0;
        VehicleMotor motor;
        VehicleVisual visual;
        float corpseTime, shieldGrace;

        public void Configure(float hp, bool player)
        {
            motor = GetComponent<VehicleMotor>(); visual = GetComponent<VehicleVisual>();
            MaxHP = hp; IsPlayer = player; ResetHealth(hp);
        }
        public void ResetHealth(float maxHP = -1)
        {
            if (maxHP > 0) MaxHP = maxHP;
            HP = MaxHP; IsDead = false; corpseTime = 0; HasShield = false; Invulnerable = false; shieldGrace = 0;
            if (visual != null) visual.SetBurnt(false);
        }
        public void Repair(float hp) { if (!IsDead) HP = Mathf.Min(MaxHP, HP + hp); }
        public void ApplyDamage(float damage, bool bypassProtection = false)
        {
            if (IsDead || damage <= 0) return;
            if (!bypassProtection)
            {
                if (Invulnerable || Time.time < shieldGrace) return;
                if (HasShield)
                {
                    HasShield = false; shieldGrace = Time.time + 0.3f;
                    GameManager.Instance.Effects.Burst(transform.position, new Color(0.2f, 0.95f, 1), 14);
                    return;
                }
            }
            HP = Mathf.Max(0, HP - damage);
            if (IsPlayer) GameManager.Instance.Chase.Shake(Mathf.Clamp(damage / 80f, 0.1f, 0.65f));
            if (HP <= 0) Die();
        }
        void OnCollisionEnter(Collision collision)
        {
            if (IsDead || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            int layer = collision.gameObject.layer;
            if (layer != GameLayers.Traffic && layer != GameLayers.Player && layer != GameLayers.Police && layer != GameLayers.Obstacle) return;
            float speed = collision.relativeVelocity.magnitude;
            var config = GameManager.Instance.Config;
            if (speed < config.minimumImpactSpeed) return;
            var other = collision.rigidbody;
            float mass = other != null ? other.mass : 4500f;
            bool protectedNow = Invulnerable || HasShield || Time.time < shieldGrace;
            ApplyDamage(config.Damage(mass, speed));
            if (!protectedNow) motor.Stun(Mathf.Clamp(speed * 0.025f, 0.2f, 0.9f));
            // PhysX xử lý xung lượng chính. Lực phụ bằng nhau/ngược chiều chỉ áp dụng một lần/cặp.
            if (other != null && other.GetComponent<VehicleDamage>() != null && GetInstanceID() < other.GetComponent<VehicleDamage>().GetInstanceID())
            {
                var contact = collision.GetContact(0);
                Vector3 normal = contact.normal; normal.y = 0;
                normal.Normalize();
                float impulse = Mathf.Min(motor.Body.mass, mass) * Mathf.Min(speed, 35f) * 0.10f;
                motor.Body.AddForceAtPosition(normal * impulse, contact.point, ForceMode.Impulse);
                other.AddForceAtPosition(-normal * impulse, contact.point, ForceMode.Impulse);
                if (Mathf.Abs(normal.x) > 0.55f)
                {
                    Rigidbody light = motor.Body.mass < mass ? motor.Body : other;
                    float ratio = Mathf.Max(mass, motor.Body.mass) / Mathf.Min(mass, motor.Body.mass);
                    light.AddTorque(Vector3.up * Mathf.Sign(normal.x) * impulse * Mathf.Clamp(ratio, 1f, 3f), ForceMode.Impulse);
                }
            }
            if (IsPlayer || other == null || GetInstanceID() < other.GetInstanceID())
                GameManager.Instance.Effects.Burst(collision.GetContact(0).point, new Color(1, 0.68f, 0.16f), 8);
        }
        void Die()
        {
            IsDead = true; corpseTime = Time.time + 2.6f;
            visual.SetBurnt(true);
            motor.Body.constraints = RigidbodyConstraints.None;
            motor.Body.AddExplosionForce(10000, transform.position + Vector3.down * 1.2f - Vector3.forward, 6, 1.5f, ForceMode.Impulse);
            motor.Body.AddTorque(new Vector3(2000, 900, 2600), ForceMode.Impulse);
            GameManager.Instance.Effects.Explode(transform.position);
            Destroyed?.Invoke(this);
            if (IsPlayer) GameManager.Instance.EndRun();
        }
        void Update()
        {
            if (IsDead && !IsPlayer && Time.time >= corpseTime) gameObject.SetActive(false);
        }
    }
}
