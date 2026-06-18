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

        var weaponIds = new List<string>();
        if (player != null)
            foreach (var w in player.Weapons)
                weaponIds.Add(w.Id);

        var pool = new List<BuffDef>();
        foreach (var b in Config.BUFFS)
        {
            if (b.Weapon == null) pool.Add(b);
            else if (weaponIds.Contains(b.Weapon)) pool.Add(b);
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
            case "dmg": player.BuffDmg += 0.20f; break;
            case "spd": player.BuffSpeed += 0.15f; break;
            case "hp": player.BuffHp += 0.25f; break;
            case "regen": player.BuffRegen += 1; break;
            case "ammo_bag":
                foreach (var w in player.Weapons) w.Reserve = (int)(w.Reserve * 1.5f);
                break;
            case "fir": player.BuffFireRate += 0.25f; break;
            case "vmp": player.BuffVampire += 5; break;
            case "grenade": player.BuffExplosion += 0.40f; break;
            case "rifle_dmg":
                if (!player.BuffWeaponDmg.ContainsKey("rifle")) player.BuffWeaponDmg["rifle"] = 0;
                player.BuffWeaponDmg["rifle"] += 0.35f;
                break;
            case "sg_wide": player.BuffSgPellets += 3; break;
            case "flame_burn": player.BuffBurn = true; break;
            case "laser_beam": player.BuffLaserBeam = true; break;
            case "prc": player.BuffPierce = true; break;
            case "dbl": player.BuffDouble = true; break;
            case "crit": player.BuffCritChance += 0.15f; break;
            case "rifle_burst": player.BuffBurst = true; break;
            case "sg_slug": player.BuffSlug = true; break;
            case "flame_wave": player.BuffFlameRange += 0.60f; break;
            case "laser_chain": player.BuffChain = true; break;
            case "rocket_cluster": player.BuffCluster = true; break;
            case "bsk": player.BuffBerserk = true; break;
            case "shield": player.BuffShield = true; player.ShieldCd = 0; break;
            case "reload_all": player.BuffReloadSpeed += 0.60f; break;
            case "rifle_turret": player.BuffTurret = true; break;
            case "sg_stun": player.BuffStun = true; break;
            case "flame_dragon": player.BuffDragon = true; break;
            case "laser_overload":
                if (!player.BuffWeaponDmg.ContainsKey("laser")) player.BuffWeaponDmg["laser"] = 0;
                player.BuffWeaponDmg["laser"] += 0.80f;
                break;
            case "rocket_nuke": player.BuffNuke = true; break;
            case "god": player.BuffDmg += 0.80f; player.BuffSpeed += 0.30f; break;
            case "inf_ammo": player.BuffInfAmmo = true; break;
            case "death_blow": player.BuffDeathBlow = true; break;
            case "bullet_hell": player.BuffExtraBullets += 5; break;
        }
        player.ActiveBuffs.Add(buff);
        AudioManager.Buff();
    }
}