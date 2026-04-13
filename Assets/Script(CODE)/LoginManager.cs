using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.IO;

public class LoginManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text feedbackText;

    [Header("雲端名單設定")]
    [Tooltip("請貼上 Google Sheet 發佈為 CSV 的連結")]
    public string studentCsvUrl = "YOUR_GOOGLE_SHEET_CSV_LINK_HERE";

    // 暫存下載返嚟嘅「帳號:密碼」字典
    private Dictionary<string, string> validStudents = new Dictionary<string, string>();

    void Start()
    {
        // 1. 顯示本機存檔根路徑，方便調試
        Debug.Log($"<color=yellow>[System]</color> 本機存檔總目錄: {Application.persistentDataPath}");

        // 2. 遊戲啟動即同步雲端名單
        StartCoroutine(DownloadStudentList());
    }

    // 綁定落你個 Quit Button 度
    public void QuitGame()
    {
        Debug.Log("<color=red>[System]</color> 正在關閉遊戲...");

        // 1. 如果係喺 Unity Editor 運行
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 2. 如果係 Build 出嚟嘅真正遊戲程式
            Application.Quit();
#endif
    }

    IEnumerator DownloadStudentList()
    {
        feedbackText.text = "正在連接學校系統...";
        UnityWebRequest www = UnityWebRequest.Get(studentCsvUrl);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            ParseCSV(www.downloadHandler.text);
            // 🔥 更新：顯示成功載入的帳號數量
            feedbackText.text = $"系統連接成功！請登入。";
            Debug.Log($"[LoginManager] 成功從 CSV 讀取 {validStudents.Count} 個帳號。");
        }
        else
        {
            feedbackText.text = "網絡錯誤，無法獲取學生名單。";
            Debug.LogError("CSV Error: " + www.error);
        }
    }

    void ParseCSV(string csvData)
    {
        validStudents.Clear();
        // 兼容不同平台的換行符 (CRLF 或 LF)
        string[] rows = csvData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 由 i=1 開始讀取，跳過 Header 標題行
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            if (columns.Length >= 2)
            {
                string user = columns[0].Trim();
                string pass = columns[1].Trim();
                if (!string.IsNullOrEmpty(user))
                {
                    validStudents[user] = pass;
                }
            }
        }
    }

    public void Login()
    {
        string user = usernameInput.text.Trim();
        string pass = passwordInput.text.Trim();

        // 1. 老師獨立通道
        if (user.ToLower() == "teacher")
        {
            Debug.Log("Teacher Mode: Redirecting to Teacher Portal...");

            // 🔥 將下面呢條換成你頭先攞到嗰條網址
            Application.OpenURL("https://rachelqwq419.github.io/FYPWEB/teacher_portal.html");

            feedbackText.text = "歡迎老師，正在打開管理後台...";
            return;
        }

        // 2. 基本檢查
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            feedbackText.text = "請輸入帳號及密碼！";
            return;
        }

        // 3. 雲端密碼驗證
        if (validStudents.ContainsKey(user) && validStudents[user] == pass)
        {
            feedbackText.text = "登入成功！載入中...";

            // 重要：將當前使用者儲存，供 DataUploader 紀錄錯題使用
            PlayerPrefs.SetString("CurrentUser", user);
            PlayerPrefs.Save();

            // 4. 處理本地存檔 JSON
            LoadLocalProgress(user);
        }
        else
        {
            feedbackText.text = "登入失敗：帳號或密碼錯誤。";
        }
    }

    private void LoadLocalProgress(string username)
    {
        // 定義存檔完整路徑
        string fileName = username + "_save.json";
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // 🔥 新增：喺 Console 顯示藍色字路徑，你直接 Click 就會見到檔案
        Debug.Log($"<color=cyan>[SaveSystem]</color> 帳號 {username} 的存檔路徑: {path}");

        if (File.Exists(path))
        {
            Debug.Log($"[SaveSystem] 搵到 {username} 的舊存檔，準備讀取進度...");
            // 這裡可以加入讀取 JSON 並還原遊戲狀態的代碼
        }
        else
        {
            Debug.Log($"<color=yellow>[SaveSystem]</color> 新學生首次登入，正在為 {username} 建立新 JSON 檔...");
            CreateInitialSaveFile(username, path);
        }

        // 跳轉到主地圖
        UnityEngine.SceneManagement.SceneManager.LoadScene("Map_Start");
    }

    // 真正執行寫入檔案的指令
    private void CreateInitialSaveFile(string username, string path)
    {
        // 建立初始資料結構
        UserData initialData = new UserData
        {
            studentName = username,
            lastLoginTime = System.DateTime.Now.ToString(),
            levelProgress = 1
        };

        // 轉換為 JSON 格式
        string json = JsonUtility.ToJson(initialData, true);

        try
        {
            // 寫入硬碟
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            Debug.Log("<color=green>[SaveSystem] 存檔建立成功！</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 存檔寫入失敗: " + e.Message);
        }
    }
}

// 存檔數據模板
[System.Serializable]
public class UserData
{
    public string studentName;
    public string lastLoginTime;
    public int levelProgress;
}