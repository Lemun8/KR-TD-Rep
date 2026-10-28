# Kingdom Defense TD

<img src= "https://github.com/user-attachments/assets/4ae2d157-d494-4770-bfe4-78e59a459422"/>


A 2D base-defense game inspired by Kingdom Rush, built in Unity as a one-week technical test.

## Game Overview

You defend a village against five waves of goblins, orcs and wulfs marching along a fixed road. Between waves there is a short **build phase** where you spend resources on towers; then the **threat phase** starts and the enemies come. The loop repeats for five waves.

**Win condition:** survive all 5 waves.
**Lose condition:** the village's health reaches 0 (every enemy that reaches the end of the road damages it).

**What the player does**

- **Builds on preset spots.** The map has a fixed set of build pads (as in Kingdom Rush, not a free grid). Each pad opens a menu where you pick what to build, so where you put each tower matters more than how many you can stack.
- **Four building types:**
  - *Archer tower:* fast physical damage, arrows are aimed at where the enemy was, so fast enemies can dodge them.
  - *Mage tower:* slower, high magic damage, bolts home in and never miss.
  - *Artillery:* slow, splash physical damage that hits groups.
  - *Barracks:* spawns 3 soldiers that block and fight enemies. The player chooses a rally point on the road, soldiers respawn when they die.
- **Manages five resources:** Gold, Wood, Rocks, Steel and Population. Every building costs gold plus a different mix of the others (for example, mage towers need steel, barracks need the most population and rocks), so the build order is a real trade-off.
- **Upgrades the village** through a UI panel. Four upgrades (Settlement, Lumber, Mine, Forge) each raise the income of one resource over time, for a gold cost. Steel is the rarest and most expensive to grow.
- **Earns income** from enemy kills (gold), a steady trickle of resources every few seconds, and a bonus at the end of every wave. Finishing a wave with the village at 100% health gives an extra bonus.

**Enemies**

| Enemy | Role |
|---|---|
| Goblin | Weak and low damage, comes in large swarms |
| Orc | Slower, tougher, has armor (physical resistance) |
| Wulf | Fast and frail, hits hard, dangerous if soldiers do not block it |

Waves get harder by adding more enemies, mixing types, and scaling enemy health per group.

At the end of a run, a summary screen shows waves survived, enemies defeated, perfect waves, village health and time.

**Controls:** mouse only. Click a build pad to build, sell or set a barracks rally point. Click "Village" to open the upgrade panel. "Start Wave Now" skips the build timer.

## How to Run

**Play the build**

1. Download or clone this repository.
2. Open the build folder and run `[YourGame].exe` (Windows).
3. Press **Start** on the main menu.

**Open the project in Unity**

1. Install **Unity 6000.0.75f1** from Unity Hub.
2. In Unity Hub choose *Add project from disk* and select this repository's folder.
3. Open `Assets/Scenes/MainMenu.unity` and press Play.
4. To make a build, open *File > Build Profiles*, make sure `MainMenu` is scene 0 and the game scene is scene 1, then build for Windows.

