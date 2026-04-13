using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text; // 🔥 必須加呢行嚟處理中文字

public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string qText;
        public string option1;
        public string option2;
        public string option3;
        public int correctIdx;
    }

    public List<Question> chineseBank = new List<Question>();
    public List<Question> englishBank = new List<Question>();
    public List<Question> mathBank = new List<Question>();

    /// <summary>本場戰鬥已出過嘅數學題（題庫 key）；用盡後先啟用程序化生成。</summary>
    readonly HashSet<string> _mathBankKeysUsedThisBattle = new HashSet<string>();

    public static QuestionManager instance;

    [Header("雲端題庫連結 (Google Sheet CSV)")]
    public string chineseCSVUrl = "";
    public string englishCSVUrl = "";
    public string mathCSVUrl = "";

    void Awake()
    {
        instance = this;
        StartCoroutine(InitQuestionBanks());
    }

    IEnumerator InitQuestionBanks()
    {
        yield return StartCoroutine(DownloadOrLoadCSV(chineseCSVUrl, "QuestionData_Chinese", chineseBank));
        yield return StartCoroutine(DownloadOrLoadCSV(englishCSVUrl, "QuestionData_English", englishBank));
        yield return StartCoroutine(DownloadOrLoadCSV(mathCSVUrl, "QuestionData_Math", mathBank));
        Debug.Log("所有題庫更新程序完成！");
    }

    IEnumerator DownloadOrLoadCSV(string url, string localFileName, List<Question> targetList)
    {
        bool downloadSuccess = false;

        if (!string.IsNullOrEmpty(url))
        {
            // 🔥 破解快取大法：喺 URL 後面加個隨機數，等 Google 每次都俾最新資料你
            string cacheBuster = url + "&t=" + Random.Range(0, 1000000);

            using (UnityWebRequest www = UnityWebRequest.Get(cacheBuster))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // 🔥 強制使用 UTF-8 解碼，防止中文字變亂碼
                    string decodedText = Encoding.UTF8.GetString(www.downloadHandler.data);

                    if (!string.IsNullOrEmpty(decodedText) && decodedText.Contains(","))
                    {
                        ParseCSV(decodedText, targetList);
                        if (targetList.Count > 0)
                        {
                            Debug.Log($"成功從雲端更新 {localFileName}！現有 {targetList.Count} 題");
                            downloadSuccess = true;
                        }
                    }
                }
            }
        }

        if (!downloadSuccess)
        {
            TextAsset localData = Resources.Load<TextAsset>(localFileName);
            if (localData != null)
            {
                Debug.Log($"雲端失敗，使用本地備份: {localFileName}");
                ParseCSV(localData.text, targetList);
            }
        }
    }

    void ParseCSV(string csvText, List<Question> targetList)
    {
        targetList.Clear();
        // 🔥 兼容不同平台的換行符 (CRLF 或 LF)
        string[] rows = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (rows.Length <= 1) return;

        // 讀 Header，用欄名做 mapping
        string[] headerCols = rows[0].Trim().Split(',');
        int levelCol = FindHeaderIndex(headerCols, "Level", "level", "等級", "年級");

        int qCol = FindHeaderIndex(headerCols, "Question", "question", "題目", "題干", "qText");
        // 🔥 已加入 option1, option2, option3 兼容你的最新 Sheet
        int aCol = FindHeaderIndex(headerCols, "OptionA", "optionA", "A", "選項A", "option1");
        int bCol = FindHeaderIndex(headerCols, "OptionB", "optionB", "B", "選項B", "option2");
        int cCol = FindHeaderIndex(headerCols, "OptionC", "optionC", "C", "選項C", "option3");
        int correctCol = FindHeaderIndex(headerCols, "Correct", "correct", "Answer", "answer", "正確答案", "correctIdx");

        // 舊格式 fallback
        if (qCol < 0) qCol = 1;
        if (aCol < 0) aCol = 2;
        if (bCol < 0) bCol = 3;
        if (cCol < 0) cCol = 4;
        if (correctCol < 0) correctCol = 5;

        for (int i = 1; i < rows.Length; i++)
        {
            string line = rows[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            // Level 篩選
            if (levelCol >= 0 && cols.Length > levelCol)
            {
                if (int.TryParse(cols[levelCol].Trim(), out int rowLevel))
                {
                    if (rowLevel != GameData.chosenLevel) continue;
                }
            }

            int maxIdxNeeded = Mathf.Max(qCol, aCol, bCol, cCol, correctCol);
            if (cols.Length > maxIdxNeeded)
            {
                Question q = new Question();
                // 🔥 修復位：全面加強版 .Trim()，清走 Excel 啲隱形空格，保證 Cat = cat！
                q.qText = cols[qCol].Trim();
                q.option1 = cols[aCol].Trim();
                q.option2 = cols[bCol].Trim();
                q.option3 = cols[cCol].Trim();
                int.TryParse(cols[correctCol].Trim(), out q.correctIdx);
                targetList.Add(q);
            }
        }
    }

    public Question GetRandomQuestion(string subject)
    {
        if (subject == "Math")
            return GetMathQuestionBankFirstThenProcedural();

        return GetRandomQuestionFromList(subject);
    }

    public void ResetMathQuestionUsageForBattle()
    {
        _mathBankKeysUsedThisBattle.Clear();
    }

    static string MathQuestionKey(Question q)
    {
        if (q == null) return "";
        string o3 = q.option3 ?? "";
        return $"{q.qText}|{q.option1}|{q.option2}|{o3}|{q.correctIdx}";
    }

    Question GetMathQuestionBankFirstThenProcedural()
    {
        if (mathBank != null && mathBank.Count > 0)
        {
            List<Question> unused = new List<Question>();
            for (int i = 0; i < mathBank.Count; i++)
            {
                Question q = mathBank[i];
                if (q == null) continue;
                string key = MathQuestionKey(q);
                if (string.IsNullOrEmpty(key)) continue;
                if (!_mathBankKeysUsedThisBattle.Contains(key))
                    unused.Add(q);
            }

            if (unused.Count > 0)
            {
                Question pick = unused[Random.Range(0, unused.Count)];
                _mathBankKeysUsedThisBattle.Add(MathQuestionKey(pick));
                return pick;
            }
        }

        return GenerateProceduralMathQuestion();
    }

    Question GenerateProceduralMathQuestion()
    {
        int level = Mathf.Clamp(GameData.chosenLevel, 1, 3);
        int a, b;
        Question q = new Question();

        if (level == 1)
        {
            a = Random.Range(1, 10);
            b = Random.Range(1, 10);
            q.qText = $"{a} + {b} = ?";
            q.option1 = (a + b).ToString();
        }
        else if (level == 2)
        {
            a = Random.Range(10, 50);
            b = Random.Range(10, 50);
            q.qText = $"{a} + {b} = ?";
            q.option1 = (a + b).ToString();
        }
        else
        {
            a = Random.Range(2, 9);
            b = Random.Range(2, 9);
            q.qText = $"{a} x {b} = ?";
            q.option1 = (a * b).ToString();
        }

        q.option2 = "[INPUT]";
        q.option3 = "";
        q.correctIdx = 1;
        return q;
    }

    Question GetRandomQuestionFromList(string subject)
    {
        List<Question> targetList = null;
        if (subject == "Chinese") targetList = chineseBank;
        else if (subject == "English") targetList = englishBank;

        if (targetList != null && targetList.Count > 0)
            return targetList[Random.Range(0, targetList.Count)];
        return null;
    }

    int FindHeaderIndex(string[] headerCols, params string[] candidates)
    {
        if (headerCols == null) return -1;

        for (int i = 0; i < headerCols.Length; i++)
        {
            string h = (headerCols[i] ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(h)) continue;

            foreach (string c in candidates)
            {
                if (string.Equals(h, c, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }
}