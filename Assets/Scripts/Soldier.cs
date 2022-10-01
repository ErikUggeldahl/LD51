using UnityEngine;

public class Soldier : MonoBehaviour
{
    const float SPEED = 2f;
    static Vector3 Y_MASK = new Vector3(1f, 0f, 1f);

    public Transform target;

    [SerializeField]
    new Rigidbody rigidbody;

    [SerializeField]
    Billboard billboard;

    [SerializeField]
    new Renderer renderer;

    public BattleSettings settings;
    public Timer timer;
    public Squad squad;

    public int indexInSquad;

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
}
