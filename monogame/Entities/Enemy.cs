using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;
using DY01.Engine;
using DY01.Game;

namespace DY01.Entities;

public class Enemy
{
    public float X, Y, Vx, Vy, Hp, MaxHp, Speed, Damage;
    public int W, H, Score;
    public bool Fly, Grounded, Dead;
    public Color Color;
    public string Type;
    public int Facing = 1;
    public int Flash = 0;
    public float Anim = 0;

    private float _patrolCenter;
    private float _patrolRange;
    private int _patrolDir = 1;
    private int _patrolTimer;
    private bool _aggro;
    private float _detectRange = 220;
    private int _attackCd = 30;
    private int _stun = 0;
    private int _burn = 0;
    private int _burnTimer = 0;
    private Random _rng = new();

    public Enemy(float x, float y, string etype)
    {
        X = x; Y = y; Type = etype;
        var t = Config.ENEMY_TYPES[etype];
        W = t.W; H = t.H; Hp = t.HP; MaxHp = t.HP;
        Speed = t.Speed; Damage = t.Damage; Color = t.Color; Score = t.Score;
        Fly = t.Fly;

        _patrolCenter = x;
        _patrolRange = 80 + _rng.Next(0, 60);
        _patrolDir = 1;
        _patrolTimer = _rng.Next(30, 90);
    }

    public bool Update(Player p1, Player? p2, Level level, ParticleSystem particles, Camera? camera = null)
    {
        Anim += 1;
        if (Flash > 0) Flash--;
        if (_attackCd > 0) _attackCd--;

        if (_stun > 0)
        {
            _stun--;
            return true;
        }

        if (_burn > 0)
        {
            _burnTimer--;
            if (_burnTimer <= 0)
            {
                _burnTimer = 15;
                Hp -= _burn;
                Flash = 4;
                particles.SpawnBurst(X, Y - 10, 3, 2, new Color(255, 136, 0), (5, 10), (1, 2), (-2, 0));
                if (Hp <= 0) { Hp = 0; return true; }
            }
            _burn--;
        }

        var (tgt, d) = FindTarget(p1, p2);

        if (tgt != null && d < _detectRange)
            _aggro = true;
        else if (tgt != null && d > _detectRange * 1.3f)
            _aggro = false;

        if (tgt == null) return true;

        if (Fly) UpdateFly(tgt, d);
        else UpdateGround(tgt, d, level);

        if (d < (W + tgt.W) / 2f + 4 && _attackCd <= 0)
        {
            tgt.TakeDamage(Damage, particles, amt => camera?.AddShake(amt));
            _attackCd = 30;
        }

        // Fall death check - if enemy falls below the map, remove it
        float mapBottom = level.H * Config.TILE + 100;
        if (Y > mapBottom)
        {
            return false;
        }

        return Hp > 0;
    }

    private (Player?, float) FindTarget(Player p1, Player? p2)
    {
        Player? tgt = null;
        float d = float.MaxValue;
        if (p1 != null && !p1.Dead)
        { tgt = p1; d = MathF.Sqrt((X - p1.X) * (X - p1.X) + (Y - p1.Y) * (Y - p1.Y)); }
        if (p2 != null && !p2.Dead)
        {
            float d2 = MathF.Sqrt((X - p2.X) * (X - p2.X) + (Y - p2.Y) * (Y - p2.Y));
            if (d2 < d) { tgt = p2; d = d2; }
        }
        return (tgt, d);
    }

    private void UpdateFly(Player tgt, float d)
    {
        float dx = tgt.X - X, dy = tgt.Y - Y;
        if (d > 5)
        {
            Vx = dx / d * Speed;
            Vy = dy / d * Speed;
        }
        Facing = Vx >= 0 ? 1 : -1;
        X += Vx; Y += Vy;
    }

