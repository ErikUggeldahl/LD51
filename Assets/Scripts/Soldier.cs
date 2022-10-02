using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Soldier : MonoBehaviour
{
    public enum UnitState
    {
        Navigating,
        Following,
        Defending,
        Attacking,
        Dead,
    }

    const float SPEED = 2f;
    const float STOPPING_DISTANCE = 0.01f;
    const float ATTACK_DISTANCE = 2f;
    const float AUDIO_PITCH_HALF_RANGE = 0.2f;
    const int AUDIO_SKIP_RANGE = 10;
    static Vector3 Y_MASK = new Vector3(1f, 0f, 1f);

    [SerializeField]
    SoldierSprites sprites;

    [SerializeField]
    SoldierSounds sounds;

    [SerializeField]
    new Rigidbody rigidbody;

    [SerializeField]
    NavMeshAgent agent;

    [SerializeField]
    new AudioSource audio;

    [SerializeField]
    Billboard billboard;

    [SerializeField]
    new Renderer renderer;

    [SerializeField]
    GameObject targeterPre;

    public BattleSettings settings;
    public Timer timer;
    public Squad squad;

    public int indexInSquad;

    public UnitState initialState = UnitState.Defending;
    public UnitState State { get; private set; }

    public event System.Action<int> OnDie;

    Soldier enemyTarget;

    new Transform camera;
    const float VOCAL_COOLDOWN = 5f;
    const float VOCAL_COOLDOWN_RANGE = 3f;
    const float VOCAL_DELAY_RANGE = 0.5f;
    bool vocalsReady = true;

    void Start()
    {
        camera = Camera.main.transform;

        name = $"Soldier({squad.teamID}:{squad.squadID}:{indexInSquad})";

        renderer.material.color = settings.teams[squad.teamID].color;

        agent.enabled = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        switch (initialState)
        {
            case UnitState.Navigating: EnterNavigating(); break;
            case UnitState.Following: EnterFollowing(); break;
            case UnitState.Defending: break;
        }
    }

    void FixedUpdate()
    {
        if (!timer.Active || State == UnitState.Dead) return;
        switch (State)
        {
            case UnitState.Navigating: Navigate(); break;
            case UnitState.Following: Follow(); break;
            case UnitState.Attacking: Attack(); break;
        }
    }

    void Navigate()
    {
        if (!agent.enabled)
        {
            Debug.LogWarning("Trying to navigate without agent enabled.");
            return;
        }

        var toDestination = Vector3.Scale(agent.steeringTarget - transform.position, Y_MASK);
        if (toDestination.magnitude < STOPPING_DISTANCE) return;
        toDestination.Normalize();

        Move(toDestination);
    }

    void Follow()
    {
        if (!squad.Leader)
        {
            Debug.LogWarning("Trying to follow but leader is null.");
            return;
        }

        if (squad.Leader.State == UnitState.Dead)
        {
            Debug.LogWarning("Trying to follow a dead leader.");
            return;
        }

        var toLeader = Vector3.Scale(squad.Leader.transform.position - transform.position, Y_MASK);
        if (toLeader.magnitude < STOPPING_DISTANCE) return;
        toLeader.Normalize();

        Move(toLeader);
    }

    void Move(Vector3 direction)
    {
        billboard.flipped = camera.InverseTransformDirection(direction).x > 0f;

        var deltaVelocity = Mathf.Clamp(SPEED - rigidbody.velocity.magnitude, 0f, SPEED);
        rigidbody.AddForce(direction * deltaVelocity, ForceMode.Impulse);
        agent.nextPosition = transform.position;
    }

    void Attack()
    {
        if (!enemyTarget)
        {
            Debug.LogWarning("Trying to attack but enemy target is dead.");
            return;
        }

        if (Vector3.Distance(transform.position, enemyTarget.transform.position) > ATTACK_DISTANCE)
        {
            var toEnemy = Vector3.Scale(enemyTarget.transform.position - transform.position, Y_MASK).normalized;
            Move(toEnemy);
        }
        else if (Random.Range(0, 60) == 0)
        {
            enemyTarget.EnterDeath();
        }
    }

    bool SwitchStates(UnitState newState)
    {
        if (State == UnitState.Dead)
        {
            Debug.LogWarning("Trying to switch states from death.");
            return false;
        }

        switch (State)
        {
            case UnitState.Navigating: ExitNavigating(); break;
            case UnitState.Attacking: ExitAttacking(); break;
        }

        State = newState;
        return true;
    }

    public void EnterNavigating()
    {
        if (!squad.navigationTarget)
        {
            Debug.LogWarning("Trying to switch to Navigating, but squad doesn't have a target.");
            return;
        }

        if (!SwitchStates(UnitState.Navigating)) return;

        agent.enabled = true;
        agent.destination = squad.navigationTarget.position;
    }

    void ExitNavigating()
    {
        agent.enabled = false;
    }

    public void EnterFollowing()
    {
        if (!squad.Leader)
        {
            Debug.LogWarning("Trying to switch to Following, but leader is null.");
            return;
        }

        if (squad.Leader.State == UnitState.Dead)
        {
            Debug.LogWarning("Trying to switch to Following, but the leader is dead.");
            return;
        }

        if (!SwitchStates(UnitState.Following)) return;

        SetSprite(sprites.run);
        PlaySound(sounds.BattleCry());
    }

    public void EnterAttacking(Soldier enemy)
    {
        if (!enemy)
        {
            Debug.LogWarning("Trying to switch to Attacking, but enemy is null.");
            return;
        }

        if (enemy.State == UnitState.Dead)
        {
            Debug.LogWarning("Trying to switch to Attacking, but enemy is dead.");
            return;
        }

        if (!SwitchStates(UnitState.Attacking)) return;

        enemyTarget = enemy;
        enemyTarget.OnDie += OnEnemyDeath;

        SetSprite(sprites.Attack());
        PlaySound(sounds.BattleCry());
    }

    void ExitAttacking()
    {
        enemyTarget.OnDie -= OnEnemyDeath;
        enemyTarget = null;
    }

    public void EnterDeath()
    {
        if (!SwitchStates(UnitState.Dead)) return;

        name = "(D) " + name;
        gameObject.layer = 0;

        var color = renderer.material.color;
        renderer.material.color = settings.teams[squad.teamID].deadColor;
        SetSprite(sprites.die);

        Destroy(agent);
        Destroy(rigidbody);
        Destroy(GetComponent<Collider>());

        OnDie(indexInSquad);

        enabled = false;
    }

    void OnDrawGizmos()
    {
        if (agent && agent.enabled && agent.hasPath && !agent.pathPending)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.steeringTarget);

            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawSphere(corner, 0.5f);
            }
        }

        if (enemyTarget == null || State == UnitState.Dead) return;

        Gizmos.color = squad.teamID == 0 ? Color.red : Color.blue;
        Gizmos.DrawLine(transform.position, enemyTarget.transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        Soldier soldier = collision.collider.GetComponent<Soldier>();
        if (soldier == null || soldier.squad.teamID == squad.teamID) return;

        EnterAttacking(soldier);
    }

    void OnEnemyDeath(int _)
    {
        if (squad.EnemyTarget && squad.EnemyTarget.State != UnitState.Dead)
        {
            EnterAttacking(squad.EnemyTarget);
        }
        else if (squad.Leader == this)
        {
            EnterNavigating();
        }
        else
        {
            EnterFollowing();
        }
    }

    public Targeter EnableTargeter()
    {
        var targeterGO = Instantiate(targeterPre);
        var targeter = targeterGO.GetComponent<Targeter>();
        targeter.transform.parent = transform;
        targeter.transform.localPosition = Vector3.zero;
        targeter.teamID = squad.teamID;
        return targeter;
    }

    void SetSprite(Texture sprite)
    {
        renderer.material.mainTexture = sprite;
    }

    void PlaySound(AudioClip sound, bool vocal = false)
    {
        if (indexInSquad != 0 && Random.Range(0, AUDIO_SKIP_RANGE) != 0) return;

        if (vocal)
        {
            StartCoroutine(PlayVocal(sound));
        }
        else
        {
            audio.pitch = 1f;
            audio.PlayOneShot(sound);
        }
    }

    IEnumerator PlayVocal(AudioClip sound)
    {
        if (!vocalsReady || audio.isPlaying) yield break;

        yield return new WaitForSeconds(Random.Range(0f, VOCAL_DELAY_RANGE));

        audio.pitch = Random.Range(1 - AUDIO_PITCH_HALF_RANGE, 1 + AUDIO_PITCH_HALF_RANGE);
        audio.PlayOneShot(sound);
        StartCoroutine(CooldownVocals());
    }

    IEnumerator CooldownVocals()
    {
        vocalsReady = false;
        yield return new WaitForSecondsRealtime(VOCAL_COOLDOWN + Random.Range(0f, VOCAL_COOLDOWN_RANGE));
        vocalsReady = true;
    }
}
