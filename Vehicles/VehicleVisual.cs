using UnityEngine;

namespace FinalFreedom
{
    public sealed class VehicleVisual : MonoBehaviour
    {
        Renderer[] paint, brakes, all;
        Transform[] wheels;
        Renderer red, blue;
        GameObject flames, shield;
        MaterialPropertyBlock properties;
        VehicleMotor motor;
        bool braking, burnt;
        public void Configure(Transform model, Renderer[] paint, Transform[] wheels, Renderer[] brakes, VehicleKind kind)
        {
            this.paint = paint; this.wheels = wheels; this.brakes = brakes;
            properties = new MaterialPropertyBlock(); motor = GetComponent<VehicleMotor>();
            if (kind == VehicleKind.Police)
            {
                red = ProceduralArt.Part("Red beacon", model, new Vector3(-0.35f, 1.2f, -0.2f), new Vector3(0.5f, 0.16f, 0.3f), Color.red).GetComponent<Renderer>();
                blue = ProceduralArt.Part("Blue beacon", model, new Vector3(0.35f, 1.2f, -0.2f), new Vector3(0.5f, 0.16f, 0.3f), Color.cyan).GetComponent<Renderer>();
                ProceduralArt.Part("Police stripe", model, new Vector3(0, 0.15f, 0), new Vector3(1.93f, 0.18f, 2.5f), ProceduralArt.Ink);
            }
            if (kind == VehicleKind.Player)
            {
                flames = new GameObject("Nitro exhaust"); flames.transform.SetParent(model, false);
                for (int s = -1; s <= 1; s += 2)
                    ProceduralArt.Part("Flame", flames.transform, new Vector3(s * 0.58f, 0, -2.35f), new Vector3(0.25f, 0.25f, 1.1f), ProceduralArt.Cyan);
                flames.SetActive(false);
                shield = new GameObject("Shield corners"); shield.transform.SetParent(model, false);
                for (int s = -1; s <= 1; s += 2)
                    ProceduralArt.Part("Guard", shield.transform, new Vector3(s * 1.12f, 0.2f, 0), new Vector3(0.06f, 0.12f, 4.3f), ProceduralArt.Cyan);
                shield.SetActive(false);
            }
            all = model.GetComponentsInChildren<Renderer>(true);
        }
        void Tint(Renderer renderer, Color color, float glow = 0)
        {
            properties.Clear(); properties.SetColor("_BaseColor", color); properties.SetFloat("_Glow", glow);
            renderer.SetPropertyBlock(properties);
        }
        public void SetPlayerColor(int index)
        {
            Color c = index == 0 ? ProceduralArt.Cyan : index == 1 ? new Color(1, 0.36f, 0.23f) : new Color(0.48f, 0.56f, 0.69f);
            foreach (var renderer in paint) Tint(renderer, c);
        }
        public void SetBrake(bool enabled)
        {
            braking = enabled;
            foreach (var b in brakes) Tint(b, enabled ? new Color(1, 0.06f, 0.02f) : new Color(0.32f, 0.025f, 0.018f), enabled ? 0.8f : 0);
        }
        public void SetNitro(bool enabled) { if (flames != null) flames.SetActive(enabled && !burnt); }
        public void SetShield(bool enabled) { if (shield != null) shield.SetActive(enabled && !burnt); }
        public void SetBurnt(bool enabled)
        {
            burnt = enabled;
            foreach (var renderer in all) { if (enabled) Tint(renderer, new Color(0.07f, 0.065f, 0.06f)); else renderer.SetPropertyBlock(null); }
            SetBrake(false); SetNitro(false); SetShield(false);
        }
        void Update()
        {
            if (motor == null || burnt) return;
            float turn = motor.Body.linearVelocity.magnitude * Time.deltaTime * 150f;
            foreach (var wheel in wheels) wheel.Rotate(Vector3.up, turn, Space.Self);
            if (red != null)
            {
                bool flash = ((int)(Time.time * 8) & 1) == 0;
                Tint(red, new Color(flash ? 1 : 0.25f, 0.025f, 0.06f), flash ? 0.8f : 0);
                Tint(blue, new Color(0.04f, 0.4f, flash ? 0.3f : 1), flash ? 0 : 0.8f);
            }
            if (flames != null && flames.activeSelf) flames.transform.localScale = new Vector3(1, 1, 0.85f + Mathf.Sin(Time.time * 50) * 0.15f);
        }
    }
}
