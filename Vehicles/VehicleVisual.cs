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
        bool burnt;
        float wheelRadius;
        VehicleKind kind;
        int playerColor;
        public void Configure(Transform model, Renderer[] paint, Transform[] wheels, Renderer[] brakes, VehicleKind kind, Bounds bounds)
        {
            this.paint = paint; this.wheels = wheels; this.brakes = brakes;
            this.kind = kind;
            wheelRadius = wheels.Length > 0 ? Mathf.Max(0.1f, PrefabArt.Measure(wheels[0], model).extents.y) : 0.3f;
            properties = new MaterialPropertyBlock(); motor = GetComponent<VehicleMotor>();
            if (kind == VehicleKind.Police)
            {
                red = ProceduralArt.Part("Red beacon glow", model, new Vector3(-bounds.size.x * 0.18f, bounds.max.y, 0), new Vector3(bounds.size.x * 0.25f, 0.08f, 0.22f), Color.red).GetComponent<Renderer>();
                blue = ProceduralArt.Part("Blue beacon glow", model, new Vector3(bounds.size.x * 0.18f, bounds.max.y, 0), new Vector3(bounds.size.x * 0.25f, 0.08f, 0.22f), Color.cyan).GetComponent<Renderer>();
            }
            if (kind == VehicleKind.Player)
            {
                flames = new GameObject("Nitro exhaust"); flames.transform.SetParent(model, false);
                for (int s = -1; s <= 1; s += 2)
                    ProceduralArt.Part("Flame", flames.transform, new Vector3(s * bounds.size.x * 0.3f, bounds.min.y + 0.25f, bounds.min.z - 0.45f), new Vector3(0.25f, 0.25f, 1.1f), ProceduralArt.Cyan);
                flames.SetActive(false);
                shield = new GameObject("Shield corners"); shield.transform.SetParent(model, false);
                for (int s = -1; s <= 1; s += 2)
                    ProceduralArt.Part("Guard", shield.transform, new Vector3(s * (bounds.extents.x + 0.15f), bounds.center.y, 0), new Vector3(0.06f, 0.12f, bounds.size.z + 0.4f), ProceduralArt.Cyan);
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
            if (kind != VehicleKind.Player) return;
            var variants = CityPrefabLibrary.Instance.playerCars;
            playerColor = Mathf.Clamp(index, 0, variants.Length - 1);
            // Changing the atlas preserves tyres, windows and headlights when selecting a paint colour.
            var sources = variants[playerColor].GetComponentsInChildren<Renderer>();
            foreach (var renderer in paint)
                foreach (var source in sources)
                    if (source.name == renderer.name)
                    {
                        var materials = source.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = ProceduralArt.PrefabMaterial(materials[i]);
                        renderer.sharedMaterials = materials;
                        if (!burnt) renderer.SetPropertyBlock(null);
                        break;
                    }
        }
        public void SetBrake(bool enabled)
        {
            foreach (var b in brakes) Tint(b, enabled ? new Color(1, 0.06f, 0.02f) : new Color(0.32f, 0.025f, 0.018f), enabled ? 0.8f : 0);
        }
        public void SetNitro(bool enabled) { if (flames != null) flames.SetActive(enabled && !burnt); }
        public void SetShield(bool enabled) { if (shield != null) shield.SetActive(enabled && !burnt); }
        public void SetBurnt(bool enabled)
        {
            burnt = enabled;
            foreach (var renderer in all) { if (enabled) Tint(renderer, new Color(0.07f, 0.065f, 0.06f)); else renderer.SetPropertyBlock(null); }
            SetBrake(false); SetNitro(false); SetShield(false);
            if (!enabled && kind == VehicleKind.Player) SetPlayerColor(playerColor);
        }
        void Update()
        {
            if (motor == null || burnt) return;
            float turn = Vector3.Dot(motor.Body.linearVelocity, transform.forward) * Time.deltaTime / wheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in wheels) wheel.Rotate(Vector3.right, turn, Space.Self);
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
