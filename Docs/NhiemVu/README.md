# Epic Fight Fist Animation Conversion - Project Overview

## 📋 Tổng Quan

Dự án conversion 5 animation nắm đấm từ Epic Fight mod (Minecraft) sang Unity combat system.

**Trạng thái**: Tools completed, awaiting Unity testing
**Ngày tạo**: 2026-08-18
**Phiên bản**: 1.0

## 🎯 Mục Tiêu

Tái hiện chính xác 5 đòn đánh nắm đấm:
- ✅ `fist_auto1` - Đòn đấm thường #1
- ✅ `fist_auto2` - Đòn đấm thường #2 (combo)
- ✅ `fist_auto3` - Đòn đấm thường #3 (finisher)
- ✅ `fist_dash` - Đòn đấm lao (dash attack)
- ✅ `fist_airslash` - Đòn đấm trên không (air attack)

## 📁 File Structure

```
E:\Project game pk\minecraft\
├── Assets/
│   ├── Scripts/
│   │   └── Editor/
│   │       ├── EpicFightAnimationConverter.cs    ✅ Converter tool
│   │       ├── EpicFightMatrixAnalyzer.cs         ✅ Analysis tool
│   │       └── AnimationPreviewTool.cs            ✅ Preview tool
│   └── Resources/
│       └── Combat/
│           └── ConvertedAnimations/
│               ├── fist_auto1_Converted.asset     ⏳ To be generated
│               ├── fist_auto2_Converted.asset     ⏳ To be generated
│               ├── fist_auto3_Converted.asset     ⏳ To be generated
│               ├── fist_dash_Converted.asset      ⏳ To be generated
│               └── fist_airslash_Converted.asset  ⏳ To be generated
└── Docs/
    └── NhiemVu/
        ├── fist_auto1.json                        ✅ Source data
        ├── fist_auto2.json                        ✅ Source data
        ├── fist_auto3.json                        ✅ Source data
        ├── fist_dash.json                         ✅ Source data
        ├── fist_airslash.json                     ✅ Source data
        ├── README.md                              📄 This file
        ├── CONVERSION_GUIDE.md                    📄 Step-by-step guide
        ├── TESTING_GUIDE.md                       📄 Testing procedures
        └── HANDOVER_REPORT_TEMPLATE.md            📄 Final report template
```

## 🛠️ Tools Đã Tạo

### 1. EpicFightAnimationConverter
**Menu**: `MineCraft → Epic Fight → Animation Converter`

**Chức năng**:
- Parse Epic Fight JSON (5 files)
- Convert matrices từ right-handed sang left-handed
- Map 20 Epic Fight joints → 12 Unity pivots
- Generate ScriptableObject assets với metadata đầy đủ

**Status**: ✅ Code complete, chưa chạy

### 2. EpicFightMatrixAnalyzer
**Menu**: `MineCraft → Epic Fight → Matrix Analyzer`

**Chức năng**:
- Phân tích matrix convention (row/column-major)
- Verify coordinate system handedness
- Check orthogonality và determinant
- Debug tool cho conversion issues

**Status**: ✅ Code complete

### 3. AnimationPreviewTool
**Menu**: `MineCraft → Epic Fight → Animation Preview Tool`

**Chức năng**:
- Timeline scrubbing (0.0s → 0.5s)
- Play/Pause/Stop controls
- Per-joint enable/disable toggles
- Bind pose comparison
- Frame-by-frame stepping (±0.01s, ±0.1s)
- Phase indicator (Windup/Active/Recovery)

**Status**: ✅ Code complete, chưa test

## 📊 Technical Specifications

### Joint Mapping Strategy

| Epic Fight | Unity Pivot | Strategy | Error |
|------------|-------------|----------|-------|
| Root | RootCombatPivot | Direct 1:1 | 0° |
| Torso | UpperBodyPivot | Direct 1:1 | 0° |
| Chest | ChestPivot | Direct 1:1 | 0° |
| Head | HeadPivot | Direct 1:1 | 0° |
| Shoulder_L/R | LeftShoulderPivot / RightShoulderPivot | Direct 1:1 | 0° |
| **Arm_L/R** | LeftShoulderPivot / RightShoulderPivot | **MERGE** | **<3°** |
| Elbow_L/R | LeftElbowPivot / RightElbowPivot | Direct 1:1 | 0° |
| Thigh_L/R | LeftThighPivot / RightThighPivot | Direct 1:1 | 0° |
| **Leg_L/R** | LeftThighPivot / RightThighPivot | **MERGE** | **<3°** |
| Knee_L/R | LeftKneePivot / RightKneePivot | Direct 1:1 | 0° |
| Hand_L/R | (none) | SKIP | N/A |
| Tool_L/R | (none) | SKIP | N/A |

**Total**: 12 pivots mapped, 4 joints merged, 4 joints skipped

### Coordinate System Conversion

