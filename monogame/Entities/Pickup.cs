using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;
using DY01.Engine;
using DY01.Game;

namespace DY01.Entities;

public class Pickup
{
    public float X, Y, Vy;
    public int W = 12, H = 12;
    public string Type; // "health", "ammo"
    public int Lifetime = 600; // 10 seconds at 60fps
    public float Value;
    private float _bobTimer;
    private bool _grounded;

    public Pickup(float x, float y, string type, float value)
    {
        X = x;
        Y = y;
        Type = type;
        Value = value;
        Vy = -3; // Initial upward velocity
        _bobTimer = 0;
        _grounded = false;
    }

    public bool Update(Level level, ParticleSystem particles)
    {
        Lifetime--;
        if (Lifetime <= 0) return false;

        // Blink when about to expire
        if (Lifetime < 120 && (Lifetime / 10) % 2 == 0) return true;

        // Gravity
        if (!_grounded)
        {
            Vy += Config.GRAVITY * 0.5f;
            Y += Vy;

            // Ground collision
            var (tx, ty) = Physics.AabbVsTiles(X, Y + H / 2, W, H, level);
            if (ty.HasValue)
            {
                Y = ty.Value * Config.TILE - H / 2 - 1;
                Vy = 0;
                _grounded = true;
            }
        }
        else
        {
            // Bobbing animation when grounded
            _bobTimer += 0.05f;
        }

        return true;
    }

    public bool CheckPickup(Player player)
    {
        if (player.Dead) return false;
        float dist = MathF.Sqrt((X - player.X) * (X - player.X) + (Y - player.Y) * (Y - player.Y));
        return dist < 30;
    }

    public void Apply(Player player, ParticleSystem particles)
    {
        if (Type == "health")
        {
            player.Heal(Value);
            particles.SpawnText(X, Y - 10, $"+{(int)Value} HP", new Color(100, 255, 100), 14);
            particles.SpawnBurst(X, Y, 8, 4, new Color(100, 255, 100), (5, 15), (2, 4), (-3, 1));
        }
        else if (Type == "ammo")
        {
            // Refill current weapon ammo
            var w = player.Weapon;
            int ammoToAdd = (int)Value;
            int actual = Math.Min(ammoToAdd, w.MaxAmmo - w.Ammo);
            w.Ammo += actual;
            if (w.Ammo > w.MaxAmmo) w.Ammo = w.MaxAmmo;
            particles.SpawnText(X, Y - 10, $"+{actual} AMMO", new Color(100, 200, 255), 14);
            particles.SpawnBurst(X, Y, 8, 4, new Color(100, 200, 255), (5, 15), (2, 4), (-3, 1));
        }
        AudioManager.Pickup();
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        int sx = (int)(X - camX);
        int sy = (int)Y;
        
        // Bobbing effect
        if (_grounded)
        {
            sy += (int)(Math.Sin(_bobTimer) * 2);
        }

        // Glow effect
        float glowAlpha = 0.3f + MathF.Sin(_bobTimer * 2) * 0.2f;
        Color glowColor = Type == "health" ? new Color(100, 255, 100, (int)(glowAlpha * 100)) : new Color(100, 200, 255, (int)(glowAlpha * 100));
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 8, 16, 16), glowColor);

        // Main icon
        if (Type == "health")
        {
            // Health cross
            sb.Draw(pixel, new Rectangle(sx - 2, sy - 5, 4, 10), new Color(255, 100, 100));
            sb.Draw(pixel, new Rectangle(sx - 5, sy - 2, 10, 4), new Color(255, 100, 100));
            sb.Draw(pixel, new Rectangle(sx - 3, sy - 6, 6, 12), new Color(200, 50, 50));
            sb.Draw(pixel, new Rectangle(sx - 6, sy - 3, 12, 6), new Color(200, 50, 50));
        }
        else if (Type == "ammo")
        {
            // Ammo box
            sb.Draw(pixel, new Rectangle(sx - 5, sy - 4, 10, 8), new Color(100, 150, 200));
            sb.Draw(pixel, new Rectangle(sx - 4, sy - 3, 8, 6), new Color(150, 200, 255));
            sb.Draw(pixel, new Rectangle(sx - 2, sy - 2, 4, 4), new Color(200, 230, 255));
        }
    }
}
