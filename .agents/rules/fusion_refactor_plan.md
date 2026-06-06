# Fusion Multiplayer — Refactor & Bug Fix Plan

> Project: Cooking Game | Network: Photon Fusion 2 | Mode: Host/Client

---

## Phase 1 — Fix Lag (Ưu tiên cao nhất)

### 1.1 Đổi NetworkTransform → NetworkRigidbody3D
**File:** `Player prefab (Inspector)`

- [ ] Xóa component `NetworkTransform` trên Player prefab
- [ ] Thêm component `NetworkRigidbody3D`
- [ ] Set `Rigidbody.interpolation = None` (NetworkRigidbody3D tự xử lý interpolation)

---

### 1.2 Fix `FixedUpdateNetwork` — chỉ chạy trên StateAuthority
**File:** `Player.cs`

```csharp
public override void FixedUpdateNetwork()
{
    if (!HasStateAuthority) return;

    if (GetInput(out NetworkInputData inputData))
    {
        _moveInput = Vector2.ClampMagnitude(
            new Vector2(inputData.MoveX, inputData.MoveY), 1f
        );
        NetworkMoveInput = _moveInput;
    }

    Move();
    HandleInteractions(inputData);

    if (_moveInput != Vector2.zero && _isCutting)
        StopCutting();

    if (_isCutting && _currentCuttingCounter != null)
        _currentCuttingCounter.InteractAlternate(this);
}
```

**Lý do:** Client có `HasInputAuthority` nhưng không có `HasStateAuthority` → không set được `NetworkMoveInput` → host không biết hướng di chuyển → prediction sai → giật.

---

### 1.3 Bỏ Slerp trong `Move()`, dùng snap rotation
**File:** `Player.cs`

```csharp
private void Move()
{
    if (_rb == null) return;
    Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

    if (moveDir != Vector3.zero)
    {
        _rb.velocity = new Vector3(moveDir.x * moveSpeed, _rb.velocity.y, moveDir.z * moveSpeed);
        _lastInteractDir = moveDir.normalized;
        NetworkTargetForward = _lastInteractDir;

        // Snap rotation — NetworkRigidbody3D sẽ interpolate giữa các tick
        _rb.rotation = Quaternion.LookRotation(_lastInteractDir);
    }
    else
    {
        _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
    }
}
```

- [ ] Xóa field `_currentRotation`
- [ ] Xóa toàn bộ Slerp logic cũ

---

## Phase 2 — Fix Bug Gameplay

### 2.1 Unsubscribe event khi despawn
**File:** `Player.cs`

```csharp
public override void Despawned(NetworkRunner runner, bool hasState)
{
    StopCutting(); // StopCutting đã unsubscribe OnCutComplete bên trong
}
```

**Lý do:** Nếu player bị despawn trong khi đang cutting, `OnCutComplete` vẫn fire → NullReferenceException.

---

### 2.2 Sync animation trigger `IsPicked` qua network
**File:** `Player.cs`

```csharp
// Thêm networked property:
[Networked]
[OnChangedRender(nameof(OnPickedChanged))]
public NetworkBool NetworkIsPicked { get; set; }

private void OnPickedChanged() => animator.SetTrigger(s_IsPicked);

// Trong HandleInteractions — thay SetTrigger trực tiếp bằng:
// animator.SetTrigger("IsPicked");  ❌
NetworkIsPicked = !NetworkIsPicked;  // ✅ toggle để trigger OnChangedRender
```

**Lý do:** `SetTrigger` chỉ chạy trên StateAuthority → remote client không thấy animation pick-up.

---

### 2.3 Validate input phía host
**File:** `Player.cs`

```csharp
_moveInput = Vector2.ClampMagnitude(
    new Vector2(inputData.MoveX, inputData.MoveY), 1f
);
```

**Lý do:** Tránh client gian lận gửi giá trị > 1 → velocity bất thường.

---

## Phase 3 — Performance

### 3.1 Cache Material reference
**File:** `Player.cs`

```csharp
private Material _bodyMaterial;

public override void Spawned()
{
    if (bodyRend != null && bodyRend.materials.Length > 1)
        _bodyMaterial = bodyRend.materials[1];
    // ... rest of Spawned
}

public void UpdateVisualColor(EPlayerColor color)
{
    if (_bodyMaterial == null) return;
    _bodyMaterial.color = GetColorByEnum(color);
    // Không cần set lại bodyRend.materials
}
```

**Lý do:** `bodyRend.materials` clone toàn bộ array mỗi lần gọi → GC pressure mỗi khi màu thay đổi.

---

### 3.2 Cache Animator hash
**File:** `Player.cs`

```csharp
// Thêm static fields:
private static readonly int s_MovingValue = Animator.StringToHash("MovingValue");
private static readonly int s_IsChopping  = Animator.StringToHash("IsChopping");
private static readonly int s_HasObject   = Animator.StringToHash("HasObject");
private static readonly int s_IsPicked    = Animator.StringToHash("IsPicked");

// Dùng trong UpdateAnimation():
animator.SetFloat(s_MovingValue, magnitude);
animator.SetBool(s_IsChopping, NetworkIsChopping);
animator.SetBool(s_HasObject, NetworkIsHoldingObject);
```

