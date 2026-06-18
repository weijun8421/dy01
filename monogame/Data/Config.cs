using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DY01.Data;

public static class Config
{
    // Window
    public const int W = 960;
    public const int H = 640;
    public const int TILE = 20;
    public const int FPS = 60;

    // Physics
    public const float GRAVITY = 0.55f;
    public const float MAX_FALL = 11.0f;
    public const float FRICTION = 0.82f;
    public const float ACCEL = 0.35f;
    public const float JUMP_VEL = -10.5f;
    public const float SHORT_JUMP = -6.0f;
    public const int COYOTE_FRAMES = 6;
    public const int JUMP_BUFFER = 6;

    // Weapons
    public static readonly WeaponDef[] WEAPONS = new WeaponDef[]
    {
        new WeaponDef { Name = "突击步枪", Icon = "AR", Id = "rifle", Damage = 14, FireRate = 7, BulletSpeed = 12,
            Spread = 0.04f, Ammo = 35, MaxAmmo = 35, Reserve = 140, ReloadTime = 50,
            Pellets = 1, Explosive = false, Pierce = false, Color = new Color(255, 221, 68),
            BulletW = 4, BulletH = 3 },
        new WeaponDef { Name = "霰弹枪", Icon = "SG", Id = "shotgun", Damage = 7, FireRate = 32, BulletSpeed = 9,
            Spread = 0.22f, Ammo = 8, MaxAmmo = 8, Reserve = 48, ReloadTime = 70,
            Pellets = 6, Explosive = false, Pierce = false, Color = new Color(255, 136, 51),
            BulletW = 5, BulletH = 5 },
        new WeaponDef { Name = "火焰枪", Icon = "FL", Id = "flame", Damage = 4, FireRate = 2, BulletSpeed = 7,
            Spread = 0.12f, Ammo = 120, MaxAmmo = 120, Reserve = 360, ReloadTime = 90,
            Pellets = 1, Explosive = false, Pierce = true, Color = new Color(255, 68, 34),
            BulletW = 6, BulletH = 6 },
        new WeaponDef { Name = "激光枪", Icon = "LA", Id = "laser", Damage = 22, FireRate = 10, BulletSpeed = 20,
            Spread = 0.01f, Ammo = 25, MaxAmmo = 25, Reserve = 100, ReloadTime = 55,
            Pellets = 1, Explosive = false, Pierce = true, Color = new Color(68, 221, 255),
            BulletW = 3, BulletH = 10 },
        new WeaponDef { Name = "火箭筒", Icon = "RK", Id = "rocket", Damage = 55, FireRate = 55, BulletSpeed = 5.5f,
            Spread = 0.02f, Ammo = 4, MaxAmmo = 4, Reserve = 16, ReloadTime = 100,
            Pellets = 1, Explosive = true, ExplosionRadius = 70, Pierce = false,
            Color = new Color(255, 34, 34), BulletW = 8, BulletH = 8 },
    };

