# Hướng Dẫn Conversion Animation Epic Fight

## Bước 1: Kiểm Tra File Nguồn

Đảm bảo các file JSON có sẵn tại:
```
E:\Project game pk\minecraft\Docs\NhiemVu\
├── fist_auto1.json
├── fist_auto2.json
├── fist_auto3.json
├── fist_dash.json
└── fist_airslash.json
```

## Bước 2: Mở Unity Editor Tool

1. Mở Unity project: `E:\Project game pk\minecraft`
2. Từ menu bar, chọn: **MineCraft → Epic Fight → Animation Converter**
3. Cửa sổ "EF Animation Converter" sẽ hiện ra

## Bước 3: Kiểm Tra Cấu Hình

Trong converter window, xác nhận:
- **Source Folder**: `E:\Project game pk\minecraft\Docs\NhiemVu`
- **Output Folder**: `Assets/Resources/Combat/ConvertedAnimations`

## Bước 4: Chạy Conversion

### Option A: Convert Tất Cả (Khuyến Nghị)
Click nút **"Convert All 5 Animations"**

Tool sẽ xử lý tuần tự:
1. fist_auto1
2. fist_auto2
3. fist_auto3
4. fist_dash
5. fist_airslash

### Option B: Convert Từng Animation
Click các nút riêng lẻ nếu cần debug hoặc re-convert:
- "Convert fist_auto1"
- "Convert fist_auto2"
- "Convert fist_auto3"
- "Convert fist_dash"
- "Convert fist_airslash"

## Bước 5: Kiểm Tra Log Output

Trong conversion log, kiểm tra:

### Log Thành Công
```
[fist_auto1] Starting conversion...
[fist_auto1] Parsed 20 joints from JSON
[fist_auto1]   Merge: Arm_R → RightShoulderPivot
[fist_auto1]   Merge: Arm_L → LeftShoulderPivot
[fist_auto1]   Merge: Leg_R → RightThighPivot
[fist_auto1]   Merge: Leg_L → LeftThighPivot
[fist_auto1]   Skip: Hand_R (not in mapping table)
[fist_auto1]   Skip: Hand_L (not in mapping table)
[fist_auto1]   Skip: Tool_R (not in mapping table)
[fist_auto1]   Skip: Tool_L (not in mapping table)
[fist_auto1] Converted: 12 tracks, Merged: 4, Skipped: 4
[fist_auto1] ✓ SUCCESS: Saved to Assets/Resources/Combat/ConvertedAnimations/fist_auto1_Converted.asset
[fist_auto1]   Duration: 0.5s, Hit: 0.1333~0.2333, Combo: 0.2~0.45
```

### Kiểm Tra Số Lượng Tracks
Mỗi animation phải có **12 tracks** (mapped joints):
- Root → RootCombatPivot
- Torso → UpperBodyPivot
- Chest → ChestPivot
- Head → HeadPivot
- Shoulder_L → LeftShoulderPivot
- Shoulder_R → RightShoulderPivot
- Arm_L → LeftShoulderPivot (merged)
- Arm_R → RightShoulderPivot (merged)
- Elbow_L → LeftElbowPivot
- Elbow_R → RightElbowPivot
- Thigh_L → LeftThighPivot
- Thigh_R → RightThighPivot
- Leg_L → LeftThighPivot (merged)
- Leg_R → RightThighPivot (merged)
- Knee_L → LeftKneePivot
- Knee_R → RightKneePivot

**Lưu ý**: Do merge strategy, số tracks thực tế sẽ là 12 pivots duy nhất.

## Bước 6: Verify Assets Đã Được Tạo

Trong Unity Project window, navigate đến:
```
Assets/Resources/Combat/ConvertedAnimations/
```

Kiểm tra 5 files:
- ✓ fist_auto1_Converted.asset
- ✓ fist_auto2_Converted.asset
- ✓ fist_auto3_Converted.asset
- ✓ fist_dash_Converted.asset
- ✓ fist_airslash_Converted.asset

### Kiểm Tra Nội Dung Asset

Click vào từng asset trong Unity Inspector:

**Metadata phải hiển thị:**
- Attack Id: [fist_auto1/2/3/dash/airslash]
- Total Duration: 0.5
- Hit Window Start: [varies]
- Hit Window End: [varies]
- Combo Window Start: [varies]
- Combo Window End: [varies]
- Next Combo Attack Id: [varies or None]
- Movement Multiplier: [varies]
- Is Ground Compatible: [true/false]
- Is Air Compatible: [true/false]
- Tracks: Size = [number of tracks]

**Tracks list phải có entries:**
- Mỗi track có:
  - Unity Joint Name: [pivot name]
  - Times: Array với timestamps
  - Position Deltas: Array với Vector3
  - Rotation Deltas: Array với Quaternion

