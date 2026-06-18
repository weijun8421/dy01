"""
DY01 - 关卡
支持多地图主题（平原/沙地/雪山/火山）
"""
import random
import math
from data.config import TILE


# ─── 地图主题 ──────────────────────────────────────
MAP_THEMES = {
    'plains': {
        'name': '平原', 'sky': (26, 26, 46),
        'ground': (58, 92, 62), 'ground_top': (74, 124, 89),
        'plat': (107, 68, 35), 'plat_top': (139, 101, 51),
        'wall': (85, 85, 85), 'wall_top': (119, 119, 119),
        'bg_hill': (15, 15, 26), 'bg_build': (21, 21, 37),
    },
    'desert': {
        'name': '沙地', 'sky': (46, 46, 26),
        'ground': (170, 140, 60), 'ground_top': (210, 180, 80),
        'plat': (160, 120, 40), 'plat_top': (200, 160, 60),
        'wall': (140, 100, 40), 'wall_top': (180, 140, 60),
        'bg_hill': (30, 30, 15), 'bg_build': (40, 35, 20),
    },
    'snow': {
        'name': '雪山', 'sky': (34, 55, 85),
        'ground': (200, 210, 220), 'ground_top': (230, 240, 250),
        'plat': (170, 180, 190), 'plat_top': (200, 210, 220),
        'wall': (140, 150, 160), 'wall_top': (170, 180, 190),
        'bg_hill': (20, 30, 45), 'bg_build': (25, 35, 50),
    },
    'volcano': {
        'name': '火山', 'sky': (46, 20, 15),
        'ground': (90, 40, 30), 'ground_top': (130, 60, 40),
        'plat': (80, 35, 25), 'plat_top': (120, 55, 35),
        'wall': (70, 30, 20), 'wall_top': (110, 50, 30),
        'bg_hill': (25, 10, 8), 'bg_build': (30, 15, 10),
    },
}

MAP_ORDER = ['plains', 'desert', 'snow', 'volcano']


