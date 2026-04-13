using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject levelSelectPanel;

    [Header("Level 按鈕文字 (請順序拖入 Lv1 到 Lv6 嘅 Text)")]
    public TMP_Text[] levelButtonLabels;

    private string currentSubject;

    void Start()
    {
        // 如果無記錄，就預設當佢係 "Chinese" (防呆機制)
        currentSubject = PlayerPrefs.GetString("CurrentSubject", "Chinese");
        Debug.Log("載入科目區域: " + currentSubject);

        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(true);
        }

        string[] levelNames = { "Grade 1 Level", "Grade 2 Level", "Grade 3 Level", "Grade 4 Level", "Grade 5 Level", "Grade 6 Level" };

        if (levelButtonLabels != null)
        {
            for (int i = 0; i < levelButtonLabels.Length && i < levelNames.Length; i++)
            {
                if (levelButtonLabels[i] != null)
                {
                    levelButtonLabels[i].text = levelNames[i];
                }
            }
        }
    }

    // === 給 [Lv1~Lv6 按鈕] 用的函式 ===
    public void SelectLevel(int level)
    {
        // 1. 將科目同等級寫入 GameData，等戰鬥系統讀取
        GameData.chosenSubject = currentSubject;
        GameData.chosenLevel = level;

        Debug.Log("最終決定: " + currentSubject + " - Lv." + level);

        // 2. 載入戰鬥場景
        SceneManager.LoadScene("BattleScene");
    }

    // === 給 [X 關閉按鈕] 用的函式 ===
    public void ClosePanel()
    {
        // 如果你想玩家撳 X 可以返出去大地圖，可以取消下面註解：
        SceneManager.LoadScene("Map_MainCity"); 

        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }
    }
}