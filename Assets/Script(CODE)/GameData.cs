using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class GameData
{
    // 這裡用 static (靜態)，代表資料會一直活著，切換場景也不會不見
    public static string chosenSubject; // 存 "Chinese", "English", "Math"
    public static int chosenLevel;      // 存 1, 2, 3, 4, 5
}