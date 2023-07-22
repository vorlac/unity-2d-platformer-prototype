using UnityEngine;

namespace Utils
{
    static class Physics
    {
        // v  = final velocity 
        // v0 = initial velocity
        // a  = constant accelleration (gravity)
        // t  = time interval
        // dx = displacement / distance
        //
        // Solve for target velocity (given gravity, time interval, and initial velocity)
        //     v = v0 + (a * t)
        //
        // Solve for distance traveled (given: target velocity, initial velocity, and time interval)
        //     dx = ( (v + v0) / 2 ) * t
        //
        // Solve for distance traveled (given: gravity, initial velocity, and time interval)
        //     dx = v0 * t + 0.5 * a * t
        //
        // Solve for final velocity squared (given initial velocity, accelleration/gravity, and distance traveled)
        //   v^2 = v0^2 + (2 * a * dX)

        static public Vector2 CalculateLaunchData(Vector2 initialPosition, Vector2 targetLocation, float gravity, float jumpHeight)
        {
            float distanceY = targetLocation.y - initialPosition.y;
            Vector3 distanceX = new Vector3(targetLocation.x - initialPosition.x, 0);
            float timeToTarget = Mathf.Sqrt(-2 * jumpHeight / gravity) + Mathf.Sqrt(2 * (distanceY - jumpHeight) / gravity);
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * jumpHeight);
            Vector3 velocityX = distanceX / timeToTarget;
            return velocityX + velocityY * -Mathf.Sign(gravity);
        }

        static public float CalculateGravity(float jumpHeight, float timeToApex)
        {
            return -(2 * jumpHeight) / Mathf.Pow(timeToApex, 2);
        }

        static public float CalculateMaxJumpVelocity(float gravity, float timeToJumpApex)
        {
            return Mathf.Abs(gravity) * timeToJumpApex;
        }

        static public float CalculateMinJumpVelocity(float gravity, float minJumpHeight)
        {
            return Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        }

        static public Vector2 CalculateJumpLocation(Vector2 initialPosition, Vector2 launchVelocity, float gravity, float elapsedTimeSec)
        {
            Vector2 velocity = new Vector2(launchVelocity.x, launchVelocity.y);
            velocity.y += gravity * elapsedTimeSec * 0.5f;
            return initialPosition + (velocity * elapsedTimeSec);
        }

    }
}