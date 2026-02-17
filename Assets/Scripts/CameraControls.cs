using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCameraNewInput : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float boostMultiplier = 5f;

    [Header("Look")]
    public float lookSensitivity = 0.1f; // tune this
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
        // Toggle cursor lock with Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // Mouse look
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch -= delta.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (Keyboard.current == null) return;

        // Movement (WASD + QE)
        float boost = Keyboard.current.leftShiftKey.isPressed ? boostMultiplier : 1f;
        float speed = moveSpeed * boost;

        float x = 0f;
        float z = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        float y = 0f;
        if (Keyboard.current.eKey.isPressed) y += 1f;
        if (Keyboard.current.qKey.isPressed) y -= 1f;

        Vector3 move = (transform.right * x) + (transform.forward * z) + (transform.up * y);
        transform.position += move * speed * Time.deltaTime;
    }
}
