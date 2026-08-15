using UnityEngine;
using UnityEngine.UI;
using MineCraftUnity.Player;

namespace MineCraftUnity.UI
{
    public class PlayerHud : MonoBehaviour
    {
        private HudSpriteLibrary _library;
        private PlayerStats _stats;
        private PlayerInventory _inventory;

        // ──────────────────────────────────────────────
        //  Kéo thả toàn bộ UI đã vẽ sẵn vào đây
        // ──────────────────────────────────────────────

        [Header("Crosshair (Kéo thả Image crosshair)")]
        [SerializeField] private Image crosshairImage;

        [Header("Hotbar (Kéo thả các element hotbar)")]
        [SerializeField] private RectTransform hotbarSelectionRect;
        [Tooltip("Khoảng cách pixel giữa các slot (đã tính scale). Mặc định = 20 * 3 = 60.")]
        [SerializeField] private float slotWidth = 60f;

        [Header("XP Bar (Kéo thả Image fill của thanh XP)")]
        [SerializeField] private Image xpProgressFill;

        [Header("Food Icons (Kéo 10 Image đồ ăn, từ trái sang phải)")]
        [SerializeField] private Image[] foodIcons = new Image[10];

        [Header("Armor Icons (Kéo 10 Image giáp, từ trái sang phải)")]
        [SerializeField] private Image[] armorIcons = new Image[10];

        [Header("Resource Bars (Kéo Image fill cho HP / Mana / Stamina)")]
        [SerializeField] private Image customHpFill;
        [SerializeField] private Text customHpText;
        [SerializeField] private Image customManaFill;
        [SerializeField] private Text customManaText;
        [SerializeField] private Image customStaminaFill;
        [SerializeField] private Text customStaminaText;

        [Header("Auto-Generate Settings (Chỉ dùng khi KHÔNG gán bằng tay)")]
        [SerializeField] private Vector2 resourceBarsPosition = new Vector2(20f, -20f);
        [SerializeField] private Vector2 healthBarSize = new Vector2(360f, 34f);
        [SerializeField] private Vector2 manaBarSize = new Vector2(320f, 30f);
        [SerializeField] private Vector2 staminaBarSize = new Vector2(280f, 26f);
        [SerializeField] private float healthBarRotation = 8f;
        [SerializeField] private float manaBarRotation = 8f;
        [SerializeField] private float staminaBarRotation = 8f;
        [SerializeField] private float manaVerticalOffset = -42f;
        [SerializeField] private float staminaVerticalOffset = -80f;
        [SerializeField] private Vector2 shadowOffset = new Vector2(4f, -4f);
        [SerializeField] private Color healthColor = Color.red;
        [SerializeField] private Color manaColor = Color.blue;
        [SerializeField] private Color staminaColor = Color.white;
        [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.2f);

        // Runtime references (filled from SerializeField or auto-generated)
        private Image _hpFill;
        private Image _manaFill;
        private Image _staminaFill;
        private Image[] _foodImages;
        private Image[] _armorImages;
        private Image _xpProgressImage;
        private RectTransform _hotbarSelection;
        private Vector2 _hotbarSelectionBasePos;

        private const float UIScale = 3f;

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────

        public void Initialize(HudSpriteLibrary library, PlayerStats stats, PlayerInventory inventory)
        {
            _library = library;
            _stats = stats;
            _inventory = inventory;

            BuildUI();

            _stats.StatsChanged += UpdateStatsUI;
            _inventory.InventoryChanged += UpdateInventoryUI;

            UpdateStatsUI();
            UpdateInventoryUI();
        }

        private void OnDestroy()
        {
            if (_stats != null) _stats.StatsChanged -= UpdateStatsUI;
            if (_inventory != null) _inventory.InventoryChanged -= UpdateInventoryUI;
        }

        // ──────────────────────────────────────────────
        //  Build: chỉ tạo những gì chưa được gán
        // ──────────────────────────────────────────────

