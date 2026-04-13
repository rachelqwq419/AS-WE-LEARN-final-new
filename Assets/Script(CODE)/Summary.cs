using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 錯題摘要（簡化版）：顯示 Level / Question / Correct answer / Area。
/// BattleUIManager 會呼叫 <see cref="RecordWrong"/>。
/// </summary>
public class Summary : MonoBehaviour
{
    public static Summary Instance { get; private set; }

    [Serializable]
    public class WrongAnswerEntry
    {
        public int level;
        public string question;
        public string correctAnswer;
        public string area;
    }

    [Serializable]
    class WrongAnswerListFile
    {
        public List<WrongAnswerEntry> items = new List<WrongAnswerEntry>();
    }

    [Header("必填")]
    [Tooltip("成日 Active 嘅物件（例如 SummaryRoot），上面掛呢個 Summary")]
    public GameObject summaryPanelRoot;

    [Tooltip("Scroll View → Viewport → Content（錯題行會生成喺呢度）")]
    public RectTransform contentRoot;

    [Tooltip("一行嘅模板：下面要有四個 TMP，GameObject 名稱：level、question、correctAnswer、area")]
    public GameObject rowTemplate;

    [Header("可選")]
    public bool startHidden = true;
    public bool persistToDisk = true;
    public float rowHeight = 88f;

    [Tooltip("為 Content 加 VerticalLayoutGroup + ContentSizeFitter")]
    public bool setupContentLayout = true;

    [Tooltip("若模板入面有「xxx title」表頭，複製出嚟嘅每行會收埋，避免重複")]
    public bool hideTitleLabelsInClonedRows = true;

