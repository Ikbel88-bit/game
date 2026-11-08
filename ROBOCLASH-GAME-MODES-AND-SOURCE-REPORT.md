# RoboClash Game Modes and C# Source Report

## Scope

This report describes the match modes implemented in the RoboClash Unity project, how the player selects them, how their rules are applied, and the C# source excerpts that implement them. The excerpts show the relevant mode logic; the linked source files contain the full implementations.

The match-mode enum contains seven values. The current pre-game menu presents six choices. `FreeForAll` is still handled in gameplay code but is not a separate card in the current menu; the menu's timed-deathmatch card selects `TimedDeathmatch`.

## Mode overview

| Mode | Main objective | End condition | Respawn behavior |
|---|---|---|---|
| Kill Race | Reach the selected elimination target first | A participant reaches the target | Unlimited match respawns |
| Timed Deathmatch | Get the most eliminations before time expires | Timer expires; highest kill count wins | Unlimited match respawns |
| Duel | Fight one AI opponent, using either a kill target or timer | Selected kill target or timed duel ends | Optional respawns |
| Survival Waves | Survive increasingly large waves of AI opponents | Player is eliminated | No respawns; wave size increases |
| King of the Arena | Control the center zone and accumulate control time | Timer expires; highest control score wins | Optional respawns |
| Knockout | Be the last participant with a life remaining | One or no participants remain | Limited to the selected lives |
| Free-for-All | Outlast all other combatants | A single participant remains; timeout compares remaining health plus shield | Uses the general respawn settings; this is not a current menu option |

## Player-facing mode selection

The pre-game screen exposes Kill Race, Timed Deathmatch, Duel, Survival Waves, King of the Arena, and Knockout. It conditionally displays only settings relevant to the selected mode:

- Kill Race: 5, 10, 15, or 20 eliminations.
- Timed Deathmatch and King of the Arena: 3, 5, or 7 minutes.
- Duel: 5, 10, 15, or 20 eliminations, or 3, 5, or 7 minutes; respawns can be enabled or disabled.
- Survival: starting enemy count, enemies added per wave, and wave-break duration.
- Knockout: 1, 3, or 5 lives per robot.
- King of the Arena: respawns can be enabled or disabled.

The menu's mode-to-enum mapping is implemented in [RoboClashMenuController.cs](Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs):

```csharp
private static RoboClashGameMode GetGameModeFromOption(int option)
{
    switch (option)
    {
        case 0: return RoboClashGameMode.KillRace;
        case 1: return RoboClashGameMode.TimedDeathmatch;
        case 2: return RoboClashGameMode.Duel;
        case 3: return RoboClashGameMode.Survival;
        case 4: return RoboClashGameMode.KingOfTheArena;
        case 5: return RoboClashGameMode.Knockout;
        default: return RoboClashGameMode.TimedDeathmatch;
    }
}
```

The selected mode and rule settings are passed from the menu through [RoboClashSession.cs](Assets/Scripts/RoboClash/UI/RoboClashSession.cs) and applied by `RoboClashMatchManager` when gameplay starts.

## Mode definitions

### Kill Race

Compete against the AI to reach the configured elimination target first. The target is selected from 5, 10, 15, or 20 kills. Match respawns are unlimited so a participant can continue competing after being eliminated.

### Timed Deathmatch

Fight for the highest elimination count during the selected 3-, 5-, or 7-minute match. Participants respawn without a match limit. When time runs out, the participant with the most kills wins; tied scores produce a draw if the player is among the leaders.

### Duel

A one-on-one match against AI. The player chooses either a kill target (5, 10, 15, or 20) or a time limit (3, 5, or 7 minutes). Respawns are separately configurable. A no-respawn kill-target duel ends when only one participant remains, even if nobody reached the kill target.

### Survival Waves

The player faces a wave of AI opponents, then gets a configurable break before the next wave. Each new wave adds the configured growth amount, up to the configured per-wave maximum. The match ends when the player is eliminated. Survival does not grant respawns. AI difficulty can increase as waves progress.

