using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DY01.Engine;

/// <summary>
/// 程序化音频系统 - 使用 DynamicSoundEffectInstance 在运行时生成音效
/// 无需外部音频文件即可产生各种游戏音效
/// </summary>
public static class AudioManager
{
    private static float _volume = 0.3f;
    private static bool _initialized = false;
    private static int _sampleRate = 44100;
    private static readonly Random _rng = new();

    // 音效冷却，防止同类型音效过于频繁
    private static readonly Dictionary<string, int> _cooldowns = new();
    private static readonly Dictionary<string, int> _cooldownMax = new()
    {
        ["shoot_rifle"] = 3,
        ["shoot_shotgun"] = 8,
        ["shoot_flame"] = 1,
        ["shoot_laser"] = 4,
        ["shoot_rocket"] = 12,
        ["hit"] = 2,
        ["kill"] = 4,
        ["jump"] = 3,
        ["dash"] = 5,
    };

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
    }

    public static void SetVolume(float vol)
    {
        _volume = MathHelper.Clamp(vol, 0f, 1f);
    }

    public static void Update()
    {
        if (!_initialized) return;
        foreach (var key in _cooldowns.Keys)
        {
            if (_cooldowns[key] > 0) _cooldowns[key]--;
        }
    }

    private static bool CanPlay(string name)
    {
        if (!_cooldowns.TryGetValue(name, out int cd)) return true;
        return cd <= 0;
    }

    private static void SetCooldown(string name)
    {
        if (_cooldownMax.TryGetValue(name, out int max))
            _cooldowns[name] = max;
    }

    private static void Play(byte[] data, int sampleRate = 44100)
    {
        if (!_initialized || _volume <= 0) return;
        try
        {
            var sound = new SoundEffect(data, sampleRate, AudioChannels.Mono);
            var instance = sound.CreateInstance();
            instance.Volume = _volume;
            instance.Play();
        }
        catch { }
    }

    /// <summary>生成短促的方波音效（射击类）</summary>
    private static byte[] GenerateShoot(float freq, float duration, float freqDrop = 0.5f)
    {
        int samples = (int)(_sampleRate * duration);
        byte[] data = new byte[samples * 2];
        float phase = 0;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float envelope = (float)Math.Exp(-t * 8);
            float f = freq * (1 - freqDrop * t);
            phase += f / _sampleRate;
            if (phase > 1) phase -= 1;
            float val = phase < 0.5f ? 1f : -1f;
            val *= envelope * 0.6f;
            short s = (short)(val * 32767);
            data[i * 2] = (byte)(s & 0xFF);
            data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return data;
    }

    /// <summary>生成噪声音效（爆炸、打击类）</summary>
    private static byte[] GenerateNoise(float duration, float freq = 100, float decay = 6)
    {
        int samples = (int)(_sampleRate * duration);
        byte[] data = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float envelope = (float)Math.Exp(-t * decay);
            float noise = (float)(_rng.NextDouble() * 2 - 1);
            float val = noise * envelope * 0.5f;
            short s = (short)(val * 32767);
            data[i * 2] = (byte)(s & 0xFF);
            data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return data;
    }

    /// <summary>生成音调上升音效（跳跃、拾取类）</summary>
    private static byte[] GenerateTone(float startFreq, float endFreq, float duration, string waveType = "sine")
    {
        int samples = (int)(_sampleRate * duration);
        byte[] data = new byte[samples * 2];
        float phase = 0;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float envelope = (float)Math.Exp(-t * 3);
            float f = startFreq + (endFreq - startFreq) * t;
            phase += f / _sampleRate;
            if (phase > 1) phase -= 1;
            float val = waveType switch
            {
                "square" => phase < 0.5f ? 1f : -1f,
                "saw" => phase * 2 - 1,
                _ => (float)Math.Sin(phase * Math.PI * 2),
            };
            val *= envelope * 0.4f;
            short s = (short)(val * 32767);
            data[i * 2] = (byte)(s & 0xFF);
            data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return data;
    }

    public static void Shoot(string weaponName)
    {
        if (!_initialized) return;
        string key = $"shoot_{weaponName}";
        if (!CanPlay(key)) return;
        SetCooldown(key);

        byte[] data = weaponName switch
        {
            "rifle" => GenerateShoot(800, 0.06f, 0.4f),
            "shotgun" => GenerateShoot(400, 0.12f, 0.6f),
            "flame" => GenerateNoise(0.04f, 200, 10),
            "laser" => GenerateTone(1200, 600, 0.08f, "square"),
            "rocket" => GenerateShoot(200, 0.2f, 0.3f),
            _ => GenerateShoot(600, 0.05f, 0.5f),
        };
        Play(data);
    }

    public static void Hit()
    {
        if (!_initialized) return;
        if (!CanPlay("hit")) return;
        SetCooldown("hit");
        Play(GenerateNoise(0.06f, 300, 12));
    }

    public static void Explode()
    {
        if (!_initialized) return;
        if (!CanPlay("explode")) return;
        SetCooldown("explode");
        Play(GenerateNoise(0.3f, 80, 4));
    }

    public static void Kill()
    {
        if (!_initialized) return;
        if (!CanPlay("kill")) return;
        SetCooldown("kill");
        Play(GenerateTone(300, 800, 0.15f, "square"));
    }

    public static void Pickup()
    {
        if (!_initialized) return;
        if (!CanPlay("pickup")) return;
        SetCooldown("pickup");
        Play(GenerateTone(400, 900, 0.1f, "sine"));
    }

    public static void Jump()
    {
        if (!_initialized) return;
        if (!CanPlay("jump")) return;
        SetCooldown("jump");
        Play(GenerateTone(250, 500, 0.08f, "sine"));
    }

    public static void Dash()
    {
        if (!_initialized) return;
        if (!CanPlay("dash")) return;
        SetCooldown("dash");
        Play(GenerateTone(150, 600, 0.12f, "saw"));
    }

    public static void Die()
    {
        if (!_initialized) return;
        Play(GenerateTone(400, 100, 0.4f, "square"));
    }

    public static void Buff()
    {
        if (!_initialized) return;
        Play(GenerateTone(500, 1000, 0.2f, "sine"));
    }

    public static void MenuMove()
    {
        if (!_initialized) return;
        Play(GenerateTone(300, 400, 0.05f, "sine"));
    }

    public static void MenuSelect()
    {
        if (!_initialized) return;
        Play(GenerateTone(400, 800, 0.1f, "square"));
    }
}
