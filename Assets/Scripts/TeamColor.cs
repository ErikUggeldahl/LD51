using UnityEngine;

public class TeamColor : MonoBehaviour
{
    [SerializeField]
    BattleSettings settings;

    public int team;

    void Start()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.material.color = settings.teams[team].color;
        }    
    }
}
