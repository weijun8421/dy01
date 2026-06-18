"""
DY01 - 渲染器
相机系统、屏幕震动、字体管理、HUD绘制
"""
import math
import pygame
import random
from data.config import W, H, TILE


class FontManager:
    """中文字体管理器"""

    _CHINESE_FONTS = [
        "C:/Windows/Fonts/msyh.ttc",
        "C:/Windows/Fonts/simhei.ttf",
        "C:/Windows/Fonts/msyhbd.ttc",
        "C:/Windows/Fonts/simsun.ttc",
        "C:/Windows/Fonts/simkai.ttf",
    ]

    def __init__(self):
        self._cache = {}
        self._font_path = None
        self._init_font()

    def _init_font(self):
        for path in self._CHINESE_FONTS:
            try:
                import os
                if os.path.exists(path):
                    self._font_path = path
                    break
            except Exception:
                continue

        if self._font_path:
            self._cache['sm'] = pygame.font.Font(self._font_path, 14)
            self._cache['md'] = pygame.font.Font(self._font_path, 18)
            self._cache['lg'] = pygame.font.Font(self._font_path, 28)
            self._cache['xl'] = pygame.font.Font(self._font_path, 64)
            self._cache['title'] = pygame.font.Font(self._font_path, 48)
            self._cache['huge'] = pygame.font.Font(self._font_path, 80)
        else:
            self._cache['sm'] = pygame.font.SysFont("monospace", 12, bold=True)
            self._cache['md'] = pygame.font.SysFont("monospace", 16, bold=True)
            self._cache['lg'] = pygame.font.SysFont("monospace", 24, bold=True)
            self._cache['xl'] = pygame.font.SysFont("monospace", 56, bold=True)
            self._cache['title'] = pygame.font.SysFont("monospace", 42, bold=True)
            self._cache['huge'] = pygame.font.SysFont("monospace", 64, bold=True)

    def get(self, size_or_name):
        if size_or_name in self._cache:
            return self._cache[size_or_name]
        if isinstance(size_or_name, int):
            try:
                return pygame.font.Font(self._font_path, size_or_name) if self._font_path else pygame.font.Font(None, size_or_name)
            except Exception:
                return pygame.font.Font(None, size_or_name)
        return self._cache.get('md', pygame.font.Font(None, 18))

    def render(self, name, text, color, antialias=True):
        font = self.get(name)
        return font.render(text, antialias, color)


class Camera:
    """相机系统，带平滑跟随和屏幕震动"""

    def __init__(self):
        self.x = 0.0
        self.target_x = 0.0
        self.shake_amount = 0.0
        self.shake_x = 0.0
        self.shake_y = 0.0

    def reset(self):
        self.x = 0.0
        self.target_x = 0.0
        self.shake_amount = 0.0
        self.shake_x = 0.0
        self.shake_y = 0.0

    def follow(self, target_x, smooth=0.07):
        self.target_x = target_x - W / 2
        self.x += (self.target_x - self.x) * smooth

    def clamp(self, min_x, max_x):
        if self.x < min_x:
            self.x = min_x
        if self.x > max_x:
            self.x = max_x

    def add_shake(self, amount):
        self.shake_amount = max(self.shake_amount, amount)

    def update(self):
        if self.shake_amount > 0:
            self.shake_x = random.uniform(-self.shake_amount, self.shake_amount)
            self.shake_y = random.uniform(-self.shake_amount, self.shake_amount)
            self.shake_amount *= 0.88
            if self.shake_amount < 0.4:
                self.shake_amount = 0
                self.shake_x = 0
                self.shake_y = 0
        else:
            self.shake_x = 0
            self.shake_y = 0

    @property
    def offset_x(self):
        return self.x + self.shake_x

    @property
    def offset_y(self):
        return self.shake_y


