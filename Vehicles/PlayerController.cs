using UnityEngine;

namespace FinalFreedom
{
    public sealed class PlayerController : MonoBehaviour
    {
        public int Lane { get; private set; } = 2;
        public float Nitro { get; private set; } = 1000f;
        public float maxNitro => 1000f;
        public VehicleDamage Damage { get; private set; }
        public VehicleMotor Motor { get; private set; }
        public bool NitroActive { get; private set; }
        public bool MagnetActive => magnetLeft > 0;
        public bool BoostActive => boostLeft > 0;
        public bool DoubleScoreActive => doubleLeft > 0;
        public float MagnetTime => magnetLeft;
        public float BoostTime => boostLeft;
        public float DoubleTime => doubleLeft;
        public float Speed => Mathf.Abs(Motor.Body.linearVelocity.z);
        GameManager game;
        VehicleVisual visual;
        float brakeLeft, magnetLeft, boostLeft, doubleLeft, depletedDelay;
        bool ghostCollisions;
        public void Initialize(GameManager manager)
        {
            game = manager; Motor = GetComponent<VehicleMotor>(); Damage = GetComponent<VehicleDamage>();
            visual = GetComponent<VehicleVisual>();
        }
        public void ResetForRun()
        {
            Lane = 2; Nitro = this.maxNitro;
            brakeLeft = magnetLeft = boostLeft = doubleLeft = depletedDelay = 0;
            NitroActive = false;
            SetGhost(false);
            Motor.ResetMotion(new Vector3(GameConfig.LaneX(Lane), 0.64f, 0), 1, 0);
            Damage.ResetHealth(SaveService.Health[SaveService.Selected]);
            visual.SetPlayerColor(SaveService.Selected);
        }
        public void MoveLane(int direction) { if (game.IsPlaying) Lane = Mathf.Clamp(Lane + direction, 0, 3); }
        public void Brake() { if (game.IsPlaying) brakeLeft = 1.25f; }
        public void SlowFromPothole() { if (!BoostActive) { brakeLeft = Mathf.Max(brakeLeft, 1.1f); game.Chase.Shake(0.12f); } }
        public void ApplyPowerup(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Magnet: magnetLeft = game.Config.magnetSeconds; break;
                case PickupKind.Boost: boostLeft = game.Config.boostSeconds; Nitro = this.maxNitro; SetGhost(true); break;
                case PickupKind.Shield: Damage.HasShield = true; break;
                case PickupKind.Repair: Damage.Repair(30); break;
                case PickupKind.DoubleScore: doubleLeft = game.Config.doubleScoreSeconds; break;
            }
        }
        void Update()
        {
            if (!game.IsPlaying)
            {
                NitroActive = false;
                visual.SetNitro(false);
                return;
            }
            float dt = Time.deltaTime;
            magnetLeft = Mathf.Max(0, magnetLeft - dt); boostLeft = Mathf.Max(0, boostLeft - dt); doubleLeft = Mathf.Max(0, doubleLeft - dt);
            brakeLeft = Mathf.Max(0, brakeLeft - dt); depletedDelay = Mathf.Max(0, depletedDelay - dt);
            bool held = game.Input.HoldingNitro;
            NitroActive = BoostActive || (held && Nitro > 0 && depletedDelay <= 0 && brakeLeft <= 0);

            if (!BoostActive && NitroActive)
            {
                Nitro = Mathf.Max(0, Nitro - game.Config.nitroDrain * dt);
                if (Nitro == 0) depletedDelay = 1.5f;
            }
            else if (!held)
            {
                Nitro = Mathf.Min(this.maxNitro, Nitro + game.Config.nitroRefill * dt);
            }

            Damage.Invulnerable = BoostActive;
            SetGhost(BoostActive);
            visual.SetNitro(NitroActive);
            visual.SetShield(Damage.HasShield || BoostActive);
            visual.SetBrake(brakeLeft > 0);
        }
        void SetGhost(bool enabled)
        {
            if (ghostCollisions == enabled) return;
            ghostCollisions = enabled;
            Physics.IgnoreLayerCollision(GameLayers.Player, GameLayers.Traffic, enabled);
            Physics.IgnoreLayerCollision(GameLayers.Player, GameLayers.Police, enabled);
            Physics.IgnoreLayerCollision(GameLayers.Player, GameLayers.Obstacle, enabled);
        }
        void OnDestroy() { SetGhost(false); }
        void FixedUpdate()
        {
            if (!game.IsPlaying || Damage.IsDead) return;
            float speed = game.CruiseSpeed * (NitroActive ? game.Config.nitroMultiplier : 1f) * (brakeLeft > 0 ? 0.45f : 1f);
            Motor.Drive(GameConfig.LaneX(Lane), speed);
        }
    }
}
