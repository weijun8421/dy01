"""
DY01 - 物理引擎
瓦片碰撞检测、重力、摩擦力
"""
import math
from data.config import TILE, GRAVITY, MAX_FALL


class Physics:
    """物理计算工具类"""

    @staticmethod
    def apply_gravity(vy):
        vy += GRAVITY
        if vy > MAX_FALL:
            vy = MAX_FALL
        return vy

    @staticmethod
    def apply_friction(vx, friction=0.82, threshold=0.1):
        vx *= friction
        if abs(vx) < threshold:
            vx = 0
        return vx

    @staticmethod
    def accelerate(vx, target_speed, accel, direction):
        """平滑加速，带缓入效果"""
        remaining = target_speed * direction - vx
        if abs(remaining) < 0.01:
            return target_speed * direction
        step = accel
        if abs(remaining) < accel * 2:
            step = abs(remaining) * 0.3  # 缓入
        vx += step * (1 if remaining > 0 else -1)
        if direction > 0 and vx > target_speed:
            vx = target_speed
        elif direction < 0 and vx < -target_speed:
            vx = -target_speed
        return vx

    @staticmethod
    def resolve_collision(x, y, w, h, vx, vy, level):
        """解析瓦片碰撞，返回 (x, y, vx, vy, grounded)"""
        grounded = False
        tiles = level.get_tiles(x, y, w, h)

        for t in tiles:
            if not t['solid']:
                continue
            tx = t['x'] * TILE
            ty = t['y'] * TILE
            ox = (w + TILE) / 2 - abs(x - (tx + TILE / 2))
            oy = (h + TILE) / 2 - abs(y - (ty + TILE / 2))

            if ox > 0 and oy > 0:
                if ox < oy:
                    x += (-ox if x < tx + TILE / 2 else ox)
                    vx = 0
                else:
                    y += (-oy if y < ty + TILE / 2 else oy)
                    vy = 0
                    if y <= ty + TILE / 2:
                        grounded = True

        return x, y, vx, vy, grounded

    @staticmethod
    def aabb_vs_tiles(bx, by, bw, bh, level):
        """子弹/小物体 vs 瓦片碰撞"""
        tiles = level.get_tiles(bx, by, bw, bh)
        for t in tiles:
            if t['solid']:
                ttx = t['x'] * TILE
                tty = t['y'] * TILE
                if ttx < bx < ttx + TILE and tty < by < tty + TILE:
                    return t['x'], t['y']
        return None, None

    @staticmethod
    def distance(a, b):
        return math.hypot(a.x - b.x, a.y - b.y)