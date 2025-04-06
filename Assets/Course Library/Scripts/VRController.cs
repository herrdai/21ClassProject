using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VRController : MonoBehaviour
{
    [Header("VR Settings")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightRayInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRayInteractor;
    public float rayLength = 10f;
    public LayerMask interactableLayer;

    [Header("UI Settings")]
    public float uiDistance = 2f; // UI与相机的距离
    public float uiVerticalOffset = 0.5f; // UI垂直偏移
    public float uiHorizontalSpacing = 0.5f; // UI水平间距
    public Camera uiCamera; // UI专用相机

    private GameManager gameManager;
    private Camera mainCamera;
    private InputAction triggerAction;
    private XRInteractionManager interactionManager;
    private UIInputModule uiInputModule;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        mainCamera = Camera.main;

        // 设置UI相机
        SetupUICamera();

        // Setup ray interactors
        SetupRayInteractors();

        // Setup UI interaction
        SetupUIInteraction();

        // Setup input actions
        SetupInputActions();

        // Setup UI positions
        SetupUIPositions();
    }

    private void SetupUICamera()
    {
        // 创建UI相机
        GameObject uiCameraObj = new GameObject("UI Camera");
        uiCamera = uiCameraObj.AddComponent<Camera>();
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = LayerMask.GetMask("UI");
        uiCamera.depth = 1;
        uiCamera.nearClipPlane = 0.01f;
        uiCamera.farClipPlane = 1000f;
        uiCamera.transform.SetParent(mainCamera.transform);
        uiCamera.transform.localPosition = Vector3.zero;
        uiCamera.transform.localRotation = Quaternion.identity;
    }

    private void SetupRayInteractors()
    {
        if (rightRayInteractor == null)
        {
            Debug.LogWarning("Right ray interactor not set!");
        }
        else
        {
            rightRayInteractor.maxRaycastDistance = rayLength;
            rightRayInteractor.raycastMask = interactableLayer;
            rightRayInteractor.enableUIInteraction = true; // 启用UI交互
        }

        if (leftRayInteractor == null)
        {
            Debug.LogWarning("Left ray interactor not set!");
        }
        else
        {
            leftRayInteractor.maxRaycastDistance = rayLength;
            leftRayInteractor.raycastMask = interactableLayer;
            leftRayInteractor.enableUIInteraction = true; // 启用UI交互
        }
    }

    private void SetupUIInteraction()
    {
        // 确保场景中有 XR Interaction Manager
        interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogWarning("No XR Interaction Manager found in scene!");
        }

        // 确保有 UI Input Module
        uiInputModule = FindObjectOfType<UIInputModule>();
        if (uiInputModule == null)
        {
            Debug.LogWarning("No UI Input Module found in scene!");
        }
    }

    private void SetupInputActions()
    {
        // Create input action map
        var actionMap = new InputActionMap("VR");
        triggerAction = actionMap.AddAction("Trigger", InputActionType.Button);
        
        // Bind right controller trigger button
        triggerAction.AddBinding("<XRController>{RightHand}/triggerPressed");
        
        // Add callback
        triggerAction.performed += OnTriggerPerformed;
        
        // Enable input action
        triggerAction.Enable();
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CheckAndScoreObject();
        }
    }

    private void CheckAndScoreObject()
    {
        if (!gameManager.IsGameActive) return;

        // Use right ray interactor to detect objects
        if (rightRayInteractor != null && rightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (gameManager.IsObjectActive(hitObject))
            {
                // Increase score
                gameManager.UpdateScore(1);
                
                // Destroy the hit object
                gameManager.RemoveActiveObject(hitObject);
            }
        }
    }

    private void SetupUIPositions()
    {
        if (mainCamera == null) return;

        // 获取所有UI Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        foreach (Canvas canvas in canvases)
        {
            // 设置Canvas为屏幕空间相机模式
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            
            // 设置Canvas大小
            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 设置Canvas的尺寸
                rectTransform.sizeDelta = new Vector2(1920, 1080);
                
                // 确保Canvas的排序顺序正确
                canvas.sortingOrder = 0;
                canvas.overrideSorting = true;
                
                // 设置Canvas的缩放
                canvas.transform.localScale = Vector3.one;
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup input actions
        if (triggerAction != null)
        {
            triggerAction.performed -= OnTriggerPerformed;
            triggerAction.Disable();
        }
    }
} 