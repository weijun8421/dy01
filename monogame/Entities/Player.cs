using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;
using DY01.Engine;
using DY01.Game;

namespace DY01.Entities;

public class Player
{
    public float X, Y, Vx, Vy;
    public int W = 20, H = 32;
    public bool IsP2, Grounded, Dead, HoldingJump, WasInAir = true;
    public Color Color, SkinColor, HairColor;
    public float Hp = 100, MaxHp = 100;
    public int Facing = 1;
    public float Anim = 0, AnimSpd = 0;
    public int RespawnTimer = 0, Invincible = 0, DashCooldown = 0;
    public int Coyote = 0, JumpBuffer = 0;

    // Weapons
    public int WeaponIdx = 0;
    public WeaponDef[] Weapons = new WeaponDef[5];
    public int FireCd = 0, Reloading = 0;

    // Buffs
    public float BuffDmg = 1f, BuffSpeed = 1f, BuffFireRate = 1f, BuffHp = 1f, BuffExplosion = 1f;
    public bool BuffPierce, BuffDouble, BuffBerserk, BuffNuke;
    public int BuffVampire = 0, BuffRegen = 0;
    public Dictionary<string, float> BuffWeaponDmg = new();
    public int BuffSgPellets = 0;
    public bool BuffBurn, BuffLaserBeam, BuffBurst, BuffSlug, BuffChain, BuffCluster;
    public float BuffCritChance = 0, BuffFlameRange = 0;
    public bool BuffShield, BuffTurret, BuffStun, BuffDragon, BuffInfAmmo, BuffDeathBlow;
    public float BuffReloadSpeed = 0;
    public int BuffExtraBullets = 0;
    public List<BuffDef> ActiveBuffs = new();
    public int ShieldCd = 0;
    private bool _shieldReady = true;
    private int _regenTimer = 0;
    private Random _rng = new();

    public WeaponDef Weapon => Weapons[WeaponIdx];
    public float MaxHpActual => MaxHp * BuffHp;
    public float Speed => 3.2f * BuffSpeed;

    public Player(float x, float y, bool isP2)
    {
        X = x; Y = y; IsP2 = isP2;
        Color = isP2 ? Config.COLOR_P2 : Config.COLOR_P1;
        SkinColor = Config.COLOR_SKIN;
        HairColor = isP2 ? new Color(17, 85, 170) : new Color(85, 51, 17);
        for (int i = 0; i < 5; i++) Weapons[i] = Config.WEAPONS[i].Clone();
    }

    public bool IsBerserk => BuffBerserk && Hp < MaxHpActual * 0.3f;

    public void SwitchWeapon(int idx)
    {
        if (idx >= 0 && idx < Weapons.Length)
        {
            // Complete any pending reload before switching
            if (Reloading > 0)
            {
                int need = Weapon.MaxAmmo - Weapon.Ammo;
                int take = Math.Min(need, Weapon.Reserve);
                Weapon.Ammo += take;
                Weapon.Reserve -= take;
                Reloading = 0;
            }
            WeaponIdx = idx;
        }
    }

