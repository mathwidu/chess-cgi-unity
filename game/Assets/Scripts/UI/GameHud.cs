using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed partial class GameHud : MonoBehaviour
{
    private static readonly Vector3 VrPanelPosition = new Vector3(0f, 1.4f, 4f);
    private const float VrPanelScale = 0.0032f;

    [SerializeField] private ChessGameController gameController;
    [SerializeField] private int visibleMoveCount = 6;

    private readonly Color panelColor = new Color32(10, 57, 36, 248);
    private readonly Color panelStrongColor = new Color32(4, 43, 27, 255);
    private readonly Color previewSurfaceColor = new Color32(24, 66, 43, 255);
    private readonly Color overlayColor = new Color32(8, 37, 29, 255);
    private readonly Color textColor = Color.white;
    private readonly Color mutedTextColor = new Color32(202, 228, 211, 255);
    private readonly Color accentColor = new Color32(255, 221, 0, 255);
    private readonly Color actionColor = new Color32(255, 221, 0, 255);
    private readonly Color actionHoverColor = new Color32(255, 235, 108, 255);
    private readonly Color neutralButtonColor = new Color32(11, 75, 43, 255);

    private bool showStartScreen = true;
    private bool showHowToPlay;
    private bool chooseComputer = true;
    private ChessSide chosenSide = ChessSide.White;
    private ComputerDifficulty chosenDifficulty = ComputerDifficulty.Beginner;
    private RectTransform computerErrorPanel;
    private Font hudFont;
    private Font hudBoldFont;
    private RectTransform hudRoot;
    private RectTransform startOverlay;
    private RectTransform howToPlayPanel;
    private RectTransform promotionPanel;
    private RectTransform selectedPiecePanel;
    private RawImage selectedPiecePreviewImage;
    private Text turnText;
    private Text statusText;
    private Text moveHistoryText;
    private Text howToPlayButtonText;
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
    private bool panelSeatedAsBlack;

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

        if (hudCanvas != null && hudCanvas.renderMode == RenderMode.WorldSpace && panelSeatedAsBlack != XRRig.SeatedAsBlack)
        {
            PlaceWorldPanel((RectTransform)transform);
        }

        RefreshInterface();
    }

    public void RebuildInterface()
    {
        EnsureCanvasInfrastructure();
        ClearExistingRoot();

        hudRoot = CreateRect("HudRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);

        BuildMatchInterface();
        BuildStartMenu();
        BuildHelpDialog();

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

        RefreshStartMenu();
        SetActive(matchInterface, !showStartScreen);
        SetActive(computerErrorPanel, hasController && gameController.HasComputerError && !showStartScreen);
        SetActive(startOverlay, showStartScreen);
        SetActive(howToPlayPanel, showHowToPlay);
        SetActive(promotionPanel, awaitingPromotion && !showStartScreen);
        if (matchSummaryText != null && hasController)
            matchSummaryText.text = gameController.IsAgainstComputer
                ? "CONTRA IA  /  " + DifficultyName(gameController.Difficulty).ToUpperInvariant() + "  /  " + SideName(gameController.HumanSide).ToUpperInvariant()
                : "DOIS JOGADORES  /  PARTIDA LOCAL";

        if (turnText != null)
        {
            turnText.text = !hasController || gameController.CurrentTurn == ChessSide.White ? "Brancas jogam" : "Pretas jogam";
        }

        if (statusText != null)
        {
            string status = hasController ? CompactStatus(gameController.StatusMessage) : "Escolha uma peça para mover.";
            statusText.text = status;
            statusText.color = status.StartsWith("Movimento invalido") ? new Color(0.95f, 0.45f, 0.36f, 1f) : textColor;
        }

        if (moveHistoryText != null)
        {
            moveHistoryText.text = hasController ? FormatMoveHistory(gameController.MoveHistory) : "Nenhuma jogada ainda.";
        }

        RefreshSelectedPiecePanel(hasController ? gameController.SelectedPiece : null);
        RefreshNavigationFocus();

        if (howToPlayButtonText != null)
        {
            howToPlayButtonText.text = showHowToPlay && !showStartScreen ? "Ocultar" : "Como jogar";
        }

    }

    private void StartGame()
    {
        showStartScreen = false;
        showHowToPlay = false;
        if (gameController != null)
        {
            if (chooseComputer) gameController.StartComputerGame(chosenSide, chosenDifficulty);
            else gameController.StartLocalGame();
        }

        RefreshInterface();
    }

    private static string DifficultyName(ComputerDifficulty difficulty)
    {
        return difficulty == ComputerDifficulty.Beginner ? "Iniciante" :
            difficulty == ComputerDifficulty.Intermediate ? "Intermediário" : "Difícil";
    }

    private void RestartGame()
    {
        if (gameController != null) gameController.NewGame();
        RefreshInterface();
    }

    private void ShowMenu()
    {
        if (gameController != null) gameController.ReturnToMenu();
        showStartScreen = true;
        showHowToPlay = false;
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

    private void OnPerformanceModeChanged(bool enabled)
    {
        if (gameController != null)
        {
            gameController.SetPerformanceMode(enabled);
        }
    }

    private void ToggleHowToPlay()
    {
        if (!showHowToPlay && EventSystem.current != null)
            focusBeforeHelp = EventSystem.current.currentSelectedGameObject;
        showHowToPlay = !showHowToPlay;
        RefreshInterface();
        if (EventSystem.current != null)
        {
            GameObject target = showHowToPlay
                ? howToPlayPanel.GetComponentInChildren<Button>().gameObject : focusBeforeHelp;
            EventSystem.current.SetSelectedGameObject(target != null && target.activeInHierarchy ? target : null);
        }
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
        if (selectedPiecePreviewStage != null) DestroyUnityObject(selectedPiecePreviewStage.gameObject);

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
        SetActive(selectedPiecePanel, hasSelection && !showStartScreen);
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
            // A rendered 3D preview must not inherit the Canvas scale (especially in VR).
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
        canvasRect.localScale = Vector3.one * VrPanelScale;
        PlaceWorldPanel(canvasRect);

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

    private void PlaceWorldPanel(RectTransform canvasRect)
    {
        panelSeatedAsBlack = XRRig.SeatedAsBlack;
        Vector3 panelPosition = XRRig.SeatAwarePoint(VrPanelPosition);
        canvasRect.position = panelPosition;
        canvasRect.rotation = Quaternion.LookRotation(panelPosition - XRRig.SeatAwarePoint(XRRig.SeatEyePosition), Vector3.up);
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
            outline.effectColor = new Color(0.45f, 0.57f, 0.56f, 0.15f);
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
        label.font = fontStyle == FontStyle.Bold ? GetHudBoldFont() : GetHudFont();
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Normal;
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
        image.color = Color.white;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = normalColor,
            highlightedColor = normalColor == actionColor ? actionHoverColor : new Color32(24, 102, 61, 255),
            pressedColor = normalColor == actionColor ? new Color32(221, 190, 0, 255) : new Color32(5, 65, 35, 255),
            selectedColor = normalColor == actionColor ? actionHoverColor : new Color32(24, 102, 61, 255),
            disabledColor = new Color(0.16f, 0.15f, 0.14f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        button.onClick.AddListener(action);

        Text buttonText = CreateText("Label", rect, label, 20, FontStyle.Bold, normalColor == actionColor ? panelStrongColor : textColor, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta);
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
            return "Escolha uma peça para mover.";
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

        hudFont = Resources.Load<Font>("UI/Lato-Regular");
        if (hudFont == null) hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null)
        {
            hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return hudFont;
    }

    private Font GetHudBoldFont()
    {
        if (hudBoldFont == null) hudBoldFont = Resources.Load<Font>("UI/Lato-Bold");
        return hudBoldFont != null ? hudBoldFont : GetHudFont();
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
