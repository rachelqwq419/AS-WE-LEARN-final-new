using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform target; // 把 Player 拖進來

    [Header("鏡頭移動速度")]
    public float smoothSpeed = 5f;

    [Header("鏡頭活動範圍 (請手動調整)")]
    public Vector2 minPosition; // 左下角極限 (X=左邊界, Y=下邊界)
    public Vector2 maxPosition; // 右上角極限 (X=右邊界, Y=上邊界)

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 計算目標位置 (保留原本的 Z 軸)
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 2. 限制 X 和 Y 軸的移動範圍 (最關鍵的一步！)
        // Mathf.Clamp(目前數值, 最小值, 最大值) 會確保數值不會跑出範圍
        float clampedX = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);

        Vector3 finalPosition = new Vector3(clampedX, clampedY, transform.position.z);

        // 3. 平滑移動鏡頭
        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed * Time.deltaTime);
    }
}