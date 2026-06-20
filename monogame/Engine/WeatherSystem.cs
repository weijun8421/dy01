using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;

namespace DY01.Engine;

public enum WeatherType
{
    Clear,
    Rain,
    Snow,
    Sandstorm
}

public class WeatherParticle
{
    public float X, Y, Vx, Vy, Life, MaxLife, Size;
    public Color Color;
    public WeatherType Type;

    public WeatherParticle(float x, float y, float vx, float vy, Color color, float life, float size, WeatherType type)
    {
        X = x; Y = y; Vx = vx; Vy = vy; Color = color; Life = life; MaxLife = life; Size = size; Type = type;
    }

    public bool Update()
    {
        X += Vx;
        Y += Vy;
        
        // 风的影响
        if (Type == WeatherType.Rain)
        {
            Vx += 0.05f; // 雨滴受风影响
        }
        else if (Type == WeatherType.Snow)
        {
            Vx += (float)(Math.Sin(Y * 0.01f) * 0.1f); // 雪花飘落摇摆
        }
        else if (Type == WeatherType.Sandstorm)
        {
            Vx += 0.1f; // 沙尘暴强风
        }
        
        Life--;
        return Life > 0 && Y < Config.H + 50;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        float a = Math.Min(1f, Life / MaxLife * 2f);
        var c = Color * a;
        
        if (Type == WeatherType.Rain)
        {
            // 雨滴画成短线
            int len = (int)(Math.Abs(Vy) * 2);
            sb.Draw(pixel, new Rectangle((int)(X - camX), (int)Y, 1, len), c);
        }
        else if (Type == WeatherType.Snow)
        {
            // 雪花画成方块
            sb.Draw(pixel, new Rectangle((int)(X - camX), (int)Y, (int)Size, (int)Size), c);
        }
        else if (Type == WeatherType.Sandstorm)
        {
            // 沙尘画成小方块
            sb.Draw(pixel, new Rectangle((int)(X - camX), (int)Y, (int)Size, (int)Size), c);
        }
    }
}

public class WeatherSystem
{
    private List<WeatherParticle> _particles = new();
    private List<EnvironmentalParticle> _envParticles = new();
    private Random _rng = new();
    private WeatherType _currentWeather = WeatherType.Clear;
    private int _weatherTimer = 0;
    private float _windStrength = 0f;
    private string _currentTheme = "";
    
    public WeatherType CurrentWeather => _currentWeather;
    public float WindStrength => _windStrength;

    public void SetTheme(string themeName)
    {
        _currentTheme = themeName;
        _envParticles.Clear();
    }

    public void Update(Camera camera)
    {
        // 天气切换
        _weatherTimer--;
        if (_weatherTimer <= 0)
        {
            // 随机切换天气
            double roll = _rng.NextDouble();
            if (roll < 0.6) _currentWeather = WeatherType.Clear;
            else if (roll < 0.8) _currentWeather = WeatherType.Rain;
            else if (roll < 0.95) _currentWeather = WeatherType.Snow;
            else _currentWeather = WeatherType.Sandstorm;
            
            _weatherTimer = _rng.Next(600, 1200); // 10-20秒切换一次
            _windStrength = _currentWeather == WeatherType.Sandstorm ? 3f : 
                           _currentWeather == WeatherType.Rain ? 1.5f : 0.5f;
        }

        // 生成天气粒子
        if (_currentWeather != WeatherType.Clear)
        {
            SpawnWeatherParticles(camera);
        }

        // 生成主题环境粒子
        SpawnEnvironmentalParticles(camera);

        // 更新粒子
        _particles.RemoveAll(p => !p.Update());
        _envParticles.RemoveAll(p => !p.Update());
    }

