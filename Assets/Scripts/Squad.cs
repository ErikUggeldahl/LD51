using System.Linq;
using UnityEngine;

public class Squad : MonoBehaviour
{
    public int teamID;
    public int squadID;

    public bool Active { get; private set; }

    Targeter targeter;
    public Soldier EnemyTarget { get; private set; }
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

        if (aliveCount == 0)
        {
            Deactivate();
            return;
        }

        if (targeter.transform.parent.GetComponent<Soldier>().indexInSquad == index)
        {
            Soldier firstAlive = soldiers.Where((soldier) => soldier.Alive).First();
            targeter.transform.parent = firstAlive.transform;
            targeter.transform.localPosition = Vector3.zero;
        }
    }

    void Deactivate()
    {
        Active = false;

        name = "(D) " + name;

        targeter.OnTarget -= OnTargetAcquired;
        Destroy(targeter.gameObject);
        targeter = null;

        if (EnemyTarget)
        {
            EnemyTarget.OnDie -= OnTargetDeath;
            EnemyTarget = null;
        }
    }

    void OnTargetAcquired(Soldier target)
    {
        if (!Active) Debug.LogWarning("Target acquired after deactivated.");
        if (EnemyTarget != null) Debug.LogWarning("Target acquired while it isn't yet cleared.");

        targeter.gameObject.SetActive(false);

        EnemyTarget = target;
        EnemyTarget.OnDie += OnTargetDeath;

        foreach (var soldier in soldiers)
        {
            soldier.SetEnemyTarget(target);
        }
    }

    void OnTargetDeath(int _)
    {
        targeter.gameObject.SetActive(true);

        EnemyTarget.OnDie -= OnTargetDeath;
        EnemyTarget = null;
    }

    public void AddSoldier(Soldier soldier)
    {
        soldier.squad = this;
        soldier.indexInSquad = nextSoldierIndex;
        soldier.target = debugTarget;
        soldier.OnDie += OnDeath;

        soldiers[nextSoldierIndex] = soldier;

        if (nextSoldierIndex == 0)
        {
            targeter = soldier.EnableTargeter();
            targeter.OnTarget += OnTargetAcquired;
        }

        nextSoldierIndex++;
        aliveCount++;

        Active = true;
    }
}
