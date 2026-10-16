using UnityEngine;

namespace FinalFreedom
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        GameManager game;
        Camera view;
        float shake;
        Vector3 smoothing;
        static readonly int CityBend = Shader.PropertyToID("_CityBend");
        static readonly int CityFogColor = Shader.PropertyToID("_CityFogColor");
        static readonly int CityFogParams = Shader.PropertyToID("_CityFogParams");
        public void Initialize(GameManager manager)
        {
            game = manager; view = GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.nearClipPlane = 0.15f; view.farClipPlane = 310; view.fieldOfView = 62;
            view.allowHDR = false; view.allowMSAA = true;
            view.cullingMask &= ~(1 << GaragePreview.PreviewLayer);
            Snap();
        }
        Vector3 Desired()
        {
            var p = game.Player.transform.position;
            bool portrait = view.aspect < 1;
            float extra = Mathf.Max(0, game.Player.Motor.Length - 4.2f) * 0.55f;
            return new Vector3(p.x * 0.32f, (portrait ? 9.5f : 7.3f) + extra * 0.5f,
                p.z - (portrait ? 18f : 13f) - extra);
        }
        public void Snap() { transform.position = Desired(); smoothing = Vector3.zero; shake = 0; Aim(); UpdateAtmosphere(); }
        void Aim()
        {
            Vector3 p = game.Player.transform.position;
            transform.rotation = Quaternion.LookRotation(new Vector3(p.x * 0.3f, 0.8f, p.z + 13f) - transform.position);
        }
        public void Shift(float shift) { transform.position -= Vector3.forward * shift; UpdateAtmosphere(); }
        public void Shake(float strength) { shake = Mathf.Max(shake, strength); }
        void UpdateAtmosphere()
        {
            var config = game.Config;
            view.backgroundColor = config.fogColor;
            // Shader globals are raw vectors. Match the camera clear colour in the rendering colour space.
            Color color = QualitySettings.activeColorSpace == ColorSpace.Linear ? config.fogColor.linear : config.fogColor;
            Shader.SetGlobalVector(CityFogColor, new Vector4(color.r, color.g, color.b, 1));
            // Always finish the fade before the physical scenery edge / far clip plane.
            float sideEnd = Mathf.Clamp(config.sideFogEnd, 1f, TrackGenerator.SceneryOuterEdge - 0.5f);
            float distanceEnd = Mathf.Clamp(config.distanceFogEnd, 1f, view.farClipPlane - 1f);
            Shader.SetGlobalVector(CityFogParams, new Vector4(
                Mathf.Clamp(config.sideFogStart, 0, sideEnd - 0.1f), sideEnd,
                Mathf.Clamp(config.distanceFogStart, 0, distanceEnd - 0.1f), distanceEnd));
            Shader.SetGlobalVector(CityBend, new Vector4(game.Player.transform.position.z,
                Mathf.Sin(game.Distance / 850f) * 0.00042f, 0, 0));
        }
        void LateUpdate()
        {
            UpdateAtmosphere();
            if (game.State == RunState.Paused) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref smoothing, 0.14f);
            Aim();
            if (shake > 0)
            {
                transform.position += Random.insideUnitSphere * shake * 0.14f;
                shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 1.5f);
            }
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, game.Player.NitroActive ? 71 : 62, Time.deltaTime * 4);
        }
    }
}