class TileTextures:
    """Minecraft风格方块纹理（程序化生成，16x16像素）"""

    _cache = {}

    @classmethod
    def _noise(cls, base, r_var, g_var, b_var, seed=0):
        """给颜色加随机噪点"""
        r = max(0, min(255, base[0] + (hash(str(seed)) % (r_var * 2 + 1)) - r_var))
        g = max(0, min(255, base[1] + (hash(str(seed + 1)) % (g_var * 2 + 1)) - g_var))
        b = max(0, min(255, base[2] + (hash(str(seed + 2)) % (b_var * 2 + 1)) - b_var))
        return (r, g, b)

    @classmethod
    def get_grass(cls, theme):
        """草地方块：顶部绿色草皮 + 侧面泥土带草边"""
        key = ('grass', theme['name'])
        if key in cls._cache:
            return cls._cache[key]

        surf = pygame.Surface((16, 16))
        dirt = theme.get('ground', (101, 67, 33))
        grass_top = theme.get('ground_top', (74, 124, 89))
        grass_dark = (max(0, grass_top[0] - 20), max(0, grass_top[1] - 20), max(0, grass_top[2] - 15))

        for row in range(16):
            for col in range(16):
                seed = row * 16 + col
                if row < 4:
                    # 草皮层 - 绿色带噪点
                    c = cls._noise(grass_top, 12, 15, 10, seed)
                    # 偶尔加一点深绿
                    if seed % 7 == 0:
                        c = cls._noise(grass_dark, 5, 8, 5, seed + 100)
                elif row == 4:
                    # 草皮边缘 - 深绿过渡
                    c = cls._noise(grass_dark, 8, 10, 5, seed)
                else:
                    # 泥土层 - 棕色带石粒噪点
                    c = cls._noise(dirt, 15, 12, 15, seed)
                    # 随机小石子
                    if seed % 11 == 0:
                        c = cls._noise((140, 110, 80), 10, 10, 10, seed)
                    elif seed % 17 == 0:
                        c = cls._noise((80, 55, 25), 8, 8, 8, seed)

                surf.set_at((col, row), c)

        cls._cache[key] = surf
        return surf

    @classmethod
    def get_dirt(cls, theme):
        """泥土方块：纯泥土带石粒纹理"""
        key = ('dirt', theme['name'])
        if key in cls._cache:
            return cls._cache[key]

        surf = pygame.Surface((16, 16))
        dirt = theme.get('ground', (101, 67, 33))

        for row in range(16):
            for col in range(16):
                seed = row * 16 + col
                c = cls._noise(dirt, 18, 15, 18, seed)
                # 小石子
                if seed % 9 == 0:
                    c = cls._noise((130, 100, 70), 12, 12, 12, seed + 50)
                elif seed % 13 == 0:
                    c = cls._noise((75, 50, 22), 10, 10, 10, seed + 80)

                surf.set_at((col, row), c)

        cls._cache[key] = surf
        return surf

    @classmethod
    def get_stone(cls, theme):
        """石头/墙壁方块：灰色石材质感"""
        key = ('stone', theme['name'])
        if key in cls._cache:
            return cls._cache[key]

        surf = pygame.Surface((16, 16))
        stone = theme.get('wall', (100, 100, 100))

        for row in range(16):
            for col in range(16):
                seed = row * 16 + col
                c = cls._noise(stone, 20, 20, 20, seed)
                # 裂缝/纹理
                if seed % 8 == 0:
                    c = cls._noise((stone[0] - 25, stone[1] - 25, stone[2] - 25), 8, 8, 8, seed)
                elif seed % 14 == 0:
                    c = cls._noise((stone[0] + 25, stone[1] + 25, stone[2] + 25), 8, 8, 8, seed)
                # 砖缝线
                if row in (7, 8) and col % 4 == 0:
                    c = cls._noise((stone[0] - 30, stone[1] - 30, stone[2] - 30), 5, 5, 5, seed)
                if col in (3, 7, 11, 15):
                    c = cls._noise((stone[0] - 18, stone[1] - 18, stone[2] - 18), 5, 5, 5, seed + 200)

                surf.set_at((col, row), c)

        cls._cache[key] = surf
        return surf

    @classmethod
    def get_plank(cls, theme):
        """木板/平台方块：木质纹理"""
        key = ('plank', theme['name'])
        if key in cls._cache:
            return cls._cache[key]

        surf = pygame.Surface((16, 16))
        wood = theme.get('plat', (139, 90, 43))

        for row in range(16):
            for col in range(16):
                seed = row * 16 + col
                # 水平木纹
                grain = int((seed % 5) * 0.6)
                c = cls._noise(wood, 12 + grain, 8 + grain, 8, seed)
                # 年轮线
                if row in (4, 8, 12):
                    c = cls._noise((wood[0] - 18, wood[1] - 12, wood[2] - 8), 6, 4, 3, seed + 100)
                # 木节
                if seed % 23 == 0:
                    c = cls._noise((wood[0] - 25, wood[1] - 20, wood[2] - 15), 5, 5, 5, seed)

                surf.set_at((col, row), c)

        cls._cache[key] = surf
        return surf

    @classmethod
    def clear_cache(cls):
        cls._cache.clear()


