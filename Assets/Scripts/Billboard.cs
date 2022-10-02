using UnityEngine;

public class Billboard : MonoBehaviour
{
    static Vector2 FLIPPED = new Vector2(-1, 1);

    public bool flipped = false;

    new Transform camera;

    void Start()
    {
        camera = Camera.main.transform;
    }

    void Update()
    {
        var yRotation = camera.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        GetComponent<Renderer>().material.mainTextureScale = flipped ? FLIPPED : Vector2.one;
    }
}
