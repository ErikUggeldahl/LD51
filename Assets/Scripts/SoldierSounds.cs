using UnityEngine;

[CreateAssetMenu(fileName = "SoldierSounds", menuName = "ScriptableObjects/SoldierSounds", order = 1)]
public class SoldierSounds : ScriptableObject
{
    public AudioClip[] battleCries;

    public AudioClip BattleCry()
    {
        return battleCries[Random.Range(0, battleCries.Length)];
    }
}
