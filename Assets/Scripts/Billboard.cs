using UnityEngine;

public class Billboard : MonoBehaviour
{
    public bool flipped = false;

    new Transform camera;

    void Start()
    {
        camera = Camera.main.transform;
    }

    void Update()
    {
        var yRotation = camera.rotation.eulerAngles.y + (flipped ? 180f : 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}