class Level:
    def __init__(self):
        self.tiles = []
        self.w = 0
        self.h = 0
        self.barrels = []
        self.exitX = 0
        self.exitY = 0
        self.theme = MAP_THEMES['plains']

    def get_theme_for_map(self, map_idx):
        key = MAP_ORDER[min(map_idx, len(MAP_ORDER) - 1)]
        return MAP_THEMES[key]

    def generate(self, lv, map_idx=0):
        self.theme = self.get_theme_for_map(map_idx)
        self.w = 70 + lv * 8
        self.h = 40
        gy = self.h - 4  # 地面行
        exit_tile = self.w - 5  # 出口X坐标(tile)

        self.tiles = []
        for y in range(self.h):
            self.tiles.append([{'type': 'air', 'solid': False, 'hp': 0} for _ in range(self.w)])

        # ── 1. 地形高度变化（模拟自然地形） ──
        # 地面不再是平的，而是有起伏
        ground_profile = []
        current_h = gy  # 当前地面高度(tile行号)
        for x in range(self.w):
            # 模拟自然地形：缓慢起伏
            if x > 10 and x < self.w - 10 and random.random() < 0.06:
                change = random.choice([-1, 0, 1])
                current_h = max(gy - 2, min(gy + 2, current_h + change))
            ground_profile.append(current_h)

        # 出口区域保持平坦
        for x in range(exit_tile - 3, self.w):
            ground_profile[x] = gy

        # 填充地面方块（从地面高度到底部）
        for x in range(self.w):
            for y in range(ground_profile[x], self.h):
                self.tiles[y][x] = {'type': 'ground', 'solid': True, 'hp': 4}

        # ── 2. 阶梯平台（可达的爬升结构） ──
        # 最大跳跃高度 ≈ 5.4 tiles，平台间距设为 2-3 tiles 保证舒适
        max_jump_tiles = 3  # 安全跳跃高度（tile数）

        # 在多个区域生成上升阶梯
        stair_zones = []
        zone_w = 8 + lv
        for zone_start in range(15, self.w - 20, zone_w + random.randint(2, 6)):
            stair_zones.append(zone_start)

        for zx in stair_zones:
            if zx > exit_tile - 8:
                continue
            self._make_stairs(zx, gy, ground_profile, max_jump_tiles, lv)

        # ── 3. 浮空平台（在阶梯区域之间） ──
        for _ in range(10 + lv * 2):
            px = random.randint(5, self.w - 10)
            # 找到此位置的有效地面高度
            base_y = ground_profile[min(px, len(ground_profile) - 1)]
            # 平台高度：地面以上 1-3 tiles
            py = base_y - random.randint(1, max_jump_tiles)
            if py < 2:
                continue
            pw = random.randint(3, 8)
            # 不挡住出口
            if px + pw > exit_tile - 3:
                px = max(5, exit_tile - 3 - pw)

            valid = True
            for x in range(px, min(px + pw, self.w)):
                if self.tiles[py][x]['solid']:
                    valid = False
                    break
                # 检查下方是否有支撑（避免完全悬空的孤立平台）
                # 允许纯浮空，但至少离地面不太远
            if valid and py > 2:
                for x in range(px, min(px + pw, self.w)):
                    self.tiles[py][x] = {'type': 'plat', 'solid': True, 'hp': 2}

        # ── 4. 墙壁/柱子（障碍物，不高） ──
        for _ in range(4 + lv // 2):
            wx = random.randint(10, self.w - 15)
            if abs(wx - exit_tile) < 6:
                continue
            base_y = ground_profile[min(wx, len(ground_profile) - 1)]
            wy = base_y - random.randint(2, 4)
            wh = min(random.randint(2, 5), base_y - wy)
            if wh < 2:
                continue
            for y in range(wy, wy + wh):
                if 0 <= y < self.h and wx < self.w:
                    if not self.tiles[y][wx]['solid']:
                        self.tiles[y][wx] = {'type': 'wall', 'solid': True, 'hp': 5}
                        if wx + 1 < self.w and not self.tiles[y][wx + 1]['solid']:
                            self.tiles[y][wx + 1] = {'type': 'wall', 'solid': True, 'hp': 5}

        # ── 5. 油桶 ──
        self.barrels = []
        for _ in range(4 + lv):
            bx = random.randint(8, self.w - 8)
            by = random.randint(gy - 8, gy - 1)
            if not self.tiles[by][bx]['solid']:
                self.barrels.append({'x': bx * TILE + 8, 'y': by * TILE + 8, 'ok': True})

        # ── 6. 出口 → 确保可达 ──
        self.exitX = exit_tile * TILE
        self.exitY = (gy - 1) * TILE  # 出口标记在地面上方1格
        # 清空出口周围的墙壁和平台
        for y in range(gy - 3, gy):
            for x in range(exit_tile - 2, exit_tile + 3):
                if 0 <= y < self.h and 0 <= x < self.w:
                    if self.tiles[y][x].get('type') in ('wall', 'plat'):
                        self.tiles[y][x] = {'type': 'air', 'solid': False, 'hp': 0}

    def _make_stairs(self, start_x, gy, ground_profile, max_jump_tiles, lv):
        """在指定位置生成可攀登的阶梯结构"""
        # 阶梯方向随机
        going_up = random.random() < 0.6
        steps = random.randint(3, max(4, 6 + lv // 2))
        cx = start_x
        cy = ground_profile[min(cx, len(ground_profile) - 1)]

        for i in range(steps):
            if cx >= self.w - 10:
                break
            if cy < 4:
                break

            if going_up and random.random() < 0.7:
                cy -= random.randint(1, max_jump_tiles)
            elif not going_up and random.random() < 0.5 and cy < gy:
                cy += random.randint(1, 2)

            # 不放太高的阶梯
            if cy < gy - 8:
                cy = gy - 8
            if cy < 3:
                cy = 3

            pw = random.randint(3, 6)
            for x in range(cx, min(cx + pw, self.w)):
                if not self.tiles[cy][x]['solid']:
                    self.tiles[cy][x] = {'type': 'plat', 'solid': True, 'hp': 2}

            cx += pw + random.randint(1, 3)

    def damage_tile(self, tx, ty, dmg, particles):
        if ty < 0 or ty >= len(self.tiles) or tx < 0 or tx >= len(self.tiles[0]):
            return
        t = self.tiles[ty][tx]
        if not t['solid']:
            return
        t['hp'] -= dmg
        if t['hp'] <= 0:
            t['solid'] = False
            t['type'] = 'air'
            particles.spawn_burst(
                tx * TILE + 8, ty * TILE + 8, 5, 3,
                self.theme['ground'], (15, 30), (2, 5),
                vy_range=(-4, 0)
            )

    def explode(self, cx, cy, radius, dmg, particles, explosions, barrel_explosions):
        cxt = int(cx / TILE)
        cyt = int(cy / TILE)
        r = int(math.ceil(radius / TILE))
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if math.sqrt(dx * dx + dy * dy) <= r:
                    self.damage_tile(cxt + dx, cyt + dy, dmg, particles)

        for b in self.barrels:
            if not b['ok']:
                continue
            if math.hypot(cx - b['x'], cy - b['y']) < radius + 20:
                b['ok'] = False
                from entities.explosion import Explosion
                barrel_explosions.append({
                    'timer': 9,
                    'x': b['x'], 'y': b['y'],
                    'radius': 50, 'damage': 40,
                })

    def get_tiles(self, x, y, w, h):
        ret = []
        sx = int((x - w / 2) / TILE)
        ex = int((x + w / 2) / TILE)
        sy = int((y - h / 2) / TILE)
        ey = int((y + h / 2) / TILE)
        for ty in range(sy, ey + 1):
            for tx in range(sx, ex + 1):
                if 0 <= ty < len(self.tiles) and 0 <= tx < len(self.tiles[0]):
                    t = self.tiles[ty][tx].copy()
                    t['x'] = tx
                    t['y'] = ty
                    ret.append(t)
        return ret