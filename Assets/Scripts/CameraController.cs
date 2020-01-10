using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera mainCamera;

    public float zoomSpeed = 10;
    public float speed = 2;

    public void Start()
    {
        mainCamera = Camera.main;
        var screenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        mainCamera.transform.position = new Vector3(screenSize.x / 2, screenSize.y / 2, mainCamera.transform.position.z);
    }

    public void Update()
    {
        var direction = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector2.up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector2.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector2.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector2.down;
        }

        if (direction != Vector2.zero)
        {
            mainCamera.transform.position += new Vector3(direction.x * Time.deltaTime * speed * mainCamera.orthographicSize, direction.y * Time.deltaTime * speed * mainCamera.orthographicSize, 0);
        }

        if (Input.mouseScrollDelta != Vector2.zero)
        {
            mainCamera.orthographicSize += Input.mouseScrollDelta.y * Time.deltaTime * zoomSpeed;
        }
    }
}