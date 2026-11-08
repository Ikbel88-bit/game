using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoboClash
{
    public enum RoboClashAudioEvent
    {
        WeaponFire,
        ReloadStart,
        ReloadComplete,
        ProjectileImpact,
        Damage,
        ShieldImpact,
        Ability,
        Ultimate,
        Death,
        Footstep,
        DroneRepair,
        Emp,
        Dash,
        ShieldActivation,
        Cloak,
        RepairPulse,
        Charge
    }

    [Serializable]
    public sealed class RoboClashAudioSlot
    {
        public RoboClashAudioEvent audioEvent;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Range(0.5f, 2f)] public float pitch = 1f;
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class RoboClashRobotAudio : MonoBehaviour
    {
        private const int VoiceCount = 3;
        private const int SampleRate = 22050;
        private static readonly Dictionary<int, AudioClip> FallbackClips = new Dictionary<int, AudioClip>();
        private static readonly Dictionary<string, AudioClip> BundledClips =
            new Dictionary<string, AudioClip>();
        private static readonly HashSet<RoboClashRobotAudio> ActiveAudio = new HashSet<RoboClashRobotAudio>();

        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(1f)] private float maximumDistance = 28f;
        [SerializeField] private RoboClashRobotType robotType;
        [SerializeField] private RoboClashAudioSlot[] audioSlots =
        {
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.WeaponFire, volume = 0.38f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ReloadStart, volume = 0.42f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ReloadComplete, volume = 0.35f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ProjectileImpact, volume = 0.45f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Damage, volume = 0.4f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ShieldImpact, volume = 0.42f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Ability, volume = 0.48f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Ultimate, volume = 0.52f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Death, volume = 0.45f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Footstep, volume = 0.2f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.DroneRepair, volume = 0.18f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Emp, volume = 0.5f }
        };

        private AudioSource[] voices;
        private float[] nextAllowedTime;
        private int nextVoice;

        private void Awake()
        {
            voices = new AudioSource[VoiceCount];
            nextAllowedTime = new float[Enum.GetValues(typeof(RoboClashAudioEvent)).Length];
            var baseSource = GetComponent<AudioSource>();
            ConfigureSource(baseSource);
            voices[0] = baseSource;

            for (var i = 1; i < voices.Length; i++)
            {
                var child = new GameObject("Audio Voice " + i);
                child.transform.SetParent(transform, false);
                voices[i] = child.AddComponent<AudioSource>();
                CopySourceSettings(baseSource, voices[i]);
            }

            ActiveAudio.Add(this);
            foreach (RoboClashAudioEvent audioEvent in Enum.GetValues(typeof(RoboClashAudioEvent)))
                GetBundledClip(robotType, audioEvent);
        }

        private void OnDestroy()
        {
            ActiveAudio.Remove(this);
        }

        public void Configure(RoboClashRobotType type)
        {
            robotType = type;
            var source = GetComponent<AudioSource>();
            if (source == null)
                return;
            ConfigureSource(source);
        }

        public void Play(RoboClashAudioEvent audioEvent, float volumeScale = 1f)
        {
            if (voices == null || voices.Length == 0)
                return;

            var index = (int)audioEvent;
            if (index < 0 || index >= nextAllowedTime.Length)
                return;

            var minimumInterval = audioEvent == RoboClashAudioEvent.WeaponFire ? 0.045f : 0.075f;
            if (Time.unscaledTime < nextAllowedTime[index])
                return;
            nextAllowedTime[index] = Time.unscaledTime + minimumInterval;

            var slot = FindSlot(audioEvent);
            var clip = slot != null && slot.clip != null
                ? slot.clip
                : GetBundledClip(robotType, audioEvent) ?? GetFallbackClip(robotType, audioEvent);
            if (clip == null)
                return;

            var source = voices[nextVoice++ % voices.Length];
            source.pitch = (slot != null ? slot.pitch : 1f) * UnityEngine.Random.Range(0.96f, 1.04f);
            source.PlayOneShot(clip,
                (slot != null ? slot.volume : GetFallbackVolume(audioEvent)) * Mathf.Clamp01(volumeScale));
        }

        public void PlayDetached(RoboClashAudioEvent audioEvent)
        {
            PlayDetachedAt(audioEvent, transform.position);
        }

        public void PlayDetachedAt(RoboClashAudioEvent audioEvent, Vector3 position)
        {
            var slot = FindSlot(audioEvent);
            var clip = slot != null && slot.clip != null
                ? slot.clip
                : GetBundledClip(robotType, audioEvent) ?? GetFallbackClip(robotType, audioEvent);
            var source = GetComponent<AudioSource>();
            if (clip == null || source == null)
                return;

            var volume = source.volume * (slot != null ? slot.volume : GetFallbackVolume(audioEvent));
            PlayClipAt(clip, position, volume);
        }

        private void PlayClipAt(AudioClip clip, Vector3 position, float volume)
        {
            var sourceObject = new GameObject("RoboClash One Shot");
            sourceObject.transform.position = position;
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
            source.Play();
            UnityEngine.Object.Destroy(sourceObject, clip.length + 0.1f);
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            if (voices == null)
                return;

            var blend = isLocalPlayer ? 0.2f : spatialBlend;
            foreach (var voice in voices)
            {
                if (voice != null)
                    voice.spatialBlend = blend;
            }
        }

        private RoboClashAudioSlot FindSlot(RoboClashAudioEvent audioEvent)
        {
            if (audioSlots == null)
                return null;
            foreach (var slot in audioSlots)
            {
                if (slot != null && slot.audioEvent == audioEvent)
                    return slot;
            }

            return null;
        }

        private static AudioClip GetBundledClip(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var path = GetBundledClipPath(type, audioEvent);
            if (string.IsNullOrEmpty(path))
                return null;
            if (BundledClips.TryGetValue(path, out var clip))
                return clip;

            clip = Resources.Load<AudioClip>(path);
            BundledClips.Add(path, clip);
            if (clip == null)
                Debug.LogError($"RoboClash audio asset is missing at Resources/{path}.");
            return clip;
        }

        private static string GetBundledClipPath(
            RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.WeaponFire:
                    switch (type)
                    {
                        case RoboClashRobotType.Titan: return "RoboClash/Audio/Weapons/GatlingCannon";
                        case RoboClashRobotType.Shadow: return "RoboClash/Audio/Weapons/ShadowSMG";
                        case RoboClashRobotType.Spark: return "RoboClash/Audio/Weapons/SparkRocket";
                        default: return "RoboClash/Audio/Weapons/PulseRifle";
                    }
                case RoboClashAudioEvent.ReloadStart:
                    return "RoboClash/Audio/Robots/ReloadStart";
                case RoboClashAudioEvent.ReloadComplete:
                    return "RoboClash/Audio/Robots/ReloadComplete";
                case RoboClashAudioEvent.ProjectileImpact:
                    return type == RoboClashRobotType.Spark
                        ? "RoboClash/Audio/Impacts/RocketExplosion"
                        : type == RoboClashRobotType.Titan
                            ? "RoboClash/Audio/Impacts/Metal"
                            : "RoboClash/Audio/Impacts/Energy";
                case RoboClashAudioEvent.Damage:
                    return "RoboClash/Audio/Robots/Damage";
                case RoboClashAudioEvent.ShieldImpact:
                    return "RoboClash/Audio/Impacts/Shield";
                case RoboClashAudioEvent.Ability:
                    return type == RoboClashRobotType.Titan
                        ? "RoboClash/Audio/Abilities/TitanShield"
                        : type == RoboClashRobotType.Shadow
                            ? "RoboClash/Audio/Abilities/ShadowCloak"
                            : type == RoboClashRobotType.Spark
                                ? "RoboClash/Audio/Abilities/SparkRepair"
                                : "RoboClash/Audio/Abilities/VoltDash";
                case RoboClashAudioEvent.Ultimate:
                case RoboClashAudioEvent.Charge:
                    return GetUltimateClipPath(type);
                case RoboClashAudioEvent.Death:
                    return "RoboClash/Audio/Impacts/LargeExplosion";
                case RoboClashAudioEvent.Footstep:
                    return "RoboClash/Audio/Robots/Footstep";
                case RoboClashAudioEvent.DroneRepair:
                    return "RoboClash/Audio/Robots/DroneRepair";
                case RoboClashAudioEvent.Emp:
                    return "RoboClash/Audio/Abilities/SparkEMP";
                case RoboClashAudioEvent.Dash:
                    return "RoboClash/Audio/Abilities/VoltDash";
                case RoboClashAudioEvent.ShieldActivation:
                    return "RoboClash/Audio/Abilities/TitanShield";
                case RoboClashAudioEvent.Cloak:
                    return "RoboClash/Audio/Abilities/ShadowCloak";
                case RoboClashAudioEvent.RepairPulse:
                    return "RoboClash/Audio/Robots/RepairPulse";
                default:
                    return null;
            }
        }

        private static string GetUltimateClipPath(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Titan: return "RoboClash/Audio/Abilities/TitanUltimate";
                case RoboClashRobotType.Shadow: return "RoboClash/Audio/Abilities/ShadowUltimate";
                case RoboClashRobotType.Spark: return "RoboClash/Audio/Abilities/SparkEMP";
                default: return "RoboClash/Audio/Abilities/VoltUltimate";
            }
        }

        private void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.volume = RoboClashSaveService.Current.settings.sfxVolume;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
        }

        private static void CopySourceSettings(AudioSource from, AudioSource to)
        {
            to.playOnAwake = false;
            to.loop = false;
            to.spatialBlend = from.spatialBlend;
            to.rolloffMode = from.rolloffMode;
            to.minDistance = from.minDistance;
            to.maxDistance = from.maxDistance;
            to.dopplerLevel = 0f;
            to.priority = from.priority;
            to.volume = from.volume;
        }

        public static void ApplySfxVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp01(volume);
            foreach (var audio in ActiveAudio)
            {
                if (audio == null || audio.voices == null)
                    continue;
                foreach (var voice in audio.voices)
                {
                    if (voice != null)
                        voice.volume = clampedVolume;
                }
            }
        }

        public static class RoboClashAudioManager
        {
                private const string MainMenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
                private static AudioSource uiSource;
                private static AudioSource backgroundMusicSource;
                private static AudioClip selectionClip;
                private static AudioClip confirmClip;
                private static AudioClip gameplayMusicClip;
                private static AudioClip menuMusicClip;
                private static float nextSelectionTime;
                private static float nextConfirmTime;
                private static bool initialized;

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                private static void ResetRuntimeState()
                {
                    initialized = false;
                    uiSource = null;
                    backgroundMusicSource = null;
                    selectionClip = null;
                    confirmClip = null;
                    gameplayMusicClip = null;
                    menuMusicClip = null;
                    nextSelectionTime = 0f;
                    nextConfirmTime = 0f;
                }

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
                private static void Initialize()
                {
                    if (initialized)
                        return;

                    initialized = true;
                    var root = new GameObject(nameof(RoboClashAudioManager));
                    UnityEngine.Object.DontDestroyOnLoad(root);
                    uiSource = CreateSource(root.transform, "UI Audio", false);
                    backgroundMusicSource = CreateSource(root.transform, "Background Music", true);
                    selectionClip = LoadClip("RoboClash/Audio/UI/Selection");
                    confirmClip = LoadClip("RoboClash/Audio/UI/Confirm");
                    gameplayMusicClip = LoadClip("RoboClash/Audio/Music/Hangar Drift - Empty Orbit - Treblo");
                    menuMusicClip = LoadClip("RoboClash/Audio/Music/RoboClash Ignite - Steel Fists Ready - Treblo");
                    backgroundMusicSource.volume = RoboClashSaveService.Current.settings.musicVolume * 0.08f;
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    RoboClashSaveService.MusicVolumeChanged -= OnMusicVolumeChanged;
                    RoboClashSaveService.MusicVolumeChanged += OnMusicVolumeChanged;
                    OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
                }

                public static void AddButtonAudio(Button button)
                {
                    if (button == null)
                        return;

                    var trigger = button.gameObject.AddComponent<EventTrigger>();
                    trigger.triggers = new List<EventTrigger.Entry>();
                    AddSelectionTrigger(trigger, EventTriggerType.PointerEnter);
                    AddSelectionTrigger(trigger, EventTriggerType.Select);
                    button.onClick.AddListener(PlayConfirmation);
                }

                public static void PlaySelection()
                {
                    if (!initialized || Time.unscaledTime < nextSelectionTime)
                        return;

                    nextSelectionTime = Time.unscaledTime + 0.07f;
                    PlayUI(selectionClip, 0.35f);
                }

                public static void PlayConfirmation()
                {
                    if (!initialized || Time.unscaledTime < nextConfirmTime)
                        return;

                    nextConfirmTime = Time.unscaledTime + 0.1f;
                    PlayUI(confirmClip, 0.5f);
                }

                private static AudioSource CreateSource(Transform parent, string sourceName, bool loop)
                {
                    var sourceObject = new GameObject(sourceName);
                    sourceObject.transform.SetParent(parent, false);
                    var source = sourceObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = loop;
                    source.spatialBlend = 0f;
                    source.ignoreListenerPause = true;
                    return source;
                }

                private static void AddSelectionTrigger(EventTrigger trigger, EventTriggerType eventType)
                {
                    var entry = new EventTrigger.Entry { eventID = eventType };
                    entry.callback.AddListener(_ => PlaySelection());
                    trigger.triggers.Add(entry);
                }

            private static void PlayUI(AudioClip clip, float volume)
            {
                if (clip != null && uiSource != null)
                {
                    uiSource.volume = RoboClashSaveService.Current.settings.sfxVolume;
                    uiSource.PlayOneShot(clip, volume);
                }
            }

            private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (backgroundMusicSource == null)
                    return;

                AudioClip targetClip = null;
                var musicVolume = RoboClashSaveService.Current.settings.musicVolume;
                if (RoboClashSceneUtility.IsGameplayScene(scene))
                {
                    targetClip = gameplayMusicClip;
                    musicVolume *= 0.08f;
                }
                else if (string.Equals(scene.path, MainMenuScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    targetClip = menuMusicClip;
                }

                if (targetClip == null)
                {
                    backgroundMusicSource.Stop();
                    backgroundMusicSource.clip = null;
                    return;
                }

                if (backgroundMusicSource.clip != targetClip)
                {
                    backgroundMusicSource.Stop();
                    backgroundMusicSource.clip = targetClip;
                }

                backgroundMusicSource.volume = musicVolume;
                if (!backgroundMusicSource.isPlaying)
                    backgroundMusicSource.Play();
            }

            private static void OnMusicVolumeChanged(float volume)
            {
                if (backgroundMusicSource != null)
                {
                    var scenePath = SceneManager.GetActiveScene().path;
                    var volumeScale = RoboClashSceneUtility.IsGameplayScene(SceneManager.GetActiveScene())
                        ? 0.08f
                        : string.Equals(scenePath, MainMenuScenePath, StringComparison.OrdinalIgnoreCase)
                            ? 1f
                            : 0f;
                    backgroundMusicSource.volume = Mathf.Clamp01(volume) * volumeScale;
                }
            }

            private static AudioClip LoadClip(string resourcePath)
            {
                var clip = Resources.Load<AudioClip>(resourcePath);
                if (clip == null)
                    Debug.LogError($"RoboClash audio asset is missing at Resources/{resourcePath}.");
                return clip;
            }
        }

        private static AudioClip GetFallbackClip(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var key = (int)type * 100 + (int)audioEvent;
            if (FallbackClips.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var duration = GetDuration(type, audioEvent);
            var sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[sampleCount];
            var seed = key * 7919 + 17;
            var baseFrequency = GetFrequency(type, audioEvent);
            var filteredNoise = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var time = (float)i / SampleRate;
                var progress = time / duration;
                var attack = Mathf.Clamp01(time / (type == RoboClashRobotType.Titan ? 0.012f : 0.004f));
                var decayRate = audioEvent == RoboClashAudioEvent.Ultimate ||
                                audioEvent == RoboClashAudioEvent.Charge ? 2.8f : 7.5f;
                var envelope = attack * Mathf.Exp(-time * decayRate);
                var sweep = type == RoboClashRobotType.Spark
                    ? Mathf.Lerp(1.8f, 0.55f, progress)
                    : type == RoboClashRobotType.Shadow
                        ? Mathf.Lerp(1.3f, 0.82f, progress)
                        : Mathf.Lerp(0.72f, 1.15f, progress);
                var phase = 2f * Mathf.PI * baseFrequency * sweep * time;
                seed = unchecked(seed * 1103515245 + 12345);
                var noise = ((seed >> 16) & 0x7fff) / 16383.5f - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, type == RoboClashRobotType.Shadow ? 0.12f : 0.48f);
                var impact = audioEvent == RoboClashAudioEvent.ProjectileImpact ||
                             audioEvent == RoboClashAudioEvent.ShieldImpact ||
                             audioEvent == RoboClashAudioEvent.Emp;
                var tone = Mathf.Sin(phase);
                float texture;
                switch (type)
                {
                    case RoboClashRobotType.Titan:
                        texture = Mathf.Clamp(tone * 2.8f + Mathf.Sin(phase * 0.47f) * 0.8f, -1f, 1f);
                        break;
                    case RoboClashRobotType.Shadow:
                        texture = tone * 0.65f + Mathf.Sin(phase * 3.1f) * 0.14f + filteredNoise * 0.2f;
                        break;
                    case RoboClashRobotType.Spark:
                        texture = tone * 0.48f + Mathf.Sin(phase * 0.5f) * 0.5f +
                                  noise * (impact ? 0.85f : 0.42f);
                        break;
                    default:
                        texture = tone * 0.72f + Mathf.Sin(phase * 2.03f) * 0.27f +
                                  noise * (impact ? 0.42f : 0.12f);
                        break;
                }
                if (type == RoboClashRobotType.Titan && audioEvent == RoboClashAudioEvent.WeaponFire)
                    texture *= 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 34f * time);
                if (audioEvent == RoboClashAudioEvent.Charge)
                    texture += Mathf.Sin(phase * 0.5f) * progress * 0.65f;
                samples[i] = Mathf.Clamp(texture * envelope * 0.3f, -0.9f, 0.9f);
            }

            var clip = AudioClip.Create("Procedural " + type + " " + audioEvent, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            FallbackClips[key] = clip;
            return clip;
        }

        private static float GetDuration(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.Ultimate: return 0.5f;
                case RoboClashAudioEvent.Charge: return type == RoboClashRobotType.Titan ? 0.8f : 0.6f;
                case RoboClashAudioEvent.Ability:
                case RoboClashAudioEvent.Emp:
                case RoboClashAudioEvent.ShieldActivation:
                case RoboClashAudioEvent.RepairPulse: return 0.32f;
                case RoboClashAudioEvent.ProjectileImpact:
                case RoboClashAudioEvent.ShieldImpact:
                case RoboClashAudioEvent.Death: return 0.34f;
                case RoboClashAudioEvent.WeaponFire:
                    return type == RoboClashRobotType.Titan ? 0.19f : 0.12f;
                default: return 0.16f;
            }
        }

        private static float GetFrequency(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var baseFrequency = type == RoboClashRobotType.Titan ? 92f :
                type == RoboClashRobotType.Shadow ? 520f :
                type == RoboClashRobotType.Spark ? 270f : 390f;
            switch (audioEvent)
            {
                case RoboClashAudioEvent.ProjectileImpact:
                case RoboClashAudioEvent.ShieldImpact: return baseFrequency * 0.55f;
                case RoboClashAudioEvent.Ability: return baseFrequency * 1.3f;
                case RoboClashAudioEvent.Ultimate:
                case RoboClashAudioEvent.Emp: return baseFrequency * 0.78f;
                case RoboClashAudioEvent.Dash: return baseFrequency * 1.7f;
                case RoboClashAudioEvent.ShieldActivation: return baseFrequency * 0.72f;
                case RoboClashAudioEvent.Cloak: return baseFrequency * 1.9f;
                case RoboClashAudioEvent.RepairPulse: return baseFrequency * 1.15f;
                case RoboClashAudioEvent.Charge: return baseFrequency * 0.62f;
                case RoboClashAudioEvent.ReloadStart:
                case RoboClashAudioEvent.ReloadComplete: return baseFrequency * 1.8f;
                default: return baseFrequency;
            }
        }

        private static float GetFallbackVolume(RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.Footstep: return 0.2f;
                case RoboClashAudioEvent.DroneRepair: return 0.18f;
                case RoboClashAudioEvent.WeaponFire: return 0.36f;
                default: return 0.45f;
            }
        }
    }
}
