using System;
using System.Collections.Generic;

namespace Scripts.Character
{
    using UnityEngine;
    using Core.Geom;

    public class NPCFollower : Character 
    {
        public Rect2 GetJumpArcBoundingRect(Vector2 input, Rect2 objLocation = new Rect2(), float jumpForcePercent = 1f, int samples = 25, float interval = 0.05f)
        {
            Rect2 jumpRect = Rect2.Empty;

            var jumpPositions = GetJumpArc(input, objLocation, jumpForcePercent, samples, interval);
            foreach (var pos in jumpPositions)
                jumpRect.ExpandTo(pos);

            return jumpRect;
        }

        public Rect2 GetFallArcBoundingRect(Vector2 input, Rect2 objLocation = new Rect2(), int samples = 25, float interval = 0.05f)
        {
            Rect2 fallRect = Rect2.Empty;

            var jumpPositions = GetFallArc(input, objLocation, samples, interval);
            foreach (var pos in jumpPositions)
                fallRect.ExpandTo(pos);

            return fallRect;
        }

        public List<Rect2> GetFallArc(Vector2 input, Rect2 objLocation = new Rect2(), int samples = 25, float interval = 0.05f)
        {
            List<Rect2> locations = new List<Rect2>();

            Rect2 rect = objLocation;
            if (rect.IsEmpty)
            {
                var colliderBounds = this.controller.boxCollider.bounds;
                rect = Rect2.FromBounds(colliderBounds);
            }

            var currVelocity = new Vector2(this.velocity.x, this.velocity.y);
            for (int timestep = 0; timestep < samples; timestep++)
            {
                var elapsedTime = (interval * timestep);
                var charVelocity = GetFallVelocity(input, currVelocity, elapsedTime);
                var predPosition = Rect2.Offset(rect, charVelocity * elapsedTime);
                locations.Add(predPosition);
            }

            return locations;
        }

        public List<Rect2> GetJumpArc(Vector2 input, Rect2 objLocation = new Rect2(), float jumpForcePercent = 0.98f, int samples = 25, float interval = 0.05f)
        {
            List<Rect2> locations = new List<Rect2>();

            Rect2 rect = objLocation;
            if (rect.IsEmpty)
            {
                var colliderBounds = this.controller.boxCollider.bounds;
                rect = Rect2.FromBounds(colliderBounds);
            }

            var currVelocity = new Vector2(this.velocity.x, this.velocity.y);
            for (int timestep = 0; timestep < samples; timestep++)
            {
                var elapsedTime = (interval * timestep);
                var charVelocity = GetJumpVelocity(input, currVelocity, elapsedTime, jumpForcePercent);
                var predPosition = Rect2.Offset(rect, charVelocity * elapsedTime);

                locations.Add(predPosition);
            }

            return locations;
        }

        private Vector2 GetFallVelocity(Vector2 input, Vector2 calcVelocity, float elapsedTime)
        {
            // update horizontal velocity based on x input
            calcVelocity.x = input.x * walkSpeed;

            // update vertical velocity by appluing gravity
            calcVelocity.y += gravity * elapsedTime * 0.5f;

            return calcVelocity;
        }

        private Vector2 GetJumpVelocity(Vector2 input, Vector2 calcVelocity, float elapsedTime, float jumpForcePercent = 1f)
        {
            // update horizontal velocity based on x input
            calcVelocity.x = input.x * walkSpeed;

            // update velocity by applying the proper jump force
            var jumpVelocity = jumpForcePercent >= 1
                ? maxJumpVelocity
                : Math.Max(minJumpVelocity,
                           maxJumpVelocity * jumpForcePercent);

            bool touchingJumpableSurface = isWallSliding || controller.collisions.below;
            if (touchingJumpableSurface)
            {
                if (isWallSliding)
                {
                    float inputDirectionX =
                        (directionalInput.x < 0) ? -1 :
                        (directionalInput.x > 0) ? 1 : 0;

                    if (inputDirectionX == wallDirection)
                    {
                        calcVelocity.x = -wallDirection * wallJumpVelocityClimb.x;
                        calcVelocity.y = wallJumpVelocityClimb.y;
                    }
                    else if (inputDirectionX == 0)
                    {
                        calcVelocity.x = -wallDirection * wallJumpVelocityOff.x;
                        calcVelocity.y = wallJumpVelocityOff.y;
                    }
                    else
                    {
                        calcVelocity.x = -wallDirection * wallJumpVelocityLeap.x;
                        calcVelocity.y = wallJumpVelocityLeap.y;
                    }
                }

                if (controller.collisions.below)
                {
                    if (controller.collisions.slidingDownMaxSlope)
                    {
                        if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                        {
                            calcVelocity.y = jumpVelocity * controller.collisions.slopeNormal.y;
                            calcVelocity.x = jumpVelocity * controller.collisions.slopeNormal.x;
                        }
                    }
                    else
                    {
                        calcVelocity.y = jumpVelocity;
                    }
                }
            }
            else if (jumpCount < jumpLimit)
            {
                calcVelocity.y = jumpVelocity;
            }

            // update vertical velocity by appluing gravity
            calcVelocity.y += gravity * elapsedTime * 0.5f;

            return calcVelocity;
        }

        private void OnDrawGizmos()
        {
            return;
        }
    }
}