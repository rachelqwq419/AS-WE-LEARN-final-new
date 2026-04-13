using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{

    [Header("UIComponent")]
    public TextMeshProUGUI textLabel;


    [Header("ScriptAssest")]
    public TextAsset textFile;
    public int index;

    List<string> textList = new List<string>();
    // Start is called before the first frame update
    void Start()
    {
       GetTextFromFile(textFile);
        index = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) 
        {

        textLabel.text = textList[index];
        index++;
        
        }
    }

    void GetTextFromFile(TextAsset file)
    {
        textList.Clear();
        index = 0;

        var lineDate = file.text.Split('\n');

        foreach(var line in lineDate)
        {
            textList.Add(line);
        }
    }
}
