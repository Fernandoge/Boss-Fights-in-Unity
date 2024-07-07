using UnityEngine;
using TMPro;

public class AdjustBackgroundSize : MonoBehaviour
{
    public RectTransform backgroundRectTransform;
    public TextMeshProUGUI textMeshProUGUI;
    private const float padding = -50f;

    private void OnEnable()
    {
        // Get the preferred width of the text
        var preferredWidth = textMeshProUGUI.preferredWidth;

        // Set the width of the background RectTransform to the preferred width of the text plus padding
        backgroundRectTransform.sizeDelta = new Vector2(preferredWidth + padding, backgroundRectTransform.sizeDelta.y);
    }
}