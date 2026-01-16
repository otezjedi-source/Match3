# Match3 Game

A classic match-3 puzzle game built with Unity using a hybrid ECS + MonoBehaviour architecture.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      UI Layer                           │
│         MonoBehaviour + UniRx (reactive binding)        │
├─────────────────────────────────────────────────────────┤
│                  Controllers Layer                      │
│     POCO classes with DI (business logic bridge)        │
├─────────────────────────────────────────────────────────┤
│                    ECS Layer                            │
│   Unity DOTS (game logic, grid state, matching)         │
└─────────────────────────────────────────────────────────┘
```

### Why Hybrid?

- **ECS** handles high-performance game logic (matching algorithms, grid operations, tile movement)
- **MonoBehaviour** manages visuals and animations (easier DOTween/Spine integration)
- **Controllers** bridge the two worlds, exposing reactive properties for UI

## Project Structure

```
Assets/Scripts/
├── Controllers/          # Business logic (input, audio, scoring, scenes)
├── Core/                 # DI containers, configuration, initialization
├── Data/                 # Data models (SaveData)
├── ECS/
│   ├── Authoring/       # Bakers for ECS conversion
│   ├── Components/      # Pure data components
│   └── Systems/         # Game logic systems
├── Factories/           # Object creation (tile pooling)
├── Game/                # Visual components (TileView)
├── Interfaces/          # Contracts (ISaveController)
├── UI/                  # Menu screens
└── Utils/               # Helpers (AssetHandle, AdjustCamera)
```

## Game Loop

```
[Idle] ←──────────────────────────────────┐
   │                                       │
   ▼ (player swaps tiles)                  │
[Swap] → swap animation                    │
   │                                       │
   ▼                                       │
[Match] → find matches                     │
   │         │                             │
   │         └─ no matches → revert swap ──┤
   ▼                                       │
[Clear] → destruction animation            │
   │                                       │
   ▼                                       │
[Fall] → tiles fall down                   │
   │                                       │
   ▼                                       │
[Fill] → spawn new tiles                   │
   │                                       │
   └─→ [Match] (check chain reactions)     │
          │                                │
          └─ no matches ───────────────────┘
```

## Dependencies

### Unity DOTS / Entities
Entity Component System for data-oriented game logic.

**Why:** Cache-friendly data layout and burst-compiled systems make grid operations fast. Matching algorithm benefits from contiguous memory access.

**Used in:** `ECS/Systems/`, `ECS/Components/`

---

### VContainer
Lightweight dependency injection container.

**Why:** Cleaner than Zenject, better performance, smaller footprint. Enables loose coupling and easy testing.

**Used in:** `Core/*LifetimeScope.cs`

```csharp
// Dependencies injected automatically
public class GameController : IDisposable
{
    [Inject] private readonly EntityManager entityManager;
    [Inject] private readonly ScoreController scoreController;
}
```

---

### UniRx
Reactive Extensions for Unity.

**Why:** Declarative event handling, eliminates callback hell. Perfect for connecting ECS state to UI.

**Used in:** Controllers, UI components

```csharp
// UI updates automatically when score changes
scoreController.Score
    .Subscribe(score => scoreText.text = $"Score: {score}")
    .AddTo(this);
```

---

### UniTask
Zero-allocation async/await.

**Why:** Proper async support without GC pressure. Essential for scene loading, asset loading, save operations.

**Used in:** Scene loading, save/load, asset loading

```csharp
await SceneManager.LoadSceneAsync(sceneName)
    .ToUniTask(cancellationToken: ct);
```

---

### DOTween
Animation engine for procedural tweening.

**Why:** Smooth tile animations (swap, drop). More flexible than Unity's animation system for runtime-generated animations.

**Used in:** `TileView.cs`

```csharp
DOTween.Sequence()
    .Append(sprite.transform.DOScale(squashScale, squashDuration))
    .Append(sprite.transform.DOScale(Vector3.one, recoverDuration));
```

---

### Spine Unity
2D skeletal animation runtime.

**Why:** Tile destruction effects. Spine provides smooth animations with smaller file sizes than sprite sheets.

**Used in:** `TileView.cs` for clear animations

---

### Addressables
Async asset management.

**Why:** On-demand loading reduces initial load time and memory. Tiles load sprites when needed.

**Used in:** `AssetHandle.cs`, `TileView.cs`

---

### Newtonsoft.Json
JSON serialization.

**Why:** More powerful than JsonUtility. Handles complex types and provides better error handling.

**Used in:** `SaveData.cs`

---

### Input System
New Unity input handling.

**Why:** Device-agnostic input, action-based design, better for multiple input methods.

**Used in:** `InputController.cs`

---

## Key Design Patterns

### Object Pooling
`TileFactory` maintains a pool of tile entities. Tiles are recycled instead of destroyed to avoid GC during gameplay.

### Reactive UI Binding
UI subscribes to `ReactiveProperty<T>` values. No manual update calls needed.

### Command Buffer Pattern
ECS structural changes go through `EntityCommandBuffer` for thread safety.

### Phase-Based State Machine
Explicit game phases (`Idle`, `Swap`, `Match`, `Clear`, `Fall`, `Fill`) ensure proper sequencing.

## Configuration

`GameConfig` ScriptableObject:

| Setting | Description | Default |
|---------|-------------|---------|
| GridWidth | Grid width | 5 |
| GridHeight | Grid height | 9 |
| MatchCount | Tiles for match | 3 |
| PointsPerTile | Score per tile | 10 |
| SwapDuration | Swap animation | 0.3s |
| FallDuration | Fall per cell | 0.3s |

## Building

1. Open in Unity 2022.3+ LTS
2. Resolve packages in Package Manager
3. Build for target platform
