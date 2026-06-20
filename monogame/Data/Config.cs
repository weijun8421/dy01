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
        // 突击步枪 - 均衡全能，中距离稳定输出
        new WeaponDef { Name = "突击步枪", Icon = "AR", Id = "rifle", Damage = 13, FireRate = 6, BulletSpeed = 12,
            Spread = 0.04f, Ammo = 30, MaxAmmo = 30, Reserve = 150, ReloadTime = 45,
            Pellets = 1, Explosive = false, Pierce = false, Color = new Color(255, 221, 68),
            BulletW = 4, BulletH = 3 },
        // 霰弹枪 - 近程爆发，高风险高回报
        new WeaponDef { Name = "霰弹枪", Icon = "SG", Id = "shotgun", Damage = 9, FireRate = 28, BulletSpeed = 10,
            Spread = 0.18f, Ammo = 8, MaxAmmo = 8, Reserve = 56, ReloadTime = 60,
            Pellets = 7, Explosive = false, Pierce = false, Color = new Color(255, 136, 51),
            BulletW = 5, BulletH = 5 },
        // 火焰枪 - 持续输出，穿透+大范围
        new WeaponDef { Name = "火焰枪", Icon = "FL", Id = "flame", Damage = 5, FireRate = 2, BulletSpeed = 7,
            Spread = 0.15f, Ammo = 150, MaxAmmo = 150, Reserve = 450, ReloadTime = 80,
            Pellets = 1, Explosive = false, Pierce = true, Color = new Color(255, 68, 34),
            BulletW = 6, BulletH = 6 },
        // 激光枪 - 精准高伤，穿透但射速慢
        new WeaponDef { Name = "激光枪", Icon = "LA", Id = "laser", Damage = 18, FireRate = 12, BulletSpeed = 22,
            Spread = 0.01f, Ammo = 20, MaxAmmo = 20, Reserve = 100, ReloadTime = 60,
            Pellets = 1, Explosive = false, Pierce = true, Color = new Color(68, 221, 255),
            BulletW = 3, BulletH = 10 },
        // 火箭筒 - 爆炸范围伤害，群控之王
        new WeaponDef { Name = "火箭筒", Icon = "RK", Id = "rocket", Damage = 70, FireRate = 50, BulletSpeed = 6,
            Spread = 0.02f, Ammo = 5, MaxAmmo = 5, Reserve = 20, ReloadTime = 90,
            Pellets = 1, Explosive = true, ExplosionRadius = 75, Pierce = false,
            Color = new Color(255, 34, 34), BulletW = 8, BulletH = 8 },
    };

    // Buffs
    public static readonly BuffDef[] BUFFS = new BuffDef[]
    {
        // 通用 Buff (所有武器可用)
        new BuffDef { Id = "dmg", Name = "力量强化", Desc = "伤害+20%", Tier = 0, Weapon = null, Icon = "!" },
        new BuffDef { Id = "spd", Name = "敏捷步伐", Desc = "移速+15%", Tier = 0, Weapon = null, Icon = ">" },
        new BuffDef { Id = "hp", Name = "生命扩容", Desc = "生命上限+25%", Tier = 0, Weapon = null, Icon = "+" },
        new BuffDef { Id = "regen", Name = "缓慢恢复", Desc = "每秒恢复1HP", Tier = 0, Weapon = null, Icon = "R" },
        new BuffDef { Id = "ammo_bag", Name = "弹药袋", Desc = "备弹+50%", Tier = 0, Weapon = null, Icon = "A" },
        new BuffDef { Id = "fir", Name = "急速射击", Desc = "射速+25%", Tier = 1, Weapon = null, Icon = "F" },
        new BuffDef { Id = "vmp", Name = "生命偷取", Desc = "击杀恢复5HP", Tier = 1, Weapon = null, Icon = "V" },
        new BuffDef { Id = "prc", Name = "穿甲弹", Desc = "子弹穿透敌人", Tier = 2, Weapon = null, Icon = "#" },
        new BuffDef { Id = "dbl", Name = "双发", Desc = "每次射击两发", Tier = 2, Weapon = null, Icon = "2" },
        new BuffDef { Id = "crit", Name = "致命一击", Desc = "15%概率3倍伤害", Tier = 2, Weapon = null, Icon = "C" },
        new BuffDef { Id = "bsk", Name = "狂战士", Desc = "低血时伤害翻倍", Tier = 3, Weapon = null, Icon = "B" },
        new BuffDef { Id = "shield", Name = "能量护盾", Desc = "每30秒挡一次伤害", Tier = 3, Weapon = null, Icon = "S" },
        new BuffDef { Id = "reload_all", Name = "快速装填", Desc = "换弹速度+60%", Tier = 3, Weapon = null, Icon = "R" },
        new BuffDef { Id = "god", Name = "天神下凡", Desc = "伤害+80% 移速+30%", Tier = 4, Weapon = null, Icon = "G" },
        new BuffDef { Id = "inf_ammo", Name = "无限弹药", Desc = "不再消耗弹药", Tier = 4, Weapon = null, Icon = "I" },
        new BuffDef { Id = "death_blow", Name = "死亡之触", Desc = "20%概率一击必杀", Tier = 4, Weapon = null, Icon = "D" },

        // 突击步枪专属 Buff
        new BuffDef { Id = "rifle_dmg", Name = "穿甲弹头", Desc = "伤害+35%", Tier = 0, Weapon = "rifle", Icon = "R" },
        new BuffDef { Id = "rifle_speed", Name = "快速瞄准", Desc = "射速+30%", Tier = 0, Weapon = "rifle", Icon = ">" },
        new BuffDef { Id = "rifle_burst", Name = "三连发", Desc = "每次射击3发子弹", Tier = 1, Weapon = "rifle", Icon = "3" },
        new BuffDef { Id = "rifle_pierce", Name = "穿透强化", Desc = "子弹穿透+2个敌人", Tier = 1, Weapon = "rifle", Icon = "#" },
        new BuffDef { Id = "rifle_turret", Name = "自动哨戒", Desc = "自动索敌射击", Tier = 2, Weapon = "rifle", Icon = "T" },
        new BuffDef { Id = "rifle_crit", Name = "精准射击", Desc = "暴击率+25%", Tier = 2, Weapon = "rifle", Icon = "C" },
        new BuffDef { Id = "rifle_hypershot", Name = "超频射击", Desc = "射速+60% 伤害+40%", Tier = 3, Weapon = "rifle", Icon = "H" },
        new BuffDef { Id = "rifle_gauss", Name = "高斯步枪", Desc = "子弹速度翻倍 伤害+80%", Tier = 4, Weapon = "rifle", Icon = "G" },

        // 霰弹枪专属 Buff
        new BuffDef { Id = "sg_wide", Name = "散射强化", Desc = "弹丸+3", Tier = 0, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "sg_dmg", Name = "大口径弹", Desc = "伤害+40%", Tier = 0, Weapon = "shotgun", Icon = "!" },
        new BuffDef { Id = "sg_slug", Name = "独头弹", Desc = "单发高伤 穿透", Tier = 1, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "sg_stun", Name = "震撼弹", Desc = "命中眩晕敌人", Tier = 1, Weapon = "shotgun", Icon = "S" },
        new BuffDef { Id = "sg_explosive", Name = "爆破弹", Desc = "弹丸爆炸效果", Tier = 2, Weapon = "shotgun", Icon = "O" },
        new BuffDef { Id = "sg_vampire", Name = "嗜血本能", Desc = "击杀恢复10HP", Tier = 2, Weapon = "shotgun", Icon = "V" },
        new BuffDef { Id = "sg_mega", Name = "毁灭散射", Desc = "弹丸+6 伤害+50%", Tier = 3, Weapon = "shotgun", Icon = "M" },
        new BuffDef { Id = "sg_nuke", Name = "核弹霰弹", Desc = "每发子弹爆炸", Tier = 4, Weapon = "shotgun", Icon = "N" },

        // 火焰枪专属 Buff
        new BuffDef { Id = "flame_burn", Name = "持续灼烧", Desc = "附加燃烧伤害", Tier = 0, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "flame_range", Name = "延伸喷嘴", Desc = "射程+40%", Tier = 0, Weapon = "flame", Icon = ">" },
        new BuffDef { Id = "flame_wave", Name = "火焰波", Desc = "火焰范围+60%", Tier = 1, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "flame_speed", Name = "增压燃烧", Desc = "射速+50%", Tier = 1, Weapon = "flame", Icon = "F" },
        new BuffDef { Id = "flame_dragon", Name = "炎龙吐息", Desc = "射出龙形火焰", Tier = 2, Weapon = "flame", Icon = "D" },
        new BuffDef { Id = "flame_aoe", Name = "燃烧领域", Desc = "留下火焰地面", Tier = 2, Weapon = "flame", Icon = "A" },
        new BuffDef { Id = "flame_inferno", Name = "炼狱之火", Desc = "伤害+80% 范围+50%", Tier = 3, Weapon = "flame", Icon = "I" },
        new BuffDef { Id = "flame_phoenix", Name = "凤凰涅槃", Desc = "火焰穿透 持续灼烧", Tier = 4, Weapon = "flame", Icon = "P" },

        // 激光枪专属 Buff
        new BuffDef { Id = "laser_beam", Name = "聚焦光束", Desc = "光束宽度+150%", Tier = 0, Weapon = "laser", Icon = "L" },
        new BuffDef { Id = "laser_dmg", Name = "能量增幅", Desc = "伤害+35%", Tier = 0, Weapon = "laser", Icon = "!" },
        new BuffDef { Id = "laser_chain", Name = "连锁闪电", Desc = "命中后弹射", Tier = 1, Weapon = "laser", Icon = "L" },
        new BuffDef { Id = "laser_pierce", Name = "贯穿射线", Desc = "穿透所有敌人", Tier = 1, Weapon = "laser", Icon = "#" },
        new BuffDef { Id = "laser_overload", Name = "超载激光", Desc = "伤害+80%", Tier = 2, Weapon = "laser", Icon = "O" },
        new BuffDef { Id = "laser_speed", Name = "快速充能", Desc = "射速+40%", Tier = 2, Weapon = "laser", Icon = ">" },
        new BuffDef { Id = "laser_prism", Name = "棱镜折射", Desc = "分裂为3道激光", Tier = 3, Weapon = "laser", Icon = "P" },
        new BuffDef { Id = "laser_death", Name = "死亡光线", Desc = "伤害+150% 宽度+100%", Tier = 4, Weapon = "laser", Icon = "D" },

        // 火箭筒专属 Buff
        new BuffDef { Id = "grenade", Name = "手雷精通", Desc = "爆炸范围+40%", Tier = 0, Weapon = "rocket", Icon = "O" },
        new BuffDef { Id = "rocket_dmg", Name = "高爆弹头", Desc = "伤害+45%", Tier = 0, Weapon = "rocket", Icon = "!" },
        new BuffDef { Id = "rocket_cluster", Name = "集束弹头", Desc = "分裂为3枚", Tier = 1, Weapon = "rocket", Icon = "R" },
        new BuffDef { Id = "rocket_speed", Name = "推进强化", Desc = "弹速+60%", Tier = 1, Weapon = "rocket", Icon = ">" },
        new BuffDef { Id = "rocket_nuke", Name = "微型核弹", Desc = "爆炸范围+100%", Tier = 2, Weapon = "rocket", Icon = "N" },
        new BuffDef { Id = "rocket_homing", Name = "追踪导弹", Desc = "自动追踪敌人", Tier = 2, Weapon = "rocket", Icon = "H" },
        new BuffDef { Id = "rocket_mega", Name = "毁灭打击", Desc = "伤害+100% 范围+60%", Tier = 3, Weapon = "rocket", Icon = "M" },
        new BuffDef { Id = "rocket_apocalypse", Name = "末日审判", Desc = "分裂5枚 核爆范围", Tier = 4, Weapon = "rocket", Icon = "A" },
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
        ["shooter"] = new EnemyTypeDef { W = 18, H = 28, HP = 20, Speed = 0.8f, Damage = 12, Color = new Color(255, 153, 51), Score = 200 },
        ["bomber"] = new EnemyTypeDef { W = 22, H = 26, HP = 35, Speed = 2.5f, Damage = 40, Color = new Color(255, 100, 0), Score = 350 },
        ["healer"] = new EnemyTypeDef { W = 20, H = 28, HP = 40, Speed = 1.0f, Damage = 5, Color = new Color(100, 255, 100), Score = 400 },
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