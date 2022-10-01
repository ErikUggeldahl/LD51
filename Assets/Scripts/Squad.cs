using UnityEngine;

public class Squad : MonoBehaviour
{
    public int teamID;
    public int squadID;

    public bool Active { get; private set; }

    Transform debugTarget;

    int nextSoldierIndex = 0;
    int aliveCount = 0;
    Soldier[] soldiers;

    public Squad Create(int size, int teamID, int squadID)
    {
        debugTarget = GameObject.Find("Debug Target").transform;

        soldiers = new Soldier[size];

        this.teamID = teamID;
        this.squadID = squadID;

        return this;
    }

    void OnDeath(int index)
    {
        aliveCount--;

        if (aliveCount == 0) Deactivate();
    }

    void Deactivate()
    {
        Active = false;

        name = "(D) " + name;
    }

    public void AddSoldier(Soldier soldier)
    {
        soldier.squad = this;
        soldier.indexInSquad = nextSoldierIndex;
        soldier.target = debugTarget;
        soldier.OnDie += OnDeath;

        soldiers[nextSoldierIndex] = soldier;

        nextSoldierIndex++;
        aliveCount++;

        Active = true;
    }
}
