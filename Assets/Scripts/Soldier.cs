using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Soldier : MonoBehaviour
{
    const float SPEED = 2f;
    const float STOPPING_DISTANCE = 0.01f;
    const float ATTACK_DISTANCE = 2f;
    const float AUDIO_PITCH_HALF_RANGE = 0.2f;
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

    public Transform target;
    Soldier enemyTarget;
    Vector3 lastTargetPosition;

    public bool Alive { get; private set; } = true;
    public event System.Action<int> OnDie;

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

        agent.updatePosition = false;
        agent.updateRotation = false;

        if (indexInSquad != 0) agent.enabled = false;
        else
        {
            agent.destination = target.position;
        }
    }

    void FixedUpdate()
    {
        if (!timer.Active || !target || !Alive) return;

        if (lastTargetPosition != target.position)
        {
            PlaySound(sounds.BattleCry(), true);
            lastTargetPosition = target.position;
        }

        MoveToTarget();
        AttackEnemy();
    }

    void MoveToTarget()
    {
        if (!agent.enabled) return;

        //if (Vector3.Distance(transform.position, target.position) < 0.1f) return;
        var toTarget = Vector3.Scale(agent.steeringTarget - transform.position, Y_MASK);
        //var toTarget = Vector3.Scale(target.position - transform.position, Y_MASK);
        if (toTarget.magnitude < STOPPING_DISTANCE) return;
        toTarget.Normalize();

        billboard.flipped = camera.InverseTransformDirection(toTarget).x > 0f;

        var deltaVelocity = Mathf.Clamp(SPEED - rigidbody.velocity.magnitude, 0f, SPEED);
        rigidbody.AddForce(toTarget * deltaVelocity, ForceMode.Impulse);
        agent.nextPosition = transform.position;
    }

    void AttackEnemy()
    {
        if (enemyTarget && Vector3.Distance(transform.position, enemyTarget.transform.position) <= ATTACK_DISTANCE)
        {
            if (Random.Range(0, 60) == 0)
            {
                enemyTarget.Die();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (agent.hasPath && !agent.pathPending)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.steeringTarget);

            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawSphere(corner, 0.5f);
            }
        }

        if (enemyTarget == null || !Alive) return;

        Gizmos.color = squad.teamID == 0 ? Color.red : Color.blue;
        Gizmos.DrawLine(transform.position, enemyTarget.transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        Soldier soldier = collision.collider.GetComponent<Soldier>();
        if (soldier == null || soldier.squad.teamID == squad.teamID) return;

        SetEnemyTarget(soldier);
    }

    public void SetEnemyTarget(Soldier enemy)
    {
        if (enemyTarget)
        {
            enemyTarget.OnDie -= OnEnemyDeath;
        }

        target = enemy.transform;
        enemyTarget = enemy;
        enemyTarget.OnDie += OnEnemyDeath;
    }

    void OnEnemyDeath(int _)
    {
        if (squad.EnemyTarget)
        {
            target = squad.EnemyTarget.transform;
            enemyTarget = squad.EnemyTarget;
        }
        else
        {
            enemyTarget = null;
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

    public void Die()
    {
        if (!Alive) return;
        Alive = false;

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

    void SetSprite(Texture sprite)
    {
        renderer.material.mainTexture = sprite;
    }

    void PlaySound(AudioClip sound, bool vocal = false)
    {
        if (indexInSquad != 0 && Random.Range(0, 10) != 0) return;

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
