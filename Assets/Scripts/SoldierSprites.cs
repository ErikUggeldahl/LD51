using UnityEngine;

[CreateAssetMenu(fileName = "SoldierSprites", menuName = "ScriptableObjects/SoldierSprites", order = 1)]
public class SoldierSprites : ScriptableObject
{
    public Texture run;
    public Texture[] attack;
    public Texture die;

    public Texture Attack()
    {
        return attack[Random.Range(0, attack.Length)];
    }
}