    public void Update(Dictionary<string, bool> keys, Level level, List<Bullet> bullets, ParticleSystem particles, Camera? camera = null)
    {
        if (Dead) { RespawnTimer--; return; }

        string p = IsP2 ? "p2_" : "p1_";
        bool left = keys.GetValueOrDefault(p + "left");
        bool right = keys.GetValueOrDefault(p + "right");
        bool jump = keys.GetValueOrDefault(p + "jump");
        bool dash = keys.GetValueOrDefault(p + "dash");
        bool shoot = keys.GetValueOrDefault(p + "shoot");
        bool melee = keys.GetValueOrDefault(p + "melee");
        bool reloadK = keys.GetValueOrDefault(p + "reload_pressed");

        // Movement
        if (left && right)
        {
            Vx = Physics.Accelerate(Vx, Speed, Config.ACCEL, -Facing);
            Facing = -Facing;
        }
        else if (left)
        {
            Vx = Physics.Accelerate(Vx, Speed, Config.ACCEL, -1);
            Facing = -1;
        }
        else if (right)
        {
            Vx = Physics.Accelerate(Vx, Speed, Config.ACCEL, 1);
            Facing = 1;
        }
        else
        {
            Vx = Physics.ApplyFriction(Vx);
        }

        // Dash
        if (dash && DashCooldown <= 0)
        {
            Vx = Facing * Speed * 3.5f;
            DashCooldown = 45;
            Invincible = 18;
            AudioManager.Dash();
            particles.SpawnBurst(X, Y + 16, 8, 4, Color.White, (10, 20), (2, 4), (-3, 1));
        }
        if (DashCooldown > 0) DashCooldown--;

        // Shield cooldown
        if (!_shieldReady)
        {
            ShieldCd--;
            if (ShieldCd <= 0) _shieldReady = true;
        }

        // Regen
        if (BuffRegen > 0 && Hp < MaxHpActual)
        {
            _regenTimer++;
            if (_regenTimer >= 60) { _regenTimer = 0; Heal(BuffRegen); }
        }

        // Jump
        HoldingJump = jump;
        if (jump) JumpBuffer = Config.JUMP_BUFFER;
        else if (JumpBuffer > 0) JumpBuffer--;

        if (Grounded) Coyote = Config.COYOTE_FRAMES;
        else if (Coyote > 0) Coyote--;

        if (JumpBuffer > 0 && Coyote > 0)
        {
            Vy = Config.JUMP_VEL;
            Grounded = false;
            Coyote = 0;
            JumpBuffer = 0;
            AudioManager.Jump();
            particles.SpawnBurst(X, Y + 16, 4, 2, new Color(204, 204, 204), (8, 15), (2, 3), (-1, 0));
        }

        if (!HoldingJump && Vy < Config.SHORT_JUMP) Vy = Config.SHORT_JUMP;

        // Gravity
        Vy = Physics.ApplyGravity(Vy);
        X += Vx; Y += Vy;

        // Collision
        Grounded = false;
        (X, Y, Vx, Vy, Grounded) = Physics.ResolveCollision(X, Y, W, H, Vx, Vy, level);

        // Wall climb - help player climb over edges
        if (!Grounded && Vy >= 0 && Vy < 2)
        {
            // Check if player is near a wall edge
            float checkX = Facing > 0 ? X + W / 2f + 2 : X - W / 2f - 2;
            float checkY = Y - H / 2f - 2;
            var tiles = level.GetTiles(checkX, checkY, 4, 4);
            bool wallAbove = false;
            foreach (var t in tiles)
            {
                if (t.Solid) { wallAbove = true; break; }
            }

            // Check if there's ground at feet level
            float feetCheckY = Y + H / 2f + 4;
            var feetTiles = level.GetTiles(checkX, feetCheckY, 4, 4);
            bool groundAhead = false;
            foreach (var t in feetTiles)
            {
                if (t.Solid) { groundAhead = true; break; }
            }

            // If wall above and ground ahead, give a small boost
            if (wallAbove && groundAhead && (left || right || jump))
            {
                Vy = -2.5f;
                particles.SpawnBurst(checkX, Y, 3, 2, new Color(180, 180, 180), (5, 10), (1, 2), (-1, 0));
            }
        }

        if (Grounded && WasInAir && Vy > 3)
            particles.SpawnBurst(X, Y + 13, 6, 3, new Color(136, 136, 136), (8, 18), (2, 4), (-3, 0));
        WasInAir = !Grounded;

        if (Invincible > 0) Invincible--;

        // Reload - only start if not already reloading and has room/ammo
        if (reloadK && Reloading <= 0 && Weapon.Ammo < Weapon.MaxAmmo && Weapon.Reserve > 0)
        {
            Reloading = Math.Max(20, (int)(Weapon.ReloadTime * Math.Max(0.1f, 1f - BuffReloadSpeed)));
            AudioManager.Pickup();
        }
        if (Reloading > 0)
        {
            Reloading--;
            if (Reloading <= 0)
            {
                int need = Weapon.MaxAmmo - Weapon.Ammo;
                int take = Math.Min(need, Weapon.Reserve);
                Weapon.Ammo += take;
                Weapon.Reserve -= take;
                Reloading = 0; // Ensure it's reset
            }
        }

        // Shoot
        if (FireCd > 0) FireCd--;
        if ((shoot || melee) && FireCd <= 0 && Reloading <= 0)
        {
            if (Weapon.Ammo > 0)
                Fire(bullets, particles, camera);
            else if (Weapon.Reserve > 0 && Reloading == 0)
            {
                Reloading = Math.Max(20, (int)(Weapon.ReloadTime * Math.Max(0.1f, 1f - BuffReloadSpeed)));
                AudioManager.Pickup();
            }
        }
        AnimSpd = Math.Abs(Vx);
        Anim += AnimSpd * 0.15f;
        if (Anim > 4) Anim -= 4;

        X = Math.Clamp(X, 10, 5000);

        // Fall death check - if player falls below the map, die
        float mapBottom = level.H * Config.TILE + 100;
        if (Y > mapBottom)
        {
            Die(particles);
        }
    }

