using UnityEngine;

namespace Scripts.Character
{
    using Scripts.Player;

    public class NPCFollowerInput : MonoBehaviour
    {
        Character character;
        GameObject target;

        void Start()
        {
            character = GetComponent<Character>();
            target = GameObject.FindGameObjectWithTag("Player");
        }

        void Update()
        {
            //Vector2.MoveTowards(character.transform.position, target.transform.position, 5f);
            // Vector2 dir = Vector2.right;
            // character.SetDirectionalInput(dir);
            // character.Jump();
            // character.StopJumping();
            // character.ShootWeapon();
        }
    }
}