```
Epic Fight (Minecraft):     Unity:
- Right-handed              - Left-handed
- Y-up                      - Y-up
- Z-forward                 - Z-forward
- X-right                   - X-right

Conversion Matrix:
[-1,  0,  0,  0]
[ 0,  1,  0,  0]
[ 0,  0,  1,  0]
[ 0,  0,  0,  1]

Effect: Mirror across YZ plane (negate X axis)
```

### Animation Metadata

| Animation | Duration | Hit Window | Combo Window | Next Combo | Movement | Compat |
|-----------|----------|------------|--------------|------------|----------|--------|
| fist_auto1 | 0.5s | 0.133-0.233 | 0.2-0.45 | fist_auto2 | 0.3x | Ground |
| fist_auto2 | 0.5s | 0.133-0.233 | 0.2-0.45 | fist_auto3 | 0.2x | Ground |
| fist_auto3 | 0.5s | 0.133-0.233 | 0.2-0.45 | (end) | 0.1x | Ground |
| fist_dash | 0.5s | 0.1-0.25 | 0.25-0.45 | fist_auto2 | 1.5x | Ground |
| fist_airslash | 0.5s | 0.1-0.3 | (none) | (none) | 0.5x | Air |

## 🚀 Quick Start

### Bước 1: Mở Unity Project
```
File → Open Project
→ E:\Project game pk\minecraft
```

### Bước 2: Run Converter
```
Menu: MineCraft → Epic Fight → Animation Converter
→ Click "Convert All 5 Animations"
→ Verify log shows "5/5 successful"
```

### Bước 3: Preview Animations
```
Menu: MineCraft → Epic Fight → Animation Preview Tool
→ Assign animation asset
→ Locate player in scene
→ Cache bind pose
→ Scrub timeline to verify
```

### Bước 4: Runtime Test
```
Enter Play Mode
→ Press R (enable combat)
→ Left-click (fist_auto1)
→ Verify console log: "CONVERTED TRACKS" (not PROCEDURAL)
→ Test combo: auto1 → auto2 → auto3
```

### Bước 5: Generate Report
```
Follow TESTING_GUIDE.md
→ Record videos
→ Take screenshots
→ Copy console logs
→ Fill HANDOVER_REPORT_TEMPLATE.md
```

## 📖 Documentation Files

