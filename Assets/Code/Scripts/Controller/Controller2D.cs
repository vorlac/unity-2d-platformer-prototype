using System;
using UnityEngine;

namespace Scripts.Controller
{
    public class Controller2D : RaycastController
    {
        public bool debugMode = false;

        public float maxSlopeAngle = 80;
        public CollisionInfo collisions;
        public Vector2 playerInput;

        [Serializable]
        public class CollisionInfo
        {
            public bool left;
            public bool right;
            public bool above;
            public bool below;

            public GameObject leftObject = null;
            public GameObject rightObject = null;
            public GameObject aboveObject = null;
            public GameObject belowObject = null;

            public bool climbingSlope;
            public bool descendingSlope;
            public bool slidingDownMaxSlope;

            public float slopeAngle;
            public float slopeAngleOld;

            [SerializeField]
            public Vector2 slopeNormal;

            [SerializeField]
            public Vector2 moveAmountOld;

            public short faceDir;
            public bool fallingThroughPlatform;

            public void Reset()
            {
                left = false;
                right = false;
                above = false;
                below = false;

                leftObject = null;
                rightObject = null;
                aboveObject = null;
                belowObject = null;

                climbingSlope = false;
                descendingSlope = false;
                slidingDownMaxSlope = false;
                slopeNormal = Vector2.zero;
                slopeAngleOld = slopeAngle;
                slopeAngle = 0;
            }
        }

        public override void Start()
        {
            base.Start();
            collisions.faceDir = 1;
        }

        public void Move(Vector2 moveAmount, bool standingOnPlatform)
        {
            Move(moveAmount, Vector2.zero, standingOnPlatform);
        }

        public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
        {
            UpdateRaycastOrigins();

            collisions.Reset();
            collisions.moveAmountOld = moveAmount;

            playerInput = input;

            if (moveAmount.y < 0)
                DescendSlope(ref moveAmount);

            if (moveAmount.x != 0)
                collisions.faceDir = (short) Mathf.Sign(moveAmount.x);

            CheckHorizontalCollisions(ref moveAmount);

            if (moveAmount.y != 0)
                CheckVerticalCollisions(ref moveAmount);

            transform.Translate(moveAmount);

            if (standingOnPlatform)
                collisions.below = true;
        }

        void CheckHorizontalCollisions(ref Vector2 moveAmount)
        {
            float directionX = collisions.faceDir;
            float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

            if (Mathf.Abs(moveAmount.x) < skinWidth)
                rayLength = 2 * skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.botLeft : raycastOrigins.botRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

                if (debugMode)
                    Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

                if (hit)
                {
                    if (hit.distance == 0)
                        continue;

                    float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                    if (i == 0 && slopeAngle <= maxSlopeAngle)
                    {
                        if (collisions.descendingSlope)
                        {
                            collisions.descendingSlope = false;
                            moveAmount = collisions.moveAmountOld;
                        }

                        float distanceToSlopeStart = 0;

                        if (slopeAngle != collisions.slopeAngleOld)
                        {
                            distanceToSlopeStart = hit.distance - skinWidth;
                            moveAmount.x -= distanceToSlopeStart * directionX;
                        }

                        ClimbSlope(ref moveAmount, slopeAngle, hit);
                        
                        moveAmount.x += distanceToSlopeStart * directionX;
                    }

                    if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                    {
                        moveAmount.x = (hit.distance - skinWidth) * directionX;
                        rayLength = hit.distance;

                        if (collisions.climbingSlope)
                        {
                            moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                        }

                        if (directionX == -1)
                        {
                            collisions.left = true;
                            collisions.leftObject = hit.collider.gameObject;
                        }
                        else if (directionX == 1)
                        {
                            collisions.right = true;
                            collisions.rightObject = hit.collider.gameObject;
                        }
                    }
                }
            }
        }

        void CheckVerticalCollisions(ref Vector2 moveAmount)
        {
            float directionY = Mathf.Sign(moveAmount.y);
            float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.botLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

                if (debugMode)
                    Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

                if (hit)
                {
                    if (hit.collider.tag == "Through")
                    {
                        if (directionY == 1 || hit.distance == 0)
                            continue;
                        else if (collisions.fallingThroughPlatform)
                            continue;
                        else if (playerInput.y == -1)
                            collisions.fallingThroughPlatform = true;

                        Invoke("ResetFallingThroughPlatform", .5f);
                        continue;
                    }

                    moveAmount.y = (hit.distance - skinWidth) * directionY;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                        moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);

                    if (directionY == -1)
                    {
                        collisions.below = true;
                        collisions.belowObject = hit.collider.gameObject;
                    }
                    else if (directionY == 1)
                    {
                        collisions.above = true;
                        collisions.aboveObject = hit.collider.gameObject;
                    }
                }
            }

            if (collisions.climbingSlope)
            {
                float directionX = Mathf.Sign(moveAmount.x);
                rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
                Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.botLeft : raycastOrigins.botRight) + Vector2.up * moveAmount.y;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

                if (hit)
                {
                    float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                    if (slopeAngle != collisions.slopeAngle)
                    {
                        moveAmount.x = (hit.distance - skinWidth) * directionX;
                        collisions.slopeAngle = slopeAngle;
                        collisions.slopeNormal = hit.normal;
                    }
                }
            }
        }

        bool ClimbSlope(ref Vector2 moveAmount, float slopeAngle, RaycastHit2D hit)
        {
            Vector2 slopeNormal = hit.normal;
            float moveDistance = Mathf.Abs(moveAmount.x);
            float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

            if (moveAmount.y <= climbmoveAmountY)
            {
                moveAmount.y = climbmoveAmountY;
                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                collisions.below = true;
                collisions.belowObject = hit.collider.gameObject;
                collisions.climbingSlope = true;
                collisions.slopeAngle = slopeAngle;
                collisions.slopeNormal = slopeNormal;
                return true;
            }

            return false;
        }

        void DescendSlope(ref Vector2 moveAmount)
        {
            RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(
                raycastOrigins.botLeft, Vector2.down,
                Mathf.Abs(moveAmount.y) + skinWidth, collisionMask
            );

            RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(
                raycastOrigins.botRight, Vector2.down,
                Mathf.Abs(moveAmount.y) + skinWidth, collisionMask
            );

            if (maxSlopeHitLeft ^ maxSlopeHitRight)
            {
                SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
                SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
            }

            if (!collisions.slidingDownMaxSlope)
            {
                float directionX = Mathf.Sign(moveAmount.x);
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.botRight : raycastOrigins.botLeft;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

                if (hit)
                {
                    float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                    if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                    {
                        if (Mathf.Sign(hit.normal.x) == directionX)
                        {
                            if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                            {
                                float moveDistance = Mathf.Abs(moveAmount.x);
                                float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                                moveAmount.y -= descendmoveAmountY;

                                collisions.slopeAngle = slopeAngle;
                                collisions.descendingSlope = true;
                                collisions.below = true;
                                collisions.slopeNormal = hit.normal;
                            }
                        }
                    }
                }
            }
        }

        void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
        {
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                    collisions.slopeAngle = slopeAngle;
                    collisions.slidingDownMaxSlope = true;
                    collisions.slopeNormal = hit.normal;
                }
            }

        }

        void ResetFallingThroughPlatform()
        {
            collisions.fallingThroughPlatform = false;
        }
    }
}