### King of the Arena

Participants compete to control a central zone. A participant gains control score while they are the only living participant inside the zone. If more than one participant occupies it, it is contested and no score accrues. At the time limit, the highest control score wins. Respawns are configurable.

### Knockout

Each robot begins with the chosen number of lives (1, 3, or 5). Elimination consumes a life; the match ends when one participant remains or all are eliminated. Respawns are limited to the remaining lives.

### Free-for-All (internal/legacy mode)

The manager retains a Free-for-All mode in the enum and match-resolution logic, but the current menu does not expose it as a separate option. It waits until one or fewer combatants remain. If the time limit expires first, the manager compares the living participants' combined health and shield to resolve the result.

## C# source

### Mode enum and settings

From [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs):

```csharp
public enum RoboClashGameMode
{
    FreeForAll,
    Duel,
    Survival,
    KillRace,
    TimedDeathmatch,
    KingOfTheArena,
    Knockout
}
```

`RoboClashMatchSettings` stores the selected mode and common rules such as match duration, AI count, Survival starting enemies/growth/cap/break duration, respawns, kill target, Knockout lives, and Duel win/respawn rules. The declarations are in the same [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs) source file.

The manager classifies timed modes and kill-target modes as follows:

```csharp
private bool IsTimedMatch()
{
    return settings.gameMode == RoboClashGameMode.FreeForAll ||
           settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
           settings.gameMode == RoboClashGameMode.KingOfTheArena ||
           (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget);
}

private bool IsKillTargetMode()
{
    return settings.gameMode == RoboClashGameMode.KillRace ||
           (settings.gameMode == RoboClashGameMode.Duel && settings.duelUsesKillTarget);
}
```

### Respawn policy by mode

The mode-specific respawn policy is applied when a participant is spawned:

```csharp
switch (settings.gameMode)
{
    case RoboClashGameMode.KillRace:
    case RoboClashGameMode.TimedDeathmatch:
        controller.ConfigureUnlimitedMatchRespawns();
        break;
    case RoboClashGameMode.Duel:
        if (settings.duelRespawnsEnabled)
            controller.ConfigureUnlimitedMatchRespawns();
        else
            controller.ConfigureMatchRespawns(0);
        break;
    case RoboClashGameMode.KingOfTheArena:
        if (settings.respawnEnabled)
            controller.ConfigureUnlimitedMatchRespawns();
        else
            controller.ConfigureMatchRespawns(0);
        break;
    case RoboClashGameMode.Knockout:
        controller.ConfigureMatchRespawns(settings.knockoutLives - 1);
        break;
    default:
        var allowRespawns = settings.gameMode != RoboClashGameMode.Survival &&
                            settings.respawnEnabled;
        controller.ConfigureMatchRespawns(allowRespawns ? settings.maximumRespawns : 0);
        break;
}
```

### Elimination and kill-target resolution

`EvaluateEliminationResult` handles the mode-specific completion conditions. This excerpt includes kill-target modes, Survival, Knockout, and Free-for-All:

```csharp
if (IsKillTargetMode())
{
    ResolveKillTarget();
    if (State == RoboClashMatchState.Playing && settings.gameMode == RoboClashGameMode.Duel &&
        !settings.duelRespawnsEnabled)
    {
        if (CountAliveCombatants() == 0)
            EndMatch(RoboClashMatchState.Draw);
        else if (CountAliveCombatants() == 1)
            EndMatch(IsPlayerAlive() ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
    }
    return;
}

if (settings.gameMode == RoboClashGameMode.Survival)
{
    if (!IsPlayerAlive())
        EndMatch(RoboClashMatchState.Defeat);
    else if (CountLivingWaveEnemies() == 0 && !waveBreakActive)
        StartWaveBreak();
    return;
}

if (settings.gameMode == RoboClashGameMode.Knockout)
{
    var remaining = 0;
    var playerSurvives = false;
    foreach (var participant in participants)
    {
        if (participant.IsEliminated || !IsAlive(participant))
            continue;
        remaining++;
        playerSurvives = participant.IsPlayer;
    }

    if (remaining == 0)
        EndMatch(RoboClashMatchState.Draw);
    else if (remaining == 1)
        EndMatch(playerSurvives ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
    return;
}

var aliveCombatants = CountAliveCombatants();
if (settings.gameMode == RoboClashGameMode.FreeForAll && aliveCombatants > 1)
    return;
```

