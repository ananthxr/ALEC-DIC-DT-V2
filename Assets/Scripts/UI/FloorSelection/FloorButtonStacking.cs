using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class FloorButtonStacking : MonoBehaviour
{
    [Header("Floor Button Settings")]
    [SerializeField] private Button mainFloorButton;
    [SerializeField] private List<GameObject> floorButtons = new List<GameObject>();
    [SerializeField] private float stackDistance = 250f;
    [SerializeField] private float firstButtonPadding = 100f;
    [SerializeField] private float baseAnimationDuration = 0.8f;
    [SerializeField] private float delayBetweenButtons = 0.1f;
    [SerializeField] private Ease animationEase = Ease.InFlash;

    [Header("Button State Management")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color pressedButtonColor = Color.gray;

    [Header("MasterUI Integration")]
    [SerializeField] private MasterUI masterUI;

    // State management
    private bool isExpanded = false;
    private Vector2 mainButtonPosition;
    private List<Vector2> originalFloorPositions = new List<Vector2>();
    private List<Image> floorButtonImages = new List<Image>();
    private List<Button> floorButtonComponents = new List<Button>();
    private int currentSelectedFloorIndex = -1; // -1 means no floor selected

    private void Start()
    {
        InitializeComponents();
        SetupInitialState();
    }

    private void InitializeComponents()
    {
        if (mainFloorButton == null)
            mainFloorButton = GetComponent<Button>();

        mainButtonPosition = ((RectTransform)mainFloorButton.transform).anchoredPosition;

        // Store original positions of floor buttons and cache their components
        foreach (GameObject floorButton in floorButtons)
        {
            originalFloorPositions.Add(((RectTransform)floorButton.transform).anchoredPosition);

            // Cache Image component (for color changes)
            Image buttonImage = floorButton.GetComponent<Image>();
            if (buttonImage == null)
                buttonImage = floorButton.GetComponentInChildren<Image>();
            floorButtonImages.Add(buttonImage);

            // Cache Button component
            Button buttonComponent = floorButton.GetComponent<Button>();
            if (buttonComponent == null)
                buttonComponent = floorButton.GetComponentInChildren<Button>();
            floorButtonComponents.Add(buttonComponent);
        }

        mainFloorButton.onClick.AddListener(ToggleFloorButtons);

        // Add click listeners to floor buttons
        SetupFloorButtonListeners();

        // Initialize all button colors to normal
        UpdateAllButtonColors();
    }

    private void SetupInitialState()
    {
        // Hide all floor buttons initially
        foreach (GameObject floorButton in floorButtons)
        {
            RectTransform buttonRect = (RectTransform)floorButton.transform;
            buttonRect.DOKill();
            floorButton.SetActive(false);
            buttonRect.anchoredPosition = mainButtonPosition;
        }
    }

    public void ToggleFloorButtons()
    {
        Debug.Log($"[FloorButtonStacking] Main floor button clicked - isExpanded: {isExpanded}");

        if (isExpanded)
        {
            Debug.Log("[FloorButtonStacking] Collapsing floor buttons");
            CollapseFloorButtons();
        }
        else
        {
            Debug.Log("[FloorButtonStacking] Expanding floor buttons");
            ExpandFloorButtons();
        }

        UpdateAllButtonColors();

        // Notify MasterUI
        if (masterUI != null)
        {
            masterUI.UpdateUIElementState("FloorButtonStack", isExpanded);
        }
    }

    private void ExpandFloorButtons()
    {
        isExpanded = true;

        // Calculate target positions for floor buttons
        List<Vector2> targetPositions = CalculateStackPositions();

        // Animate floor buttons with staggered start times
        for (int i = 0; i < floorButtons.Count; i++)
        {
            GameObject floorButton = floorButtons[i];
            Vector2 targetPosition = targetPositions[i];
            RectTransform buttonRect = (RectTransform)floorButton.transform;

            buttonRect.DOKill();
            floorButton.SetActive(true);

            float buttonDelay = i * delayBetweenButtons;

            buttonRect.DOAnchorPos(targetPosition, baseAnimationDuration)
                .SetEase(animationEase)
                .SetDelay(buttonDelay)
                .SetUpdate(true);
        }
    }

    private void CollapseFloorButtons()
    {
        isExpanded = false;

        // Animate floor buttons back in reverse order
        for (int i = 0; i < floorButtons.Count; i++)
        {
            GameObject floorButton = floorButtons[i];
            RectTransform buttonRect = (RectTransform)floorButton.transform;

            buttonRect.DOKill();

            // Reverse order delay for smooth collapse (top buttons start first)
            float buttonDelay = (floorButtons.Count - 1 - i) * delayBetweenButtons;

            GameObject buttonToHide = floorButton;

            buttonRect.DOAnchorPos(mainButtonPosition, baseAnimationDuration)
                .SetEase(animationEase)
                .SetDelay(buttonDelay)
                .SetUpdate(true)
                .OnComplete(() => buttonToHide.SetActive(false));
        }
    }

    private List<Vector2> CalculateStackPositions()
    {
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < floorButtons.Count; i++)
        {
            // Add padding before first button, then use consistent stackDistance
            float totalDistance = firstButtonPadding + (stackDistance * (i + 1));
            Vector2 targetPosition = mainButtonPosition + Vector2.down * totalDistance;
            positions.Add(targetPosition);
        }

        return positions;
    }

    private void SetupFloorButtonListeners()
    {
        Debug.Log($"[FloorButtonStacking] Setting up listeners for {floorButtons.Count} floor buttons");

        for (int i = 0; i < floorButtonComponents.Count; i++)
        {
            Button floorButtonComponent = floorButtonComponents[i];

            if (floorButtonComponent != null)
            {
                int floorIndex = i; // Capture for closure
                floorButtonComponent.onClick.AddListener(() => OnFloorButtonClicked(floorIndex));
                Debug.Log($"[FloorButtonStacking] Added listener: Button {i} -> Floor Index {floorIndex}");
            }
        }
    }

    private void OnFloorButtonClicked(int floorIndex)
    {
        Debug.Log($"[FloorButtonStacking] Floor button {floorIndex} clicked!");

        // Check if clicking the same floor (toggle behavior)
        if (currentSelectedFloorIndex == floorIndex)
        {
            Debug.Log($"[FloorButtonStacking] Same floor {floorIndex} clicked - deselecting");
            currentSelectedFloorIndex = -1; // Deselect

            // Collapse menu
            if (isExpanded)
            {
                CollapseFloorButtons();
            }
        }
        else
        {
            Debug.Log($"[FloorButtonStacking] Floor {floorIndex} selected");
            currentSelectedFloorIndex = floorIndex; // Update selection

            // Collapse the button menu
            if (isExpanded)
            {
                CollapseFloorButtons();
            }
        }

        // Update button colors to show current selection
        UpdateAllButtonColors();

        // Notify MasterUI of floor selection
        if (masterUI != null)
        {
            masterUI.UpdateFloorButtonState(floorIndex, currentSelectedFloorIndex == floorIndex);
        }
    }

    private void UpdateAllButtonColors()
    {
        for (int i = 0; i < floorButtonImages.Count; i++)
        {
            Image buttonImage = floorButtonImages[i];
            if (buttonImage != null)
            {
                if (i == currentSelectedFloorIndex)
                {
                    buttonImage.color = pressedButtonColor;
                }
                else
                {
                    buttonImage.color = normalButtonColor;
                }
            }
        }
    }

    // Public properties
    public bool IsExpanded => isExpanded;
    public int CurrentSelectedFloorIndex => currentSelectedFloorIndex;

    private void OnDestroy()
    {
        // Kill all DOTween animations
        foreach (GameObject floorButton in floorButtons)
        {
            if (floorButton != null)
            {
                ((RectTransform)floorButton.transform).DOKill();
            }
        }

        if (mainFloorButton != null)
            mainFloorButton.onClick.RemoveListener(ToggleFloorButtons);

        // Remove floor button listeners
        for (int i = 0; i < floorButtonComponents.Count; i++)
        {
            Button floorButtonComponent = floorButtonComponents[i];
            if (floorButtonComponent != null)
            {
                floorButtonComponent.onClick.RemoveAllListeners();
            }
        }
    }
}
