# 🎨 TakoBoyStudios.Animation

A powerful and flexible sprite animation system for Unity with built-in editor tools, pooling integration, and a streamlined workflow.

## ✨ Features

- 🎬 **Frame-by-frame sprite animation** for both SpriteRenderer and UI Image components
- 🎮 **Editor preview player** with play/pause/stop controls and frame scrubbing
- 🔄 **Multiple loop modes**: No loop, loop to start, loop to specific frame
- ⚡ **Performance optimized** with object pooling support
- 🎯 **Smart naming convention** for automatic animation creation
- ⏱️ **Flexible timing**: JSON-based frame timing or fixed frame rates
- 🔧 **Powerful editor tools** for sprite processing and slicing
- 📦 **Optional Odin Inspector** integration with proper defines

## 📦 Installation

### Via Unity Package Manager

1. Open Unity Package Manager (Window → Package Manager)
2. Click the '+' button and select "Add package from git URL..."
3. Enter the repository URL or local path
4. Unity will automatically import the package

### Via Git URL

Add this line to your `manifest.json` in the `Packages` folder:

```json
{
  "dependencies": {
    "com.takoboystudios.animation": "https://github.com/takoboystudios/com.takoboystudios.animation.git",
    "com.takoboystudios.core": "https://github.com/takoboystudios/com.takoboystudios.core.git"
  }
}
```

### Via Local Package

1. Clone or download this repository
2. In Unity Package Manager, click '+' → "Add package from disk..."
3. Select the `package.json` file from the repository

### Dependencies

**Required:**
- Unity 2020.3 or later
- com.takoboystudios.core (for PoolManager integration)

**Optional:**
- Odin Inspector (automatically detected - no manual define needed)

### Initial Setup

After installation:
1. Configure your animation save path: `Assets → TakoBoy Studios → Animation → Configure Save Path`
2. Configure your sprites PPU: `Assets → TakoBoy Studios → Sprites → Configure PPU`

The package automatically sets up the `ODIN_INSPECTOR` define if Odin Inspector is detected.

## 🚀 Quick Start

### Creating Your First Animation

#### Step 1: Prepare Your Sprite Sheet

Name your sprite sheet using the convention:
```
asset-name@animation-name_WIDTHxHEIGHT.png
```

**Naming rules:**
- All lowercase
- Words separated by hyphens (-)
- Only exception is the underscore before dimensions

**Examples:**
- `player@idle-loop_32x32.png` → Creates "player" asset with "idle-loop" animation
- `enemy@attack_64x64.png` → Creates "enemy" asset with "attack" animation
- `coin@spin-loop_16x16.png` → Creates "coin" asset with "spin-loop" animation
- `fire-ball@explode_32x32.png` → Creates "fire-ball" asset with "explode" animation

#### Step 2: Process the Sprite

1. Select your sprite in the Project window
2. Right-click → `TakoBoy Studios → Sprites → Slice Sprite Sheet`

This will:
- Set proper import settings (PPU, filter mode, etc.)
- Slice the sprite sheet into individual frames
- Skip empty frames automatically

#### Step 3: Create the Animation

1. Select your processed sprite
2. Right-click → `TakoBoy Studios → Animation → Create Animation from Selected`

This will:
- Create or update the SpriteAnimationAsset
- Add the animation with all frames
- Auto-detect loop mode from name (contains "loop")
- Load frame timing from JSON if available

#### Step 4: Use the Animation

1. Create a GameObject with a SpriteRenderer or Image component
2. Add the `SpriteAnimation` component
3. Assign your SpriteAnimationAsset
4. Select the animation from the dropdown
5. Hit Play!

## 📖 Core Components

### SpriteAnimation

The main animation player component.

```csharp
using TakoBoyStudios.Animation;

public class CharacterController : MonoBehaviour
{
    private SpriteAnimation animator;
    
    void Start()
    {
        animator = GetComponent<SpriteAnimation>();
        
        // Play an animation
        animator.Play("idle_loop");
        
        // Play with callback
        animator.Play("attack", OnAttackComplete);
        
        // Play with frame callback
        animator.Play("walk", OnFrameUpdate);
        
        // Control playback
        animator.Pause(true);
        animator.Stop();
        animator.SetReverse(true);
    }
    
    void OnAttackComplete()
    {
        Debug.Log("Attack animation finished!");
    }
    
    void OnFrameUpdate(int frame)
    {
        Debug.Log($"Now on frame: {frame}");
    }
}
```

