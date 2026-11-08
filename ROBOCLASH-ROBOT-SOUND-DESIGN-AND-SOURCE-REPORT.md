# RoboClash Robot Sounds: Design and Code Source Report

## Scope

This report documents the sound assets and gameplay code used for robot weapons, abilities, ultimates, and common combat events. It describes which clips are selected and what code triggers them. The descriptions are based on asset names and code behavior; they do not claim a subjective listening assessment of each recording.

## Audio implementation at a glance

Robot sounds are sent through `RoboClashRobotAudio`. It maps a robot type and audio event to a clip under `Assets/Resources/RoboClash/Audio`, plays it through one of three `AudioSource` voices, applies the SFX volume setting, and uses spatial falloff for in-world audio. Local-player audio is made less spatial than remote robot audio. Most robot audio is subject to a short per-event repeat limit so rapid events do not start an unlimited number of overlapping sounds.

The main code paths are:

- [RoboClashRobotAudio.cs](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs) — audio events, clip lookup, per-robot weapon mappings, playback, spatial settings, and procedural fallback sounds.
- [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) — standard weapon firing and reload start/completion.
- [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs) — audio calls paired with ability/ultimate visuals, dash/teleport, repair effects, and impact visuals.
- [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) — gameplay activation and duration of abilities and ultimates.
- [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) — movement-triggered footstep sounds.
- [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) — impact and shield-impact sounds at projectile collision locations.
- [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) — detached death sound when a robot is eliminated.
- [SpikeTrap.cs](./Assets/Scripts/RoboClash/SpikeTrap.cs) — the current direct call for the generic damage sound.

## Robot sound profiles

| Robot | Standard weapon sound | Ability sound and gameplay | Ultimate sound and gameplay |
|---|---|---|---|
| **Volt** | `Weapons/PulseRifle.ogg` on each successful pulse-rifle shot. | `Abilities/VoltDash.ogg` when its dash succeeds. The dash is a short movement burst; its sound is triggered with the dash visuals. | `Abilities/VoltUltimate.ogg` at Overdrive activation. Overdrive lasts 8 seconds and increases movement and weapon fire rate; the ultimate sound is an activation cue, not a continuous loop. |
| **Titan** | `Weapons/GatlingCannon.ogg` on each successful Gatling shot. The implementation uses a short fire-event interval, while the configured weapon fires rapidly. | `Abilities/TitanShield.ogg` when Defensive Field is activated. The field reduces incoming damage for its configured duration. | `Abilities/TitanUltimate.ogg` at Rail Cannon activation/charge. The code plays this through the `Charge` event before a brief charge-up and the rail attack. |
| **Shadow** | `Weapons/ShadowSMG.ogg` on each successful Energy SMG shot. | `Abilities/ShadowCloak.ogg` when Cloak is activated. The ability hides the robot temporarily; firing ends the cloak. | `Abilities/ShadowUltimate.ogg` at Shadow Strike activation. The attack teleports toward a target and fires a special projectile; there is no separate `WeaponFire` event for that special projectile. |
| **Spark** | `Weapons/SparkRocket.ogg` on each successful rocket shot. | The current Repair Drone activation plays `Robots/RepairPulse.ogg`; when the repair beam starts targeting, it also plays `Robots/DroneRepair.ogg`. The drone heals and restores shields during its active period. | `Abilities/SparkEMP.ogg` at EMP Blast activation. The EMP is an area effect; it disrupts/slows affected enemies. |

Robot identities, weapon names, cooldowns, and ability behavior are configured in [RoboClashRobotDefinition.cs](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs). The standard weapon event is emitted from the firing path in [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs), and the ability/ultimate event selection is in [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs).

## Clip inventory

### Per-robot weapons

| Asset | Assigned to |
|---|---|
| [PulseRifle.ogg](./Assets/Resources/RoboClash/Audio/Weapons/PulseRifle.ogg) | Volt — Pulse Rifle |
| [GatlingCannon.ogg](./Assets/Resources/RoboClash/Audio/Weapons/GatlingCannon.ogg) | Titan — Gatling Cannon |
| [ShadowSMG.ogg](./Assets/Resources/RoboClash/Audio/Weapons/ShadowSMG.ogg) | Shadow — Energy SMG |
| [SparkRocket.ogg](./Assets/Resources/RoboClash/Audio/Weapons/SparkRocket.ogg) | Spark — Rocket Launcher |

### Ability and ultimate clips

| Asset | Current use |
|---|---|
| [VoltDash.ogg](./Assets/Resources/RoboClash/Audio/Abilities/VoltDash.ogg) | Volt ability dash. The same sound mapping is used by dash and teleport event types. |
| [VoltUltimate.ogg](./Assets/Resources/RoboClash/Audio/Abilities/VoltUltimate.ogg) | Volt Overdrive activation, selected through the `Charge` event. |
| [TitanShield.ogg](./Assets/Resources/RoboClash/Audio/Abilities/TitanShield.ogg) | Titan Defensive Field activation. |
| [TitanUltimate.ogg](./Assets/Resources/RoboClash/Audio/Abilities/TitanUltimate.ogg) | Titan Rail Cannon activation/charge, selected through `Charge`. |
| [ShadowCloak.ogg](./Assets/Resources/RoboClash/Audio/Abilities/ShadowCloak.ogg) | Shadow Cloak activation. |
| [ShadowUltimate.ogg](./Assets/Resources/RoboClash/Audio/Abilities/ShadowUltimate.ogg) | Shadow Strike activation, selected through `Ultimate`. |
| [SparkRepair.ogg](./Assets/Resources/RoboClash/Audio/Abilities/SparkRepair.ogg) | Available through the generic `Ability` clip mapping, but the current Spark ability path selects `RepairPulse` instead. |
| [SparkEMP.ogg](./Assets/Resources/RoboClash/Audio/Abilities/SparkEMP.ogg) | Spark EMP Blast activation, selected through `Emp`. |