The kill-target winner is selected from the participants who meet or exceed the target:

```csharp
private void ResolveKillTarget()
{
    Participant winner = null;
    foreach (var participant in participants)
    {
        if (participant.Kills < settings.killTarget)
            continue;

        if (winner == null || participant.Kills > winner.Kills ||
            (participant.Kills == winner.Kills &&
             string.CompareOrdinal(participant.Id, winner.Id) < 0))
            winner = participant;
    }

    if (winner != null)
        EndMatch(winner.IsPlayer ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
}
```

### Timed match and King of the Arena scoring

When time expires, Timed Deathmatch and timed Duel use elimination totals; King of the Arena uses zone-control score:

```csharp
if (settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
    (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget))
{
    ResolveTimedScore(false);
    return;
}
if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
{
    ResolveTimedScore(true);
    return;
}
```

The zone is controlled only by a sole living participant in the zone:

```csharp
private void UpdateKingControl()
{
    if (settings.gameMode != RoboClashGameMode.KingOfTheArena || kingZone == null)
        return;

    ArenaDamageable soleOccupant = null;
    foreach (var occupant in kingZone.Occupants)
    {
        if (occupant == null || occupant.IsDead || !IsParticipant(occupant))
            continue;

        if (soleOccupant != null)
        {
            kingController = null;
            kingContested = true;
            return;
        }

        soleOccupant = occupant;
    }

    if (soleOccupant == null)
    {
        kingController = null;
        kingContested = false;
        return;
    }

    var participant = FindParticipant(soleOccupant);
    if (participant == null)
        return;
    kingController = soleOccupant;
    kingContested = false;
    participant.KingScore += Time.deltaTime;
}
```

### Survival wave growth

After a wave is cleared, the manager records it, removes the cleared wave's participants, waits for the configured break, and creates the next wave:

```csharp
currentWave++;
var requestedCount = (long)settings.startingSurvivalEnemies +
                     (long)(currentWave - 1) * settings.additionalEnemiesPerWave;
var enemyCount = (int)Math.Min(settings.maximumSurvivalEnemiesPerWave, requestedCount);
var enemyTypes = GetOpponentRobotTypes(enemyCount);
var zones = FindSpawnZones();
```

The mode also raises AI difficulty as the wave count increases:

```csharp
private RoboClashAIDifficulty GetAIDifficultyForCurrentWave()
{
    if (settings.gameMode != RoboClashGameMode.Survival)
        return settings.aiDifficulty;

    if (settings.aiDifficulty == RoboClashAIDifficulty.Easy)
        return currentWave >= 10 ? RoboClashAIDifficulty.Hard :
            currentWave >= 5 ? RoboClashAIDifficulty.Normal : RoboClashAIDifficulty.Easy;
    if (settings.aiDifficulty == RoboClashAIDifficulty.Normal && currentWave >= 5)
        return RoboClashAIDifficulty.Hard;
    return settings.aiDifficulty;
}
```

## Related source files

- [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs) — match setup, mode rules, spawning, scoring, elimination, timeout, and results.
- [RoboClashMenuController.cs](Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) — mode cards and match-rule controls.
- [RoboClashSession.cs](Assets/Scripts/RoboClash/UI/RoboClashSession.cs) — selected mode and options passed from the menu to gameplay.
- [RoboClashHUD.cs](Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) — in-match mode labels and scoreboard display.
- [RoboClashSaveService.cs](Assets/Scripts/RoboClash/RoboClashSaveService.cs) — saved statistics, including Survival wave records.
