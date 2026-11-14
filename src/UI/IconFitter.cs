using UnityEngine;
using UnityEngine.UI;

namespace PolytopiaMapManager.UI;

internal static class IconFitter
{
    internal static void Fit(Image image, Sprite? sprite, float targetSize)
    {
        image.sprite = sprite;
        image.useSpriteMesh = true;
        image.preserveAspect = false;
        image.gameObject.SetActive(sprite != null);
        if (sprite == null)
            return;

        RectTransform rect = image.rectTransform;
        rect.localScale = Vector3.one;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 boundsSize = sprite.bounds.size;
        Vector2 pivot = new(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        if (min.x > max.x)
        {
            min = -Vector2.Scale(boundsSize, pivot);
            max = min + boundsSize;
        }

        Vector2 contentSize = max - min;
        float largest = Mathf.Max(contentSize.x, contentSize.y);
        float scale = largest > 0f ? targetSize / largest : 1f;
        Vector2 contentCenter = (min + max) / 2f + Vector2.Scale(boundsSize, pivot - new Vector2(0.5f, 0.5f));

        rect.sizeDelta = boundsSize * scale;
        rect.anchoredPosition = -contentCenter * scale;
    }
}