## Shared combat and movement sounds

| Event | Asset | Trigger and notes |
|---|---|---|
| Reload start | [ReloadStart.ogg](./Assets/Resources/RoboClash/Audio/Robots/ReloadStart.ogg) | Played when a reload begins. |
| Reload complete | [ReloadComplete.ogg](./Assets/Resources/RoboClash/Audio/Robots/ReloadComplete.ogg) | Played when the magazine is refilled. |
| Energy projectile impact | [Energy.ogg](./Assets/Resources/RoboClash/Audio/Impacts/Energy.ogg) | Used for Volt and Shadow projectile impacts. |
| Metal projectile impact | [Metal.ogg](./Assets/Resources/RoboClash/Audio/Impacts/Metal.ogg) | Used for Titan projectile impacts. |
| Rocket impact | [RocketExplosion.ogg](./Assets/Resources/RoboClash/Audio/Impacts/RocketExplosion.ogg) | Used for Spark projectile impacts. |
| Shield impact | [Shield.ogg](./Assets/Resources/RoboClash/Audio/Impacts/Shield.ogg) | Played when a projectile hits an energy shield; attached to a one-shot positioned at the impact point. |
| Death | [LargeExplosion.ogg](./Assets/Resources/RoboClash/Audio/Impacts/LargeExplosion.ogg) | Played from a detached one-shot when a robot is eliminated, so disabling the robot does not cut it off. |
| Footstep | [Footstep.ogg](./Assets/Resources/RoboClash/Audio/Robots/Footstep.ogg) | Played while grounded and moving. Shadow's footstep volume is reduced. |
| Drone repair | [DroneRepair.ogg](./Assets/Resources/RoboClash/Audio/Robots/DroneRepair.ogg) | Played when Spark's repair beam begins targeting. |
| Repair pulse / respawn | [RepairPulse.ogg](./Assets/Resources/RoboClash/Audio/Robots/RepairPulse.ogg) | Used for Spark's current ability activation sound and the robot respawn visual. |
| Damage | [Damage.ogg](./Assets/Resources/RoboClash/Audio/Robots/Damage.ogg) | Currently called from the spike-trap damage path. Ordinary projectile damage does not currently call this event in `ArenaDamageable`. |

Impact sounds are played from the projectile owner's audio component at the collision point, allowing the sound to be positioned in the arena rather than remaining at the firing robot. Robot death uses a detached source for the same reason: the robot object may be disabled immediately.

## Event-to-code flow

The standard weapon flow is:

1. `RoboClashRobotCombat` validates and launches a projectile.
2. On a successful shot, it plays the `WeaponFire` event.
3. `RoboClashRobotAudio` uses the robot type to select that robot's weapon clip.
4. On collision, `ArenaProjectile` selects an impact or shield-impact event and plays it at the impact position.

The ability flow is:

1. `RoboClashRobotAbilities` performs cooldown/readiness checks and runs the robot-specific effect.
2. Ability/ultimate visuals call `PlayAbility` or `PlayUltimate` on `RoboClashRobotVFX`.
3. That component selects the corresponding robot sound event and passes it to `RoboClashRobotAudio`.
4. The audio component loads a matching `Resources` clip and plays it using its configured spatial audio source.

Simplified source excerpt from `RoboClashRobotAudio.GetBundledClipPath`:

```csharp
case RoboClashAudioEvent.WeaponFire:
    switch (type)
    {
        case RoboClashRobotType.Titan: return "RoboClash/Audio/Weapons/GatlingCannon";
        case RoboClashRobotType.Shadow: return "RoboClash/Audio/Weapons/ShadowSMG";
        case RoboClashRobotType.Spark: return "RoboClash/Audio/Weapons/SparkRocket";
        default: return "RoboClash/Audio/Weapons/PulseRifle";
    }
```

See the complete mappings and fallback behavior in [RoboClashRobotAudio.cs](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs).

## Fallbacks and implementation notes

- Robot audio first checks for a clip override in its serialized audio slots, then loads the bundled `Resources` clip, and finally generates a short procedural fallback if no bundled clip is mapped.
- Missing mapped resource clips are logged as errors by the loader. The procedural fallback is intended to keep an event audible if there is no clip to play; it does not replace the authored bundled sound when that asset exists.
- `RoboClashRobotVFX` uses specific event types for current abilities. For example, Spark activation uses `RepairPulse`, even though a separate `SparkRepair.ogg` exists for the generic `Ability` mapping.
- `Emp` maps to Spark's EMP clip; `Charge` maps to each robot's ultimate clip. These event names describe the audio-routing category and do not necessarily mean every robot has a distinct looping charge or EMP effect.
- The code defines a `Damage` event and loads `Damage.ogg`, but the current ordinary projectile damage path does not play it. The direct gameplay call found is the spike-trap path.
- Special ultimate projectiles use `ArenaProjectile` for their impact audio. They do not automatically play the standard weapon-fire sound because they are launched from the ability code rather than the standard combat firing method.
- The audio sources use the project's SFX volume setting, logarithmic spatial rolloff, and a configured maximum audible distance. Sounds are effects, not voice dialogue; robot-specific character comes primarily from the weapon and ability clip selected for each type.
