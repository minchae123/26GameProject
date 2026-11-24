using UnityEngine;

public sealed class DinosaurHealthBar : MonoBehaviour
{
    private const float Height = 0.055f;
    private const float Gap = 0.09f;
    private static Sprite pixel;

    private SpriteRenderer dinosaur;
    private SpriteRenderer background;
    private SpriteRenderer fill;
    private float healthFraction;

    public static DinosaurHealthBar Attach(SpriteRenderer dinosaur, Color color)
    {
        if (dinosaur == null)
            return null;

        var root = new GameObject("HP Bar");
        root.transform.SetParent(dinosaur.transform, false);

        var bar = root.AddComponent<DinosaurHealthBar>();
        bar.dinosaur = dinosaur;
        bar.background = bar.CreatePart("Background", new Color32(52, 44, 49, 235), 1);
        bar.fill = bar.CreatePart("Health", color, 2);
        return bar;
    }

    public void SetHealth(int current, int maximum)
    {
        healthFraction = Mathf.Clamp01((float)current / Mathf.Max(1, maximum));
        UpdateBar();
    }

    private SpriteRenderer CreatePart(string partName, Color color, int orderOffset)
    {
        if (pixel == null)
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

        var part = new GameObject(partName);
        part.transform.SetParent(transform, false);
        var sprite = part.AddComponent<SpriteRenderer>();
        sprite.sprite = pixel;
        sprite.color = color;
        sprite.sortingLayerID = dinosaur.sortingLayerID;
        sprite.sortingOrder = dinosaur.sortingOrder + orderOffset;
        return sprite;
    }

    private void LateUpdate()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        Bounds bounds = dinosaur.bounds;
        float width = Mathf.Clamp(bounds.size.x, 0.28f, 0.65f);
        float innerWidth = width - 0.024f;
        float filledWidth = innerWidth * healthFraction;
        Vector3 center = new(bounds.center.x, bounds.max.y + Gap, bounds.center.z);
        Vector3 scale = transform.lossyScale;

        background.enabled = dinosaur.enabled;
        fill.enabled = dinosaur.enabled && healthFraction > 0f;
        background.transform.position = center;
        fill.transform.position = center + Vector3.left * ((innerWidth - filledWidth) * 0.5f);
        background.transform.localScale = new Vector3(width / Mathf.Abs(scale.x), Height / Mathf.Abs(scale.y), 1);
        fill.transform.localScale = new Vector3(filledWidth / Mathf.Abs(scale.x), (Height - 0.018f) / Mathf.Abs(scale.y), 1);
    }
}
