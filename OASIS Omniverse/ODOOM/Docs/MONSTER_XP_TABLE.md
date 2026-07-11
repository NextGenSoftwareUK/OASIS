# ODOOM Monster XP Table

When the beamed-in avatar kills a monster, they receive **XP** (experience points) added to their avatar. The amount is proportional to the monster's threat (health, damage, and notoriety in Doom/Quake lore). Boss monsters also mint **[BOSSNFT]** items in the Monsters tab; other mint-enabled monsters mint **[NFT]** items.

## Doom Monsters (ODOOM)

| Monster           | Config Key                 | Display Name              | XP   | Boss | Notes (HP / threat)     |
|-------------------|----------------------------|---------------------------|------|------|--------------------------|
| ZombieMan         | mint_monster_odoom_zombieman | (ODOOM) ZombieMan       | 10   | No   | 20 HP, former human      |
| ShotgunGuy        | mint_monster_odoom_shotgunguy | (ODOOM) ShotgunGuy    | 15   | No   | 30 HP                    |
| ChaingunGuy       | mint_monster_odoom_chaingunguy | (ODOOM) ChaingunGuy  | 15   | No   | 40 HP                    |
| Demon             | mint_monster_odoom_demon   | (ODOOM) Demon             | 25   | No   | 150 HP, melee            |
| Spectre           | mint_monster_odoom_spectre | (ODOOM) Spectre          | 30   | No   | Invisible demon          |
| DoomImp / Imp     | mint_monster_odoom_doomimp / odoom_imp | (ODOOM) DoomImp / Imp | 20 | No | 60 HP                    |
| Cacodemon         | mint_monster_odoom_cacodemon | (ODOOM) Cacodemon      | 50   | No   | 400 HP                   |
| HellKnight        | mint_monster_odoom_hellknight | (ODOOM) HellKnight    | 80   | No   | 500 HP                   |
| BaronOfHell       | mint_monster_odoom_baronofhell | (ODOOM) BaronOfHell  | 150  | **Yes** | 1000 HP, boss        |
| LostSoul          | mint_monster_odoom_lostsoul | (ODOOM) LostSoul        | 10   | No   | 20 HP                    |
| PainElemental    | mint_monster_odoom_painelemental | (ODOOM) PainElemental | 45 | No | 400 HP                   |
| Revenant          | mint_monster_odoom_revenant | (ODOOM) Revenant         | 60   | No   | 300 HP                   |
| Mancubus          | mint_monster_odoom_mancubus | (ODOOM) Mancubus         | 90   | No   | 600 HP                   |
| Arachnotron       | mint_monster_odoom_arachnotron | (ODOOM) Arachnotron   | 80   | No   | 500 HP                   |
| Archvile          | mint_monster_odoom_archvile | (ODOOM) Archvile         | 120  | No   | 700 HP, resurrects       |
| SpiderMastermind  | mint_monster_odoom_spidermastermind | (ODOOM) SpiderMastermind | 800 | **Yes** | 3000 HP, boss   |
| Cyberdemon        | mint_monster_odoom_cyberdemon | (ODOOM) Cyberdemon     | 1000 | **Yes** | 4000 HP, boss        |

## OQuake Monsters

| Monster         | Config Key                 | Display Name        | XP   | Boss | Notes        |
|-----------------|----------------------------|--------------------|------|------|--------------|
| OQMonsterDog    | mint_monster_oquake_dog   | (OQUAKE) Dog       | 15   | No   | Weak         |
| OQMonsterZombie | mint_monster_oquake_zombie| (OQUAKE) Zombie    | 20   | No   |              |
| OQMonsterDemon  | mint_monster_oquake_demon | (OQUAKE) Demon     | 40   | No   |              |
| OQMonsterShambler| mint_monster_oquake_shambler | (OQUAKE) Shambler | 200 | **Yes** | Boss   |
| OQMonsterGrunt  | mint_monster_oquake_grunt | (OQUAKE) Grunt     | 25   | No   |              |
| OQMonsterFish   | mint_monster_oquake_fish  | (OQUAKE) Fish      | 30   | No   |              |
| OQMonsterOgre   | mint_monster_oquake_ogre  | (OQUAKE) Ogre      | 70   | No   |              |
| OQMonsterEnforcer| mint_monster_oquake_enforcer | (OQUAKE) Enforcer | 60 | No   |              |
| OQMonsterSpawn  | mint_monster_oquake_spawn | (OQUAKE) Spawn     | 100  | No   |              |
| OQMonsterKnight | mint_monster_oquake_knight| (OQUAKE) Knight    | 80   | No   |              |

## Behaviour

- **XP** is awarded for every kill of a known monster (independent of mint setting). Use `star mint monster <name> 0` only to disable **NFT minting** for that monster; XP is still granted.
- **Boss NFTs** (BaronOfHell, SpiderMastermind, Cyberdemon, OQMonsterShambler) appear in the Monsters tab with the **[BOSSNFT]** prefix; other minted monsters use **[NFT]**.
- XP is sent via `star_api_queue_add_xp(amount)` and applied to the beamed-in avatar by the STAR API (enqueued and flushed like add-item jobs).
- The current avatar XP is shown in the **top-right corner** of the screen when beamed in.
