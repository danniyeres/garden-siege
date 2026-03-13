using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(80f, 10f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    private Health health;
    private Transform barRoot;
    private Image fillImage;
    private Camera cachedCamera;
    private static Sprite cachedSprite;

    private void Awake()
    {
        health = GetComponent<Health>();
        BuildBar();
    }

    private void LateUpdate()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (health == null || barRoot == null || fillImage == null)
        {
            return;
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        barRoot.position = transform.position + worldOffset;
        if (cachedCamera != null)
        {
            barRoot.forward = cachedCamera.transform.forward;
        }

        fillImage.fillAmount = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
    }

    private void BuildBar()
    {
        var bar = new GameObject(
            "EnemyHealthBar",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        barRoot = bar.transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = worldOffset;

        var canvas = bar.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        canvas.worldCamera = Camera.main;

        var scaler = bar.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var barRect = bar.GetComponent<RectTransform>();
        barRect.sizeDelta = barSize;
        barRect.localScale = Vector3.one * 0.01f;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(barRoot, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = GetDefaultSprite();
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = backgroundColor;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(barRoot, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        fillImage = fill.GetComponent<Image>();
        fillImage.sprite = GetDefaultSprite();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = fillColor;
    }

    private static Sprite GetDefaultSprite()
    {
        if (cachedSprite == null)
        {
            cachedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        return cachedSprite;
    }
}