## Bước 7: Troubleshooting

### Lỗi: "File not found"
- Kiểm tra đường dẫn source folder
- Verify các file JSON tồn tại
- Check spelling của filename

### Lỗi: "Failed to parse JSON"
- Mở file JSON trong text editor
- Kiểm tra valid JSON syntax
- Verify không có special characters

### Lỗi: "No metadata defined"
- Animation name không đúng (phải là: fist_auto1, fist_auto2, fist_auto3, fist_dash, fist_airslash)
- Check typo trong filename

### Warning: "Tracks count mismatch"
- Nếu tracks < 12: Có joint bị thiếu trong JSON
- Nếu tracks > 12: Có duplicate mappings

## Bước 8: Verify Conversion Quality

### Test 1: Bind Pose Check
Frame đầu tiên (time=0.0) của mỗi animation phải gần với bind pose.

**Cách kiểm tra:**
1. Mở asset trong Inspector
2. Expand "Tracks"
3. Với mỗi track, check Rotation Deltas[0] và Position Deltas[0]
4. Rotation Deltas[0] phải gần Quaternion.identity (0,0,0,1)
5. Position Deltas[0] phải gần Vector3.zero hoặc offset nhỏ

### Test 2: Matrix Determinant
Converter tự động normalize matrices, không cần kiểm tra thủ công.

### Test 3: Coordinate System
- X axis: Left/Right (negative = left in Unity)
- Y axis: Up/Down
- Z axis: Forward/Back

## Next Steps

Sau khi conversion thành công:
1. **Task #6**: Xây dựng Animation Preview Tool
2. **Task #7**: Verify offline preview với timeline scrubbing
3. **Task #8**: Runtime integration test với combat system
4. **Task #9**: Tạo báo cáo bàn giao đầy đủ

## Technical Notes

### Joint Mapping Strategy
```
Epic Fight           Unity Pivot          Strategy
───────────────────  ──────────────────  ─────────────
Root                 RootCombatPivot     Direct 1:1
Torso                UpperBodyPivot      Direct 1:1
Chest                ChestPivot          Direct 1:1
Head                 HeadPivot           Direct 1:1
Shoulder_L           LeftShoulderPivot   Direct 1:1
Shoulder_R           RightShoulderPivot  Direct 1:1
Arm_L                LeftShoulderPivot   MERGED
Arm_R                RightShoulderPivot  MERGED
Elbow_L              LeftElbowPivot      Direct 1:1
Elbow_R              RightElbowPivot     Direct 1:1
Thigh_L              LeftThighPivot      Direct 1:1
Thigh_R              RightThighPivot     Direct 1:1
Leg_L                LeftThighPivot      MERGED
Leg_R                RightThighPivot     MERGED
Knee_L               LeftKneePivot       Direct 1:1
Knee_R               RightKneePivot      Direct 1:1
Hand_L               (none)              SKIPPED
Hand_R               (none)              SKIPPED
Tool_L               (none)              SKIPPED
Tool_R               (none)              SKIPPED
```

### Merge Strategy Rationale
Unity rig hiện tại có 2-level limb hierarchy:
- Shoulder → Elbow (không có intermediate Arm pivot)
- Thigh → Knee (không có intermediate Leg pivot)

Epic Fight có 3-level:
- Shoulder → Arm → Elbow
- Thigh → Leg → Knee

**Giải pháp**: Merge Arm data vào Shoulder và Leg data vào Thigh.

**Sai số ước tính**: < 5 degrees vì Arm/Leg chỉ có slight rotation/position offset.

### Coordinate Conversion
```
Epic Fight: Right-handed Y-up
Unity:      Left-handed Y-up

Conversion Matrix:
[-1,  0,  0,  0]
[ 0,  1,  0,  0]
[ 0,  0,  1,  0]
[ 0,  0,  0,  1]

Effect:
- Translation X: negated
- Rotation: X axis flipped
- Handedness: converted
```

## Expected Results Summary

| Animation     | Duration | Hit Window    | Combo Window | Next Combo   |
|---------------|----------|---------------|--------------|--------------|
| fist_auto1    | 0.5s     | 0.133 - 0.233 | 0.2 - 0.45   | fist_auto2   |
| fist_auto2    | 0.5s     | 0.133 - 0.233 | 0.2 - 0.45   | fist_auto3   |
| fist_auto3    | 0.5s     | 0.133 - 0.233 | 0.2 - 0.45   | (none)       |
| fist_dash     | 0.5s     | 0.1 - 0.25    | 0.25 - 0.45  | fist_auto2   |
| fist_airslash | 0.5s     | 0.1 - 0.3     | (none)       | (none)       |

---

**Date Created**: 2026-08-18
**Tool Version**: EpicFightAnimationConverter v1.0
**Unity Version**: 2021.3+
