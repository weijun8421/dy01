"""
DY01 - 玩家
"""
import math
import random
import pygame
from data.config import (
    TILE, GRAVITY, MAX_FALL, ACCEL, FRICTION,
    JUMP_VEL, SHORT_JUMP, COYOTE_FRAMES, JUMP_BUFFER,
    WEAPONS, COLOR_P1, COLOR_P2, COLOR_SKIN,
)
from engine.physics import Physics
from engine import audio


class Player:
    def __init__(self, x, y, is_p2=False):
        self.x = x
        self.y = y
        self.w = 16
        self.h = 26
        self.vx = 0.0
        self.vy = 0.0
        self.is_p2 = is_p2
        self.color = COLOR_P2 if is_p2 else COLOR_P1
        self.skin_color = COLOR_SKIN
        self.hair_color = (17, 85, 170) if is_p2 else (85, 51, 17)
        self.hp = 100
        self.max_hp = 100
        self.grounded = False
        self.facing = 1
        self.anim = 0.0
        self.anim_spd = 0.0
        self.dead = False
        self.respawn_timer = 0
        self.invincible = 0
        self.dash_cooldown = 0
        self.coyote = 0
        self.jump_buffer = 0
        self.holding_jump = False
        self.was_in_air = True

        # 武器
        self.weapon_idx = 0
        self.weapons = [dict(w) for w in WEAPONS]
        self.fire_cd = 0
        self.reloading = 0

        # Buff状态
        self.buff_dmg = 1.0
        self.buff_speed = 1.0
        self.buff_fire_rate = 1.0
        self.buff_hp = 1.0
        self.buff_explosion = 1.0
        self.buff_pierce = False
        self.buff_vampire = 0
        self.buff_double = False
        self.buff_berserk = False
        self.buff_nuke = False
        self.buff_regen = 0
        self.buff_weapon_dmg = {}
        self.buff_sg_pellets = 0
        self.buff_burn = False
        self.buff_laser_beam = False
        self.buff_crit_chance = 0
        self.buff_burst = False
        self.buff_slug = False
        self.buff_flame_range = 0
        self.buff_chain = False
        self.buff_cluster = False
        self.buff_shield = False
        self.shield_cd = 0
        self.buff_reload_speed = 0
        self.buff_turret = False
        self.buff_stun = False
        self.buff_dragon = False
        self.buff_inf_ammo = False
        self.buff_death_blow = False
        self.buff_extra_bullets = 0
        self.active_buffs = []

    @property
    def weapon(self):
        return self.weapons[self.weapon_idx]

    @property
    def max_hp_actual(self):
        return self.max_hp * self.buff_hp

    @property
    def speed(self):
        return 3.2 * self.buff_speed

    def switch_weapon(self, idx):
        if 0 <= idx < len(self.weapons):
            self.weapon_idx = idx

    def is_berserk(self):
        return self.buff_berserk and self.hp < self.max_hp_actual * 0.3

    def update(self, keys, level, bullets, particles, camera=None):
        if self.dead:
            self.respawn_timer -= 1
            return

        p = 'p2_' if self.is_p2 else 'p1_'
        left = keys.get(p + 'left', False)
        right = keys.get(p + 'right', False)
        jump = keys.get(p + 'jump', False)
        dash = keys.get(p + 'dash', False)
        shoot = keys.get(p + 'shoot', False)
        melee = keys.get(p + 'melee', False)
        reload_k = keys.get(p + 'reload', False)

        # ── 移动 ──
        if left and right:
            # 两键同时按 → 朝当前面向的反方向加速（转身）
            self.vx = Physics.accelerate(self.vx, self.speed, ACCEL, -self.facing)
            self.facing = -self.facing
        elif left:
            self.vx = Physics.accelerate(self.vx, self.speed, ACCEL, -1)
            self.facing = -1
        elif right:
            self.vx = Physics.accelerate(self.vx, self.speed, ACCEL, 1)
            self.facing = 1
        else:
            self.vx = Physics.apply_friction(self.vx, FRICTION)

        # ── 冲刺 ──
        if dash and self.dash_cooldown <= 0:
            self.vx = self.facing * self.speed * 3.5
            self.dash_cooldown = 45
            self.invincible = 18
            audio.dash()
            particles.spawn_burst(
                self.x, self.y + 10, 8, 4,
                (255, 255, 255), (10, 20), (2, 4),
                vy_range=(-3, 1)
            )
        if self.dash_cooldown > 0:
            self.dash_cooldown -= 1

        # ── 护盾冷却 ──
        if not getattr(self, '_shield_ready', True):
            self._shield_cd = getattr(self, '_shield_cd', 0) - 1
            if self._shield_cd <= 0:
                self._shield_ready = True

        # ── 生命恢复 ──
        if self.buff_regen > 0 and self.hp < self.max_hp_actual:
            self._regen_timer = getattr(self, '_regen_timer', 0) + 1
            if self._regen_timer >= 60:  # 每秒恢复
                self._regen_timer = 0
                self.heal(self.buff_regen)

        # ── 跳跃 ──
        self.holding_jump = jump
        if jump:
            self.jump_buffer = JUMP_BUFFER
        elif self.jump_buffer > 0:
            self.jump_buffer -= 1

        if self.grounded:
            self.coyote = COYOTE_FRAMES
        elif self.coyote > 0:
            self.coyote -= 1

        if self.jump_buffer > 0 and self.coyote > 0:
            self.vy = JUMP_VEL
            self.grounded = False
            self.coyote = 0
            self.jump_buffer = 0
            audio.jump()
            particles.spawn_burst(
                self.x, self.y + 13, 4, 2,
                (204, 204, 204), (8, 15), (2, 3),
                vy_range=(-1, 0)
            )

        # 可变跳跃高度
        if not self.holding_jump and self.vy < SHORT_JUMP:
            self.vy = SHORT_JUMP

        # ── 重力 ──
        self.vy = Physics.apply_gravity(self.vy)
        self.x += self.vx
        self.y += self.vy

        # ── 碰撞 ──
        self.grounded = False
        self.x, self.y, self.vx, self.vy, self.grounded = Physics.resolve_collision(
            self.x, self.y, self.w, self.h, self.vx, self.vy, level
        )

        # 落地粒子
        if self.grounded and self.was_in_air and self.vy > 3:
            particles.spawn_burst(
                self.x, self.y + 13, 6, 3,
                (136, 136, 136), (8, 18), (2, 4),
                vy_range=(-3, 0)
            )
        self.was_in_air = not self.grounded

        if self.invincible > 0:
            self.invincible -= 1

        # ── 换弹 ──
        if reload_k and self.reloading <= 0 and self.weapon['ammo'] < self.weapon['maxAmmo'] and self.weapon['reserve'] > 0:
            self.reloading = max(20, int(self.weapon['reloadTime'] * max(0.1, 1.0 - self.buff_reload_speed)))
            audio.pickup()
        if self.reloading > 0:
            self.reloading -= 1
            if self.reloading <= 0:
                need = self.weapon['maxAmmo'] - self.weapon['ammo']
                take = min(need, self.weapon['reserve'])
                self.weapon['ammo'] += take
                self.weapon['reserve'] -= take

        # ── 射击 ──
        if self.fire_cd > 0:
            self.fire_cd -= 1
        if (shoot or melee) and self.fire_cd <= 0 and self.reloading <= 0:
            if self.weapon['ammo'] > 0:
                self._fire(bullets, particles, camera)
            elif self.weapon['reserve'] > 0:
                self.reloading = max(20, int(self.weapon['reloadTime'] * max(0.1, 1.0 - self.buff_reload_speed)))

        # ── 动画 ──
        self.anim_spd = abs(self.vx)
        self.anim += self.anim_spd * 0.15
        if self.anim > 4:
            self.anim -= 4

        # 边界 - 使用关卡宽度
        max_x = 5000
        self.x = max(10, min(max_x, self.x))

    def _fire(self, bullets, particles, camera=None):
        w = self.weapon
        fr = max(2, int(w['fireRate'] / self.buff_fire_rate))
        self.fire_cd = fr

        # 无限弹药
        if not self.buff_inf_ammo:
            w['ammo'] -= 1

        angle = 0 if self.facing > 0 else math.pi
        pellets = w['pellets']

        # 弹幕风暴
        pellets += self.buff_extra_bullets

        # 霰弹枪散射强化
        if w['id'] == 'shotgun':
            pellets += self.buff_sg_pellets

        # 独头弹
        if self.buff_slug and w['id'] == 'shotgun':
            pellets = 1

        shots = 2 if self.buff_double else 1
        # 三连发
        if self.buff_burst and w['id'] == 'rifle':
            shots = 3

        # 集束弹头
        if self.buff_cluster and w['id'] == 'rocket':
            pellets = 3

        # 音效
        n = w['name']
        weapon_sfx = {
            '激光枪': 'laser',
            '火焰枪': 'flame',
            '霰弹枪': 'shotgun',
            '火箭筒': 'rocket',
        }
        audio.shoot(weapon_sfx.get(n, 'rifle'))

        from entities.bullet import Bullet

        for _ in range(shots):
            for i in range(pellets):
                sp = (i - (pellets - 1) / 2) * w['spread']
                a = angle + sp + random.uniform(-0.03, 0.03)

                dmg = w['damage'] * self.buff_dmg

                # 武器专属伤害加成
                wid = w.get('id', '')
                if wid in self.buff_weapon_dmg:
                    dmg *= (1 + self.buff_weapon_dmg[wid])

                # 独头弹伤害翻倍
                if self.buff_slug and w['id'] == 'shotgun':
                    dmg *= 3.5

                # 狂战士
                if self.is_berserk():
                    dmg *= 2

                # 暴击
                is_crit = random.random() < self.buff_crit_chance
                if is_crit:
                    dmg *= 3

                # 致命一击
                is_death_blow = self.buff_death_blow and random.random() < 0.20
                if is_death_blow:
                    dmg = 9999

                # 子弹属性
                bullet_data = {**w, 'damage': dmg}
                bullet_data['is_crit'] = is_crit
                bullet_data['is_death_blow'] = is_death_blow

                # 灼烧
                if self.buff_burn and w['id'] == 'flame':
                    bullet_data['burn'] = True

                # 火焰范围
                if self.buff_flame_range > 0 and w['id'] == 'flame':
                    bullet_data['bulletW'] = int(w['bulletW'] * (1 + self.buff_flame_range))
                    bullet_data['bulletH'] = int(w['bulletH'] * (1 + self.buff_flame_range))

                # 激光宽度
                if self.buff_laser_beam and w['id'] == 'laser':
                    bullet_data['bulletW'] = int(w['bulletW'] * 2.5)
                    bullet_data['bulletH'] = int(w['bulletH'] * 1.5)

                # 震撼弹
                if self.buff_stun and w['id'] == 'shotgun':
                    bullet_data['stun'] = True

                # 连锁闪电（标记需要连锁）
                if self.buff_chain and w['id'] == 'laser':
                    bullet_data['chain'] = True

                owner_id = 'p2' if self.is_p2 else 'p1'
                bullets.append(Bullet(
                    self.x + self.facing * 12, self.y - 3,
                    math.cos(a) * w['bulletSpeed'],
                    math.sin(a) * w['bulletSpeed'],
                    bullet_data,
                    owner_id,
                ))

        # 后坐力
        self.vx -= math.cos(angle) * (2.5 if w.get('explosive') else 0.8)
        self.vy -= math.sin(angle) * 0.3

        # 枪口闪光
        particles.spawn_burst(
            self.x + self.facing * 16, self.y - 3, 6, 8,
            w['color'], (4, 10), (2, 5),
            vy_range=(-3, 3)
        )

        # 屏幕震动
        if camera:
            camera.add_shake(10 if w.get('explosive') else 3)

    def take_damage(self, dmg, particles, shake_func):
        if self.dead or self.invincible > 0:
            return

        # 护盾：挡一次伤害
        if self.buff_shield and getattr(self, '_shield_ready', True):
            self._shield_ready = False
            self._shield_cd = 300  # 5秒冷却
            particles.spawn_burst(
                self.x, self.y - 10, 8, 5,
                (100, 200, 255), (10, 20), (2, 4),
                vy_range=(-3, 1)
            )
            particles.spawn_text(self.x, self.y - 20, 'SHIELD', (100, 200, 255))
            return

        self.hp -= dmg
        self.invincible = 28
        shake_func(7)
        audio.hit()
        particles.spawn_burst(
            self.x, self.y, 10, 4,
            (255, 0, 0), (10, 25), (2, 5),
            vy_range=(-4, 2)
        )
        particles.spawn_text(self.x, self.y - 15, f'-{int(dmg)}', (255, 68, 68))
        if self.hp <= 0:
            self.hp = 0
            self.die(particles)

    def die(self, particles):
        self.dead = True
        self.respawn_timer = 200
        audio.die()
        particles.spawn_burst(
            self.x, self.y, 30, 6,
            self.color, (20, 50), (3, 7),
            vy_range=(-8, 2)
        )

    def respawn(self, other_player):
        self.dead = False
        self.hp = self.max_hp_actual * 0.5
        self.invincible = 70
        if other_player and not other_player.dead:
            self.x = other_player.x + (30 if self.is_p2 else -30)
            self.y = other_player.y
        else:
            self.x = 100
            self.y = 300

    def heal(self, amt):
        if self.dead:
            return
        self.hp = min(self.max_hp_actual, self.hp + amt)

    def draw(self, surf, cam_x):
        if self.dead:
            return
        if self.invincible > 0 and (self.invincible // 4) % 2 == 0:
            return

        sx = int(self.x - cam_x)
        sy = int(self.y)
        f = self.facing

        # 阴影
        pygame.draw.rect(surf, (0, 0, 0, 50), (sx - 7, sy + 12, 14, 3))

        # 腿动画
        leg_off = 0
        if self.grounded:
            if self.anim_spd > 0.3:
                leg_off = int(math.sin(self.anim * 1.5) * 4)
        else:
            leg_off = -3

        pygame.draw.rect(surf, (51, 68, 102), (sx - 5 + leg_off, sy + 6, 5, 8))
        pygame.draw.rect(surf, (51, 68, 102), (sx - leg_off, sy + 6, 5, 8))
        pygame.draw.rect(surf, (34, 34, 34), (sx - 6 + leg_off, sy + 12, 6, 3))
        pygame.draw.rect(surf, (34, 34, 34), (sx - 1 - leg_off, sy + 12, 6, 3))

        # 身体
        pygame.draw.rect(surf, self.color, (sx - 7, sy - 8, 14, 16))
        # 肌肉线
        s2 = pygame.Surface((10, 2), pygame.SRCALPHA)
        s2.fill((0, 0, 0, 64))
        surf.blit(s2, (sx - 5, sy - 4))
        s3 = pygame.Surface((2, 8), pygame.SRCALPHA)
        s3.fill((0, 0, 0, 64))
        surf.blit(s3, (sx - 1, sy - 8))
        # 腰带
        pygame.draw.rect(surf, (51, 51, 51), (sx - 7, sy + 6, 14, 3))
        pygame.draw.rect(surf, (255, 204, 0), (sx - 2, sy + 6, 4, 3))

        # 头
        pygame.draw.rect(surf, self.skin_color, (sx - 5, sy - 16, 10, 10))
        # 头发
        pygame.draw.rect(surf, self.hair_color, (sx - 6, sy - 18, 12, 4))
        pygame.draw.rect(surf, self.hair_color, (sx - 6, sy - 16, 3, 6))
        pygame.draw.rect(surf, self.hair_color, (sx + 3, sy - 16, 3, 6))
        # 墨镜
        pygame.draw.rect(surf, (17, 17, 17), (sx + f * 2 - 3, sy - 13, 7, 3))
        # 嘴
        pygame.draw.rect(surf, (0, 0, 0), (sx + f * 2, sy - 9, 3, 1))

        # 手臂 + 武器
        pygame.draw.rect(surf, self.skin_color, (sx + f * 8, sy - 8, 4, 8))
        wx = sx + f * 12
        pygame.draw.rect(surf, (85, 85, 85), (wx - 3, sy - 5, 8, 4))
        pygame.draw.rect(surf, (51, 51, 51), (wx + f * 4, sy - 4, 5, 3))

        # 狂战士光环
        if self.is_berserk():
            bs = pygame.Surface((44, 44), pygame.SRCALPHA)
            pygame.draw.circle(bs, (255, 0, 0, 40), (22, 22), 20, 2)
            surf.blit(bs, (sx - 22, sy - 24))