using UnityEngine;

[System.Serializable]
public struct Team
{
    public int id;
    public Color color;
    public Color deadColor;
}

[CreateAssetMenu(fileName = "BattleSettings", menuName = "ScriptableObjects/BattleSettings", order = 1)]
public class BattleSettings : ScriptableObject
{
    public Team[] teams;
}