    private void UpdateGround(Player tgt, float d, Level level)
    {
        if (_aggro && d > 5)
        {
            float dx = tgt.X - X;
            float accel = Type == "heavy" ? 0.2f : 0.3f;
            Vx = Physics.Accelerate(Vx, Speed, accel, dx > 0 ? 1 : -1);
            Facing = dx > 0 ? 1 : -1;
            
            // Jump if target is above and grounded
            if (Grounded && tgt.Y < Y - 30 && d < 150 && _rng.NextDouble() < 0.02)
            {
                Vy = -8;
            }
        }
        else
        {
            _patrolTimer--;
            if (_patrolTimer <= 0)
            {
                _patrolDir *= -1;
                _patrolTimer = _rng.Next(40, 100);
            }
            float offset = X - _patrolCenter;
            if (Math.Abs(offset) > _patrolRange)
                _patrolDir = offset > 0 ? -1 : 1;
            Vx = Physics.Accelerate(Vx, Speed * 0.5f, 0.15f, _patrolDir);
            Facing = _patrolDir;
        }

        Vy = Physics.ApplyGravity(Vy);
        X += Vx; Y += Vy;
        Grounded = false;
        (X, Y, Vx, Vy, Grounded) = Physics.ResolveCollision(X, Y, W, H, Vx, Vy, level);
    }

    public bool Hit(float dmg, ParticleSystem particles, float knockbackDir = 0)
    {
        Hp -= dmg;
        Flash = 6;
        _aggro = true;
        
        // Knockback
        if (knockbackDir != 0)
        {
            Vx = knockbackDir * 5;
            Vy = -3;
        }
        
        if (Hp <= 0)
        {
            particles.SpawnBurst(X, Y, 20, 6, Color, (20, 40), (2, 7), (-8, 2));
            particles.SpawnText(X, Y, $"+{Score}", new Color(255, 170, 0), 16);
            AudioManager.Kill();
            return true;
        }
        AudioManager.Hit();
        return false;
    }

