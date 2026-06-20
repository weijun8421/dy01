using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DY01.Engine;

public class LightSource
{
    public float X, Y;
    public float Radius;
    public Color Color;
    public float Intensity;
    public float FlickerSpeed;
    public float FlickerAmount;
    private float _phase;
    private static readonly Random _rng = new();

    public LightSource(float x, float y, float radius, Color color, float intensity = 1f, float flickerSpeed = 0f, float flickerAmount = 0f)
    {
        X = x; Y = y; Radius = radius; Color = color; Intensity = intensity;
        FlickerSpeed = flickerSpeed; FlickerAmount = flickerAmount;
        _phase = (float)(_rng.NextDouble() * Math.PI * 2);
    }

    public void Update()
    {
        _phase += FlickerSpeed;
    }

    public float GetCurrentIntensity()
    {
        if (FlickerSpeed <= 0 || FlickerAmount <= 0)
            return Intensity;
        float flicker = (float)(Math.Sin(_phase) * FlickerAmount);
        return Intensity + flicker;
    }
}

public class LightingSystem
{
    private List<LightSource> _lights = new();
    private LightSource? _playerLight;
    private float _ambientLight = 0.3f;
    private string _currentTheme = "";

    public float AmbientLight
    {
        get => _ambientLight;
        set => _ambientLight = Math.Clamp(value, 0f, 1f);
    }

    public void SetTheme(string themeName)
    {
        _currentTheme = themeName;
        _lights.Clear();

        // Theme-specific ambient lighting
        switch (themeName)
        {
            case "火山":
                _ambientLight = 0.25f; // Darker with lava glow
                break;
            case "雪山":
                _ambientLight = 0.4f; // Brighter snow reflection
                break;
            case "沙地":
                _ambientLight = 0.5f; // Bright desert
                break;
            default:
                _ambientLight = 0.35f; // Normal plains
                break;
        }
    }

    public void SetPlayerLight(float x, float y, float radius, Color color)
    {
        if (_playerLight == null)
        {
            _playerLight = new LightSource(x, y, radius, color, 0.8f, 0.05f, 0.05f);
            _lights.Add(_playerLight);
        }
        else
        {
            _playerLight.X = x;
            _playerLight.Y = y;
            _playerLight.Radius = radius;
            _playerLight.Color = color;
        }
    }

    public void AddTorch(float x, float y)
    {
        // Torch with flickering warm light
        var torch = new LightSource(x, y, 80, new Color(255, 180, 80), 0.9f, 0.1f, 0.15f);
        _lights.Add(torch);
    }

    public void AddLavaGlow(float x, float y)
    {
        // Lava glow with slow pulse
        var lava = new LightSource(x, y, 100, new Color(255, 100, 30), 0.7f, 0.03f, 0.1f);
        _lights.Add(lava);
    }

    public void AddCrystalGlow(float x, float y, Color color)
    {
        // Crystal/magical glow
        var crystal = new LightSource(x, y, 60, color, 0.6f, 0.08f, 0.2f);
        _lights.Add(crystal);
    }

    public void Update()
    {
        foreach (var light in _lights)
        {
            light.Update();
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX, int screenWidth, int screenHeight)
    {
        // 只绘制非玩家光源（火把、岩浆等），跳过玩家光源以避免白色方块覆盖角色
        foreach (var light in _lights)
        {
            if (light == _playerLight) continue; // 跳过玩家光源

            float intensity = light.GetCurrentIntensity();
            int screenX = (int)(light.X - camX);
            int screenY = (int)light.Y;

            // Skip if off screen
            if (screenX < -light.Radius || screenX > screenWidth + light.Radius ||
                screenY < -light.Radius || screenY > screenHeight + light.Radius)
                continue;

            // Draw radial gradient glow
            int steps = 8;
            for (int i = steps; i >= 1; i--)
            {
                float ratio = (float)i / steps;
                float r = light.Radius * ratio;
                int alpha = (int)(intensity * 60 * (1 - ratio));
                alpha = Math.Clamp(alpha, 0, 255);

                var c = new Color(light.Color.R, light.Color.G, light.Color.B, alpha);
                int size = (int)(r * 2);
                sb.Draw(pixel, new Rectangle(screenX - (int)r, screenY - (int)r, size, size), c);
            }
        }
    }

    public void Clear()
    {
        _lights.Clear();
        _playerLight = null;
    }
}
