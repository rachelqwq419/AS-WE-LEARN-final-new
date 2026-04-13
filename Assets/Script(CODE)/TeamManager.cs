using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("Team Setup")]
    public List<Character> playerTeamCharacters = new List<Character>(); // 使用場景實例

    public int maxTeamSize = 5;

    private void Awake()
    {
        SetupSingleton();
    }

    private void Start()
    {
        CollectTeamMembersFromScene();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 自動從場景收集初始隊伍成員
    private void CollectTeamMembersFromScene()
    {
        playerTeamCharacters.Clear();
        Character[] allCharacters = FindObjectsOfType<Character>(true);

        foreach (var character in allCharacters)
        {
            if (character.characterData != null)
            {
                character.InitializeFromData();

                // 🔥 關鍵修正：第一次收集隊員時，幫佢哋補滿血！
                // 確保佢哋由 Map 入 Battle 嗰陣唔會係 0 滴血
                character.health = character.maxHealth;

                if (character.characterIcon == null && character.characterData != null)
                    character.characterIcon = character.characterData.characterIcon;

                character.transform.SetParent(transform);
                character.gameObject.SetActive(false);
                playerTeamCharacters.Add(character);
            }
        }

        Debug.Log($"TeamManager 收集到 {playerTeamCharacters.Count} 個角色");
    }

    // 新增隊友（動態加入實例）
    public bool AddToTeam(GameObject characterPrefab)
    {
        if (playerTeamCharacters.Count >= maxTeamSize)
        {
            Debug.Log("隊伍已滿，無法加入更多隊友");
            return false;
        }

        GameObject newMember = Instantiate(characterPrefab);
        Character newCharacter = newMember.GetComponent<Character>();

        if (newCharacter == null)
        {
            Debug.LogError("加入的 prefab 缺少 Character 元件：" + characterPrefab.name);
            Destroy(newMember);
            return false;
        }

        newCharacter.InitializeFromData();

        // 🔥 關鍵修正：動態加入隊友時，亦要幫佢補滿血！
        newCharacter.health = newCharacter.maxHealth;

        if (newCharacter.characterIcon == null && newCharacter.characterData != null)
            newCharacter.characterIcon = newCharacter.characterData.characterIcon;

        playerTeamCharacters.Add(newCharacter);
        newMember.SetActive(false);

        Debug.Log($"已加入隊友：{newCharacter.characterName}");

        return true;
    }

    // 可選：移除隊友
    public void RemoveFromTeam(Character characterToRemove)
    {
        if (playerTeamCharacters.Remove(characterToRemove))
        {
            if (characterToRemove != null && characterToRemove.gameObject != null)
            {
                Destroy(characterToRemove.gameObject);
            }
            Debug.Log($"已移除隊友：{characterToRemove?.characterName}");
        }
    }
}