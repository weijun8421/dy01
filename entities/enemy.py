"""
DY01 - 敌人
巡逻 + 索敌AI，像素小人外观
"""
import math
import random
import pygame
from data.config import TILE, GRAVITY, MAX_FALL, ENEMY_TYPES
from engine.physics import Physics
from engine import audio


class Enemy:
    def __init__(self, x, y, etype):
        self.x = x
        self.y = y
        self.type = etype
        self.vx = 0.0
        self.vy = 0.0
        self.grounded = False
        self.anim = 0
        self.flash = 0

        t = ENEMY_TYPES[etype]
        self.w = t['w']
        self.h = t['h']
        self.hp = t['hp']
        self.max_hp = t['hp']
        self.speed = t['speed']
        self.damage = t['damage']
        self.color = t['color']
        self.score = t['score']
        self.fly = t.get('fly', False)
        self._attack_cd = 30
        self.facing = 1

        # 巡逻状态
        self._patrol_center = x
        self._patrol_range = 80 + random.randint(0, 60)
        self._patrol_dir = 1
        self._patrol_timer = random.randint(30, 90)
        self._aggro = False
        self._detect_range = 220
        self._stun = 0
        self._burn = 0
        self._burn_timer = 0

    def update(self, p1, p2, level, particles, camera=None):
        self.anim += 1
        if self.flash > 0:
            self.flash -= 1
        if self._attack_cd > 0:
            self._attack_cd -= 1

        # 眩晕
        if self._stun > 0:
            self._stun -= 1
            return True  # 眩晕时不移动不攻击

        # 灼烧DOT
        if self._burn > 0:
            self._burn_timer -= 1
            if self._burn_timer <= 0:
                self._burn_timer = 15  # 每15帧烧一次
                self.hp -= self._burn
                self.flash = 4
                particles.spawn_burst(
                    self.x, self.y - 10, 3, 2,
                    (255, 136, 0), (5, 10), (1, 2),
                    vy_range=(-2, 0)
                )
                if self.hp <= 0:
                    self.hp = 0
                    return True  # 烧死了
            self._burn -= 1

        # 选择目标
        tgt, d = self._find_target(p1, p2)

        # 索敌判定
        if tgt and d < self._detect_range:
            self._aggro = True
        elif tgt and d > self._detect_range * 1.3:
            self._aggro = False

        if not tgt:
            return True

        if self.fly:
            self._update_fly(tgt, d)
        else:
            self._update_ground(tgt, d, level)

        # 碰撞伤害
        if d < (self.w + tgt.w) / 2 + 4:
            if self._attack_cd <= 0:
                tgt.take_damage(self.damage, particles, camera.add_shake if camera else (lambda _: None))
                self._attack_cd = 30

        return self.hp > 0

    def _find_target(self, p1, p2):
        tgt = None
        d = float('inf')
        if p1 and not p1.dead:
            tgt, d = p1, math.hypot(self.x - p1.x, self.y - p1.y)
        if p2 and not p2.dead:
            d2 = math.hypot(self.x - p2.x, self.y - p2.y)
            if d2 < d:
                tgt, d = p2, d2
        return tgt, d

    def _update_fly(self, tgt, d):
        dx = tgt.x - self.x
        dy = tgt.y - self.y
        if d > 5:
            self.vx = (dx / d) * self.speed
            self.vy = (dy / d) * self.speed
        self.facing = 1 if self.vx >= 0 else -1
        self.x += self.vx
        self.y += self.vy

    def _update_ground(self, tgt, d, level):
        if self._aggro and d > 5:
            # 索敌：向玩家移动
            dx = tgt.x - self.x
            self.vx = Physics.accelerate(self.vx, self.speed, 0.3, 1 if dx > 0 else -1)
            self.facing = 1 if dx > 0 else -1
        else:
            # 巡逻：来回走动
            self._patrol_timer -= 1
            if self._patrol_timer <= 0:
                self._patrol_dir *= -1
                self._patrol_timer = random.randint(40, 100)

            offset = self.x - self._patrol_center
            if abs(offset) > self._patrol_range:
                self._patrol_dir = -1 if offset > 0 else 1

            self.vx = Physics.accelerate(self.vx, self.speed * 0.5, 0.15, self._patrol_dir)
            self.facing = self._patrol_dir

        self.vy = Physics.apply_gravity(self.vy)
        self.x += self.vx
        self.y += self.vy
        self.grounded = False
        self.x, self.y, self.vx, self.vy, self.grounded = Physics.resolve_collision(
            self.x, self.y, self.w, self.h, self.vx, self.vy, level
        )

    def hit(self, dmg, particles):
        self.hp -= dmg
        self.flash = 4
        self._aggro = True  # 被击中后立即索敌
        if self.hp <= 0:
            particles.spawn_burst(
                self.x, self.y, 15, 5,
                self.color, (15, 35), (2, 6),
                vy_range=(-6, 2)
            )
            particles.spawn_text(self.x, self.y, f'+{self.score}', (255, 170, 0), 14)
            audio.kill()
            return True
        audio.hit()
        return False

    def apply_effects(self, bullet_data):
        """应用子弹特殊效果"""
        if bullet_data.get('stun'):
            self._stun = 60  # 眩晕1秒
        if bullet_data.get('burn'):
            self._burn = 120  # 灼烧2秒
            self._burn_timer = 0
        if bullet_data.get('is_crit'):
            self.flash = 8  # 暴击更长闪烁
        if bullet_data.get('is_death_blow'):
            self.flash = 12

    def draw(self, surf, cam_x):
        """像素小人外观（类似玩家）"""
        sx = int(self.x - cam_x)
        sy = int(self.y)
        color = (255, 255, 255) if self.flash > 0 else self.color
        f = self.facing
        hw, hh = self.w // 2, self.h // 2

        if self.type == 'boss':
            # Boss：大型像素怪
            pygame.draw.rect(surf, color, (sx - hw, sy - hh, self.w, self.h))
            eye_c = (255, 255, 255) if self.flash > 0 else (255, 0, 0)
            pygame.draw.rect(surf, eye_c, (sx - 8, sy - 12, 6, 4))
            pygame.draw.rect(surf, eye_c, (sx + 2, sy - 12, 6, 4))
            pygame.draw.rect(surf, (68, 68, 68), (sx - hw - 3, sy - hh, 5, 8))
            pygame.draw.rect(surf, (68, 68, 68), (sx + hw - 2, sy - hh, 5, 8))
            # 感叹号（巡逻/索敌指示）
            if not self._aggro and self.flash <= 0:
                pygame.draw.rect(surf, (255, 255, 255), (sx - 1, sy - hh - 10, 2, 8))
                pygame.draw.rect(surf, (255, 255, 255), (sx - 1, sy - hh - 12, 2, 2))

        elif self.fly:
            # 飞行敌人：蝙蝠型
            wy = math.sin(self.anim * 0.25) * 3
            pygame.draw.rect(surf, color, (sx - hw, sy - hh, self.w, self.h))
            wing_c = (255, 255, 255) if self.flash > 0 else (170, 136, 255)
            pygame.draw.rect(surf, wing_c, (sx - hw - 2, int(sy + wy), 4, 3))
            pygame.draw.rect(surf, wing_c, (sx + hw - 2, int(sy - wy), 4, 3))
            eye_c = (255, 255, 255) if self.flash > 0 else (255, 255, 0)
            pygame.draw.rect(surf, eye_c, (sx + f * 2, sy - 4, 3, 3))

        else:
            # 地面敌人：像素小人（类似玩家外观）
            leg_off = 0
            if self.grounded and abs(self.vx) > 0.2:
                leg_off = int(math.sin(self.anim * 0.15 * self.speed * 2) * 3)
            elif not self.grounded:
                leg_off = -2

            # 腿
            pygame.draw.rect(surf, (44, 58, 88), (sx - 4 + leg_off, sy + 5, 4, 6))
            pygame.draw.rect(surf, (44, 58, 88), (sx - leg_off, sy + 5, 4, 6))
            pygame.draw.rect(surf, (30, 30, 30), (sx - 4 + leg_off, sy + 10, 5, 2))
            pygame.draw.rect(surf, (30, 30, 30), (sx - 2 - leg_off, sy + 10, 5, 2))

            # 身体
            body_h = self.h - 10
            pygame.draw.rect(surf, color, (sx - hw, sy - body_h + 2, self.w, body_h))
            # 肌肉线
            pygame.draw.rect(surf, (0, 0, 0, 50), (sx - hw + 2, sy - 2, self.w - 4, 2))

            # 头
            head_y = sy - body_h - 2
            pygame.draw.rect(surf, self._skin_color(), (sx - 4, head_y, 8, 8))
            # 头发
            hair_c = self._hair_color()
            pygame.draw.rect(surf, hair_c, (sx - 5, head_y - 2, 10, 3))
            pygame.draw.rect(surf, hair_c, (sx - 5, head_y, 3, 5))
            pygame.draw.rect(surf, hair_c, (sx + 2, head_y, 3, 5))
            # 眼睛
            eye_c = (255, 255, 255) if self.flash > 0 else (255, 255, 0)
            pygame.draw.rect(surf, eye_c, (sx + f * 2, head_y + 2, 3, 2))

            # 巡逻指示（问号）
            if not self._aggro and self.flash <= 0:
                qm_color = (230, 230, 230) if self.anim % 60 < 30 else (180, 180, 180)
                pygame.draw.rect(surf, qm_color, (sx - 2, head_y - 8, 1, 2))
                pygame.draw.rect(surf, qm_color, (sx - 2, head_y - 10, 5, 1))
                pygame.draw.rect(surf, qm_color, (sx + 3, head_y - 10, 1, 3))
                pygame.draw.rect(surf, qm_color, (sx + 2, head_y - 8, 1, 1))

            # 索敌指示（感叹号）
            if self._aggro and self.flash <= 0:
                ex_color = (255, 100, 100)
                pygame.draw.rect(surf, ex_color, (sx - 1, head_y - 10, 2, 6))
                pygame.draw.rect(surf, ex_color, (sx - 1, head_y - 12, 2, 2))

        # 血条
        if self.hp < self.max_hp:
            bw = self.w + 6
            pygame.draw.rect(surf, (34, 34, 34),
                             (sx - bw // 2, sy - self.h // 2 - 9, bw, 4))
            hp_ratio = self.hp / self.max_hp
            hp_col = (255, 0, 0) if hp_ratio > 0.3 else (255, 100, 0)
            pygame.draw.rect(surf, hp_col,
                             (sx - bw // 2, sy - self.h // 2 - 9, int(bw * hp_ratio), 4))

    def _skin_color(self):
        if self.type == 'soldier':
            return (220, 170, 140)
        elif self.type == 'elite':
            return (200, 140, 100)
        elif self.type == 'heavy':
            return (240, 100, 80)
        return (220, 170, 140)

    def _hair_color(self):
        if self.type == 'soldier':
            return (60, 50, 30)
        elif self.type == 'elite':
            return (40, 35, 25)
        elif self.type == 'heavy':
            return (80, 30, 20)
        return (60, 50, 30)