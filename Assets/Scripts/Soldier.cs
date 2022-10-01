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

    public int teamID;
    public int squadID;
    public int indexInSquad;

    new Transform camera;
    void Start()
    {
        camera = Camera.main.transform;

        name = $"Soldier({teamID}:{squadID}:{indexInSquad})";

        var material = renderer.material;
        material.color = settings.teams[teamID].color;
    }

    void FixedUpdate()
    {
        if (!timer.Active) return;
        else if (!target) return;

        var toTarget = Vector3.Scale((target.position - transform.position), Y_MASK);
        if (toTarget.magnitude < 1f) return;
        toTarget.Normalize();

        billboard.flipped = camera.InverseTransformDirection(toTarget).x > 0f;

        var deltaVelocity = Mathf.Clamp(SPEED - rigidbody.velocity.magnitude, 0f, SPEED);
        rigidbody.AddForce(toTarget * deltaVelocity, ForceMode.VelocityChange);
    }
}
