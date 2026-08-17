using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    public interface IDamageable
    {
        void TakeDamage(AttackHitEvent hitEvent);
    }
}