    private void Fire(List<Bullet> bullets, ParticleSystem particles, Camera? camera)
    {
        var w = Weapon;
        int fr = Math.Max(2, (int)(w.FireRate / BuffFireRate));
        FireCd = fr;

        if (!BuffInfAmmo) w.Ammo--;

        float angle = Facing > 0 ? 0 : MathF.PI;
        int pellets = w.Pellets + BuffExtraBullets;
        if (w.Id == "shotgun") pellets += BuffSgPellets;
        if (BuffSlug && w.Id == "shotgun") pellets = 1;

        int shots = BuffDouble ? 2 : 1;
        if (BuffBurst && w.Id == "rifle") shots = 3;
        if (BuffCluster && w.Id == "rocket") pellets = 3;

        AudioManager.Shoot(w.Id switch
        {
            "laser" => "laser",
            "flame" => "flame",
            "shotgun" => "shotgun",
            "rocket" => "rocket",
            _ => "rifle",
        });

        for (int s = 0; s < shots; s++)
        {
            for (int i = 0; i < pellets; i++)
            {
                float sp = (i - (pellets - 1) / 2f) * w.Spread;
                float a = angle + sp + (float)(_rng.NextDouble() * 0.06f - 0.03f);

                float dmg = w.Damage * BuffDmg;
                if (BuffWeaponDmg.TryGetValue(w.Id, out float wdmg)) dmg *= (1 + wdmg);
                if (BuffSlug && w.Id == "shotgun") dmg *= 3.5f;
                if (IsBerserk) dmg *= 2;

                bool isCrit = (float)_rng.NextDouble() < BuffCritChance;
                if (isCrit) dmg *= 3;

                bool isDeathBlow = BuffDeathBlow && (float)_rng.NextDouble() < 0.20f;
                if (isDeathBlow) dmg = 9999;

                var bulletData = new WeaponDef
                {
                    Name = w.Name, Id = w.Id, Damage = dmg, FireRate = w.FireRate,
                    BulletSpeed = w.BulletSpeed, Spread = w.Spread,
                    Ammo = w.Ammo, MaxAmmo = w.MaxAmmo, Reserve = w.Reserve,
                    ReloadTime = w.ReloadTime, Pellets = w.Pellets,
                    Explosive = w.Explosive, ExplosionRadius = w.ExplosionRadius,
                    Pierce = w.Pierce, Color = w.Color, BulletW = w.BulletW, BulletH = w.BulletH,
                };

                if (BuffBurn && w.Id == "flame")
                {
                    // Store burn flag in a way we can check later
                    bulletData.Name = "burn";
                }

                if (BuffFlameRange > 0 && w.Id == "flame")
                {
                    bulletData.BulletW = (int)(w.BulletW * (1 + BuffFlameRange));
                    bulletData.BulletH = (int)(w.BulletH * (1 + BuffFlameRange));
                }

                if (BuffLaserBeam && w.Id == "laser")
                {
                    bulletData.BulletW = (int)(w.BulletW * 2.5f);
                    bulletData.BulletH = (int)(w.BulletH * 1.5f);
                }

                string ownerId = IsP2 ? "p2" : "p1";
                bullets.Add(new Bullet(
                    X + Facing * 15, Y - 4,
                    MathF.Cos(a) * w.BulletSpeed,
                    MathF.Sin(a) * w.BulletSpeed,
                    bulletData, ownerId));
            }
        }

        Vx -= MathF.Cos(angle) * (w.Explosive ? 2.5f : 0.8f);
        Vy -= MathF.Sin(angle) * 0.3f;

        particles.SpawnBurst(X + Facing * 20, Y - 4, 8, 10, w.Color, (4, 10), (2, 5), (-3, 3));
        camera?.AddShake(w.Explosive ? 10 : 3);
    }