**Public Properties:**
- `CurrentAnimationName` - Name of currently playing animation
- `CurrentFrame` - Current frame index
- `CurrentFrameCount` - Total frames in current animation
- `IsDone` - Is animation complete?
- `playing` - Is currently playing?
- `paused` - Is paused?
- `speedRatio` - Speed multiplier (1.0 = normal)

**Public Methods:**
- `Play(string animName)` - Play animation by name
- `Play(string animName, Action callback)` - Play with completion callback
- `Play(string animName, Action<int> frameCallback)` - Play with frame update callback
- `Pause(bool pause)` - Pause/unpause
- `Stop()` - Stop playback
- `SetReverse(bool reverse)` - Set reverse playback
- `HasAnimation(string name)` - Check if animation exists
- `GetAnimationData(string name)` - Get animation data

### SpriteAnimationAsset

ScriptableObject containing animation data.

```csharp
using TakoBoyStudios.Animation;

// Create via menu or script
var asset = ScriptableObject.CreateInstance<SpriteAnimationAsset>();

// Add animation programmatically
var animData = new SpriteAnimationData();
animData.name = "jump";
animData.loop = SpriteAnimationLoopMode.NOLOOP;
animData.frameDatas = new List<SpriteAnimationFrameData>();

asset.AddAnimation(animData);

// Query animations
if (asset.HasAnimation("jump"))
{
    var data = asset.GetAnimation("jump");
    Debug.Log($"Jump has {data.frameDatas.Count} frames");
}
```

### DestroyAfterAnimation

Auto-destroys or returns object to pool after animation completes.

```csharp
using TakoBoyStudios.Animation;

// Attach to GameObject with SpriteAnimation
var destroyer = gameObject.AddComponent<DestroyAfterAnimation>();
destroyer.m_animation = "explosion";

// Play and auto-cleanup
destroyer.Play();

// Listen for completion before cleanup
destroyer.OnAnimationComplete += (go) => {
    Debug.Log("Animation done, about to cleanup!");
};
```

**Smart Pool Detection:**
- Automatically detects if object is from PoolManager
- Returns to pool instead of destroying
- No manual configuration needed!

### SpriteCopier

Copies sprites between renderers in real-time.

```csharp
using TakoBoyStudios.Animation;

// Setup
var copier = shadowObject.AddComponent<SpriteCopier>();
copier.SetSource(characterRenderer);

// Now shadowObject will always match characterRenderer's sprite
```

**Use Cases:**
- Character shadows
- UI mirrors
- Reflection effects
- Multi-layer rendering

### SpriteMaskAnimationCopier

Copies sprite to SpriteMask for animated masking.

```csharp
using TakoBoyStudios.Animation;

// Attach to GameObject with SpriteMask
var maskCopier = maskObject.AddComponent<SpriteMaskAnimationCopier>();
maskCopier.m_renderer = characterRenderer;

// Mask now follows character animation
```

## 🎬 Editor Features

### Animation Preview in Inspector

The SpriteAnimation component inspector includes a full preview player!

**Features:**
- ▶️ Play/Pause/Stop controls
- 🎚️ Frame scrubber for manual control
- ⚡ Speed control slider
- 🔄 Loop toggle
- 📊 Real-time frame/time display
- ✨ Works in Edit Mode!

**Usage:**
1. Select GameObject with SpriteAnimation
2. Choose an animation from dropdown
3. Use preview controls to test
4. Scrub through frames manually
5. Adjust speed to test timing

### Sprite Processing Tools

#### Process Sprite
`Assets → TakoBoy Studios → Sprites → Process Sprite`

Sets optimal import settings for pixel art:
- Configurable Pixels Per Unit (default 32)
- Point filter mode
- No mipmaps
- Uncompressed
- Bottom-center pivot

