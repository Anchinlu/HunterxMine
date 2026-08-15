# AI Dev Task: Player HUD Setup for Unity

## Objective

Implement the first functional Minecraft-style Player HUD in Unity using the reference assets in the project documentation.

This phase covers only the in-world HUD:

- Crosshair
- Nine-slot hotbar
- Health bar
- Hunger bar
- Armor bar placeholder
- Experience bar and level

Do not implement the full inventory, crafting, pause menu, or container screens in this phase.

## Reference Assets

Reference resource-pack path:

```text
Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/assets/minecraft/textures/gui/sprites/hud/
```

Required sprites:

```text
crosshair.png
hotbar.png
hotbar_selection.png
heart/container.png
heart/full.png
heart/half.png
food_empty.png
food_half.png
food_full.png
armor_empty.png
armor_half.png
armor_full.png
experience_bar_background.png
experience_bar_progress.png
```

Optional later-phase sprites:

```text
air.png
air_empty.png
air_bursting.png
effect_background.png
effect_background_ambient.png
crosshair_attack_indicator_background.png
crosshair_attack_indicator_full.png
crosshair_attack_indicator_progress.png
```

Copy the required files into:

```text
Assets/Minecraft/UI/HUD/
```

Configure every HUD texture with Point filtering, None compression, Clamp wrap mode, disabled mip maps, and preserved alpha.

## Unity UI Architecture

Create a screen-space HUD:

```text
Canvas (Screen Space - Overlay)
└── PlayerHud
    ├── Crosshair
    ├── Hotbar
    │   ├── Background
    │   ├── Selection
    │   └── Slot_0 ... Slot_8
    ├── HealthBar
    ├── HungerBar
    ├── ArmorBar
    ├── ExperienceBar
    └── ExperienceLevelText
```

Use a CanvasScaler with Scale With Screen Size and a reference resolution such as 1920x1080.

Recommended anchors:

- Crosshair: screen center
- Hotbar: bottom-center
- Health: bottom-left, above the hotbar
- Hunger: bottom-right, above the hotbar
- Armor: above health, hidden when armor is zero
- Experience bar: bottom-center, directly above the hotbar
- Experience level: centered above the experience bar

Use Unity Image components. Create individual heart and hunger icons instead of stretching one icon across the complete bar.

## Required Scripts

Create these scripts, reusing equivalent existing systems where possible:

```text
Assets/Scripts/UI/PlayerHud.cs
Assets/Scripts/UI/HudSpriteFactory.cs
Assets/Scripts/Player/PlayerStats.cs
Assets/Scripts/Player/PlayerInventory.cs
```

### PlayerStats

Expose at least:

```csharp
float Health;
float MaxHealth;
int FoodLevel;
int MaxFoodLevel;
float Saturation;
int ArmorValue;
float ExperienceProgress;
int ExperienceLevel;
```

Use these defaults for the test implementation:

```text
Health: 20 / 20
Food: 20 / 20
Armor: 0
Experience progress: 0
Experience level: 0
```

Values must be changeable at runtime so future combat, food, armor, and XP systems can update the HUD without rewriting the UI.

### PlayerInventory

Provide a minimal nine-slot hotbar model:

```csharp
ItemStack[] HotbarItems; // length 9
int SelectedHotbarSlot;
```

If ItemStack does not exist, create a temporary structure containing an item or block identifier, stack count, and optional icon reference. Do not hardcode UI slots to specific items.

### PlayerHud

The script must:

1. Find or receive the active Player reference.
2. Create or bind the HUD elements.
3. Update only changed values where practical.
4. Render health as 10 heart containers with full, half, or empty states.
5. Render hunger as 10 food icons with full, half, or empty states.
6. Render armor icons when armor is greater than zero.
7. Set XP fill from ExperienceProgress.
8. Display ExperienceLevel above the XP bar.
9. Move the hotbar selection frame when the selected slot changes.
10. Update hotbar item icons and stack counts.
11. Keep the crosshair fixed at the screen center.

## Display Rules

### Health and Hunger

Minecraft represents 20 points with 10 icons. Two points are one full icon, one point is a half icon, and zero points is empty. Clamp values safely so invalid icon states cannot occur.

Saturation and hunger animation may be deferred, but the data model must allow them later.

### Experience

Use `experience_bar_background.png` as the background and `experience_bar_progress.png` as a clipped or filled foreground. Clamp progress to 0..1.

### Hotbar

Use `hotbar.png` as the background and `hotbar_selection.png` as the selected-slot frame. Keep nine slots evenly spaced and centered. Do not stretch pixel-art sprites non-uniformly.

## Integration Requirements

- Enable the HUD when the player enters the world.
- Support the current Player object created by `PlayerSetupMenu`.
- Do not interfere with mouse look, block interaction, chat commands, or debug overlays.
- The HUD may remain visible while chat or a menu is open, but input handling must not conflict.
- Cache icon objects and update sprites, visibility, text, or fill values. Do not rebuild the HUD every frame.
- Avoid OnGUI for the new HUD. Use Canvas, Image, and Text or TextMeshPro components.
- If item icon assets are not yet organized, use a temporary mapping and document it. Do not duplicate block textures unnecessarily.

## Verification Checklist

Test in Play Mode at 1920x1080 and one smaller resolution:

- Crosshair stays exactly centered.
- Hotbar stays bottom-center and scales correctly.
- Selection frame moves with number keys or mouse wheel.
- Health shows full, half, and empty hearts.
- Hunger shows full, half, and empty food icons.
- Armor is hidden at zero and visible when armor increases.
- XP shows 0%, 50%, and 100% progress correctly.
- XP level text updates correctly.
- Pixel art remains sharp with no blur.
- HUD remains stable while chunks load and while the player moves.
- No NullReferenceException, missing sprite errors, or per-frame object allocations occur.
- Existing chat, debug statistics, loading overlay, and player controls still work.

Run a C# build after implementation and report the exact warning and error counts.

## Scope Boundaries

Do not implement the full inventory screen, crafting table UI, armor inventory screen, container screens, potion/effect icons, boss bars, touch controls, or multiplayer synchronization in this phase. Create extension points only.

## Completion Report Requirement

When implementation is complete, report back in Vietnamese, not English. The report must include:

1. Changed and newly created files.
2. Reference assets copied and their destination.
3. Functional HUD elements.
4. Known limitations and deferred features.
5. C# build result, including warning and error counts.
6. Manual Play Mode verification results and remaining issues.

Do not claim completion if the Unity scene or runtime HUD has not been tested.
