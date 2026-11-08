using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashObjectives
    {
        public static string Title(RoboClashGameMode mode)
        {
            switch (mode)
            {
                case RoboClashGameMode.KillRace: return "KILL RACE";
                case RoboClashGameMode.TimedDeathmatch: return "TIMED DEATHMATCH";
                case RoboClashGameMode.Duel: return "DUEL";
                case RoboClashGameMode.Survival: return "SURVIVAL WAVES";
                case RoboClashGameMode.KingOfTheArena: return "KING OF THE ARENA";
                case RoboClashGameMode.Knockout: return "KNOCKOUT";
                case RoboClashGameMode.Multiplayer: return "LOCAL MULTIPLAYER";
                default: return "TIMED DEATHMATCH";
            }
        }

        public static string Describe(RoboClashMatchSettings settings, float timeLimitSeconds)
        {
            if (settings == null)
                return string.Empty;

            var minutes = Mathf.Max(1, Mathf.RoundToInt(timeLimitSeconds / 60f));
            switch (settings.gameMode)
            {
                case RoboClashGameMode.KillRace:
                    return $"Be the FIRST to reach {settings.killTarget} eliminations. Respawns are unlimited.";
                case RoboClashGameMode.TimedDeathmatch:
                    return $"Get the MOST eliminations before the {minutes}-minute timer ends. Respawns are unlimited.";
                case RoboClashGameMode.Duel:
                    var rule = settings.duelUsesKillTarget
                        ? $"First to {settings.killTarget} eliminations wins."
                        : $"Most eliminations after {minutes} minutes wins.";
                    return "1v1 against one robot. " + rule +
                           (settings.duelRespawnsEnabled ? string.Empty :
                               " One life each: last robot standing wins.");
                case RoboClashGameMode.Survival:
                    return "Survive wave after wave of enemies. No respawns. The match ends when you are destroyed - beat your best wave.";
                case RoboClashGameMode.KingOfTheArena:
                    return $"Stand ALONE in the glowing center zone to earn control time. If two robots are inside, nobody scores. Most control time after {minutes} minutes wins.";
                case RoboClashGameMode.Knockout:
                    return $"Everyone has {settings.knockoutLives} life(s). Lose them all and you are out. Last robot standing wins; if time runs out, most lives left wins.";
                case RoboClashGameMode.Multiplayer:
                    return $"Free-for-all on one screen. First player to {settings.killTarget} eliminations wins. Respawns are unlimited.";
                default:
                    return string.Empty;
            }
        }

        public static string Progress(RoboClashGameMode mode, int kills, int killTarget, int wave,
            int enemiesLeft, float kingScore, int lives, int robotsLeft)
        {
            switch (mode)
            {
                case RoboClashGameMode.KillRace: return $"KILLS {kills} / {killTarget}";
                case RoboClashGameMode.TimedDeathmatch: return $"KILLS {kills}";
                case RoboClashGameMode.Duel:
                    return killTarget > 0 ? $"KILLS {kills} / {killTarget}" : $"KILLS {kills}";
                case RoboClashGameMode.Survival: return $"WAVE {wave}  |  ENEMIES LEFT {enemiesLeft}";
                case RoboClashGameMode.KingOfTheArena: return $"CONTROL {kingScore:0.0} s";
                case RoboClashGameMode.Knockout: return $"LIVES {lives}  |  ROBOTS LEFT {robotsLeft}";
                case RoboClashGameMode.Multiplayer:
                    return $"FIRST TO {killTarget} ELIMINATIONS";
                default: return string.Empty;
            }
        }

        public static string Progress(IList<int> playerKills, int killTarget)
        {
            if (playerKills == null || playerKills.Count == 0)
                return $"(first to {killTarget})";

            var scores = new List<string>(playerKills.Count);
            for (var i = 0; i < playerKills.Count; i++)
                scores.Add($"P{i + 1} {playerKills[i]}");
            return string.Join(" | ", scores) + $" (first to {killTarget})";
        }
    }
}
