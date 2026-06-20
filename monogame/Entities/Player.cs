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
    public int HitFlash = 0; // 受击闪屏计时器

    // Weapons - single weapon mode
    public WeaponDef Weapon;
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

    public float MaxHpActual => MaxHp * BuffHp;
    public float Speed => 3.2f * BuffSpeed;

    public Player(float x, float y, bool isP2, WeaponDef weapon)
    {
        X = x; Y = y; IsP2 = isP2;
        Color = isP2 ? Config.COLOR_P2 : Config.COLOR_P1;
        SkinColor = Config.COLOR_SKIN;
        HairColor = isP2 ? new Color(17, 85, 170) : new Color(85, 51, 17);
        Weapon = weapon.Clone();
    }

    public bool IsBerserk => BuffBerserk && Hp < MaxHpActual * 0.3f;

    public void Update(Dictionary<string, bool> keys, Level level, List<Bullet> bullets, ParticleSystem particles, Camera? camera = null)
    {
        if (Dead) { RespawnTimer--; return; }

        // 递减受击闪屏计时器
        if (HitFlash > 0) HitFlash--;

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
        {
            // Landing dust effect
            particles.SpawnBurst(X, Y + 13, 6, 3, new Color(136, 136, 136), (8, 18), (2, 4), (-3, 0));
            particles.SpawnBurst(X - 5, Y + 13, 3, 2, new Color(180, 180, 180), (5, 12), (1, 3), (-2, 0));
            particles.SpawnBurst(X + 5, Y + 13, 3, 2, new Color(180, 180, 180), (5, 12), (1, 3), (-2, 0));
        }
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

        // Muzzle flash effect
        float muzzleX = X + Facing * 20;
        float muzzleY = Y - 4;
        particles.Spawn(muzzleX, muzzleY, Facing * 2, 0, Color.White, 3, 8);
        particles.Spawn(muzzleX, muzzleY, Facing * 1.5f, -0.5f, w.Color, 5, 6);
        particles.Spawn(muzzleX, muzzleY, Facing * 1.5f, 0.5f, w.Color, 5, 6);
        particles.Spawn(muzzleX + Facing * 3, muzzleY, Facing * 1, 0, new Color(255, 200, 100), 4, 4);

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
        HitFlash = 15; // 设置受击闪屏时间
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

        // Shadow (larger, more defined)
        sb.Draw(pixel, new Rectangle(sx - 12, sy + 18, 24, 4), new Color(0, 0, 0, 50));
        sb.Draw(pixel, new Rectangle(sx - 10, sy + 17, 20, 2), new Color(0, 0, 0, 70));

        // Leg animation
        int legOff = 0;
        if (Grounded)
        {
            if (AnimSpd > 0.3f) legOff = (int)(Math.Sin(Anim * 1.5f) * 5);
        }
        else legOff = -4;

        // === BOOTS (detailed with laces and soles) ===
        // Left boot
        sb.Draw(pixel, new Rectangle(sx - 8 + legOff, sy + 14, 7, 5), new Color(50, 40, 30));
        sb.Draw(pixel, new Rectangle(sx - 9 + legOff, sy + 18, 9, 2), new Color(30, 25, 20)); // sole
        sb.Draw(pixel, new Rectangle(sx - 7 + legOff, sy + 15, 1, 3), new Color(70, 60, 50)); // lace
        sb.Draw(pixel, new Rectangle(sx - 5 + legOff, sy + 15, 1, 3), new Color(70, 60, 50)); // lace
        // Right boot
        sb.Draw(pixel, new Rectangle(sx + 1 - legOff, sy + 14, 7, 5), new Color(50, 40, 30));
        sb.Draw(pixel, new Rectangle(sx - legOff, sy + 18, 9, 2), new Color(30, 25, 20)); // sole
        sb.Draw(pixel, new Rectangle(sx + 2 - legOff, sy + 15, 1, 3), new Color(70, 60, 50)); // lace
        sb.Draw(pixel, new Rectangle(sx + 4 - legOff, sy + 15, 1, 3), new Color(70, 60, 50)); // lace

        // === LEGS (pants with knee pads and shading) ===
        // Left leg
        sb.Draw(pixel, new Rectangle(sx - 7 + legOff, sy + 7, 6, 8), new Color(55, 70, 100));
        sb.Draw(pixel, new Rectangle(sx - 7 + legOff, sy + 7, 2, 8), new Color(45, 60, 90)); // shadow
        sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 9, 4, 3), new Color(75, 75, 75)); // knee pad
        sb.Draw(pixel, new Rectangle(sx - 5 + legOff, sy + 10, 2, 1), new Color(95, 95, 95)); // pad highlight
        // Right leg
        sb.Draw(pixel, new Rectangle(sx + 1 - legOff, sy + 7, 6, 8), new Color(55, 70, 100));
        sb.Draw(pixel, new Rectangle(sx + 5 - legOff, sy + 7, 2, 8), new Color(45, 60, 90)); // shadow
        sb.Draw(pixel, new Rectangle(sx + 2 - legOff, sy + 9, 4, 3), new Color(75, 75, 75)); // knee pad
        sb.Draw(pixel, new Rectangle(sx + 3 - legOff, sy + 10, 2, 1), new Color(95, 95, 95)); // pad highlight

        // === BELT with detailed buckle ===
        sb.Draw(pixel, new Rectangle(sx - 9, sy + 5, 18, 3), new Color(70, 50, 30));
        sb.Draw(pixel, new Rectangle(sx - 9, sy + 5, 18, 1), new Color(90, 70, 50)); // belt highlight
        sb.Draw(pixel, new Rectangle(sx - 3, sy + 5, 6, 3), new Color(210, 180, 60)); // buckle
        sb.Draw(pixel, new Rectangle(sx - 2, sy + 6, 4, 1), new Color(240, 210, 90)); // buckle highlight

        // === BODY (tactical vest with more details) ===
        sb.Draw(pixel, new Rectangle(sx - 9, sy - 10, 18, 16), Color);
        // Vest shading
        var vestDark = new Color(Math.Max(0, Color.R - 35), Math.Max(0, Color.G - 35), Math.Max(0, Color.B - 35));
        var vestLight = new Color(Math.Min(255, Color.R + 20), Math.Min(255, Color.G + 20), Math.Min(255, Color.B + 20));
        sb.Draw(pixel, new Rectangle(sx - 9, sy - 10, 3, 16), vestDark); // left shadow
        sb.Draw(pixel, new Rectangle(sx + 6, sy - 10, 3, 16), vestDark); // right shadow
        // Vest stripes (horizontal details)
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 8, 16, 2), vestDark);
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 3, 16, 2), vestDark);
        sb.Draw(pixel, new Rectangle(sx - 8, sy + 2, 16, 2), vestDark);
        // Chest pocket
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 6, 4, 4), vestDark);
        sb.Draw(pixel, new Rectangle(sx - 5, sy - 5, 2, 2), vestLight); // pocket highlight
        
        // Shoulder pads (with highlights)
        var shoulderColor = new Color(Math.Max(0, Color.R - 45), Math.Max(0, Color.G - 45), Math.Max(0, Color.B - 45));
        var shoulderHighlight = new Color(Math.Min(255, Color.R + 10), Math.Min(255, Color.G + 10), Math.Min(255, Color.B + 10));
        sb.Draw(pixel, new Rectangle(sx - 11, sy - 9, 4, 6), shoulderColor);
        sb.Draw(pixel, new Rectangle(sx - 11, sy - 9, 4, 2), shoulderHighlight);
        sb.Draw(pixel, new Rectangle(sx + 7, sy - 9, 4, 6), shoulderColor);
        sb.Draw(pixel, new Rectangle(sx + 7, sy - 9, 4, 2), shoulderHighlight);

        // === HEAD (detailed face with more features) ===
        // Head base
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 21, 12, 12), SkinColor);
        // Face shading
        var skinDark = new Color(Math.Max(0, SkinColor.R - 30), Math.Max(0, SkinColor.G - 30), Math.Max(0, SkinColor.B - 30));
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 21, 2, 12), skinDark); // left shadow
        sb.Draw(pixel, new Rectangle(sx + 4, sy - 21, 2, 12), skinDark); // right shadow
        
        // Hair/helmet (with highlights)
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 24, 16, 5), HairColor);
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 24, 16, 2), new Color(Math.Min(255, HairColor.R + 30), Math.Min(255, HairColor.G + 30), Math.Min(255, HairColor.B + 30))); // highlight
        sb.Draw(pixel, new Rectangle(sx - 8, sy - 21, 4, 9), HairColor); // left side
        sb.Draw(pixel, new Rectangle(sx + 4, sy - 21, 4, 9), HairColor); // right side
        
        // Eyes (two separate eyes with whites)
        sb.Draw(pixel, new Rectangle(sx - 4 + f, sy - 17, 3, 2), Color.White); // eye white
        sb.Draw(pixel, new Rectangle(sx + 1 + f, sy - 17, 3, 2), Color.White); // eye white
        sb.Draw(pixel, new Rectangle(sx - 4 + f, sy - 17, 2, 2), new Color(20, 20, 20)); // pupil
        sb.Draw(pixel, new Rectangle(sx + 1 + f, sy - 17, 2, 2), new Color(20, 20, 20)); // pupil
        // Eye highlights
        sb.Draw(pixel, new Rectangle(sx - 3 + f, sy - 17, 1, 1), new Color(220, 255, 220));
        sb.Draw(pixel, new Rectangle(sx + 2 + f, sy - 17, 1, 1), new Color(220, 255, 220));
        
        // Eyebrows
        sb.Draw(pixel, new Rectangle(sx - 4 + f, sy - 19, 3, 1), new Color(40, 30, 20));
        sb.Draw(pixel, new Rectangle(sx + 1 + f, sy - 19, 3, 1), new Color(40, 30, 20));
        
        // Nose
        sb.Draw(pixel, new Rectangle(sx - 1 + f, sy - 15, 2, 2), skinDark);
        
        // Mouth (with teeth when berserk)
        if (IsBerserk)
        {
            sb.Draw(pixel, new Rectangle(sx - 2 + f, sy - 12, 5, 2), new Color(120, 40, 40)); // open mouth
            sb.Draw(pixel, new Rectangle(sx - 1 + f, sy - 12, 3, 1), Color.White); // teeth
        }
        else
        {
            sb.Draw(pixel, new Rectangle(sx - 2 + f, sy - 12, 5, 2), new Color(160, 110, 80));
        }

        // === ARM holding weapon (with muscle definition) ===
        sb.Draw(pixel, new Rectangle(sx + f * 9, sy - 10, 5, 11), SkinColor);
        sb.Draw(pixel, new Rectangle(sx + f * 9, sy - 10, 2, 11), skinDark); // arm shadow
        sb.Draw(pixel, new Rectangle(sx + f * 9, sy - 6, 5, 2), new Color(Math.Min(255, SkinColor.R + 15), Math.Min(255, SkinColor.G + 15), Math.Min(255, SkinColor.B + 15))); // muscle highlight
        // Glove (with details)
        sb.Draw(pixel, new Rectangle(sx + f * 9, sy - 2, 5, 3), new Color(45, 45, 45));
        sb.Draw(pixel, new Rectangle(sx + f * 9, sy - 2, 5, 1), new Color(65, 65, 65)); // glove highlight
        
        // === WEAPON (type-specific) ===
        DrawWeapon(sb, pixel, sx, sy, f);

        // === EFFECTS ===
        // Berserk aura (pulsing)
        if (IsBerserk)
        {
            float pulse = (float)Math.Sin(Anim * 0.2f) * 0.3f + 0.7f;
            var auraColor = new Color(255, 0, 0, (int)(40 * pulse));
            DrawCircle(sb, pixel, sx, sy - 2, 25, auraColor);
        }

        // Shield indicator (shimmering)
        if (BuffShield && _shieldReady)
        {
            float shimmer = (float)Math.Sin(Anim * 0.15f) * 0.3f + 0.7f;
            var shieldColor = new Color(100, 200, 255, (int)(60 * shimmer));
            DrawCircle(sb, pixel, sx, sy - 2, 20, shieldColor);
        }
    }

    private void DrawWeapon(SpriteBatch sb, Texture2D pixel, int sx, int sy, int f)
    {
        int wx = sx + f * 14;
        int wy = sy - 6;
        string weaponId = Weapon.Id;

        // Helper function to draw rectangles with proper facing direction
        void DrawRect(int x, int y, int w, int h, Color color)
        {
            if (w < 0) { x += w; w = -w; }
            sb.Draw(pixel, new Rectangle(x, y, w, h), color);
        }

        switch (weaponId)
        {
            case "rifle": // Assault Rifle - M16 style
                // Barrel
                DrawRect(wx, wy + 1, f * 14, 3, new Color(80, 80, 80));
                // Body
                DrawRect(wx - f * 2, wy, f * 10, 5, new Color(100, 100, 100));
                // Magazine
                DrawRect(wx + f * 2, wy + 5, 3, 6, new Color(60, 60, 60));
                DrawRect(wx + f * 1, wy + 10, 5, 2, new Color(50, 50, 50));
                // Stock
                DrawRect(wx - f * 6, wy + 1, f * 6, 4, new Color(90, 65, 40));
                // Sight
                DrawRect(wx + f * 4, wy - 2, 3, 2, new Color(120, 120, 120));
                break;

            case "shotgun": // Pump Shotgun
                // Barrel (longer)
                DrawRect(wx, wy + 1, f * 18, 4, new Color(85, 85, 85));
                // Pump handle
                DrawRect(wx + f * 6, wy - 1, f * 6, 2, new Color(110, 110, 110));
                // Body (wooden)
                DrawRect(wx - f * 3, wy, f * 12, 6, new Color(140, 90, 50));
                // Stock (wooden)
                DrawRect(wx - f * 8, wy + 1, f * 8, 5, new Color(120, 75, 40));
                // Trigger guard
                DrawRect(wx + f * 1, wy + 6, 2, 3, new Color(70, 70, 70));
                break;

            case "flame": // Flamethrower
                // Nozzle
                DrawRect(wx, wy, f * 8, 5, new Color(70, 70, 70));
                DrawRect(wx + f * 8, wy + 1, f * 4, 3, new Color(90, 90, 90));
                // Fuel tank (backpack style)
                DrawRect(wx - f * 6, wy - 2, f * 8, 8, new Color(180, 60, 40));
                DrawRect(wx - f * 5, wy - 1, f * 6, 6, new Color(200, 80, 50));
                // Hose
                DrawRect(wx - f * 2, wy + 4, f * 4, 2, new Color(60, 60, 60));
                // Handle
                DrawRect(wx + f * 2, wy + 5, 3, 4, new Color(50, 50, 50));
                break;

            case "laser": // Laser Gun
                // Barrel (sleek)
                DrawRect(wx, wy + 1, f * 16, 3, new Color(60, 180, 220));
                DrawRect(wx + f * 14, wy, f * 3, 5, new Color(80, 200, 240));
                // Body
                DrawRect(wx - f * 2, wy, f * 10, 5, new Color(50, 150, 200));
                // Energy core
                DrawRect(wx + f * 3, wy + 1, 4, 3, new Color(150, 240, 255));
                // Stock
                DrawRect(wx - f * 6, wy + 1, f * 5, 4, new Color(40, 120, 170));
                // Sight
                DrawRect(wx + f * 6, wy - 2, 2, 2, new Color(200, 255, 255));
                break;

            case "rocket": // Rocket Launcher
                // Launch tube
                DrawRect(wx, wy, f * 20, 6, new Color(80, 110, 60));
                DrawRect(wx + f * 18, wy - 1, f * 3, 8, new Color(100, 130, 75));
                // Rocket tip visible
                DrawRect(wx + f * 2, wy + 1, f * 8, 4, new Color(180, 50, 50));
                // Handle
                DrawRect(wx + f * 10, wy + 6, 3, 5, new Color(60, 60, 60));
                // Sight
                DrawRect(wx + f * 12, wy - 2, 4, 2, new Color(90, 90, 90));
                // Back cap
                DrawRect(wx - f * 2, wy + 1, f * 3, 5, new Color(70, 70, 70));
                break;

            default: // Generic weapon
                DrawRect(wx - 4, wy, 10, 5, new Color(90, 90, 90));
                DrawRect(wx + f * 5, wy + 1, 6, 4, new Color(60, 60, 60));
                DrawRect(wx - 1, wy - 2, 4, 2, new Color(120, 120, 120));
                break;
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