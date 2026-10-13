using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>People turn to face something rather than snapping round to it.</summary>
    public static class ClinicTurn
    {
        /// <summary>Degrees per second: a corner on a route takes a fraction of a second, a turn to face someone a little longer.</summary>
        public const float DegreesPerSecond=540f;
        /// <summary>The rotation after turning from <paramref name="current"/> towards <paramref name="direction"/> (floor plane)
        /// for <paramref name="seconds"/>. A zero direction keeps the current heading; a jump (<paramref name="snap"/>) arrives at once.</summary>
        public static Quaternion Step(Quaternion current,Vector3 direction,float seconds,bool snap)
        {
            direction.y=0;
            if(direction.sqrMagnitude<.0001f)return current;
            var target=Quaternion.LookRotation(direction.normalized,Vector3.up);
            if(snap)return target;
            return Quaternion.RotateTowards(current,target,DegreesPerSecond*Mathf.Max(0,seconds));
        }
    }

    /// <summary>Two-bone reach (shoulder, elbow, hand) so an arm can close round a held object.</summary>
    public static class ClinicTwoBone
    {
        /// <summary>Where the elbow goes so the hand is at <paramref name="target"/> (or as near as the arm reaches, in a straight
        /// line towards it). <paramref name="pole"/> is the direction the elbow bends towards.</summary>
        public static Vector3 Elbow(Vector3 shoulder,float upper,float fore,Vector3 target,Vector3 pole,out Vector3 hand)
        {
            var toTarget=target-shoulder;float distance=toTarget.magnitude;
            var axis=distance<.0001f?Vector3.forward:toTarget/distance;
            float reach=Mathf.Clamp(distance,Mathf.Abs(upper-fore)+.001f,upper+fore-.001f);
            float along=(upper*upper-fore*fore+reach*reach)/(2*reach);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            var bend=pole-Vector3.Dot(pole,axis)*axis;
            bend=bend.sqrMagnitude<.0001f?Vector3.Cross(axis,Vector3.up).normalized:bend.normalized;
            hand=shoulder+axis*reach;
            return shoulder+axis*along+bend*height;
        }
    }
}