    private void SpawnWeatherParticles(Camera camera)
    {
        float camX = camera.OffsetX;
        float spawnX = camX + (float)(_rng.NextDouble() * (Config.W + 200)) - 100;
        float spawnY = -20;
        
        int spawnCount = _currentWeather switch
        {
            WeatherType.Rain => 3,
            WeatherType.Snow => 2,
            WeatherType.Sandstorm => 4,
            _ => 0
        };

        for (int i = 0; i < spawnCount; i++)
        {
            float x = spawnX + (float)(_rng.NextDouble() * 100 - 50);
            float y = spawnY + (float)(_rng.NextDouble() * 50);
            
            float vx = _currentWeather switch
            {
                WeatherType.Rain => _windStrength + (float)(_rng.NextDouble() * 0.5f),
                WeatherType.Snow => (float)(_rng.NextDouble() * 1f - 0.5f),
                WeatherType.Sandstorm => _windStrength * 2 + (float)(_rng.NextDouble() * 2f),
                _ => 0
            };
            
            float vy = _currentWeather switch
            {
                WeatherType.Rain => 8 + (float)(_rng.NextDouble() * 4f),
                WeatherType.Snow => 1 + (float)(_rng.NextDouble() * 1f),
                WeatherType.Sandstorm => 2 + (float)(_rng.NextDouble() * 2f),
                _ => 0
            };
            
            Color color = _currentWeather switch
            {
                WeatherType.Rain => new Color(150, 180, 255, 180),
                WeatherType.Snow => new Color(255, 255, 255, 200),
                WeatherType.Sandstorm => new Color(210, 180, 120, 150),
                _ => Color.White
            };
            
            float size = _currentWeather switch
            {
                WeatherType.Snow => 2 + (float)(_rng.NextDouble() * 2f),
                WeatherType.Sandstorm => 1 + (float)(_rng.NextDouble() * 3f),
                _ => 1
            };
            
            float life = 200;
            
            _particles.Add(new WeatherParticle(x, y, vx, vy, color, life, size, _currentWeather));
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        foreach (var p in _particles)
        {
            p.Draw(sb, pixel, camX);
        }
        foreach (var p in _envParticles)
        {
            p.Draw(sb, pixel, camX);
        }
    }

    public void Clear()
    {
        _particles.Clear();
        _envParticles.Clear();
    }

    private void SpawnEnvironmentalParticles(Camera camera)
    {
        float camX = camera.OffsetX;
        
        // Theme-specific environmental particles
        if (_currentTheme == "平原")
        {
            // Falling leaves
            if (_rng.NextDouble() < 0.03)
            {
                float x = camX + (float)(_rng.NextDouble() * (Config.W + 100)) - 50;
                float y = -10;
                Color leafColor = _rng.NextDouble() < 0.5 
                    ? new Color(180, 120, 40, 200)  // Brown leaf
                    : new Color(200, 160, 60, 200); // Golden leaf
                _envParticles.Add(new EnvironmentalParticle(x, y, 
                    (float)(_rng.NextDouble() * 0.5f - 0.25f), 
                    0.5f + (float)(_rng.NextDouble() * 0.3f),
                    leafColor, 300, 3, EnvParticleType.Leaf));
            }
            // Fireflies (occasional)
            if (_rng.NextDouble() < 0.01)
            {
                float x = camX + (float)(_rng.NextDouble() * Config.W);
                float y = 200 + (float)(_rng.NextDouble() * 300);
                _envParticles.Add(new EnvironmentalParticle(x, y,
                    (float)(_rng.NextDouble() * 0.3f - 0.15f),
                    (float)(_rng.NextDouble() * 0.2f - 0.1f),
                    new Color(200, 255, 100, 180), 400, 2, EnvParticleType.Firefly));
            }
        }
        else if (_currentTheme == "沙地")
        {
            // Tumbling dust/debris
            if (_rng.NextDouble() < 0.02)
            {
                float x = camX - 20;
                float y = 400 + (float)(_rng.NextDouble() * 150);
                _envParticles.Add(new EnvironmentalParticle(x, y,
                    1.5f + (float)(_rng.NextDouble() * 1f),
                    (float)(_rng.NextDouble() * 0.5f - 0.25f),
                    new Color(180, 150, 100, 150), 250, 2, EnvParticleType.Dust));
            }
        }
        else if (_currentTheme == "雪山")
        {
            // Extra snowflakes (lighter than weather snow)
            if (_rng.NextDouble() < 0.05)
            {
                float x = camX + (float)(_rng.NextDouble() * (Config.W + 50)) - 25;
                float y = -5;
                _envParticles.Add(new EnvironmentalParticle(x, y,
                    (float)(_rng.NextDouble() * 0.4f - 0.2f),
                    0.3f + (float)(_rng.NextDouble() * 0.4f),
                    new Color(255, 255, 255, 120), 500, 2, EnvParticleType.Snowflake));
            }
        }
        else if (_currentTheme == "火山")
        {
            // Rising embers/sparks
            if (_rng.NextDouble() < 0.04)
            {
                float x = camX + (float)(_rng.NextDouble() * Config.W);
                float y = Config.H + 10;
                _envParticles.Add(new EnvironmentalParticle(x, y,
                    (float)(_rng.NextDouble() * 0.6f - 0.3f),
                    -(1f + (float)(_rng.NextDouble() * 1.5f)),
                    new Color(255, 120 + _rng.Next(80), 0, 220), 350, 2, EnvParticleType.Ember));
            }
            // Ash particles
            if (_rng.NextDouble() < 0.03)
            {
                float x = camX + (float)(_rng.NextDouble() * (Config.W + 100)) - 50;
                float y = -10;
                _envParticles.Add(new EnvironmentalParticle(x, y,
                    (float)(_rng.NextDouble() * 0.3f - 0.15f),
                    0.4f + (float)(_rng.NextDouble() * 0.3f),
                    new Color(80, 80, 80, 100), 600, 2, EnvParticleType.Ash));
            }
        }
    }
}

public enum EnvParticleType
{
    Leaf,
    Firefly,
    Dust,
    Snowflake,
    Ember,
    Ash
}

public class EnvironmentalParticle
{
    private static readonly Random _sharedRng = new();
    public float X, Y, Vx, Vy, Life, MaxLife, Size;
    public Color Color;
    public EnvParticleType Type;
    private float _phase;

