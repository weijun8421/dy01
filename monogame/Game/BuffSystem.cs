using System;
using System.Collections.Generic;
using System.Linq;
using DY01.Data;
using DY01.Entities;
using DY01.Engine;

namespace DY01.Game;

public static class BuffSystem
{
    private static Random _rng = new();

    public static List<BuffDef> GetChoices(int levelNum, Player? player = null)
    {
        float[] tierWeights = levelNum switch
        {
            <= 3 => new[] { 0.55f, 0.30f, 0.12f, 0.03f, 0.00f },
            <= 6 => new[] { 0.35f, 0.32f, 0.20f, 0.10f, 0.03f },
            <= 10 => new[] { 0.20f, 0.28f, 0.25f, 0.18f, 0.09f },
            _ => new[] { 0.10f, 0.20f, 0.28f, 0.25f, 0.17f },
        };

        // 获取玩家当前武器ID（单武器模式）
        string? currentWeaponId = player?.Weapon.Id;

        var pool = new List<BuffDef>();
        foreach (var b in Config.BUFFS)
        {
            // 通用Buff始终可用
            if (b.Weapon == null) 
            {
                pool.Add(b);
            }
            // 武器专属Buff只在持有对应武器时可用
            else if (currentWeaponId != null && b.Weapon == currentWeaponId) 
            {
                pool.Add(b);
            }
        }

        var tierPools = new Dictionary<int, List<BuffDef>>();
        for (int t = 0; t < 5; t++) tierPools[t] = new();
        foreach (var b in pool) tierPools[b.Tier].Add(b);

        var choices = new List<BuffDef>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < 3; i++)
        {
            int tier = WeightedRandom(tierWeights);
            for (int t = tier; t >= 0; t--)
            {
                var candidates = tierPools[t].Where(b => !usedIds.Contains(b.Id)).ToList();
                if (candidates.Count > 0)
                {
                    var pick = candidates[_rng.Next(candidates.Count)];
                    choices.Add(pick);
                    usedIds.Add(pick.Id);
                    break;
                }
            }
        }

