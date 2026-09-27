using UnityEngine;

namespace FinalFreedom
{
    public sealed class PoliceDirector : MonoBehaviour
    {
        public PoliceAI[] Units { get; private set; }
        private int maxPoliceUnits = 5;
        private float[] readyAt;
        private float[] timeToSpawn;
        private float timePerSpawn = 5f;
        GameManager game;
        public void Initialize(GameManager manager)
        {
            game = manager;
            Units = new PoliceAI[maxPoliceUnits];
            this.readyAt = new float[maxPoliceUnits];
            timeToSpawn = new float[maxPoliceUnits];
            for (int i = 0; i < maxPoliceUnits; i++)
            {
                timeToSpawn[i] = timePerSpawn * (i + 1);
                readyAt[i] = float.PositiveInfinity;
            }

            for (int i = 0; i < Units.Length; i++)
            {
                var go = VehicleFactory.Create(VehicleKind.Police);
                go.transform.SetParent(game.World, false);
                Units[i] = go.AddComponent<PoliceAI>();
                int slot = i;
                go.GetComponent<VehicleDamage>().Destroyed += _ => readyAt[slot] = Time.time + Random.Range(game.Config.policeRespawnMin, game.Config.policeRespawnMax);
                go.SetActive(false);
            }
        }
        public void ResetWorld()
        {
            for (int i = 0; i < Units.Length; i++) { Units[i].gameObject.SetActive(false); readyAt[i] = float.PositiveInfinity; }
        }
        public void StartPursuit()
        {
            for (int i = 0; i < this.maxPoliceUnits; i++) readyAt[i] = Time.time + timeToSpawn[i];
        }
        void Update()
        {
            if (!game.IsPlaying) return;
            int permitted = 1 +
                (game.Elapsed >= timeToSpawn[1] ? 1 : 0) +
                (game.Elapsed >= timeToSpawn[2] ? 1 : 0) +
                (game.Elapsed >= timeToSpawn[3] ? 1 : 0) +
                (game.Elapsed >= timeToSpawn[4] ? 1 : 0);

            for (int i = 0; i < permitted; i++)
            {
                if (Units[i].gameObject.activeSelf || Time.time < readyAt[i]) continue;
                int lane = (game.Player.Lane + i) % 4;
                Units[i].Spawn(game, lane, game.Player.transform.position.z - 32f - i * 13f);
                readyAt[i] = float.PositiveInfinity;
                game.UI.Toast("POLICE IN PURSUIT", 1.5f);
            }
        }
    }
}