#### Slice Sprite Sheet
`Assets → TakoBoy Studios → Sprites → Slice Sprite Sheet`

Automatically slices sprite sheets:
- Processes import settings first
- Uses filename convention: `Name_WxH.png`
- Skips empty frames
- Sets proper pivots
- Uses modern Unity APIs

**Example:**
- `Character_32x32.png` → Slices into 32x32 frames
- `Enemy_64x64.png` → Slices into 64x64 frames

#### GIF to Sprite Sheet Converter

A standalone Processing application to convert animated GIFs to sprite sheets.

**Location:** `GifToSpriteSheet/` folder

**Available versions:**
- `windows-amd64.zip` - Windows x64
- `macos-aarch64.zip` - macOS Apple Silicon

**How to use:**
1. Extract the appropriate ZIP file for your platform
2. Run the application (double-click the executable/app)
3. Drop your GIF file(s) into the application window
4. Output files are created in the same folder as the input GIF

**Input naming convention:**
```
asset-name@animation-name.gif
```

**Naming rules:**
- All lowercase
- Words separated by hyphens (-)
- No dimensions in the GIF filename

**Outputs:**
- `asset-name@animation-name_WxH.png` - Sprite sheet with all frames
- `asset-name@animation-name.json` - Frame timing data in milliseconds

**Examples:**
- Input: `player@run-loop.gif`
- Outputs: `player@run-loop_32x32.png` + `player@run-loop.json`
- Input: `fire-ball@explode.gif`
- Outputs: `fire-ball@explode_64x64.png` + `fire-ball@explode.json`

The generated sprite sheet and JSON can be directly imported into Unity and used with the animation workflow.

### Animation Creation Workflow

#### Automatic Creation
`Assets → TakoBoy Studios → Animation → Create Animation from Selected`

Creates animations from sprites using naming convention:

**Format:** `asset-name@animation-name_WxH.png`

**Naming rules:**
- All lowercase
- Words separated by hyphens (-)
- Only exception is the underscore before dimensions

**Examples:**
```
player@idle-loop_32x32.png
player@walk-loop_32x32.png
player@attack_32x32.png
enemy@idle_64x64.png
fire-ball@explode_64x64.png
```

**Features:**
- Creates/updates SpriteAnimationAsset
- Auto-detects loop mode (contains "loop")
- Loads timing from JSON if available
- Batch processes multiple sprites
- Shows success/fail report

#### JSON Frame Timing

For custom frame timing, create a JSON file with the same base name:

**Sprite:** `player@attack_32x32.png`
**JSON:** `player@attack.json`

```json
{
    "frames": [50, 50, 100, 50, 50]
}
```

Values are in milliseconds. If JSON is present, it overrides default timing.

### Configuration

#### Configure Save Path
`Assets → TakoBoy Studios → Animation → Configure Save Path`

Sets where SpriteAnimationAssets are saved (default: `Assets/Animations/`)

#### Configure PPU
`Assets → TakoBoy Studios → Sprites → Configure PPU`

Sets Pixels Per Unit for sprite processing (default: 32)

## 🎯 Advanced Usage

### Custom Animation Control

```csharp
using TakoBoyStudios.Animation;

public class AdvancedController : MonoBehaviour
{
    private SpriteAnimation animator;
    
    void Start()
    {
        animator = GetComponent<SpriteAnimation>();
        
        // Play from specific frame
        animator.Play("walk", startFrame: 5);
        
        // Play in reverse
        animator.Play("walk", reverse: true);
        
        // Manual frame control
        animator.SetCurrentAnimation("idle", editor: false, frame: 0);
        animator.CurrentFrame = 3;
        
        // Speed control
        animator.speedRatio = 2.0f; // Double speed
        animator.overrideTimeScale = true;
        animator.timeScaleOverride = 0.5f; // Slow motion
        
        // Query state
        bool isDone = animator.IsDone;
        int currentFrame = animator.CurrentFrame;
        float animLength = animator.GetCurrentAnimationLength();
    }
}
```

### Event-Driven Animation

