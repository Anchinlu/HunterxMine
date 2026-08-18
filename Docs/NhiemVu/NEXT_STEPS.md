# Next Steps - Hướng Dẫn Hoàn Thành Project

## 📊 Trạng Thái Hiện Tại

**Progress**: 6/9 tasks completed (67%)

### ✅ Completed Tasks
- [x] Task #1: Matrix convention analysis ✓
- [x] Task #2: Joint mapping table ✓
- [x] Task #3: Converter tool implementation ✓
- [x] Task #4: Metadata extraction ✓
- [x] Task #6: Preview tool implementation ✓
- [x] Task #9: Documentation framework ✓

### ⏳ Remaining Tasks (Require Unity Editor)
- [ ] Task #5: Convert và generate 5 assets
- [ ] Task #7: Verify offline preview
- [ ] Task #8: Runtime integration test

---

## 🚀 Quick Start Guide

### Step 1: Open Unity Project (2 minutes)
```
1. Launch Unity Hub
2. Open Project: E:\Project game pk\minecraft
3. Wait for project to load
4. Open any scene with Player object
```

### Step 2: Run Converter (5 minutes)
```
1. Menu: MineCraft → Epic Fight → Animation Converter
2. Verify paths:
   - Source: E:\Project game pk\minecraft\Docs\NhiemVu
   - Output: Assets/Resources/Combat/ConvertedAnimations
3. Click "Convert All 5 Animations"
4. Wait for completion (should take ~10-30 seconds)
5. Verify log shows "5/5 successful"
```

**Expected Output:**
```
=== BATCH CONVERSION START ===

[fist_auto1] Starting conversion...
[fist_auto1] Parsed 20 joints from JSON
[fist_auto1] Converted: 12 tracks, Merged: 4, Skipped: 4
[fist_auto1] ✓ SUCCESS: Saved to Assets/Resources/Combat/ConvertedAnimations/fist_auto1_Converted.asset

[fist_auto2] Starting conversion...
...

=== CONVERSION COMPLETE: 5/5 successful ===
```

**✅ Task #5 Complete** when all 5 assets are generated.

### Step 3: Preview Animation (10 minutes per animation)
```
1. Menu: MineCraft → Epic Fight → Animation Preview Tool
2. Drag fist_auto1_Converted asset to "Animation Asset" field
3. Click "Locate Player in Scene"
4. Click "Cache Pivots & Bind Pose"
5. Test bind pose: Click "Reset Bind Pose" → Character should be in T-pose
6. Test frame 0: Set slider to 0.0s → Should match bind pose closely
7. Scrub timeline: Drag slider 0.0s → 0.5s → Watch animation flow
8. Click Play: Animation should loop smoothly
9. Check for issues:
   - No detachment (vai, khuỷu, hông nối chặt)
   - No flipping/inversion
   - Punch goes forward (+Z direction)
10. Record screen video (OBS, ShareX, or Windows Game Bar)
```

**Repeat for all 5 animations.**

**✅ Task #7 Complete** when all 5 animations verified in preview tool.

### Step 4: Runtime Test (10 minutes)
```
1. Click Play button (Enter Play Mode)
2. Press R key → Enable Combat Mode
3. Verify console log: No errors, character visible
4. Left-click → fist_auto1 should play
5. Check console log:
   [AttackLibrary] Loaded 'fist_auto1' from Resources. Tracks=12...
   [CombatAnim] 'fist_auto1' → CONVERTED TRACKS (Tracks=12...)
   
   ❌ If log shows "PROCEDURAL fallback" → Assets not loaded correctly
   
6. Test combo chain:
   - Left-click → auto1
   - Left-click during combo window (0.2-0.45s) → auto2
   - Left-click again → auto3
   
7. Test special attacks:
   - Sprint (W + Shift) + Left-click → fist_dash
   - Jump (Space) + Left-click → fist_airslash
   
8. Record gameplay video
```

**✅ Task #8 Complete** when runtime log shows "CONVERTED TRACKS" for all animations.

---

## 📋 Detailed Checklists

