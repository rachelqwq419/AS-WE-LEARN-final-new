using System.Collections;
using UnityEngine;


[System.Serializable]
public class CharacterEquipData
{
    public string characterName;
    public string weaponId;
    public string armorId;
    public string bootsId;
    public string helmId;
}

// Class to hold all data that will be saved
[System.Serializable]
public class SaveData
{
    // Inventory data (array of slots)
    public SInventorySlot[] inventory;

    public CharacterEquipData[] characterEquipment;
}

// Structure to define one inventory slot
[System.Serializable]
public struct SInventorySlot
{
    public bool occupied;     // Whether this slot has an item
    public string itemId;     // The ID of the item
    public int quantity;      // Quantity of the item in this slot
}


public class SaveManager : MonoBehaviour
{

    public static SaveManager instance;
    [Header("Auto-Loaded Items")]
    public ItemData[] items;

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

        items = Resources.LoadAll<ItemData>("Items");
    }

    // Called on the first frame
    private void Start()
    {
        StartCoroutine(LoadInventory()); // Begin loading saved game data
    }

    // Called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) SaveInventory();               // Press 'P' to manually save
        if (Input.GetKeyDown(KeyCode.M)) PlayerPrefs.DeleteAll(); // Press 'M' to clear all saved data
    }

    // Coroutine to delay loading until the end of the first frame
    IEnumerator LoadInventory()
    {
        yield return new WaitForEndOfFrame(); // Wait until all other startup operations are done
        if (PlayerPrefs.HasKey("Save"))       // Check if save data exists
        {
            Load(); // Load saved data
        }
    }

    // Save current game state
    public void SaveInventory()
    {
        SaveData data = new SaveData(); // Create new save data object
        data.inventory = new SInventorySlot[InventoryManager.instance.slots.Length]; // Match slot count

        // Loop through all inventory slots
        for (int i = 0; i < InventoryManager.instance.slots.Length; i++)
        {
            var slot = InventoryManager.instance.slots[i]; // Get slot

            // Save each slot’s data
            data.inventory[i] = new SInventorySlot
            {
                occupied = slot.item != null,                     // Check if slot is occupied
                itemId = slot.item != null ? slot.item.id : null, // Save item ID if available
                quantity = slot.quantity                          // Save quantity
            };
        }

        // 1) create array sized to your party
        int count = TeamManager.Instance.playerTeamCharacters.Count;
        data.characterEquipment = new CharacterEquipData[count];

        // 2) for each party member, snapshot its equipped IDs
        for (int i = 0; i < count; i++)
        {
            var go = TeamManager.Instance.playerTeamCharacters[i];
            var chr = go.GetComponent<Character>();
            var ce = new CharacterEquipData
            {
                characterName = chr.characterData.characterName,
                weaponId = chr.equippedWeapon?.id,
                armorId = chr.equippedArmor?.id,
                bootsId = chr.equippedBoots?.id,
                helmId = chr.equippedHelm?.id
            };
            data.characterEquipment[i] = ce;
        }


        // Convert save data to JSON
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("Save", json); // Store it in PlayerPrefs
        Debug.Log("Saved inventory:\n" + JsonUtility.ToJson(data, true)); // Log for debug
    }

    public void Load()
    {
        // 1) Deserialize
        string json = PlayerPrefs.GetString("Save");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("沒有找到存檔資料，使用預設值");
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        // 2) Rebuild the global inventory slots
        for (int i = 0; i < saveData.inventory.Length && i < InventoryManager.instance.slots.Length; i++)
        {
            var slotData = saveData.inventory[i];
            if (!slotData.occupied)
            {
                InventoryManager.instance.slots[i].item = null;
                InventoryManager.instance.slots[i].quantity = 0;
            }
            else
            {
                ItemData item = GetItemByID(slotData.itemId);
                InventoryManager.instance.slots[i].item = item;
                InventoryManager.instance.slots[i].quantity = slotData.quantity;
            }
        }

        // 3) Restore each character’s saved equipment
        if (saveData.characterEquipment != null)
        {
            int charCount = saveData.characterEquipment.Length;
            for (int i = 0; i < charCount; i++)
            {
                // 防範隊伍大小不匹配
                if (i >= TeamManager.Instance.playerTeamCharacters.Count)
                    break;

                CharacterEquipData ce = saveData.characterEquipment[i];

                // 直接取 Character（因為清單存的就是 Character）
                Character chr = TeamManager.Instance.playerTeamCharacters[i];

                if (chr == null)
                {
                    Debug.LogWarning($"隊伍索引 {i} 的 Character 是 null，跳過");
                    continue;
                }

                // 還原裝備
                chr.equippedWeapon = GetItemByID(ce.weaponId);
                chr.equippedArmor = GetItemByID(ce.armorId);
                chr.equippedBoots = GetItemByID(ce.bootsId);
                chr.equippedHelm = GetItemByID(ce.helmId);

                // 重新計算數值（重要！）
                chr.InitializeFromData();
                chr.RecalculateStats();  // 如果你有這個方法，也呼叫它
            }
        }

        // 最後更新 UI
        InventoryManager.instance.UpdateUI();
    }

    public ItemData GetItemByID(string id)
    {
        foreach (var item in items)
        {
            if (item.id == id)
                return item;
        }
        return null;
    }
}
