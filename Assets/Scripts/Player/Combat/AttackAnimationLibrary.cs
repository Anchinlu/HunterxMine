using System.Collections.Generic;

namespace MineCraftUnity.Player.Combat
{
    public static class AttackAnimationLibrary
    {
        private static Dictionary<string, ConvertedAttackAnimation> _animations = new Dictionary<string, ConvertedAttackAnimation>();

        public static ConvertedAttackAnimation Get(string id)
        {
            if (_animations.TryGetValue(id, out var def))
            {
                return def;
            }

            // Try to load from Resources
            string path = $"Combat/ConvertedAnimations/{id}_Converted";
            var anim = UnityEngine.Resources.Load<ConvertedAttackAnimation>(path);
            
            if (anim != null)
            {
                int trackCount = (anim.Tracks != null) ? anim.Tracks.Count : 0;
                UnityEngine.Debug.Log($"[AttackLibrary] Loaded '{id}' from Resources. Tracks={trackCount}, HitWindow={anim.HitWindowStart:F4}~{anim.HitWindowEnd:F4}, Duration={anim.TotalDuration:F4}");
                _animations[id] = anim;
                return anim;
            }
            
            UnityEngine.Debug.LogWarning($"[AttackLibrary] Asset not found at Resources/{path}. Attack '{id}' will have no data.");
            return null;
        }
    }
}
