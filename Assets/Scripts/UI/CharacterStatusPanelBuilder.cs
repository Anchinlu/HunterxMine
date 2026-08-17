using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// Builds the world-space character status panel hierarchy at runtime.
    /// All visual parameters are exposed via SerializeField for Inspector tuning.
    /// The hierarchy is created once; only text values are updated at runtime.
    /// </summary>
    [RequireComponent(typeof(CharacterStatusPanel))]
    public sealed class CharacterStatusPanelBuilder : MonoBehaviour
    {
        [Header("Panel Dimensions")]
        [SerializeField] private Vector2 panelSize = new Vector2(1819.5f, 1215.4f);
        [SerializeField] private Vector3 panelLocalPosition = new Vector3(-0.59f, -1.61f, -1.04f);
        [SerializeField] private Vector3 panelLocalRotation = new Vector3(-40.379f, -174.955f, -168.763f);
        [SerializeField] private Vector3 panelLocalScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        [Header("Colors")]
        [SerializeField] private Color borderColor = new Color(0.098f, 0.749f, 1f, 1f);       // #19BFFF
        [SerializeField] private Color borderHighlight = new Color(0.553f, 0.922f, 1f, 1f);    // #8DEBFF
        [SerializeField] private Color glowColor = new Color(0.031f, 0.482f, 1f, 0.25f);       // #087BFF low alpha
        [SerializeField] private Color interiorTopColor = new Color(0.125f, 0.165f, 0.208f, 0.92f); // #202A35
        [SerializeField] private Color interiorBottomColor = new Color(0.008f, 0.012f, 0.024f, 0.95f); // #020306
        [SerializeField] private Color textColor = new Color(0.949f, 0.969f, 1f, 1f);          // #F2F7FF
        [SerializeField] private Color secondaryTextColor = new Color(0.667f, 0.718f, 0.780f, 1f); // #AAB7C7
        [SerializeField] private Color accentColor = new Color(0.553f, 0.922f, 1f, 1f);        // bright blue

        [Header("Border")]
        [SerializeField] private float borderThickness = 3f;
        [SerializeField] private float glowStrength = 12f;

        [Header("Layout")]
        [SerializeField] private float fontSize = 68f;
        [SerializeField] private float titleFontSize = 84f;
        [SerializeField] private float columnSpacing = 80f;
        [SerializeField] private float rowSpacing = 16f;
        [SerializeField] private float padding = 60f;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset minecraftFont;

        // ─── Label References (populated by Build) ───
        private TextMeshProUGUI _nameLabel;
        private TextMeshProUGUI _levelLabel;
        private TextMeshProUGUI _classLabel;
        private TextMeshProUGUI _hpLabel;
        private TextMeshProUGUI _strLabel;
        private TextMeshProUGUI _defLabel;
        private TextMeshProUGUI _agiLabel;
        private TextMeshProUGUI _dexLabel;
        private TextMeshProUGUI _intLabel;
        private TextMeshProUGUI _mpLabel;
        private TextMeshProUGUI _staLabel;
        private TextMeshProUGUI _pointsLabel;

        private Image _classIcon;
        private System.Collections.Generic.Dictionary<string, Sprite> _classSprites;

        private readonly System.Collections.Generic.Dictionary<MineCraftUnity.Player.CharacterClass, string> _classIconNames = new()
        {
            { MineCraftUnity.Player.CharacterClass.Warrior, "Warrior" },
            { MineCraftUnity.Player.CharacterClass.Archer, "Archer" },
            { MineCraftUnity.Player.CharacterClass.Mage, "Mage" },
            { MineCraftUnity.Player.CharacterClass.HeavyArmor, "HeavyArmor" },
            { MineCraftUnity.Player.CharacterClass.Assassin, "Assassin" }
        };

        private bool _built;

        private void Start()
        {
            if (!_built) Build();
        }

        public void Build()
        {
            if (_built) return;
            _built = true;

            // Check if we are a prefab instance that already has children
            var existingTitle = transform.Find("TitleLabel");
            if (existingTitle != null)
            {
                // Link existing references instead of rebuilding
                _nameLabel = transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
                _levelLabel = transform.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();
                _classLabel = transform.Find("ClassLabel")?.GetComponent<TextMeshProUGUI>();
                _hpLabel = transform.Find("HPLabel")?.GetComponent<TextMeshProUGUI>();
                _strLabel = transform.Find("STRLabel")?.GetComponent<TextMeshProUGUI>();
                _defLabel = transform.Find("DEFLabel")?.GetComponent<TextMeshProUGUI>();
                _agiLabel = transform.Find("AGILabel")?.GetComponent<TextMeshProUGUI>();
                _dexLabel = transform.Find("DEXLabel")?.GetComponent<TextMeshProUGUI>();
                _intLabel = transform.Find("INTLabel")?.GetComponent<TextMeshProUGUI>();
                _mpLabel = transform.Find("MPLabel")?.GetComponent<TextMeshProUGUI>();
                _staLabel = transform.Find("STALabel")?.GetComponent<TextMeshProUGUI>();
                _pointsLabel = transform.Find("PointsLabel")?.GetComponent<TextMeshProUGUI>();
                _classIcon = transform.Find("ClassIcon")?.GetComponent<Image>();
                LoadClassSprites();
                
                // Configure socket if applicable
                bool isPrefabLayout = IsUnderPanelSocket();
                
                if (!isPrefabLayout)
                {
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = panelLocalScale;
                }
                return;
            }

            // Try loading Minecraft font from Resources
            if (minecraftFont == null)
            {
                minecraftFont = Resources.Load<TMP_FontAsset>("UI/Fonts/Minecraft/MinecraftAscii");
            }

            // Setup Canvas (World Space)
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            var raycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                gameObject.AddComponent<GraphicRaycaster>();

            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = panelSize;
            
            bool isPrefabLayout2 = IsUnderPanelSocket();
            
            if (!isPrefabLayout2)
            {
                rt.localPosition = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = panelLocalScale;
            }

            // ─── Background (gradient) ───
            var bgGo = CreateChild("Background", rt);
            var bgRect = bgGo.GetComponent<RectTransform>();
            StretchFill(bgRect);
            var bgImage = bgGo.AddComponent<Image>();

            // Create gradient texture
            var gradTex = CreateGradientTexture(interiorTopColor, interiorBottomColor, 1, 64);
            bgImage.sprite = Sprite.Create(gradTex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;

            // ─── Glow (outer) ───
            var glowGo = CreateChild("Glow", rt);
            var glowRect = glowGo.GetComponent<RectTransform>();
            StretchFill(glowRect);
            glowRect.offsetMin = new Vector2(-glowStrength, -glowStrength);
            glowRect.offsetMax = new Vector2(glowStrength, glowStrength);
            var glowImage = glowGo.AddComponent<Image>();
            glowImage.color = glowColor;
            glowGo.transform.SetAsFirstSibling();

            // ─── Border ───
            var borderGo = CreateChild("NeonBorder", rt);
            var borderRect = borderGo.GetComponent<RectTransform>();
            StretchFill(borderRect);
            var borderOutline = borderGo.AddComponent<Outline>();
            borderOutline.effectColor = borderColor;
            borderOutline.effectDistance = new Vector2(borderThickness, borderThickness);
            var borderImage = borderGo.AddComponent<Image>();
            borderImage.color = new Color(0, 0, 0, 0); // transparent fill, outline provides the border
            borderImage.raycastTarget = false;

            // ─── Title Row ───
            float yOffset = -padding;
            float leftX = padding;
            float rightColX = panelSize.x / 2f + columnSpacing / 2f;
            float leftColWidth = panelSize.x / 2f - padding - columnSpacing / 2f;
            float rightColWidth = panelSize.x / 2f - padding - columnSpacing / 2f;

            // Title: "CHARACTER STATUS"
            var titleLabel = CreateLabel(rt, "TitleLabel", "CHARACTER STATUS", titleFontSize, accentColor,
                new Vector2(leftX, yOffset), new Vector2(panelSize.x - padding * 2f, titleFontSize + 4f));
            titleLabel.alignment = TextAlignmentOptions.Center;

            yOffset -= titleFontSize + rowSpacing + 6f;

            // ─── Separator line ───
            var sepGo = CreateChild("Separator", rt);
            var sepRect = sepGo.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0, 1);
            sepRect.anchorMax = new Vector2(0, 1);
            sepRect.pivot = new Vector2(0, 1);
            sepRect.anchoredPosition = new Vector2(padding, yOffset);
            sepRect.sizeDelta = new Vector2(panelSize.x - padding * 2f, 1.5f);
            var sepImage = sepGo.AddComponent<Image>();
            sepImage.color = borderColor * 0.6f;
            sepImage.raycastTarget = false;

            yOffset -= rowSpacing + 4f;

            // ─── Identity Row (Name | Level | Class) ───
            _nameLabel = CreateStatLabel(rt, "NameLabel", "Name: Player", leftX, yOffset,
                panelSize.x * 0.35f, textColor);
            
            _levelLabel = CreateStatLabel(rt, "LevelLabel", "Lv: 1", leftX + panelSize.x * 0.35f, yOffset,
                panelSize.x * 0.2f, accentColor);
            
            _classLabel = CreateStatLabel(rt, "ClassLabel", "Class: Warrior", leftX + panelSize.x * 0.55f, yOffset,
                panelSize.x * 0.35f, accentColor);

            yOffset -= fontSize + rowSpacing + 4f;

            // ─── Separator 2 ───
            var sep2Go = CreateChild("Separator2", rt);
            var sep2Rect = sep2Go.GetComponent<RectTransform>();
            sep2Rect.anchorMin = new Vector2(0, 1);
            sep2Rect.anchorMax = new Vector2(0, 1);
            sep2Rect.pivot = new Vector2(0, 1);
            sep2Rect.anchoredPosition = new Vector2(padding, yOffset);
            sep2Rect.sizeDelta = new Vector2(panelSize.x - padding * 2f, 1f);
            var sep2Image = sep2Go.AddComponent<Image>();
            sep2Image.color = borderColor * 0.3f;
            sep2Image.raycastTarget = false;

            yOffset -= rowSpacing + 6f;

            // ─── Two-Column Stats ───
            float statRowH = fontSize + rowSpacing;

            // Row 1: HP | MP
            _hpLabel = CreateStatLabel(rt, "HPLabel", "HP: 38", leftX, yOffset, leftColWidth, accentColor);
            _mpLabel = CreateStatLabel(rt, "MPLabel", "MP: 15", rightColX, yOffset, rightColWidth, accentColor);
            yOffset -= statRowH;

            // Row 2: STR | DEF
            _strLabel = CreateStatLabel(rt, "STRLabel", "STR: 40", leftX, yOffset, leftColWidth, textColor);
            _defLabel = CreateStatLabel(rt, "DEFLabel", "DEF: 32", rightColX, yOffset, rightColWidth, textColor);
            yOffset -= statRowH;

            // Row 3: AGI | DEX
            _agiLabel = CreateStatLabel(rt, "AGILabel", "AGI: 27", leftX, yOffset, leftColWidth, textColor);
            _dexLabel = CreateStatLabel(rt, "DEXLabel", "DEX: 25", rightColX, yOffset, rightColWidth, textColor);
            yOffset -= statRowH;

            // Row 4: INT | STA
            _intLabel = CreateStatLabel(rt, "INTLabel", "INT: 15", leftX, yOffset, leftColWidth, textColor);
            _staLabel = CreateStatLabel(rt, "STALabel", "STA: 38", rightColX, yOffset, rightColWidth, textColor);
            yOffset -= statRowH;

            // Row 5: Points
            _pointsLabel = CreateStatLabel(rt, "PointsLabel", "Points: 0", leftX, yOffset, leftColWidth, accentColor);

            // Create ClassIcon for dynamically built UI
            var iconGo = CreateChild("ClassIcon", rt);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 1);
            iconRt.anchorMax = new Vector2(0, 1);
            iconRt.pivot = new Vector2(0, 1);
            // Position it beside the ClassLabel
            iconRt.anchoredPosition = new Vector2(leftX + panelSize.x * 0.55f - 60f, yOffset + statRowH * 3 + fontSize + rowSpacing + 4f); 
            iconRt.sizeDelta = new Vector2(48f, 48f); // small size
            _classIcon = iconGo.AddComponent<Image>();

            LoadClassSprites();
        }

        private void LoadClassSprites()
        {
            if (_classSprites != null) return;
            _classSprites = new System.Collections.Generic.Dictionary<string, Sprite>();
            var classAtlas = Resources.LoadAll<Sprite>("UI/ClassIcons/class");
            if (classAtlas != null && classAtlas.Length > 0)
            {
                foreach (var sprite in classAtlas)
                {
                    _classSprites[sprite.name] = sprite;
                }
            }
            else
            {
                Debug.LogWarning("[CharacterStatusPanelBuilder] Class icon atlas not found at Resources/UI/ClassIcons/class");
            }
        }

        /// <summary>
        /// Update all stat labels. Called by CharacterStatusPanel when StatsChanged fires.
        /// </summary>
        public void SetLabels(string playerName, int level, MineCraftUnity.Player.CharacterClass characterClass,
            int points, int hp, int str, int def, int agi, int dex, int intel, int mp, int sta)
        {
            if (!_built) Build();

            if (_nameLabel != null) _nameLabel.text = $"Name: {playerName}";
            if (_levelLabel != null) _levelLabel.text = $"Lv: {level}";
            if (_classLabel != null) _classLabel.text = $"Class: {characterClass}";
            if (_pointsLabel != null) _pointsLabel.text = $"Points: {points}";
            
            if (_classIcon != null && _classIconNames.TryGetValue(characterClass, out var spriteName))
            {
                if (_classSprites != null && _classSprites.TryGetValue(spriteName, out var sprite))
                {
                    _classIcon.sprite = sprite;
                    _classIcon.color = Color.white;
                }
            }

            if (_hpLabel != null) _hpLabel.text = $"HP: {hp}";
            if (_strLabel != null) _strLabel.text = $"STR: {str}";
            if (_defLabel != null) _defLabel.text = $"DEF: {def}";
            if (_agiLabel != null) _agiLabel.text = $"AGI: {agi}";
            if (_dexLabel != null) _dexLabel.text = $"DEX: {dex}";
            if (_intLabel != null) _intLabel.text = $"INT: {intel}";
            if (_mpLabel != null) _mpLabel.text = $"MP: {mp}";
            if (_staLabel != null) _staLabel.text = $"STA: {sta}";
        }

        // ─── Helpers ───

        private TextMeshProUGUI CreateStatLabel(RectTransform parent, string name, string text,
            float x, float y, float width, Color color)
        {
            var label = CreateLabel(parent, name, text, fontSize, color,
                new Vector2(x, y), new Vector2(width, fontSize + 4f));
            label.alignment = TextAlignmentOptions.Left;
            return label;
        }

        private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
            float size, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = CreateChild(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            if (minecraftFont != null)
                tmp.font = minecraftFont;

            return tmp;
        }

        private static GameObject CreateChild(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Texture2D CreateGradientTexture(Color top, Color bottom, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private bool IsUnderPanelSocket()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == "CharacterStatusPanelSocket" ||
                    current.name == "CharacterStatusPanelSocket 1")
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
