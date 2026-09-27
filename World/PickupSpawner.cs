using UnityEngine;

namespace FinalFreedom
{
    public sealed class PickupSpawner : MonoBehaviour
    {
        ObjectPooler[] pools;
        GameManager game;
        float nextZ;
        int group;
        public void Initialize(GameManager manager)
        {
            game = manager; pools = new ObjectPooler[6];
            for (int i = 0; i < pools.Length; i++)
            {
                var kind = (PickupKind)i;
                pools[i] = new ObjectPooler(game.World, () => Create(kind), i == 0 ? 80 : 3, i == 0 ? 140 : 6);
            }
        }
        GameObject Create(PickupKind kind)
        {
            var go = new GameObject(kind.ToString()); go.layer = GameLayers.Pickup;
            var trigger = go.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = kind == PickupKind.Coin ? 0.62f : 0.8f;
            var model = new GameObject("Visual").transform; model.SetParent(go.transform, false);
            Color color = kind == PickupKind.Coin ? ProceduralArt.Gold : kind == PickupKind.Repair ? new Color(0.3f, 0.95f, 0.51f) : kind == PickupKind.DoubleScore ? new Color(1, 0.3f, 0.58f) : ProceduralArt.Cyan;
            if (kind == PickupKind.Coin)
            {
                var disk = ProceduralArt.Part("Coin", model, Vector3.zero, new Vector3(0.66f, 0.09f, 0.66f), color, PrimitiveType.Cylinder);
                disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
                ProceduralArt.Part("Coin mark", model, new Vector3(0, 0, -0.1f), new Vector3(0.1f, 0.32f, 0.05f), new Color(1, 0.9f, 0.45f));
            }
            else
            {
                var block = ProceduralArt.Part("Powerup", model, Vector3.zero, Vector3.one * 0.85f, color);
                block.transform.localRotation = Quaternion.Euler(0, 0, 15);
                var text = new GameObject("Icon", typeof(TextMesh)); text.transform.SetParent(model, false); text.transform.localPosition = new Vector3(0, 0, -0.5f);
                text.transform.localRotation = Quaternion.identity;
                var mesh = text.GetComponent<TextMesh>();
                mesh.font = Resources.Load<Font>("FinalFreedomFont");
                if (mesh.font != null) text.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
                mesh.text = new[] { "$", "M", "N", "S", "+", "x2" }[(int)kind];
                mesh.fontSize = 48; mesh.characterSize = 0.055f; mesh.anchor = TextAnchor.MiddleCenter; mesh.color = Color.white;
            }
            go.AddComponent<Pickup>().Initialize(kind, model); return go;
        }
        void Place(PickupKind kind, int lane, float z)
        {
            if (Physics.CheckBox(new Vector3(GameConfig.LaneX(lane), 1, z), new Vector3(1, 0.6f, 2), Quaternion.identity, GameLayers.Sensors, QueryTriggerInteraction.Ignore)) return;
            var go = pools[(int)kind].Rent(); if (go != null) go.GetComponent<Pickup>().Spawn(lane, z);
        }
        public void SeedOpening() { for (int i = 0; i < 7; i++) Place(PickupKind.Coin, 2, 18 + i * 5); Place(PickupKind.Shield, 2, 58); }
        public void ResetWorld() { foreach (var pool in pools) pool.ReturnAll(); nextZ = 85; group = 0; }
        void Update()
        {
            if (!game.IsPlaying || nextZ > game.Player.transform.position.z + 180) return;
            int lane = Random.Range(0, 4);
            for (int i = 0; i < 6; i++) Place(PickupKind.Coin, lane, nextZ + i * 4.8f);
            if (group % 2 == 0) Place((PickupKind)(1 + group / 2 % 5), Random.Range(0, 4), nextZ + 34);
            group++; nextZ += 52;
        }
        public void Shift(float shift) { nextZ -= shift; }
    }
}