### Task #5 Checklist: Asset Generation

Before starting:
- [ ] Unity project opened
- [ ] No compilation errors in Console
- [ ] Source JSON files exist in `Docs\NhiemVu\`

During conversion:
- [ ] Converter tool opens without errors
- [ ] Source/Output paths correct
- [ ] "Convert All 5 Animations" clicked
- [ ] Log shows processing each animation
- [ ] No ERROR messages in log
- [ ] Log shows "5/5 successful"

After conversion:
- [ ] Navigate to `Assets/Resources/Combat/ConvertedAnimations/`
- [ ] Verify 5 files exist:
  - [ ] fist_auto1_Converted.asset
  - [ ] fist_auto2_Converted.asset
  - [ ] fist_auto3_Converted.asset
  - [ ] fist_dash_Converted.asset
  - [ ] fist_airslash_Converted.asset
- [ ] Click each asset in Inspector
- [ ] Verify each has:
  - [ ] Tracks: Size > 0 (should be 12)
  - [ ] Total Duration: 0.5
  - [ ] Hit Window Start/End: filled
  - [ ] Combo Window Start/End: filled

**If all checked ✓ → Task #5 COMPLETE**

---

### Task #7 Checklist: Preview Verification

For EACH animation (fist_auto1, auto2, auto3, dash, airslash):

Setup:
- [ ] Preview tool opened
- [ ] Animation asset assigned
- [ ] Player located in scene
- [ ] Bind pose cached

Test 1 - Bind Pose:
- [ ] Click "Reset Bind Pose"
- [ ] Character in proper stance (T-pose or standing)
- [ ] No detachment:
  - [ ] Shoulders connected to torso
  - [ ] Elbows connected to upper arms
  - [ ] Hips connected to torso
  - [ ] Knees connected to thighs

Test 2 - Frame 0:
- [ ] Slider at 0.0s
- [ ] Click "Apply Current Frame"
- [ ] Compare with bind pose
- [ ] Delta is minimal (< 5° rotation, < 0.1 units position)

Test 3 - Timeline Scrub:
- [ ] Drag slider from 0.0s to 0.5s slowly
- [ ] Animation flows smoothly
- [ ] No sudden jumps or pops
- [ ] Identify key poses:
  - [ ] t=0.0s: Ready stance
  - [ ] t=0.05s: Windup
  - [ ] t=0.133s: Hit frame (arm extended)
  - [ ] t=0.233s: Follow-through
  - [ ] t=0.5s: Recovery complete

Test 4 - Playback:
- [ ] Click "Play"
- [ ] Animation loops smoothly
- [ ] No visual artifacts
- [ ] Proper punch direction (forward, not sideways/backward)
- [ ] Chest leans forward during punch
- [ ] Legs remain stable

Test 5 - Per-Joint:
- [ ] Expand "Per-Joint Toggles"
- [ ] Disable all joints
- [ ] Test right arm only:
  - [ ] Enable RightShoulderPivot + RightElbowPivot
  - [ ] Scrub timeline
  - [ ] Arm swings forward and extends
- [ ] Test left arm counter-movement
- [ ] Test chest lean
- [ ] Test leg stability

Test 6 - Record:
- [ ] Start screen recording
- [ ] Play animation 2-3 loops
- [ ] Stop recording
- [ ] Save as `[animation_name]_preview.mp4`
- [ ] Move to `E:\EpicFight_Animation_Reference_Package\videos\`

**If all 5 animations pass all tests ✓ → Task #7 COMPLETE**

---

### Task #8 Checklist: Runtime Integration

Pre-test:
- [ ] All 5 assets generated (Task #5 complete)
- [ ] Assets verified in preview (Task #7 complete)
- [ ] Scene with Player ready

Test 1 - Combat Mode:
- [ ] Enter Play Mode
- [ ] Press R key
- [ ] Console shows: `[LocoAnim] State=CombatReady` or similar
- [ ] Character does NOT disappear
- [ ] Character still controllable with WASD

Test 2 - Single Attack (fist_auto1):
- [ ] Left-click once
- [ ] Console shows:
  ```
  [AttackLibrary] Loaded 'fist_auto1' from Resources. Tracks=12...
  [CombatAnim] 'fist_auto1' → CONVERTED TRACKS (Tracks=12...)
  ```
- [ ] ❌ Console does NOT show "PROCEDURAL fallback"
- [ ] Animation plays visually
- [ ] Character does NOT:
  - [ ] Teleport
  - [ ] Clip through ground
  - [ ] Lose collision
  - [ ] Flip upside down

Test 3 - Combo Chain:
- [ ] Left-click (auto1 starts)
- [ ] Wait 0.2s (combo window opens)
- [ ] Left-click (auto2 chains)
- [ ] Wait 0.2s
- [ ] Left-click (auto3 chains)
- [ ] Console shows sequence:
  ```
  [CombatAnim] 'fist_auto1' → CONVERTED TRACKS
  [CombatAnim] Complete -> 'fist_auto2'
  [CombatAnim] 'fist_auto2' → CONVERTED TRACKS
  [CombatAnim] Complete -> 'fist_auto3'
  [CombatAnim] 'fist_auto3' → CONVERTED TRACKS
  [CombatAnim] Complete → Locomotion
  ```
- [ ] After auto3, character returns to normal locomotion

Test 4 - Special Attacks:
- [ ] fist_dash:
  - [ ] Hold W + Shift (sprint)
  - [ ] Left-click while moving fast
  - [ ] Dash attack triggers (if speed > 4 m/s)
  - [ ] Console: `[CombatAnim] 'fist_dash' → CONVERTED TRACKS`
  
- [ ] fist_airslash:
  - [ ] Press Space (jump)
  - [ ] Left-click in mid-air
  - [ ] Air attack triggers
  - [ ] Console: `[CombatAnim] 'fist_airslash' → CONVERTED TRACKS`

Test 5 - Movement During Attack:
- [ ] Start an attack (left-click)
- [ ] Try moving with WASD during attack
- [ ] Movement should be slowed (not locked)
- [ ] Character position should update smoothly
- [ ] No teleporting after attack ends

Test 6 - Physics Stability:
- [ ] Perform combo chain on flat ground
- [ ] Character stays grounded
- [ ] No bouncing or jittering
- [ ] Collider position stable (doesn't shift)

Test 7 - Record:
- [ ] Start screen recording (Game view)
- [ ] Demo sequence:
  1. Enable combat mode (R)
  2. Single punch (auto1)
  3. Full combo (auto1 → auto2 → auto3)
  4. Sprint + dash attack
  5. Jump + air attack
  6. Return to normal movement
- [ ] Stop recording
- [ ] Save as `runtime_combat_test.mp4`
- [ ] Move to `E:\EpicFight_Animation_Reference_Package\videos\`

Test 8 - Copy Console Logs:
- [ ] Open Unity Console
- [ ] Right-click → Copy all text
- [ ] Paste into `E:\EpicFight_Animation_Reference_Package\RUNTIME_LOGS.txt`
- [ ] Verify logs show "CONVERTED TRACKS" for all 5 animations

**If all tests pass ✓ → Task #8 COMPLETE**

---

## 📝 After Testing: Fill Handover Report

Once Tasks #5, #7, #8 are complete:

1. Open `HANDOVER_REPORT_TEMPLATE.md`
2. Fill in sections:
   - [ ] Section 1: Joint mapping (already mostly filled)
   - [ ] Section 2: Basis conversion (already filled)
   - [ ] Section 3: Track lists (fill keyframe counts)
   - [ ] Section 4: Asset checksums (optional, can use file size)
   - [ ] Section 5: Video list (list the 6 video files)
   - [ ] Section 6: Runtime video description
   - [ ] Section 7: Copy console logs
   - [ ] Section 8: Measure approximation errors (or estimate <3°)
   - [ ] Section 9: Set compatibility level (APPROXIMATE)
   - [ ] Section 10: List screenshots taken
   - [ ] Section 11: Summary
   - [ ] Section 12: Sign-off

3. Save as `HANDOVER_REPORT.md` (remove "TEMPLATE" from filename)

4. Create reference package folder:
```
E:\EpicFight_Animation_Reference_Package\
├── videos\
│   ├── fist_auto1_preview.mp4
│   ├── fist_auto2_preview.mp4
│   ├── fist_auto3_preview.mp4
│   ├── fist_dash_preview.mp4
│   ├── fist_airslash_preview.mp4
│   └── runtime_combat_test.mp4
├── screenshots\
│   ├── bind_pose_verified.png
│   ├── frame_0_comparison.png
│   ├── combo_chain_test.png
│   └── runtime_log_verified.png
├── source-animation-json\
│   ├── fist_auto1.json (copy from Docs\NhiemVu)
│   ├── fist_auto2.json
│   ├── fist_auto3.json
│   ├── fist_dash.json
│   └── fist_airslash.json
├── RUNTIME_LOGS.txt
└── README.md
```

---

## ⏱️ Time Estimates

| Task | Estimated Time | Difficulty |
|------|----------------|------------|
| Task #5: Convert assets | 5 minutes | Easy |
| Task #7: Preview verification | 50 minutes (10 min × 5) | Medium |
| Task #8: Runtime test | 15 minutes | Medium |
| Fill handover report | 30 minutes | Easy |
| **Total** | **~1.5 hours** | - |

---

## 🆘 Troubleshooting

### Issue: Converter tool menu item không hiện
**Fix**: 
```
1. Check Assets/Scripts/Editor/EpicFightAnimationConverter.cs exists
2. Check for compilation errors in Console
3. Restart Unity Editor
```

### Issue: "File not found" khi convert
**Fix**:
```
1. Verify path: E:\Project game pk\minecraft\Docs\NhiemVu\
2. Verify files: fist_auto1.json, fist_auto2.json, etc. exist
3. Check file extensions (should be .json, not .json.txt)
```

### Issue: Runtime log shows "PROCEDURAL fallback"
**Fix**:
```
1. Verify assets exist in Resources/Combat/ConvertedAnimations/
2. Check asset Inspector: Tracks list must have entries
3. If Tracks empty: Re-run converter
4. If still failing: Check Console for errors during asset load
```

### Issue: Animation looks wrong/flipped
**Fix**:
```
1. Check bind pose in preview tool first
2. If bind pose wrong: Player hierarchy might be incorrect
3. Verify RootCombatPivot → UpperBodyPivot → etc. structure
4. If coordinate system wrong: Check converter's ConvertEpicFightToUnity() function
```

### Issue: Character detaches during animation
**Fix**:
```
This should NOT happen with proper conversion. If it does:
1. Check bind pose was cached correctly
2. Verify per-joint toggles are all enabled
3. Test each joint individually to find culprit
4. Check console for errors during animation apply
```

---

## ✅ Success Criteria

Project is complete when:

- [ ] All 5 assets generated and verified
- [ ] All 5 animations preview correctly (no artifacts)
- [ ] Runtime log shows "CONVERTED TRACKS" (not PROCEDURAL)
- [ ] Combo chain works (auto1 → auto2 → auto3)
- [ ] Special attacks trigger correctly
- [ ] 6 videos recorded and saved
- [ ] Console logs captured
- [ ] Handover report filled completely
- [ ] Reference package organized

---

## 📞 Next Steps After Completion

1. **Archive**: Copy `EpicFight_Animation_Reference_Package` to backup location
2. **Commit**: Commit Unity assets to Git (exclude source JSON)
3. **Review**: Send handover report to team lead for review
4. **Integration**: Work with gameplay team to integrate hit detection, damage calculation, VFX, SFX
5. **Future**: This pipeline can be reused for other weapon types (sword, axe, etc.)

---

**Document Version**: 1.0  
**Created**: 2026-08-18  
**Estimated Completion Time**: 1.5 hours  
**Difficulty**: Medium (requires Unity Editor usage)

Good luck! 🚀
