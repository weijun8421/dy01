using System;
using System.Collections.Generic;
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

    // Bomber-specific
    private bool _bomberArming = false;
    private int _bomberFuse = 0;
    private const int BOMBER_FUSE_MAX = 45; // 0.75 seconds to explode
    private const float BOMBER_EXPLODE_RADIUS = 80f;

    // Healer-specific
    private int _healCd = 0;
    private const float HEALER_HEAL_RANGE = 150f;
    private const float HEALER_KEEP_DISTANCE = 120f;

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

    public bool Update(Player p1, Player? p2, Level level, ParticleSystem particles, Camera? camera = null, List<Bullet>? enemyBullets = null)
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

        if (_healCd > 0) _healCd--;

        // Bomber behavior
        if (Type == "bomber")
        {
            UpdateBomber(tgt, d, level, particles, camera);
            return Hp > 0;
        }

        // Healer behavior
        if (Type == "healer")
        {
            UpdateHealer(tgt, d, level, particles, enemyBullets);
            return Hp > 0;
        }

        if (Fly) UpdateFly(tgt, d);
        else UpdateGround(tgt, d, level);

        // Shooter enemy: ranged attack
        if (Type == "shooter" && enemyBullets != null && _aggro && _attackCd <= 0 && d < 300 && d > 50)
        {
            float dx = tgt.X - X;
            float dy = tgt.Y - Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > 0)
            {
                float bulletSpeed = 6f;
                float spread = 0.1f;
                float angle = MathF.Atan2(dy, dx) + (float)(_rng.NextDouble() * spread * 2 - spread);
                float bvx = MathF.Cos(angle) * bulletSpeed;
                float bvy = MathF.Sin(angle) * bulletSpeed;
                
                var weapon = new WeaponDef
                {
                    Name = "shooter",
                    Id = "shooter",
                    Damage = Damage,
                    FireRate = 60,
                    BulletSpeed = bulletSpeed,
                    Spread = spread,
                    Ammo = 999,
                    MaxAmmo = 999,
                    Reserve = 999,
                    ReloadTime = 0,
                    Pellets = 1,
                    Explosive = false,
                    Pierce = false,
                    Color = new Color(255, 153, 51),
                    BulletW = 4,
                    BulletH = 4
                };
                
                enemyBullets.Add(new Bullet(X, Y - 5, bvx, bvy, weapon, "enemy"));
                _attackCd = 90; // Shoot every 1.5 seconds
                Facing = dx > 0 ? 1 : -1;
            }
        }
        // Melee attack for other enemies
        else if (d < (W + tgt.W) / 2f + 4 && _attackCd <= 0)
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

    private void UpdateBomber(Player tgt, float d, Level level, ParticleSystem particles, Camera? camera)
    {
        // Bomber: 冲向玩家，接近后自爆
        float dx = tgt.X - X;
        float dy = tgt.Y - Y;
        
        // 加速冲向玩家
        if (_aggro && d > 5)
        {
            float accel = 0.4f; // 比一般敌人更快的加速
            Vx = Physics.Accelerate(Vx, Speed * 1.2f, accel, dx > 0 ? 1 : -1);
            Facing = dx > 0 ? 1 : -1;
            
            // 跳跃接近玩家
            if (Grounded && tgt.Y < Y - 30 && d < 150 && _rng.NextDouble() < 0.03)
            {
                Vy = -8;
            }
        }
        else
        {
            // 巡逻
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
        
        // 应用重力和移动
        Vy = Physics.ApplyGravity(Vy);
        X += Vx; Y += Vy;
        Grounded = false;
        (X, Y, Vx, Vy, Grounded) = Physics.ResolveCollision(X, Y, W, H, Vx, Vy, level);
        
        // 检测是否接近玩家，准备自爆
        if (d < BOMBER_EXPLODE_RADIUS * 0.8f && _aggro)
        {
            if (!_bomberArming)
            {
                _bomberArming = true;
                _bomberFuse = BOMBER_FUSE_MAX;
                particles.SpawnText(X, Y - H, "!", new Color(255, 50, 50), 20);
            }
        }
        
        // 自爆倒计时
        if (_bomberArming)
        {
            _bomberFuse--;
            
            // 闪烁效果
            if (_bomberFuse % 6 < 3)
                Flash = 2;
            
            // 产生警告粒子
            if (_bomberFuse % 10 == 0)
            {
                particles.SpawnBurst(X, Y - H / 2f, 3, 2, new Color(255, 100, 0), (10, 20), (2, 4), (-3, 0));
            }
            
            // 爆炸！
            if (_bomberFuse <= 0)
            {
                // 对玩家造成伤害
                if (d < BOMBER_EXPLODE_RADIUS)
                {
                    tgt.TakeDamage(Damage, particles, amt => camera?.AddShake(amt));
                }
                
                // 产生爆炸效果
                particles.SpawnBurst(X, Y, 30, 8, new Color(255, 100, 0), (20, 40), (3, 8), (-8, 2));
                particles.SpawnBurst(X, Y, 20, 6, new Color(255, 200, 0), (15, 30), (2, 5), (-6, 1));
                
                // 自爆死亡
                Hp = 0;
                AudioManager.Explode();
            }
        }
    }
    
    private void UpdateHealer(Player tgt, float d, Level level, ParticleSystem particles, List<Bullet>? enemyBullets)
    {
        // Healer: 保持距离，治疗其他敌人
        float dx = tgt.X - X;
        
        // 保持距离：如果玩家太近就后退
        if (d < HEALER_KEEP_DISTANCE)
        {
            float retreatDir = dx > 0 ? -1 : 1;
            Vx = Physics.Accelerate(Vx, Speed * 0.8f, 0.25f, retreatDir);
            Facing = dx > 0 ? -1 : 1; // 面向玩家但后退
        }
        else if (d > HEALER_KEEP_DISTANCE + 50)
        {
            // 如果太远就靠近一点
            float accel = 0.2f;
            Vx = Physics.Accelerate(Vx, Speed * 0.6f, accel, dx > 0 ? 1 : -1);
            Facing = dx > 0 ? 1 : -1;
        }
        else
        {
            // 在合适距离内微调
            Vx *= 0.8f; // 减速
            Facing = dx > 0 ? 1 : -1;
        }
        
        // 应用重力和移动
        Vy = Physics.ApplyGravity(Vy);
        X += Vx; Y += Vy;
        Grounded = false;
        (X, Y, Vx, Vy, Grounded) = Physics.ResolveCollision(X, Y, W, H, Vx, Vy, level);
        
        // 治疗附近的敌人（通过发射治疗子弹）
        if (_healCd <= 0 && enemyBullets != null)
        {
            // 查找附近受伤的敌人
            // 注意：这里我们需要访问游戏主循环中的敌人列表
            // 由于架构限制，我们通过发射特殊子弹来实现治疗效果
            // 在GameMain中处理这种子弹时会检查是否是治疗子弹
            
            // 发射治疗子弹（绿色）
            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
            float bulletSpeed = 4f;
            float bvx = MathF.Cos(angle) * bulletSpeed;
            float bvy = MathF.Sin(angle) * bulletSpeed;
            
            var healWeapon = new WeaponDef
            {
                Name = "heal",
                Id = "heal",
                Damage = -15, // 负伤害表示治疗
                FireRate = 120,
                BulletSpeed = bulletSpeed,
                Spread = 0,
                Ammo = 999,
                MaxAmmo = 999,
                Reserve = 999,
                ReloadTime = 0,
                Pellets = 1,
                Explosive = false,
                Pierce = false,
                Color = new Color(100, 255, 100),
                BulletW = 6,
                BulletH = 6
            };
            
            enemyBullets.Add(new Bullet(X, Y - 5, bvx, bvy, healWeapon, "heal"));
            _healCd = 180; // 3秒冷却
            
            // 治疗特效
            particles.SpawnBurst(X, Y - H / 2f, 8, 3, new Color(100, 255, 100), (15, 25), (2, 4), (-4, 0));
            particles.SpawnText(X, Y - H, "+HEAL", new Color(100, 255, 100), 14);
        }
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
            
            // 闪避行为：检测附近是否有子弹，如果有则闪避
            bool dodging = false;
            if (Type != "heavy" && _rng.NextDouble() < 0.01) // 1%概率尝试闪避
            {
                // 向侧面闪避
                float dodgeDir = _rng.NextDouble() < 0.5 ? -1 : 1;
                Vx = Physics.Accelerate(Vx, Speed * 1.5f, 0.5f, dodgeDir);
                dodging = true;
                
                // 有时跳跃闪避
                if (Grounded && _rng.NextDouble() < 0.3)
                {
                    Vy = -7;
                }
            }
            
            if (!dodging)
            {
                // 包围行为：多个敌人会从不同方向接近
                float surroundOffset = 0;
                if (Type == "soldier" || Type == "elite")
                {
                    // 根据敌人ID计算包围位置偏移
                    surroundOffset = (X % 100) - 50; // 使用位置作为伪随机
                }
                
                float targetX = tgt.X + surroundOffset;
                float targetDx = targetX - X;
                
                Vx = Physics.Accelerate(Vx, Speed, accel, targetDx > 0 ? 1 : -1);
                Facing = targetDx > 0 ? 1 : -1;
            }
            
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
        
        // 显示伤害数字
        particles.SpawnText(X, Y - H / 2f - 10, $"{(int)dmg}", new Color(255, 220, 100), 14);
        
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
            // Boss - large, imposing armored warlord
            int legOff = 0;
            if (Grounded && Math.Abs(Vx) > 0.2f)
                legOff = (int)(Math.Sin(Anim * 0.1f) * 4);
            else if (!Grounded) legOff = -3;

            // Shadow
            sb.Draw(pixel, new Rectangle(sx - 20, sy + hh - 2, 40, 6), new Color(0, 0, 0, 70));

            // === LEGS (thick armored) ===
            sb.Draw(pixel, new Rectangle(sx - 12 + legOff, sy + 4, 10, 12), new Color(60, 60, 60));
            sb.Draw(pixel, new Rectangle(sx + 2 - legOff, sy + 4, 10, 12), new Color(60, 60, 60));
            // Knee plates
            sb.Draw(pixel, new Rectangle(sx - 11 + legOff, sy + 8, 8, 4), new Color(80, 80, 80));
            sb.Draw(pixel, new Rectangle(sx + 3 - legOff, sy + 8, 8, 4), new Color(80, 80, 80));
            // Boots
            sb.Draw(pixel, new Rectangle(sx - 13 + legOff, sy + 14, 12, 5), new Color(50, 40, 30));
            sb.Draw(pixel, new Rectangle(sx + 1 - legOff, sy + 14, 12, 5), new Color(50, 40, 30));
            sb.Draw(pixel, new Rectangle(sx - 13 + legOff, sy + 18, 13, 2), new Color(30, 25, 20));
            sb.Draw(pixel, new Rectangle(sx + 1 - legOff, sy + 18, 13, 2), new Color(30, 25, 20));

            // === BODY (massive armored torso) ===
            sb.Draw(pixel, new Rectangle(sx - hw, sy - hh, W, H), color);
            // Armor plates with highlights
            var armorLight = new Color(Math.Min(255, color.R + 30), Math.Min(255, color.G + 30), Math.Min(255, color.B + 30));
            var armorDark = new Color(Math.Max(0, color.R - 40), Math.Max(0, color.G - 40), Math.Max(0, color.B - 40));
            // Top armor plate
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy - hh + 3, W - 6, 5), armorLight);
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy - hh + 8, W - 6, 2), armorDark);
            // Middle armor plate
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy - 4, W - 6, 5), armorLight);
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy + 1, W - 6, 2), armorDark);
            // Bottom armor plate
            sb.Draw(pixel, new Rectangle(sx - hw + 3, sy + hh - 8, W - 6, 5), armorLight);
            // Belt
            sb.Draw(pixel, new Rectangle(sx - hw, sy + 6, W, 4), new Color(80, 60, 30));
            sb.Draw(pixel, new Rectangle(sx - 4, sy + 6, 8, 4), new Color(220, 180, 50)); // gold buckle

            // === SHOULDER PADS (large, intimidating) ===
            sb.Draw(pixel, new Rectangle(sx - hw - 8, sy - hh + 2, 10, 14), new Color(90, 90, 90));
            sb.Draw(pixel, new Rectangle(sx - hw - 6, sy - hh + 4, 8, 10), new Color(110, 110, 110));
            sb.Draw(pixel, new Rectangle(sx + hw - 2, sy - hh + 2, 10, 14), new Color(90, 90, 90));
            sb.Draw(pixel, new Rectangle(sx + hw, sy - hh + 4, 8, 10), new Color(110, 110, 110));
            // Spikes on shoulders
            sb.Draw(pixel, new Rectangle(sx - hw - 10, sy - hh, 4, 6), new Color(150, 150, 150));
            sb.Draw(pixel, new Rectangle(sx + hw + 6, sy - hh, 4, 6), new Color(150, 150, 150));

            // === HEAD (helmeted) ===
            // Helmet base
            sb.Draw(pixel, new Rectangle(sx - 10, sy - hh - 14, 20, 16), new Color(70, 70, 70));
            // Helmet top ridge
            sb.Draw(pixel, new Rectangle(sx - 3, sy - hh - 18, 6, 6), new Color(90, 90, 90));
            // Visor slit
            var visorColor = Flash > 0 ? Color.White : new Color(255, 50, 50);
            sb.Draw(pixel, new Rectangle(sx - 8, sy - hh - 8, 16, 4), visorColor);
            // Visor glow
            if (Flash <= 0)
                sb.Draw(pixel, new Rectangle(sx - 9, sy - hh - 9, 18, 6), new Color(255, 50, 50, 40));
            // Jaw guard
            sb.Draw(pixel, new Rectangle(sx - 8, sy - hh - 4, 16, 4), new Color(60, 60, 60));

            // === WEAPON (large cannon) ===
            sb.Draw(pixel, new Rectangle(sx + f * hw, sy - 8, f * 14, 6), new Color(60, 60, 60));
            sb.Draw(pixel, new Rectangle(sx + f * (hw + 12), sy - 10, f * 4, 10), new Color(80, 80, 80));
            // Cannon glow
            sb.Draw(pixel, new Rectangle(sx + f * (hw + 14), sy - 6, f * 2, 4), new Color(255, 100, 50));

            // === CAPE (flowing behind) ===
            float capeWave = (float)Math.Sin(Anim * 0.08f) * 3;
            var capeColor = new Color(120, 20, 20);
            sb.Draw(pixel, new Rectangle(sx - hw - 2 + (int)capeWave, sy - hh + 10, 4, H - 10), capeColor);
            sb.Draw(pixel, new Rectangle(sx + hw - 2 - (int)capeWave, sy - hh + 10, 4, H - 10), capeColor);
        }
        else if (Type == "bomber")
        {
            // Bomber: 橙色身体，带有炸弹特征
            int legOff = 0;
            if (Grounded && Math.Abs(Vx) > 0.2f)
                legOff = (int)(Math.Sin(Anim * 0.2f * Speed * 2) * 5);
            else if (!Grounded) legOff = -3;

            // Legs
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 6, 6, 9), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 6, 6, 9), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 14, 7, 4), new Color(30, 30, 30));
            sb.Draw(pixel, new Rectangle(sx - 1 - legOff, sy + 14, 7, 4), new Color(30, 30, 30));

            // Body (orange)
            int bodyH = H - 12;
            sb.Draw(pixel, new Rectangle(sx - hw, sy - bodyH + 3, W, bodyH), color);
            
            // Bomb on back (circular)
            int bombRadius = 8;
            sb.Draw(pixel, new Rectangle(sx - bombRadius, sy - bodyH - bombRadius + 5, bombRadius * 2, bombRadius * 2), 
                new Color(60, 60, 60));
            sb.Draw(pixel, new Rectangle(sx - bombRadius + 2, sy - bodyH - bombRadius + 7, bombRadius * 2 - 4, bombRadius * 2 - 4), 
                new Color(80, 80, 80));
            
            // Fuse (if arming)
            if (_bomberArming)
            {
                int fuseLen = 6;
                sb.Draw(pixel, new Rectangle(sx - 1, sy - bodyH - bombRadius - fuseLen + 5, 2, fuseLen), 
                    new Color(139, 69, 19));
                
                // Spark at fuse tip
                if (_bomberFuse % 8 < 4)
                {
                    sb.Draw(pixel, new Rectangle(sx - 2, sy - bodyH - bombRadius - fuseLen + 3, 4, 4), 
                        new Color(255, 255, 0));
                }
            }
            
            // Head
            int headY = sy - bodyH - 3;
            var skinC = new Color(220, 170, 140);
            sb.Draw(pixel, new Rectangle(sx - 5, headY, 10, 10), skinC);
            // Bandana
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 2, 12, 4), new Color(200, 50, 50));
            sb.Draw(pixel, new Rectangle(sx - 6, headY + 2, 4, 4), new Color(200, 50, 50));
            // Eye (angry)
            sb.Draw(pixel, new Rectangle(sx + f * 2, headY + 3, 4, 3), Flash > 0 ? Color.White : Color.Yellow);
            
            // Arms holding bomb
            sb.Draw(pixel, new Rectangle(sx + f * 8, sy - 8, f * 6, 4), new Color(70, 70, 70));
            
            // Warning indicator when arming
            if (_bomberArming && Flash <= 0)
            {
                var warnColor = _bomberFuse % 12 < 6 ? new Color(255, 0, 0) : new Color(255, 100, 0);
                sb.Draw(pixel, new Rectangle(sx - 2, headY - 20, 4, 12), warnColor);
                sb.Draw(pixel, new Rectangle(sx - 2, headY - 8, 4, 4), warnColor);
            }
        }
        else if (Type == "healer")
        {
            // Healer: 绿色身体，带有治疗特征
            int legOff = 0;
            if (Grounded && Math.Abs(Vx) > 0.2f)
                legOff = (int)(Math.Sin(Anim * 0.15f * Speed * 2) * 4);
            else if (!Grounded) legOff = -2;

            // Legs
            sb.Draw(pixel, new Rectangle(sx - 5 + legOff, sy + 6, 5, 8), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 6, 5, 8), new Color(44, 58, 88));
            sb.Draw(pixel, new Rectangle(sx - 5 + legOff, sy + 13, 6, 3), new Color(30, 30, 30));
            sb.Draw(pixel, new Rectangle(sx - 1 - legOff, sy + 13, 6, 3), new Color(30, 30, 30));

            // Body (green)
            int bodyH = H - 12;
            sb.Draw(pixel, new Rectangle(sx - hw, sy - bodyH + 3, W, bodyH), color);
            
            // Medical cross on chest
            int crossSize = 6;
            sb.Draw(pixel, new Rectangle(sx - crossSize / 2, sy - bodyH + 8, crossSize, 2), Color.White);
            sb.Draw(pixel, new Rectangle(sx - 1, sy - bodyH + 5, 2, crossSize + 2), Color.White);
            
            // Healing aura (pulsing)
            float auraPulse = (float)Math.Sin(Anim * 0.1f) * 0.3f + 0.7f;
            var auraColor = new Color(100, 255, 100, (int)(auraPulse * 80));
            sb.Draw(pixel, new Rectangle(sx - hw - 3, sy - bodyH, W + 6, bodyH + 12), auraColor);
            
            // Head
            int headY = sy - bodyH - 3;
            var skinC = new Color(220, 170, 140);
            sb.Draw(pixel, new Rectangle(sx - 5, headY, 10, 10), skinC);
            // Hood/hat
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 3, 12, 5), new Color(80, 200, 80));
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 1, 4, 5), new Color(80, 200, 80));
            sb.Draw(pixel, new Rectangle(sx + 2, headY - 1, 4, 5), new Color(80, 200, 80));
            // Eye (calm)
            sb.Draw(pixel, new Rectangle(sx + f * 2, headY + 3, 4, 3), Flash > 0 ? Color.White : new Color(100, 255, 100));
            
            // Staff/wand
            sb.Draw(pixel, new Rectangle(sx + f * 10, sy - 12, f * 2, 16), new Color(139, 69, 19));
            // Staff orb
            sb.Draw(pixel, new Rectangle(sx + f * 10 - 2, sy - 14, 6, 6), new Color(100, 255, 100));
            
            // Healing indicator
            if (_healCd <= 0 && Flash <= 0)
            {
                var healColor = new Color(100, 255, 100, 150);
                sb.Draw(pixel, new Rectangle(sx - 1, headY - 15, 3, 8), healColor);
                sb.Draw(pixel, new Rectangle(sx - 3, headY - 12, 7, 3), healColor);
            }
        }
        else if (Fly)
        {
            // Flying enemy with detailed wings and body
            int wingOffset = (int)(Math.Sin(Anim * 0.3) * 6);
            var bodyColor = Flash > 0 ? Color.White : color;
            var wingColor = Flash > 0 ? Color.White : new Color(170, 136, 255);
            var wingDark = new Color(Math.Max(0, wingColor.R - 40), Math.Max(0, wingColor.G - 40), Math.Max(0, wingColor.B - 40));

            // Shadow on ground (smaller, further away)
            sb.Draw(pixel, new Rectangle(sx - 6, sy + 20, 12, 2), new Color(0, 0, 0, 30));

            // === WINGS (animated) ===
            // Left wing - upper part
            sb.Draw(pixel, new Rectangle(sx - hw - 8, sy - 4 + wingOffset, 8, 3), wingColor);
            sb.Draw(pixel, new Rectangle(sx - hw - 10, sy - 2 + wingOffset, 4, 5), wingColor);
            // Left wing - lower part (darker)
            sb.Draw(pixel, new Rectangle(sx - hw - 6, sy - 1 + wingOffset, 6, 2), wingDark);
            
            // Right wing - upper part
            sb.Draw(pixel, new Rectangle(sx + hw, sy - 4 - wingOffset, 8, 3), wingColor);
            sb.Draw(pixel, new Rectangle(sx + hw + 6, sy - 2 - wingOffset, 4, 5), wingColor);
            // Right wing - lower part (darker)
            sb.Draw(pixel, new Rectangle(sx + hw, sy - 1 - wingOffset, 6, 2), wingDark);

            // === BODY ===
            // Main body (oval shape)
            sb.Draw(pixel, new Rectangle(sx - hw + 2, sy - hh, W - 4, H), bodyColor);
            sb.Draw(pixel, new Rectangle(sx - hw, sy - hh + 2, W, H - 4), bodyColor);
            
            // Body details - chest plate
            var chestDark = new Color(Math.Max(0, bodyColor.R - 30), Math.Max(0, bodyColor.G - 30), Math.Max(0, bodyColor.B - 30));
            sb.Draw(pixel, new Rectangle(sx - 4, sy - 6, 8, 8), chestDark);
            
            // Glowing core in center
            var coreColor = Flash > 0 ? Color.White : new Color(255, 255, 100);
            sb.Draw(pixel, new Rectangle(sx - 2, sy - 4, 4, 4), coreColor);
            sb.Draw(pixel, new Rectangle(sx - 1, sy - 3, 2, 2), Color.White);

            // === HEAD ===
            // Head (smaller, on top)
            sb.Draw(pixel, new Rectangle(sx - 5, sy - hh - 6, 10, 8), bodyColor);
            
            // Eyes (two glowing eyes)
            var eyeColor = Flash > 0 ? Color.White : new Color(255, 200, 50);
            sb.Draw(pixel, new Rectangle(sx - 3 + f, sy - hh - 3, 2, 2), eyeColor);
            sb.Draw(pixel, new Rectangle(sx + 1 + f, sy - hh - 3, 2, 2), eyeColor);
            // Eye glow effect
            if (Flash <= 0)
            {
                sb.Draw(pixel, new Rectangle(sx - 4 + f, sy - hh - 4, 4, 4), new Color(255, 200, 50, 40));
                sb.Draw(pixel, new Rectangle(sx + f, sy - hh - 4, 4, 4), new Color(255, 200, 50, 40));
            }

            // === TAIL/PROPELLER ===
            // Tail fin
            sb.Draw(pixel, new Rectangle(sx - 2, sy + hh - 2, 4, 4), chestDark);
            
            // Propeller effect (spinning blades)
            if ((int)Anim % 3 < 2)
            {
                var propColor = new Color(200, 200, 200, 120);
                sb.Draw(pixel, new Rectangle(sx - 6, sy - hh - 8, 12, 2), propColor);
                sb.Draw(pixel, new Rectangle(sx - 1, sy - hh - 10, 2, 4), propColor);
            }
            else
            {
                var propColor = new Color(200, 200, 200, 80);
                sb.Draw(pixel, new Rectangle(sx - 8, sy - hh - 7, 16, 1), propColor);
            }
        }
        else
        {
            // Ground enemy with detailed pixel art
            int legOff = 0;
            if (Grounded && Math.Abs(Vx) > 0.2f)
                legOff = (int)(Math.Sin(Anim * 0.15f * Speed * 2) * 5);
            else if (!Grounded) legOff = -3;

            var skinC = SkinColor();
            var helmetC = Type == "elite" ? new Color(80, 80, 80) : HairColor();
            int headY = sy - (H - 12) - 3;
            int bodyH = H - 12;

            // Shadow on ground
            sb.Draw(pixel, new Rectangle(sx - 8, sy + 16, 16, 3), new Color(0, 0, 0, 50));

            // === LEGS ===
            // Upper legs (thighs)
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 4, 6, 5), new Color(50, 60, 90));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 4, 6, 5), new Color(50, 60, 90));
            // Lower legs (shins)
            sb.Draw(pixel, new Rectangle(sx - 6 + legOff, sy + 9, 6, 5), new Color(44, 54, 82));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 9, 6, 5), new Color(44, 54, 82));
            // Boots
            sb.Draw(pixel, new Rectangle(sx - 7 + legOff, sy + 13, 7, 4), new Color(40, 30, 20));
            sb.Draw(pixel, new Rectangle(sx + legOff, sy + 13, 7, 4), new Color(40, 30, 20));
            // Boot soles
            sb.Draw(pixel, new Rectangle(sx - 7 + legOff, sy + 16, 8, 2), new Color(25, 20, 15));
            sb.Draw(pixel, new Rectangle(sx - 1 - legOff, sy + 16, 8, 2), new Color(25, 20, 15));

            // === BODY ===
            // Torso base
            sb.Draw(pixel, new Rectangle(sx - hw, sy - bodyH + 3, W, bodyH), color);
            // Chest plate / vest
            var vestDark = new Color(Math.Max(0, color.R - 40), Math.Max(0, color.G - 40), Math.Max(0, color.B - 40));
            sb.Draw(pixel, new Rectangle(sx - hw + 1, sy - bodyH + 4, W - 2, 3), vestDark);
            sb.Draw(pixel, new Rectangle(sx - hw + 1, sy - bodyH + 9, W - 2, 2), vestDark);
            // Belt with buckle
            sb.Draw(pixel, new Rectangle(sx - hw, sy + 3, W, 3), new Color(60, 40, 20));
            if (Type == "elite")
                sb.Draw(pixel, new Rectangle(sx - 2, sy + 3, 4, 3), new Color(210, 180, 60)); // gold buckle

            // Shoulder pads
            var shoulderC = new Color(Math.Max(0, color.R - 50), Math.Max(0, color.G - 50), Math.Max(0, color.B - 50));
            sb.Draw(pixel, new Rectangle(sx - hw - 3, sy - bodyH + 3, 5, 6), shoulderC);
            sb.Draw(pixel, new Rectangle(sx + hw - 2, sy - bodyH + 3, 5, 6), shoulderC);

            // Type-specific body details
            if (Type == "heavy")
            {
                // Extra armor plates
                sb.Draw(pixel, new Rectangle(sx - hw - 1, sy - bodyH + 6, W + 2, bodyH - 4), new Color(180, 50, 40, 80));
                // Chest stripes
                sb.Draw(pixel, new Rectangle(sx - 3, sy - bodyH + 5, 6, 2), new Color(200, 200, 50));
                sb.Draw(pixel, new Rectangle(sx - 3, sy - bodyH + 9, 6, 2), new Color(200, 200, 50));
            }
            else if (Type == "elite")
            {
                // Elite star emblem
                sb.Draw(pixel, new Rectangle(sx - 1, sy - bodyH + 6, 3, 3), new Color(255, 215, 0));
            }
            else if (Type == "shooter")
            {
                // Ammo belt across chest
                sb.Draw(pixel, new Rectangle(sx - hw + 2, sy - bodyH + 5, W - 4, 2), new Color(180, 150, 50));
                for (int i = 0; i < 3; i++)
                    sb.Draw(pixel, new Rectangle(sx - hw + 3 + i * 5, sy - bodyH + 5, 2, 2), new Color(200, 170, 60));
            }

            // === HEAD ===
            // Face
            sb.Draw(pixel, new Rectangle(sx - 5, headY, 10, 10), skinC);
            // Jaw shadow
            sb.Draw(pixel, new Rectangle(sx - 4, headY + 8, 8, 2), new Color(skinC.R - 20, skinC.G - 20, skinC.B - 20));
            // Helmet/hair
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 4, 12, 5), helmetC);
            sb.Draw(pixel, new Rectangle(sx - 6, headY - 1, 4, 6), helmetC);
            sb.Draw(pixel, new Rectangle(sx + 2, headY - 1, 4, 6), helmetC);
            // Helmet visor for elite
            if (Type == "elite")
                sb.Draw(pixel, new Rectangle(sx - 5, headY + 1, 10, 2), new Color(60, 60, 60));

            // Eyes (two eyes with highlights)
            var eyeC = Flash > 0 ? Color.White : Color.Yellow;
            sb.Draw(pixel, new Rectangle(sx - 3 + f, headY + 3, 3, 3), eyeC);
            sb.Draw(pixel, new Rectangle(sx + 1 + f, headY + 3, 3, 3), eyeC);
            // Pupils
            if (Flash <= 0)
            {
                sb.Draw(pixel, new Rectangle(sx - 2 + f, headY + 4, 1, 1), new Color(20, 20, 20));
                sb.Draw(pixel, new Rectangle(sx + 2 + f, headY + 4, 1, 1), new Color(20, 20, 20));
            }
            // Mouth
            sb.Draw(pixel, new Rectangle(sx - 2 + f, headY + 7, 4, 1), new Color(160, 100, 70));

            // === ARM + WEAPON ===
            // Arm
            sb.Draw(pixel, new Rectangle(sx + f * 8, sy - bodyH + 5, 4, 10), skinC);
            // Glove
            sb.Draw(pixel, new Rectangle(sx + f * 8, sy - bodyH + 14, 4, 3), new Color(45, 45, 45));
            // Weapon
            var weaponColor = Type == "shooter" ? new Color(90, 90, 90) : new Color(70, 70, 70);
            sb.Draw(pixel, new Rectangle(sx + f * 10, sy - bodyH + 8, f * 8, 3), weaponColor);
            if (Type == "shooter")
            {
                // Longer barrel for shooter
                sb.Draw(pixel, new Rectangle(sx + f * 16, sy - bodyH + 7, f * 6, 2), new Color(60, 60, 60));
                // Scope
                sb.Draw(pixel, new Rectangle(sx + f * 14, sy - bodyH + 5, 3, 2), new Color(100, 100, 100));
            }

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