using UnityEngine;

/// <summary>
/// 掛喺「Tutorial」按鈕上。Button → On Click 拖 Hierarchy 入面呢粒掣，揀 TutorialOpenButton → OnClickOpenTutorial。
/// </summary>
[DisallowMultipleComponent]
public class TutorialOpenButton : MonoBehaviour
{
    public void OnClickOpenTutorial()
    {
        TutorialPanel.OpenTutorialStatic();
    }
}
