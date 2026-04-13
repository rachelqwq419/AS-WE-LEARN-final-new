using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking; // 🔥 NEW: 必須加呢行嚟下載圖片
using System.Collections;       // 🔥 NEW: 必須加呢行嚟用 Coroutine

public class BattleUIManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject mainMenu;
    public GameObject skillMenu;
    public GameObject answerMenu;
    public GameObject questionPanel;

    [Header("文字顯示")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI btnTextA;
    public TextMeshProUGUI btnTextB;
    public TextMeshProUGUI btnTextC;

    [Header("按鈕群組")]
    public CanvasGroup actionPanelGroup;

    [Header("選項按鈕物件 (用來隱藏)")]
    public GameObject btnObjectA;
    public GameObject btnObjectB;
    public GameObject btnObjectC;

    [Header("技能選單學科掣（Area Lock；留空則自動喺 SkillMenu 下搵 Btn_Chinese / Btn_English / Btn_Math）")]
    public GameObject btnSubjectChinese;
    public GameObject btnSubjectEnglish;
    public GameObject btnSubjectMath;

    [Header("填充題專用")]
    public GameObject inputPanel;
    public TMP_InputField answerInputField;
    private string currentTextInputAnswer = "";

    // 🔥 NEW: 圖片題專用
    [Header("圖片題專用")]
    public RawImage questionImage;

    private int currentCorrectAnswer;
    private int currentAttributeID;

    private string currentQuestionText;
    /// <summary>畀玩家睇到嘅題目字（已剷走 [IMG] 等），Summary 用。</summary>
    private string currentQuestionDisplayText;
    private string currentOptionAText;
    private string currentOptionBText;
    private string currentOptionCText;

    void Start()
    {
        ResolveSkillSubjectButtons();
        ShowPanel("Main");

        if (actionPanelGroup != null)
        {
            Button[] allButtons = actionPanelGroup.GetComponentsInChildren<Button>(true);

            foreach (Button btn in allButtons)
            {
                btn.onClick.AddListener(() => {
                    if (BattleController.instance != null)
                    {
                        AudioSource source = BattleController.instance.sfxSource;
                        AudioClip clickSound = BattleController.instance.sfxClick;

                        if (source != null && clickSound != null)
                        {
                            source.PlayOneShot(clickSound);
                        }
                    }
                });
            }
        }
    }

    public void SetInteractable(bool canTouch)
    {
        if (actionPanelGroup != null)
        {
            actionPanelGroup.interactable = canTouch;
            actionPanelGroup.alpha = canTouch ? 1f : 0.5f;
        }
    }

    public void OnClick_Iaido() { ShowPanel("Skill"); }
    public void OnClick_Back() { ShowPanel("Main"); }

    public void OnClick_Run()
    {
        if (BattleController.instance != null) BattleController.instance.PlayerRun();
    }

    public void OnClick_CastSpell(string subject)
    {
        if (subject == "Chinese") currentAttributeID = 0;
        else if (subject == "English") currentAttributeID = 1;
        else if (subject == "Math") currentAttributeID = 2;

        var q = QuestionManager.instance.GetRandomQuestion(subject);
        if (q == null) return;

        // 🔥 NEW: 每次出題先隱藏圖片
        if (questionImage != null) questionImage.gameObject.SetActive(false);

        // 🔥 NEW: 處理圖片標籤邏輯
        string finalQuestionText = q.qText;
        if (finalQuestionText.Contains("[IMG]") && finalQuestionText.Contains("[/IMG]"))
        {
            int startIdx = finalQuestionText.IndexOf("[IMG]") + 5;
            int endIdx = finalQuestionText.IndexOf("[/IMG]");
            string url = finalQuestionText.Substring(startIdx, endIdx - startIdx);

            // 剷走標籤，淨低真正嘅文字
            finalQuestionText = finalQuestionText.Replace($"[IMG]{url}[/IMG]", "").Trim();

            // 啟動下載圖片
            StartCoroutine(DownloadImageRoutine(url));
        }

        // 顯示最終文字 (冇咗 Link 嘅乾淨題目)
        questionText.text = finalQuestionText;
        currentQuestionDisplayText = finalQuestionText;

        // 記錄答案同埋題目文字
        currentCorrectAnswer = q.correctIdx;
        currentQuestionText = q.qText;
        currentOptionAText = q.option1;
        currentOptionBText = q.option2;
        currentOptionCText = q.option3;

        // 還原所有掣，並收埋 InputPanel
        if (btnObjectA != null) btnObjectA.SetActive(true);
        if (btnObjectB != null) btnObjectB.SetActive(true);
        if (btnObjectC != null) btnObjectC.SetActive(true);
        if (inputPanel != null) inputPanel.SetActive(false);

        // 判斷題型
        if (q.option2 == "[INPUT]")
        {
            if (btnObjectA != null) btnObjectA.SetActive(false);
            if (btnObjectB != null) btnObjectB.SetActive(false);
            if (btnObjectC != null) btnObjectC.SetActive(false);

            if (inputPanel != null)
            {
                inputPanel.SetActive(true);
                if (answerInputField != null) answerInputField.text = "";
            }
            currentTextInputAnswer = q.option1;
        }
        else if (string.IsNullOrEmpty(q.option3))
        {
            btnTextA.text = q.option1;
            btnTextB.text = q.option2;
            if (btnObjectC != null) btnObjectC.SetActive(false);
        }
        else
        {
            btnTextA.text = q.option1;
            btnTextB.text = q.option2;
            btnTextC.text = q.option3;
        }

        ShowPanel("Answer");
    }

    string GetCorrectAnswerTextForSummary()
    {
        if (currentOptionBText == "[INPUT]")
            return currentTextInputAnswer ?? "";

        string PickMcqText()
        {
            if (string.IsNullOrEmpty(currentOptionCText))
            {
                if (currentCorrectAnswer == 1) return currentOptionAText ?? "";
                if (currentCorrectAnswer == 2) return currentOptionBText ?? "";
                return "";
            }

            if (currentCorrectAnswer == 1) return currentOptionAText ?? "";
            if (currentCorrectAnswer == 2) return currentOptionBText ?? "";
            if (currentCorrectAnswer == 3) return currentOptionCText ?? "";
            return "";
        }

        string ans = PickMcqText();
        // 好多題庫會將「正確選項」字串整到同題干一樣（尤其圖片題）；Summary 改顯示選項字母唔會變兩格相同字
        string qStrip = StripImgTagsForCompare(currentQuestionDisplayText);
        string aStrip = StripImgTagsForCompare(ans);
        if (!string.IsNullOrEmpty(aStrip) && string.Equals(qStrip, aStrip, StringComparison.Ordinal))
        {
            int idx = Mathf.Clamp(currentCorrectAnswer, 1, 3);
            char letter = (char)('A' + idx - 1);
            return $"（正解：選項 {letter}）";
        }

        return ans;
    }

    static string StripImgTagsForCompare(string s)
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

    // 🔥 NEW: 下載圖片嘅 Coroutine
    IEnumerator DownloadImageRoutine(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                if (questionImage != null)
                {
                    questionImage.texture = texture;
                    questionImage.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("圖片下載失敗: " + www.error);
            }
        }
    }

    public void OnClick_Answer(int optionIndex)
    {
        ShowPanel("Main");
        bool isCorrect = (optionIndex == currentCorrectAnswer);

        if (!isCorrect && DataUploader.Instance != null)
        {
            string wrongAnswerText = "";
            if (optionIndex == 1) wrongAnswerText = currentOptionAText;
            else if (optionIndex == 2) wrongAnswerText = currentOptionBText;
            else if (optionIndex == 3) wrongAnswerText = currentOptionCText;

            DataUploader.Instance.RecordWrongAnswer(currentQuestionDisplayText, wrongAnswerText);
            Summary.RecordWrong(
                GameData.chosenLevel,
                currentQuestionDisplayText,
                GetCorrectAnswerTextForSummary(),
                Summary.AreaLabelFromSubject(GameData.chosenSubject));
        }

        if (BattleController.instance != null)
        {
            BattleController.instance.PlayerAttack_Magic(isCorrect, currentAttributeID);
        }
    }

    public void OnClick_SubmitInput()
    {
        ShowPanel("Main");

        string userAnswer = "";
        if (answerInputField != null) userAnswer = answerInputField.text.Trim();

        bool isCorrect = string.Equals(userAnswer, currentTextInputAnswer, System.StringComparison.OrdinalIgnoreCase);

        if (!isCorrect && DataUploader.Instance != null)
        {
            DataUploader.Instance.RecordWrongAnswer(currentQuestionDisplayText, "玩家輸入: " + userAnswer);
            Summary.RecordWrong(
                GameData.chosenLevel,
                currentQuestionDisplayText,
                GetCorrectAnswerTextForSummary(),
                Summary.AreaLabelFromSubject(GameData.chosenSubject));
        }

        if (inputPanel != null) inputPanel.SetActive(false);

        if (BattleController.instance != null)
        {
            BattleController.instance.PlayerAttack_Magic(isCorrect, currentAttributeID);
        }
    }

    void ResolveSkillSubjectButtons()
    {
        if (skillMenu == null) return;
        foreach (Transform t in skillMenu.GetComponentsInChildren<Transform>(true))
        {
            if (btnSubjectChinese == null && t.name == "Btn_Chinese") btnSubjectChinese = t.gameObject;
            else if (btnSubjectEnglish == null && t.name == "Btn_English") btnSubjectEnglish = t.gameObject;
            else if (btnSubjectMath == null && t.name == "Btn_Math") btnSubjectMath = t.gameObject;
        }
    }

    void ApplyMonsterSubjectAreaLock()
    {
        if (BattleController.instance == null) return;

        EnemyData enemy = BattleController.instance.currentEnemy;
        if (enemy != null)
        {
            int monsterAttr = enemy.attribute;
            if (btnSubjectChinese != null) btnSubjectChinese.SetActive(monsterAttr == 0);
            if (btnSubjectEnglish != null) btnSubjectEnglish.SetActive(monsterAttr == 1);
            if (btnSubjectMath != null) btnSubjectMath.SetActive(monsterAttr == 2);
        }
        else
        {
            if (btnSubjectChinese != null) btnSubjectChinese.SetActive(true);
            if (btnSubjectEnglish != null) btnSubjectEnglish.SetActive(true);
            if (btnSubjectMath != null) btnSubjectMath.SetActive(true);
        }
    }

    void ShowPanel(string panelName)
    {
        mainMenu.SetActive(panelName == "Main");
        skillMenu.SetActive(panelName == "Skill");

        // 學科掣喺 SkillMenu；開主選單或技能選單時都要更新，開技能嗰陣先會見到
        if (panelName == "Main" || panelName == "Skill")
            ApplyMonsterSubjectAreaLock();

        answerMenu.SetActive(panelName == "Answer");
        questionPanel.SetActive(panelName == "Answer");

        if (panelName != "Answer" && inputPanel != null) inputPanel.SetActive(false);
    }
    // === 👆 點擊頭像換人 ===
    public void OnClick_SwitchCharacter(int teamIndex)
    {
        // 確保戰鬥系統存在，同埋只有喺「玩家回合」先准換人！
        if (BattleController.instance != null && BattleController.instance.state == BattleState.PLAYER_TURN)
        {
            BattleController.instance.SwitchCharacter(teamIndex);
        }
    }

}