"""
DY01 - 粒子系统
"""
import random
import pygame


class Particle:
    def __init__(self, x, y, vx, vy, color, life, size):
        self.x = x
        self.y = y
        self.vx = vx
        self.vy = vy
        self.color = color
        self.life = life
        self.max_life = life
        self.size = size

    def update(self):
        self.x += self.vx
        self.y += self.vy
        self.vy += 0.12
        self.life -= 1
        return self.life > 0

    def draw(self, surf, cam_x):
        a = self.life / self.max_life
        s = pygame.Surface((int(self.size), int(self.size)), pygame.SRCALPHA)
        s.fill((*self.color, int(a * 255)))
        surf.blit(s, (int(self.x - cam_x), int(self.y)))


class FloatText:
    def __init__(self, x, y, text, color, size=16):
        self.x = x
        self.y = y
        self.text = text
        self.color = color
        self.size = size
        self.life = 40
        self.max_life = 40

    def update(self):
        self.y -= 1.2
        self.life -= 1
        return self.life > 0

    def draw(self, surf, cam_x, font_mgr):
        a = self.life / self.max_life
        try:
            txt = font_mgr.get(self.size).render(self.text, True, self.color)
        except Exception:
            return
        txt.set_alpha(int(a * 255))
        surf.blit(txt, (int(self.x - cam_x), int(self.y)))


class ParticleSystem:
    def __init__(self):
        self.particles = []
        self.float_texts = []

    def clear(self):
        self.particles.clear()
        self.float_texts.clear()

    def spawn(self, x, y, vx, vy, color, life, size):
        self.particles.append(Particle(x, y, vx, vy, color, life, size))

    def spawn_burst(self, x, y, count, spread, color, life_range, size_range, vy_range=None):
        for _ in range(count):
            vx = random.uniform(-spread, spread)
            if vy_range:
                vy = random.uniform(*vy_range)
            else:
                vy = random.uniform(-spread, spread * 0.5)
            life = random.randint(*life_range)
            size = random.uniform(*size_range)
            self.spawn(x, y, vx, vy, color, life, size)

    def spawn_text(self, x, y, text, color, size=16):
        self.float_texts.append(FloatText(x, y, text, color, size))

    def update(self):
        self.particles = [p for p in self.particles if p.update()]
        self.float_texts = [t for t in self.float_texts if t.update()]

    def draw(self, surf, cam_x, font_mgr=None):
        for p in self.particles:
            p.draw(surf, cam_x)
        if font_mgr:
            for t in self.float_texts:
                t.draw(surf, cam_x, font_mgr)