class Renderer:
    """渲染管理器"""

    def __init__(self, screen):
        self.screen = screen
        self.fonts = FontManager()
        self.camera = Camera()
        self.game_surf = pygame.Surface((W, H), pygame.SRCALPHA)
        self._tex_last_theme = None

    def begin_frame(self):
        self.game_surf.fill((0, 0, 0, 0))

    def end_frame(self):
        if self.camera.shake_amount > 0:
            self.screen.blit(self.game_surf,
                             (int(self.camera.shake_x), int(self.camera.shake_y)))
        else:
            self.screen.blit(self.game_surf, (0, 0))

    def draw_rect(self, color, rect):
        pygame.draw.rect(self.game_surf, color, rect)

    def draw_circle(self, color, center, radius, width=0):
        pygame.draw.circle(self.game_surf, color, center, radius, width)

    def draw_line(self, color, start, end, width=1):
        pygame.draw.line(self.game_surf, color, start, end, width)

    def draw_polygon(self, color, points):
        if len(points) >= 3:
            pygame.draw.polygon(self.game_surf, color, points)

    def blit(self, surf, pos):
        self.game_surf.blit(surf, pos)

    def fill(self, color):
        self.game_surf.fill(color)

    def text(self, font_name, text, color, pos, center_x=False):
        surf = self.fonts.render(font_name, text, color)
        if center_x:
            pos = (pos[0] - surf.get_width() // 2, pos[1])
        self.game_surf.blit(surf, pos)
        return surf

    def draw_level(self, level):
        """绘制关卡地形（使用Minecraft风格纹理）"""
        th = level.theme
        cam_x = int(self.camera.offset_x)
        sc = int(cam_x / TILE)
        ec = sc + (W // TILE) + 2

        tex_grass = TileTextures.get_grass(th)
        tex_dirt = TileTextures.get_dirt(th)
        tex_stone = TileTextures.get_stone(th)
        tex_plank = TileTextures.get_plank(th)

        for y in range(level.h):
            for x in range(sc, min(ec, level.w)):
                t = level.tiles[y][x]
                if not t['solid']:
                    continue
                px = x * TILE - cam_x
                py = y * TILE

                if t['type'] == 'ground':
                    # 判断上方是否为空气 → 是的话用草皮纹理，否则纯泥土
                    above_air = (y == 0) or (not level.tiles[y - 1][x]['solid'])
                    if above_air:
                        self.game_surf.blit(tex_grass, (px, py))
                    else:
                        self.game_surf.blit(tex_dirt, (px, py))
                    # 损坏状态
                    if t['hp'] < 4:
                        damage_s = pygame.Surface((TILE - 4, TILE - 4), pygame.SRCALPHA)
                        damage_s.fill((255, 0, 0, 50))
                        self.game_surf.blit(damage_s, (px + 2, py + 2))
                elif t['type'] == 'plat':
                    self.game_surf.blit(tex_plank, (px, py))
                elif t['type'] == 'wall':
                    self.game_surf.blit(tex_stone, (px, py))
                    if t['hp'] < 5:
                        damage_s = pygame.Surface((TILE - 8, TILE - 8), pygame.SRCALPHA)
                        damage_s.fill((255, 136, 0, 50))
                        self.game_surf.blit(damage_s, (px + 4, py + 4))

        # 油桶
        for b in level.barrels:
            if not b['ok']:
                continue
            bx = int(b['x'] - cam_x - 8)
            by = int(b['y'] - 8)
            pygame.draw.rect(self.game_surf, (204, 51, 51), (bx, by, 16, 16))
            pygame.draw.rect(self.game_surf, (255, 102, 0), (bx + 4, by + 4, 8, 8))
            pygame.draw.rect(self.game_surf, (255, 255, 0), (bx + 6, by + 2, 4, 4))
            if math.sin(pygame.time.get_ticks() * 0.008) > 0:
                warn_s = pygame.Surface((24, 24), pygame.SRCALPHA)
                pygame.draw.rect(warn_s, (255, 0, 0, 40), (0, 0, 24, 24), 2)
                self.game_surf.blit(warn_s, (bx - 2, by - 2))

        # 出口 - 显眼的绿色光柱
        ex = int(level.exitX - cam_x)
        ey = int(level.exitY)
        t = pygame.time.get_ticks()
        pulse = int(50 + 20 * math.sin(t * 0.004))

        # 底部光柱
        beam = pygame.Surface((TILE, TILE * 4), pygame.SRCALPHA)
        beam.fill((0, 255, 100, pulse))
        self.game_surf.blit(beam, (ex, ey - TILE * 3))

        # 顶部光柱（向上的光）
        beam_top = pygame.Surface((TILE, TILE * 2), pygame.SRCALPHA)
        for i in range(TILE * 2):
            a = max(0, pulse - i * 2)
            pygame.draw.line(beam_top, (0, 255, 100, a), (0, i), (TILE, i))
        self.game_surf.blit(beam_top, (ex, ey - TILE * 5))

        # 门框
        pygame.draw.rect(self.game_surf, (0, 200, 0), (ex, ey - 32, TILE, TILE * 2), 2)
        pygame.draw.rect(self.game_surf, (0, 255, 0), (ex + 2, ey - 30, TILE - 4, TILE * 2 - 4), 1)

        # 箭头（脉冲）
        arrow_s = pygame.Surface((20, 14), pygame.SRCALPHA)
        pygame.draw.polygon(arrow_s, (0, 255, 0, 200), [(10, 0), (0, 14), (20, 14)])
        self.game_surf.blit(arrow_s, (ex - 2, ey - 48))

        # 文字 "EXIT"
        txt = self.fonts.render('sm', 'EXIT', (0, 255, 0))
        self.game_surf.blit(txt, (ex + TILE // 2 - txt.get_width() // 2, ey - 64))

    def draw_background(self, cam_x, theme=None):
        """绘制多层视差背景"""
        if theme is None:
            theme = {'sky': (26, 26, 46), 'bg_hill': (15, 15, 26), 'bg_build': (21, 21, 37)}

        # 天空
        self.game_surf.fill(theme['sky'])

        # 星星（随机但固定位置）
        star_seed = 42
        for i in range(40):
            sx = (hash(str(star_seed + i)) % W)
            sy = (hash(str(star_seed + i + 100)) % 300)
            brightness = 100 + (hash(str(i)) % 156)
            c = (brightness, brightness, brightness)
            self.game_surf.set_at((sx, sy), c)

        # 远景山（最慢视差）
        for i in range(6):
            mx = int((i * 280 - cam_x * 0.03) % 1300) - 150
            points = [(mx, 520), (mx + 140, 360), (mx + 280, 520)]
            pygame.draw.polygon(self.game_surf, theme['bg_hill'], points)

        # 中景楼（中等视差）
        for i in range(10):
            bx = int((i * 170 - cam_x * 0.06) % 1200) - 50
            h = 60 + (hash(str(i)) % 80)
            pygame.draw.rect(self.game_surf, theme['bg_build'], (bx, 450 - h, 28, h))
            pygame.draw.rect(self.game_surf, theme['bg_build'], (bx + 38, 460 - h + 30, 22, h - 30))

        # 近景云（较快视差）
        cloud_color = (255, 255, 255, 30)
        for i in range(5):
            cx = int((i * 350 - cam_x * 0.12) % 1400) - 100
            cy = 80 + (i * 37) % 120
            cw = 60 + (i * 23) % 40
            ch = 20 + (i * 17) % 15
            cloud = pygame.Surface((cw * 2, ch * 2), pygame.SRCALPHA)
            for _ in range(8):
                px = random.randint(0, cw)
                py = random.randint(0, ch)
                pygame.draw.ellipse(cloud, cloud_color, (px, py, cw // 2, ch // 2))
            self.game_surf.blit(cloud, (cx, cy))