    public void TakeDamage(float dmg, ParticleSystem particles, Action<float> shakeFunc)
    {
        if (Dead || Invincible > 0) return;

        if (BuffShield && _shieldReady)
        {
            _shieldReady = false;
            ShieldCd = 300;
            particles.SpawnBurst(X, Y - 10, 8, 5, new Color(100, 200, 255), (10, 20), (2, 4), (-3, 1));
            particles.SpawnText(X, Y - 20, "SHIELD", new Color(100, 200, 255));
            return;
        }

        Hp -= dmg;
        Invincible = 28;
        shakeFunc(7);
        AudioManager.Hit();
        particles.SpawnBurst(X, Y, 10, 4, Color.Red, (10, 25), (2, 5), (-4, 2));
        particles.SpawnText(X, Y - 15, $"-{(int)dmg}", new Color(255, 68, 68));
        if (Hp <= 0) { Hp = 0; Die(particles); }
    }

    public void Die(ParticleSystem particles)
    {
        Dead = true;
        RespawnTimer = 200;
        AudioManager.Die();
        
        // 更戏剧化的死亡效果
        particles.SpawnBurst(X, Y, 40, 8, Color, (30, 60), (4, 9), (-10, 3));
        particles.SpawnBurst(X, Y, 20, 5, new Color(255, 200, 0), (20, 40), (3, 6), (-6, 2));
        particles.SpawnText(X, Y - 30, "DEAD!", new Color(255, 50, 50), 20);
    }

    public void Respawn(Player? other, ParticleSystem? particles = null)
    {
        Dead = false;
        Hp = MaxHpActual * 0.5f;
        Invincible = 90; // 增加无敌时间
        
        if (other != null && !other.Dead)
        {
            X = other.X + (IsP2 ? 40 : -40);
            Y = other.Y;
        }
        else { X = 100; Y = 300; }
        
        // 复活特效
        if (particles != null)
        {
            particles.SpawnBurst(X, Y, 25, 6, new Color(100, 255, 100), (15, 35), (3, 7), (-5, 2));
            particles.SpawnBurst(X, Y, 15, 4, new Color(255, 255, 255), (10, 25), (2, 5), (-3, 1));
            particles.SpawnText(X, Y - 30, "RESPAWN!", new Color(100, 255, 100), 18);
        }
    }

