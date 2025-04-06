using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class GameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public float gameTime = 60f; // 游戏时间（秒）
    public int maxMissedObjects = 10; // 最大错过物体数
    public float spawnInterval = 2f; // 生成间隔
    public float objectLifetime = 8f; // 物体存在时间
    public float spawnRadius = 8f; // 生成半径
    public float minSpawnHeight = 1.5f; // 最小生成高度
    public float maxSpawnHeight = 3f; // 最大生成高度
    public float scoreRadius = 0.5f; // 得分判定半径
    public float minSpawnDistance = 5f; // 最小生成距离
    public float maxSpawnDistance = 8f; // 最大生成距离

    [Header("VR设置")]
    public bool isVRMode = false; // 是否启用VR模式
    public XROrigin xrOrigin; // XR设备引用
    public float vrSpawnRadius = 3f; // VR模式下的生成半径
    private Vector3 initialXRPosition = new Vector3(0, 0, 0); // 初始 XR 位置
    private Vector3 initialCameraPosition = new Vector3(0, 1.6f, 0);
    private XRBaseController leftController;
    private XRBaseController rightController;
    private Camera xrCamera;
    private Vector3 initialLeftControllerPosition = new Vector3(-0.2f, 1.2f, 0.3f);
    private Vector3 initialRightControllerPosition = new Vector3(0.2f, 1.2f, 0.3f);
    private Quaternion initialCameraRotation;
    private Quaternion initialLeftControllerRotation;
    private Quaternion initialRightControllerRotation;

    // 添加 IsVRMode 属性
    public bool IsVRMode => isVRMode;

    [Header("UI引用")]
    public TextMeshProUGUI timerText; // 计时器文本
    public TextMeshProUGUI scoreText; // 分数文本
    public TextMeshProUGUI missedText; // 错过物体数文本
    public GameObject gameOverPanel; // 游戏结束面板
    public TextMeshProUGUI gameOverText; // 游戏结束文本
    public TextMeshProUGUI finalScoreText; // 最终分数文本
    public GameObject titleScreen; // 标题界面
    public Button easyButton; // 简单难度按钮
    public Button mediumButton; // 中等难度按钮
    public Button hardButton; // 困难难度按钮
    public Button restartButton; // 重启按钮

    [Header("射击设置")]
    public float shootRange = 100f; // 射击范围
    public LayerMask targetLayer; // 目标物体的层
    public GameObject hitEffect; // 击中特效
    public AudioClip shootSound; // 射击音效
    public AudioClip hitSound; // 击中音效
    public AudioClip missSound; // 未击中音效

    [SerializeField] private float baseSpawnRate = 2.5f; // 基础生成速度调整为2.5秒
    [SerializeField] private List<GameObject> objects;

    private float currentTime; // 当前时间
    private int score; // 分数
    private int missedObjects = 0; // 错过的物体数
    private bool isGameActive; // 游戏是否进行中
    public List<GameObject> ActiveObjects { get; private set; } = new List<GameObject>(); // 当前活跃的物体
    private AudioSource audioSource; // 音频源组件
    private float spawnRate;
    private int currentDifficulty = 1;
    private bool isClockwise;
    private bool isFirstSpawn = true;
    private const float angleStep = 25f; // 固定的角度间隔
    private Camera mainCamera;

    // 视野相关参数
    private const float cameraFOV = 60f; // 摄像机视野角度
    private const float visibleAngle = 30f; // 物体出现在视野中的角度（视野边缘）

    public bool IsGameActive { get; private set; }

    private void Start()
    {
        // 初始化相机引用
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("未找到主相机！请确保场景中有相机。");
            return;
        }

        // 初始化音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 检查必要的UI组件
        if (!CheckRequiredComponents())
        {
            Debug.LogError("一些必要的UI组件未设置！请在Inspector中设置所有必要的UI引用。");
            return;
        }

        // 检查物体预制体列表
        if (objects == null || objects.Count == 0)
        {
            Debug.LogError("未设置要生成的物体预制体！请在Inspector中添加物体预制体。");
            return;
        }

        // 初始化游戏状态
        IsGameActive = false;
        if (titleScreen != null) titleScreen.SetActive(true);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        // 初始化VR设置
        if (isVRMode)
        {
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<XROrigin>();
                if (xrOrigin == null)
                {
                    Debug.LogError("未找到XR Origin！请确保场景中有XR Origin或禁用VR模式。");
                    return;
                }
            }
            
            // 禁用所有可能导致移动的组件
            DisableMovementComponents();

            // 获取XR相机引用
            xrCamera = xrOrigin.Camera;
            if (xrCamera != null)
            {
                // 固定相机位置和旋转
                xrCamera.transform.localPosition = initialCameraPosition;
                initialCameraRotation = xrCamera.transform.localRotation;
            }

            // 获取并固定控制器位置
            var controllers = FindObjectsOfType<XRBaseController>();
            foreach (var controller in controllers)
            {
                if (controller.name.ToLower().Contains("left"))
                {
                    leftController = controller;
                    controller.transform.localPosition = initialLeftControllerPosition;
                    initialLeftControllerRotation = controller.transform.localRotation;
                }
                else if (controller.name.ToLower().Contains("right"))
                {
                    rightController = controller;
                    controller.transform.localPosition = initialRightControllerPosition;
                    initialRightControllerRotation = controller.transform.localRotation;
                }
                
                // 禁用控制器的所有可能导致移动的组件
                DisableControllerMovement(controller);
            }

            // 固定XR Origin位置
            xrOrigin.transform.position = initialXRPosition;
            xrOrigin.transform.rotation = Quaternion.identity;
        }

        // 初始化UI显示
        UpdateScoreDisplay();
        UpdateMissedDisplay();
        UpdateTimerDisplay();
    }

    private bool CheckRequiredComponents()
    {
        bool hasAllComponents = true;

        if (timerText == null)
        {
            Debug.LogWarning("Timer Text未设置！");
            hasAllComponents = false;
        }

        if (scoreText == null)
        {
            Debug.LogWarning("Score Text未设置！");
            hasAllComponents = false;
        }

        if (missedText == null)
        {
            Debug.LogWarning("Missed Text未设置！");
            hasAllComponents = false;
        }

        if (titleScreen == null)
        {
            Debug.LogWarning("Title Screen未设置！");
            hasAllComponents = false;
        }

        return hasAllComponents;
    }

    private void Update()
    {
        if (!IsGameActive) return;

        // 确保VR设备保持在固定位置
        if (isVRMode)
        {
            // 固定 XR Origin
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = initialXRPosition;
                xrOrigin.transform.rotation = Quaternion.identity;
            }
            
            // 固定相机
            if (xrCamera != null)
            {
                xrCamera.transform.localPosition = initialCameraPosition;
                xrCamera.transform.localRotation = initialCameraRotation;
            }

            // 固定左控制器
            if (leftController != null)
            {
                leftController.transform.localPosition = initialLeftControllerPosition;
                leftController.transform.localRotation = initialLeftControllerRotation;
            }

            // 固定右控制器
            if (rightController != null)
            {
                rightController.transform.localPosition = initialRightControllerPosition;
                rightController.transform.localRotation = initialRightControllerRotation;
            }
        }

        // 更新计时器
        currentTime -= Time.deltaTime;
        UpdateTimerDisplay();

        // 检查射击输入
        if (Input.GetButtonDown("Fire1")) // 默认是鼠标左键
        {
            Shoot();
        }

        // 检查游戏结束条件
        if (currentTime <= 0 || missedObjects >= maxMissedObjects)
        {
            GameOver();
        }

        // 非VR模式下检查按J键得分
        if (!isVRMode && Input.GetKeyDown(KeyCode.J))
        {
            CheckAndScoreObject();
        }

        if (Input.GetKeyDown(KeyCode.R) && !IsGameActive)
        {
            RestartGame();
        }
    }

    private void Shoot()
    {
        // 从摄像机发射射线
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 播放射击音效
        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // 检测射线是否击中物体
        if (Physics.Raycast(ray, out hit, shootRange, targetLayer))
        {
            // 击中物体
            GameObject hitObject = hit.collider.gameObject;
            if (ActiveObjects.Contains(hitObject))
            {
                // 播放击中音效
                if (hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }

                // 显示击中特效
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, hit.point, Quaternion.identity);
                }

                // 增加分数
                score++;
                UpdateScoreDisplay();

                // 移除物体
                ActiveObjects.Remove(hitObject);
                Destroy(hitObject);
            }
        }
        else
        {
            // 未击中任何物体，播放未击中音效
            if (missSound != null)
            {
                audioSource.PlayOneShot(missSound);
            }
        }
    }

    private void CheckAndScoreObject()
    {
        // 获取屏幕中心点
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        
        // 检查每个活跃物体
        foreach (GameObject obj in ActiveObjects.ToArray())
        {
            if (obj == null) continue;

            // 获取物体在屏幕上的位置
            Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);
            
            // 计算物体到屏幕中心的距离
            float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), 
                                           new Vector2(screenCenter.x, screenCenter.y));
            
            // 如果物体在屏幕中心区域内
            if (distance <= scoreRadius * 100f) // 将scoreRadius转换为像素单位
            {
                // 增加分数
                score++;
                if (scoreText != null)
                {
                    scoreText.text = "Score: " + score;
                }
                
                // 销毁物体
                ActiveObjects.Remove(obj);
                Destroy(obj);
                return; // 只处理一个物体
            }
        }
    }

    public void StartGame(int difficulty)
    {
        currentDifficulty = difficulty;
        IsGameActive = true;
        score = 0;
        missedObjects = 0;
        currentTime = gameTime;
        
        // 根据难度设置生成速率，调整难度系数
        switch(difficulty)
        {
            case 1: // 简单
                spawnRate = baseSpawnRate * 1.2f; // 降低生成速度
                break;
            case 2: // 中等
                spawnRate = baseSpawnRate * 0.8f;
                break;
            case 3: // 困难
                spawnRate = baseSpawnRate * 0.5f;
                break;
            default:
                spawnRate = baseSpawnRate;
                break;
        }

        // 更新显示
        UpdateScoreDisplay();
        UpdateMissedDisplay();
        UpdateTimerDisplay();

        // 隐藏标题界面
        if (titleScreen != null)
        {
            titleScreen.SetActive(false);
        }

        // 开始生成物体
        StartCoroutine(SpawnObject());
    }

    private IEnumerator SpawnObject()
    {
        while (IsGameActive)
        {
            yield return new WaitForSeconds(spawnRate);

            if (objects != null && objects.Count > 0)
            {
                // 选择随机物体
                GameObject prefab = objects[Random.Range(0, objects.Count)];
                if (prefab != null)
                {
                    // 根据模式选择生成位置
                    Vector3 spawnPos = isVRMode ? GetVRSpawnPosition() : GetNormalSpawnPosition();
                    
                    // 生成物体
                    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                    ActiveObjects.Add(obj);

                    // 设置物体朝向
                    if (isVRMode)
                    {
                        // VR模式下，物体朝向玩家
                        Vector3 directionToPlayer = (xrOrigin.transform.position - obj.transform.position).normalized;
                        obj.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                    }
                    else
                    {
                        // 非VR模式下，物体朝向相机
                        obj.transform.LookAt(mainCamera.transform);
                    }

                    // 添加上升下降动画
                    StartCoroutine(ObjectFloatAnimation(obj));

                    // 设置物体存在时间
                    StartCoroutine(DestroyObjectAfterDelay(obj));
                }
            }
        }
    }

    private Vector3 GetVRSpawnPosition()
    {
        // 在玩家周围圆形区域内随机生成
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        float height = Random.Range(minSpawnHeight, maxSpawnHeight);

        // 计算玩家位置
        Vector3 playerPosition = xrOrigin != null ? xrOrigin.transform.position : Vector3.zero;

        // 使用极坐标计算生成位置
        float x = playerPosition.x + distance * Mathf.Cos(angle * Mathf.Deg2Rad);
        float z = playerPosition.z + distance * Mathf.Sin(angle * Mathf.Deg2Rad);
        
        return new Vector3(x, height, z);
    }

    private Vector3 GetNormalSpawnPosition()
    {
        // 在相机周围圆形区域内随机生成
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        float height = Random.Range(minSpawnHeight, maxSpawnHeight);

        // 计算相机位置
        Vector3 cameraPosition = mainCamera.transform.position;

        // 使用极坐标计算生成位置
        float x = cameraPosition.x + distance * Mathf.Cos(angle * Mathf.Deg2Rad);
        float z = cameraPosition.z + distance * Mathf.Sin(angle * Mathf.Deg2Rad);
        
        return new Vector3(x, height, z);
    }

    private IEnumerator ObjectFloatAnimation(GameObject obj)
    {
        if (obj == null) yield break;

        float startY = obj.transform.position.y;
        float targetY = startY + 2f; // 上升高度
        float duration = 1f; // 上升时间
        float elapsed = 0f;

        // 上升阶段
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newY = Mathf.Lerp(startY, targetY, t);
            obj.transform.position = new Vector3(obj.transform.position.x, newY, obj.transform.position.z);
            yield return null;
        }

        // 下降阶段
        elapsed = 0f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newY = Mathf.Lerp(targetY, startY, t);
            obj.transform.position = new Vector3(obj.transform.position.x, newY, obj.transform.position.z);
            yield return null;
        }
    }

    private IEnumerator DestroyObjectAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(objectLifetime);
        
        if (obj != null && ActiveObjects.Contains(obj))
        {
            ActiveObjects.Remove(obj);
            missedObjects++;
            UpdateMissedDisplay();
            Destroy(obj);
        }
    }

    public void UpdateScore(int scoreToAdd)
    {
        score += scoreToAdd;
        UpdateScoreDisplay();
    }

    public void ObjectMissed()
    {
        if (!IsGameActive) return;

        missedObjects++;
        if (missedText != null)
        {
            missedText.text = "Missed: " + missedObjects + "/" + maxMissedObjects;
        }

        if (missedObjects >= maxMissedObjects)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        IsGameActive = false;
        StopAllCoroutines();

        // 显示游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null)
            {
                finalScoreText.text = $"最终分数: {score}";
            }
        }
        else
        {
            Debug.LogError("Game Over Panel未设置！");
        }

        // 清理所有活跃物体
        foreach (GameObject obj in ActiveObjects.ToList())
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        ActiveObjects.Clear();

        Debug.Log($"游戏结束！最终分数：{score}");
    }

    public void RestartGame()
    {
        // 清理所有现有物体
        foreach (GameObject obj in ActiveObjects.ToList())
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        ActiveObjects.Clear();

        // 重置游戏状态
        score = 0;
        missedObjects = 0;
        isFirstSpawn = true;
        currentTime = gameTime;
        IsGameActive = true;

        // 更新UI显示
        UpdateScoreDisplay();
        UpdateMissedDisplay();
        UpdateTimerDisplay();

        // 再次确保UI状态正确
        if (titleScreen != null) titleScreen.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        // 确保游戏UI可见
        if (timerText != null) 
        {
            timerText.gameObject.SetActive(true);
            timerText.transform.parent.gameObject.SetActive(true); // 确保父对象也是激活的
        }
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.transform.parent.gameObject.SetActive(true);
        }
        if (missedText != null)
        {
            missedText.gameObject.SetActive(true);
            missedText.transform.parent.gameObject.SetActive(true);
        }

        // 开始生成物体
        isClockwise = Random.value > 0.5f;
        StopAllCoroutines();
        StartCoroutine(SpawnObject());

        Debug.Log("游戏重新开始");
        Debug.Log("UI状态检查: " + 
            $"Timer Active: {(timerText != null ? timerText.gameObject.activeSelf : "null")}, " +
            $"Timer Parent Active: {(timerText != null ? timerText.transform.parent.gameObject.activeSelf : "null")}, " +
            $"Score Active: {(scoreText != null ? scoreText.gameObject.activeSelf : "null")}, " +
            $"Score Parent Active: {(scoreText != null ? scoreText.transform.parent.gameObject.activeSelf : "null")}");
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(currentTime).ToString();
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    private void UpdateMissedDisplay()
    {
        if (missedText != null)
        {
            missedText.text = "Missed: " + missedObjects + "/" + maxMissedObjects;
        }
    }

    public void RemoveActiveObject(GameObject obj)
    {
        if (obj != null && ActiveObjects.Contains(obj))
        {
            ActiveObjects.Remove(obj);
            Destroy(obj);
        }
    }

    public bool IsObjectActive(GameObject obj)
    {
        return obj != null && ActiveObjects.Contains(obj);
    }

    private void DisableMovementComponents()
    {
        if (xrOrigin == null) return;

        // 禁用所有可能导致移动的组件
        var components = xrOrigin.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && (
                component.GetType().Name.Contains("Movement") ||
                component.GetType().Name.Contains("Locomotion") ||
                component.GetType().Name.Contains("Provider") ||
                component.GetType().Name.Contains("Controller")))
            {
                component.enabled = false;
            }
        }

        // 禁用 CharacterController
        var characterController = xrOrigin.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 禁用 Rigidbody
        var rb = xrOrigin.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void DisableControllerMovement(XRBaseController controller)
    {
        // 禁用控制器上的所有移动相关组件
        var components = controller.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component.GetType().Name.Contains("Movement") ||
                component.GetType().Name.Contains("Locomotion") ||
                component.GetType().Name.Contains("Provider"))
            {
                component.enabled = false;
            }
        }

        // 禁用物理组件
        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void OnDestroy()
    {
        // 清理所有活跃物体
        foreach (GameObject obj in ActiveObjects.ToList())
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        ActiveObjects.Clear();
        
        // 停止所有协程
        StopAllCoroutines();
    }
}