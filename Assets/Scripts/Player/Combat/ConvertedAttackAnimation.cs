using System.Collections.Generic;
using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    [System.Serializable]
    public class ConvertedJointTrack
    {
        public string UnityJointName;
        public float[] Times;
        public Vector3[] PositionDeltas;
        public Quaternion[] RotationDeltas;
    }

    [CreateAssetMenu(fileName = "NewConvertedAttackAnimation", menuName = "MineCraft/Combat/Converted Attack Animation")]
    public class ConvertedAttackAnimation : ScriptableObject
    {
        public string AttackId;
        public float TotalDuration;

        // Custom combat metadata mapped from source
        public float HitWindowStart;
        public float HitWindowEnd;
        
        public float ComboWindowStart;
        public float ComboWindowEnd;

        public float MovementMultiplier = 1f;
        public string NextComboAttackId = null;

        public bool IsAirCompatible = false;
        public bool IsGroundCompatible = true;

        [HideInInspector]
        public List<ConvertedJointTrack> Tracks = new List<ConvertedJointTrack>();

        public ConvertedJointTrack GetTrack(string unityJointName)
        {
            for (int i = 0; i < Tracks.Count; i++)
            {
                if (Tracks[i].UnityJointName == unityJointName)
                    return Tracks[i];
            }
            return null;
        }
    }
}
