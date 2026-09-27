using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FinalFreedom
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            // Scene chỉ cần Bootstrap; mọi liên kết được gán ở đây, không kéo thả Inspector.
            if (GameManager.Instance != null) return;
            var config = Resources.Load<GameConfig>("GameConfig");
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            var pipeline = Resources.Load<UniversalRenderPipelineAsset>("FinalFreedomURP");
            if (pipeline != null)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
            }
            Time.timeScale = 1;
            Time.fixedDeltaTime = 1f / 50f;
            Application.targetFrameRate = config.targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 4;
            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(GameLayers.Scenery, i, true);
                Physics.IgnoreLayerCollision(GameLayers.Pickup, i, i != GameLayers.Player);
            }
            var game = gameObject.AddComponent<GameManager>();
            game.Initialize(config);
        }
    }
}
