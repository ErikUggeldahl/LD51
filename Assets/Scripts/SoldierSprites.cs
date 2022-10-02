using UnityEngine;

[CreateAssetMenu(fileName = "SoldierSprites", menuName = "ScriptableObjects/SoldierSprites", order = 1)]
public class SoldierSprites : ScriptableObject
{
    public Texture[] stand;
    public Texture[] run;
    public Texture[] attack;
    public Texture[] die;

    public static Texture Get(Texture[] sprites)
    {
        return sprites[Random.Range(0, sprites.Length)];
    }
}