using UnityEngine;

namespace FinalFreedom
{
    public sealed class EffectsManager : MonoBehaviour
    {
        readonly ParticleSystem[] particles = new ParticleSystem[12];
        readonly float[] until = new float[12];
        GameManager game;
        AudioSource engine, siren, oneShot, music;
        AudioClip coin, powerup, explosion;
        Material particleMaterial;
        float nextCoin;
        float musicVolume = 0.3f;
        public bool Muted { get; private set; }
        public void Initialize(GameManager manager)
        {
            game = manager;
            particleMaterial = new Material(Resources.Load<Shader>("CityParticles"));
            for (int i = 0; i < particles.Length; i++)
            {
                var go = new GameObject("Pooled sparks " + i); go.transform.SetParent(game.World, false);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1.2f;
                main.startLifetime = 0.8f; main.startSpeed = 7f; main.startSize = 0.28f; main.maxParticles = 40;
                main.gravityModifier = 0.5f; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = ps.emission; emission.enabled = false;
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.5f;
                var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
                particles[i] = ps; go.SetActive(false);
            }
            engine = MakeAudio("Engine", "Audio/engine", true, 0.15f);
            siren = MakeAudio("Siren", "Audio/siren", true, 0);
            oneShot = MakeAudio("SFX", null, false, 0.65f);
            this.music = MakeAudio("Music", "Audio/music", true, this.musicVolume);
            coin = Resources.Load<AudioClip>("Audio/coin");
            powerup = Resources.Load<AudioClip>("Audio/powerup");
            explosion = Resources.Load<AudioClip>("Audio/explosion");
            Muted = PlayerPrefs.GetInt("FinalFreedom.muted", 0) == 1;
        }
        AudioSource MakeAudio(string name, string path, bool loop, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = volume;
            if (path != null) { source.clip = Resources.Load<AudioClip>(path); source.Play(); }
            return source;
        }
        public void ToggleMute()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt("FinalFreedom.muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void Burst(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].gameObject.activeSelf) continue;
                var ps = particles[i]; ps.transform.position = position; ps.gameObject.SetActive(true);
                ps.Clear(); ps.Play();
                var emit = new ParticleSystem.EmitParams { startColor = color };
                ps.Emit(emit, count); until[i] = Time.time + 1.3f; break;
            }
        }
        public void Explode(Vector3 position)
        {
            Burst(position, new Color(1, 0.38f, 0.06f), 36);
            if (!Muted) oneShot.PlayOneShot(explosion, 0.8f);
        }
        public void PlayPickup(bool special)
        {
            if (Muted || (!special && Time.unscaledTime < nextCoin)) return;
            nextCoin = Time.unscaledTime + 0.045f;
            oneShot.PlayOneShot(special ? powerup : coin, special ? 0.8f : 0.35f);
        }

        public void PlayMusic()
        {
            if (!music.isPlaying) music.Play();
        }

        public void PauseMusic()
        {
            if (music.isPlaying) music.Pause();
        }

        public void StopMusic()
        {
            this.music.Stop();
            this.music.time = 0f;
        }

        public void ResetEffects()
        {
            foreach (var ps in particles) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); }
            oneShot.Stop();
        }
        public void Shift(float shift) { /* ParticleSystem dùng Local space, tự dịch cùng world. */ }
        void Update()
        {
            for (int i = 0; i < particles.Length; i++)
                if (particles[i].gameObject.activeSelf && Time.time > until[i]) particles[i].gameObject.SetActive(false);
            bool play = game.IsPlaying && !Muted;
            engine.volume = play ? 0.12f : 0;
            engine.pitch = 0.55f + game.Player.Speed / 45f;
            float nearest = 200;
            if (game.Police != null)
                foreach (var unit in game.Police.Units)
                    if (unit.gameObject.activeSelf && !unit.Damage.IsDead) nearest = Mathf.Min(nearest, Vector3.Distance(unit.transform.position, game.Player.transform.position));
            siren.volume = play ? Mathf.Clamp01(1 - nearest / 70f) * 0.16f : 0;
            this.music.volume = play ? this.musicVolume : 0;
            oneShot.mute = Muted || game.State == RunState.Paused;
        }
        void OnDestroy() { if (particleMaterial != null) Destroy(particleMaterial); }
    }
}
