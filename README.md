# Match3 — Puzzle Game on Unity ECS

A classic Match-3 puzzle game built with **Unity DOTS / ECS** architecture, featuring hybrid rendering, DI, reactive UI, and custom URP render features.

## Tech Stack

| Layer | Technology |
|---|---|
| **Architecture** | Unity ECS (Entities 1.x), Hybrid ECS (data in ECS, visuals in MonoBehaviour) |
| **DI** | VContainer (scoped lifetimes, constructor injection) |
| **Reactive** | UniRx (reactive properties, UI binding) |
| **Async** | UniTask (async/await, cancellation) |
| **Rendering** | URP, Custom ScriptableRendererFeature + RenderGraph API, GPU Instancing |
| **Shaders** | Shader Graph (procedural ocean floor effect with UV distortion, caustics, foam) |
| **Animation** | Spine, DOTween (squash & stretch) |
| **Assets** | Addressables (async loading, ref-counted handles) |
| **Serialization** | Newtonsoft.Json |
| **Performance** | Burst Compiler, IJobParallelFor, Object Pooling |

## Architecture Overview

The project follows a **hybrid ECS** approach: all game logic (matching, swapping, falling, scoring) runs in pure ECS systems, while visual representation (sprites, animations, UI) lives in MonoBehaviour land. A bridge layer (`TileViewSyncSystem`, `ScoreSyncSystem`, `SoundSyncSystem`) synchronizes the two worlds every frame.

```
┌─────────────────────────────────────────────────┐
│                   VContainer DI                 │
│  BootLifetimeScope → StartLifetimeScope         │
│                    → GameLifetimeScope          │
├─────────────────────────────────────────────────┤
│              ECS (Data + Logic)                 │
│  Components: TileData, GridCell, GameState, ... │
│  Systems:    Swap → Match → Clear → Fall → Fill │
├─────────────────────────────────────────────────┤
│           Sync Layer (ECS → MonoBehaviour)      │
│  TileViewSyncSystem, ScoreSyncSystem,           │
│  SoundSyncSystem                                │
├─────────────────────────────────────────────────┤
│            MonoBehaviour (Visuals + UI)         │
│  TileView, GameUI, MenuStart, MenuGameOver,     │
│  BubbleRenderer, LoadingScreen                  │
│  Shader Graph: Ocean floor (distortion,         │
│                caustics, foam)                  │
└─────────────────────────────────────────────────┘
```

## Game Loop State Machine

```
         ┌──────────────────────────────────────┐
         ▼                                      │
      [Idle] ──drag──▶ [Swap] ──done──▶ [Match] │
         ▲                                 │    │
         │                          match? │    │
         │                         ┌──yes──┘    │
         │                         ▼            │
         │                      [Clear] ──▶  [Fall] ──▶ [Fill]─┐
         │                                                     │
         └──────────── no matches, idle ◀──────────────────────┘
         │
    no possible moves
         ▼
     [GameOver]
```

Each phase is an enum value in `GameState.phase`. Systems check the current phase and only execute when relevant, forming a clean implicit state machine without `switch` statements scattered across the codebase.

## Project Structure

