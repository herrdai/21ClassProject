using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UISetup : MonoBehaviour
{
    [Header("字体设置")]
    public TMP_FontAsset chineseFont; // 添加中文字体引用

    public GameManager gameManager;
    private Canvas mainCanvas;
    private GraphicRaycaster graphicRaycaster;
    private TrackedDeviceGraphicRaycaster vrGraphicRaycaster;
    private bool isVRMode;

    void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager reference not set!");
            return;
        }

        isVRMode = gameManager.IsVRMode;
        SetupUI();
    }

    void SetupUI()
    {
        // Create or get main Canvas
        mainCanvas = CreateOrGetMainCanvas();
        if (mainCanvas == null) return;

        // Setup canvas based on mode
        SetupCanvasForMode();

        // Create game UI group
        GameObject gameUI = CreateGameUI();
        
        // Create title screen
        GameObject titleScreen = CreateTitleScreen();
        
        // Create game over panel
        GameObject gameOverPanel = CreateGameOverPanel();

        // Setup GameManager references
        SetupGameManagerReferences(gameUI, titleScreen, gameOverPanel);
    }

    private Canvas CreateOrGetMainCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            
            // Add required components
            canvasObj.AddComponent<CanvasScaler>();
            graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();
        }
        return canvas;
    }

    private void SetupCanvasForMode()
    {
        if (isVRMode)
        {
            // VR mode setup
            mainCanvas.renderMode = RenderMode.WorldSpace;
            
            // Set canvas size - 减小画布尺寸
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(400, 300); // 缩小画布尺寸
            }

            // 找到XR Origin和相机
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                // 将Canvas放置在相机前方较近的位置
                Vector3 cameraPosition = xrOrigin.Camera.transform.position;
                Vector3 cameraForward = xrOrigin.Camera.transform.forward;
                
                // 设置Canvas位置 - 距离减小到0.8米，高度降低到1.1米
                mainCanvas.transform.position = cameraPosition + cameraForward * 0.8f;
                mainCanvas.transform.position = new Vector3(mainCanvas.transform.position.x, 1.1f, mainCanvas.transform.position.z);
                
                // 使Canvas面向相机
                mainCanvas.transform.LookAt(cameraPosition);
                mainCanvas.transform.Rotate(0, 180, 0, Space.Self);
            }
            else
            {
                // 如果找不到XR相机，使用默认位置
                mainCanvas.transform.position = new Vector3(0, 1.1f, 0.8f);
                mainCanvas.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            // 调整缩放 - 由于距离更近，稍微缩小一点
            mainCanvas.transform.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f);
            
            // Setup VR interaction
            if (vrGraphicRaycaster == null)
            {
                if (graphicRaycaster != null)
                {
                    Destroy(graphicRaycaster);
                }
                vrGraphicRaycaster = mainCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }
        else
        {
            // Non-VR mode setup
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.transform.localScale = Vector3.one;
        }
    }

    private GameObject CreateGameUI()
    {
        GameObject gameUI = new GameObject("Game UI");
        gameUI.transform.SetParent(mainCanvas.transform, false);

        // 创建Score Text
        GameObject scoreObj = CreateTextObject("Score Text", "分数: 0", new Vector2(50, -50));
        scoreObj.transform.SetParent(gameUI.transform, false);

        // 创建Missed Text
        GameObject missedObj = CreateTextObject("Missed Text", "错过: 0", new Vector2(50, -100));
        missedObj.transform.SetParent(gameUI.transform, false);

        // 创建Timer Text
        GameObject timerObj = CreateTextObject("Timer Text", "时间: 60", new Vector2(50, -150));
        timerObj.transform.SetParent(gameUI.transform, false);

        gameUI.SetActive(false);
        return gameUI;
    }

    private GameObject CreateTitleScreen()
    {
        GameObject titleScreen = new GameObject("Title Screen");
        titleScreen.transform.SetParent(mainCanvas.transform, false);

        // 创建标题文本 - 调整位置
        GameObject titleText = CreateTextObject("Title Text", "VR 射击游戏", new Vector2(0, 80));
        titleText.transform.SetParent(titleScreen.transform, false);
        TextMeshProUGUI titleTMP = titleText.GetComponent<TextMeshProUGUI>();
        titleTMP.fontSize = 50; // 减小字体大小
        titleTMP.color = Color.yellow;

        // 创建难度按钮，减小间距
        float buttonSpacing = 60f; // 减小按钮间距
        CreateDifficultyButton("Easy Button", "简单", new Vector2(0, 10), titleScreen.transform);
        CreateDifficultyButton("Medium Button", "中等", new Vector2(0, -buttonSpacing), titleScreen.transform);
        CreateDifficultyButton("Hard Button", "困难", new Vector2(0, -buttonSpacing * 2), titleScreen.transform);

        return titleScreen;
    }

    private GameObject CreateGameOverPanel()
    {
        GameObject panel = new GameObject("Game Over Panel");
        panel.transform.SetParent(mainCanvas.transform, false);

        // 创建游戏结束文本
        GameObject gameOverText = CreateTextObject("Game Over Text", "游戏结束", new Vector2(0, 50));
        gameOverText.transform.SetParent(panel.transform, false);
        gameOverText.GetComponent<TextMeshProUGUI>().fontSize = 36;

        // 创建最终分数文本
        GameObject finalScoreText = CreateTextObject("Final Score Text", "最终分数: 0", new Vector2(0, 0));
        finalScoreText.transform.SetParent(panel.transform, false);

        // 创建重启按钮
        CreateButton("Restart Button", "重新开始", new Vector2(0, -50), panel.transform);

        panel.SetActive(false); // 初始时隐藏游戏结束面板
        return panel;
    }

    private GameObject CreateTextObject(string name, string text, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // 设置字体
        if (chineseFont != null)
        {
            tmp.font = chineseFont;
            tmp.fontSharedMaterial = chineseFont.material;
        }
        else
        {
            Debug.LogError($"未设置中文字体资源！请在 Inspector 中为 {gameObject.name} 设置支持中文的TMP字体。");
            // 尝试加载默认字体
            var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null)
            {
                tmp.font = defaultFont;
                tmp.fontSharedMaterial = defaultFont.material;
            }
        }
        
        tmp.text = text;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        
        return obj;
    }

    private void CreateDifficultyButton(string name, string text, Vector2 position, Transform parent)
    {
        GameObject buttonObj = CreateButton(name, text, position, parent);
        Button button = buttonObj.GetComponent<Button>();
        
        // 设置按钮颜色
        ColorBlock colors = button.colors;
        colors.normalColor = name.Contains("Easy") ? new Color(0.4f, 1f, 0.4f, 1f) : 
                           name.Contains("Medium") ? new Color(1f, 1f, 0.4f, 1f) : 
                           new Color(1f, 0.4f, 0.4f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.gray;
        button.colors = colors;

        // 调整按钮大小
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50); // 减小按钮大小
    }

    private GameObject CreateButton(string name, string text, Vector2 position, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 50); // 保持按钮大小一致

        Button button = buttonObj.AddComponent<Button>();
        
        // 添加按钮背景图片
        GameObject background = new GameObject("Background");
        background.transform.SetParent(buttonObj.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;

        // 添加按钮文本
        GameObject textObj = CreateTextObject("Text", text, Vector2.zero);
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 32; // 调整字体大小
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return buttonObj;
    }

    private void SetupGameManagerReferences(GameObject gameUI, GameObject titleScreen, GameObject gameOverPanel)
    {
        if (gameManager != null)
        {
            // 设置分数文本
            gameManager.scoreText = gameUI.transform.Find("Score Text")?.GetComponent<TextMeshProUGUI>();
            
            // 设置错过文本
            gameManager.missedText = gameUI.transform.Find("Missed Text")?.GetComponent<TextMeshProUGUI>();
            
            // 设置计时器文本
            gameManager.timerText = gameUI.transform.Find("Timer Text")?.GetComponent<TextMeshProUGUI>();
            
            // 设置标题界面
            gameManager.titleScreen = titleScreen;
            
            // 设置游戏结束面板和相关文本
            gameManager.gameOverPanel = gameOverPanel;
            gameManager.gameOverText = gameOverPanel.transform.Find("Game Over Text")?.GetComponent<TextMeshProUGUI>();
            gameManager.finalScoreText = gameOverPanel.transform.Find("Final Score Text")?.GetComponent<TextMeshProUGUI>();
            
            // 设置按钮
            gameManager.restartButton = gameOverPanel.transform.Find("Restart Button")?.GetComponent<Button>();
            gameManager.easyButton = titleScreen.transform.Find("Easy Button")?.GetComponent<Button>();
            gameManager.mediumButton = titleScreen.transform.Find("Medium Button")?.GetComponent<Button>();
            gameManager.hardButton = titleScreen.transform.Find("Hard Button")?.GetComponent<Button>();

            // 设置按钮点击事件
            if (gameManager.restartButton != null)
                gameManager.restartButton.onClick.AddListener(gameManager.RestartGame);
            
            if (gameManager.easyButton != null)
                gameManager.easyButton.onClick.AddListener(() => gameManager.StartGame(1));
            
            if (gameManager.mediumButton != null)
                gameManager.mediumButton.onClick.AddListener(() => gameManager.StartGame(2));
            
            if (gameManager.hardButton != null)
                gameManager.hardButton.onClick.AddListener(() => gameManager.StartGame(3));
        }
    }
}

// 修改Billboard类
public class Billboard : MonoBehaviour
{
    private Transform mainCamera;
    
    void Start()
    {
        // 使用XROrigin替代XR.Interaction.Toolkit.XROrigin
        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            mainCamera = xrOrigin.Camera.transform;
            Debug.Log("找到XR相机");
        }
        else
        {
            // 如果找不到XR相机，尝试找普通相机
            mainCamera = Camera.main?.transform;
            if (mainCamera != null)
            {
                Debug.Log("使用普通相机");
            }
            else
            {
                Debug.LogWarning("未找到任何相机！");
            }
        }
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward,
                           mainCamera.rotation * Vector3.up);
        }
    }
} 