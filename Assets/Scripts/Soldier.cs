using UnityEngine;

public class Soldier : MonoBehaviour
{
    const float SPEED = 2f;
    Vector3 Y_MASK = new Vector3(1f, 0f, 1f);

    [SerializeField]
    Transform target;

    [SerializeField]
    new Rigidbody rigidbody;

    [SerializeField]
    Billboard billboard;

    new Transform camera;
    void Start()
    {
        camera = Camera.main.transform;
    }

    void FixedUpdate()
    {
        var toTarget = Vector3.Scale((target.position - transform.position), Y_MASK);
        if (toTarget.magnitude < 1f) return;
        toTarget.Normalize();

        billboard.flipped = camera.InverseTransformDirection(toTarget).x > 0f;

        var deltaVelocity = SPEED - rigidbody.velocity.magnitude;
        rigidbody.AddForce(toTarget * deltaVelocity, ForceMode.VelocityChange);
    }
}