```
Assets/Scripts/
├── Bubbles/              # Custom URP render feature (GPU-instanced bubbles)
│   ├── BubbleRenderFeature.cs    # ScriptableRendererFeature + RenderGraph pass
│   ├── BubbleRenderer.cs         # MonoBehaviour managing bubble lifecycle
│   └── BubbleSettings.cs         # ScriptableObject config (iridescence, pop, wobble)
│
├── Controllers/          # Application-level services
│   ├── GameController.cs         # ECS ↔ app bridge (reactive game state)
│   ├── InputController.cs        # New Input System → PlayerSwapRequest entities
│   ├── ScoreController.cs        # Score + high score persistence (debounced save)
│   ├── SoundController.cs        # One-shot SFX playback
│   ├── LoadingController.cs      # Ref-counted loading operations
│   ├── SceneLoader.cs            # Async scene loading with DI scope propagation
│   └── PlayerPrefsSaveController.cs  # ISaveController implementation
│
├── Core/                 # Bootstrap and configuration
│   ├── BootLifetimeScope.cs      # Root DI container (DontDestroyOnLoad)
│   ├── GameLifetimeScope.cs      # Game scene DI (scoped, destroyed on unload)
│   ├── StartLifetimeScope.cs     # Start menu scene DI
│   ├── GameInitializer.cs        # Game scene entry point (IStartable + ITickable)
│   ├── StartInitializer.cs       # Start scene entry point
│   └── GameConfig.cs             # Central config SO with editor + runtime validation
│
├── Data/                 # Data layer
│   ├── DataCache.cs              # Lookup cache for tile/bonus configs
│   ├── TileTypeRegistry.cs       # Available tile types for random spawning
│   └── SaveData.cs               # Serializable save model
│
├── ECS/                  # Entity Component System
│   ├── Authoring/                # Baking (GameConfigAuthoring → ECS components)
│   ├── Components/
│   │   ├── TileComponents.cs         # TileData, TileMove, TileState, BonusType, tags
│   │   ├── GridComponents.cs         # GridCell buffer, type cache, match groups
│   │   ├── GameStateComponents.cs    # GamePhase, SwapRequest, ScoreEvent
│   │   ├── ConfigComponents.cs       # GridConfig, MatchConfig, TimingConfig
│   │   ├── ManagedComponents.cs      # TileViewData, ManagedReferences
│   │   └── SoundComponents.cs        # PlaySoundRequest, SoundType
│   └── Systems/
│       ├── GridInitSystem.cs          # Grid creation + match-free generation
│       ├── GridCacheSystem.cs         # Burst-compiled type cache rebuild
│       ├── GridResetSystem.cs         # Cleanup for restart
│       ├── SwapSystem.cs             # Input processing → tile swap
│       ├── MatchSystem.cs            # Line-scan match detection + bonus combos
│       ├── BonusDetectSystem.cs      # Match count → bonus type mapping
│       ├── BonusActivateSystem.cs    # Bonus effect execution (line, bomb, cross)
│       ├── BonusInitSystem.cs        # Bonus assignment to tiles
│       ├── ClearSystem.cs            # Clear animation + score events
│       ├── FallSystem.cs             # Gravity (column-wise drop)
│       ├── FillSystem.cs             # New tile spawning above grid
│       ├── TileMoveSystem.cs         # Burst parallel movement interpolation
│       ├── TileViewSyncSystem.cs     # ECS position → Transform sync
│       ├── ScoreSyncSystem.cs        # ScoreEvent → ScoreController
│       ├── SoundSyncSystem.cs        # PlaySoundRequest → SoundController
│       ├── PossibleMovesSystem.cs    # Game over detection (brute-force swap check)
│       └── SystemGroups.cs           # GameInitSystemGroup, GameSystemGroup, GameSyncSystemGroup
│
├── Factories/            # Object creation and pooling
│   ├── TileFactory.cs            # Create/return tiles (ECS entity + TileView)
│   ├── TilePool.cs               # Queue-based entity pool
│   └── TileViewInitializer.cs    # Async Addressables loading for tile visuals
│
├── Game/                 # Visual representation
│   └── TileView.cs               # Sprite + Spine animation + squash & stretch
│
├── UI/                   # User interface
│   ├── GameUI.cs                 # Menu state management
│   ├── MenuStart.cs              # Start screen (play, high score reset, quit)
│   ├── MenuGame.cs               # In-game HUD (score, back)
│   ├── MenuGameOver.cs           # Game over (score, new high score, restart)
│   └── LoadingScreen.cs          # CanvasGroup-based loading overlay
│
├── Utils/                # Utilities
│   ├── AdjustCamera.cs           # Orthographic camera auto-fit
│   └── AssetHandle.cs            # Addressables wrapper (load, cache, release)
│
└── Interfaces/
    └── ISaveController.cs        # Save/load abstraction
```

## Key Technical Decisions

### Hybrid ECS

Game logic benefits from ECS data locality and Burst compilation: match detection, grid cache rebuild, and tile movement run as parallel jobs. Meanwhile, Unity's rendering, Spine animations, and UI toolkit remain in MonoBehaviour. The sync layer is intentionally thin — three small systems that fire events and copy positions.

### Dependency Injection (VContainer)

Three-tier scope hierarchy mirrors the application lifecycle:

- **BootLifetimeScope** — global singletons (audio, save, scene loader). Survives scene loads via `DontDestroyOnLoad`.
- **StartLifetimeScope** — start menu. Disposed on scene unload.
- **GameLifetimeScope** — game session services (input, grid, factory). Scoped lifetime ensures clean teardown.

`LifetimeScope.EnqueueParent` propagates the parent scope during async scene loading.

