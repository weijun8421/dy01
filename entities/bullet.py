"""
DY01 - 子弹
"""
import math
import pygame


class Bullet:
    def __init__(self, x, y, vx, vy, weapon, owner_id):
        self.x = x
        self.y = y
        self.vx = vx
        self.vy = vy
        self.weapon = weapon
        self.owner_id = owner_id
        self.life = 80
        self.hits = set()
        self.trail = []

    def update(self):
        self.trail.append((self.x, self.y))
        if len(self.trail) > 4:
            self.trail.pop(0)
        self.x += self.vx
        self.y += self.vy
        self.life -= 1
        return (self.life > 0 and -200 < self.x < 6000 and -200 < self.y < 800)

    def draw(self, surf, cam_x):
        w = self.weapon
        # 拖尾
        for i, (tx, ty) in enumerate(self.trail):
            a = int((i / len(self.trail)) * 0.35 * 255)
            s = pygame.Surface((w['bulletW'] + 2, w['bulletH'] + 2), pygame.SRCALPHA)
            if w.get('explosive'):
                pygame.draw.rect(s, (*w['color'], a), (1, 1, 4, 4))
            else:
                pygame.draw.rect(s, (*w['color'], a), (0, 0, w['bulletW'], w['bulletH']))
            surf.blit(s, (int(tx - cam_x), int(ty)))

        sx, sy = int(self.x - cam_x), int(self.y)
        if w.get('explosive'):
            pygame.draw.rect(surf, w['color'], (sx - 3, sy - 3, 6, 6))
            pygame.draw.rect(surf, (255, 136, 68), (sx - 2, sy - 2, 4, 4))
        else:
            pygame.draw.rect(surf, w['color'], (sx, sy, w['bulletW'], w['bulletH']))