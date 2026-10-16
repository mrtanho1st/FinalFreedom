using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FinalFreedom
{
    // One live preview for the focused list entry, rather than 42 cameras rendering at once.
    public sealed class GaragePreview : MonoBehaviour
    {
        public const int PreviewLayer = 31;
        readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        GameObject rig;
        Transform pivot;
        Camera view;
        RawImage output;
        RenderTexture target;
        float radius;
        public Bounds ModelBounds { get; private set; }
        public string VehicleId { get; private set; }

        public void Initialize(RawImage image, Transform owner)
        {
            output = image;
            rig = new GameObject("Garage preview rig");
            rig.transform.SetParent(owner, false);
            rig.transform.localPosition = new Vector3(0, -1000, 0);
            view = new GameObject("Garage preview camera").AddComponent<Camera>();
            view.transform.SetParent(rig.transform, false);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.035f, 0.065f, 0.09f);
            view.cullingMask = 1 << PreviewLayer;
            view.orthographic = true;
            view.nearClipPlane = 0.1f; view.farClipPlane = 60;
            view.allowHDR = false; view.allowMSAA = false;
            view.depth = -10;
        }

        public void Show(PlayerVehicle vehicle)
        {
            if (pivot != null) { pivot.gameObject.SetActive(false); Destroy(pivot.gameObject); }
            ClearMaterials();
            pivot = new GameObject("Preview model").transform;
            pivot.SetParent(rig.transform, false);
            var artwork = PrefabArt.Instantiate(vehicle.prefab, pivot);
            ModelBounds = PrefabArt.Fit(artwork, vehicle.maximumSize, Vector3.zero);
            // Centre the model at the turntable origin; use exactly the gameplay model's scale.
            artwork.localPosition -= ModelBounds.center;
            foreach (var child in artwork.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
            foreach (var renderer in artwork.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++)
                {
                    if (!materials.TryGetValue(source[i], out var material))
                    {
                        material = new Material(source[i]);
                        material.SetFloat("_WorldEffects", 0);
                        materials.Add(source[i], material);
                    }
                    source[i] = material;
                }
                renderer.sharedMaterials = source;
            }
            radius = ModelBounds.extents.magnitude;
            pivot.localRotation = Quaternion.Euler(0, -25, 0);
            view.transform.localPosition = new Vector3(0, 12, 20);
            view.transform.LookAt(rig.transform.position);
            VehicleId = vehicle.id;
            Resize();
        }

        void Resize()
        {
            var rect = output.rectTransform.rect;
            float aspect = Mathf.Max(0.1f, rect.width / Mathf.Max(1, rect.height));
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 256, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(width / aspect), 128, 1024);
            if (target == null || target.width != width || target.height != height)
            {
                ReleaseTarget();
                target = new RenderTexture(width, height, 24) { name = "Garage preview texture" };
                target.Create();
                view.targetTexture = target; output.texture = target;
            }
            view.aspect = aspect;
            view.orthographicSize = Mathf.Max(2.8f, radius * 1.12f / Mathf.Min(1, aspect));
        }
        void LateUpdate()
        {
            if (pivot == null) return;
            Resize();
            pivot.Rotate(Vector3.up, 18 * Time.unscaledDeltaTime, Space.Self);
        }
        void OnDisable() { if (rig != null) rig.SetActive(false); }
        void OnEnable() { if (rig != null) rig.SetActive(true); }
        void ClearMaterials()
        {
            foreach (var material in materials.Values) Destroy(material);
            materials.Clear();
        }
        void ReleaseTarget()
        {
            if (view != null) view.targetTexture = null;
            if (output != null) output.texture = null;
            if (target == null) return;
            target.Release(); Destroy(target); target = null;
        }
        void OnDestroy()
        {
            ReleaseTarget(); ClearMaterials();
            if (rig != null) Destroy(rig);
        }
    }
}
