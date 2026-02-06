using UnityEngine;

public class CameraControls : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float boostMultiplier = 5f;

    [Header("Look")]
    public float lookSensitivity = 2f;
    public bool lockCursor = true;

    float yaw;
    float pitch;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void Update()
    {
        // Toggle cursor lock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // Mouse look
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Movement (WASD + QE)
        float boost = Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f;
        float speed = moveSpeed * boost;

        Vector3 input =
            new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        float upDown = 0f;
        if (Input.GetKey(KeyCode.E)) upDown += 1f;
        if (Input.GetKey(KeyCode.Q)) upDown -= 1f;

        Vector3 move = (transform.right * input.x) + (transform.forward * input.z) + (transform.up * upDown);
        transform.position += move * speed * Time.deltaTime;
    }
}
