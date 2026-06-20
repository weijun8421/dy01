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
    private Random _rng = new();
    private WeatherType _currentWeather = WeatherType.Clear;
    private int _weatherTimer = 0;
    private float _windStrength = 0f;
    
    public WeatherType CurrentWeather => _currentWeather;
    public float WindStrength => _windStrength;

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

        // 更新粒子
        _particles.RemoveAll(p => !p.Update());
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
    }

    public void Clear()
    {
        _particles.Clear();
    }
}
