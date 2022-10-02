using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Soldier : MonoBehaviour
{
    public enum State
    {
        Navigating,
        Following,
        Defending,
        Attacking,
        Shooting,
        Dead,
    }

    public enum Kind
    {
        Soldier,
        Archer,
    }

    const float SPEED = 2f;
    const float STOPPING_DISTANCE = 0.01f;
    const float ATTACK_DISTANCE = 2f;
    public const float ARCHER_TARGET_RANGE = 50;
    const int SOLDIER_KILL_CHANCE = 60;
    const int ARCHER_KILL_CHANCE = 180;
    const float SHOOTING_VARIANCE = 3f;
    const float SHOOTING_VELOCITY_ADJUST_FACTOR = 3f;
    const float AUDIO_PITCH_HALF_RANGE = 0.2f;
    const int AUDIO_SKIP_RANGE = 10;
    static Vector3 Y_MASK = new Vector3(1f, 0f, 1f);
    static Vector3 ARROW_SPAWN_OFFSET = new Vector3(0f, 2f, 0f);

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

    [SerializeField]
    GameObject arrowPre;

    public BattleSettings settings;
    public Timer timer;
    public Squad squad;

    public int indexInSquad;

    public Kind kind;
    public SoldierSprites sprites;

    public State initialState = State.Defending;
    public State state { get; private set; }

    public event System.Action<int> OnDie;

    Vector3 lastNavigatePosition;
    Soldier enemyTarget;

    new Transform camera;

    const float SHOOT_COOLDOWN = 5f;
    float shootCooldown = 0f;

    const float VOCAL_COOLDOWN = 5f;
    const float VOCAL_COOLDOWN_RANGE = 3f;
    const float VOCAL_DELAY_RANGE = 0.5f;
    bool vocalsReady = true;

    void Start()
    {
        camera = Camera.main.transform;

        name = $"{kind.ToString()} ({squad.teamID}:{squad.squadID}:{indexInSquad})";

        renderer.material.color = settings.teams[squad.teamID].color;

        agent.enabled = false;
        agent.updatePosition = false;
        agent.updateRotation = false;

        switch (initialState)
        {
            case State.Navigating: EnterNavigating(); break;
            case State.Following: EnterFollowing(); break;
            case State.Defending: EnterDefending(); break;
        }
    }

    void FixedUpdate()
    {
        if (transform.position.y < -20)
        {
            EnterDeath();
        }

        if (!timer.Active || state == State.Dead) return;
        switch (state)
        {
            case State.Navigating: Navigate(); break;
            case State.Following: Follow(); break;
            case State.Attacking: Attack(); break;
            case State.Shooting: Shoot(); break;
        }
    }

    void Navigate()
    {
        if (!agent.enabled)
        {
            Debug.LogWarning("Trying to navigate without agent enabled.");
            return;
        }

        if (squad.navigationTarget.position != lastNavigatePosition)
        {
            agent.destination = squad.navigationTarget.position;
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

        if (squad.Leader.state == State.Dead)
        {
            Debug.LogWarning("Trying to follow a dead leader.");
            return;
        }

        var toLeader = Vector3.Scale(squad.Leader.transform.position - transform.position, Y_MASK);
        if (toLeader.magnitude < STOPPING_DISTANCE) return;
        toLeader.Normalize();

        Move(toLeader);
    }

    void Face(Vector3 direction)
    {
        billboard.flipped = camera.InverseTransformDirection(direction).x > 0f;
    }

    void Move(Vector3 direction)
    {
        Face(direction);

        var deltaVelocity = Mathf.Clamp(SPEED - rigidbody.velocity.magnitude, 0f, SPEED);
        rigidbody.AddForce(direction * deltaVelocity, ForceMode.Impulse);
        agent.nextPosition = transform.position;
    }

    void Attack()
    {
        if (!enemyTarget)
        {
            Debug.LogWarning("Trying to attack but enemy target is null.");
            return;
        }

        if (enemyTarget.state == State.Dead)
        {
            Debug.LogWarning("Trying to attack but enemy target is dead.");
            return;
        }

        if (Vector3.Distance(transform.position, enemyTarget.transform.position) > ATTACK_DISTANCE)
        {
            var toEnemy = Vector3.Scale(enemyTarget.transform.position - transform.position, Y_MASK).normalized;
            Move(toEnemy);
        }
        else if (kind == Kind.Soldier && Random.Range(0, SOLDIER_KILL_CHANCE) == 0)
        {
            enemyTarget.EnterDeath();
        }
        else if (kind == Kind.Soldier && Random.Range(0, ARCHER_KILL_CHANCE) == 0)
        {
            enemyTarget.EnterDeath();
        }
    }

    void Shoot()
    {
        if (!enemyTarget)
        {
            Debug.LogWarning("Trying to shoot but enemy target is null.");
            return;
        }

        if (enemyTarget.state == State.Dead)
        {
            Debug.LogWarning("Trying to shoot but enemy target is dead.");
            return;
        }

        var toEnemy = Vector3.Scale(enemyTarget.transform.position - transform.position, Y_MASK);
        if (toEnemy.magnitude > ARCHER_TARGET_RANGE)
        {
            EnterDefending();
        }

        if (shootCooldown > 0)
        {
            shootCooldown -= Time.deltaTime;
            return;
        }
        shootCooldown = SHOOT_COOLDOWN;

        toEnemy.Normalize();

        var arrowGO = Instantiate(arrowPre, transform.position + toEnemy + ARROW_SPAWN_OFFSET, Quaternion.identity);
        var arrow = arrowGO.GetComponent<Arrow>();

        var velocityAdjust = enemyTarget.rigidbody.velocity * SHOOTING_VELOCITY_ADJUST_FACTOR;
        var variance = new Vector3(Random.Range(-SHOOTING_VARIANCE, SHOOTING_VARIANCE), 0f, Random.Range(-SHOOTING_VARIANCE, SHOOTING_VARIANCE));
        arrow.desiredPosition = enemyTarget.transform.position + velocityAdjust + variance;

        arrow.color = settings.teams[squad.teamID].color;
    }

    bool SwitchStates(State newState)
    {
        if (state == State.Dead)
        {
            Debug.LogWarning("Trying to switch states from death.");
            return false;
        }

        switch (state)
        {
            case State.Navigating: ExitNavigating(); break;
            case State.Attacking: ExitAttacking(); break;
            case State.Shooting: ExitShooting(); break;
        }

        state = newState;
        return true;
    }

    public void EnterNavigating()
    {
        if (!squad.navigationTarget)
        {
            Debug.LogWarning("Trying to switch to Navigating, but squad doesn't have a target.");
            return;
        }

        if (!SwitchStates(State.Navigating)) return;

        agent.enabled = true;
        agent.destination = squad.navigationTarget.position;
        lastNavigatePosition = squad.navigationTarget.position;

        SetSprite(sprites.run);
        PlaySound(sounds.BattleCry());
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

        if (squad.Leader.state == State.Dead)
        {
            Debug.LogWarning("Trying to switch to Following, but the leader is dead.");
            return;
        }

        if (!SwitchStates(State.Following)) return;

        SetSprite(sprites.run);
        PlaySound(sounds.BattleCry());
    }

    public void EnterDefending()
    {
        if (!SwitchStates(State.Defending)) return;

        SetSprite(sprites.stand);
    }

    public void EnterAttacking(Soldier enemy)
    {
        if (!enemy)
        {
            Debug.LogWarning("Trying to switch to Attacking, but enemy is null.");
            return;
        }

        if (enemy.state == State.Dead)
        {
            Debug.LogWarning("Trying to switch to Attacking, but enemy is dead.");
            return;
        }

        if (!SwitchStates(State.Attacking)) return;

        enemyTarget = enemy;
        enemyTarget.OnDie += OnEnemyDeath;

        SetSprite(sprites.attack);
        PlaySound(sounds.BattleCry());
    }

    void ExitAttacking()
    {
        enemyTarget.OnDie -= OnEnemyDeath;
        enemyTarget = null;
    }

    public void EnterShooting(Soldier enemy)
    {
        if (kind == Kind.Soldier)
        {
            Debug.LogWarning("Trying to switch to Shooting as a Soldier.");
            return;
        }

        if (!enemy)
        {
            Debug.LogWarning("Trying to switch to Shooting, but enemy is null.");
            return;
        }

        if (enemy.state == State.Dead)
        {
            Debug.LogWarning("Trying to switch to Shooting, but enemy is dead.");
            return;
        }

        if (!SwitchStates(State.Shooting)) return;

        enemyTarget = enemy;
        enemyTarget.OnDie += OnEnemyDeath;

        SetSprite(sprites.attack);
    }

    void ExitShooting()
    {
        enemyTarget.OnDie -= OnEnemyDeath;
        enemyTarget = null;
    }

    public void EnterDeath()
    {
        if (!SwitchStates(State.Dead)) return;

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

        if (enemyTarget == null || state == State.Dead) return;

        Gizmos.color = squad.teamID == 0 ? Color.red : Color.blue;
        Gizmos.DrawLine(transform.position, enemyTarget.transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        Soldier soldier = collision.collider.GetComponent<Soldier>();
        if (soldier == null || soldier.squad.teamID == squad.teamID) return;

        if (state != State.Dead && soldier.state != State.Dead)
        {
            EnterAttacking(soldier);
        }
    }

    void OnEnemyDeath(int _)
    {
        if (squad.EnemyTarget && squad.EnemyTarget.state != State.Dead)
        {
            switch (kind)
            {
                case Kind.Soldier: EnterAttacking(squad.EnemyTarget); break;
                case Kind.Archer: EnterShooting(squad.EnemyTarget); break;
            }   
        }
        else if (kind == Kind.Archer)
        {
            EnterDefending();
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

    void SetSprite(Texture[] sprite)
    {
        renderer.material.mainTexture = SoldierSprites.Get(sprite);
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