    // Buffs
    public static readonly BuffDef[] BUFFS = new BuffDef[]
    {
        new BuffDef { Id = "dmg", Name = "力量强化", Desc = "所有武器伤害+20%", Tier = 0, Weapon = null, Icon = "!" },
        new BuffDef { Id = "spd", Name = "敏捷步伐", Desc = "移动速度+15%", Tier = 0, Weapon = null, Icon = ">" },
        new BuffDef { Id = "hp", Name = "生命扩容", Desc = "生命上限+25%", Tier = 0, Weapon = null, Icon = "+" },
        new BuffDef { Id = "regen", Name = "缓慢恢复", Desc = "每秒恢复1HP", Tier = 0, Weapon = null, Icon = "R" },
        new BuffDef { Id = "ammo_bag", Name = "弹药袋", Desc = "所有武器备弹+50%", Tier = 0, Weapon = null, Icon = "A" },
        new BuffDef { Id = "fir", Name = "急速射击", Desc = "射速+25%", Tier = 1, Weapon = null, Icon = "F" },
        new BuffDef { Id = "vmp", Name = "生命偷取", Desc = "击杀恢复5HP", Tier = 1, Weapon = null, Icon = "V" },
        new BuffDef { Id = "grenade", Name = "手雷精通", Desc = "爆炸范围+40%", Tier = 1, Weapon = "rocket", Icon = "O" },
        new BuffDef { Id = "rifle_dmg", Name = "穿甲步枪弹", Desc = "步枪伤害+35%", Tier = 1, Weapon = "rifle", Icon = "R" },
        new BuffDef { Id = "sg_wide", Name = "散射强化", Desc = "霰弹枪弹丸+3", Tier = 1, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "flame_burn", Name = "持续灼烧", Desc = "火焰枪附加燃烧伤害", Tier = 1, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "laser_beam", Name = "聚焦光束", Desc = "激光枪宽度+50%", Tier = 1, Weapon = "laser", Icon = "L" },
        new BuffDef { Id = "prc", Name = "穿甲弹", Desc = "所有子弹穿透敌人", Tier = 2, Weapon = null, Icon = "#" },
        new BuffDef { Id = "dbl", Name = "双发", Desc = "每次射击两发子弹", Tier = 2, Weapon = null, Icon = "2" },
        new BuffDef { Id = "crit", Name = "致命一击", Desc = "15%概率3倍伤害", Tier = 2, Weapon = null, Icon = "C" },
        new BuffDef { Id = "rifle_burst", Name = "三连发", Desc = "步枪改为三连发", Tier = 2, Weapon = "rifle", Icon = "3" },
        new BuffDef { Id = "sg_slug", Name = "独头弹", Desc = "霰弹枪单发高伤", Tier = 2, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "flame_wave", Name = "火焰波", Desc = "火焰枪范围+60%", Tier = 2, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "laser_chain", Name = "连锁闪电", Desc = "激光命中后弹射附近敌人", Tier = 2, Weapon = "laser", Icon = "L" },
        new BuffDef { Id = "rocket_cluster", Name = "集束弹头", Desc = "火箭弹分裂为3枚", Tier = 2, Weapon = "rocket", Icon = "R" },
        new BuffDef { Id = "bsk", Name = "狂战士之怒", Desc = "低血时伤害翻倍", Tier = 3, Weapon = null, Icon = "B" },
        new BuffDef { Id = "shield", Name = "能量护盾", Desc = "每30秒抵挡一次伤害", Tier = 3, Weapon = null, Icon = "S" },
        new BuffDef { Id = "reload_all", Name = "快速装填", Desc = "换弹速度+60%", Tier = 3, Weapon = null, Icon = "R" },
        new BuffDef { Id = "rifle_turret", Name = "自动哨戒", Desc = "步枪自动索敌射击", Tier = 3, Weapon = "rifle", Icon = "T" },
        new BuffDef { Id = "sg_stun", Name = "震撼弹", Desc = "霰弹枪命中眩晕敌人", Tier = 3, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "flame_dragon", Name = "炎龙", Desc = "火焰枪射出龙形火焰", Tier = 3, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "laser_overload", Name = "超载激光", Desc = "激光枪伤害+80%", Tier = 3, Weapon = "laser", Icon = "L" },
        new BuffDef { Id = "rocket_nuke", Name = "微型核弹", Desc = "火箭弹爆炸范围+100%", Tier = 3, Weapon = "rocket", Icon = "R" },
        new BuffDef { Id = "god", Name = "天神下凡", Desc = "伤害+80% 移速+30%", Tier = 4, Weapon = null, Icon = "G" },
        new BuffDef { Id = "inf_ammo", Name = "无限弹药", Desc = "不再消耗弹药", Tier = 4, Weapon = null, Icon = "I" },
        new BuffDef { Id = "death_blow", Name = "死亡之触", Desc = "20%概率一击必杀", Tier = 4, Weapon = null, Icon = "D" },
        new BuffDef { Id = "bullet_hell", Name = "弹幕风暴", Desc = "所有子弹数量+5", Tier = 4, Weapon = null, Icon = "B" },
    };

