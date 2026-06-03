using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Prueba3 : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadCaminar = 3.5f;

    [Header("Referencias")]
    public Transform camaraVR;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        float x = 0f;
        float z = 0f;

        // Lee el joystick izquierdo del mando Bluetooth
        if (Gamepad.current != null)
        {
            x = Gamepad.current.leftStick.x.ReadValue();
            z = Gamepad.current.leftStick.y.ReadValue();
        }

        if (camaraVR != null)
        {
            Vector3 direccionCamara = camaraVR.forward;
            direccionCamara.y = 0;
            direccionCamara.Normalize();

            Vector3 direccionDerecha = camaraVR.right;
            direccionDerecha.y = 0;
            direccionDerecha.Normalize();

            Vector3 movimiento = (direccionDerecha * x + direccionCamara * z) * velocidadCaminar;
            rb.linearVelocity = new Vector3(movimiento.x, rb.linearVelocity.y, movimiento.z);
        }
    }
}