```csharp
using TakoBoyStudios.Animation;

public class EventDrivenPlayer : MonoBehaviour
{
    private SpriteAnimation animator;
    
    void Start()
    {
        animator = GetComponent<SpriteAnimation>();
        
        // Subscribe to events
        animator.AnimationComplete += OnAnimComplete;
        animator.AnimationFrameUpdate += OnFrameUpdate;
        
        animator.Play("attack");
    }
    
    void OnAnimComplete()
    {
        Debug.Log("Animation completed!");
        animator.Play("idle");
    }
    
    void OnFrameUpdate(int frame)
    {
        if (frame == 3) // Attack hit frame
        {
            DealDamage();
        }
    }
    
    void OnDestroy()
    {
        // Clean up events
        if (animator != null)
        {
            animator.AnimationComplete -= OnAnimComplete;
            animator.AnimationFrameUpdate -= OnFrameUpdate;
        }
    }
}
```

### Pooling Integration

```csharp
using TakoBoyStudios.Animation;
using TakoBoyStudios.Core;

public class VFXSpawner : MonoBehaviour
{
    public SpriteAnimationAsset vfxAnimations;
    
    void SpawnEffect(Vector3 position)
    {
        // Get from pool
        var effect = PoolManager.Instance.Get("explosion_vfx", position, Quaternion.identity);
        
        // Setup animation
        var destroyer = effect.GetComponent<DestroyAfterAnimation>();
        if (destroyer == null)
        {
            destroyer = effect.AddComponent<DestroyAfterAnimation>();
            destroyer.m_animation = "explosion";
        }
        
        // Play - will auto-return to pool when done!
        destroyer.Play();
    }
}
```

### State Machine Integration

```csharp
using TakoBoyStudios.Animation;
using TakoBoyStudios.Core;

public class CharacterStateMachine : MonoBehaviour
{
    private SpriteAnimation animator;
    private Fsm<State> fsm;
    
    enum State { Idle, Walk, Attack, Dead }
    
    void Start()
    {
        animator = GetComponent<SpriteAnimation>();
        fsm = new Fsm<State>();
        
        // Setup states
        fsm.AddState(State.Idle, EnterIdle, null, null);
        fsm.AddState(State.Walk, EnterWalk, null, null);
        fsm.AddState(State.Attack, EnterAttack, null, ExitAttack);
        
        fsm.SetState(State.Idle);
    }
    
    void EnterIdle()
    {
        animator.Play("idle_loop");
    }
    
    void EnterWalk()
    {
        animator.Play("walk_loop");
    }
    
    void EnterAttack()
    {
        animator.Play("attack", OnAttackComplete);
    }
    
    void ExitAttack()
    {
        // Attack animation stopped early
    }
    
    void OnAttackComplete()
    {
        fsm.SetState(State.Idle);
    }
}
```

## 🔧 Odin Inspector Integration

If you have Odin Inspector installed, additional features are automatically enabled:

### SpriteCopier Enhanced Inspector

Shows/hides fields based on `isSpriteRenderer` toggle for cleaner UI.

### EditorAnimationPlayer Button

Test animations with a single button click in the inspector.

**Setup:**
The package automatically detects Odin Inspector through version defines in the assembly definitions. No manual setup required!

## 📝 Naming Convention Reference

### Sprite Sheet Naming

```
asset-name@animation-name_WIDTHxHEIGHT.png
```

**Naming Rules:**
- **All lowercase** - No capital letters allowed
- **Hyphens for word separation** - Use `-` to separate words in both asset and animation names
- **Underscore before dimensions** - Only `_` allowed is right before `WIDTHxHEIGHT`

**Parts:**
- `asset-name`: Name of the SpriteAnimationAsset (e.g., "player", "fire-ball", "coin")
- `@`: Required separator between asset and animation
- `animation-name`: Name of the animation (e.g., "idle-loop", "walk", "attack")
- `_`: Required separator before dimensions
- `WIDTHxHEIGHT`: Frame dimensions for slicing (e.g., "32x32", "64x64")