        private void BuildUI()
        {
            // Ensure Canvas infrastructure
            EnsureCanvas();

            // --- Crosshair ---
            if (crosshairImage == null && _library.Crosshair != null)
            {
                var go = AutoCreateImage("Crosshair", transform, _library.Crosshair,
                    new Vector2(0.5f, 0.5f), Vector2.zero);
                crosshairImage = go.GetComponent<Image>();
            }

            // --- Hotbar ---
            if (hotbarSelectionRect == null && _library.Hotbar != null)
            {
                var hotbarRoot = new GameObject("HotbarRoot");
                var hotbarRect = hotbarRoot.AddComponent<RectTransform>();
                hotbarRect.SetParent(transform, false);
                hotbarRect.anchorMin = new Vector2(0.5f, 0f);
                hotbarRect.anchorMax = new Vector2(0.5f, 0f);
                hotbarRect.pivot = new Vector2(0.5f, 0f);
                hotbarRect.anchoredPosition = new Vector2(0, 10);

                AutoCreateImage("HotbarBackground", hotbarRect, _library.Hotbar,
                    new Vector2(0.5f, 0f), Vector2.zero);

                var selObj = AutoCreateImage("HotbarSelection", hotbarRect, _library.HotbarSelection,
                    new Vector2(0.5f, 0f), Vector2.zero);
                hotbarSelectionRect = selObj.GetComponent<RectTransform>();
            }
            _hotbarSelection = hotbarSelectionRect;
            if (_hotbarSelection != null)
                _hotbarSelectionBasePos = _hotbarSelection.anchoredPosition;

            // --- XP Bar ---
            if (xpProgressFill == null && _library.ExperienceBarBackground != null)
            {
                var xpBg = AutoCreateImage("XPBackground", transform, _library.ExperienceBarBackground,
                    new Vector2(0.5f, 0f), new Vector2(0, 10 + 22 * UIScale + 5));
                var xpFillObj = AutoCreateImage("XPProgress", xpBg.transform, _library.ExperienceBarProgress,
                    new Vector2(0.5f, 0f), Vector2.zero);
                xpProgressFill = xpFillObj.GetComponent<Image>();
                xpProgressFill.type = Image.Type.Filled;
                xpProgressFill.fillMethod = Image.FillMethod.Horizontal;
                xpProgressFill.fillOrigin = 0;
            }
            _xpProgressImage = xpProgressFill;

            // --- Food ---
            bool hasManualFood = foodIcons != null && foodIcons.Length >= 10 && foodIcons[0] != null;
            if (hasManualFood)
            {
                _foodImages = foodIcons;
            }
            else
            {
                _foodImages = new Image[10];
                float statsY = 10 + 22 * UIScale + 15;
                float centerOffset = 91 * UIScale;
                for (int i = 0; i < 10; i++)
                {
                    var fGo = AutoCreateImage($"Food_{i}", transform, _library.FoodEmpty,
                        new Vector2(0.5f, 0f),
                        new Vector2(centerOffset - (9 - i) * 8 * UIScale - (4 * UIScale), statsY));
                    _foodImages[i] = fGo.GetComponent<Image>();
                }
            }

            // --- Armor ---
            bool hasManualArmor = armorIcons != null && armorIcons.Length >= 10 && armorIcons[0] != null;
            if (hasManualArmor)
            {
                _armorImages = armorIcons;
            }
            else
            {
                _armorImages = new Image[10];
                float statsY = 10 + 22 * UIScale + 15;
                float centerOffset = 91 * UIScale;
                float armorY = statsY + 10 * UIScale;
                for (int i = 0; i < 10; i++)
                {
                    var aGo = AutoCreateImage($"Armor_{i}", transform, _library.ArmorEmpty,
                        new Vector2(0.5f, 0f),
                        new Vector2(-centerOffset + i * 8 * UIScale + (4 * UIScale), armorY));
                    _armorImages[i] = aGo.GetComponent<Image>();
                }
            }

            // --- Resource Bars ---
            BuildResourceBars();
        }

        private void EnsureCanvas()
        {
            if (GetComponent<Canvas>() == null)
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void BuildResourceBars()
        {
            _hpFill = customHpFill;
            _manaFill = customManaFill;
            _staminaFill = customStaminaFill;

            // All assigned manually — nothing to generate
            if (_hpFill != null && _manaFill != null && _staminaFill != null)
                return;

            // Create root for auto-generated bars
            RectTransform rootRect = null;
            var rootObj = new GameObject("ResourceBarsRoot");
            rootRect = rootObj.AddComponent<RectTransform>();
            rootRect.SetParent(transform, false);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = resourceBarsPosition;

            if (_hpFill == null)
                _hpFill = AutoCreateBar("HealthBarRoot", rootRect, healthBarSize, healthColor, healthBarRotation, 0f);
            if (_manaFill == null)
                _manaFill = AutoCreateBar("ManaBarRoot", rootRect, manaBarSize, manaColor, manaBarRotation, manaVerticalOffset);
            if (_staminaFill == null)
                _staminaFill = AutoCreateBar("StaminaBarRoot", rootRect, staminaBarSize, staminaColor, staminaBarRotation, staminaVerticalOffset);
        }

        // ──────────────────────────────────────────────
        //  Auto-generate helpers (fallback khi không gán)
        // ──────────────────────────────────────────────

        private GameObject AutoCreateImage(string name, Transform parent, Sprite sprite, Vector2 pivotAndAnchor, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = pivotAndAnchor;
            rect.anchorMax = pivotAndAnchor;
            rect.pivot = pivotAndAnchor;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.SetNativeSize();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * UIScale, rect.sizeDelta.y * UIScale);
            return go;
        }