**Lý do:** String lookup trong Animator mỗi frame tốn CPU không cần thiết.

---

### 3.3 Đổi MoveX/MoveY sang sbyte
**File:** `NetworkInputData.cs`

```csharp
public struct NetworkInputData : INetworkInput
{
    public sbyte MoveX; // -1, 0, 1 thay vì float
    public sbyte MoveY;
    public byte Buttons;
    // ...
}
```

**File:** `NetworkInputHandler.cs`

```csharp
data.MoveX = (sbyte)Mathf.RoundToInt(x);
data.MoveY = (sbyte)Mathf.RoundToInt(y);
```

**Lý do:** Tiết kiệm 7 bytes/tick/player → ~420 bytes/s/player ở tick rate 60.

---

## Phase 4 — Code Structure

### 4.1 LobbyPlayer registry — thay FindObjectsOfType
**File:** Tạo mới `LobbyPlayerRegistry.cs`

```csharp
public static class LobbyPlayerRegistry
{
    private static readonly List<LobbyPlayer> _all = new();
    public static IReadOnlyList<LobbyPlayer> All => _all;
    public static void Register(LobbyPlayer lp)   => _all.Add(lp);
    public static void Unregister(LobbyPlayer lp) => _all.Remove(lp);
}
```

**File:** `LobbyPlayer.cs`

```csharp
public override void Spawned()   => LobbyPlayerRegistry.Register(this);
public override void Despawned() => LobbyPlayerRegistry.Unregister(this);
```

**File:** `FusionNetworkRunner.cs`

```csharp
// Thay:
var lobbyPlayers = FindObjectsOfType<LobbyPlayer>(); // ❌
// Bằng:
var lobbyPlayers = LobbyPlayerRegistry.All;          // ✅
```

---

### 4.2 Đổi async void → async Task
**File:** `FusionNetworkRunner.cs`

```csharp
public async Task StartGameSession(GameMode mode, string sessionName) { ... }
public async Task JoinLobby() { ... }
public async Task LeaveSession() { ... }
```

---

### 4.3 Thêm guard chống race condition
**File:** `FusionNetworkRunner.cs`

```csharp
private bool _isStarting;

public async Task StartGameSession(GameMode mode, string sessionName)
{
    if (_isStarting) return;
    _isStarting = true;
    try
    {
        // ... logic hiện tại
    }
    finally
    {
        _isStarting = false;
    }
}
```

---

### 4.4 Magic strings → constants
**File:** `FusionNetworkRunner.cs`

```csharp
private const string SCENE_GAME      = "GameScene";
private const string SCENE_MAIN_MENU = "MainMenuScene";
private const string SCENE_LOBBY     = "LobbyScene";
```

---

### 4.5 Tách OnSceneLoadDone thành các method nhỏ
**File:** `FusionNetworkRunner.cs`

```csharp
public void OnSceneLoadDone(NetworkRunner runner)
{
    string sceneName = SceneManager.GetActiveScene().name;
    if      (sceneName == SCENE_GAME)      HandleGameSceneLoaded(runner);
    else if (sceneName == SCENE_MAIN_MENU) HandleMenuSceneLoaded(runner);
    else if (sceneName == SCENE_LOBBY)     HandleMenuSceneLoaded(runner);
    else Debug.Log($"[FusionNetworkRunner] Unhandled scene: {sceneName}");
}

private void HandleGameSceneLoaded(NetworkRunner runner) { ... }
private void HandleMenuSceneLoaded(NetworkRunner runner) { ... }
```

---

## Phase 5 — Nice to Have

| # | Việc cần làm | File | Lý do |
|---|---|---|---|
| 5.1 | `EPlayerColor` → `Dictionary<EPlayerColor, Color>` | `Player.cs` | Tránh index lệch enum/array |
| 5.2 | `DestroyImmediate` → `Destroy` | `FusionNetworkRunner.cs` | DestroyImmediate không dùng trong runtime |
| 5.3 | Tách `HandleInteractions` thành 2 method | `Player.cs` | SRP, dễ đọc |
| 5.4 | SpawnPoint array thay magic number | `FusionNetworkRunner.cs` | Linh hoạt khi thêm map |
| 5.5 | Keybinding system thay hardcode KeyCode | `NetworkInputHandler.cs` | Dễ remap |

---

## Thứ tự thực hiện

```
Phase 1  →  Build + test lag ngay lập tức
Phase 2  →  Test gameplay đầy đủ
Phase 3  →  Profiler check memory/CPU
Phase 4  →  Code review / cleanup
Phase 5  →  Cuối sprint hoặc khi có thời gian
```

---

## Files cần chỉnh sửa

| File | Phase |
|------|-------|
| `Player prefab` (Inspector) | 1.1 |
| `Player.cs` | 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 5.3 |
| `NetworkInputData.cs` | 3.3 |
| `NetworkInputHandler.cs` | 3.3, 5.5 |
| `FusionNetworkRunner.cs` | 4.2, 4.3, 4.4, 4.5, 5.2, 5.4 |
| `LobbyPlayer.cs` | 4.1 |
| `LobbyPlayerRegistry.cs` *(tạo mới)* | 4.1 |
