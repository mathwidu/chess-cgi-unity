using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Short sounds for what happens on the board: a wooden tap for a move, a double tap for a
// capture, a chime for check, a soft cue when the computer hands the turn back, and a phrase
// when the match ends. The clips are synthesized once in code, so no audio assets or licenses
// ride along. In VR they come from the board in 3D; on the desktop they play flat.
public sealed class BoardSounds : MonoBehaviour
{
    private const int SampleRate = 44100;
    private const float Volume = 0.8f;
    private const float CheckDelay = 0.12f;
    private const float FollowUpDelay = 0.25f;

    private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

    private readonly List<string> played = new List<string>();
    private ChessGameController game;
    private AudioSource source;

    // The names of the sounds played so far, in order; used by tests and harnesses.
    public IReadOnlyList<string> Played => played;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = Volume;
        bool headset = XRRig.IsHeadsetPresent;
        source.spatialBlend = headset ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 0.6f;
        source.maxDistance = 6f;
    }

    private void Update()
    {
        if (game != null)
        {
            return;
        }

        game = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        if (game != null)
        {
            game.MoveApplied += OnMoveApplied;
        }
    }

    private void OnDestroy()
    {
        if (game != null)
        {
            game.MoveApplied -= OnMoveApplied;
        }
    }

    // Called when a move lands, so the tap arrives with the piece, once per move.
    private void OnMoveApplied(MoveResult move)
    {
        Play(move.IsCapture ? "Capture" : "Move");
        if (game.IsGameOver)
        {
            StartCoroutine(PlayLater(EndingSound(), FollowUpDelay));
        }
        else if (game.CheckedKing.HasValue)
        {
            StartCoroutine(PlayLater("Check", CheckDelay));
        }
        else if (game.IsAgainstComputer && game.CurrentTurn == game.HumanSide)
        {
            StartCoroutine(PlayLater("Turn", FollowUpDelay));
        }
    }

    private string EndingSound()
    {
        if (!game.Winner.HasValue)
        {
            return "Draw";
        }

        return game.IsAgainstComputer && game.Winner.Value != game.HumanSide ? "Loss" : "Win";
    }

    private IEnumerator PlayLater(string name, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Play(name);
    }

    private void Play(string name)
    {
        played.Add(name);
        source.PlayOneShot(GetClip(name));
    }

    private static AudioClip GetClip(string name)
    {
        // Unity may unload a clip between play sessions while this cache survives; rebuild it then.
        if (!Clips.TryGetValue(name, out AudioClip clip) || clip == null)
        {
            clip = Synthesize(name);
            Clips[name] = clip;
        }

        return clip;
    }

    private static AudioClip Synthesize(string name)
    {
        switch (name)
        {
            case "Move":
                return Render(name, 0.12f, 0.7f, t => Tap(t, 190f, 1f));
            case "Capture":
                return Render(name, 0.2f, 0.85f, t => Tap(t, 175f, 0.8f) + Tap(t - 0.055f, 140f, 1.15f));
            case "Check":
                return Render(name, 0.6f, 0.55f, t => Bell(t, 880f, 0.12f) + Bell(t - 0.13f, 1175f, 0.14f));
            case "Turn":
                return Render(name, 0.5f, 0.3f, t => Bell(t, 660f, 0.14f) + 0.5f * Bell(t, 990f, 0.1f));
            case "Win":
                return Render(name, 1f, 0.5f, t => Notes(t, 0.11f, 0.24f, 523.25f, 659.25f, 783.99f, 1046.5f));
            case "Loss":
                return Render(name, 1.1f, 0.5f, t => Notes(t, 0.18f, 0.3f, 392f, 311.13f, 261.63f));
            default:
                return Render(name, 0.9f, 0.45f, t => Notes(t, 0.2f, 0.28f, 587.33f, 493.88f));
        }
    }

    // A wooden tap: a filtered noise click over a short low thump.
    private static float Tap(float t, float pitch, float weight)
    {
        if (t < 0f)
        {
            return 0f;
        }

        float click = Noise(t) * Mathf.Exp(-t / 0.01f);
        float thump = Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t / 0.03f);
        return weight * (0.55f * click + 0.6f * thump);
    }

    // A soft bell: a sine with a faint octave, quick attack and exponential decay.
    private static float Bell(float t, float frequency, float decay)
    {
        if (t < 0f)
        {
            return 0f;
        }

        float envelope = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t / decay);
        return envelope * (Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2.01f * frequency * t));
    }

    private static float Notes(float t, float spacing, float decay, params float[] frequencies)
    {
        float sum = 0f;
        for (int i = 0; i < frequencies.Length; i++)
        {
            sum += Bell(t - i * spacing, frequencies[i], decay);
        }

        return sum;
    }

    // Deterministic noise, each value held for three samples to soften it: the same click every run.
    private static float Noise(float t)
    {
        int sample = Mathf.FloorToInt(t * SampleRate / 3f);
        uint hash = (uint)sample * 2654435761u;
        hash ^= hash >> 15;
        return (hash & 0xFFFF) / 32767.5f - 1f;
    }

    // Gain is the clip's loudness after normalizing: taps are crisp, the turn cue stays soft.
    private static AudioClip Render(string name, float seconds, float gain, Func<float, float> wave)
    {
        int length = Mathf.CeilToInt(seconds * SampleRate);
        var samples = new float[length];
        float peak = 0.0001f;
        for (int i = 0; i < length; i++)
        {
            samples[i] = wave((float)i / SampleRate);
            peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        }

        // Normalize and fade the last few milliseconds so no clip ends on a click.
        int fade = Mathf.Min(length, SampleRate / 100);
        for (int i = 0; i < length; i++)
        {
            float tail = i >= length - fade ? (length - i) / (float)fade : 1f;
            samples[i] = samples[i] / peak * gain * tail;
        }

        AudioClip clip = AudioClip.Create("ChessCgi_" + name, length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