- Engine & version used: **Unity 6000.0.75f1** (C#, 2D)
- Build location: `/Build/` *(or replace with your release link)*

## Technical Decisions

### Everything talks through events, not references
The systems are decoupled through C# events. `Health` raises `Changed` and `Died`. `Enemy` raises `Killed` and `ReachedVillage`. `WaveManager` raises `WaveStarted`, `WaveCompleted`, `EnemyKilled`, `GameWon` and `GameLost`. `ResourceManager` raises `ResourceChanged`. The resource system, HUD, end screen and audio all subscribe to these. Nothing in the wave logic knows about gold, UI or sound, so each of those could be added or removed without touching the wave code.

### A small state machine drives the game loop
`GamePhase` (`Preparing`, `WaveActive`, `Won`, `Lost`) is driven by one coroutine in `WaveManager`. A wave ends when every spawn group has finished spawning **and** no enemies are alive. Enemy counts are kept consistent with `try/finally` in the spawn coroutine, and a defeat calls `StopAllCoroutines` so nothing spawns after the game is lost.

### Data-driven content
Balance and content live in data, not in code:
- `BuildingData` and `VillageUpgradeData` are ScriptableObjects (cost, prefab, refund percent, upgrade levels).
- Waves are `WaveDefinition` objects made of `SpawnGroup`s (enemy type, count, start delay, swarm size, spacing, health multiplier), all editable in the Inspector.
- Costs use a shared `ResourceAmount` list, so any building or upgrade can cost any mix of resources, and `ResourceManager` spends them **all-or-nothing** (`TrySpend`).

### Spawn groups and swarms
All groups in a wave start at the same time, each after its own delay. Each group releases enemies in swarms (a tight clump, then a pause). This lets one wave mix a steady goblin swarm with a late wulf pack. Difficulty escalates through a per-group health multiplier applied by `Health.SetMaxHealth`.

### Targeting without physics
Towers and soldiers read a static `Enemy.Active` list instead of using colliders or physics overlap queries. With swarms of 30 enemies this is simple, cheap and predictable. Towers target the enemy **closest to the village** ("first"), which uses `RemainingPathDistance` from the path follower.

### One damage entry point, two damage types
Every hit goes through `Enemy.TakeDamage(amount, type)`. Armor reduces physical damage and magic resistance reduces magic damage, as percentages. New enemy types only need different numbers, no new code.

### Projectiles are configured per prefab
There is **one** `Tower` script and **one** `Projectile` script shared by all towers; differences come from prefab settings. Projectiles can be *homing* (mage, never misses) or *fixed-spot* (archer, artillery: they fly to where the enemy was and hit the nearest enemy within a small radius). That makes fast enemies dodgeable, so soldiers matter. Splash damage is a radius setting on the same script.

### Soldiers and the "claim" system
An enemy can be claimed by only one soldier at a time (`TryClaim`). Soldiers prefer unclaimed enemies, but if none are nearby they gang up on a claimed one. Enemies only fight their single blocker, so the player can gang up on enemies, but enemies cannot gang up on the player. The rally point can only be placed **on the road** and within the barracks' range, and the game draws indicators for the valid area. Soldiers drop everything and walk to a new rally point first, then re-engage. They regenerate when out of battle, and each slot has its own respawn timer.

### Preset build spots instead of a grid
`BuildSpot` is a click target with an optional allowed-buildings list. One shared `BuildMenuUI` serves every spot and shows only what that spot allows, with unaffordable options disabled and refreshed live as resources change. Limited spots plus multi-resource costs are what force decisions.

### Income over time with a remainder accumulator
Production is stored **per minute** but paid out every 5 seconds, so income feels continuous. Fractions are carried over (`incomeRemainder`) so low-income resources like steel are not rounded away.

### Audio is one call away
Any script plays a sound with `AudioManager.Play(SfxEvent.X)`. Clips are assigned in one list on the manager, with a pool of voices (oldest is replaced), a minimum time between identical sounds (so 20 deaths do not become a roar) and small random pitch changes. Volume is split into **Master / BGM / SFX** by `VolumeSettings`, which saves with `PlayerPrefs` and applies in every scene.

### Editor tooling over manual setup
Repeatable steps are one-click menu items under **TD**: build the HUD, end screen and main menu; fill default waves; create village upgrades; slice and animate sprite sheets for units, towers and projectiles. They are safe to re-run, skip or warn instead of overwriting your tweaks, and never modify the art files. This made the one-week schedule realistic and keeps the scene setup reproducible.

### Shared animation scripts
`UnitAnimation` drives every walking unit (goblin, orc, wulf, soldier): it works out walking and facing from movement, while gameplay code only triggers attack and death. `TowerAnimation` listens to the tower's `Fired` event, so the animation never needs to know about targeting.

### What I deliberately chose NOT to do
- **No grid or free placement:** it would not give the Kingdom Rush feel and needs much more work.
- **Village upgrades are a UI panel, not buildings in the world:** it saved time and keeps the economy readable.
- **No object pooling:** projectiles and enemies are instantiated and destroyed. At the current counts that is fine, and pooling would add complexity.
- **No Audio Mixer:** three volume groups did not justify the extra setup.
- **No save system:** only the audio settings are stored.
- **Singletons for shared services** (`ResourceManager`, `AudioManager`, `VillageHealth`, `BuildMenuUI`): fast to build and easy to call, at the cost of testability. I accepted this for the time limit.
- **No automated tests:** I tested by playing and with Inspector context-menu debug tools.

## What I Would Do With More Time

1. **Tower upgrade system.** Each tower gets 3 levels bought with gold and resources, reusing the same pattern as village upgrades (a ScriptableObject with a list of levels). At level 3 the player picks a specialisation (for example, archers choose between rapid fire or armor-piercing, artillery between wider splash or a slow effect). Selling would refund a share of everything invested.
2. **More enemy variants** that force different tower choices, using the armor and magic resistance stats that already exist:
   - **Troll / Brute:** very high health and armor, slow. Physical damage barely works, so it pushes the player toward mages.
   - **Shaman / Wraith:** high magic resistance, low armor. Pushes the player toward archers and artillery, and makes the mage tower less of an automatic answer.
   - **Healer:** heals nearby enemies (`Health.Heal` already exists), so the player must prioritise it.
   - **Flying enemy (bat):** ignores soldiers, so barracks cannot block it and only towers can stop it.
   - **Warlord (boss):** appears at the final wave, tough and surrounded by goblin swarms.
3. **In-game range preview** for every tower while placing or selecting it.
4. **Object pooling** for enemies and projectiles, plus a profiling pass.

## Known Issues

- **Damage types have limited impact right now.** Armor and magic resistance are implemented, but only the orc has any armor and no enemy has magic resistance, so the mage tower's advantage is mostly raw damage rather than a real counter. Resistant enemy variants (see above) would fix this.
- **The "perfect village" bonus is easy to lose for good.** The village never heals, so once any enemy gets through, the bonus is gone for the rest of the run.
- **Tower ranges are not shown in-game.** Range circles only appear in the editor (gizmos). Only the barracks shows its range and rally area to the player.
- **Restart and Start use scene names or build indices,** so the scenes must stay in the Build Profiles scene list (MainMenu first, then the game scene).
