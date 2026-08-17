using System.Collections.Generic;
using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    public class AttackAnimationDefinition
    {
        public string Id;
        public float TotalDuration;
        public Dictionary<string, AttackJointKeyframe> Joints;

        // Custom combat metadata (set manually after loading from JSON data)
        public float HitWindowStart;
        public float HitWindowEnd;
        
        public float ComboWindowStart;
        public float ComboWindowEnd;

        public float MovementMultiplier = 1f;
        public string NextComboAttackId = null;

        public bool IsAirCompatible = false;
        public bool IsGroundCompatible = true;
    }
}
