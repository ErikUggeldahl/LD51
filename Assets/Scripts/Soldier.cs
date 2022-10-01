using UnityEngine;

public class Soldier : MonoBehaviour
{
    const float SPEED = 2f;
    const float AUDIO_PITCH_HALF_RANGE = 0.2f;
    static Vector3 Y_MASK = new Vector3(1f, 0f, 1f);

    [SerializeField]
    SoldierSound sounds;

    [SerializeField]
    new Rigidbody rigidbody;

    [SerializeField]
    new AudioSource audio;

    [SerializeField]
    Billboard billboard;

    [SerializeField]
    new Renderer renderer;

    public BattleSettings settings;
    public Timer timer;
    public Squad squad;

    public int indexInSquad;

    public Transform target;
    Vector3 lastTargetPosition;

    public bool Alive { get; private set; } = true;
    public event System.Action<int> OnDie;

    new Transform camera;
    void Start()
    {
        camera = Camera.main.transform;

        name = $"Soldier({squad.teamID}:{squad.squadID}:{indexInSquad})";

        renderer.material.color = settings.teams[squad.teamID].color;
    }

    void FixedUpdate()
    {
        if (!timer.Active || !target || !Alive) return;

        if (lastTargetPosition != target.position)
        {
            PlaySound(sounds.BattleCry());
            lastTargetPosition = target.position;
        }

        var toTarget = Vector3.Scale((target.position - transform.position), Y_MASK);
        if (toTarget.magnitude < 1f) return;
        toTarget.Normalize();

        billboard.flipped = camera.InverseTransformDirection(toTarget).x > 0f;

        var deltaVelocity = Mathf.Clamp(SPEED - rigidbody.velocity.magnitude, 0f, SPEED);
        rigidbody.AddForce(toTarget * deltaVelocity, ForceMode.VelocityChange);
    }

    public void Die()
    {
        Alive = false;

        name = "(D) " + name;

        var color = renderer.material.color;
        renderer.material.color = settings.teams[squad.teamID].deadColor;

        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<Collider>());

        OnDie(indexInSquad);
    }

    void PlaySound(AudioClip sound, bool vocal = false)
    {
        if (indexInSquad != 0 && Random.Range(0, 10) != 0) return;

        if (vocal)
            audio.pitch = Random.Range(1 - AUDIO_PITCH_HALF_RANGE, 1 + AUDIO_PITCH_HALF_RANGE);
        else
            audio.pitch = 1f;

        audio.PlayOneShot(sound);
    }
}