    public void Heal(float amt)
    {
        if (Dead) return;
        Hp = Math.Min(MaxHpActual, Hp + amt);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        if (Dead) return;
        if (Invincible > 0 && (Invincible / 4) % 2 == 0) return;

        int sx = (int)(X - camX), sy = (int)Y;
        int f = Facing;

        // Shadow
        sb.Draw(pixel, new Rectangle(sx - 9, sy + 15, 18, 4), new Color(0, 0, 0, 50));

        // Leg animation
        int legOff = 0;
        if (Grounded)
        {
            if (AnimSpd > 0.3f) legOff = (int)(Math.Sin(Anim * 1.5f) * 5);
        }
        else legOff = -4;

        // Boots (darker, wider)
        sb.Draw(pixel, new Rectangle(sx - 8 + legOff, sy + 15, 8, 4), new Color(40, 30, 20));
        sb.Draw(pixel, new Rectangle(sx - 1 - legOff, sy + 15, 8, 4), new Color(40, 30, 20));
        // Legs (pants)
        sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 8, 6, 9), new Color(60, 75, 110));
        sb.Draw(pixel, new Rectangle(sx - legOff, sy + 8, 6, 9), new Color(60, 75, 110));
        // Knee pads
        sb.Draw(pixel, new Rectangle(sx - 5 + legOff, sy + 10, 4, 3), new Color(80, 80, 80));
        sb.Draw(pixel, new Rectangle(sx + 1 - legOff, sy + 10, 4, 3), new Color(80, 80, 80));

        // Body (torso with armor)
        sb.Draw(pixel, new Rectangle(sx - 9, sy - 10, 18, 20), Color);
        // Chest plate detail
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 8, 16, 4), new Color(
            Math.Max(0, Color.R - 30), Math.Max(0, Color.G - 30), Math.Max(0, Color.B - 30)));
        // Belt
        sb.Draw(pixel, new Rectangle(sx - 9, sy + 6, 18, 3), new Color(60, 45, 25));
        // Belt buckle
        sb.Draw(pixel, new Rectangle(sx - 3, sy + 6, 6, 3), new Color(200, 170, 50));
        // Shoulder pads
        sb.Draw(pixel, new Rectangle(sx - 10, sy - 9, 4, 5), new Color(
            Math.Max(0, Color.R - 40), Math.Max(0, Color.G - 40), Math.Max(0, Color.B - 40)));
        sb.Draw(pixel, new Rectangle(sx + 6, sy - 9, 4, 5), new Color(
            Math.Max(0, Color.R - 40), Math.Max(0, Color.G - 40), Math.Max(0, Color.B - 40)));

        // Head
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 20, 12, 12), SkinColor);
        // Helmet/hair
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 23, 16, 5), HairColor);
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 20, 4, 8), HairColor);
        sb.Draw(pixel, new Rectangle(sx + 4, sy - 20, 4, 8), HairColor);
        // Visor/goggles
        sb.Draw(pixel, new Rectangle(sx + f * 2 - 4, sy - 16, 9, 4), new Color(30, 30, 30));
        // Eye glow
        sb.Draw(pixel, new Rectangle(sx + f * 2, sy - 15, 3, 2), new Color(200, 255, 200));
        // Mouth
        sb.Draw(pixel, new Rectangle(sx + f * 2, sy - 11, 4, 2), new Color(180, 130, 100));

        // Arm + weapon
        sb.Draw(pixel, new Rectangle(sx + f * 10, sy - 10, 5, 10), SkinColor);
        // Glove
        sb.Draw(pixel, new Rectangle(sx + f * 10, sy - 3, 5, 3), new Color(50, 50, 50));
        // Weapon body
        int wx = sx + f * 15;
        sb.Draw(pixel, new Rectangle(wx - 4, sy - 6, 10, 5), new Color(90, 90, 90));
        // Weapon barrel
        sb.Draw(pixel, new Rectangle(wx + f * 5, sy - 5, 6, 4), new Color(60, 60, 60));
        // Weapon detail
        sb.Draw(pixel, new Rectangle(wx - 1, sy - 8, 4, 2), new Color(120, 120, 120));

        // Berserk aura
        if (IsBerserk)
        {
            var auraColor = new Color(255, 0, 0, 40);
            DrawCircle(sb, pixel, sx, sy - 2, 25, auraColor);
        }

        // Shield indicator
        if (BuffShield && _shieldReady)
        {
            var shieldColor = new Color(100, 200, 255, 60);
            DrawCircle(sb, pixel, sx, sy - 2, 20, shieldColor);
        }
    }

    private void DrawCircle(SpriteBatch sb, Texture2D pixel, int cx, int cy, int radius, Color color)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radius * radius && dx * dx + dy * dy >= (radius - 1) * (radius - 1))
                {
                    sb.Draw(pixel, new Rectangle(cx + dx, cy + dy, 1, 1), color);
                }
            }
        }
    }
}