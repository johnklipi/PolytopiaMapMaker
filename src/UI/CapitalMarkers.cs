using HarmonyLib;
using TMPro;
using UnityEngine;

namespace PolytopiaMapManager.UI;

public static class CapitalMarkers
{
    private const string SORTING_LAYER = "CityStatusDisplays";
    private const float TILE_WIDTH = 0.9622f;
    private const float CROWN_WIDTH = TILE_WIDTH * 0.45f;
    private static readonly Vector3 OFFSET = new(0f, 0.32f, 0f);

    private static readonly List<GameObject> markers = new();
    private static string lastSignature = "";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if (!Main.isActive || !GameManager.Instance.isLevelLoaded || GameManager.GameState == null)
        {
            if (markers.Count > 0)
                Clear();
            return;
        }

        string signature = BuildSignature();
        if (signature != lastSignature)
            Rebuild(signature);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static void GameManager_ReturnToMenu()
    {
        Clear();
    }

    private static string BuildSignature()
    {
        MapData map = GameManager.GameState.Map;
        System.Text.StringBuilder sb = new();
        sb.Append(map.Width).Append('x').Append(map.Height);
        foreach (Data.Capital capital in Main.currCapitals)
            sb.Append('|').Append(capital.player).Append('@').Append(capital.coordinates.x).Append(',').Append(capital.coordinates.y);
        return sb.ToString();
    }

    private static void Clear()
    {
        foreach (GameObject marker in markers)
        {
            if (marker != null)
                GameObject.Destroy(marker);
        }
        markers.Clear();
        lastSignature = "";
    }

    private static void Rebuild(string signature)
    {
        Clear();
        lastSignature = signature;

        Sprite? crown = UIManager.IconData.GetSprite("UI_crown");
        TMP_FontAsset? font = FindFont();
        MapData map = GameManager.GameState.Map;
        foreach (Data.Capital capital in Main.currCapitals)
        {
            WorldCoordinates coords = capital.coordinates;
            if (coords.x < 0 || coords.y < 0 || coords.x >= map.Width || coords.y >= map.Height)
                continue;
            markers.Add(CreateMarker(capital, crown, font));
        }
    }

    private static GameObject CreateMarker(Data.Capital capital, Sprite? crown, TMP_FontAsset? font)
    {
        Vector2 tilePosition = capital.coordinates.ToPosition();
        GameObject root = new GameObject($"MapMaker capital marker {capital.player}");
        root.transform.position = new Vector3(tilePosition.x, tilePosition.y, 0f) + OFFSET;

        if (crown != null)
        {
            GameObject crownObject = new GameObject("crown");
            crownObject.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = crownObject.AddComponent<SpriteRenderer>();
            renderer.sprite = crown;
            renderer.sortingLayerName = SORTING_LAYER;
            renderer.sortingOrder = 10;
            float width = crown.bounds.size.x;
            float scale = width > 0f ? CROWN_WIDTH / width : 1f;
            crownObject.transform.localScale = new Vector3(scale, scale, 1f);
        }

        Vector3 badgePosition = new(0f, -CROWN_WIDTH * 0.42f, 0f);
        GameObject badgeObject = new GameObject("badge");
        badgeObject.transform.SetParent(root.transform, false);
        badgeObject.transform.localPosition = badgePosition;
        SpriteRenderer badge = badgeObject.AddComponent<SpriteRenderer>();
        badge.sprite = GetBadgeSprite();
        badge.color = ColorConstants.blue;
        badge.sortingLayerName = SORTING_LAYER;
        badge.sortingOrder = 11;

        GameObject textObject = new GameObject("player number");
        textObject.transform.SetParent(root.transform, false);
        textObject.transform.localPosition = badgePosition;
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        if (font != null)
            text.font = font;
        text.text = capital.player.ToString();
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.color = Color.white;
        float inner = BADGE_DIAMETER * 0.72f;
        text.rectTransform.sizeDelta = new Vector2(inner, inner * 0.8f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.1f;
        text.fontSizeMax = 20f;
        text.margin = Vector4.zero;
        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
        textRenderer.sortingLayerName = SORTING_LAYER;
        textRenderer.sortingOrder = 12;

        return root;
    }

    private const float BADGE_DIAMETER = CROWN_WIDTH * 0.75f;
    private const int BADGE_TEXTURE_SIZE = 64;
    private static Sprite? badgeSprite;

    private static Sprite GetBadgeSprite()
    {
        if (badgeSprite != null)
            return badgeSprite;

        int size = BADGE_TEXTURE_SIZE;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        float radius = size / 2f;
        float rim = size * 0.09f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - distance);
                float fill = Mathf.Clamp01(radius - rim - distance);
                float shade = Mathf.Lerp(0.12f, 1f, fill);
                texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
            }
        }
        texture.Apply();

        badgeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / BADGE_DIAMETER);
        badgeSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return badgeSprite;
    }

    private static TMP_FontAsset? FindFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;
        TextMeshProUGUI? uiText = GameObject.FindObjectOfType<TextMeshProUGUI>();
        return uiText != null ? uiText.font : null;
    }
}
