"""
DY01 - 音频引擎
程序化生成音效，无需外部音频文件
"""
import pygame
import math
import random
import struct
import io
from array import array

AUDIO_OK = False
_SAMPLE_RATE = 22050
_SOUND_CACHE = {}  # 缓存已生成的Sound对象


def init():
    global AUDIO_OK
    try:
        pygame.mixer.init(frequency=_SAMPLE_RATE, size=-16, channels=2, buffer=512)
        AUDIO_OK = True
    except Exception:
        print("Audio not available - running without sound")


def _gen_wav(freq, duration, vol=0.3, wave_type='square'):
    n_samples = int(_SAMPLE_RATE * duration)
    samples = array('h', [0] * n_samples)
    for i in range(n_samples):
        t = i / _SAMPLE_RATE
        if wave_type == 'square':
            v = 1.0 if (t * freq) % 1 < 0.5 else -1.0
        elif wave_type == 'sawtooth':
            v = 2.0 * ((t * freq) % 1) - 1.0
        elif wave_type == 'triangle':
            v = 4.0 * abs(((t * freq) % 1) - 0.5) - 1.0
        else:
            v = math.sin(2 * math.pi * freq * t)
        samples[i] = int(v * vol * 32767 * 0.3)
    return samples


def _gen_noise(duration, vol=0.3):
    n_samples = int(_SAMPLE_RATE * duration)
    samples = array('h', [0] * n_samples)
    for i in range(n_samples):
        samples[i] = int((random.random() * 2 - 1) * vol * 32767 * 0.3)
    return samples


def _play(samples, cache_key=None):
    if not AUDIO_OK:
        return
    try:
        if cache_key and cache_key in _SOUND_CACHE:
            snd = _SOUND_CACHE[cache_key]
        else:
            buf = io.BytesIO()
            for s in samples:
                buf.write(struct.pack('<h', s))
            buf.seek(0)
            snd = pygame.mixer.Sound(buffer=buf.read())
            if cache_key:
                _SOUND_CACHE[cache_key] = snd
        snd.set_volume(0.3)
        snd.play()
    except Exception:
        pass


def shoot(weapon_name):
    if weapon_name == 'shotgun':
        _play(_gen_noise(0.15, 0.3), 'shoot_sg1')
        _play(_gen_wav(80, 0.1, 0.2, 'sawtooth'), 'shoot_sg2')
    elif weapon_name == 'rocket':
        _play(_gen_noise(0.3, 0.5), 'shoot_rk1')
        _play(_gen_wav(40, 0.2, 0.3, 'sawtooth'), 'shoot_rk2')
    elif weapon_name == 'laser':
        _play(_gen_wav(800, 0.08, 0.15, 'square'), 'shoot_ls1')
        _play(_gen_wav(1200, 0.06, 0.1, 'square'), 'shoot_ls2')
    elif weapon_name == 'flame':
        _play(_gen_noise(0.08, 0.15), 'shoot_fl')
    else:
        _play(_gen_wav(200, 0.06, 0.15, 'square'), 'shoot_rf')


def hit():
    _play(_gen_wav(150, 0.06, 0.15, 'square'), 'hit1')
    _play(_gen_wav(80, 0.08, 0.1, 'triangle'), 'hit2')


def explode():
    _play(_gen_noise(0.4, 0.6), 'explode1')
    _play(_gen_wav(30, 0.3, 0.4, 'sawtooth'), 'explode2')


def kill():
    _play(_gen_wav(400, 0.06, 0.2, 'square'), 'kill1')
    _play(_gen_wav(600, 0.08, 0.15, 'square'), 'kill2')


def pickup():
    _play(_gen_wav(600, 0.06, 0.15, 'square'), 'pickup1')
    _play(_gen_wav(900, 0.08, 0.12, 'square'), 'pickup2')


def jump():
    _play(_gen_wav(150, 0.08, 0.08, 'triangle'), 'jump')


def dash():
    _play(_gen_noise(0.1, 0.2), 'dash')


def die():
    _play(_gen_wav(60, 0.5, 0.3, 'sawtooth'), 'die')


def buff():
    _play(_gen_wav(500, 0.06, 0.12, 'square'), 'buff1')
    _play(_gen_wav(700, 0.06, 0.1, 'square'), 'buff2')
    _play(_gen_wav(1000, 0.1, 0.15, 'square'), 'buff3')


def menu_move():
    _play(_gen_wav(300, 0.04, 0.08, 'square'), 'menu_move')


def menu_select():
    _play(_gen_wav(500, 0.06, 0.12, 'square'), 'menu_select1')
    _play(_gen_wav(800, 0.08, 0.1, 'square'), 'menu_select2')