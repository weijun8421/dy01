"""
DY01 - 输入处理
基于事件的键盘追踪，支持 P1/P2
"""
import pygame


class Input:
    """输入管理器，维护当前帧和上一帧的按键状态"""

    def __init__(self):
        self.keys = {}
        self.prev = {}
        self._raw = {}          # key_code -> bool（事件驱动）

    def handle_event(self, event):
        """在主循环中调用，处理所有事件"""
        if event.type == pygame.KEYDOWN:
            self._raw[event.key] = True
        elif event.type == pygame.KEYUP:
            self._raw[event.key] = False

    def update(self):
        """每帧调用一次，将原始按键映射为命名动作"""
        self.prev = dict(self.keys)
        self.keys = {}
        k = self._raw

        get = lambda code: k.get(code, False)

        # P1: WASD + 周边键
        self.keys['p1_left'] = get(pygame.K_a) or get(pygame.K_LEFT)
        self.keys['p1_right'] = get(pygame.K_d) or get(pygame.K_RIGHT)
        self.keys['p1_jump'] = get(pygame.K_SPACE) or get(pygame.K_w) or get(pygame.K_UP)
        self.keys['p1_dash'] = get(pygame.K_LSHIFT) or get(pygame.K_RSHIFT)
        self.keys['p1_shoot'] = get(pygame.K_j)
        self.keys['p1_melee'] = get(pygame.K_k)
        self.keys['p1_reload'] = get(pygame.K_r)

        # P2: 方向键 + 小键盘
        self.keys['p2_left'] = get(pygame.K_LEFT)
        self.keys['p2_right'] = get(pygame.K_RIGHT)
        self.keys['p2_jump'] = get(pygame.K_KP0)
        self.keys['p2_dash'] = get(pygame.K_KP_PERIOD)
        self.keys['p2_shoot'] = get(pygame.K_KP1)
        self.keys['p2_melee'] = get(pygame.K_KP2)
        self.keys['p2_reload'] = get(pygame.K_KP8)

        # 系统键
        self.keys['up'] = get(pygame.K_UP) or get(pygame.K_w)
        self.keys['down'] = get(pygame.K_DOWN) or get(pygame.K_s)
        self.keys['left'] = get(pygame.K_LEFT) or get(pygame.K_a)
        self.keys['right'] = get(pygame.K_RIGHT) or get(pygame.K_d)
        self.keys['enter'] = get(pygame.K_RETURN) or get(pygame.K_SPACE)
        self.keys['escape'] = get(pygame.K_ESCAPE)
        self.keys['key_1'] = get(pygame.K_1)
        self.keys['key_2'] = get(pygame.K_2)
        self.keys['key_3'] = get(pygame.K_3)
        self.keys['key_4'] = get(pygame.K_4)
        self.keys['key_5'] = get(pygame.K_5)
        self.keys['key_m'] = get(pygame.K_m)

    def get(self, name):
        return self.keys.get(name, False)

    def pressed(self, name):
        """按下瞬间（上升沿）"""
        return self.keys.get(name, False) and not self.prev.get(name, False)

    def released(self, name):
        """释放瞬间（下降沿）"""
        return not self.keys.get(name, False) and self.prev.get(name, False)

    def get_weapon_switch(self, player_idx):
        """获取武器切换数字键"""
        get = lambda code: self._raw.get(code, False)
        if player_idx == 0:
            raw_keys = [pygame.K_1, pygame.K_2, pygame.K_3, pygame.K_4, pygame.K_5]
        else:
            raw_keys = [pygame.K_KP3, pygame.K_KP4, pygame.K_KP5, pygame.K_KP6, pygame.K_KP7]
        result = []
        for i, code in enumerate(raw_keys):
            if get(code) and not self.prev.get(f'wep_{player_idx}_{i}', False):
                result.append(i)
            self.prev[f'wep_{player_idx}_{i}'] = get(code)
        return result