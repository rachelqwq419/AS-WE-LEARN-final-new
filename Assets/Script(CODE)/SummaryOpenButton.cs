using UnityEngine;

/// <summary>
/// 掛喺「summary button」上面。Button → On Click 請拖 <b>Hierarchy 入面呢粒掣</b>，
/// 再揀 <see cref="OnClickOpenSummary"/>（唔好拖 Project 嘅 .cs 檔）。
/// 場景入面必須有一個 Active 嘅物件掛住 <see cref="Summary"/>（例如 summary system）。
/// </summary>
[DisallowMultipleComponent]
public class SummaryOpenButton : MonoBehaviour
{
    public void OnClickOpenSummary()
    {
        Summary.OpenSummaryPanelStatic();
    }
}
