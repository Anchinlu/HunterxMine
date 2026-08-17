using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    public struct AttackHitEvent
    {
        public GameObject Attacker;
        public GameObject Target;
        public string AttackId;
        public float Damage;
        public Vector3 HitPoint;
        public Vector3 KnockbackDirection;
    }
}