        return choices;
    }

    private static int WeightedRandom(float[] weights)
    {
        float total = weights.Sum();
        float roll = (float)_rng.NextDouble() * total;
        float cumulative = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return i;
        }
        return weights.Length - 1;
    }

    public static void Apply(Player player, BuffDef buff)
    {
        switch (buff.Id)
        {
            // 通用Buff
            case "dmg": player.BuffDmg += 0.20f; break;
            case "spd": player.BuffSpeed += 0.15f; break;
            case "hp": player.BuffHp += 0.25f; break;
            case "regen": player.BuffRegen += 1; break;
            case "ammo_bag": player.Weapon.Reserve = (int)(player.Weapon.Reserve * 1.5f); break;
            case "fir": player.BuffFireRate += 0.25f; break;
            case "vmp": player.BuffVampire += 5; break;
            case "prc": player.BuffPierce = true; break;
            case "dbl": player.BuffDouble = true; break;
            case "crit": player.BuffCritChance += 0.15f; break;
            case "bsk": player.BuffBerserk = true; break;
            case "shield": player.BuffShield = true; player.ShieldCd = 0; break;
            case "reload_all": player.BuffReloadSpeed += 0.60f; break;
            case "god": player.BuffDmg += 0.80f; player.BuffSpeed += 0.30f; break;
            case "inf_ammo": player.BuffInfAmmo = true; break;
            case "death_blow": player.BuffDeathBlow = true; break;

            // 突击步枪专属
            case "rifle_dmg":
                if (!player.BuffWeaponDmg.ContainsKey("rifle")) player.BuffWeaponDmg["rifle"] = 0;
                player.BuffWeaponDmg["rifle"] += 0.35f;
                break;
            case "rifle_speed": player.BuffFireRate += 0.30f; break;
            case "rifle_burst": player.BuffBurst = true; break;
            case "rifle_pierce": player.BuffPierce = true; break;
            case "rifle_turret": player.BuffTurret = true; break;
            case "rifle_crit": player.BuffCritChance += 0.25f; break;
            case "rifle_hypershot": player.BuffFireRate += 0.60f; player.BuffDmg += 0.40f; break;
            case "rifle_gauss": 
                player.Weapon.BulletSpeed *= 2;
                if (!player.BuffWeaponDmg.ContainsKey("rifle")) player.BuffWeaponDmg["rifle"] = 0;
                player.BuffWeaponDmg["rifle"] += 0.80f;
                break;

            // 霰弹枪专属
            case "sg_wide": player.BuffSgPellets += 3; break;
            case "sg_dmg":
                if (!player.BuffWeaponDmg.ContainsKey("shotgun")) player.BuffWeaponDmg["shotgun"] = 0;
                player.BuffWeaponDmg["shotgun"] += 0.40f;
                break;
            case "sg_slug": player.BuffSlug = true; break;
            case "sg_stun": player.BuffStun = true; break;
            case "sg_explosive": player.Weapon.Explosive = true; player.Weapon.ExplosionRadius = 30; break;
            case "sg_vampire": player.BuffVampire += 10; break;
            case "sg_mega": player.BuffSgPellets += 6; player.BuffDmg += 0.50f; break;
            case "sg_nuke": player.BuffNuke = true; break;

            // 火焰枪专属
            case "flame_burn": player.BuffBurn = true; break;
            case "flame_range": player.BuffFlameRange += 0.40f; break;
            case "flame_wave": player.BuffFlameRange += 0.60f; break;
            case "flame_speed": player.BuffFireRate += 0.50f; break;
            case "flame_dragon": player.BuffDragon = true; break;
            case "flame_aoe": player.BuffFlameRange += 0.30f; break;
            case "flame_inferno": player.BuffDmg += 0.80f; player.BuffFlameRange += 0.50f; break;
            case "flame_phoenix": player.BuffPierce = true; player.BuffBurn = true; break;

            // 激光枪专属
            case "laser_beam": player.BuffLaserBeam = true; break;
            case "laser_dmg":
                if (!player.BuffWeaponDmg.ContainsKey("laser")) player.BuffWeaponDmg["laser"] = 0;
                player.BuffWeaponDmg["laser"] += 0.35f;
                break;
            case "laser_chain": player.BuffChain = true; break;
            case "laser_pierce": player.BuffPierce = true; break;
            case "laser_overload":
                if (!player.BuffWeaponDmg.ContainsKey("laser")) player.BuffWeaponDmg["laser"] = 0;
                player.BuffWeaponDmg["laser"] += 0.80f;
                break;
            case "laser_speed": player.BuffFireRate += 0.40f; break;
            case "laser_prism": player.BuffDouble = true; break;
            case "laser_death": player.BuffDmg += 1.50f; player.BuffLaserBeam = true; break;

            // 火箭筒专属
            case "grenade": player.BuffExplosion += 0.40f; break;
            case "rocket_dmg":
                if (!player.BuffWeaponDmg.ContainsKey("rocket")) player.BuffWeaponDmg["rocket"] = 0;
                player.BuffWeaponDmg["rocket"] += 0.45f;
                break;
            case "rocket_cluster": player.BuffCluster = true; break;
            case "rocket_speed": player.Weapon.BulletSpeed *= 1.6f; break;
            case "rocket_nuke": player.BuffNuke = true; break;
            case "rocket_homing": break; // 追踪导弹（需要额外逻辑）
            case "rocket_mega": player.BuffDmg += 1.0f; player.BuffExplosion += 0.60f; break;
            case "rocket_apocalypse": player.BuffCluster = true; player.BuffNuke = true; break;
        }
        player.ActiveBuffs.Add(buff);
        AudioManager.Buff();
    }
}