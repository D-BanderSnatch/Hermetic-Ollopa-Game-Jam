using UnityEngine;

using UnityEngine.InputSystem; 

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
      
        float moveX = 0f;

       
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveX = 1f;
            }
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveX = -1f;
            }
        }

        if (Gamepad.current != null)
        {
          
            float gamepadX = Gamepad.current.leftStick.x.ReadValue();
            
          
            if (Mathf.Abs(gamepadX) > 0.1f) 
            {
                moveX = gamepadX;
            }
        }

      
        Vector3 move = new Vector3(moveX, 0, 0) * speed * Time.deltaTime;
        controller.Move(move);
    }
}
