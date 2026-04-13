using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubjectFilter : MonoBehaviour
{
    public GameObject btnChinese;
    public GameObject btnEnglish;
    public GameObject btnMath;

    void Start()
    {
        // 讀取便條紙，如果無就預設叫 "None"
        string subject = PlayerPrefs.GetString("CurrentSubject", "None");

        // 先全部熄晒佢
        btnChinese.SetActive(false);
        btnEnglish.SetActive(false);
        btnMath.SetActive(false);

        // 根據便條紙開返對應嗰個，順便將佢移去畫面正中間 (X坐標變0)
        if (subject == "Chinese")
        {
            btnChinese.SetActive(true);
            CenterButton(btnChinese);
        }
        else if (subject == "English")
        {
            btnEnglish.SetActive(true);
            CenterButton(btnEnglish);
        }
        else if (subject == "Math")
        {
            btnMath.SetActive(true);
            CenterButton(btnMath);
        }
        else
        {
            // 如果直接喺呢個 Scene 禁 Play 測試，就全部著晒
            btnChinese.SetActive(true);
            btnEnglish.SetActive(true);
            btnMath.SetActive(true);
        }
    }

    void CenterButton(GameObject btn)
    {
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y);
    }
}