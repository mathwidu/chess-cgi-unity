using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class GameHud : MonoBehaviour
{
    private static readonly Vector3 WorldPanelPosition = new Vector3(4.8f, 1.8f, -1.6f);
    private const float WorldPanelScale = 0.0032f;

    [SerializeField] private ChessGameController gameController;
    [SerializeField] private int visibleMoveCount = 6;

    private readonly Color panelColor = new Color(0.085f, 0.082f, 0.074f, 0.94f);
    private readonly Color panelStrongColor = new Color(0.048f, 0.046f, 0.043f, 0.98f);
    private readonly Color previewSurfaceColor = new Color(0.12f, 0.13f, 0.13f, 1f);
    private readonly Color overlayColor = new Color(0.02f, 0.018f, 0.016f, 0.66f);
    private readonly Color textColor = new Color(0.97f, 0.94f, 0.87f, 1f);
    private readonly Color mutedTextColor = new Color(0.78f, 0.76f, 0.68f, 1f);
    private readonly Color accentColor = new Color(1f, 0.77f, 0.36f, 1f);
    private readonly Color actionColor = new Color(0.2f, 0.32f, 0.38f, 0.96f);
    private readonly Color actionHoverColor = new Color(0.27f, 0.42f, 0.49f, 1f);
    private readonly Color neutralButtonColor = new Color(0.27f, 0.25f, 0.22f, 0.96f);

    private bool showStartScreen = true;
    private bool showHowToPlay;
    private Font hudFont;
    private RectTransform hudRoot;
    private RectTransform startOverlay;
    private RectTransform howToPlayPanel;
    private RectTransform startHowToPlayText;
    private RectTransform promotionPanel;
    private RectTransform selectedPiecePanel;
    private RawImage selectedPiecePreviewImage;
    private Text turnText;
    private Text statusText;
    private Text moveHistoryText;
    private Text howToPlayButtonText;
    private Text startHowToPlayButtonText;
    private Text selectedPieceNameText;
    private Text selectedPieceKindText;
    private Text selectedPieceSquareText;
    private Text selectedPieceSideText;
    private Text selectedPieceProfileText;
    private Text selectedPieceDescriptionText;
    private SelectedPiecePreviewInput selectedPiecePreviewInput;
    private RenderTexture selectedPiecePreviewTexture;
    private Camera selectedPiecePreviewCamera;
    private Light selectedPiecePreviewLight;
    private Transform selectedPiecePreviewStage;
    private GameObject selectedPiecePreviewClone;
    private Vector3 selectedPiecePreviewFocusPoint;
    private PieceView previewedPiece;
    private Canvas hudCanvas;

    public void Configure(ChessGameController controller)
    {
        gameController = controller;
        RefreshInterface();
    }

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = Object.FindFirstObjectByType<ChessGameController>();
        }
    }

    private void Start()
    {
        RebuildInterface();
    }

    private void Update()
    {
        if (hudCanvas != null && hudCanvas.renderMode == RenderMode.WorldSpace && hudCanvas.worldCamera == null)
        {
            hudCanvas.worldCamera = XRRig.EyeCamera;
        }

        RefreshInterface();
    }

    public void RebuildInterface()
    {
        EnsureCanvasInfrastructure();
        ClearExistingRoot();

        hudRoot = CreateRect("HudRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);

        RectTransform topBar = CreateRect("TopBar", hudRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(0f, 88f));
        RectTransform brandPanel = CreatePanel("BrandPanel", topBar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -2f), new Vector2(320f, 74f), panelColor);
        CreateText("TitleText", brandPanel, "Xadrez CGI", 25, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(14f, -10f), new Vector2(292f, 34f));
        CreateText("SubtitleText", brandPanel, "Computacao Grafica I", 12, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(14f, -45f), new Vector2(292f, 18f));

        RectTransform turnPanel = CreatePanel("TurnPanel", topBar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -2f), new Vector2(360f, 74f), panelColor);
        turnText = CreateText("TurnText", turnPanel, "Brancas jogam", 17, FontStyle.Bold, accentColor, TextAnchor.UpperRight, new Vector2(14f, -10f), new Vector2(332f, 24f));
        statusText = CreateText("StatusText", turnPanel, "Escolha uma peca para mover.", 13, FontStyle.Normal, textColor, TextAnchor.UpperRight, new Vector2(14f, -38f), new Vector2(332f, 26f));

        RectTransform historyPanel = CreatePanel("MoveHistoryPanel", hudRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -104f), new Vector2(360f, 210f), panelColor);
        CreateText("MoveHistoryTitle", historyPanel, "Historico", 17, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(14f, -12f), new Vector2(332f, 24f));
        moveHistoryText = CreateText("MoveHistoryText", historyPanel, "Nenhuma jogada ainda.", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(14f, -42f), new Vector2(332f, 150f));

        selectedPiecePanel = CreatePanel("SelectedPiecePanel", hudRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -330f), new Vector2(360f, 560f), panelStrongColor);
        CreateText("SelectedPieceEyebrowText", selectedPiecePanel, "PECA SELECIONADA", 11, FontStyle.Bold, accentColor, TextAnchor.UpperLeft, new Vector2(16f, -14f), new Vector2(328f, 18f));
        selectedPiecePreviewImage = CreateRawImage("SelectedPiecePreview", selectedPiecePanel, new Vector2(16f, -42f), new Vector2(328f, 310f), Color.white);
        selectedPiecePreviewInput = selectedPiecePreviewImage.gameObject.AddComponent<SelectedPiecePreviewInput>();
        CreateButton("PreviewZoomOutButton", selectedPiecePanel, "-", new Vector2(266f, 484f), new Vector2(34f, 34f), neutralButtonColor, ZoomSelectedPiecePreviewOut);
        CreateButton("PreviewZoomInButton", selectedPiecePanel, "+", new Vector2(306f, 484f), new Vector2(34f, 34f), actionColor, ZoomSelectedPiecePreviewIn);
        selectedPieceNameText = CreateText("SelectedPieceNameText", selectedPiecePanel, "-", 22, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -368f), new Vector2(328f, 30f));
        selectedPieceKindText = CreateText("SelectedPieceKindText", selectedPiecePanel, "-", 15, FontStyle.Bold, accentColor, TextAnchor.UpperLeft, new Vector2(16f, -402f), new Vector2(328f, 24f));
        selectedPieceSquareText = CreateText("SelectedPieceSquareText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(16f, -430f), new Vector2(150f, 22f));
        selectedPieceSideText = CreateText("SelectedPieceSideText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperRight, new Vector2(190f, -430f), new Vector2(154f, 22f));
        selectedPieceProfileText = CreateText("SelectedPieceProfileText", selectedPiecePanel, "-", 13, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -462f), new Vector2(328f, 66f));
        selectedPieceDescriptionText = CreateText("SelectedPieceDescriptionText", selectedPiecePanel, "-", 12, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(16f, -532f), new Vector2(328f, 22f));
        EnsureSelectedPiecePreviewResources();
        selectedPiecePreviewImage.texture = selectedPiecePreviewTexture;
        selectedPiecePreviewInput.Configure(null, selectedPiecePreviewCamera);

        RectTransform actionBar = CreatePanel("ActionBar", hudRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(410f, 58f), panelColor);
        CreateButton("NewGameButton", actionBar, "Nova partida", new Vector2(14f, 12f), new Vector2(122f, 34f), actionColor, StartGame);
        CreateButton("CancelButton", actionBar, "Cancelar", new Vector2(144f, 12f), new Vector2(108f, 34f), neutralButtonColor, CancelSelection);
        howToPlayButtonText = CreateButton("HowToPlayButton", actionBar, "Como jogar", new Vector2(260f, 12f), new Vector2(124f, 34f), neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();

        howToPlayPanel = CreatePanel("HowToPlayPanel", hudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -104f), new Vector2(380f, 210f), panelStrongColor);
        CreateText("HowToPlayTitle", howToPlayPanel, "Como jogar", 18, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -14f), new Vector2(348f, 26f));
        CreateText("HowToPlayText", howToPlayPanel, BuildHowToPlayText(), 13, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(16f, -48f), new Vector2(348f, 142f));

        promotionPanel = CreatePanel("PromotionPanel", hudRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 150f), panelStrongColor);
        CreateText("PromotionTitle", promotionPanel, "Promocao", 22, FontStyle.Bold, textColor, TextAnchor.UpperCenter, new Vector2(18f, -14f), new Vector2(464f, 30f));
        CreateText("PromotionHelp", promotionPanel, "Escolha a nova peca do peao.", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperCenter, new Vector2(18f, -48f), new Vector2(464f, 22f));
        CreateButton("PromoteQueenButton", promotionPanel, "Rainha", new Vector2(22f, 92f), new Vector2(104f, 34f), actionColor, () => ChoosePromotion('Q'));
        CreateButton("PromoteRookButton", promotionPanel, "Torre", new Vector2(142f, 92f), new Vector2(94f, 34f), neutralButtonColor, () => ChoosePromotion('R'));
        CreateButton("PromoteBishopButton", promotionPanel, "Bispo", new Vector2(252f, 92f), new Vector2(94f, 34f), neutralButtonColor, () => ChoosePromotion('B'));
        CreateButton("PromoteKnightButton", promotionPanel, "Cavalo", new Vector2(362f, 92f), new Vector2(104f, 34f), neutralButtonColor, () => ChoosePromotion('N'));

        startOverlay = CreatePanel("StartOverlay", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, overlayColor);
        RectTransform startCard = CreatePanel("StartCard", startOverlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 390f), panelStrongColor);
        CreateText("StartTitle", startCard, "Xadrez CGI", 34, FontStyle.Bold, textColor, TextAnchor.UpperCenter, new Vector2(24f, -28f), new Vector2(512f, 48f));
        CreateText("StartSubtitle", startCard, "Xadrez 3D local com personagens da turma.", 15, FontStyle.Normal, mutedTextColor, TextAnchor.UpperCenter, new Vector2(42f, -82f), new Vector2(476f, 28f));
        CreateButton("StartPlayButton", startCard, "Jogar", new Vector2(160f, 128f), new Vector2(240f, 42f), actionColor, StartGame);
        startHowToPlayButtonText = CreateButton("StartHowToPlayButton", startCard, "Como jogar", new Vector2(180f, 182f), new Vector2(200f, 36f), neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();
        startHowToPlayText = CreateText("StartHowToPlayText", startCard, BuildHowToPlayText(), 13, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(64f, -236f), new Vector2(432f, 118f)).rectTransform;

        RefreshInterface();
    }

    public void RefreshInterface()
    {
        if (hudRoot == null)
        {
            return;
        }

        bool hasController = gameController != null;
        bool awaitingPromotion = hasController && gameController.IsAwaitingPromotion;

        SetActive(startOverlay, showStartScreen);
        SetActive(howToPlayPanel, showHowToPlay && !showStartScreen);
        SetActive(startHowToPlayText, showHowToPlay && showStartScreen);
        SetActive(promotionPanel, awaitingPromotion);

        if (turnText != null)
        {
            turnText.text = !hasController || gameController.CurrentTurn == ChessSide.White ? "Brancas jogam" : "Pretas jogam";
        }

        if (statusText != null)
        {
            string status = hasController ? CompactStatus(gameController.StatusMessage) : "Escolha uma peca para mover.";
            statusText.text = status;
            statusText.color = status.StartsWith("Movimento invalido") ? new Color(0.95f, 0.45f, 0.36f, 1f) : textColor;
        }

        if (moveHistoryText != null)
        {
            moveHistoryText.text = hasController ? FormatMoveHistory(gameController.MoveHistory) : "Nenhuma jogada ainda.";
        }

        RefreshSelectedPiecePanel(hasController ? gameController.SelectedPiece : null);

        if (howToPlayButtonText != null)
        {
            howToPlayButtonText.text = showHowToPlay && !showStartScreen ? "Ocultar" : "Como jogar";
        }

        if (startHowToPlayButtonText != null)
        {
            startHowToPlayButtonText.text = showHowToPlay ? "Ocultar como jogar" : "Como jogar";
        }
    }

    private void StartGame()
    {
        showStartScreen = false;
        showHowToPlay = false;
        if (gameController != null)
        {
            gameController.StartLocalGame();
        }

        RefreshInterface();
    }

    private void CancelSelection()
    {
        if (gameController != null)
        {
            gameController.CancelSelection();
        }

        RefreshInterface();
    }

    private void ToggleHowToPlay()
    {
        showHowToPlay = !showHowToPlay;
        RefreshInterface();
    }

    private void ChoosePromotion(char piece)
    {
        if (gameController != null)
        {
            gameController.ChoosePromotion(piece);
        }

        RefreshInterface();
    }

    private void ZoomSelectedPiecePreviewIn()
    {
        if (selectedPiecePreviewInput != null)
        {
            selectedPiecePreviewInput.ZoomPreview(1f);
        }
    }

    private void ZoomSelectedPiecePreviewOut()
    {
        if (selectedPiecePreviewInput != null)
        {
            selectedPiecePreviewInput.ZoomPreview(-1f);
        }
    }

    private void OnDestroy()
    {
        ClearSelectedPiecePreviewClone();

        if (selectedPiecePreviewTexture != null)
        {
            selectedPiecePreviewTexture.Release();
            DestroyUnityObject(selectedPiecePreviewTexture);
            selectedPiecePreviewTexture = null;
        }
    }

    private void RefreshSelectedPiecePanel(PieceView selectedPiece)
    {
        if (selectedPiecePanel == null)
        {
            return;
        }

        bool hasSelection = selectedPiece != null;
        SetActive(selectedPiecePanel, hasSelection);
        if (!hasSelection)
        {
            previewedPiece = null;
            ClearSelectedPiecePreviewClone();
            return;
        }

        if (selectedPieceNameText != null)
        {
            selectedPieceNameText.text = GetPieceModelName(selectedPiece);
        }

        if (selectedPieceKindText != null)
        {
            selectedPieceKindText.text = $"{PieceKindName(selectedPiece.Kind)} {SideAdjective(selectedPiece.Side)}";
        }

        if (selectedPieceSquareText != null)
        {
            selectedPieceSquareText.text = $"Casa {selectedPiece.Square.ToAlgebraic()}";
        }

        if (selectedPieceSideText != null)
        {
            selectedPieceSideText.text = $"Time: {SideName(selectedPiece.Side)}";
        }

        if (selectedPieceProfileText != null)
        {
            selectedPieceProfileText.text = BuildPieceProfileText(selectedPiece);
        }

        if (selectedPieceDescriptionText != null)
        {
            selectedPieceDescriptionText.text = BuildPieceDescription(selectedPiece);
        }

        if (previewedPiece != selectedPiece || selectedPiecePreviewClone == null)
        {
            BuildSelectedPiecePreview(selectedPiece);
            previewedPiece = selectedPiece;
        }

        if (selectedPiecePreviewCamera != null)
        {
            if (selectedPiecePreviewInput != null)
            {
                selectedPiecePreviewInput.Configure(
                    selectedPiecePreviewClone != null ? selectedPiecePreviewClone.transform : null,
                    selectedPiecePreviewCamera,
                    selectedPiecePreviewFocusPoint);
                selectedPiecePreviewInput.NormalizeCameraDistance();
            }

            selectedPiecePreviewCamera.Render();
        }
    }

    private void EnsureSelectedPiecePreviewResources()
    {
        if (selectedPiecePreviewTexture == null)
        {
            selectedPiecePreviewTexture = new RenderTexture(768, 640, 24)
            {
                name = "SelectedPiecePreviewTexture",
                antiAliasing = 4,
                useMipMap = false
            };
            selectedPiecePreviewTexture.Create();
        }

        if (selectedPiecePreviewStage == null)
        {
            GameObject stageObject = new GameObject("SelectedPiecePreviewStage");
            stageObject.transform.SetParent(transform, false);
            stageObject.transform.position = new Vector3(96f, 96f, 96f);
            selectedPiecePreviewStage = stageObject.transform;
        }

        if (selectedPiecePreviewCamera == null)
        {
            GameObject cameraObject = new GameObject("SelectedPiecePreviewCamera");
            cameraObject.transform.SetParent(selectedPiecePreviewStage, false);
            selectedPiecePreviewCamera = cameraObject.AddComponent<Camera>();
            selectedPiecePreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            selectedPiecePreviewCamera.backgroundColor = previewSurfaceColor;
            selectedPiecePreviewCamera.fieldOfView = 32f;
            selectedPiecePreviewCamera.nearClipPlane = 0.03f;
            selectedPiecePreviewCamera.farClipPlane = 12f;
            selectedPiecePreviewCamera.targetTexture = selectedPiecePreviewTexture;
        }

        selectedPiecePreviewCamera.aspect = 768f / 640f;
        selectedPiecePreviewCamera.transform.localPosition = new Vector3(0f, 0.9f, -3.8f);
        selectedPiecePreviewCamera.transform.LookAt(selectedPiecePreviewStage.position + new Vector3(0f, 0.78f, 0f));

        if (selectedPiecePreviewLight == null)
        {
            GameObject lightObject = new GameObject("SelectedPiecePreviewLight");
            lightObject.transform.SetParent(selectedPiecePreviewStage, false);
            selectedPiecePreviewLight = lightObject.AddComponent<Light>();
            selectedPiecePreviewLight.type = LightType.Directional;
            selectedPiecePreviewLight.intensity = 1.6f;
            selectedPiecePreviewLight.color = new Color(1f, 0.95f, 0.86f, 1f);
        }

        selectedPiecePreviewLight.transform.localRotation = Quaternion.Euler(38f, -28f, 0f);
    }

    private void BuildSelectedPiecePreview(PieceView selectedPiece)
    {
        EnsureSelectedPiecePreviewResources();
        ClearSelectedPiecePreviewClone();

        selectedPiecePreviewClone = Object.Instantiate(selectedPiece.gameObject, selectedPiecePreviewStage);
        selectedPiecePreviewClone.name = "SelectedPiecePreviewClone";
        selectedPiecePreviewClone.transform.localPosition = Vector3.zero;
        selectedPiecePreviewClone.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        selectedPiecePreviewClone.transform.localScale = Vector3.one;

        DisablePreviewInteractionComponents(selectedPiecePreviewClone);
        FitPreviewClone(selectedPiecePreviewClone.transform);
        FramePreviewCamera(selectedPiecePreviewClone.transform);
        if (selectedPiecePreviewInput != null)
        {
            selectedPiecePreviewInput.Configure(selectedPiecePreviewClone.transform, selectedPiecePreviewCamera, selectedPiecePreviewFocusPoint);
            selectedPiecePreviewInput.NormalizeCameraDistance();
        }
    }

    private void ClearSelectedPiecePreviewClone()
    {
        if (selectedPiecePreviewClone != null)
        {
            DestroyUnityObject(selectedPiecePreviewClone);
            selectedPiecePreviewClone = null;
        }
    }

    private static void DisablePreviewInteractionComponents(GameObject clone)
    {
        PieceView clonePieceView = clone.GetComponent<PieceView>();
        if (clonePieceView != null)
        {
            clonePieceView.enabled = false;
        }

        Collider[] colliders = clone.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void FitPreviewClone(Transform clone)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            clone.localPosition = new Vector3(0f, 0.08f, 0f);
            return;
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y > 0.001f)
        {
            float targetHeight = 1.32f;
            float scale = targetHeight / bounds.size.y;
            clone.localScale *= scale;
        }

        bounds = CalculateBounds(renderers);
        Vector3 targetCenter = selectedPiecePreviewStage.position + new Vector3(0f, 0.78f, 0f);
        clone.position += targetCenter - bounds.center;

        bounds = CalculateBounds(renderers);
        float targetFloor = selectedPiecePreviewStage.position.y + 0.07f;
        clone.position += Vector3.up * (targetFloor - bounds.min.y);
    }

    private void FramePreviewCamera(Transform clone)
    {
        if (selectedPiecePreviewCamera == null)
        {
            return;
        }

        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            selectedPiecePreviewCamera.transform.localPosition = new Vector3(0f, 0.9f, -3.8f);
            selectedPiecePreviewFocusPoint = selectedPiecePreviewStage.position + new Vector3(0f, 0.78f, 0f);
            selectedPiecePreviewCamera.transform.LookAt(selectedPiecePreviewFocusPoint);
            return;
        }

        Bounds bounds = CalculateBounds(renderers);
        float aspect = Mathf.Max(0.1f, selectedPiecePreviewCamera.aspect);
        float verticalExtent = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect);
        float distance = verticalExtent / Mathf.Tan(selectedPiecePreviewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        distance = Mathf.Clamp(distance * 1.38f, 2.25f, 5.2f);

        Vector3 target = bounds.center + Vector3.up * Mathf.Max(0.02f, bounds.size.y * 0.04f);
        selectedPiecePreviewFocusPoint = target;
        selectedPiecePreviewCamera.transform.position = target + new Vector3(0f, bounds.size.y * 0.06f, -distance);
        selectedPiecePreviewCamera.transform.LookAt(target);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void EnsureCanvasInfrastructure()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        hudCanvas = canvas;

        bool vrMode = XRRig.IsHeadsetPresent;
        if (vrMode)
        {
            ConfigureWorldSpaceCanvas(canvas);
        }
        else
        {
            ConfigureScreenSpaceCanvas(canvas);
        }

        EnsureEventSystem(vrMode);
    }

    private void ConfigureScreenSpaceCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.enabled = true;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        TrackedDeviceGraphicRaycaster xrRaycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
        if (xrRaycaster != null)
        {
            DestroyUnityObject(xrRaycaster);
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void ConfigureWorldSpaceCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.enabled = false;
        }

        RectTransform canvasRect = (RectTransform)transform;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = new Vector2(1920f, 1080f);
        canvasRect.localScale = Vector3.one * WorldPanelScale;
        canvasRect.position = WorldPanelPosition;
        canvasRect.rotation = Quaternion.LookRotation(XRRig.SeatEyePosition - WorldPanelPosition);

        GraphicRaycaster legacyRaycaster = GetComponent<GraphicRaycaster>();
        if (legacyRaycaster != null)
        {
            DestroyUnityObject(legacyRaycaster);
        }

        if (GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }

    private void EnsureEventSystem(bool vrMode)
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.transform.SetParent(transform);
        eventSystemObject.AddComponent<EventSystem>();
        if (vrMode)
        {
            eventSystemObject.AddComponent<XRUIInputModule>();
        }
        else
        {
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void ClearExistingRoot()
    {
        Transform existing = transform.Find("HudRoot");
        if (existing != null)
        {
            DestroyUnityObject(existing.gameObject);
        }
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        if (color.a > 0.75f)
        {
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        return rect;
    }

    private RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Text CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = GetHudFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = text;
        label.alignByGeometry = true;
        label.raycastTarget = false;
        return label;
    }

    private RawImage CreateRawImage(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color normalColor,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), anchoredPosition, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = normalColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = normalColor,
            highlightedColor = normalColor == actionColor ? actionHoverColor : new Color(0.36f, 0.33f, 0.28f, 1f),
            pressedColor = new Color(0.14f, 0.22f, 0.26f, 1f),
            selectedColor = normalColor,
            disabledColor = new Color(0.16f, 0.15f, 0.14f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        button.onClick.AddListener(action);

        Text buttonText = CreateText("Label", rect, label, 13, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta);
        buttonText.raycastTarget = false;
        return button;
    }

    private string FormatMoveHistory(IReadOnlyList<string> moveHistory)
    {
        if (moveHistory.Count == 0)
        {
            return "Nenhuma jogada ainda.";
        }

        StringBuilder builder = new StringBuilder();
        int startIndex = Mathf.Max(0, moveHistory.Count - visibleMoveCount);
        for (int i = startIndex; i < moveHistory.Count; i++)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(moveHistory[i]);
        }

        return builder.ToString();
    }

    private static string CompactStatus(string status)
    {
        if (status.StartsWith("Turno:"))
        {
            return "Escolha uma peca para mover.";
        }

        return status;
    }

    private static string BuildHowToPlayText()
    {
        return "1. Clique em uma peca do turno atual.\n" +
            "2. Clique em uma casa destacada para mover.\n" +
            "3. Q/E giram a camera, scroll aproxima.\n" +
            "4. Esc cancela selecao, N reinicia.";
    }

    private static string GetPieceModelName(PieceView piece)
    {
        switch (piece.Kind)
        {
            case ChessPieceKind.Pawn:
                return "Matheus Duarte";
            case ChessPieceKind.Rook:
                return "Alex Fenner";
            case ChessPieceKind.Knight:
                return "Gustavo Cornalewski";
            case ChessPieceKind.Bishop:
                return "Rafael Scharer";
            case ChessPieceKind.Queen:
                return "Marta Rosecler Bez";
            case ChessPieceKind.King:
                return "Ricardo Ferreira de Oliveira";
            default:
                return "Peca classica";
        }
    }

    private static string BuildPieceProfileText(PieceView piece)
    {
        return $"Nome: {GetPieceFullName(piece)}\n" +
            $"Categoria: {GetPieceCategory(piece.Kind)}\n" +
            $"Registro: {GetPieceRegistry(piece.Kind)}";
    }

    private static string BuildPieceDescription(PieceView piece)
    {
        if (piece.Kind == ChessPieceKind.Pawn)
        {
            return "Peao representado por Matheus Duarte, criador do jogo.";
        }

        if (piece.Kind == ChessPieceKind.Queen)
        {
            return "Rainha representada por Marta Rosecler Bez, professora de Ciencias da Computacao da Universidade Feevale.";
        }

        if (piece.Kind == ChessPieceKind.King)
        {
            return "Rei representado por Ricardo Ferreira de Oliveira, professor de Ciencias da Computacao da Universidade Feevale.";
        }

        return $"{PieceKindName(piece.Kind)} representado por {GetPieceModelName(piece)}.";
    }

    private static string GetPieceFullName(PieceView piece)
    {
        return GetPieceModelName(piece);
    }

    private static string GetPieceCategory(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return "Criador do jogo";
            case ChessPieceKind.Queen:
                return "Professora";
            case ChessPieceKind.King:
                return "Professor";
            default:
                return "Colega";
        }
    }

    private static string GetPieceRegistry(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return "Matricula 0276899";
            case ChessPieceKind.Bishop:
                return "Matricula 040603";
            case ChessPieceKind.Knight:
                return "Matricula 0407923";
            case ChessPieceKind.Rook:
                return "Matricula 0403240";
            case ChessPieceKind.Queen:
                return "Professora de Ciencias da Computacao - Universidade Feevale";
            case ChessPieceKind.King:
                return "Professor de Ciencias da Computacao - Universidade Feevale";
            default:
                return "-";
        }
    }

    private static string PieceKindName(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return "Peao";
            case ChessPieceKind.Rook:
                return "Torre";
            case ChessPieceKind.Knight:
                return "Cavalo";
            case ChessPieceKind.Bishop:
                return "Bispo";
            case ChessPieceKind.Queen:
                return "Rainha";
            case ChessPieceKind.King:
                return "Rei";
            default:
                return "Peca";
        }
    }

    private static string SideName(ChessSide side)
    {
        return side == ChessSide.White ? "Brancas" : "Pretas";
    }

    private static string SideAdjective(ChessSide side)
    {
        return side == ChessSide.White ? "branco" : "preto";
    }

    private Font GetHudFont()
    {
        if (hudFont != null)
        {
            return hudFont;
        }

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null)
        {
            hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return hudFont;
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
