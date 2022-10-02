using System.Linq;
using UnityEngine;

public class Squad : MonoBehaviour
{
    public int teamID;
    public int squadID;

    public bool Active { get; private set; }
    Soldier.Kind kind;
    public Soldier Leader { get; private set; }

    public Transform navigationTarget { get; private set; }

    Targeter targeter;
    public Soldier EnemyTarget { get; private set; }

    int nextSoldierIndex = 0;
    Soldier[] soldiers;

    public Squad Create(Soldier.Kind kind, int size, int teamID, int squadID)
    {
        navigationTarget = GameObject.Find("Debug Target").transform;

        soldiers = new Soldier[size];

        this.kind = kind;
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

            if (kind == Soldier.Kind.Archer)
            {
                targeter.GetComponent<SphereCollider>().radius = Soldier.ARCHER_TARGET_RANGE;
            }

            var initialState = soldier.kind == Soldier.Kind.Soldier ? Soldier.State.Navigating : Soldier.State.Defending;
            soldier.initialState = initialState;
        }
        else
        {
            var initialState = soldier.kind == Soldier.Kind.Soldier ? Soldier.State.Following : Soldier.State.Defending;
            soldier.initialState = initialState;
        }

        nextSoldierIndex++;

        Active = true;
    }

    void OnMemberDeath(int index)
    {
        if (soldiers.Where((soldier) => soldier.state != Soldier.State.Dead).Count() == 0)
        {
            Deactivate();
            return;
        }

        if (targeter.transform.parent.GetComponent<Soldier>().indexInSquad == index)
        {
            Soldier firstAlive = soldiers.Where((soldier) => soldier.state != Soldier.State.Dead).First();
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

        foreach (var soldier in soldiers.Where((soldier) => soldier.state != Soldier.State.Attacking && soldier.state != Soldier.State.Dead))
        {
            switch (kind)
            {
                case Soldier.Kind.Soldier: soldier.EnterAttacking(EnemyTarget); break;
                case Soldier.Kind.Archer: soldier.EnterShooting(EnemyTarget); break;
            }
        }
    }

    void OnTargetDeath(int _)
    {
        targeter.gameObject.SetActive(true);

        EnemyTarget.OnDie -= OnTargetDeath;
        EnemyTarget = null;
    }
}