        private Image AutoCreateBar(string name, Transform parent, Vector2 size, Color color, float rotation, float yOffset)
        {
            var barRoot = new GameObject(name);
            var barRect = barRoot.AddComponent<RectTransform>();
            barRect.SetParent(parent, false);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(0f, yOffset);
            barRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            barRect.sizeDelta = size;

            AutoCreateSolidRect("Shadow", barRect, size, shadowColor, shadowOffset);
            AutoCreateSolidRect("Background", barRect, size, bgColor, Vector2.zero);
            var fillObj = AutoCreateSolidRect("Fill", barRect, size, color, Vector2.zero);
            var fillImg = fillObj.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            var hlSize = new Vector2(size.x, size.y * 0.25f);
            AutoCreateSolidRect("Highlight", barRect, hlSize, highlightColor, new Vector2(0f, -size.y * 0.1f));
            return fillImg;
        }

        private GameObject AutoCreateSolidRect(string name, RectTransform parent, Vector2 size, Color color, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        // ──────────────────────────────────────────────
        //  Update logic (Event-driven)
        // ──────────────────────────────────────────────

        private void UpdateStatsUI()
        {
            if (_stats == null) return;

            // Resource Bars
            UpdateResourceBars();

            // Food
            if (_foodImages != null && _library != null)
            {
                int foodHalf = Mathf.CeilToInt((float)_stats.FoodLevel / _stats.MaxFoodLevel * 20f);
                for (int i = 0; i < _foodImages.Length; i++)
                {
                    if (_foodImages[i] == null) continue;
                    int foodValue = i * 2;
                    if (foodHalf > foodValue + 1) _foodImages[i].sprite = _library.FoodFull;
                    else if (foodHalf == foodValue + 1) _foodImages[i].sprite = _library.FoodHalf;
                    else _foodImages[i].sprite = _library.FoodEmpty;
                }
            }

            // Armor
            if (_armorImages != null && _library != null)
            {
                int armorHalf = _stats.ArmorValue;
                for (int i = 0; i < _armorImages.Length; i++)
                {
                    if (_armorImages[i] == null) continue;
                    _armorImages[i].gameObject.SetActive(armorHalf > 0);
                    int armorValue = i * 2;
                    if (armorHalf > armorValue + 1) _armorImages[i].sprite = _library.ArmorFull;
                    else if (armorHalf == armorValue + 1) _armorImages[i].sprite = _library.ArmorHalf;
                    else _armorImages[i].sprite = _library.ArmorEmpty;
                }
            }

            // XP
            if (_xpProgressImage != null)
                _xpProgressImage.fillAmount = _stats.ExperienceProgress;
        }

        private void UpdateResourceBars()
        {
            if (_hpFill != null)
                _hpFill.fillAmount = Mathf.Clamp01(_stats.Health / _stats.MaxHealth);
            if (_manaFill != null)
                _manaFill.fillAmount = Mathf.Clamp01(_stats.Mana / _stats.MaxMana);
            if (_staminaFill != null)
                _staminaFill.fillAmount = Mathf.Clamp01(_stats.Stamina / _stats.MaxStamina);

            if (customHpText != null) customHpText.text = $"{Mathf.FloorToInt(_stats.Health)} / {Mathf.FloorToInt(_stats.MaxHealth)}";
            if (customManaText != null) customManaText.text = $"{Mathf.FloorToInt(_stats.Mana)} / {Mathf.FloorToInt(_stats.MaxMana)}";
            if (customStaminaText != null) customStaminaText.text = $"{Mathf.FloorToInt(_stats.Stamina)} / {Mathf.FloorToInt(_stats.MaxStamina)}";
        }

        private void UpdateInventoryUI()
        {
            if (_inventory == null || _hotbarSelection == null) return;

            int selected = _inventory.SelectedHotbarSlot;
            // Dịch chuyển tương đối từ vị trí ban đầu (slot 0) đã kéo tay
            _hotbarSelection.anchoredPosition = _hotbarSelectionBasePos + new Vector2(selected * slotWidth, 0f);
        }
    }
}
