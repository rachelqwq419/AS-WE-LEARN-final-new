using UnityEngine;
using TMPro;

public class QuitGameManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject quitConfirmPanel; // 退出確認視窗 (Panel)
    public TextMeshProUGUI analysisText; // 顯示最弱學科嘅文字

    void Start()
    {
        // 確保一開始視窗係收埋嘅
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    // 當玩家撳右上角個「結束遊戲」掣時呼叫
    public void OpenQuitPanel()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
            UpdateAnalysisText(); // 更新最弱學科提示
            Time.timeScale = 0; // 暫停遊戲
        }
    }

    // 當玩家撳「取消」時呼叫
    public void CancelQuit()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
            Time.timeScale = 1; // 恢復遊戲
        }
    }

    // 當玩家撳「確定」時呼叫
    public void ConfirmQuit()
    {
        // 恢復時間，以防萬一
        Time.timeScale = 1;

        Debug.Log("玩家退出遊戲！");

        // 離開遊戲
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 如果喺 Editor 入面，停止 Play Mode
#else
            Application.Quit(); // 如果已經 Build 咗出嚟，直接關閉程式
#endif
    }

    // === 計算並更新最弱學科 ===
    private void UpdateAnalysisText()
    {
        if (analysisText == null || Summary.Instance == null) return;

        var weakestInfo = Summary.Instance.GetWeakestAreaInfo();

        if (weakestInfo.count == 0)
        {
            // 表現完美時嘅文字
            analysisText.text = "<b><size=120%><color=#55FF55>Excellent Progress!</color></size></b>\n\n" +
                               "Your performance is perfect with no wrong answers!\n\n" +
                               "<size=90%>Are you sure you want to end your study session?</size>";
        }
        else
        {
            // 有錯題時嘅數據分析報告
            // 使用 <color=#FFCC00> (金色) 突出科目，使用 \n 換行
            analysisText.text = "<b><size=125%><color=#FFFFFF>Learning Analysis</color></size></b>\n\n" +
                               $"It seems you struggled a bit in the \n<b><color=#FFCC00>\"{weakestInfo.weakest} Section\"</color></b>\n\n" +
                               $"<size=90%>Targeted practice is recommended to improve your mastery.\n\n" +
                               "<b>Are you sure you want to leave now?</b></size>";
        }
    }
}