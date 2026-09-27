using UnityEngine;

namespace FinalFreedom
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        GameManager game;
        Camera view;
        float shake;
        Vector3 smoothing;
        public void Initialize(GameManager manager)
        {
            game = manager; view = GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(0.29f, 0.48f, 0.54f);
            view.nearClipPlane = 0.15f; view.farClipPlane = 310; view.fieldOfView = 62;
            view.allowHDR = false; view.allowMSAA = true;
            Snap();
        }
        Vector3 Desired()
        {
            var p = game.Player.transform.position;
            bool portrait = view.aspect < 1;
            return new Vector3(p.x * 0.32f, portrait ? 9.5f : 7.3f, p.z - (portrait ? 18f : 13f));
        }
        public void Snap() { transform.position = Desired(); smoothing = Vector3.zero; shake = 0; Aim(); }
        void Aim()
        {
            Vector3 p = game.Player.transform.position;
            transform.rotation = Quaternion.LookRotation(new Vector3(p.x * 0.3f, 0.8f, p.z + 13f) - transform.position);
        }
        public void Shift(float shift) { transform.position -= Vector3.forward * shift; }
        public void Shake(float strength) { shake = Mathf.Max(shake, strength); }
        void LateUpdate()
        {
            if (game.State == RunState.Paused) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref smoothing, 0.14f);
            Aim();
            if (shake > 0)
            {
                transform.position += Random.insideUnitSphere * shake * 0.14f;
                shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 1.5f);
            }
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, game.Player.NitroActive ? 71 : 62, Time.deltaTime * 4);
            Shader.SetGlobalVector("_CityBend", new Vector4(game.Player.transform.position.z, Mathf.Sin(game.Distance / 850f) * 0.00042f, 0, 0));
        }
    }
}
