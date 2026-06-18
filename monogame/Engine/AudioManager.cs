using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DY01.Engine;

public static class AudioManager
{
    private static Dictionary<string, SoundEffect> _sounds = new();
    private static float _volume = 0.3f;
    private static bool _initialized = false;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        // Sound effects would be loaded here if audio files were available
        // For now, we'll use procedural generation or silent mode
    }

    public static void SetVolume(float vol)
    {
        _volume = MathHelper.Clamp(vol, 0f, 1f);
    }

    private static void PlaySound(string name)
    {
        if (!_initialized) return;
        if (_sounds.TryGetValue(name, out var sound))
        {
            var instance = sound.CreateInstance();
            instance.Volume = _volume;
            instance.Play();
        }
    }

    public static void Shoot(string weaponName)
    {
        // Procedural shoot sound based on weapon type
        PlaySound($"shoot_{weaponName}");
    }

    public static void Hit()
    {
        PlaySound("hit");
    }

    public static void Explode()
    {
        PlaySound("explode");
    }

    public static void Kill()
    {
        PlaySound("kill");
    }

    public static void Pickup()
    {
        PlaySound("pickup");
    }

    public static void Jump()
    {
        PlaySound("jump");
    }

    public static void Dash()
    {
        PlaySound("dash");
    }

    public static void Die()
    {
        PlaySound("die");
    }

    public static void Buff()
    {
        PlaySound("buff");
    }

    public static void MenuMove()
    {
        PlaySound("menu_move");
    }

    public static void MenuSelect()
    {
        PlaySound("menu_select");
    }
}