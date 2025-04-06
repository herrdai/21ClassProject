using UnityEngine;
using Unity.XR.CoreUtils;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f; // 旋转速度
    [SerializeField] private float moveSpeed = 5f; // 移动速度
    [SerializeField] private float minVerticalAngle = -45f; // 最小垂直角度
    [SerializeField] private float maxVerticalAngle = 45f;  // 最大垂直角度
    
    private float verticalRotation = 0f;
    private bool isVRMode;
    private GameManager gameManager;

    void Start()
    {
        // 获取GameManager引用
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            isVRMode = gameManager.IsVRMode;
            if (isVRMode)
            {
                // 在VR模式下禁用这个脚本
                enabled = false;
                Debug.Log("VR模式：相机控制已禁用");
                return;
            }
        }

        // 锁定并隐藏鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("非VR模式：相机控制已启用");
    }

    void Update()
    {
        if (isVRMode) return;

        // WASD移动控制
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S
        
        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 鼠标移动控制视角
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // 水平旋转（左右）
        transform.Rotate(Vector3.up * mouseX);

        // 垂直旋转（上下）
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(verticalRotation, transform.localEulerAngles.y, 0);

        // 按ESC键显示/隐藏鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? 
                              CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }
    }
} 