### ECS ↔ Managed Bridge (ManagedReferences)

ECS systems are unmanaged and can't directly access MonoBehaviour services. A singleton `ManagedReferences` component holds references to `ScoreController`, `SoundController`, `TileFactory`, etc. — created once by `GameInitializer` and queried by sync systems. This keeps ECS logic pure while allowing controlled escape hatches to the managed world.

### Structural Change Avoidance (IEnableableComponent)

`TileMove` implements `IEnableableComponent` — instead of adding/removing the component each time a tile starts or stops moving, the system toggles `SetComponentEnabled`. This avoids costly archetype changes on every swap, fall, and fill, which on a 5×9 grid with cascades would mean dozens of structural changes per frame.

### Config Baking

`GameConfig` (ScriptableObject) is baked via `GameConfigBaker` into separate ECS components — `GridConfig`, `MatchConfig`, `TimingConfig`, `BonusConfig` buffer. This lets systems query only the config slice they need, and the data lives alongside other ECS data in the same cache lines.

### Bonus System

Bonuses are data-driven via `BonusConfig` buffer baked from `GameConfig`. The pipeline:

1. **BonusDetectSystem** — maps match groups to bonus types based on count and shape (line vs non-line).
2. **BonusInitSystem** — attaches `TileBonusData` component to the target tile.
3. **BonusActivateSystem** — on clear, applies spatial effects (row/column/area clear).
4. **MatchSystem.TryBonusCombo** — swapping two bonus tiles produces combo types (Cross, BombHorizontal, BigBomb, etc.).

### Bubble Render Feature

Decorative bubbles are rendered via a custom `ScriptableRendererFeature` using the RenderGraph API. Bubbles are GPU-instanced (`DrawMeshInstanced`) with per-instance data (age, color seed, size variation) passed via `MaterialPropertyBlock`-style `SetVectorArray`. This avoids creating GameObjects per bubble.

### Ocean Floor Shader (Shader Graph)

The game background uses a Sprite Unlit Shader Graph that simulates an underwater view of a sandy floor. Three procedural layers are composited over the original sprite texture:

1. **UV distortion** — Gradient Noise offsets the texture UV coordinates over time, creating a water refraction effect.
2. **Caustics** — Animated Voronoi noise (with `Angle Offset` driven by `Time`) generates light patterns on the seabed. `Power` node sharpens the highlights.
3. **Foam lines** — `Dot Product` projects UV onto a direction vector, fed through `Sine` → `Smoothstep` to produce moving foam strips. The same sine wave goes through `Remap` to darken wave troughs.

All layers are combined additively over the shadow-modulated sprite and clamped with `Saturate`.

### Object Pooling

`TileFactory` + `TilePool` reuse both ECS entities and their associated `TileView` GameObjects. On clear, tiles are deactivated and returned to the queue. On spawn, they're reactivated and reinitialized. This eliminates GC pressure during gameplay.

### Async Asset Loading

Tile sprites and Spine skeleton data are loaded via Addressables. `AssetHandle<T>` wraps the loading with GUID-based caching, cancellation token support, and proper `Addressables.Release` on cleanup. `TileViewInitializer` batches all pending loads and exposes `WaitAllAsync` for the loading screen.

## Game Features

- **Grid generation** — random placement with constraint: no pre-existing matches, at least one valid move guaranteed.
- **Drag-to-swap** — New Input System, configurable minimum drag distance.
- **Match detection** — line-scan algorithm, supports 3+ matches in rows and columns.
- **Bonuses** — horizontal/vertical line, bomb (3×3), and combo variants (cross, big bomb).
- **Cascade** — after clearing, tiles fall, new tiles spawn, re-check for matches (chain reactions).
- **Game over** — brute-force check of all possible swaps. Cached and invalidated only on grid changes.
- **Score persistence** — debounced auto-save via `PlayerPrefs` (swappable via `ISaveController`).
- **Squash & stretch** — DOTween sequence on tile landing for tactile feel.
- **Spine clear animations** — per-tile-type destruction effects.
- **Ocean floor background** — procedural Shader Graph effect (UV distortion, caustics, foam) applied over a sand sprite.

## How to Run

1. Open the project in Unity (2022.3+ with URP).
2. Ensure packages are resolved: Entities, VContainer, UniTask, UniRx, DOTween, Spine-Unity, Addressables, Newtonsoft.Json.
3. Open `BootScene` and press Play. The game loads `StartScene` automatically.
