
using UnityEngine;

public class GoldManager : MonoBehaviour

{

    public static GoldManager instance;
    public int gold;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadGold();
    }

    // Start is called before the first frame update
  

    


    public void AddGold(int amount)
    {
        gold += amount;
        SaveGold();
    }

    public void ReduceGold(int amount)
    {
        gold -= amount;

        if (gold <= 0)
        {
            gold = 0;
        }

        SaveGold();
    }

    private void OnApplicationQuit()
    {
        SaveGold();
    }

    public void SaveGold()
    {
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.Save(); // Ensure it's written to disk
    }

    public void LoadGold()
    {
        gold = PlayerPrefs.GetInt("Gold", 0); // Default to 0 if not found
    }
}
