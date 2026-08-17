using UnityEngine;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// Ensures the blocky player visual exists without duplicating hierarchy or components.
    /// </summary>
    public static class PlayerVisualBootstrap
    {
        public static Transform EnsurePlayerVisual(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return null;
            }

            // Ensure PlayerViewController exists on Player root
            var viewCtrl = playerRoot.GetComponent<PlayerViewController>();
            if (viewCtrl == null)
            {
                viewCtrl = playerRoot.gameObject.AddComponent<PlayerViewController>();
            }

            Transform existingVisual;
            bool existingFound = PlayerVisualBuilder.TryGetExisting(playerRoot, out existingVisual);
            bool existingValid = existingFound && PlayerVisualBuilder.IsSkinModelValid(existingVisual);

            Debug.Log($"[PlayerVisual] Existing found: {existingFound}");
            Debug.Log($"[PlayerVisual] Existing valid: {existingValid}");

            if (existingFound && existingValid)
            {
                // Fix transform for PlayerVisual created by older code versions but has valid meshes
                existingVisual.localPosition = Vector3.zero;
                existingVisual.localRotation = Quaternion.identity;
                existingVisual.localScale = Vector3.one * 0.9f;

                EnsureController(existingVisual);
                LogDiagnostics(existingVisual);
                return existingVisual;
            }

            // Build replacement
            Transform replacement = null;
            bool replacementValid = false;
            try
            {
                replacement = PlayerVisualBuilder.Build(playerRoot, PlayerModelType.SteveWide);
                if (replacement != null)
                {
                    replacement.gameObject.name = "PlayerVisual_Temp";
                    replacementValid = PlayerVisualBuilder.IsSkinModelValid(replacement);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            Debug.Log($"[PlayerVisual] Replacement built: {replacement != null}");
            Debug.Log($"[PlayerVisual] Replacement valid: {replacementValid}");

            if (replacementValid)
            {
                if (existingFound && existingVisual != null)
                {
                    if (Application.isPlaying) Object.Destroy(existingVisual.gameObject);
                    else Object.DestroyImmediate(existingVisual.gameObject);
                }
                
                replacement.gameObject.name = "PlayerVisual";
                EnsureController(replacement);
                LogDiagnostics(replacement);
                return replacement;
            }
            else
            {
                Debug.LogError("[PlayerVisualBootstrap] Replacement visual is invalid; keeping existing PlayerVisual.");
                if (replacement != null)
                {
                    if (Application.isPlaying) Object.Destroy(replacement.gameObject);
                    else Object.DestroyImmediate(replacement.gameObject);
                }

                if (existingFound && existingVisual != null)
                {
                    EnsureController(existingVisual);
                    LogDiagnostics(existingVisual);
                    return existingVisual;
                }
                
                return null;
            }
        }

        private static void LogDiagnostics(Transform visualRoot)
        {
            if (visualRoot == null) return;
            Debug.Log($"[PlayerVisual] Root active: {visualRoot.gameObject.activeSelf}");
            var renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(false);
            Debug.Log($"[PlayerVisual] Renderer count: {renderers.Length}");
        }

        private static void EnsureController(Transform visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            var controller = visualRoot.GetComponent<PlayerVisualController>();
            if (controller == null)
            {
                controller = visualRoot.gameObject.AddComponent<PlayerVisualController>();
            }

            // Override any old serialized hideHeadInFirstPerson = true
            controller.ResetHeadVisibility();
            controller.ApplyModelSettings();

            // Ensure PlayerLocomotionAnimator is attached for movement animations
            if (visualRoot.GetComponent<PlayerLocomotionAnimator>() == null)
            {
                visualRoot.gameObject.AddComponent<PlayerLocomotionAnimator>();
            }
        }
    }
}
