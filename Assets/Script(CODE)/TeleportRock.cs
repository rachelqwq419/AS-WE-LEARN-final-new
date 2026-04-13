using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 為了控制 UI
using System.Collections;

public class TeleportRock : MonoBehaviour
{
    [Header("目標場景名稱")]
    public string targetScene = "MenuScene"; // 預設去 MenuScene

    [Header("過場黑布 (請拖曳 UI Panel)")]
    public Image fadePanel;

    private bool isTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 判斷是否為玩家 (Player) 碰到
        // 為了保險，我們檢查是否有 Rigidbody2D (通常玩家身上都有)
        if (other.GetComponent<Rigidbody2D>() != null && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(TransitionAndLoad());
        }
    }

    IEnumerator TransitionAndLoad()
    {
        // 1. 開始轉場：讓黑布慢慢變不透明 (Fade In)
        if (fadePanel != null)
        {
            float timer = 0f;
            float duration = 1.0f; // 轉場需要幾秒

            while (timer < duration)
            {
                timer += Time.deltaTime;
                // 修改透明度 Alpha 值 (0 -> 1)
                Color c = fadePanel.color;
                c.a = timer / duration;
                fadePanel.color = c;
                yield return null; // 等待下一幀
            }

            // 確保最後是全黑
            Color finalColor = fadePanel.color;
            finalColor.a = 1f;
            fadePanel.color = finalColor;
        }

        // 2. 等待一下 (全黑停留時間)
        yield return new WaitForSeconds(0.5f);

        // 3. 切換場景
        SceneManager.LoadScene(targetScene);
    }
}