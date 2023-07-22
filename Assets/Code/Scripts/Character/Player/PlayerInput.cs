using UnityEngine;

namespace Scripts.Player
{
    using Scripts.Character;

    [RequireComponent(typeof(Character))]
    public class PlayerInput : MonoBehaviour
    {
        /*
         * A Button = Joystick Button 0
         * B Button = Joystick Button 1
         * X Button = Joystick Button 2
         * Y Button = Joystick Button 3
         * Left Bumper  = Joystick Button 4
         * Right Bumper = Joystick Button 5
         * Select / Squares Button = Joystick Button 6
         * Start  / Hamburger Button = Joystick Button 7
         * Left Trigger  =  9th Axis
         * Right Trigger = 10th Axis
         * Left Joystick / Vertical = Y Axis
         * Left Joystick / Horizontal = X Axis
         * Right Joystick / Vertical = 4th Axis
         * Right Joystick / Horizontal = 5th Axis
         * DPad Left/Right = 6th Axis
         * DPad Up/Down = 7th Axis
         */

        Character player;

        void Start()
        {
            player = GetComponent<Character>();
        }

        void Update()
        {
            Vector2 directionalInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            player.SetDirectionalInput(directionalInput);

            if (Input.GetButtonDown("Jump"))
                player.Jump();

            if (Input.GetButtonUp("Jump"))
                player.StopJumping();

            if (Input.GetButtonDown("Fire"))
                player.ShootWeapon();
        }
    }
}