    public void ApplyEffects(WeaponDef bulletData)
    {
        // Check for stun, burn, etc. via reflection-like approach
        // We'll use tags stored in the weapon data
        if (bulletData.Id == "shotgun" && Flash > 0) _stun = 60; // Simplified
        if (Flash > 0 && bulletData.Id == "flame") { _burn = 120; _burnTimer = 0; }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        int sx = (int)(X - camX), sy = (int)Y;
        var color = Flash > 0 ? Color.White : Color;
        int f = Facing;
        int hw = W / 2, hh = H / 2;

        if (Type == "boss")
        {
            // Boss body with armor
            sb.Draw(pixel, new Rectangle(sx - hw, sy - hh, W, H), color);
            // Armor plates
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy - hh + 3, W - 6, 5), new Color(100, 100, 100));
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy + hh - 8, W - 6, 5), new Color(100, 100, 100));
            // Eyes
            var eyeC = Flash > 0 ? Color.White : new Color(255, 50, 50);
            sb.Draw(pixel, new Rectangle(sx - 12, sy - 18, 10, 8), eyeC);
            sb.Draw(pixel, new Rectangle(sx + 2, sy - 18, 10, 8), eyeC);
            // Shoulder pads
            sb.Draw(pixel, new Rectangle(sx - hw - 5, sy - hh + 3, 8, 12), new Color(80, 80, 80));
            sb.Draw(pixel, new Rectangle(sx + hw - 3, sy - hh + 3, 8, 12), new Color(80, 80, 80));
            // Weapon
            sb.Draw(pixel, new Rectangle(sx + f * hw, sy - 5, f * 10, 5), new Color(60, 60, 60));
        }
        else if (Fly)
        {
            // Flying enemy with wings
            int wy = (int)(Math.Sin(Anim * 0.25) * 5);
            sb.Draw(pixel, new Rectangle(sx - hw, sy - hh, W, H), color);
            // Wings
            var wingC = Flash > 0 ? Color.White : new Color(170, 136, 255);
            sb.Draw(pixel, new Rectangle(sx - hw - 4, sy + wy - 3, 6, 5), wingC);
            sb.Draw(pixel, new Rectangle(sx + hw - 2, sy - wy - 3, 6, 5), wingC);
            // Eye
            sb.Draw(pixel, new Rectangle(sx + f * 2, sy - 6, 5, 5), Flash > 0 ? Color.White : Color.Yellow);
            // Propeller effect
            if ((int)Anim % 4 < 2)
            {
                sb.Draw(pixel, new Rectangle(sx - 3, sy - hh - 3, 6, 3), new Color(200, 200, 200, 150));
            }
        }
        else
        {
            // Ground enemy with better detail
            int legOff = 0;
            if (Grounded && Math.Abs(Vx) > 0.2f)
                legOff = (int)(Math.Sin(Anim * 0.15f * Speed * 2) * 5);
            else if (!Grounded) legOff = -3;

            // Legs with boots
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 6, 6, 9), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 6, 6, 9), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 14, 7, 4), new Color(30, 30, 30));
            sb.Draw(pixel, new Rectangle(sx - 1 - legOff, sy + 14, 7, 4), new Color(30, 30, 30));

            // Body with vest
            int bodyH = H - 12;
            sb.Draw(pixel, new Rectangle(sx - hw, sy - bodyH + 3, W, bodyH), color);
            // Vest detail
            sb.Draw(pixel, new Rectangle(sx - hw + 2, sy - bodyH + 5, W - 4, 4), new Color(color.R / 2, color.G / 2, color.B / 2));
            // Belt
            sb.Draw(pixel, new Rectangle(sx - hw, sy + 5, W, 3), new Color(60, 40, 20));

            // Head with helmet
            int headY = sy - bodyH - 3;
            var skinC = SkinColor();
            sb.Draw(pixel, new Rectangle(sx - 5, headY, 10, 10), skinC);
            var helmetC = Type == "elite" ? new Color(80, 80, 80) : HairColor();
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 4, 12, 5), helmetC);
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 1, 4, 6), helmetC);
            sb.Draw(pixel, new Rectangle(sx + 2, headY - 1, 4, 6), helmetC);
            // Eye
            sb.Draw(pixel, new Rectangle(sx + f * 2, headY + 3, 4, 3), Flash > 0 ? Color.White : Color.Yellow);
            // Weapon in hand
            sb.Draw(pixel, new Rectangle(sx + f * 8, sy - 8, f * 6, 4), new Color(70, 70, 70));

            // Aggro indicator
            if (_aggro && Flash <= 0)
            {
                var exColor = new Color(255, 100, 100);
                sb.Draw(pixel, new Rectangle(sx - 1, headY - 15, 3, 8), exColor);
                sb.Draw(pixel, new Rectangle(sx - 1, headY - 18, 3, 3), exColor);
            }
        }

        // HP bar
        if (Hp < MaxHp)
        {
            int bw = W + 8;
            sb.Draw(pixel, new Rectangle(sx - bw / 2, sy - H / 2 - 11, bw, 5), new Color(34, 34, 34));
            float hpR = Hp / MaxHp;
            var hpCol = hpR > 0.3f ? Color.Red : new Color(255, 100, 0);
            sb.Draw(pixel, new Rectangle(sx - bw / 2, sy - H / 2 - 11, (int)(bw * hpR), 5), hpCol);
        }
    }

    private Color SkinColor() => Type switch
    {
        "soldier" => new Color(220, 170, 140),
        "elite" => new Color(200, 140, 100),
        "heavy" => new Color(240, 100, 80),
        _ => new Color(220, 170, 140),
    };

    private Color HairColor() => Type switch
    {
        "soldier" => new Color(60, 50, 30),
        "elite" => new Color(40, 35, 25),
        "heavy" => new Color(80, 30, 20),
        _ => new Color(60, 50, 30),
    };
}