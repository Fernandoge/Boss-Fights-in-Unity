using System;
using UnityEngine;

namespace Characters.Axeman
{
    // One attack or skill of the Axeman, tuned in the Inspector. Times are seconds of the animation clip, so they stay valid when Speed changes:
    // the move starts at Clip Start (which skips the clip's wind-up), strikes at Hit Time, lets the next input cancel it from Cancel Time, and ends by itself at End Time.
    // The Mixamo axe clips turn the whole body (some a full spin), so the clip's own turning is played and the character is aimed so the strike ends up at the cursor.
    [Serializable]
    public class AxeMove
    {
        public string Name;
        public string State;
        public float ClipStart;
        public float Speed = 1.8f;
        public float HitTime;
        public float CancelTime;
        public float EndTime;
        public bool TurnsToMouse = true;
        // The move slides toward the cursor without turning the body to it (a sidestep or a back step)
        public bool KeepsFacing;

        [Header("Body")]
        // How many degrees the clip turns the body between Clip Start and Hit Time (measured from the clip, + is clockwise seen from above); used to aim the strike at the cursor
        public float YawToHit;

        [Header("Strike")]
        public int Damage;
        public float Radius = 3f;
        [Range(0f, 360f)] public float Arc = 360f;
        public float HitForwardOffset;
        public bool CountersBoss;
        public float CameraPunch;
        // Skips the impact particles on the target (the basic attack hits too often for them)
        public bool NoImpactEffect;

        [Header("Movement")]
        // A lunge slides the character toward the cursor with an ease-out, for moves whose clip stands still
        public float LungeDistance;
        public bool LungeToMouse;
        public float LungeMinDistance;
        public float LungeEndTime;
        public bool StopsAtEnemy = true;
        public float InvulnerableTime;
        // Degrees the whole body pitches forward during the move (a shoulder charge): it leans in quickly, holds, and stands up again before the move ends
        public float LeanAngle;
        // Moves whose clip really travels (the leap and the dash) play the clip's own motion, scaled so it covers Lunge Distance instead of Clip Travel
        public bool UsesClipMotion;
        public float ClipTravel;
        // How much the clip's rise follows the scaling of its travel: 0 keeps the clip's own height, 1 scales the height like the distance (a longer leap rises higher)
        [Range(0f, 1f)] public float ArcHeightFactor;
        // Multiplies the rise on top of that, to make a jump flatter (below 1) or higher (above 1) without touching its distance
        public float ArcHeightScale = 1f;

        [Header("Blade Trail")]
        public float TrailStartTime;
        public float TrailEndTime;
        // The strike is made by the left hand's axe (a mirrored clip), so the trail shows on that axe
        public bool UsesLeftAxe;

        [Header("Effect")]
        public GameObject EffectPrefab;
        public Vector3 EffectOffset = new Vector3(0f, 0.1f, 0f);
        public float EffectScale = 1f;
        public float EffectRoll;
        // A ring bursting across the floor at the strike, as wide as Radius, so a heavy landing reads as an area hit (0 = no splash)
        public float SplashTime;
        public Color SplashColor = new Color(1f, 0.78f, 0.38f, 1f);

        [NonSerialized] private int _stateHash;

        public int StateHash => _stateHash != 0 ? _stateHash : _stateHash = Animator.StringToHash(State);

        public float GetRealTime(float clipTime, float speed) => Mathf.Max(0f, clipTime - ClipStart) / speed;
    }
}
