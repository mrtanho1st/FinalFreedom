using UnityEngine;
using UnityEngine.UI;

namespace FinalFreedom
{
    public sealed class GestureCameraView : MonoBehaviour
    {
        GestureDriveController controller;
        RawImage cameraImage;
        AspectRatioFitter fit;
        SteeringWheelGraphic wheel;
        public void Initialize(GestureDriveController controller)
        {
            this.controller = controller;
            var content = new GameObject("Mirrored front camera", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            content.transform.SetParent(transform, false);
            var rect = (RectTransform)content.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            cameraImage = content.GetComponent<RawImage>(); cameraImage.raycastTarget = false;
            fit = content.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var graphic = new GameObject("Virtual steering wheel and hand landmarks", typeof(RectTransform));
            graphic.transform.SetParent(content.transform, false);
            var wheelRect = (RectTransform)graphic.transform;
            wheelRect.anchorMin = Vector2.zero; wheelRect.anchorMax = Vector2.one;
            wheelRect.offsetMin = wheelRect.offsetMax = Vector2.zero;
            wheel = graphic.AddComponent<SteeringWheelGraphic>(); wheel.Controller = controller; wheel.raycastTarget = false;
        }
        void Update()
        {
            if (controller == null) return;
            cameraImage.texture = controller.Preview;
            cameraImage.color = controller.Preview == null ? new Color(0.03f, 0.07f, 0.1f) : Color.white;
            fit.aspectRatio = controller.Preview == null ? 4f / 3f : (float)controller.Preview.width / controller.Preview.height;
            wheel.SetVerticesDirty();
        }
    }

    public sealed class SteeringWheelGraphic : MaskableGraphic
    {
        public GestureDriveController Controller;
        static readonly int[] Links = { 0,1, 1,2, 2,3, 3,4, 0,5, 5,6, 6,7, 7,8, 5,9, 9,10, 10,11, 11,12,
            9,13, 13,14, 14,15, 15,16, 13,17, 0,17, 17,18, 18,19, 19,20 };
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Controller == null) return;
            var rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.36f;
            var color = Controller.Interpreter.Brake ? new Color(1, 0.3f, 0.2f, 0.9f) :
                Controller.HoldingNitro ? new Color(1, 0.75f, 0.1f, 0.9f) : new Color(0.2f, 0.95f, 1, 0.8f);
            for (int i = 0; i < 64; i++)
                Line(mesh, rect.center + Circle(i * 360f / 64) * radius, rect.center + Circle((i + 1) * 360f / 64) * radius, radius * 0.05f, color);
            float angle = Controller.Interpreter.WheelAngle;
            for (int i = 0; i < 3; i++)
                Line(mesh, rect.center, rect.center + Circle(angle + (i == 2 ? -90 : i * 180)) * radius, radius * 0.045f, color);
            float aspect = rect.width / Mathf.Max(1, rect.height);
            foreach (var hand in Controller.Hands)
            {
                Vector2 Point(int index) => rect.min + new Vector2(hand.points[index].x / aspect * rect.width, hand.points[index].y * rect.height);
                for (int i = 0; i < Links.Length; i += 2)
                    Line(mesh, Point(Links[i]), Point(Links[i + 1]), radius * 0.015f, Color.white);
                for (int i = 0; i < 21; i++)
                {
                    var p = Point(i);
                    Line(mesh, p - Vector2.right * radius * 0.015f, p + Vector2.right * radius * 0.015f, radius * 0.03f, ProceduralArt.Gold);
                }
            }
        }
        static Vector2 Circle(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
        static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            var direction = (b - a).normalized;
            var normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal, color, Vector2.zero); mesh.AddVert(a + normal, color, Vector2.zero);
            mesh.AddVert(b + normal, color, Vector2.zero); mesh.AddVert(b - normal, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
