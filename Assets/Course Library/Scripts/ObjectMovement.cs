using UnityEngine;

public class ObjectMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 1f; // 移动速度
    public float moveRange = 2f; // 移动范围
    public float moveOffset = 0f; // 移动偏移

    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // 随机初始相位
    }

    void Update()
    {
        // 使用正弦函数创建平滑的上下移动
        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * moveSpeed) * moveRange;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
} 