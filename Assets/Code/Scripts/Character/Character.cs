using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Character
{
    using Core.Geom;
    using Scripts.Controller;
    using Scripts.Weapon;

    [RequireComponent(typeof(Controller2D))]
    public class Character : MonoBehaviour
    {
        [Flags]
        public enum State
        {
            None       = 0b_0000_0000, //   0
            Standing   = 0b_0000_0001, //   1
            Crouching  = 0b_0000_0010, //   2
            Crawling   = 0b_0000_0100, //   4
            Walking    = 0b_0000_1000, //   8
            Running    = 0b_0001_0000, //  16
            Jumping    = 0b_0010_0000, //  32
            Falling    = 0b_0100_0000, //  64

            Traversing = Standing | Walking | Running | Crouching | Crawling
        }

        protected Controller2D controller;
        protected Weapon weapon;

        public bool debugLogging = false;

        public float walkSpeed = 6.0f;
        public float runSpeed = 10.0f;

        public short jumpLimit = 2;
        public float maxJumpHeight = 4.0f;
        public float minJumpHeight = 1.0f;
        public float timeToJumpApex = 0.4f;

        // wall jump forces (neutral, jump away from wall, jump towards wall)
        public Vector2 wallJumpVelocityOff = new Vector2 { x = 8.5f, y = 7.0f };
        public Vector2 wallJumpVelocityLeap = new Vector2 { x = 18.0f, y = 17f };
        public Vector2 wallJumpVelocityClimb = new Vector2 { x = 7.5f, y = 16f };

        public float wallSlideSpeedMax = 3.0f;
        public float wallStickDuration = 0.25f;

        // time to accel to max velocity
        public float accelerationAirborne = 0.2f;
        public float accelerationGrounded = 0.1f;

        // Calculated from jump height and apex time
        public float gravity = 0.0f;

        // Calculated from gravity and jump time
        protected float maxJumpVelocity = 0.0f;

        // Calculated from gravity and min jump height
        protected float minJumpVelocity = 0.0f;

        // Updated based on movement / collision
        protected bool isWallSliding = false;
        protected short faceDirection = 0;
        protected short wallDirection = 0;
        protected short jumpCount = 0;

        // Countdown timer for wall sticking
        protected float wallStickTimer = 0.0f;

        // Smoothing of horizontal movement / input
        // Updated by the Mathf.SmoothDamp function
        protected float velocityXSmoothing = 0.0f;

        protected Vector3 velocity = Vector3.zero;
        protected Vector2 directionalInput = Vector2.zero;

        public Controller2D Controller
        {
            get => this.controller;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                Debug.Log($"Character OnCollisionEnter2D [this:{this.gameObject.name}] [other:{collision.gameObject.name}]");
            }
        }

        public void Start()
        {
            faceDirection = 1;
            controller = GetComponent<Controller2D>();
            weapon = GetComponentInChildren<Weapon>();

            // Update the gravity value based on the
            // jump parameters/constants defined above.
            // Computing gravity (rather than defining
            // it as it's own constant) using these jump
            // parameters allows us to easily reconfigure 
            // how the character jumps (height/apex time)
            CalculateGravity();
        }

        public void Update()
        {
            // Update the characters horizontal and vertical velocity
            // based on the horizontal input values and any forces
            CalculateVelocity();

            // Update the characters horizontal and vertical velocity
            // based on whether or not they are detected to be sliding
            // down a wall. The fall speed can be slowed here, and the
            // character can also be intentionally stuck to the wall 
            // for a short duration when the input direction is facing
            // away from the wall the character is currently sliding down.
            HandleWallSliding();

            // Call into the base 2D controller to update the character's
            // location based on the velocity vectors computed in the 
            // functions called directly above this one.
            controller.Move(velocity * Time.deltaTime, directionalInput);

            // Now that the character has been moved, we'll want to check
            // if they are colliding with anything directly above/below.
            if (controller.collisions.above || controller.collisions.below)
            {
                if (controller.collisions.slidingDownMaxSlope)
                {
                    // If the character is currently in a slide down a slope we'll want to 
                    // adjust the downward velocity to adjust for the slope. The smaller the
                    // slope, the slower the character will slide/descend downwards on it.
                    velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
                }
                else
                {
                    // If a basic upper/lower collision is detected the 
                    // vertical velocity should be set to 0 to help avoid 
                    // any unwanted movement through any ground/ceilings.
                    velocity.y = 0;
                }
            }

            var controllerDir = controller.collisions.faceDir;
            if (faceDirection != controllerDir)
            {
                foreach (Transform child in transform)
                    child.transform.Rotate(0, 180, 0);

                faceDirection = controllerDir;
            }
        }

        public void FixedUpdate()
        {
            // TODO: remove this if/when jump params
            // don't need to be adjusted at runtime
            CalculateGravity();
        }

        void CalculateGravity()
        {
            // used for debugging only by being able to adjust jump 
            // parameters in the editor while the game is running
            var currentGravity = Utils.Physics.CalculateGravity(maxJumpHeight, timeToJumpApex);
            if (currentGravity != gravity)
            {
                gravity = currentGravity;
                maxJumpVelocity = Utils.Physics.CalculateMaxJumpVelocity(gravity, timeToJumpApex);
                minJumpVelocity = Utils.Physics.CalculateMinJumpVelocity(gravity, minJumpHeight);
            }
        }

        void CalculateVelocity()
        {
            // update horizontal velocity based on x input
            float targetVelocityX = directionalInput.x * walkSpeed;

            // apply slightly different input responsiveness depending 
            // on whether or not the character is touching the ground
            velocity.x = Mathf.SmoothDamp(
                velocity.x, targetVelocityX, ref velocityXSmoothing,
                controller.collisions.below ? accelerationGrounded
                                            : accelerationAirborne
            );

            // update vertical velocity by appluing gravity
            velocity.y += gravity * Time.deltaTime;
        }

        public void SetDirectionalInput(Vector2 input)
        {
            directionalInput = input;
        }

        public void Jump()
        {
            bool touchingJumpableSurface = isWallSliding ||
                                           controller.collisions.below;
            if (touchingJumpableSurface)
            {
                // Reset the jump count since the character is
                // touching a jumpable surface (wall/ground)
                // It's set to 1 instead of 0 to account for
                // the jump that's going to be executed below.
                jumpCount = 1;

                // First check if the character is wall sliding
                // so that the jump can propel off of the wall
                if (isWallSliding)
                {
                    float inputDirectionX = (directionalInput.x < 0) ? -1 :
                                            (directionalInput.x > 0) ? 1 : 0;
                    if (inputDirectionX == wallDirection)
                    {
                        // If the character is jumping towards the wall,
                        // apply the wallJumpClimb velocity values for a smaller jump
                        velocity.x = -wallDirection * wallJumpVelocityClimb.x;
                        velocity.y = wallJumpVelocityClimb.y;
                    }
                    else if (inputDirectionX == 0)
                    {
                        // If the character is neutral jumping off of the wall,
                        // apply the wallJumpOff velocity values for a medium jump
                        velocity.x = -wallDirection * wallJumpVelocityOff.x;
                        velocity.y = wallJumpVelocityOff.y;
                    }
                    else
                    {
                        // If the character is jumping away from the wall,
                        // apply the wallLeap velocity values for a larger jump
                        velocity.x = -wallDirection * wallJumpVelocityLeap.x;
                        velocity.y = wallJumpVelocityLeap.y;
                    }
                }

                // Then check to see if character is touching the ground.
                // This isn't an 'else if' because we may just want to 
                // update one velocity axis while keeping the updated value 
                // for the other axis that might have already been updated above
                if (controller.collisions.below)
                {
                    if (controller.collisions.slidingDownMaxSlope)
                    {
                        // If the character is sliding down a slope check to see if the
                        // character is trying to jump with or against the direction of the slope.
                        if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                        {
                            // If the character is not jumping against max slope, adjust the jump
                            // based on the normal/perpendicular vector of the slope they're on
                            velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                            velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                        }
                    }
                    else
                    {
                        // If the character isn't sliding down a slope,
                        // this is just a basic jump. All we need to 
                        // do is apply the jump velocity upwards.
                        velocity.y = maxJumpVelocity;
                    }
                }
            }
            else
            {
                // If the character isn't touching a jumpable surface,
                // check to see if the character is able to double+ jump
                if (jumpCount < jumpLimit)
                {
                    velocity.y = maxJumpVelocity;
                    ++jumpCount;
                }
            }
        }

        public void StopJumping()
        {
            if (velocity.y > minJumpVelocity)
                velocity.y = minJumpVelocity;
        }

        public void ShootWeapon()
        {
            weapon.Shoot();
        }

        void HandleWallSliding()
        {
            isWallSliding = false;

            // If the character is colliding with a wall, convert 
            // the wall's direction to an integer value so that it 
            // can be consistently compared to the directional input
            wallDirection = controller.collisions.left ? (short)-1 : (short)1;

            // Check to see if the character is colliding with a wall, not touching the ground, and moving towards the ground
            if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
            {
                // If the condition above was met, the character is wall sliding
                isWallSliding = true;

                // Clamp the downward movement speed to the wallSlideSpeedMax
                if (velocity.y < -wallSlideSpeedMax)
                    velocity.y = -wallSlideSpeedMax;

                // Check the timeToWallUnstick value to see if the character 
                // is eligible to jump off of the wall. This value is important 
                // since it will essentially keep the character stuck to the 
                // wall the short period of time where the directional input 
                // is moved away from the wall. A good example of when this 
                // is critical would be during a walljump. If the character 
                // doesn't 'stick' to the wall a bit, they'd be detached from 
                // the wall before the jump button is able to be pressed.
                if (wallStickTimer <= 0)
                {
                    // If we're in here, the timer has expired and any directional 
                    // input being made for the character. If the input is still facing
                    // the wall, the character will just keep wallsliding, but if the input
                    // is in the opposite direction of the wall then the character will 
                    // detach from it. Once this happens, reset the wall stick countdown.
                    wallStickTimer = wallStickDuration;
                }
                else
                {
                    // If we're in here, the wall stick timer hasn't run out yet.
                    // The horizontal input smoothing is temporarily removed so 
                    // it doesn't delay the detection of horizontal input facing 
                    // away from the wall (in case the character is trying to walljump).
                    velocityXSmoothing = 0;
                    velocity.x = 0;

                    if (directionalInput.x != wallDirection && directionalInput.x != 0)
                    {
                        // If the directional input is facing away from the wall 
                        // continue to count the stick timer down by subtacking 
                        // the time delta since the last update.
                        wallStickTimer -= Time.deltaTime;
                    }
                    else
                    {
                        // If the directional input is facing the wall this
                        // timer should be reset to the full duration since 
                        // the character 'stickyness' only applys or matters 
                        // when the opposite direction is being inputted.
                        wallStickTimer = wallStickDuration;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (debugLogging)
            {
                // Handle collisions with any other objects.
                // IsTrigger must be set on the Collider object 
                // for this to be invoked on object collisions.
                Debug.Log($"{this.name} Collision: {collision.name}");
            }
        }

        private List<Rect2> GetPredictedFallArc(int samples = 25, float interval = 0.05f)
        {
            return GetPredictedJumpArc(samples, interval, 0.1f);
        }

        private List<Rect2> GetPredictedJumpArc(int samples = 25, float interval = 0.05f, float jumpForcePercent = 1f)
        {
            List<Rect2> locations = new List<Rect2>();

            var currXSmoothing = this.velocityXSmoothing;
            var colliderBounds = this.controller.boxCollider.bounds;
            var currVelocity = new Vector2(this.velocity.x, this.velocity.y);
            var characterRect = Rect2.FromBounds(colliderBounds);

            for (int timestep = 0; timestep < samples; timestep++)
            {
                var elapsedTime = (interval * timestep);
                var charVelocity = CalculateJumpVelocity(currVelocity, ref currXSmoothing, elapsedTime, jumpForcePercent);
                var predPosition = Rect2.Offset(characterRect, charVelocity * elapsedTime);
                locations.Add(predPosition);
            }

            return locations;
        }

        private Vector2 CalculateJumpVelocity(Vector2 calcVelocity, ref float xSmoothing, float elapsedTime, float jumpForcePercent = 1.0f)
        {
            // update horizontal velocity based on x input
            float targetVelocityX = this.directionalInput.x * walkSpeed;

            calcVelocity.x = Mathf.SmoothDamp(
                calcVelocity.x, targetVelocityX, ref xSmoothing,
                this.controller.collisions.below ?
                    this.accelerationGrounded
                  : this.accelerationAirborne);

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
            if (controller == null)
                return;

            var locationsFull = GetPredictedJumpArc(25, 0.05f);
            foreach (var loc in locationsFull)
                Utils.Debug.DrawRectangle(loc, Color.cyan, 2);
        }
    }
}