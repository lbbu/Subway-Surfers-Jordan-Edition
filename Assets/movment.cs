using UnityEngine;
using UnityEngine.InputSystem;

public class movment : MonoBehaviour
{
    Vector3 movement = Vector3.zero;
    int speed = 2;

    void Update()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            movement += Vector3.forward;
        }

        transform.position += movement * speed * Time.deltaTime;
    }
}