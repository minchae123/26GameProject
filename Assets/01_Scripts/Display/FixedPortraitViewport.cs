using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class FixedPortraitViewport : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;
    private const float AspectRatio = ReferenceWidth / ReferenceHeight;

    [SerializeField] private CanvasScaler canvasScaler;

    private Camera gameCamera;
    private Camera backgroundCamera;
    private int previousWidth;
    private int previousHeight;
    private Rect previousSafeArea;
    private CanvasScaler.ScaleMode originalScaleMode;
    private float originalScaleFactor;

    private void Awake()
    {
        gameCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (canvasScaler != null)
        {
            originalScaleMode = canvasScaler.uiScaleMode;
            originalScaleFactor = canvasScaler.scaleFactor;
        }

        previousWidth = 0;
        ApplyLayout();
    }

    private void Update()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        Rect safeArea = Screen.safeArea;

        if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0 || safeArea.height <= 0)
            return;

        if (screenWidth == previousWidth && screenHeight == previousHeight && safeArea == previousSafeArea)
            return;

        previousWidth = screenWidth;
        previousHeight = screenHeight;
        previousSafeArea = safeArea;

        // 안전 영역 안에서 가장 큰 9:16 직사각형을 중앙에 놓는다.
        float gameWidth = Mathf.Min(safeArea.width, safeArea.height * AspectRatio);
        float gameHeight = gameWidth / AspectRatio;
        float left = safeArea.center.x - gameWidth * 0.5f;
        float bottom = safeArea.center.y - gameHeight * 0.5f;

        gameCamera.rect = new Rect(left / screenWidth, bottom / screenHeight,
            gameWidth / screenWidth, gameHeight / screenHeight);
        gameCamera.aspect = AspectRatio;

        // 편집 시 사용한 Canvas 기준 폭을 안전 영역 안의 9:16 화면에도 적용한다.
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = gameWidth / canvasScaler.referenceResolution.x;
        }

        bool hasMargins = gameWidth < screenWidth - 0.5f || gameHeight < screenHeight - 0.5f;
        if (hasMargins && backgroundCamera == null)
            CreateBackgroundCamera();

        if (backgroundCamera != null)
            backgroundCamera.enabled = hasMargins;
    }

    private void CreateBackgroundCamera()
    {
        // 맵을 그리기 전에 화면 전체를 검은색으로 채운다.
        var background = new GameObject("Viewport Background");
        background.transform.SetParent(transform, false);
        backgroundCamera = background.AddComponent<Camera>();
        backgroundCamera.depth = gameCamera.depth - 1f;
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;
        backgroundCamera.cullingMask = 0;
        backgroundCamera.allowHDR = false;
        backgroundCamera.allowMSAA = false;
        backgroundCamera.targetDisplay = gameCamera.targetDisplay;
    }

    private void OnDisable()
    {
        if (backgroundCamera != null)
            backgroundCamera.enabled = false;

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = originalScaleMode;
            canvasScaler.scaleFactor = originalScaleFactor;
        }

        gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
        gameCamera.ResetAspect();
    }
}
