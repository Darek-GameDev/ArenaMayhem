# Huong Dan Cai Dat Shared Mode (V1)

Tai lieu nay huong dan ket noi cac script Fusion moi duoc them trong Assets/_Project/Scripts/Networking/.

## 1) Cai Dat Runner Trong Scene

1. Mo scene gameplay cua ban.
2. Dam bao chi co dung mot NetworkRunner dang hoat dong trong luong khoi tao scene.
3. Them SharedModeRunnerCallbacks vao cung object voi NetworkRunner (hoac bat ky object nao trong scene).
4. Gan cac truong trong SharedModeRunnerCallbacks:
   - Runner: runner cua scene.
   - Player Prefab: prefab nhan vat da network, co NetworkObject.
   - Player Input: object input local (co co che tu tim neu de trong).
   - Spawn Points: danh sach transform spawn (tuy chon).

## 2) Cai Dat Player Prefab

Tren player prefab duoc dung boi runner.Spawn(...):
1. Giu lai NetworkObject.
2. Them NetworkCharacterController.
3. Them SharedModePlayerController.
4. Them SharedModeAnimatorBridge.
5. Giu cac component combat hien co (Weapon, va script legacy neu can).

### Tuong Thich Voi Script Legacy
- PlayerMoment va PlayerAttack se tu ngung logic khi co SharedModePlayerController.
- PlayerHealth se chuyen xu ly damage sang health network khi co SharedModePlayerController.

## 3) Yeu Cau Input

Ten action ma SharedModeRunnerCallbacks mong doi trong action map cua PlayerInput:
- Move (Vector2)
- Look (Vector2)
- Jump (Button)
- Sprint (Button)
- Attack (Button)
- Block (Button)

Neu ten action khac, hay cap nhat cac chuoi ten trong SharedModeRunnerCallbacks.

## 4) Tham So Animator Duoc Su Dung

SharedModeAnimatorBridge se ghi vao cac tham so animator sau:
- Speed (float)
- ComboStep (int)
- isBlocking (bool)
- isMove (bool)
- isIdle (bool)
- isGrounded (bool)
- isFalling (bool)
- isDead (bool)
- Trigger: AttackSword, GetHit, BlockHit, DeadTrigger

## 5) Checklist Cau Hinh Fusion

- Dang ky player prefab trong danh sach network prefabs cua NetworkProjectConfig.
- Chay o che do GameMode.Shared.
- Dam bao tat ca peer deu load cung mot scene.

## 6) Kiem Tra Nhanh

1. Chay 2 peer trong Shared Mode.
2. Xac minh moi peer chi dieu khien dung nhan vat cua minh.
3. Kiem tra dong bo move/jump/sprint va cap nhat animation tu xa.
4. Kiem tra hanh vi tan cong va do don tu ca hai peer.
5. Xac nhan moi lan danh chi gay sat thuong 1 lan (khong bi lap khi dang overlap).
6. Xac nhan trang thai chet se khoa hanh dong.

## Gioi Han Da Biet Cua V1

- Xac thuc hit da di qua authority, nhung van dua tren trigger overlap (chua co lag compensation).
- API respawn da co trong controller (RPC_ResetAfterRespawn) nhung chua co respawn manager rieng.
