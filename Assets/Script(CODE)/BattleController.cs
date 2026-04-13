using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum BattleState { START, PLAYER_TURN, ENEMY_TURN, WON, LOST }

public class BattleController : MonoBehaviour
{
    public static BattleController instance;

    [Header("狀態監控")]
    public BattleState state;
    public int turnCount = 0;

    [Header("角色")]
    public Animator playerAnimator;

    [Header("當前角色")]
    public Character currentPlayer;

    [Header("特效 (VFX)")]
    public GameObject vfxSwordSlash;
    public GameObject vfxMagicExplosion;
    public GameObject vfxEnemyHit;

    // 🔥 NEW: 鏡頭震動 (請把 Main Camera 拖進來)
    public CameraShake cameraShake;

    [Header("音效來源")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("音效素材")]
    public AudioClip bgmBattle;
    public AudioClip sfxSword;
    public AudioClip sfxMagic;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;
    public AudioClip sfxHurt;       // 玩家受傷
    public AudioClip sfxEnemyHurt;  // 怪物受傷
    public AudioClip sfxWin;
    public AudioClip sfxLose;
    public AudioClip sfxClick;

    [Header("UI 綁定")]
    public TextMeshProUGUI txtTurnIndicator;
    public TextMeshProUGUI txtTurnCount;
    public TextMeshProUGUI txtCombatLog;
    public BattleUIManager uiManager;

    [Header("儀表板")]
    public TextMeshProUGUI txtPlayerName;
    public TextMeshProUGUI txtPlayerHP;
    public Slider sliderPlayerHP;
    public TextMeshProUGUI txtEnemyName;
    public TextMeshProUGUI txtEnemyHP;
    public Slider sliderEnemyHP;

    [Header("數值設定")]
    private int playerCurrentHP;
    /// <summary>0=中文 1=英文 2=數學；UI Area Lock 用。</summary>
    public EnemyData currentEnemy;

    [Header("學習結算（Session-based）")]
    public int correctCount = 0;
    public int wrongCount = 0;

    [Header("結算 UI（可選：冇綁就自動生成）")]
    public GameObject endReportPanel;
    public TextMeshProUGUI endReportText;

    void Awake() { instance = this; }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        state = BattleState.START;
        turnCount = 0;
        correctCount = 0;
        wrongCount = 0;
        txtPlayerName.text = "準備中...";
        UpdateCombatLog("");

        // 播放 BGM
        if (musicSource != null && bgmBattle != null)
        {
            musicSource.clip = bgmBattle;
            musicSource.Play();
        }

        // 自動抓取震動腳本 (如果忘記拖的話)
        if (cameraShake == null) cameraShake = FindObjectOfType<CameraShake>();
        if (uiManager == null) uiManager = FindObjectOfType<BattleUIManager>();

        EnsureEndReportUI();
        if (endReportPanel != null) endReportPanel.SetActive(false);

