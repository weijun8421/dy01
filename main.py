"""
DY01 - 硬核像素射击游戏
入口文件
"""
import sys
import math
import random
import pygame

from data.config import W, H, TILE, FPS, WEAPONS
from engine.audio import init as audio_init
from engine.input import Input
from engine.physics import Physics
from engine.renderer import Renderer
from engine.particles import ParticleSystem

from entities.player import Player
from entities.enemy import Enemy
from entities.bullet import Bullet
from entities.explosion import Explosion

from game.level import Level, MAP_ORDER
from game.buffs import BuffSystem
from game.ui import MenuScreen, HUD, BuffSelectScreen, OverlayScreen


class Game:
    """游戏主控制器"""

    def __init__(self):
        self.screen = pygame.display.set_mode((W, H))
        pygame.display.set_caption("DY01")
        self.clock = pygame.time.Clock()

        # 子系统
        self.renderer = Renderer(self.screen)
        self.input = Input()
        self.particles = ParticleSystem()
        self.physics = Physics()

        # UI
        self.menu = MenuScreen(self.renderer.fonts)
        self.hud = HUD(self.renderer.fonts)
        self.buff_screen = BuffSelectScreen(self.renderer.fonts)

        # 游戏状态
        self.state = 'menu'
        self.mode = 'campaign'
        self.player = None
        self.player2 = None
        self.level = None
        self.enemies = []
        self.bullets = []
        self.explosions = []
        self.barrel_explosions = []
        self.score = 0
        self.kills = 0
        self.level_num = 1
        self.wave_num = 1
        self.wave_remain = 0
        self.wave_done = False
        self.hitstop = 0
        self.buff_choices = []

        # 关卡模式：地图系统
        self.map_idx = 0
        self.map_level = 1      # 当前地图内关卡(1-5)
        self.MAP_LEVELS = 5     # 每张地图5关
        self._first_buff = True

    # ─── 游戏流程 ────────────────────────────────

    def _map_name(self):
        if self.map_idx < len(MAP_ORDER):
            from game.level import MAP_THEMES
            return MAP_THEMES[MAP_ORDER[self.map_idx]]['name']
        return '??'

    def begin(self):
        self.state = 'playing'
        self.score = max(0, self.score)
        self.wave_done = False

        self.level = Level()
        self.level.generate(self.level_num, self.map_idx)

        if self.player:
            self.player.x = 80
            self.player.y = 300
            self.player.vx = self.player.vy = 0
            self.player.dead = False
            self.player.respawn_timer = 0
            self.player.hp = self.player.max_hp_actual
        else:
            self.player = Player(80, 300, False)

        if self.mode == 'coop':
            if self.player2:
                self.player2.x = 120
                self.player2.y = 300
                self.player2.vx = self.player2.vy = 0
                self.player2.dead = False
                self.player2.respawn_timer = 0
                self.player2.hp = self.player2.max_hp_actual
            else:
                self.player2 = Player(120, 300, True)
        else:
            self.player2 = None

        self.enemies = []
        self.bullets = []
        self.particles.clear()
        self.explosions = []
        self.barrel_explosions = []
        self.renderer.camera.reset()
        self.hitstop = 0
        self._spawn_wave()

    def _spawn_wave(self):
        if self.mode == 'endless':
            self.wave_remain = 6 + self.wave_num * 3
        else:
            self.wave_remain = 10 + self.level_num * 5
        self.wave_done = False
        self._spawn_enemies(min(5, self.wave_remain))
        # 战役模式每张地图最后一关出Boss
        if self.mode == 'campaign' and self.map_level == self.MAP_LEVELS:
            self._spawn_boss()

    def _spawn_enemies(self, count):
        gy = (self.level.h - 4) * TILE
        for _ in range(count):
            if self.wave_remain <= 0:
                break
            ex = random.uniform(300, (self.level.w - 5) * TILE)
            ey = random.uniform(100, gy - 30)
            etype = 'soldier'
            roll = random.random()
            if self.mode == 'endless' and self.wave_num > 3:
                if roll < 0.08:
                    etype = 'heavy'
                elif roll < 0.28:
                    etype = 'elite'
                elif roll < 0.42:
                    etype = 'flyer'
            elif self.map_idx > 0 and self.map_level > 2:
                if roll < 0.15:
                    etype = 'elite'
                elif roll < 0.28:
                    etype = 'flyer'
            if self.mode == 'endless' and self.wave_num % 5 == 0 and self.wave_num >= 5:
                if len(self.enemies) == 0:
                    etype = 'boss'
            self.enemies.append(Enemy(ex, ey, etype))
            self.wave_remain -= 1

    def _spawn_boss(self):
        """在出口附近生成Boss"""
        ex = self.level.exitX - 150
        ey = self.level.exitY - 20
        self.enemies.append(Enemy(ex, ey, 'boss'))
        # Boss额外血量
        self.enemies[-1].hp *= 1.5

    def _show_buffs(self):
        self.state = 'buff'
        self.buff_choices = BuffSystem.get_choices(self.level_num, self.player)
        self.buff_screen.set_choices(self.buff_choices)

    def _select_buff(self, idx):
        buff = self.buff_choices[idx]
        # 首次选择：先创建玩家
        if self.player is None:
            self.player = Player(80, 300, False)
            if self.mode == 'coop':
                self.player2 = Player(120, 300, True)

        targets = [self.player]
        if self.player2:
            targets.append(self.player2)
        for t in targets:
            BuffSystem.apply(t, buff)

        # 非首次才递增关卡
        if not self._first_buff:
            if self.mode == 'campaign':
                self.map_level += 1
                if self.map_level > self.MAP_LEVELS:
                    # 一张地图通关 → 检查是否所有地图都完成
                    if self.map_idx >= len(MAP_ORDER) - 1:
                        # 全部通关！
                        self.state = 'victory'
                        return
                    self.map_idx = (self.map_idx + 1) % len(MAP_ORDER)
                    self.map_level = 1
                self.level_num += 1
            else:
                self.wave_num += 1
        self._first_buff = False

        self.begin()

    # ─── 更新 ────────────────────────────────────

    def update(self):
        if self.state != 'playing':
            return

        if self.hitstop > 0:
            self.hitstop -= 1
            return

        # 检查死亡
        p1_dead = self.player.dead and self.player.respawn_timer <= 0
        p2_dead = not self.player2 or (self.player2.dead and self.player2.respawn_timer <= 0)
        if self.mode == 'coop':
            if p1_dead and p2_dead:
                self.state = 'gameover'
                return
        elif p1_dead:
            self.state = 'gameover'
            return

        # 更新玩家
        p1_weps = self.input.get_weapon_switch(0)
        self.player.update(self.input.keys, self.level, self.bullets, self.particles,
                          self.renderer.camera)
        for wi in p1_weps:
            self.player.switch_weapon(wi)

        if self.player2:
            p2_weps = self.input.get_weapon_switch(1)
            self.player2.update(self.input.keys, self.level, self.bullets, self.particles,
                               self.renderer.camera)
            for wi in p2_weps:
                self.player2.switch_weapon(wi)

        # 相机
        tx = self.player.x
        if self.player2 and not self.player2.dead:
            if not self.player.dead:
                tx = (self.player.x + self.player2.x) / 2
            else:
                tx = self.player2.x
        self.renderer.camera.follow(tx)
        self.renderer.camera.clamp(0, self.level.w * TILE - W)
        self.renderer.camera.update()

        # 更新子弹
        self._update_bullets()

        # 更新敌人
        self.enemies = [e for e in self.enemies
                        if e.update(self.player, self.player2, self.level, self.particles,
                                     self.renderer.camera) and e.hp > 0]
        if self.wave_remain > 0 and len(self.enemies) < 7:
            self._spawn_enemies(min(3, self.wave_remain))

        # ── 自动哨戒（turret buff）──
        self._update_turret()

        # 更新粒子和爆炸
        self.particles.update()
        self.explosions = [e for e in self.explosions if e.update()]
        # 爆炸对敌人造成伤害
        self._apply_explosion_damage()

        # 延迟爆炸（油桶）
        for be in self.barrel_explosions[:]:
            be['timer'] -= 1
            if be['timer'] <= 0:
                self.explosions.append(Explosion(be['x'], be['y'], be['radius'], be['damage']))
                self.level.explode(be['x'], be['y'], be['radius'], be['damage'],
                                   self.particles, self.explosions, self.barrel_explosions)
                from engine.audio import explode as sfx_explode
                sfx_explode()
                self.renderer.camera.add_shake(14)
                self.barrel_explosions.remove(be)

        # 检查波次完成
        alive = len(self.enemies)
        if alive == 0 and self.wave_remain <= 0 and not self.wave_done:
            self.wave_done = True
            if self.mode == 'endless':
                self._show_buffs()

        # 闯关模式：走向出口触发
        if self.mode == 'campaign' and self.wave_done and alive == 0 and self.wave_remain <= 0:
            if self.player and not self.player.dead:
                dist = abs(self.player.x - self.level.exitX)
                if dist < 80:
                    self._show_buffs()

    def _update_bullets(self):
        new_bullets = []
        for b in self.bullets:
            if not b.update():
                continue

            hit_something = False
            for e in self.enemies:
                if e in b.hits:
                    continue
                if math.hypot(b.x - e.x, b.y - e.y) < e.w / 2 + b.weapon['bulletW'] + 2:
                    b.hits.add(e)
                    killed = e.hit(b.weapon['damage'], self.particles)
                    e.apply_effects(b.weapon)  # 应用特殊效果
                    # 暴击/致命一击视觉
                    if b.weapon.get('is_death_blow'):
                        self.particles.spawn_text(e.x, e.y - 20, 'DEATH!', (255, 0, 0), 22)
                    elif b.weapon.get('is_crit'):
                        self.particles.spawn_text(e.x, e.y - 20, f'CRIT! {int(b.weapon["damage"])}', (255, 255, 0), 18)
                    if killed:
                        self.kills += 1
                        self.score += e.score
                        owner = self.player if b.owner_id == 'p1' else (self.player2 if b.owner_id == 'p2' else None)
                        if owner and owner.buff_vampire > 0:
                            owner.heal(owner.buff_vampire)
                        self.renderer.camera.add_shake(4)
                        self.hitstop = 3
                    # 连锁闪电
                    if b.weapon.get('chain') and not killed:
                        chained = 0
                        for e2 in self.enemies:
                            if e2 is e or e2 in b.hits:
                                continue
                            if math.hypot(e.x - e2.x, e.y - e2.y) < 120:
                                killed2 = e2.hit(b.weapon['damage'] * 0.5, self.particles)
                                self.particles.spawn_burst(
                                    e2.x, e2.y, 4, 3,
                                    (100, 200, 255), (5, 12), (2, 4),
                                    vy_range=(-2, 1)
                                )
                                if killed2:
                                    self.kills += 1
                                    self.score += e2.score
                                chained += 1
                                if chained >= 2:
                                    break
                    if b.weapon.get('explosive'):
                        owner = self.player if b.owner_id == 'p1' else (self.player2 if b.owner_id == 'p2' else None)
                        r = b.weapon['explosionRadius'] * (owner.buff_explosion if owner else 1)
                        dmg = b.weapon['damage'] * (2 if (owner and owner.buff_nuke) else 1)
                        self.explosions.append(Explosion(b.x, b.y, r, dmg))
                        self.level.explode(b.x, b.y, r, 3, self.particles, self.explosions, self.barrel_explosions)
                        from engine.audio import explode as sfx_explode
                        sfx_explode()
                        self.renderer.camera.add_shake(14)
                        hit_something = True
                        break
                    owner = self.player if b.owner_id == 'p1' else (self.player2 if b.owner_id == 'p2' else None)
                    if not owner or not owner.buff_pierce:
                        hit_something = True
                        break

            if hit_something:
                continue

            # 瓦片碰撞
            tx, ty = Physics.aabb_vs_tiles(b.x, b.y, 4, 4, self.level)
            if tx is not None:
                self.level.damage_tile(tx, ty, 1, self.particles)
                self.particles.spawn_burst(
                    b.x, b.y, 3, 2,
                    (136, 136, 136), (5, 12), (1, 3),
                    vy_range=(-2, 1)
                )
                if b.weapon.get('explosive'):
                    owner = self.player if b.owner_id == 'p1' else (self.player2 if b.owner_id == 'p2' else None)
                    r = b.weapon['explosionRadius'] * (owner.buff_explosion if owner else 1)
                    self.explosions.append(Explosion(b.x, b.y, r, b.weapon['damage']))
                    self.level.explode(b.x, b.y, r, 3, self.particles, self.explosions, self.barrel_explosions)
                    from engine.audio import explode as sfx_explode
                    sfx_explode()
                    self.renderer.camera.add_shake(14)
                continue

            new_bullets.append(b)
        self.bullets = new_bullets

    def _apply_explosion_damage(self):
        """对所有活跃爆炸检查敌人碰撞，造成范围伤害"""
        for ex in self.explosions:
            if ex.life != ex.max_life - 1:  # 只在爆炸第一帧造成伤害
                continue
            for e in self.enemies:
                dist = math.hypot(ex.x - e.x, ex.y - e.y)
                if dist < ex.radius:
                    killed = e.hit(ex.damage, self.particles)
                    if killed:
                        self.kills += 1
                        self.score += e.score
                        owner = self.player
                        if owner and owner.buff_vampire > 0:
                            owner.heal(owner.buff_vampire)

    def _update_turret(self):
        """自动哨戒：自动向最近敌人射击"""
        if not self.player or self.player.dead:
            return
        if not self.player.buff_turret:
            return
        self._turret_timer = getattr(self, '_turret_timer', 0) + 1
        if self._turret_timer < 30:  # 每30帧射击一次
            return
        self._turret_timer = 0
        if not self.enemies:
            return

        # 找最近的敌人
        nearest = None
        nearest_dist = float('inf')
        for e in self.enemies:
            d = math.hypot(self.player.x - e.x, self.player.y - e.y)
            if d < nearest_dist:
                nearest_dist = d
                nearest = e

        if nearest and nearest_dist < 500:
            # 自动射击
            from entities.bullet import Bullet
            angle = math.atan2(nearest.y - self.player.y, nearest.x - self.player.x)
            w = self.player.weapon
            dmg = w['damage'] * self.player.buff_dmg * 0.6  # 哨戒伤害60%
            owner_id = 'p2' if self.player.is_p2 else 'p1'
            self.bullets.append(Bullet(
                self.player.x, self.player.y - 3,
                math.cos(angle) * w['bulletSpeed'],
                math.sin(angle) * w['bulletSpeed'],
                {**w, 'damage': dmg, 'is_turret': True},
                owner_id,
            ))
            from engine.audio import shoot as sfx_shoot
            sfx_shoot('rifle')

    # ─── 绘制 ────────────────────────────────────

    def draw(self):
        self.renderer.begin_frame()

        if self.state == 'menu':
            self.menu.draw(self.renderer)

        elif self.state in ('playing', 'buff', 'paused', 'gameover', 'victory'):
            # 背景
            if self.level:
                th = self.level.theme
                self.renderer.draw_background(self.renderer.camera.offset_x, th)
                self.renderer.draw_level(self.level)

            # 子弹
            for b in self.bullets:
                b.draw(self.renderer.game_surf, self.renderer.camera.offset_x)

            # 敌人
            for e in self.enemies:
                e.draw(self.renderer.game_surf, self.renderer.camera.offset_x)

            # 爆炸
            for ex in self.explosions:
                ex.draw(self.renderer.game_surf, self.renderer.camera.offset_x)

            # 玩家
            if self.player:
                self.player.draw(self.renderer.game_surf, self.renderer.camera.offset_x)
            if self.player2:
                self.player2.draw(self.renderer.game_surf, self.renderer.camera.offset_x)

            # 粒子
            self.particles.draw(self.renderer.game_surf, self.renderer.camera.offset_x, self.renderer.fonts)

            # 准星
            if self.state == 'playing':
                self._draw_crosshairs()

            # HUD
            if self.state in ('playing', 'buff'):
                if self.mode == 'campaign':
                    wave_label = f'{self._map_name()} {self.map_level}/{self.MAP_LEVELS}'
                else:
                    wave_label = f'WAVE {self.wave_num}'
                self.hud.draw(self.renderer, self.player, self.player2,
                              self.score, self.kills, wave_label, self.mode.upper())

            # 波次完成提示 → 前往出口
            if self.state == 'playing' and self.wave_done and self.mode == 'campaign':
                if self.player and not self.player.dead:
                    dist = abs(self.player.x - self.level.exitX)
                    if dist < 200:
                        # 距离提示
                        hint = f'>>>  EXIT  <<<  ({int(dist)}px)'
                    else:
                        hint = '>>>  向右前往出口  >>>'
                    t = self.renderer.fonts.render('lg', hint, (0, 255, 0))
                    tw = t.get_width()
                    # 闪烁效果
                    if (pygame.time.get_ticks() // 500) % 2 == 0:
                        self.renderer.game_surf.blit(t, (W // 2 - tw // 2, H - 20))

            # 覆盖层
            if self.state == 'paused':
                OverlayScreen.draw_paused(self.renderer)
            elif self.state == 'gameover':
                OverlayScreen.draw_gameover(self.renderer, self.kills, self.score)
            elif self.state == 'victory':
                OverlayScreen.draw_victory(self.renderer, self._map_name(), self.kills, self.score)
            elif self.state == 'buff':
                self.buff_screen.update()
                self.buff_screen.draw(self.renderer)

        self.renderer.end_frame()
        pygame.display.flip()

    def _draw_crosshairs(self):
        for pl in [self.player, self.player2]:
            if pl and not pl.dead:
                cx = int(pl.x - self.renderer.camera.offset_x + pl.facing * 40)
                cy = int(pl.y - 2)
                cc = (255, 0, 0, 136) if not pl.is_p2 else (68, 136, 255, 136)
                s = pygame.Surface((20, 20), pygame.SRCALPHA)
                pygame.draw.line(s, cc, (2, 10), (18, 10), 2)
                pygame.draw.line(s, cc, (10, 2), (10, 18), 2)
                pygame.draw.circle(s, cc, (10, 10), 4, 2)
                self.renderer.game_surf.blit(s, (cx - 10, cy - 10))

    # ─── 主循环 ──────────────────────────────────

    def run(self):
        running = True
        while running:
            self.clock.tick(FPS)

            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    running = False
                self.input.handle_event(event)

            self.input.update()

            # ── 状态机 ──
            if self.state == 'menu':
                self.menu.update()
                if self.input.pressed('up'):
                    self.menu.move_up()
                elif self.input.pressed('down'):
                    self.menu.move_down()
                elif self.input.pressed('enter'):
                    mode = self.menu.confirm()
                    self.mode = mode
                    self.level_num = 1
                    self.wave_num = 1
                    self.map_idx = 0
                    self.map_level = 1
                    self._first_buff = True

                    # 开局先选一个Buff
                    self._show_buffs()

            elif self.state == 'playing':
                if self.input.pressed('escape'):
                    self.state = 'paused'
                self.update()

            elif self.state == 'paused':
                if self.input.pressed('escape'):
                    self.state = 'playing'
                elif self.input.pressed('key_m'):
                    self.player = None
                    self.player2 = None
                    self.state = 'menu'

            elif self.state == 'victory':
                if self.input.pressed('enter'):
                    # 继续下一地图
                    self.map_idx = (self.map_idx + 1) % len(MAP_ORDER)
                    self.map_level = 1
                    self.level_num += 1
                    self._show_buffs()
                elif self.input.pressed('escape'):
                    self.player = None
                    self.player2 = None
                    self.state = 'menu'

            elif self.state == 'gameover':
                if self.input.pressed('enter'):
                    if self.mode == 'campaign':
                        self.level_num = 1
                        self.wave_num = 1
                        self.map_idx = 0
                        self.map_level = 1
                    self.player = None
                    self.player2 = None
                    self._first_buff = True
                    self._show_buffs()
                elif self.input.pressed('escape'):
                    self.player = None
                    self.player2 = None
                    self.state = 'menu'

            elif self.state == 'buff':
                if self.input.pressed('key_1'):
                    self._select_buff(0)
                elif self.input.pressed('key_2'):
                    self._select_buff(1)
                elif self.input.pressed('key_3'):
                    self._select_buff(2)

            self.draw()

        pygame.quit()
        sys.exit()


def main():
    pygame.init()
    audio_init()
    game = Game()
    game.run()


if __name__ == '__main__':
    main()