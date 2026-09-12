using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float normalSpeed = 15f;
    public float fastSpeed = 40f; // Shift'e basınca hızlanma

    [Header("Fare Hassasiyeti")]
    public float lookSensitivity = 0.2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void OnEnable()
    {
        // Kamera ilk açıldığında mevcut açısını hafızaya al
        rotationX = transform.localEulerAngles.x;
        rotationY = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        // --- 1. ETRAFA BAKMA (Sağ Fare Tuşuna Basılı Tutarak) ---
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationY += mouseDelta.x * lookSensitivity;
            rotationX -= mouseDelta.y * lookSensitivity;

            // Yukarı/aşağı bakış açısını -90 ve 90 derece ile sınırla (boyun kırılmasını engeller)
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        // --- 2. HAREKET ETME (WASD + Q/E) ---
        float currentSpeed = Keyboard.current.shiftKey.isPressed ? fastSpeed : normalSpeed;
        Vector3 moveDirection = Vector3.zero;

        // İleri, Geri, Sağa, Sola
        if (Keyboard.current.wKey.isPressed) moveDirection += transform.forward;
        if (Keyboard.current.sKey.isPressed) moveDirection -= transform.forward;
        if (Keyboard.current.dKey.isPressed) moveDirection += transform.right;
        if (Keyboard.current.aKey.isPressed) moveDirection -= transform.right;

        // Yukarı (E) ve Aşağı (Q)
        if (Keyboard.current.eKey.isPressed) moveDirection += Vector3.up;
        if (Keyboard.current.qKey.isPressed) moveDirection -= Vector3.up;

        // Hareketi uygula
        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }
}