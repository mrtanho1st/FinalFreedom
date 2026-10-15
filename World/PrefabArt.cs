using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalFreedom
{
    public static class PrefabArt
    {
        // Fit uniformly, including child transforms and off-centre pivots. Never stretch the artwork.
        // The returned bounds are in the parent's local space, useful for gameplay colliders.
        public static Bounds Fit(Transform model, Vector3 maximumSize, Vector3 groundCenter)
        {
            var parent = model.parent;
            var bounds = Measure(model, parent);
            var size = bounds.size;
            if (Mathf.Min(size.x, size.y, size.z) < 0.0001f)
                throw new InvalidOperationException("Prefab has empty mesh bounds: " + model.name);
            float scale = Mathf.Min(maximumSize.x / size.x, maximumSize.y / size.y, maximumSize.z / size.z);
            model.localScale *= scale;
            bounds = Measure(model, parent);
            model.localPosition += groundCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return Measure(model, parent);
        }

        public static Bounds Measure(Transform model, Transform relativeTo)
        {
            var result = new Bounds();
            bool found = false;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var meshBounds = filter.sharedMesh.bounds;
                var matrix = (relativeTo != null ? relativeTo.worldToLocalMatrix : Matrix4x4.identity)
                    * filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = matrix.MultiplyPoint3x4(corner);
                    if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        public static Transform Instantiate(GameObject prefab, Transform parent)
        {
            if (prefab == null) throw new InvalidOperationException("Missing city prefab reference.");
            var model = UnityEngine.Object.Instantiate(prefab, parent, false).transform;
            model.localPosition = Vector3.zero;
            foreach (var child in model.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = GameLayers.Scenery;
            // Prefabs supply visuals only; the game owns physics and collision layers.
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true))
            { body.isKinematic = true; body.detectCollisions = false; }
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = ProceduralArt.PrefabMaterial(materials[i]);
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return model;
        }
    }
}
