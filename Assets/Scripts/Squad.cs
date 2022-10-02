using System.Linq;
using UnityEngine;

public class Squad : MonoBehaviour
{
    public int teamID;
    public int squadID;

    public bool Active { get; private set; }
    public Soldier Leader { get; private set; }

    public Transform navigationTarget { get; private set; }

    Targeter targeter;
    public Soldier EnemyTarget { get; private set; }

    int nextSoldierIndex = 0;
    Soldier[] soldiers;

    public Squad Create(int size, int teamID, int squadID)
    {
        navigationTarget = GameObject.Find("Debug Target").transform;

        soldiers = new Soldier[size];

        this.teamID = teamID;
        this.squadID = squadID;

        return this;
    }
    public void AddSoldier(Soldier soldier)
    {
        soldier.squad = this;
        soldier.indexInSquad = nextSoldierIndex;
        soldier.OnDie += OnMemberDeath;

        soldiers[nextSoldierIndex] = soldier;

        if (nextSoldierIndex == 0)
        {
            Leader = soldier;
            targeter = soldier.EnableTargeter();
            targeter.OnTarget += OnTargetAcquired;

            soldier.initialState = Soldier.UnitState.Navigating;
        }
        else
        {
            soldier.initialState = Soldier.UnitState.Following;
        }

        nextSoldierIndex++;

        Active = true;
    }


    void OnMemberDeath(int index)
    {
        if (soldiers.Where((soldier) => soldier.State != Soldier.UnitState.Dead).Count() == 0)
        {
            Deactivate();
            return;
        }

        if (targeter.transform.parent.GetComponent<Soldier>().indexInSquad == index)
        {
            Soldier firstAlive = soldiers.Where((soldier) => soldier.State != Soldier.UnitState.Dead).First();
            targeter.transform.parent = firstAlive.transform;
            targeter.transform.localPosition = Vector3.zero;
            Leader = firstAlive;
        }
    }

    void Deactivate()
    {
        Active = false;

        name = "(D) " + name;

        Leader = null;

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
        if (!Active)
        {
            Debug.LogWarning("Target acquired after deactivated.");
            return;
        }

        if (EnemyTarget != null)
        {
            Debug.LogWarning("Target acquired while it isn't yet cleared.");
            return;
        }

        targeter.gameObject.SetActive(false);

        EnemyTarget = target;
        EnemyTarget.OnDie += OnTargetDeath;

        foreach (var soldier in soldiers.Where((soldier) => soldier.State != Soldier.UnitState.Attacking && soldier.State != Soldier.UnitState.Dead))
        {
            soldier.EnterAttacking(EnemyTarget);
        }
    }

    void OnTargetDeath(int _)
    {
        targeter.gameObject.SetActive(true);

        EnemyTarget.OnDie -= OnTargetDeath;
        EnemyTarget = null;
    }
}
