using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalFreedom
{
    public static class ProceduralArt
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static readonly List<Mesh> combinedMeshes = new List<Mesh>();
        public static readonly Color Asphalt = new Color(0.10f, 0.16f, 0.20f);
        public static readonly Color Gold = new Color(1f, 0.69f, 0.13f);
        public static readonly Color Cyan = new Color(0.14f, 0.9f, 0.95f);
        public static readonly Color Ink = new Color(0.035f, 0.055f, 0.085f);
        public static PhysicsMaterial VehiclePhysics { get; private set; }
        public static void Initialize()
        {
            VehiclePhysics = new PhysicsMaterial("Low friction body") { dynamicFriction = 0.08f, staticFriction = 0.1f, bounciness = 0.08f,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
        }
        public static Material Mat(Color color)
        {
            if (materials.TryGetValue(color, out var mat)) return mat;
            // Resources bảo đảm shader có mặt trong APK, không bị shader stripping.
            Shader shader = Resources.Load<Shader>("CurvedCity");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            mat = new Material(shader) { name = "City " + ColorUtility.ToHtmlStringRGB(color), enableInstancing = true };
            mat.SetColor("_BaseColor", color);
            materials[color] = mat;
            return mat;
        }
        public static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Color color, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = Mat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.layer = GameLayers.Scenery;
            return go;
        }
        public static void MergeStatic(Transform root)
        {
            // Gộp mesh theo material, giảm draw calls cho mỗi đoạn thành phố.
            var batches = new Dictionary<Material, List<CombineInstance>>();
            var originalMeshes = root.GetComponentsInChildren<MeshFilter>();
            foreach (var filter in originalMeshes)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                var mat = renderer.sharedMaterial;
                if (!batches.ContainsKey(mat)) batches[mat] = new List<CombineInstance>();
                batches[mat].Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                renderer.enabled = false;
            }
            foreach (var batch in batches)
            {
                var go = new GameObject("Combined city mesh", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false); go.layer = GameLayers.Scenery;
                var mesh = new Mesh { name = "City chunk", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(batch.Value.ToArray()); mesh.RecalculateBounds();
                // Shader bẻ cong có thể đi ra ngoài bounds cũ ở xa.
                var bounds = mesh.bounds; bounds.Expand(new Vector3(80, 100, 0)); mesh.bounds = bounds;
                go.GetComponent<MeshFilter>().sharedMesh = mesh; combinedMeshes.Add(mesh);
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = batch.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var filter in originalMeshes)
            {
                filter.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(filter.gameObject); else Object.DestroyImmediate(filter.gameObject);
            }
        }
        public static void Dispose()
        {
            foreach (var mat in materials.Values) Object.Destroy(mat);
            foreach (var mesh in combinedMeshes) Object.Destroy(mesh);
            materials.Clear(); combinedMeshes.Clear();
            if (VehiclePhysics != null) Object.Destroy(VehiclePhysics);
        }
    }
}