**Examples:**
- `player@idle-loop_32x32.png`
- `fire-ball@explode_64x64.png`
- `enemy-boss@attack-heavy_128x128.png`

**Auto-Loop Detection:**
If animation name contains "loop", it automatically uses `LOOPTOSTART` mode.

### GIF Naming (for GIF to Sprite Sheet Converter)

```
asset-name@animation-name.gif
```

**Same rules as sprite sheets, but without dimensions:**
- All lowercase
- Hyphens for word separation
- No `_WIDTHxHEIGHT` suffix

**Examples:**
- `player@run-loop.gif` → Outputs `player@run-loop_32x32.png` + `player@run-loop.json`
- `fire-ball@explode.gif` → Outputs `fire-ball@explode_64x64.png` + `fire-ball@explode.json`

### JSON Timing Files

```
asset-name@animation-name.json
```

Must match the sprite/GIF name (without dimensions).

**Format:**
```json
{
    "frames": [100, 100, 150, 100]
}
```

Values in milliseconds per frame.

## 🎨 Tips & Best Practices

### Performance

1. **Use Object Pooling** for frequently spawned animated objects
2. **Disable animations** on off-screen objects
3. **Use sprite atlases** to reduce draw calls
4. **Cache animation data** queries

### Workflow

1. **Organize by asset** - One SpriteAnimationAsset per character/object
2. **Use consistent naming** - Follow the convention strictly
3. **Test with preview** - Use inspector preview before runtime
4. **JSON for complex timing** - Use when frames need different durations

### Animation Design

1. **Keep loops smooth** - First and last frame should flow
2. **Use frameToLoop** for intro sequences
3. **NOLOOP for one-shots** - Attacks, deaths, VFX
4. **Consider speed ratio** - Adjust per-animation for feel

## 🐛 Troubleshooting

### Animation Not Playing

**Check:**
- GameObject is active
- SpriteRenderer or Image component exists
- Animation asset is assigned
- Animation name is correct (case-sensitive)
- Animation has frames

### Sprites Not Slicing

**Check:**
- Filename follows convention: `Name_WxH.png`
- Dimensions are correct
- Texture is readable
- Using "Slice Sprite Sheet" menu item

### Pool Not Releasing

**Check:**
- PoolManager.Instance exists
- Object was spawned from pool
- DestroyAfterAnimation component attached
- Animation completes successfully

### Preview Not Working

**Check:**
- Animation selected in dropdown
- Asset has animations
- Not in Play mode
- Inspector window has focus

## 📚 API Reference

### SpriteAnimation

| Method | Description |
|--------|-------------|
| `Play(string)` | Play animation by name |
| `Play(string, Action)` | Play with completion callback |
| `Play(string, Action<int>)` | Play with frame callback |
| `Pause(bool)` | Pause/resume animation |
| `Stop()` | Stop animation and cleanup |
| `SetReverse(bool)` | Enable reverse playback |
| `HasAnimation(string)` | Check if animation exists |
| `GetAnimationData(string)` | Get animation data by name |
| `UpdateAnimations()` | Refresh animation list from asset |

### SpriteAnimationAsset

| Method | Description |
|--------|-------------|
| `GetAnimation(string)` | Get animation by name |
| `HasAnimation(string)` | Check if animation exists |
| `AddAnimation(SpriteAnimationData)` | Add new animation |
| `RemoveAnimation(string)` | Remove animation by name |
| `SortAnimationsAlphabetically()` | Sort animations |

### SpriteAnimationData

| Property | Description |
|----------|-------------|
| `name` | Animation name |
| `speedRatio` | Speed multiplier (0.001-10) |
| `loop` | Loop mode |
| `frameToLoop` | Frame to loop back to |
| `frameDatas` | List of frame data |

## 🤝 Contributing

This package is part of the TakoBoyStudios toolkit. For issues or improvements:

1. Document the issue/feature clearly
2. Provide reproduction steps
3. Include Unity version
4. Test with a clean project if possible

## 📄 License

Part of TakoBoyStudios internal toolkit. All rights reserved.

## 🎉 Credits

Created with ❤️ by TakoBoy Studios

---

**Happy Animating!** 🎬✨
