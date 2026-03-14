using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class TutorialKeyPromptView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Outline outline;
        [SerializeField] private Color idleColor = new(0.18f, 0.21f, 0.27f, 0.95f);
        [SerializeField] private Color activeColor = new(0.83f, 0.68f, 0.31f, 1f);
        [SerializeField] private Color idleTextColor = new(0.95f, 0.92f, 0.84f, 1f);
        [SerializeField] private Color activeTextColor = new(0.18f, 0.12f, 0.04f, 1f);

        private bool visualsInitialized;

        private void Awake()
        {
            EnsureRuntimeVisuals();
        }

        public void EnsureRuntimeVisuals()
        {
            if (visualsInitialized)
            {
                return;
            }

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            visualsInitialized = true;
            rectTransform.sizeDelta = new Vector2(56f, 44f);

            if (background == null)
            {
                background = gameObject.GetComponent<Image>();
                if (background == null)
                {
                    background = gameObject.AddComponent<Image>();
                }
            }

            if (outline == null)
            {
                outline = gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = gameObject.AddComponent<Outline>();
                }
            }

            outline.effectColor = new Color32(255, 236, 194, 60);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            if (label == null)
            {
                Transform existing = transform.Find("Label");
                if (existing != null)
                {
                    label = existing.GetComponent<TMP_Text>();
                }
            }

            if (label == null)
            {
                GameObject labelObject = new("Label", typeof(RectTransform));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label = labelObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 20f;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
            }

            if (label != null && string.IsNullOrEmpty(label.text))
            {
                label.text = "?";
            }

            if (background != null)
            {
                background.color = idleColor;
            }

            if (label != null)
            {
                label.color = idleTextColor;
            }

            if (outline != null)
            {
                outline.effectColor = new Color32(255, 236, 194, 60);
            }
        }

        public void SetLabel(string value)
        {
            EnsureRuntimeVisuals();
            if (label != null)
            {
                label.text = value;
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            EnsureRuntimeVisuals();
            if (background != null)
            {
                background.color = highlighted ? activeColor : idleColor;
            }

            if (label != null)
            {
                label.color = highlighted ? activeTextColor : idleTextColor;
            }

            if (outline != null)
            {
                outline.effectColor = highlighted
                    ? new Color32(255, 230, 161, 180)
                    : new Color32(255, 236, 194, 60);
            }
        }
    }
}
