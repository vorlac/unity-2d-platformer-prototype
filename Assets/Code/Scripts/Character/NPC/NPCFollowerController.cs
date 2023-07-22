using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.NPC
{
    using Core.Pathfinding;
    using Scripts.Character;
    using Scripts.Controller;

    [RequireComponent(typeof(PathfindingGraphGenerator))]
    public class NPCFollowerController : MonoBehaviour
    {
        float speed = 20f;

        public GameObject player;
        Character playerScript;
        Controller2D controller;
        Controller2D playerController;

        Queue<Tuple<Vector3, float>> movementQueue = new Queue<Tuple<Vector3, float>>();
        float followDelay = 1.0f;  // move x seconds behond target

        public void Start()
        {
            controller = GetComponent<Controller2D>();
            controller.CalculateRaySpacing();

            playerController = player.GetComponent<Controller2D>();
            playerScript = player.GetComponent<Character>();

            followDelay = 1.0f;
        }

        public void Update()
        {
            if (movementQueue.Count > 0)
            {
                var now = Time.time;
                var front = movementQueue.Peek();
                while (now - front.Item2 >= followDelay)
                {
                    Vector3 playerPosition = new Vector3(front.Item1.x, front.Item1.y);
                    Vector3 currPosition = new Vector3(transform.position.x, transform.position.y);
                    Vector3 delta = playerPosition - currPosition;

                    delta.y -= playerScript.gravity * Time.deltaTime;

                    controller.Move(
                        delta * speed * Time.deltaTime,
                        new Vector3(delta.x > 0 ? 1 : delta.x < 0 ? -1 : 0,
                                    delta.y > 0 ? 1 : delta.y < 0 ? -1 : 0, 0)
                    );

                    movementQueue.Dequeue();
                    front = movementQueue.Peek();
                }
            }
        }

        public void FixedUpdate()
        {
            float positionTime = Time.time;
            Vector3 playerPosition = new Vector3(
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z);

            movementQueue.Enqueue(new Tuple<Vector3, float>(playerPosition, positionTime));
        }
    }
}