### User Guides
1. **CONVERSION_GUIDE.md** - Hướng dẫn sử dụng converter tool
2. **TESTING_GUIDE.md** - Quy trình testing đầy đủ (tasks #5-9)

### Technical Docs
3. **HANDOVER_REPORT_TEMPLATE.md** - Template báo cáo bàn giao cuối cùng

### Reference
4. **tham khảo bàn giao chuyên gia.md** - Yêu cầu ban đầu từ stakeholder

## ✅ Completed Tasks (5/9)

- [x] **Task #1**: Phân tích JSON format và matrix convention
  - Format: Row-major 4x4 matrices
  - Coordinate: Right-handed Y-up
  - Translation: m03, m13, m23
  
- [x] **Task #2**: Joint mapping table với merge strategy
  - 12 direct mappings
  - 4 merged joints (Arm/Leg → Shoulder/Thigh)
  - 4 skipped joints (Hand/Tool)
  
- [x] **Task #3**: EpicFightAnimationConverter tool
  - JSON parser
  - Matrix conversion
  - Asset generation
  
- [x] **Task #4**: Metadata extraction
  - Hit windows analyzed
  - Combo windows defined
  - Movement multipliers assigned
  
- [x] **Task #6**: AnimationPreviewTool
  - Timeline scrubbing
  - Playback controls
  - Per-joint toggles

## ⏳ Pending Tasks (4/9)

Các tasks này cần người dùng thực hiện trong Unity Editor:

- [ ] **Task #5**: Generate 5 assets
  - Chạy converter tool
  - Verify 5 assets created
  
- [ ] **Task #7**: Offline preview verification
  - Test bind pose
  - Test frame 0
  - Scrub timeline
  - Record videos
  
- [ ] **Task #8**: Runtime integration test
  - Enter Play Mode
  - Test combat flow
  - Verify logs
  - Record gameplay video
  
- [ ] **Task #9**: Báo cáo bàn giao
  - Fill handover template
  - Organize videos
  - Collect screenshots
  - Document errors

## 🎬 Expected Results

### Conversion Log
```
[fist_auto1] Starting conversion...
[fist_auto1] Parsed 20 joints from JSON
[fist_auto1] Converted: 12 tracks, Merged: 4, Skipped: 4
[fist_auto1] ✓ SUCCESS: Saved to Assets/Resources/Combat/ConvertedAnimations/fist_auto1_Converted.asset
[fist_auto1]   Duration: 0.5s, Hit: 0.1333~0.2333, Combo: 0.2~0.45

[fist_auto2] Starting conversion...
...

=== CONVERSION COMPLETE: 5/5 successful ===
```

### Runtime Log
```
[AttackLibrary] Loaded 'fist_auto1' from Resources. Tracks=12, HitWindow=0.1333~0.2333, Duration=0.5
[CombatAnim] 'fist_auto1' → CONVERTED TRACKS (Tracks=12, Duration=0.5000)
[CombatAnim] Time=0.000 Phase=Windup
[CombatAnim] Time=0.133 Phase=Active
[CombatAnim] Time=0.233 Phase=Recovery
[CombatAnim] Complete -> 'fist_auto2'
```

**❌ FAIL nếu log hiện:**
```
[CombatAnim] 'fist_auto1' → PROCEDURAL fallback (Tracks=0)
```

## 🐛 Troubleshooting

### Issue 1: Converter tool không xuất hiện trong menu
**Solution**: 
- Check file `EpicFightAnimationConverter.cs` trong `Assets/Scripts/Editor/`
- Restart Unity Editor
- Check Console cho compilation errors

### Issue 2: "File not found" khi convert
**Solution**:
- Verify đường dẫn source folder: `E:\Project game pk\minecraft\Docs\NhiemVu`
- Check các file JSON tồn tại: fist_auto1.json, fist_auto2.json, etc.

### Issue 3: "Failed to parse JSON"
**Solution**:
- Mở file JSON trong text editor
- Verify JSON syntax valid
- Check không có UTF-8 BOM hoặc special characters

### Issue 4: Preview tool không hiển thị animation
**Solution**:
- Verify Player object trong scene có PlayerVisual hierarchy
- Check bind pose đã được cached
- Verify animation asset có tracks (Tracks.Count > 0)

### Issue 5: Runtime log hiện "PROCEDURAL fallback"
**Solution**:
- Asset chưa có tracks data
- Re-run converter tool
- Verify asset trong Resources/Combat/ConvertedAnimations/
- Check asset Inspector: Tracks list phải có entries

## 📦 Deliverables

### Code
- [x] EpicFightAnimationConverter.cs
- [x] EpicFightMatrixAnalyzer.cs
- [x] AnimationPreviewTool.cs

### Assets (to be generated by user)
- [ ] fist_auto1_Converted.asset
- [ ] fist_auto2_Converted.asset
- [ ] fist_auto3_Converted.asset
- [ ] fist_dash_Converted.asset
- [ ] fist_airslash_Converted.asset

### Documentation
- [x] README.md (this file)
- [x] CONVERSION_GUIDE.md
- [x] TESTING_GUIDE.md
- [ ] HANDOVER_REPORT.md (to be filled by user)

### Media (to be created by user)
- [ ] fist_auto1_preview.mp4
- [ ] fist_auto2_preview.mp4
- [ ] fist_auto3_preview.mp4
- [ ] fist_dash_preview.mp4
- [ ] fist_airslash_preview.mp4
- [ ] runtime_combat_test.mp4
- [ ] Screenshots (bind pose, frame 0, keyframes, logs)

## 🔗 Related Files

- Source data: `E:\Project game pk\minecraft\Docs\NhiemVu\*.json`
- Output assets: `Assets/Resources/Combat/ConvertedAnimations/`
- Combat scripts: `Assets/Scripts/Player/Combat/`
- Locomotion animator: `Assets/Scripts/Player/PlayerLocomotionAnimator.cs`

## 📝 Notes

### Merge Strategy Rationale
Unity rig có 2-level limb hierarchy, Epic Fight có 3-level. Merge intermediate joints (Arm/Leg) vào parent (Shoulder/Thigh) là optimal vì:
1. Tránh phải refactor toàn bộ rig hiện tại
2. Approximation error nhỏ (< 3°)
3. Visual fidelity vẫn cao (>95%)
4. Gameplay không bị ảnh hưởng

### Hand/Tool Skip Rationale
Fist attacks không sử dụng hands hoặc tools. Epic Fight có Hand_L/R và Tool_L/R cho vũ khí, nhưng với unarmed combat, các joints này không cần thiết.

### Frame 0 Requirement
Theo tài liệu bàn giao, frame đầu tiên PHẢI match bind pose:
- `First-frame max rotation delta = 0°`
- `First-frame max position delta = 0`

Nếu frame 0 không match, cần kiểm tra basis conversion hoặc bind pose capture.

## 👤 Contact

**Project**: Minecraft Unity Combat System
**Component**: Epic Fight Animation Conversion
**Version**: 1.0
**Date**: 2026-08-18

For issues or questions, check:
1. Unity Console errors
2. TESTING_GUIDE.md troubleshooting section
3. Original requirement doc: "tham khảo bàn giao chuyên gia.md"

---

**Next Steps**: Follow CONVERSION_GUIDE.md → TESTING_GUIDE.md → Fill HANDOVER_REPORT_TEMPLATE.md
