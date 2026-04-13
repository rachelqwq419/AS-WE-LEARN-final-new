
using UnityEngine;

[CreateAssetMenu(menuName = "New CharacterData")]
public class CharacterData : ScriptableObject

{
    public string characterName;
    public Sprite characterIcon;

    [Header("角色基礎數值")]
    public float baseMaxHealth = 300f;
    public float baseMaxMana = 100f;
    public float baseAttackPower = 50f;
    public float baseDefencePower = 25f;
    public float BaseCriticalChance = 0.1f;
    public float BaseCriticalMultiplier = 1.5f;
    public float BasePoisonDamage = 0f;
    public float currentExp = 0f;
}
