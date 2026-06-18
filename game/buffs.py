"""
DY01 - Buff系统
5档稀有度（白/蓝/紫/金/红），武器专属加成
"""
import random
from data.config import BUFFS, TIER_COLORS, TIER_NAMES
from engine import audio


class BuffSystem:
    """Buff管理"""

    @staticmethod
    def get_choices(level_num, player=None):
        """根据关卡和玩家武器生成3个Buff选择"""
        # 根据进度决定稀有度权重
        if level_num <= 3:
            tier_weights = [0.55, 0.30, 0.12, 0.03, 0.0]
        elif level_num <= 6:
            tier_weights = [0.35, 0.32, 0.20, 0.10, 0.03]
        elif level_num <= 10:
            tier_weights = [0.20, 0.28, 0.25, 0.18, 0.09]
        else:
            tier_weights = [0.10, 0.20, 0.28, 0.25, 0.17]

        # 获取玩家武器ID列表
        weapon_ids = []
        if player:
            for w in player.weapons:
                weapon_ids.append(w.get('id', ''))

        # 构建候选池：通用 + 玩家当前武器专属
        pool = []
        for b in BUFFS:
            if b['weapon'] is None:
                pool.append(b)
            elif b['weapon'] in weapon_ids:
                pool.append(b)

        # 按稀有度分组
        tier_pools = {t: [] for t in range(5)}
        for b in pool:
            tier_pools[b['tier']].append(b)

        # 选3个不同稀有度的Buff
        choices = []
        used_ids = set()
        for _ in range(3):
            # 按权重选稀有度
            tier = random.choices(range(5), weights=tier_weights, k=1)[0]
            # 如果该稀有度没候选，降级找
            for t in range(tier, -1, -1):
                candidates = [b for b in tier_pools[t] if b['id'] not in used_ids]
                if candidates:
                    pick = random.choice(candidates)
                    choices.append(pick)
                    used_ids.add(pick['id'])
                    break
            else:
                # 实在没有，随便拿一个
                remaining = [b for b in pool if b['id'] not in used_ids]
                if remaining:
                    pick = random.choice(remaining)
                    choices.append(pick)
                    used_ids.add(pick['id'])

        return choices

    @staticmethod
    def apply(player, buff):
        bid = buff['id']
        if bid == 'dmg':
            player.buff_dmg += 0.20
        elif bid == 'spd':
            player.buff_speed += 0.15
        elif bid == 'hp':
            player.buff_hp += 0.25
        elif bid == 'regen':
            player.buff_regen = getattr(player, 'buff_regen', 0) + 1
        elif bid == 'ammo_bag':
            for w in player.weapons:
                w['reserve'] = int(w['reserve'] * 1.5)
        elif bid == 'fir':
            player.buff_fire_rate += 0.25
        elif bid == 'vmp':
            player.buff_vampire += 5
        elif bid == 'grenade':
            player.buff_explosion += 0.40
        elif bid == 'rifle_dmg':
            player.buff_weapon_dmg['rifle'] = getattr(player, 'buff_weapon_dmg', {}).get('rifle', 0) + 0.35
        elif bid == 'sg_wide':
            player.buff_sg_pellets = getattr(player, 'buff_sg_pellets', 0) + 3
        elif bid == 'flame_burn':
            player.buff_burn = True
        elif bid == 'laser_beam':
            player.buff_laser_beam = True
        elif bid == 'prc':
            player.buff_pierce = True
        elif bid == 'dbl':
            player.buff_double = True
        elif bid == 'crit':
            player.buff_crit_chance = getattr(player, 'buff_crit_chance', 0) + 0.15
        elif bid == 'rifle_burst':
            player.buff_burst = True
        elif bid == 'sg_slug':
            player.buff_slug = True
        elif bid == 'flame_wave':
            player.buff_flame_range = getattr(player, 'buff_flame_range', 0) + 0.60
        elif bid == 'laser_chain':
            player.buff_chain = True
        elif bid == 'rocket_cluster':
            player.buff_cluster = True
        elif bid == 'bsk':
            player.buff_berserk = True
        elif bid == 'shield':
            player.buff_shield = True
            player.shield_cd = 0
        elif bid == 'reload_all':
            player.buff_reload_speed = getattr(player, 'buff_reload_speed', 0) + 0.60
        elif bid == 'rifle_turret':
            player.buff_turret = True
        elif bid == 'sg_stun':
            player.buff_stun = True
        elif bid == 'flame_dragon':
            player.buff_dragon = True
        elif bid == 'laser_overload':
            player.buff_weapon_dmg = getattr(player, 'buff_weapon_dmg', {})
            player.buff_weapon_dmg['laser'] = player.buff_weapon_dmg.get('laser', 0) + 0.80
        elif bid == 'rocket_nuke':
            player.buff_nuke = True
        elif bid == 'god':
            player.buff_dmg += 0.80
            player.buff_speed += 0.30
        elif bid == 'inf_ammo':
            player.buff_inf_ammo = True
        elif bid == 'death_blow':
            player.buff_death_blow = True
        elif bid == 'bullet_hell':
            player.buff_extra_bullets = getattr(player, 'buff_extra_bullets', 0) + 5

        player.active_buffs.append(buff)
        audio.buff()