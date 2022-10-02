using UnityEngine;

public class Targeter : MonoBehaviour
{
    public int teamID;

    public event System.Action<Soldier> OnTarget;

    void OnTriggerEnter(Collider other)
    {
        Soldier soldier = other.GetComponent<Soldier>();
        if (soldier == null)
        {
            Debug.LogWarning("Targeter triggered with a non-soldier entity.");
            return;
        }
        if (!soldier.Alive) Debug.LogWarning("Targeting a dead soldier.");

        if (soldier.squad.teamID == teamID || !soldier.Alive) return;

        OnTarget?.Invoke(soldier);
    }
}