    static readonly List<WrongAnswerEntry> SessionEntries = new List<WrongAnswerEntry>();
    const string FileName = "wrong_answer_summary.json";

    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (persistToDisk) LoadFromDisk();
        RefreshUI();
    }

    void Start()
    {
        if (startHidden && summaryPanelRoot != null)
            summaryPanelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 🔥 NEW: 專門俾 QuitGameManager 呼叫嘅分析函數
    // 專門俾 QuitGameManager 呼叫嘅分析函數
    public (string weakest, int count) GetWeakestAreaInfo()
    {
        if (SessionEntries.Count == 0) return ("None", 0);

        int chinese = 0, english = 0, math = 0;
        foreach (var e in SessionEntries)
        {
            // 呢度係檢查資料庫入面嘅標籤，通常已經係英文
            if (e.area.Contains("Chinese")) chinese++;
            else if (e.area.Contains("English")) english++;
            else if (e.area.Contains("Math")) math++;
        }

        // 🔥 將下面呢幾行嘅中文字改做英文！
        string weakest = "Chinese"; // 原本係 "中文"
        int max = chinese;

        if (english > max) { weakest = "English"; max = english; } // 原本係 "英文"
        if (math > max) { weakest = "Math"; max = math; } // 原本係 "數學"

        return (weakest, max);
    }

    public void OpenSummaryPanel()
    {
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(true);
        RefreshUI();
    }

    public static void OpenSummaryPanelStatic()
    {
        if (Instance != null) Instance.OpenSummaryPanel();
    }

    public void CloseSummaryPanel()
    {
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
    }

    public static void CloseSummaryPanelStatic()
    {
        if (Instance != null) Instance.CloseSummaryPanel();
    }

    public static void RecordWrong(int level, string question, string correctAnswer, string area)
    {
        if (string.IsNullOrEmpty(question)) question = "(empty)";

        SessionEntries.Add(new WrongAnswerEntry
        {
            level = level,
            question = question,
            correctAnswer = correctAnswer ?? "",
            area = area ?? ""
        });

        bool save = Instance == null || Instance.persistToDisk;
        if (save) SaveToDisk();

        if (Instance != null) Instance.RefreshUI();
    }

    public static string AreaLabelFromSubject(string subject)
    {
        if (string.IsNullOrEmpty(subject)) return "-";
        switch (subject)
        {
            case "Chinese": return "Chinese Area";
            case "English": return "English Area";
            case "Math": return "Math Area";
            default: return subject;
        }
    }

    public void ClearAll()
    {
        SessionEntries.Clear();
        if (persistToDisk && File.Exists(SavePath)) File.Delete(SavePath);
        RefreshUI();
    }

    void LoadFromDisk()
    {
        SessionEntries.Clear();
        if (!File.Exists(SavePath)) return;
        try
        {
            var data = JsonUtility.FromJson<WrongAnswerListFile>(File.ReadAllText(SavePath, System.Text.Encoding.UTF8));
            if (data?.items != null) SessionEntries.AddRange(data.items);
        }
        catch (Exception e) { Debug.LogWarning("[Summary] Load: " + e.Message); }
    }

    static void SaveToDisk()
    {
        try
        {
            var json = JsonUtility.ToJson(new WrongAnswerListFile { items = new List<WrongAnswerEntry>(SessionEntries) }, true);
            File.WriteAllText(SavePath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception e) { Debug.LogWarning("[Summary] Save: " + e.Message); }
    }
    void Update()
    {
        // 喺 Summary 畫面撳 Backspace (退格鍵) 就一嘢清空晒所有舊記錄！
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ClearAll();
            Debug.Log("Summary 已完全清空！");
        }
    }

    public void RefreshUI()
    {
        if (rowTemplate == null || contentRoot == null)
        {
            Debug.LogWarning("[Summary] 請喺 Inspector 設定 Content Root 同 Row Template。");
            return;
        }

        if (setupContentLayout)
            SetupVerticalList(contentRoot);

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = contentRoot.GetChild(i);
            if (c.gameObject == rowTemplate) continue;
            if (c.name.StartsWith("SummaryRow_")) Destroy(c.gameObject);
        }

        rowTemplate.SetActive(false);

        // 🔥 倒轉行，最新記錄喺面
        for (int i = SessionEntries.Count - 1; i >= 0; i--)
        {
            GameObject row = Instantiate(rowTemplate, contentRoot);
            row.name = "SummaryRow_" + i;
            row.SetActive(true);

            if (hideTitleLabelsInClonedRows)
                HideTitles(row.transform);

            ApplyRowSize(row);
            FillRow(row.transform, SessionEntries[i]);
        }
    }

    static void SetupVerticalList(RectTransform content)
    {
        var v = content.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = 8f;
        v.padding = new RectOffset(12, 12, 12, 12);
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var f = content.GetComponent<ContentSizeFitter>();
        if (f == null) f = content.gameObject.AddComponent<ContentSizeFitter>();
        f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
    }

    void ApplyRowSize(GameObject row)
    {
        var rt = row.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        var le = row.GetComponent<LayoutElement>();
        if (le == null) le = row.AddComponent<LayoutElement>();
        le.preferredHeight = Mathf.Max(40f, rowHeight);
        le.minHeight = 40f;
        le.flexibleHeight = 0f;
    }

    static void HideTitles(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            if (t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                t.gameObject.SetActive(false);
        }
        var areas = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == "area") areas.Add(t);
        if (areas.Count >= 2) areas[0].gameObject.SetActive(false);
    }

    static void FillRow(Transform row, WrongAnswerEntry e)
    {
        SetTmp(row, "level", e.level.ToString());
        SetTmp(row, "question", StripImg(e.question));
        SetTmp(row, new[] { "correctAnswer", "correct answer", "coreect answer", "coreect" }, StripImg(e.correctAnswer));
        SetTmpLastNamed(row, "area", e.area);
    }

    static void SetTmp(Transform root, string childName, string text)
    {
        var t = FindChildDeep(root, childName);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = text ?? "";
    }

    static void SetTmp(Transform root, string[] names, string text)
    {
        foreach (var n in names)
        {
            var t = FindChildDeep(root, n);
            if (t != null)
            {
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null) { tmp.text = text ?? ""; return; }
            }
        }
    }

    static void SetTmpLastNamed(Transform root, string name, string text)
    {
        TMP_Text last = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != name) continue;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) last = tmp;
        }
        if (last != null) last.text = text ?? "";
    }

    static Transform FindChildDeep(Transform p, string name)
    {
        if (p.name == name) return p;
        for (int i = 0; i < p.childCount; i++)
        {
            var r = FindChildDeep(p.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    static string StripImg(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        while (true)
        {
            int a = s.IndexOf("[IMG]", StringComparison.Ordinal);
            int b = s.IndexOf("[/IMG]", StringComparison.Ordinal);
            if (a < 0 || b < 0 || b < a) break;
            s = s.Remove(a, b - a + 7);
        }
        return s.Trim();
    }
}