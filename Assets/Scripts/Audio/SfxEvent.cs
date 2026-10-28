// Every sound effect in the game. To add a new one, add a name here, then run
// TD > Create Audio Manager again (or add an entry to the Audio Manager's Sounds list by hand).
public enum SfxEvent
{
    None,

    // Towers shooting
    ArcherShoot,
    MageShoot,
    ArtilleryShoot,

    // Projectiles hitting
    ArrowHit,
    CannonballHit,
    MageBoltHit,

    // Fighting
    MeleeHit,
    EnemyDeath,
    SoldierDeath,
    SoldierRespawn,

    // Game flow
    WaveStart,

    ButtonClick
}