using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DY01.Engine;

public class Particle
{
    public float X, Y, Vx, Vy, Life, MaxLife, Size;
    public Color Color;
    public bool Active;

    public Particle()
    {
        Active = false;
    }

    public void Init(float x, float y, float vx, float vy, Color color, float life, float size)
    {
        X = x; Y = y; Vx = vx; Vy = vy; Color = color; Life = life; MaxLife = life; Size = size;
        Active = true;
    }

    public bool Update()
    {
        if (!Active) return false;
        X += Vx; Y += Vy; Vy += 0.12f; Life--;
        if (Life <= 0) Active = false;
        return Active;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        if (!Active) return;
        float a = Life / MaxLife;
        var c = Color * a;
        sb.Draw(pixel, new Rectangle((int)(X - camX), (int)Y, (int)Size, (int)Size), c);
    }
}

public class FloatText
{
    public float X, Y, Life, MaxLife;
    public string Text;
    public Color Color;
    public float Size;

    public FloatText(float x, float y, string text, Color color, float size = 16)
    {
        X = x; Y = y; Text = text; Color = color; Size = size; Life = 40; MaxLife = 40;
    }

    public bool Update()
    {
        Y -= 1.2f; Life--;
        return Life > 0;
    }
}

public class ParticleSystem
{
    public List<Particle> Particles = new();
    public List<FloatText> FloatTexts = new();
    private Random _rng = new();
    private Queue<Particle> _pool = new();

    public void Clear() 
    { 
        foreach (var p in Particles)
        {
            p.Active = false;
            _pool.Enqueue(p);
        }
        Particles.Clear(); 
        FloatTexts.Clear(); 
    }

    public void Spawn(float x, float y, float vx, float vy, Color color, float life, float size)
    {
        Particle p;
        if (_pool.Count > 0)
        {
            p = _pool.Dequeue();
        }
        else
        {
            p = new Particle();
        }
        p.Init(x, y, vx, vy, color, life, size);
        Particles.Add(p);
    }

    public void SpawnBurst(float x, float y, int count, float spread, Color color,
        (float min, float max) lifeRange, (float min, float max) sizeRange,
        (float min, float max)? vyRange = null)
    {
        for (int i = 0; i < count; i++)
        {
            float vx = (float)(_rng.NextDouble() * 2 - 1) * spread;
            float vy = vyRange.HasValue
                ? (float)(_rng.NextDouble() * (vyRange.Value.max - vyRange.Value.min) + vyRange.Value.min)
                : (float)(_rng.NextDouble() * 2 - 1) * spread * 0.5f;
            float life = (float)(_rng.NextDouble() * (lifeRange.max - lifeRange.min) + lifeRange.min);
            float size = (float)(_rng.NextDouble() * (sizeRange.max - sizeRange.min) + sizeRange.min);
            Spawn(x, y, vx, vy, color, life, size);
        }
    }

    public void SpawnText(float x, float y, string text, Color color, float size = 16)
    {
        FloatTexts.Add(new FloatText(x, y, text, color, size));
    }

    public void Update()
    {
        // 回收失活粒子到对象池，避免频繁GC
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            if (!p.Update())
            {
                _pool.Enqueue(p);
                Particles.RemoveAt(i);
            }
        }
        FloatTexts.RemoveAll(t => !t.Update());
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX, SpriteFont? font = null)
    {
        foreach (var p in Particles)
        {
            float a = p.Life / p.MaxLife;
            var c = p.Color * a;
            int size = (int)p.Size;
            // Draw with slight glow for larger particles
            if (size > 3)
            {
                var glowC = c * 0.3f;
                sb.Draw(pixel, new Rectangle((int)(p.X - camX) - 1, (int)p.Y - 1, size + 2, size + 2), glowC);
            }
            sb.Draw(pixel, new Rectangle((int)(p.X - camX), (int)p.Y, size, size), c);
        }
        if (font != null)
        {
            foreach (var t in FloatTexts)
            {
                float a = t.Life / t.MaxLife;
                var c = t.Color * a;
                // Shadow for readability
                sb.DrawString(font, t.Text, new Vector2(t.X - camX + 1, t.Y + 1), Color.Black * (a * 0.5f), 0, Vector2.Zero, t.Size / 16f, SpriteEffects.None, 0);
                sb.DrawString(font, t.Text, new Vector2(t.X - camX, t.Y), c, 0, Vector2.Zero, t.Size / 16f, SpriteEffects.None, 0);
            }
        }
    }
}