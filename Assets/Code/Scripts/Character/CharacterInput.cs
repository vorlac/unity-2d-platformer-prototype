using UnityEngine;

namespace Scripts.Character
{
    public class CharacterInput : MonoBehaviour
    {
        Character character;

        void Start()
        {
            character = GetComponent<Character>();
        }

        void Update()
        {
            // Vector2 dir = Vector2.right;
            // character.SetDirectionalInput(dir);
            // character.Jump();
            // character.StopJumping();
            // character.ShootWeapon();
        }
    }
}