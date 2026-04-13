using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // 讓原本的位置固定住
    private Vector3 originalPos;

    void Start()
    {
        // 遊戲開始時，記住攝影機原本在哪裡
        originalPos = transform.localPosition;
    }

    // 給外部呼叫的震動功能
    // duration = 震動幾秒 (通常 0.1 ~ 0.3 秒)
    // magnitude = 震動多大方 (通常 0.1 ~ 0.5)
    public IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 在原本位置附近隨機亂跳
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            // 等待下一幀
            yield return null;
        }

        // 時間到，強制歸位 (不然鏡頭會歪掉)
        transform.localPosition = originalPos;
    }
}