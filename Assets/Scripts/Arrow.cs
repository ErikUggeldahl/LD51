using UnityEngine;

public class Arrow : MonoBehaviour
{
    const float DESTROY_DELAY = 15;
    const int ARROW_KILL_CHANCE = 3;

    public Vector3 desiredPosition;
    public Color color;

    public float d_initialForce;
    public float d_predictedDistance;
    Vector3 d_initialPosition;

    new Rigidbody rigidbody;
    new Collider collider;

    // Values computed from quadratic regression of force to distance
    const float A = -0.00145f;
    const float B = 0.3915f;
    const float C = 6.0750f;

    const float INITIAL_COLLIDER_DISABLE_TIME = 0.3f;
    float lifetime = 0f;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();

        var toTarget = desiredPosition - transform.position;

        transform.parent = Spawner.arrowParent;
        var look = Quaternion.LookRotation(toTarget.normalized).eulerAngles;
        transform.rotation = Quaternion.Euler(new Vector3(-45, look.y, 0));

        var initialForce = DesiredDistanceToForce(toTarget.magnitude);
        rigidbody.AddForce(transform.forward * initialForce, ForceMode.VelocityChange);

        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.color = color;
        }

        d_initialPosition = transform.position;
    }

    float DesiredDistanceToForce(float desiredDistance)
    {
        return A * desiredDistance * desiredDistance + B * desiredDistance + C;
    }

    void Update()
    {
        // To prevent archers from killing themselves
        if (!collider.enabled)
        {
            if (lifetime < INITIAL_COLLIDER_DISABLE_TIME)
            {
                lifetime += Time.deltaTime;
            }
            else
            {
                collider.enabled = true;
            }
        }

        transform.rotation = Quaternion.LookRotation(rigidbody.velocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"Arrow with force {initialForce} travelled {Vector3.Distance(d_initialPosition, transform.position)}m");
        //Debug.Log($"Arrow with desired distance {desiredDistance} travelled {Vector3.Distance(d_initialPosition, transform.position)}");
        var soldier = collision.collider.GetComponent<Soldier>();
        if (soldier && soldier.state != Soldier.State.Dead && Random.Range(0, ARROW_KILL_CHANCE) == 0)
        {
            collision.collider.GetComponent<Soldier>().EnterDeath();

            transform.parent = collision.transform;
            transform.localPosition = Vector3.zero;
        }

        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<Collider>());
        Destroy(this);

        Destroy(gameObject, DESTROY_DELAY);
    }
}
