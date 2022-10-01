using UnityEngine;

[CreateAssetMenu(fileName = "SoldierSound", menuName = "ScriptableObjects/SoldierSound", order = 1)]
public class SoldierSound : ScriptableObject
{
    public AudioClip[] battleCries;

    public AudioClip BattleCry()
    {
        return battleCries[Random.Range(0, battleCries.Length)];
    }
}
