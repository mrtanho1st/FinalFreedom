using UnityEngine;
using UnityEngine.UI;

namespace FinalFreedom
{
    public sealed class WorldHealthBar : MonoBehaviour
    {
        VehicleDamage damage;
        Canvas canvas;
        Image fill;
        Transform holder;
        float height;
        Camera view;
        public void Initialize(VehicleDamage vehicle, float above)
        {
            damage = vehicle; height = above;
            var go = new GameObject("Damage HP", typeof(RectTransform), typeof(Canvas));
            holder = go.transform; holder.SetParent(transform, false);
            holder.localPosition = Vector3.up * height; holder.localScale = Vector3.one * 0.015f;
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)holder).sizeDelta = new Vector2(100, 9);
            var background = new GameObject("Track", typeof(RectTransform), typeof(Image)); background.transform.SetParent(holder, false);
            var bg = background.GetComponent<RectTransform>(); bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f); background.GetComponent<Image>().raycastTarget = false;
            var bar = new GameObject("Health", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(holder, false);
            var rect = bar.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            fill = bar.GetComponent<Image>(); fill.color = ProceduralArt.Gold; fill.raycastTarget = false;
            canvas.enabled = false;
        }
        void LateUpdate()
        {
            if (damage == null || canvas == null) return;
            canvas.enabled = damage.HP < damage.MaxHP && !damage.IsDead;
            if (!canvas.enabled) return;
            if (view == null) view = Camera.main;
            holder.position = transform.position + Vector3.up * height;
            if (view != null) holder.rotation = view.transform.rotation;
            var rect = (RectTransform)fill.transform; rect.anchorMax = new Vector2(damage.Fraction, 1);
            fill.color = damage.Fraction < 0.3f ? new Color(1, 0.24f, 0.17f) : ProceduralArt.Gold;
        }
    }
}