    public EnvironmentalParticle(float x, float y, float vx, float vy, Color color, float life, float size, EnvParticleType type)
    {
        X = x; Y = y; Vx = vx; Vy = vy; Color = color; Life = life; MaxLife = life; Size = size; Type = type;
        _phase = (float)(_sharedRng.NextDouble() * Math.PI * 2);
    }

    public bool Update()
    {
        _phase += 0.05f;
        
        switch (Type)
        {
            case EnvParticleType.Leaf:
                // Leaves drift and sway
                Vx += (float)(Math.Sin(_phase) * 0.02f);
                Vy += 0.005f; // gentle gravity
                break;
            case EnvParticleType.Firefly:
                // Fireflies wander randomly
                Vx += (float)(Math.Sin(_phase * 0.7f) * 0.03f);
                Vy += (float)(Math.Cos(_phase * 0.5f) * 0.02f);
                // Flicker
                float flicker = (float)(Math.Sin(_phase * 3) * 0.3f + 0.7f);
                Color = new Color(Color.R, Color.G, Color.B, (int)(180 * flicker));
                break;
            case EnvParticleType.Dust:
                // Dust tumbles along ground
                Vy += 0.01f;
                break;
            case EnvParticleType.Snowflake:
                // Snowflakes drift
                Vx += (float)(Math.Sin(_phase * 0.8f) * 0.015f);
                break;
            case EnvParticleType.Ember:
                // Embers rise and fade
                Vx += (float)(Math.Sin(_phase * 1.2f) * 0.04f);
                Vy *= 0.99f; // slow down rise
                break;
            case EnvParticleType.Ash:
                // Ash drifts down slowly
                Vx += (float)(Math.Sin(_phase * 0.6f) * 0.01f);
                break;
        }
        
        X += Vx;
        Y += Vy;
        Life--;
        return Life > 0 && Y < Config.H + 50 && Y > -50;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        float a = Math.Min(1f, Life / MaxLife * 2f);
        var c = Color * a;
        
        int px = (int)(X - camX);
        int py = (int)Y;
        int sz = (int)Size;
        
        sb.Draw(pixel, new Rectangle(px, py, sz, sz), c);
    }
}