    public static readonly Color[] TIER_COLORS = new Color[]
    {
        new Color(200, 200, 200),
        new Color(68, 136, 255),
        new Color(170, 68, 255),
        new Color(255, 170, 0),
        new Color(255, 34, 34),
    };

    public static readonly string[] TIER_NAMES = { "普通", "稀有", "史诗", "传说", "神话" };

    // Enemy types - 平衡性调整
    public static readonly Dictionary<string, EnemyTypeDef> ENEMY_TYPES = new()
    {
        ["soldier"] = new EnemyTypeDef { W = 18, H = 28, HP = 28, Speed = 1.2f, Damage = 8, Color = new Color(204, 51, 51), Score = 100 },
        ["elite"] = new EnemyTypeDef { W = 20, H = 30, HP = 55, Speed = 1.8f, Damage = 13, Color = new Color(170, 34, 34), Score = 250 },
        ["heavy"] = new EnemyTypeDef { W = 30, H = 32, HP = 130, Speed = 0.6f, Damage = 22, Color = new Color(136, 34, 34), Score = 500 },
        ["flyer"] = new EnemyTypeDef { W = 18, H = 22, HP = 22, Speed = 2.2f, Damage = 10, Color = new Color(136, 68, 204), Score = 300, Fly = true },
        ["boss"] = new EnemyTypeDef { W = 55, H = 55, HP = 900, Speed = 0.7f, Damage = 30, Color = new Color(102, 0, 0), Score = 5000 },
    };

    // Colors
    public static readonly Color COLOR_BG = new Color(10, 10, 10);
    public static readonly Color COLOR_SKY = new Color(26, 26, 46);
    public static readonly Color COLOR_MENU_BG = new Color(8, 8, 20);
    public static readonly Color COLOR_GROUND = new Color(58, 92, 62);
    public static readonly Color COLOR_GROUND_TOP = new Color(74, 124, 89);
    public static readonly Color COLOR_PLAT = new Color(107, 68, 35);
    public static readonly Color COLOR_PLAT_LIGHT = new Color(139, 101, 51);
    public static readonly Color COLOR_WALL = new Color(85, 85, 85);
    public static readonly Color COLOR_WALL_LIGHT = new Color(119, 119, 119);
    public static readonly Color COLOR_BARREL = new Color(204, 51, 51);
    public static readonly Color COLOR_EXIT = new Color(0, 255, 0);
    public static readonly Color COLOR_HP = new Color(255, 51, 51);
    public static readonly Color COLOR_HP_BG = new Color(34, 34, 34);
    public static readonly Color COLOR_HUD_BG = new Color(0, 0, 0, 170);
    public static readonly Color COLOR_HUD_BORDER = new Color(85, 85, 85);
    public static readonly Color COLOR_GOLD = new Color(255, 170, 0);
    public static readonly Color COLOR_WHITE = new Color(255, 255, 255);
    public static readonly Color COLOR_GRAY = new Color(136, 136, 136);
    public static readonly Color COLOR_DARK_GRAY = new Color(85, 85, 85);
    public static readonly Color COLOR_P1 = new Color(204, 51, 51);
    public static readonly Color COLOR_P2 = new Color(51, 136, 238);
    public static readonly Color COLOR_SKIN = new Color(238, 187, 153);
}

public class WeaponDef
{
    public string Name = "";
    public string Icon = "";
    public string Id = "";
    public float Damage;
    public int FireRate;
    public float BulletSpeed;
    public float Spread;
    public int Ammo;
    public int MaxAmmo;
    public int Reserve;
    public int ReloadTime;
    public int Pellets;
    public bool Explosive;
    public float ExplosionRadius;
    public bool Pierce;
    public Color Color;
    public int BulletW;
    public int BulletH;

    public WeaponDef Clone()
    {
        return (WeaponDef)MemberwiseClone();
    }
}

public class BuffDef
{
    public string Id = "";
    public string Name = "";
    public string Desc = "";
    public int Tier;
    public string? Weapon;
    public string Icon = "";
}

public class EnemyTypeDef
{
    public int W, H;
    public float HP, Speed, Damage;
    public Color Color;
    public int Score;
    public bool Fly;
}