        Invoke("SetupBattle", 0.1f);
    }

    void SetupBattle()
    {
        // 1. 自動從 Resources/Characters 載入角色 (如果隊伍空)
        if (TeamManager.Instance != null && TeamManager.Instance.playerTeamCharacters.Count == 0)
        {
            Debug.Log("隊伍空咗！自動從 Resources/Characters 載入角色...");
            GameObject p0 = Resources.Load<GameObject>("Characters/Bella");
            GameObject p1 = Resources.Load<GameObject>("Characters/Kael");
            GameObject p2 = Resources.Load<GameObject>("Characters/Mia");

            if (p0 != null) TeamManager.Instance.AddToTeam(p0);
            if (p1 != null) TeamManager.Instance.AddToTeam(p1);
            if (p2 != null) TeamManager.Instance.AddToTeam(p2);
        }

        if (TeamManager.Instance == null || TeamManager.Instance.playerTeamCharacters.Count == 0)
        {
            Debug.LogError("Resources 找不到角色！");
            return;
        }

        // 🔥 NEW: 強制為隊伍重新排隊！(解決每次入場順序唔同嘅問題)
        // 強制順序: Kael (0) -> Bella (1) -> Mia (2) -> 其他/武士
        TeamManager.Instance.playerTeamCharacters.Sort((a, b) =>
        {
            int GetOrder(Character c)
            {
                if (c == null || string.IsNullOrEmpty(c.characterName)) return 99;
                string name = c.characterName.ToLower();
                if (name.Contains("kael") || name.Contains("keal")) return 0; // 兼容兩種串法
                if (name.Contains("bella")) return 1;
                if (name.Contains("mia")) return 2;
                return 10; // 武士或其他角色掉去後面
            }
            return GetOrder(a).CompareTo(GetOrder(b));
        });

        // 2. 收埋武士
        GameObject samurai = GameObject.Find("Samurai");
        if (samurai != null) samurai.SetActive(false);

        // 3. 準備怪物資料
        currentEnemy = FindObjectOfType<EnemyData>();
        if (currentEnemy != null)
        {
            txtEnemyName.text = currentEnemy.monsterName;
            sliderEnemyHP.maxValue = currentEnemy.maxHP;
        }

        if (QuestionManager.instance != null)
            QuestionManager.instance.ResetMathQuestionUsageForBattle();

        // 4. 強制將「真身」排好隊！
        // 由於已經排咗序，positions[0] 一定係 Kael, positions[1] 係 Bella, positions[2] 係 Mia
        Vector3[] positions = new Vector3[] {
            new Vector3(-6f, 55f, -1f),
            new Vector3(-8f, 55f, -1f),
            new Vector3(-10f, 55f, -1f)
        };

        for (int i = 0; i < TeamManager.Instance.playerTeamCharacters.Count; i++)
        {
            Character c = TeamManager.Instance.playerTeamCharacters[i];
            if (c != null)
            {
                c.gameObject.SetActive(true); // 確保真身現形

                // 🔥 關鍵修正：每次開新一場戰鬥，自動幫全隊補滿血！
                c.health = c.maxHealth;

                // 根據隊伍順序，安排佢哋企喺指定位置
                if (i < positions.Length)
                {
                    c.transform.position = positions[i];
                }
            }
        }

        // 5. 預設將控制權交畀第 0 隻角色 (即係 Kael)
        SwitchCharacter(0);

        UpdateUI();
        StartCoroutine(BattleStartFlow());
    }

    IEnumerator BattleStartFlow()
    {
        uiManager.SetInteractable(false);
        ShowTurnText("遭遇敵人！");
        UpdateTurnCountUI();
        yield return new WaitForSeconds(2f);
        state = BattleState.PLAYER_TURN;
        PlayerTurn();
    }

    void PlayerTurn()
    {
        turnCount++;
        UpdateTurnCountUI();
        ShowTurnText("你的回合");
        UpdateCombatLog("");
        uiManager.SetInteractable(true);
    }

    // === 玩家攻擊 ===
    public void PlayerAttack_Normal()
    {
        if (state != BattleState.PLAYER_TURN) return;
        if (currentPlayer == null || currentEnemy == null)
        {
            Debug.LogWarning("缺少 currentPlayer 或 currentEnemy");
            return;
        }

        // 教育向：物理攻擊接近無效，迫玩家答題
        float damageMultiplier = 0.05f;
        int totalDamage = Mathf.RoundToInt(currentPlayer.characterData.baseAttackPower * damageMultiplier);

        string msg = $"物理攻擊太弱了！<color=red>對知識怪獸無效！</color>造成 {totalDamage} 點傷害。";
        StartCoroutine(PlayerAttackSequence("Attack", totalDamage, msg, vfxSwordSlash, sfxSword, 0.1f));
    }

    public void PlayerAttack_Magic(bool isCorrect, int attackAttributeID)
    {
        if (state != BattleState.PLAYER_TURN) return;
        if (currentPlayer == null || currentEnemy == null) return;

        // 第四招：記錄正誤數（學習反饋）
        if (isCorrect) correctCount++;
        else wrongCount++;

        float baseMP = currentPlayer.characterData.baseMaxMana;
        float totalMP = currentPlayer.GetTotalStat(StatsType.MP);
        float baseAD = currentPlayer.characterData.baseAttackPower;
        float totalAD = currentPlayer.GetTotalStat(StatsType.AD);
        int baseSpellDamage = 150;
        int mpBonus = Mathf.RoundToInt(totalMP - baseMP);
        int adBonusFromGear = Mathf.Max(0, Mathf.RoundToInt(totalAD - baseAD));
        int totalDamage = baseSpellDamage + mpBonus + adBonusFromGear;

        string resultPrefix = "";
        string damageColor = "cyan";
        float shakePower = 0.1f;

        if (isCorrect)
        {
            sfxSource.PlayOneShot(sfxCorrect);
            if (currentEnemy.attribute == attackAttributeID)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 1.5f);
                resultPrefix = "<color=red>答對了！</color>";
                damageColor = "yellow";
                shakePower = 0.4f;
            }
            else
            {
                resultPrefix = "<color=green>答對了！</color>";
                damageColor = "white";
                shakePower = 0.2f;
            }
        }
        else
        {
            sfxSource.PlayOneShot(sfxWrong);
            if (currentEnemy.attribute == attackAttributeID)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 0.3f);
                resultPrefix = "<color=grey>答錯了...</color>";
                damageColor = "grey";
            }
            else
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 0.1f);
                resultPrefix = "<color=grey>答錯了...</color>";
                damageColor = "grey";
            }
        }

        // 學科專精：對應科目 +20%（鼓勵換人）
        float specializationMultiplier = GetSubjectSpecializationMultiplier(attackAttributeID);
        if (specializationMultiplier > 1f)
            totalDamage = Mathf.RoundToInt(totalDamage * specializationMultiplier);

        bool isCrit = Random.value <= currentPlayer.GetTotalStat(StatsType.CR);
        if (isCrit)
        {
            totalDamage = Mathf.RoundToInt(totalDamage * currentPlayer.GetTotalStat(StatsType.CRD));
            damageColor = "orange";
        }

        string specNote = specializationMultiplier > 1f ? " <color=#00FFFF></color>" : "";
        string msg = $"{resultPrefix}{specNote} 對 {currentEnemy.monsterName} 造成了 <color={damageColor}><b>{totalDamage}</b></color> 點傷害。";
        if (isCrit) msg += " <color=red>暴擊！</color>";

        StartCoroutine(PlayerAttackSequence("Attack", totalDamage, msg, vfxMagicExplosion, sfxMagic, shakePower));

        if (cameraShake != null) cameraShake.Shake(shakePower + 0.1f, 0.15f);
    }

    public void PlayerRun()
    {
        if (state != BattleState.PLAYER_TURN) return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        uiManager.SetInteractable(false);
        txtTurnIndicator.gameObject.SetActive(false);
        UpdateCombatLog("<color=green>逃跑成功！</color>");
        if (sfxSource != null && sfxCorrect != null) sfxSource.PlayOneShot(sfxCorrect);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("Map_MainCity");
    }

    IEnumerator PlayerAttackSequence(string animName, int damage, string customMessage, GameObject vfxPrefab, AudioClip soundClip, float shakePower)
    {
        uiManager.SetInteractable(false);
        txtTurnIndicator.gameObject.SetActive(false);

        if (playerAnimator != null) playerAnimator.SetTrigger(animName);

        yield return new WaitForSeconds(0.5f);

        bool isDead = false;

        if (currentEnemy != null)
        {
            PlayHitVFX(vfxPrefab, currentEnemy.transform.position);

            if (sfxSource != null && soundClip != null) sfxSource.PlayOneShot(soundClip);
            if (sfxSource != null && sfxEnemyHurt != null) sfxSource.PlayOneShot(sfxEnemyHurt);

            if (cameraShake != null && shakePower > 0)
            {
                StartCoroutine(cameraShake.Shake(0.15f, shakePower));
            }

            currentEnemy.TakeDamage(damage);
            UpdateUI();
            UpdateCombatLog(customMessage);

            if (currentEnemy.currentHP <= 0)
            {
                state = BattleState.WON;
                isDead = true;
                currentEnemy.PlayDeathAnim();
                EndBattle();
            }
        }

        if (!isDead)
        {
            yield return new WaitForSeconds(1.5f);
            state = BattleState.ENEMY_TURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator EnemyTurn()
    {
        ShowTurnText("敵方回合");
        yield return new WaitForSeconds(1f);
        txtTurnIndicator.gameObject.SetActive(false);
        if (currentEnemy != null) currentEnemy.PlayAttackAnim();
        yield return new WaitForSeconds(0.5f);

        if (playerAnimator != null)
        {
            PlayHitVFX(vfxEnemyHit, playerAnimator.transform.position);
            if (sfxSource != null && sfxHurt != null) sfxSource.PlayOneShot(sfxHurt);
            if (cameraShake != null) StartCoroutine(cameraShake.Shake(0.2f, 0.3f));

            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == "Hurt") playerAnimator.SetTrigger("Hurt");
                if (param.name == "Hit") playerAnimator.SetTrigger("Hit");
            }
        }

        int enemyDmg = (currentEnemy != null) ? currentEnemy.damage : 50;
        float reductionMultiplier = 100f / (100f + currentPlayer.GetTotalStat(StatsType.DF));
        int finalDmg = Mathf.RoundToInt(enemyDmg * reductionMultiplier);
        finalDmg = Mathf.Max(Mathf.RoundToInt(enemyDmg * 0.1f), finalDmg);

        playerCurrentHP -= finalDmg;
        if (playerCurrentHP < 0) playerCurrentHP = 0;

        UpdateUI();
        UpdateCombatLog($"敵方發動攻擊！你受到了 <color=red>{finalDmg}</color> 點傷害！");

        if (playerCurrentHP <= 0)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
            state = BattleState.PLAYER_TURN;
            PlayerTurn();
        }
    }

    void PlayHitVFX(GameObject vfx, Vector3 targetPos)
    {
        if (vfx == null) return;
        Vector3 spawnPos = new Vector3(targetPos.x, targetPos.y + 0.5f, -1f);
        GameObject effect = Instantiate(vfx, spawnPos, Quaternion.identity);
        Destroy(effect, 2f);
    }

    void EndBattle()
    {
        if (musicSource != null) musicSource.Stop();

        // 🔥 核心修改：戰鬥結束時，強制收埋後面嗰堆阻噏嘅舊文字
        if (txtCombatLog != null) txtCombatLog.gameObject.SetActive(false);
        if (txtTurnIndicator != null) txtTurnIndicator.gameObject.SetActive(false);

        if (state == BattleState.WON)
        {
            GoldManager.instance.AddGold(10);
            if (sfxSource != null && sfxWin != null) sfxSource.PlayOneShot(sfxWin);
        }
        else if (state == BattleState.LOST)
        {
            if (playerAnimator != null)
            {
                foreach (AnimatorControllerParameter param in playerAnimator.parameters)
                {
                    if (param.name == "Die") playerAnimator.SetTrigger("Die");
                    if (param.name == "Death") playerAnimator.SetTrigger("Death");
                }
            }
            if (sfxSource != null && sfxLose != null) sfxSource.PlayOneShot(sfxLose);
        }

        ShowEndReport();
        StartCoroutine(ReturnToMenu());
    }

    IEnumerator ReturnToMenu()
    {
        // 俾玩家睇到結算報告（簡單但足夠 present）
        yield return new WaitForSeconds(5f);

        // 離開前關閉結算 UI，避免被持久 Canvas 帶去下一個場景
        if (endReportPanel != null) endReportPanel.SetActive(false);

        SceneManager.LoadScene("Map_MainCity");
    }

    void UpdateUI()
    {
        if (currentPlayer != null)
        {
            sliderPlayerHP.maxValue = currentPlayer.maxHealth;
            sliderPlayerHP.value = playerCurrentHP;
            txtPlayerHP.text = $"{playerCurrentHP} / {currentPlayer.maxHealth}";
        }
        if (currentEnemy != null)
        {
            sliderEnemyHP.value = currentEnemy.currentHP;
            txtEnemyHP.text = $"{currentEnemy.currentHP} / {currentEnemy.maxHP}";
        }
    }
    void UpdateTurnCountUI() { if (txtTurnCount != null) txtTurnCount.text = $"{turnCount}T"; }
    void ShowTurnText(string text) { if (txtTurnIndicator != null) { txtTurnIndicator.text = text; txtTurnIndicator.gameObject.SetActive(true); } }
    void UpdateCombatLog(string text) { if (txtCombatLog != null) { txtCombatLog.text = text; txtCombatLog.gameObject.SetActive(true); } }

    // === 🔄 點擊頭像切換控制權 ===
    float GetSubjectSpecializationMultiplier(int attackAttributeID)
    {
        if (currentPlayer == null || string.IsNullOrEmpty(currentPlayer.characterName)) return 1f;
        string n = currentPlayer.characterName.ToLowerInvariant();
        if (n.Contains("mia") && attackAttributeID == 0) return 1.2f;
        if (n.Contains("bella") && attackAttributeID == 1) return 1.2f;
        if ((n.Contains("kael") || n.Contains("keal")) && attackAttributeID == 2) return 1.2f;
        return 1f;
    }

    public void SwitchCharacter(int teamIndex)
    {
        if (TeamManager.Instance == null || teamIndex >= TeamManager.Instance.playerTeamCharacters.Count)
        {
            Debug.LogWarning($"隊伍無第 {teamIndex} 個角色！");
            return;
        }

        // 🔥 關鍵修正 1：喺換人之前，將當前場上角色嘅剩餘血量儲存返入佢個 Script
        if (currentPlayer != null)
        {
            currentPlayer.health = playerCurrentHP;
        }

        // 切換到新角色
        for (int i = 0; i < TeamManager.Instance.playerTeamCharacters.Count; i++)
        {
            Character c = TeamManager.Instance.playerTeamCharacters[i];
            if (c != null)
            {
                bool isSelected = (i == teamIndex);
                c.gameObject.SetActive(isSelected);
                if (isSelected) c.transform.position = new Vector3(-6f, 55f, -1f);
            }
        }

        currentPlayer = TeamManager.Instance.playerTeamCharacters[teamIndex];
        playerAnimator = currentPlayer.GetComponentInChildren<Animator>();

        // 🔥 關鍵修正 2：重新計算裝備數值，但唔好呼叫會自動補血嘅 InitializeFromData
        currentPlayer.RecalculateStats();

        // 讀取新角色「本身剩低」嘅血量，而唔係重新補滿
        playerCurrentHP = currentPlayer.health;

        txtPlayerName.text = currentPlayer.characterName;
        UpdateUI();
        UpdateCombatLog($"切換至 <color=yellow>{currentPlayer.characterName}</color>！");
    }

    void EnsureEndReportUI()
    {
        if (endReportPanel != null && endReportText != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (endReportPanel == null)
        {
            // Full-screen overlay
            endReportPanel = new GameObject("EndReportPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            endReportPanel.transform.SetParent(canvas.transform, false);

            var rt = endReportPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = endReportPanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
        }

        if (endReportText == null)
        {
            // Center card (方格物件)
            GameObject cardObj = new GameObject("EndReportCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(endReportPanel.transform, false);

            var crt = cardObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);

            // 🔥 修改位置：將 Y 由 0 改做 100f (數值越大越向上搬)
            crt.anchoredPosition = new Vector2(0f, 100f);
            crt.sizeDelta = new Vector2(1000f, 500f);

            var cimg = cardObj.GetComponent<Image>();
            cimg.color = new Color(0f, 0f, 0f, 0.80f);

            // Text inside card (文字物件)
            GameObject textObj = new GameObject("EndReportText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);

            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(40f, 30f);
            trt.offsetMax = new Vector2(-40f, -30f);

            endReportText = textObj.GetComponent<TextMeshProUGUI>();
            endReportText.alignment = TextAlignmentOptions.Center;
            endReportText.enableAutoSizing = true;

            // 🔥 新增：強制設定為粗體
            endReportText.fontStyle = FontStyles.Bold;

            endReportText.fontSizeMax = 80;
            endReportText.fontSizeMin = 40;
            endReportText.fontSize = 70;
            endReportText.color = Color.white;
            endReportText.enableWordWrapping = true;
            endReportText.richText = true;

            // 🔧 Fix: 只係結算報告變方格＝字體冇中文字形
            // 最穩係沿用場景內其他已正常顯示中文的 TMP 字體（例如 combat log / turn indicator）
            if (txtCombatLog != null && txtCombatLog.font != null)
            {
                endReportText.font = txtCombatLog.font;
                endReportText.fontSharedMaterial = txtCombatLog.fontSharedMaterial;
            }
            else if (txtTurnIndicator != null && txtTurnIndicator.font != null)
            {
                endReportText.font = txtTurnIndicator.font;
                endReportText.fontSharedMaterial = txtTurnIndicator.fontSharedMaterial;
            }
        }

        // Always keep it on top
        endReportPanel.transform.SetAsLastSibling();
    }

    void ShowEndReport()
    {
        EnsureEndReportUI();
        if (endReportPanel == null || endReportText == null) return;

        int total = correctCount + wrongCount;
        float acc = total > 0 ? (correctCount * 100f / total) : 0f;

        string titleText = "";
        if (state == BattleState.WON)
        {
            titleText = "<color=#FFD700>Victory! Received 10 gold coins.</color>\n\n";
        }
        else
        {
            titleText = "<color=#FF9999>Defeat...</color>\n\n";
        }

        // 🔥 修改內容：喺成串文字前後加上 <b>...</b>
        endReportText.text = "<b>" +
            titleText +
            $"Learning Settlement Report\n" +
            $"Correctly <color=#55FF55>{correctCount}</color> , Incorrectly <color=#FF5555>{wrongCount}</color>.\n" +
            $"Accuracy: {acc:0.#}%" +
            "</b>";

        endReportPanel.SetActive(true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只要唔係戰鬥場景，就確保結算 UI 唔會殘留
        if (scene.name != "BattleScene")
        {
            if (endReportPanel != null)
            {
                Destroy(endReportPanel);
                endReportPanel = null;
                endReportText = null;
            }
        }
    }
}