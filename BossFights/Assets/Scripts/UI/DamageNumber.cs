using TMPro;
using UnityEngine;

namespace UI
{
    // A floating number that rises above whatever was hit, then fades out; it is built in code, so nothing needs to be wired in a scene
    public class DamageNumber : MonoBehaviour
    {
        private const float Lifetime = 0.9f;
        private const float FadeStart = 0.5f;
        private const float RiseSpeed = 2.2f;
        private const float RiseDrag = 2.5f;
        private const float SideSpread = 0.8f;
        private const float NormalSize = 9.6f;
        private const float CritSize = 14.4f;
        private static readonly Color NormalColor = Color.white;
        private static readonly Color CritColor = new Color(1f, 0.1f, 0.1f);
        private static readonly Color OutlineColor = new Color(0.1f, 0.05f, 0f);

        private TextMeshPro _text;
        private Color _color;
        private float _elapsed;
        private float _riseSpeed;

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            _riseSpeed = Mathf.Max(0f, _riseSpeed - RiseDrag * Time.deltaTime);
            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            Camera mainCamera = Camera.main;
            if (mainCamera)
                transform.rotation = mainCamera.transform.rotation;

            _color.a = _elapsed < FadeStart ? 1f : 1f - (_elapsed - FadeStart) / (Lifetime - FadeStart);
            _text.color = _color;
        }

        // top is the highest point of whatever was hit
        public static void Show(Vector3 top, int amount, bool isCrit)
        {
            GameObject numberObject = new GameObject("Damage Number");
            numberObject.transform.position = top + new Vector3(Random.Range(-SideSpread, SideSpread), 0.4f, 0f);

            DamageNumber number = numberObject.AddComponent<DamageNumber>();
            number._riseSpeed = RiseSpeed;
            number._color = isCrit ? CritColor : NormalColor;

            TextMeshPro text = numberObject.AddComponent<TextMeshPro>();
            text.text = amount.ToString();
            text.fontSize = isCrit ? CritSize : NormalSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = number._color;
            text.outlineWidth = 0.25f;
            text.outlineColor = OutlineColor;
            text.sortingOrder = 100;
            number._text = text;
        }

        public static void Show(Collider target, int amount, bool isCrit)
        {
            Vector3 top = target && target.bounds.size.sqrMagnitude > 0f
                ? new Vector3(target.bounds.center.x, target.bounds.max.y, target.bounds.center.z)
                : target ? target.transform.position + Vector3.up * 2f : Vector3.zero;
            Show(top, amount, isCrit);
        }
    }
}
