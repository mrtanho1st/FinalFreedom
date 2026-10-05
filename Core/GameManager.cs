using UnityEngine;

namespace FinalFreedom
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameConfig Config { get; private set; }
        public RunState State { get; private set; } = RunState.Menu;
        public bool IsPlaying => State == RunState.Playing;
        public PlayerController Player { get; private set; }
        public SwipeManager Input { get; private set; }
        public UIManager UI { get; private set; }
        public PoliceDirector Police { get; private set; }
        public EffectsManager Effects { get; private set; }
        public ChaseCamera Chase { get; private set; }
        public Transform World { get; private set; }
        public float Elapsed { get; private set; }
        public float Distance { get; private set; }
        public float Score { get; private set; }
        public int Coins { get; private set; }
        public float Difficulty => Mathf.Clamp01(Elapsed / 160f);
        public float CruiseSpeed => Mathf.Min(Config.maxSpeed, Config.baseSpeed + Config.speedGainPerSecond * Elapsed) + SaveService.SpeedBonus[SaveService.Selected];
        public float ScoreMultiplier => (Player.Lane < 2 ? 2f : 1f) * (Player.DoubleScoreActive ? 2f : 1f);
        TrackGenerator track;
        TrafficSpawner traffic;
        PickupSpawner pickups;
        ObstacleSpawner obstacles;
        float previousZ;
        bool banked;

        public void Initialize(GameConfig config)
        {

            Instance = this; Config = config;
            World = new GameObject("WORLD - floating origin").transform;
            World.SetParent(transform, false);
            ProceduralArt.Initialize();
            Input = gameObject.AddComponent<SwipeManager>();
            Effects = gameObject.AddComponent<EffectsManager>(); Effects.Initialize(this);
            track = gameObject.AddComponent<TrackGenerator>(); track.Initialize(this);
            var playerObject = VehicleFactory.Create(VehicleKind.Player);
            playerObject.transform.SetParent(World, false);
            Player = playerObject.AddComponent<PlayerController>(); Player.Initialize(this);
            var cam = new GameObject("Chase Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.SetParent(transform, false);
            Chase = cam.AddComponent<ChaseCamera>(); Chase.Initialize(this);
            traffic = gameObject.AddComponent<TrafficSpawner>(); traffic.Initialize(this);
            obstacles = gameObject.AddComponent<ObstacleSpawner>(); obstacles.Initialize(this);
            pickups = gameObject.AddComponent<PickupSpawner>(); pickups.Initialize(this);
            Police = gameObject.AddComponent<PoliceDirector>(); Police.Initialize(this);
            UI = gameObject.AddComponent<UIManager>(); UI.Initialize(this);
            ToMenu();
        }

        public void BeginRun()
        {
            Time.timeScale = 1;
            banked = false; Elapsed = Distance = Score = 0; Coins = 0;
            Random.InitState(Config.randomSeed + (int)(Time.realtimeSinceStartup * 100));
            Input.Clear();
            traffic.ResetWorld(); obstacles.ResetWorld(); pickups.ResetWorld(); Effects.ResetEffects(); Police.ResetWorld();
            track.ResetWorld();
            Player.ResetForRun(); previousZ = Player.transform.position.z;
            State = RunState.Playing;
            traffic.SeedOpening(); pickups.SeedOpening();
            Police.StartPursuit();
            Physics.SyncTransforms();
            Chase.Snap();
            UI.ShowHUD(); UI.Toast("ESCAPE THE CITY", 2f);
            this.Effects.PlayMusic();
        }
        public void ToMenu()
        {
            if (State == RunState.Playing || State == RunState.Paused) Bank();
            Time.timeScale = 1;
            State = RunState.Menu;
            Input.Clear();
            traffic.ResetWorld();
            obstacles.ResetWorld();
            pickups.ResetWorld();
            Police.ResetWorld();
            Effects.ResetEffects();
            track.ResetWorld();
            Player.ResetForRun();
            previousZ = 0;
            Chase.Snap(); UI.ShowMenu();
            this.Effects.StopMusic();
        }
        public void EndRun()
        {
            if (!IsPlaying) return;
            State = RunState.GameOver; Input.Clear(); Bank();
            UI.ShowGameOver();
            this.Effects.StopMusic();
        }
        void Bank()
        {
            if (banked) return;
            banked = true; SaveService.BankRun(Coins, Mathf.FloorToInt(Score));
        }
        public void TogglePause()
        {
            if (State == RunState.Playing)
            {
                this.Effects.PauseMusic();
                State = RunState.Paused;
                Time.timeScale = 0; Input.Clear();
                UI.ShowPause();
            }
            else if (State == RunState.Paused)
            {
                this.Effects.PlayMusic();
                State = RunState.Playing;
                Time.timeScale = 1;
                Input.Clear(); UI.ShowHUD();
            }
        }
        public void Collect(PickupKind kind)
        {
            if (!IsPlaying) return;
            if (kind == PickupKind.Coin) { Coins++; Score += 10; }
            else { Player.ApplyPowerup(kind); UI.Toast(kind.ToString().ToUpperInvariant(), 1.6f); }
            Effects.PlayPickup(kind != PickupKind.Coin);
        }
        void Update()
        {
            if (!IsPlaying) return;
            Elapsed += Time.deltaTime;
            float z = Player.transform.position.z;
            float advance = Mathf.Max(0, z - previousZ);
            previousZ = z;
            Distance += advance; Score += advance * ScoreMultiplier;
            if (Player.transform.position.y < -4 || Mathf.Abs(Player.transform.position.x) > 11)
                Player.Damage.ApplyDamage(9999, true);
        }
        void FixedUpdate()
        {
            // Dịch cả world cùng một lượng: giữ độ chính xác float khi chạy hàng chục km.
            if (!IsPlaying || Player.transform.position.z < 800f) return;
            const float shift = 640f;
            foreach (Transform child in World)
            {
                var body = child.GetComponent<Rigidbody>();
                if (body != null) body.position -= Vector3.forward * shift;
                else child.position -= Vector3.forward * shift;
            }
            previousZ -= shift;
            track.Shift(shift); traffic.Shift(shift); obstacles.Shift(shift); pickups.Shift(shift);
            Chase.Shift(shift); Effects.Shift(shift);
            Physics.SyncTransforms();
        }
        void OnApplicationPause(bool paused)
        {
            if (paused && IsPlaying) TogglePause();
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (Instance == this) { Instance = null; ProceduralArt.Dispose(); }
        }
    }
}
