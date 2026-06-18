"""
DY01 - 爆炸
"""
import pygame


class Explosion:
    def __init__(self, x, y, radius, damage):
        self.x = x
        self.y = y
        self.radius = radius
        self.damage = damage
        self.life = 22
        self.max_life = 22

    def update(self):
        self.life -= 1
        return self.life > 0

    def draw(self, surf, cam_x):
        a = self.life / self.max_life
        r = self.radius * (1 - a * 0.5) * (a / 0.3 if a < 0.3 else 1)
        cx, cy = int(self.x - cam_x), int(self.y)

        s = pygame.Surface((int(r * 2 + 4), int(r * 2 + 4)), pygame.SRCALPHA)
        pygame.draw.circle(s, (255, 102, 0, int(a * 0.6 * 255)), (int(r + 2), int(r + 2)), int(r))
        pygame.draw.circle(s, (255, 204, 0, int(a * 0.8 * 255)), (int(r + 2), int(r + 2)), int(r * 0.55))
        pygame.draw.circle(s, (255, 255, 255, int(a * 255)), (int(r + 2), int(r + 2)), int(r * 0.25))
        surf.blit(s, (cx - int(r + 2), cy - int(r + 2)))