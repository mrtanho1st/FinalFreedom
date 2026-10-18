using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalFreedom
{
    // Small shared meshes: pooled pickups never allocate new geometry when respawning.
    public static class PickupArt
    {
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Color Red = new Color(0.95f, 0.12f, 0.14f);
        static readonly Color Silver = new Color(0.87f, 0.94f, 1f);
        static readonly Color Blue = new Color(0.08f, 0.43f, 0.95f);

        public static void Create(PickupKind kind, Transform parent)
        {
            switch (kind)
            {
                case PickupKind.Magnet:
                    // Lower semicircular band and upright arms leave a real open U silhouette.
                    MeshPart("Magnet curved base", parent, MagnetMesh(), Red);
                    foreach (float x in new[] { -0.38f, 0.38f })
                    {
                        Box("Magnet arm", parent, new Vector3(x, 0.10f, 0), new Vector3(0.24f, 0.46f, 0.28f), Red);
                        Box("Silver pole", parent, new Vector3(x, 0.42f, 0), new Vector3(0.24f, 0.18f, 0.28f), Silver);
                    }
                    break;
                case PickupKind.Boost:
                    MeshPart("Rocket body", parent, Lathe("Rocket body", new[] { new Vector2(0.18f, -0.28f), new Vector2(0.24f, -0.12f), new Vector2(0.24f, 0.28f) }), Silver);
                    MeshPart("Rocket nose", parent, Lathe("Rocket nose", new[] { new Vector2(0.24f, 0.28f), new Vector2(0, 0.63f) }), Red);
                    MeshPart("Rocket exhaust", parent, Lathe("Rocket exhaust", new[] { new Vector2(0.12f, -0.32f), new Vector2(0.13f, -0.48f), new Vector2(0, -0.72f) }), new Color(1, 0.48f, 0.06f));
                    var fin = Polygon("Rocket fin", new[] { new Vector2(0.14f, -0.3f), new Vector2(0.46f, -0.4f), new Vector2(0.40f, -0.12f), new Vector2(0.18f, 0.04f) }, 0.08f, new Vector2(0.27f, -0.2f));
                    for (int i = 0; i < 4; i++) MeshPart("Rocket fin", parent, fin, Red).localRotation = Quaternion.Euler(0, i * 90, 0);
                    foreach (float z in new[] { -0.235f, 0.235f })
                    {
                        var rim = ProceduralArt.Part("Porthole rim", parent, new Vector3(0, 0.1f, z), new Vector3(0.25f, 0.025f, 0.25f), ProceduralArt.Ink, PrimitiveType.Cylinder);
                        rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
                        var glass = ProceduralArt.Part("Porthole glass", parent, new Vector3(0, 0.1f, z * 1.12f), new Vector3(0.18f, 0.012f, 0.18f), ProceduralArt.Cyan, PrimitiveType.Cylinder);
                        glass.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    }
                    break;
                case PickupKind.Shield:
                    var shield = Polygon("Shield", new[] { new Vector2(0, -0.62f), new Vector2(0.42f, -0.25f), new Vector2(0.51f, 0.37f), new Vector2(0, 0.57f), new Vector2(-0.51f, 0.37f), new Vector2(-0.42f, -0.25f) }, 0.22f, Vector2.zero);
                    MeshPart("Shield silver rim", parent, shield, Silver);
                    var face = MeshPart("Shield blue inset", parent, shield, Blue);
                    face.localScale = new Vector3(0.8f, 0.8f, 1.18f);
                    foreach (float z in new[] { -0.15f, 0.15f })
                    {
                        Box("Shield crest vertical", parent, new Vector3(0, 0.04f, z), new Vector3(0.09f, 0.62f, 0.035f), Silver);
                        Box("Shield crest horizontal", parent, new Vector3(0, 0.13f, z), new Vector3(0.48f, 0.09f, 0.035f), Silver);
                    }
                    break;
                case PickupKind.Repair:
                    var cross = Polygon("Repair cross", new[] { new Vector2(-0.17f, -0.54f), new Vector2(0.17f, -0.54f), new Vector2(0.17f, -0.17f), new Vector2(0.54f, -0.17f), new Vector2(0.54f, 0.17f), new Vector2(0.17f, 0.17f), new Vector2(0.17f, 0.54f), new Vector2(-0.17f, 0.54f), new Vector2(-0.17f, 0.17f), new Vector2(-0.54f, 0.17f), new Vector2(-0.54f, -0.17f), new Vector2(-0.17f, -0.17f) }, 0.24f, Vector2.zero);
                    MeshPart("Green repair cross", parent, cross, new Color(0.13f, 0.9f, 0.3f));
                    break;
                case PickupKind.DoubleScore:
                    var points = new Vector2[10];
                    for (int i = 0; i < points.Length; i++)
                    {
                        float angle = (90 + i * 36) * Mathf.Deg2Rad;
                        points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? 0.65f : 0.34f);
                    }
                    MeshPart("Golden score star", parent, Polygon("Score star", points, 0.22f, Vector2.zero), ProceduralArt.Gold);
                    // Geometry lettering stays legible on both sides, using the same city fog/bend shader.
                    for (int side = 0; side < 2; side++)
                    {
                        var label = new GameObject("x2").transform; label.SetParent(parent, false);
                        label.localRotation = Quaternion.Euler(0, side * 180, 0);
                        label.localPosition = new Vector3(0, 0, side == 0 ? -0.13f : 0.13f);
                        Stroke(label, new Vector2(-0.22f, -0.1f), new Vector2(-0.07f, 0.1f));
                        Stroke(label, new Vector2(-0.22f, 0.1f), new Vector2(-0.07f, -0.1f));
                        var digit = new[] { new Vector2(0.03f, 0.13f), new Vector2(0.19f, 0.13f), new Vector2(0.23f, 0.08f), new Vector2(0.21f, 0.02f), new Vector2(0.03f, -0.11f), new Vector2(0.23f, -0.11f) };
                        for (int i = 1; i < digit.Length; i++) Stroke(label, digit[i - 1], digit[i]);
                    }
                    break;
            }
        }

        static void Box(string name, Transform parent, Vector3 position, Vector3 size, Color color)
            => ProceduralArt.Part(name, parent, position, size, color);
        static void Stroke(Transform parent, Vector2 from, Vector2 to)
        {
            var bar = ProceduralArt.Part("Letter stroke", parent, (from + to) * 0.5f, new Vector3(0.043f, Vector2.Distance(from, to) + 0.025f, 0.028f), ProceduralArt.Ink);
            bar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg - 90);
        }
        static Transform MeshPart(string name, Transform parent, Mesh mesh, Color color)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = GameLayers.Scenery; go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = ProceduralArt.Mat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go.transform;
        }
        // All callers use CCW outlines star-shaped about the specified interior point.
        static Mesh Polygon(string key, Vector2[] outline, float depth, Vector2 center)
        {
            if (meshes.TryGetValue(key, out var cached)) return cached;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < outline.Length; i++)
            {
                Vector3 a = outline[i], b = outline[(i + 1) % outline.Length], c = center;
                Vector3 offset = Vector3.forward * depth * 0.5f;
                Triangle(vertices, triangles, c - offset, b - offset, a - offset);
                Triangle(vertices, triangles, c + offset, a + offset, b + offset);
                Quad(vertices, triangles, a - offset, b - offset, b + offset, a + offset);
            }
            return Store(key, vertices, triangles);
        }
        static Mesh MagnetMesh()
        {
            const string key = "Magnet U bend";
            if (meshes.TryGetValue(key, out var cached)) return cached;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                float a = Mathf.PI + i * Mathf.PI / 16, b = Mathf.PI + (i + 1) * Mathf.PI / 16;
                Vector3 innerA = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.26f + Vector3.down * 0.13f;
                Vector3 outerA = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.5f + Vector3.down * 0.13f;
                Vector3 innerB = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0) * 0.26f + Vector3.down * 0.13f;
                Vector3 outerB = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0) * 0.5f + Vector3.down * 0.13f;
                Vector3 d = Vector3.forward * 0.14f;
                Quad(vertices, triangles, innerA - d, innerB - d, outerB - d, outerA - d);
                Quad(vertices, triangles, innerA + d, outerA + d, outerB + d, innerB + d);
                Quad(vertices, triangles, outerA - d, outerB - d, outerB + d, outerA + d);
                Quad(vertices, triangles, innerB - d, innerA - d, innerA + d, innerB + d);
            }
            return Store(key, vertices, triangles);
        }
        static Mesh Lathe(string key, Vector2[] profile)
        {
            if (meshes.TryGetValue(key, out var cached)) return cached;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            // Rings ordered bottom to top, including axis end caps.
            var rings = new List<Vector2> { new Vector2(0, profile[0].y) };
            rings.AddRange(profile); rings.Add(new Vector2(0, profile[profile.Length - 1].y));
            if (rings[0].y > rings[rings.Count - 1].y) rings.Reverse();
            for (int j = 1; j < rings.Count; j++)
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8, b = (i + 1) * Mathf.PI / 8;
                Vector3 Ring(Vector2 p, float angle) => new Vector3(p.x * Mathf.Cos(angle), p.y, p.x * Mathf.Sin(angle));
                Quad(vertices, triangles, Ring(rings[j - 1], b), Ring(rings[j - 1], a), Ring(rings[j], a), Ring(rings[j], b));
            }
            return Store(key, vertices, triangles);
        }
        static void Triangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }
        static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Triangle(vertices, triangles, a, b, c); Triangle(vertices, triangles, a, c, d); }
        static Mesh Store(string key, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = key };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes.Add(key, mesh); return mesh;
        }
        public static void Dispose()
        {
            foreach (var mesh in meshes.Values)
                if (Application.isPlaying) Object.Destroy(mesh); else Object.DestroyImmediate(mesh);
            meshes.Clear();
        }
    }
}
