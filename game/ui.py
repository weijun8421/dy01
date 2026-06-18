"""
DY01 - UI系统
菜单、HUD、Buff选择界面
"""
import math
import pygame
from data.config import W, H, TILE, COLOR_GOLD, COLOR_WHITE, COLOR_GRAY
from engine.audio import menu_move as sfx_menu_move, menu_select as sfx_menu_select


class MenuScreen:
    """主菜单 - 重新设计"""

    MENU_ITEMS = [
        ('campaign', '战役模式', '单人闯关，消灭所有敌人进入下一层'),
        ('endless', '无尽模式', '层层推进，敌人越来越强'),
        ('coop', '双人合作', '和朋友一起战斗'),
    ]

    def __init__(self, fonts):
        self.fonts = fonts
        self.selected = 0
        self.star_particles = []
        self.time = 0
        self._init_stars()

    def _init_stars(self):
        import random
        self.star_particles = []
        for _ in range(80):
            self.star_particles.append({
                'x': random.randint(0, W),
                'y': random.randint(0, H),
                'speed': random.uniform(0.2, 1.0),
                'size': random.randint(1, 3),
                'brightness': random.randint(40, 180),
            })

    def update(self):
        self.time += 1
        import random
        for s in self.star_particles:
            s['y'] += s['speed']
            if s['y'] > H:
                s['y'] = -5
                s['x'] = random.randint(0, W)

    def move_up(self):
        self.selected = (self.selected - 1) % len(self.MENU_ITEMS)
        sfx_menu_move()

    def move_down(self):
        self.selected = (self.selected + 1) % len(self.MENU_ITEMS)
        sfx_menu_move()

    def confirm(self):
        sfx_menu_select()
        return self.MENU_ITEMS[self.selected][0]

    def draw(self, renderer):
        # 背景 - 渐变
        surf = renderer.game_surf
        for y in range(H):
            c = int(8 + y / H * 12)
            pygame.draw.line(surf, (c, c, c + 20), (0, y), (W, y))

        # 星星
        for s in self.star_particles:
            alpha = s['brightness']
            pygame.draw.rect(surf, (alpha, alpha, alpha + 40),
                             (int(s['x']), int(s['y']), s['size'], s['size']))

        # 扫描线效果
        scan_y = (self.time * 2) % H
        scan = pygame.Surface((W, 2), pygame.SRCALPHA)
        scan.fill((255, 255, 255, 8))
        surf.blit(scan, (0, scan_y))

        # 标题
        title = self.fonts.render('huge', 'DY01', (255, 51, 51))
        title_glow = self.fonts.render('huge', 'DY01', (255, 0, 0))
        glow_x = W // 2 - title_glow.get_width() // 2 + int(math.sin(self.time * 0.03) * 3)
        glow_a = int(40 + math.sin(self.time * 0.05) * 20)
        title_glow.set_alpha(glow_a)
        surf.blit(title_glow, (glow_x, 100))
        surf.blit(title, (W // 2 - title.get_width() // 2, 100))

        # 副标题
        sub = self.fonts.render('lg', '硬核像素射击', COLOR_GOLD)
        sub_a = int(170 + math.sin(self.time * 0.04) * 50)
        sub.set_alpha(sub_a)
        surf.blit(sub, (W // 2 - sub.get_width() // 2, 180))

        # 分割线
        line_y = 215
        pygame.draw.line(surf, (255, 51, 51, 60), (W // 2 - 200, line_y), (W // 2 + 200, line_y), 1)

        # 菜单项
        for i, (mode_key, name, desc) in enumerate(self.MENU_ITEMS):
            is_sel = i == self.selected
            base_y = 270 + i * 80

            # 高亮背景
            if is_sel:
                bg = pygame.Surface((400, 60), pygame.SRCALPHA)
                bg.fill((255, 51, 51, 25))
                surf.blit(bg, (W // 2 - 200, base_y - 10))
                # 选择指示器
                ind_x = W // 2 - 210 + int(math.sin(self.time * 0.1) * 5)
                ind = self.fonts.render('lg', '>', (255, 51, 51))
                surf.blit(ind, (ind_x, base_y + 5))
                ind2 = self.fonts.render('lg', '<', (255, 51, 51))
                surf.blit(ind2, (W // 2 + 210 - ind2.get_width(), base_y + 5))

            color = COLOR_WHITE if is_sel else COLOR_GRAY
            txt = self.fonts.render('lg', name, color)
            surf.blit(txt, (W // 2 - txt.get_width() // 2, base_y))

            # 描述
            desc_color = (160, 160, 160) if is_sel else (85, 85, 85)
            desc_txt = self.fonts.render('sm', desc, desc_color)
            surf.blit(desc_txt, (W // 2 - desc_txt.get_width() // 2, base_y + 32))

        # 底部操作提示
        hint1 = self.fonts.render('sm', 'W/S 或 上下键 选择   Enter/Space 确认', (100, 100, 100))
        surf.blit(hint1, (W // 2 - hint1.get_width() // 2, H - 60))

        # P1/P2 操控说明
        p1_text = 'P1: WASD移动 | J射击 | K近战 | Space跳跃 | Shift冲刺 | R换弹 | 1-5武器'
        p2_text = 'P2: 方向键移动 | Num1射击 | Num2近战 | Num0跳跃 | Num.冲刺 | 34567武器'
        p1 = self.fonts.render('sm', p1_text, (70, 70, 70))
        p2 = self.fonts.render('sm', p2_text, (70, 70, 70))
        surf.blit(p1, (W // 2 - p1.get_width() // 2, H - 40))
        surf.blit(p2, (W // 2 - p2.get_width() // 2, H - 24))

        # 底部版本
        ver = self.fonts.render('sm', 'pygame-ce · Python', (60, 60, 60))
        surf.blit(ver, (10, H - 16))


class HUD:
    """游戏内HUD"""

    def __init__(self, fonts):
        self.fonts = fonts

    def draw(self, renderer, player, player2, score, kills, wave_label, mode_name):
        surf = renderer.game_surf
        fmgr = self.fonts

        # 左上 - P1 HP
        if player:
            panel_w, panel_h = 200, 56
            s = pygame.Surface((panel_w, panel_h), pygame.SRCALPHA)
            s.fill((0, 0, 0, 170))
            pygame.draw.rect(s, (85, 85, 85), (0, 0, panel_w, panel_h), 1)
            surf.blit(s, (8, 8))

            hp_ratio = player.hp / player.max_hp_actual
            pygame.draw.rect(surf, (34, 34, 34), (16, 14, 184, 12))
            pygame.draw.rect(surf, (255, 51, 51), (16, 14, int(184 * hp_ratio), 12))

            hp_t = fmgr.render('sm', f'HP {int(player.hp)}/{int(player.max_hp_actual)}', (255, 255, 255))
            surf.blit(hp_t, (20, 28))
            kill_t = fmgr.render('sm', f'KILLS {kills}', (255, 255, 255))
            surf.blit(kill_t, (20, 42))

        # 右上 - 分数/波次
        s2 = pygame.Surface((200, 40), pygame.SRCALPHA)
        s2.fill((0, 0, 0, 170))
        pygame.draw.rect(s2, (85, 85, 85), (0, 0, 200, 40), 1)
        surf.blit(s2, (W - 208, 8))

        sc_t = fmgr.render('sm', f'SCORE {score}', (255, 255, 255))
        surf.blit(sc_t, (W - 200, 10))
        wv_t = fmgr.render('sm', f'{wave_label}  {mode_name}', (255, 255, 255))
        surf.blit(wv_t, (W - 200, 28))

        # 底部 - 武器栏
        if player:
            ww, wh = 300, 40
            wx0 = W // 2 - ww // 2
            wy0 = H - 54

            s3 = pygame.Surface((ww, wh), pygame.SRCALPHA)
            s3.fill((0, 0, 0, 220))
            pygame.draw.rect(s3, COLOR_GOLD, (0, 0, ww, wh), 1)
            surf.blit(s3, (wx0, wy0))

            for i in range(5):
                box_x = wx0 + 8 + i * 58
                wp = player.weapons[i]
                sel = player.weapon_idx == i

                if sel:
                    sel_bg = pygame.Surface((52, 30), pygame.SRCALPHA)
                    sel_bg.fill((255, 170, 0, 51))
                    surf.blit(sel_bg, (box_x, wy0 + 5))
                    # 选中指示条
                    pygame.draw.rect(surf, COLOR_GOLD, (box_x, wy0 + 2, 52, 2))

                border_c = COLOR_GOLD if sel else (68, 68, 68)
                pygame.draw.rect(surf, border_c, (box_x, wy0 + 5, 52, 30), 1)

                num_t = fmgr.render('sm', str(i + 1), (255, 255, 255))
                surf.blit(num_t, (box_x + 4, wy0 + 10))
                icon_t = fmgr.render('sm', wp['icon'], (255, 255, 255))
                surf.blit(icon_t, (box_x + 20, wy0 + 8))
                ammo_t = fmgr.get(10).render(str(wp['ammo']), True, (255, 255, 255))
                surf.blit(ammo_t, (box_x + 36, wy0 + 12))

                if sel:
                    name_t = fmgr.render('sm', player.weapon['name'], (255, 255, 255))
                    surf.blit(name_t, (W // 2 - name_t.get_width() // 2, wy0 + 36))

        # P2 HUD
        if player2:
            hp2 = player2.hp / player2.max_hp_actual
            s5 = pygame.Surface((200, 22), pygame.SRCALPHA)
            s5.fill((0, 0, 0, 170))
            pygame.draw.rect(s5, (85, 85, 85), (0, 0, 200, 22), 1)
            surf.blit(s5, (W - 208, H - 90))
            pygame.draw.rect(surf, (34, 34, 34), (W - 200, H - 86, 184, 8))
            pygame.draw.rect(surf, (51, 136, 255), (W - 200, H - 86, int(184 * hp2), 8))
            p2_t = fmgr.render('sm', f'P2 HP {int(player2.hp)}', (255, 255, 255))
            surf.blit(p2_t, (W - 196, H - 88))


class BuffSelectScreen:
    """Buff选择界面 - 卡牌式"""

    def __init__(self, fonts):
        self.fonts = fonts
        self.choices = []
        self.anim = 0
        self.card_anims = [0, 0, 0]

    def set_choices(self, choices):
        self.choices = choices
        self.card_anims = [0, 0, 0]

    def update(self):
        self.anim += 1
        for i in range(3):
            if self.card_anims[i] < 1.0:
                self.card_anims[i] = min(1.0, self.card_anims[i] + 0.06)

    def draw(self, renderer):
        surf = renderer.game_surf
        fmgr = self.fonts
        import math

        # 半透明背景
        bg = pygame.Surface((W, H), pygame.SRCALPHA)
        bg.fill((0, 0, 0, 220))
        surf.blit(bg, (0, 0))

        # 标题
        title = fmgr.render('lg', '选择强化', (255, 51, 51))
        surf.blit(title, (W // 2 - title.get_width() // 2, 40))
        hint = fmgr.render('sm', '按 1 / 2 / 3 选择一张卡牌', (150, 150, 150))
        surf.blit(hint, (W // 2 - hint.get_width() // 2, 75))

        from data.config import TIER_COLORS, TIER_NAMES

        for i in range(min(3, len(self.choices))):
            a = self.card_anims[i]
            c = self.choices[i]
            tc = TIER_COLORS.get(c['tier'], (200, 200, 200))
            tn = TIER_NAMES.get(c['tier'], '普通')

            # 卡片尺寸与位置
            card_w, card_h = 200, 260
            target_x = W // 2 - 310 + i * 210
            bx = target_x
            by = 120 + int((1 - a) * 40)

            # 卡片阴影
            shadow_s = pygame.Surface((card_w + 6, card_h + 6), pygame.SRCALPHA)
            shadow_s.fill((0, 0, 0, int(80 * a)))
            surf.blit(shadow_s, (bx - 3, by + 3))

            # 卡片背景
            card_s = pygame.Surface((card_w, card_h), pygame.SRCALPHA)
            card_s.fill((12, 12, 28, int(255 * a)))
            pygame.draw.rect(card_s, tc, (0, 0, card_w, card_h), 2)

            # 稀有度标签
            tag_s = pygame.Surface((60, 20), pygame.SRCALPHA)
            tag_s.fill((*tc, 80))
            card_s.blit(tag_s, (0, 0))
            tier_t = fmgr.render('sm', tn, tc)
            card_s.blit(tier_t, (30 - tier_t.get_width() // 2, 2))

            # 图标
            icon_s = pygame.Surface((60, 60), pygame.SRCALPHA)
            pygame.draw.rect(icon_s, (*tc, 40), (0, 0, 60, 60), 2)
            icon_t = fmgr.render('lg', c['icon'], (255, 255, 255))
            icon_t.set_alpha(int(255 * a))
            card_s.blit(icon_s, (card_w // 2 - 30, 35))
            card_s.blit(icon_t, (card_w // 2 - icon_t.get_width() // 2, 45))

            # 名称
            name_t = fmgr.render('md', c['name'], (255, 255, 255))
            name_t.set_alpha(int(255 * a))
            card_s.blit(name_t, (card_w // 2 - name_t.get_width() // 2, 110))

            # 武器专属标记
            if c.get('weapon'):
                wp_label = fmgr.render('sm', f'[武器专属]', tc)
                wp_label.set_alpha(int(255 * a))
                card_s.blit(wp_label, (card_w // 2 - wp_label.get_width() // 2, 138))

            # 描述
            desc_t = fmgr.render('sm', c['desc'], (180, 180, 180))
            desc_t.set_alpha(int(255 * a))
            card_s.blit(desc_t, (card_w // 2 - desc_t.get_width() // 2, 170))

            # 按键提示
            key_bg = pygame.Surface((30, 24), pygame.SRCALPHA)
            key_bg.fill((*tc, 120))
            card_s.blit(key_bg, (card_w // 2 - 15, card_h - 40))
            key_t = fmgr.render('sm', f'{i + 1}', (255, 255, 255))
            key_t.set_alpha(int(255 * a))
            card_s.blit(key_t, (card_w // 2 - key_t.get_width() // 2, card_h - 38))

            surf.blit(card_s, (bx, by))

            # 选中提示脉冲
            pulse = abs(math.sin(self.anim * 0.06))
            pygame.draw.rect(surf, (*tc, int(60 * pulse)), (bx, by, card_w, card_h), 2)


class OverlayScreen:
    """暂停 / 结束 / 胜利 覆盖层"""

    @staticmethod
    def draw_paused(renderer):
        surf = renderer.game_surf
        bg = pygame.Surface((W, H), pygame.SRCALPHA)
        bg.fill((0, 0, 0, 187))
        surf.blit(bg, (0, 0))

        t = renderer.fonts.render('lg', 'PAUSED', (255, 255, 255))
        surf.blit(t, (W // 2 - t.get_width() // 2, H // 2 - 60))

        t2 = renderer.fonts.render('md', 'ESC  继续', (200, 200, 200))
        surf.blit(t2, (W // 2 - t2.get_width() // 2, H // 2))

        t3 = renderer.fonts.render('md', 'M  返回主菜单', (200, 200, 200))
        surf.blit(t3, (W // 2 - t3.get_width() // 2, H // 2 + 30))

    @staticmethod
    def draw_gameover(renderer, kills, score, mode='campaign'):
        surf = renderer.game_surf
        bg = pygame.Surface((W, H), pygame.SRCALPHA)
        bg.fill((0, 0, 0, 238))
        surf.blit(bg, (0, 0))

        t = renderer.fonts.render('lg', '任务失败', (255, 50, 50))
        surf.blit(t, (W // 2 - t.get_width() // 2, H // 2 - 80))

        t2 = renderer.fonts.render('md', f'击杀: {kills}  得分: {score}', (255, 170, 0))
        surf.blit(t2, (W // 2 - t2.get_width() // 2, H // 2 - 20))

        t3 = renderer.fonts.render('md', 'Enter  重试', (200, 200, 200))
        surf.blit(t3, (W // 2 - t3.get_width() // 2, H // 2 + 20))

        t4 = renderer.fonts.render('md', 'ESC  返回主菜单', (200, 200, 200))
        surf.blit(t4, (W // 2 - t4.get_width() // 2, H // 2 + 50))

    @staticmethod
    def draw_victory(renderer, map_name, kills, score):
        """通关胜利画面"""
        surf = renderer.game_surf
        bg = pygame.Surface((W, H), pygame.SRCALPHA)
        bg.fill((0, 0, 0, 238))
        surf.blit(bg, (0, 0))

        t = renderer.fonts.render('lg', f'{map_name} 通关!', (0, 255, 100))
        surf.blit(t, (W // 2 - t.get_width() // 2, H // 2 - 100))

        t2 = renderer.fonts.render('md', f'击杀: {kills}  得分: {score}', (255, 255, 0))
        surf.blit(t2, (W // 2 - t2.get_width() // 2, H // 2 - 40))

        t3 = renderer.fonts.render('md', 'Enter  继续下一地图', (200, 200, 200))
        surf.blit(t3, (W // 2 - t3.get_width() // 2, H // 2 + 20))

        t4 = renderer.fonts.render('sm', 'ESC  返回主菜单', (180, 180, 180))
        surf.blit(t4, (W // 2 - t4.get_width() // 2, H // 2 + 55))