#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ExportProjectForAI : EditorWindow
{
    [MenuItem("Tools/Export Scripts for AI 🤖")]
    public static void ExportScripts()
    {
        string exportPath = EditorUtility.SaveFilePanel("Save AI Context", "", "UnityProjectContext", "txt");
        if (string.IsNullOrEmpty(exportPath)) return;

        StringBuilder sb = new StringBuilder();
        string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        sb.AppendLine("=== Unity Project Scripts Context ===");
        sb.AppendLine($"Total Scripts: {files.Length}\n");

        foreach (string file in files)
        {
            if (file.Contains("Plugins") || file.Contains("TextMesh Pro")) continue;

            string relativePath = file.Replace(Application.dataPath, "Assets");
            
            sb.AppendLine("////////////////////////////////////////////////////");
            sb.AppendLine($"// File Path: {relativePath}");
            sb.AppendLine("////////////////////////////////////////////////////");
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine("\n\n");
        }

        File.WriteAllText(exportPath, sb.ToString());
        Debug.Log($"[AI Export] 搞掂！成功將 {files.Length} 個 Script 匯出到: {exportPath}");
    }
}
#endif