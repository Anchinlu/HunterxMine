#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using TMPro;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Generates a TMP_FontAsset (bitmap) from the Minecraft ascii.png atlas.
    /// Atlas is 128×128 pixels, 16×16 grid of 8×8 pixel character cells.
    /// Uses SerializedObject to write read-only TMP_FontAsset properties.
    /// </summary>
    public static class MinecraftFontAssetGenerator
    {
        private const string TexturePath = "Assets/Resources/UI/Fonts/Minecraft/ascii.png";
        private const string OutputPath = "Assets/Resources/UI/Fonts/Minecraft/MinecraftAscii.asset";

        private const int AtlasWidth = 128;
        private const int AtlasHeight = 128;
        private const int GridCols = 16;
        private const int GridRows = 16;
        private const int CellW = AtlasWidth / GridCols;   // 8
        private const int CellH = AtlasHeight / GridRows;  // 8
        private const int Ascent = 7;

        private static readonly string[] CharRows = new string[]
        {
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000",
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000",
            " !\"#$%&'()*+,-./",
            "0123456789:;<=>?",
            "@ABCDEFGHIJKLMNO",
            "PQRSTUVWXYZ[\\]^_",
            "`abcdefghijklmno",
            "pqrstuvwxyz{|}~\u0000",
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000",
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u00a3\u0000\u0000\u0192",
            "\u0000\u0000\u0000\u0000\u0000\u0000\u00aa\u00ba\u0000\u0000\u00ac\u0000\u0000\u0000\u00ab\u00bb",
            "\u2591\u2592\u2593\u2502\u2524\u2561\u2562\u2556\u2555\u2563\u2551\u2557\u255d\u255c\u255b\u2510",
            "\u2514\u2534\u252c\u251c\u2500\u253c\u255e\u255f\u255a\u2554\u2569\u2566\u2560\u2550\u256c\u2567",
            "\u2568\u2564\u2565\u2559\u2558\u2552\u2553\u256b\u256a\u2518\u250c\u2588\u2584\u258c\u2590\u2580",
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u2205\u2208\u0000",
            "\u2261\u00b1\u2265\u2264\u2320\u2321\u00f7\u2248\u00b0\u2219\u0000\u221a\u207f\u00b2\u25a0\u0000"
        };

        [MenuItem("Tools/Minecraft/Generate Bitmap Font Asset")]
        public static void Generate()
        {
            // 1. Import texture
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[FontGen] Texture not found at {TexturePath}. Copy ascii.png first.");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                Debug.LogError("[FontGen] Failed to load texture after reimport.");
                return;
            }

            // 2. Build glyph and character data
            var glyphList = new List<Glyph>();
            var charList = new List<TMP_Character>();
            uint glyphIndex = 0;

            for (int row = 0; row < GridRows; row++)
            {
                string rowChars = CharRows[row];
                for (int col = 0; col < GridCols; col++)
                {
                    char c = rowChars[col];
                    if (c == '\0') continue;

                    int pixelX = col * CellW;
                    int pixelY = (GridRows - 1 - row) * CellH;

                    int glyphWidth = MeasureGlyphWidth(texture, pixelX, pixelY, CellW, CellH);

                    if (c == ' ' || glyphWidth == 0)
                    {
                        var spaceGlyph = new Glyph(glyphIndex,
                            new GlyphMetrics(0, 0, 0, 0, 4),
                            new GlyphRect(pixelX, pixelY, 1, 1), 1f, 0);
                        glyphList.Add(spaceGlyph);
                        charList.Add(new TMP_Character((uint)c, spaceGlyph));
                        glyphIndex++;
                        continue;
                    }

                    var glyph = new Glyph(glyphIndex,
                        new GlyphMetrics(glyphWidth, CellH, 0, Ascent, glyphWidth + 1),
                        new GlyphRect(pixelX, pixelY, glyphWidth, CellH), 1f, 0);
                    glyphList.Add(glyph);
                    charList.Add(new TMP_Character((uint)c, glyph));
                    glyphIndex++;
                }
            }

            // 3. Resolve shader — try bitmap first, then fall back to SDF, then Unlit
            Shader shader = Shader.Find("TextMeshPro/Bitmap");
            if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Bitmap");
            if (shader == null)
            {
                // TMP Essential Resources may not be imported yet — safely try default font
                try
                {
                    var defaultFont = TMP_Settings.defaultFontAsset;
                    if (defaultFont != null && defaultFont.material != null)
                    {
                        shader = defaultFont.material.shader;
                        Debug.Log($"[FontGen] Using shader from default TMP font: {shader.name}");
                    }
                }
                catch (System.Exception)
                {
                    // TMP_Settings not initialized yet, skip
                }
            }
            if (shader == null) shader = Shader.Find("UI/Unlit/Text");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                Debug.LogError("[FontGen] No suitable shader found. Import TMP Essentials first (Window > TextMeshPro > Import TMP Essential Resources).");
                return;
            }

            // 4. Create material FIRST (before SerializedObject, to prevent OnValidate crash)
            var material = new Material(shader);
            material.name = "MinecraftAscii Material";
            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            material.SetColor("_FaceColor", Color.white);

            // 5. Create TMP_FontAsset
            var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = "MinecraftAscii";

            // Assign material and atlas textures BEFORE SerializedObject (prevents OnValidate NRE)
            fontAsset.material = material;
            fontAsset.atlasTextures = new Texture2D[] { texture };

            // 6. Use SerializedObject for read-only properties
            var so = new SerializedObject(fontAsset);

            SetPropFloat(so, "m_AtlasWidth", AtlasWidth);
            SetPropFloat(so, "m_AtlasHeight", AtlasHeight);
            SetPropFloat(so, "m_AtlasPadding", 0);

            // m_AtlasRenderMode is an enum stored as int — set directly
            var renderModeProp = so.FindProperty("m_AtlasRenderMode");
            if (renderModeProp != null)
                renderModeProp.intValue = (int)GlyphRenderMode.SMOOTH;

            // Face info — all use floatValue to be safe
            var faceInfoProp = so.FindProperty("m_FaceInfo");
            if (faceInfoProp != null)
            {
                SetRelativeFloat(faceInfoProp, "m_PointSize", CellH);
                SetRelativeFloat(faceInfoProp, "m_LineHeight", CellH + 1f);
                SetRelativeFloat(faceInfoProp, "m_AscentLine", Ascent);
                SetRelativeFloat(faceInfoProp, "m_DescentLine", Ascent - CellH);
                SetRelativeFloat(faceInfoProp, "m_Baseline", 0f);
                SetRelativeFloat(faceInfoProp, "m_UnderlineOffset", -1f);
                SetRelativeFloat(faceInfoProp, "m_UnderlineThickness", 1f);
                SetRelativeFloat(faceInfoProp, "m_StrikethroughOffset", CellH / 2f);
                SetRelativeFloat(faceInfoProp, "m_TabWidth", CellW * 4f);
            }

            // Glyph table
            var glyphTableProp = so.FindProperty("m_GlyphTable");
            if (glyphTableProp != null)
            {
                glyphTableProp.ClearArray();
                for (int i = 0; i < glyphList.Count; i++)
                {
                    glyphTableProp.InsertArrayElementAtIndex(i);
                    var elem = glyphTableProp.GetArrayElementAtIndex(i);
                    var g = glyphList[i];

                    SetRelativeUInt(elem, "m_Index", g.index);
                    SetRelativeFloat(elem, "m_Scale", g.scale);

                    var metrics = elem.FindPropertyRelative("m_Metrics");
                    if (metrics != null)
                    {
                        SetRelativeFloat(metrics, "m_Width", g.metrics.width);
                        SetRelativeFloat(metrics, "m_Height", g.metrics.height);
                        SetRelativeFloat(metrics, "m_HorizontalBearingX", g.metrics.horizontalBearingX);
                        SetRelativeFloat(metrics, "m_HorizontalBearingY", g.metrics.horizontalBearingY);
                        SetRelativeFloat(metrics, "m_HorizontalAdvance", g.metrics.horizontalAdvance);
                    }

                    var rect = elem.FindPropertyRelative("m_GlyphRect");
                    if (rect != null)
                    {
                        SetRelativeInt(rect, "m_X", g.glyphRect.x);
                        SetRelativeInt(rect, "m_Y", g.glyphRect.y);
                        SetRelativeInt(rect, "m_Width", g.glyphRect.width);
                        SetRelativeInt(rect, "m_Height", g.glyphRect.height);
                    }
                }
            }

            // Character table
            var charTableProp = so.FindProperty("m_CharacterTable");
            if (charTableProp != null)
            {
                charTableProp.ClearArray();
                for (int i = 0; i < charList.Count; i++)
                {
                    charTableProp.InsertArrayElementAtIndex(i);
                    var elem = charTableProp.GetArrayElementAtIndex(i);

                    SetRelativeUInt(elem, "m_Unicode", charList[i].unicode);
                    SetRelativeUInt(elem, "m_GlyphIndex", charList[i].glyphIndex);
                    SetRelativeFloat(elem, "m_Scale", 1f);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // 7. Delete old asset if exists
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(OutputPath);

            // 8. Save
            AssetDatabase.CreateAsset(fontAsset, OutputPath);
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            fontAsset.ReadFontAssetDefinition();

            Debug.Log($"[FontGen] SUCCESS: Created MinecraftAscii bitmap font at {OutputPath} with {charList.Count} characters, {glyphList.Count} glyphs. Shader: {shader.name}");
        }

        private static int MeasureGlyphWidth(Texture2D tex, int cellX, int cellY, int cellW, int cellH)
        {
            int maxCol = 0;
            for (int x = 0; x < cellW; x++)
            {
                for (int y = 0; y < cellH; y++)
                {
                    var pixel = tex.GetPixel(cellX + x, cellY + y);
                    if (pixel.a > 0.01f)
                        maxCol = x + 1;
                }
            }
            return maxCol;
        }

        // ─── SerializedProperty helpers (safe for any type) ───

        private static void SetPropFloat(SerializedObject so, string name, float value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
            {
                if (prop.propertyType == SerializedPropertyType.Float)
                    prop.floatValue = value;
                else if (prop.propertyType == SerializedPropertyType.Integer)
                    prop.intValue = (int)value;
            }
        }

        private static void SetPropInt(SerializedObject so, string name, int value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
            {
                if (prop.propertyType == SerializedPropertyType.Integer)
                    prop.intValue = value;
                else if (prop.propertyType == SerializedPropertyType.Float)
                    prop.floatValue = value;
                else if (prop.propertyType == SerializedPropertyType.Enum)
                    prop.enumValueIndex = value;
            }
        }

        private static void SetRelativeFloat(SerializedProperty parent, string name, float value)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null) return;
            if (prop.propertyType == SerializedPropertyType.Float)
                prop.floatValue = value;
            else if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = (int)value;
        }

        private static void SetRelativeInt(SerializedProperty parent, string name, int value)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null) return;
            if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = value;
            else if (prop.propertyType == SerializedPropertyType.Float)
                prop.floatValue = value;
        }

        private static void SetRelativeUInt(SerializedProperty parent, string name, uint value)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null) return;
            if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = (int)value;
            else if (prop.propertyType == SerializedPropertyType.Float)
                prop.floatValue = value;
        }
    }
}
#endif
