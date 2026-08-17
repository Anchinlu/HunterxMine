using UnityEngine;

namespace MineCraftUnity.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerLevelSystem : MonoBehaviour
    {
        [SerializeField] private int baseXp = 100;
        [SerializeField] private int xpPerLevel = 50;
        [SerializeField] private int statPointsPerLevel = 3;

        private PlayerStats _stats;
        private CharacterClassDefinition _currentDefinition;

        public int Level => _stats != null ? _stats.Level : 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel => baseXp + (Level - 1) * xpPerLevel;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        public void Initialize(CharacterClassDefinition classDef)
        {
            _currentDefinition = classDef;
            _stats.ApplyClass(classDef, refillResources: true);
            SyncToStats();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || _currentDefinition == null) return;

            Experience += amount;
            bool leveledUp = false;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                _stats.Level++;
                _stats.AddStatPoints(statPointsPerLevel);
                leveledUp = true;
            }

            if (leveledUp)
            {
                // Level up recalculates stats, preserving current resources percentage or simply refilling
                // For simplicity as requested, we can refill or just clamp. Let's do refill for level up.
                _stats.ApplyClass(_currentDefinition, refillResources: true);
            }

            // Always sync XP progress to PlayerStats so HUD updates immediately.
            SyncToStats();
        }

        /// <summary>
        /// Synchronizes internal XP state to PlayerStats legacy XP fields so the HUD
        /// (which reads ExperienceProgress and ExperienceLevel) stays up-to-date.
        /// </summary>
        private void SyncToStats()
        {
            _stats.ExperienceLevel = _stats.Level;
            var toNext = ExperienceToNextLevel;
            _stats.ExperienceProgress = toNext > 0
                ? (float)Experience / toNext
                : 0f;
        }
    }
}
