"""
DY01 - 全局配置
"""
import math

# ─── 窗口 ──────────────────────────────────────────
W, H = 960, 640
TILE = 16
FPS = 60

# ─── 物理常量 ──────────────────────────────────────
GRAVITY = 0.55
MAX_FALL = 11.0
FRICTION = 0.82
ACCEL = 0.35
JUMP_VEL = -9.8
SHORT_JUMP = -5.5
COYOTE_FRAMES = 6
JUMP_BUFFER = 6

# ─── 武器定义 ──────────────────────────────────────
WEAPONS = [
    {'name': '突击步枪', 'icon': 'AR', 'id': 'rifle', 'damage': 14, 'fireRate': 7, 'bulletSpeed': 12,
     'spread': 0.04, 'ammo': 35, 'maxAmmo': 35, 'reserve': 140, 'reloadTime': 50,
     'pellets': 1, 'explosive': False, 'pierce': False, 'color': (255, 221, 68),
     'bulletW': 3, 'bulletH': 2},
    {'name': '霰弹枪', 'icon': 'SG', 'id': 'shotgun', 'damage': 7, 'fireRate': 32, 'bulletSpeed': 9,
     'spread': 0.22, 'ammo': 8, 'maxAmmo': 8, 'reserve': 48, 'reloadTime': 70,
     'pellets': 6, 'explosive': False, 'pierce': False, 'color': (255, 136, 51),
     'bulletW': 4, 'bulletH': 4},
    {'name': '火焰枪', 'icon': 'FL', 'id': 'flame', 'damage': 4, 'fireRate': 2, 'bulletSpeed': 7,
     'spread': 0.12, 'ammo': 120, 'maxAmmo': 120, 'reserve': 360, 'reloadTime': 90,
     'pellets': 1, 'explosive': False, 'pierce': True, 'color': (255, 68, 34),
     'bulletW': 5, 'bulletH': 5},
    {'name': '激光枪', 'icon': 'LA', 'id': 'laser', 'damage': 22, 'fireRate': 10, 'bulletSpeed': 20,
     'spread': 0.01, 'ammo': 25, 'maxAmmo': 25, 'reserve': 100, 'reloadTime': 55,
     'pellets': 1, 'explosive': False, 'pierce': True, 'color': (68, 221, 255),
     'bulletW': 2, 'bulletH': 8},
    {'name': '火箭筒', 'icon': 'RK', 'id': 'rocket', 'damage': 55, 'fireRate': 55, 'bulletSpeed': 5.5,
     'spread': 0.02, 'ammo': 4, 'maxAmmo': 4, 'reserve': 16, 'reloadTime': 100,
     'pellets': 1, 'explosive': True, 'explosionRadius': 70, 'pierce': False,
     'color': (255, 34, 34), 'bulletW': 6, 'bulletH': 6},
]

# ─── Buff定义（5稀有度：白/蓝/紫/金/红） ──────────
# tier: 0=白 1=蓝 2=紫 3=金 4=红
# weapon: None=通用, 武器id=专属

BUFFS = [
    # ─── 白色（通用） ───
    {'id': 'dmg', 'name': '力量强化', 'desc': '所有武器伤害+20%', 'tier': 0, 'weapon': None, 'icon': '!'},
    {'id': 'spd', 'name': '敏捷步伐', 'desc': '移动速度+15%', 'tier': 0, 'weapon': None, 'icon': '>'},
    {'id': 'hp', 'name': '生命扩容', 'desc': '生命上限+25%', 'tier': 0, 'weapon': None, 'icon': '+'},
    {'id': 'regen', 'name': '缓慢恢复', 'desc': '每秒恢复1HP', 'tier': 0, 'weapon': None, 'icon': 'R'},
    {'id': 'ammo_bag', 'name': '弹药袋', 'desc': '所有武器备弹+50%', 'tier': 0, 'weapon': None, 'icon': 'A'},

    # ─── 蓝色（通用/专属） ───
    {'id': 'fir', 'name': '急速射击', 'desc': '射速+25%', 'tier': 1, 'weapon': None, 'icon': 'F'},
    {'id': 'vmp', 'name': '生命偷取', 'desc': '击杀恢复5HP', 'tier': 1, 'weapon': None, 'icon': 'V'},
    {'id': 'grenade', 'name': '手雷精通', 'desc': '爆炸范围+40%', 'tier': 1, 'weapon': 'rocket', 'icon': 'O'},
    {'id': 'rifle_dmg', 'name': '穿甲步枪弹', 'desc': '步枪伤害+35%', 'tier': 1, 'weapon': 'rifle', 'icon': 'R'},
    {'id': 'sg_wide', 'name': '散射强化', 'desc': '霰弹枪弹丸+3', 'tier': 1, 'weapon': 'shotgun', 'icon': 'S'},
    {'id': 'flame_burn', 'name': '持续灼烧', 'desc': '火焰枪附加燃烧伤害', 'tier': 1, 'weapon': 'flame', 'icon': 'F'},
    {'id': 'laser_beam', 'name': '聚焦光束', 'desc': '激光枪宽度+50%', 'tier': 1, 'weapon': 'laser', 'icon': 'L'},

    # ─── 紫色（通用/专属） ───
    {'id': 'prc', 'name': '穿甲弹', 'desc': '所有子弹穿透敌人', 'tier': 2, 'weapon': None, 'icon': '#'},
    {'id': 'dbl', 'name': '双发', 'desc': '每次射击两发子弹', 'tier': 2, 'weapon': None, 'icon': '2'},
    {'id': 'crit', 'name': '致命一击', 'desc': '15%概率3倍伤害', 'tier': 2, 'weapon': None, 'icon': 'C'},
    {'id': 'rifle_burst', 'name': '三连发', 'desc': '步枪改为三连发', 'tier': 2, 'weapon': 'rifle', 'icon': '3'},
    {'id': 'sg_slug', 'name': '独头弹', 'desc': '霰弹枪单发高伤', 'tier': 2, 'weapon': 'shotgun', 'icon': 'S'},
    {'id': 'flame_wave', 'name': '火焰波', 'desc': '火焰枪范围+60%', 'tier': 2, 'weapon': 'flame', 'icon': 'F'},
    {'id': 'laser_chain', 'name': '连锁闪电', 'desc': '激光命中后弹射附近敌人', 'tier': 2, 'weapon': 'laser', 'icon': 'L'},
    {'id': 'rocket_cluster', 'name': '集束弹头', 'desc': '火箭弹分裂为3枚', 'tier': 2, 'weapon': 'rocket', 'icon': 'R'},

    # ─── 金色（通用/专属） ───
    {'id': 'bsk', 'name': '狂战士之怒', 'desc': '低血时伤害翻倍', 'tier': 3, 'weapon': None, 'icon': 'B'},
    {'id': 'shield', 'name': '能量护盾', 'desc': '每30秒抵挡一次伤害', 'tier': 3, 'weapon': None, 'icon': 'S'},
    {'id': 'reload_all', 'name': '快速装填', 'desc': '换弹速度+60%', 'tier': 3, 'weapon': None, 'icon': 'R'},
    {'id': 'rifle_turret', 'name': '自动哨戒', 'desc': '步枪自动索敌射击', 'tier': 3, 'weapon': 'rifle', 'icon': 'T'},
    {'id': 'sg_stun', 'name': '震撼弹', 'desc': '霰弹枪命中眩晕敌人', 'tier': 3, 'weapon': 'shotgun', 'icon': 'S'},
    {'id': 'flame_dragon', 'name': '炎龙', 'desc': '火焰枪射出龙形火焰', 'tier': 3, 'weapon': 'flame', 'icon': 'F'},
    {'id': 'laser_overload', 'name': '超载激光', 'desc': '激光枪伤害+80%', 'tier': 3, 'weapon': 'laser', 'icon': 'L'},
    {'id': 'rocket_nuke', 'name': '微型核弹', 'desc': '火箭弹爆炸范围+100%', 'tier': 3, 'weapon': 'rocket', 'icon': 'R'},

    # ─── 红色（传说） ───
    {'id': 'god', 'name': '天神下凡', 'desc': '伤害+80% 移速+30%', 'tier': 4, 'weapon': None, 'icon': 'G'},
    {'id': 'inf_ammo', 'name': '无限弹药', 'desc': '不再消耗弹药', 'tier': 4, 'weapon': None, 'icon': 'I'},
    {'id': 'death_blow', 'name': '死亡之触', 'desc': '20%概率一击必杀', 'tier': 4, 'weapon': None, 'icon': 'D'},
    {'id': 'bullet_hell', 'name': '弹幕风暴', 'desc': '所有子弹数量+5', 'tier': 4, 'weapon': None, 'icon': 'B'},
]

# 稀有度颜色
TIER_COLORS = {
    0: (200, 200, 200),  # 白
    1: (68, 136, 255),   # 蓝
    2: (170, 68, 255),   # 紫
    3: (255, 170, 0),    # 金
    4: (255, 34, 34),    # 红
}

TIER_NAMES = {0: '普通', 1: '稀有', 2: '史诗', 3: '传说', 4: '神话'}

# ─── 敌人类型 ──────────────────────────────────────
ENEMY_TYPES = {
    'soldier': {'w': 14, 'h': 22, 'hp': 28, 'speed': 1.2, 'damage': 8, 'color': (204, 51, 51), 'score': 100},
    'elite': {'w': 16, 'h': 24, 'hp': 55, 'speed': 1.8, 'damage': 13, 'color': (170, 34, 34), 'score': 200},
    'heavy': {'w': 24, 'h': 26, 'hp': 130, 'speed': 0.6, 'damage': 22, 'color': (136, 34, 34), 'score': 400},
    'flyer': {'w': 14, 'h': 18, 'hp': 22, 'speed': 2.2, 'damage': 10, 'color': (136, 68, 204), 'score': 250, 'fly': True},
    'boss': {'w': 44, 'h': 44, 'hp': 900, 'speed': 0.7, 'damage': 30, 'color': (102, 0, 0), 'score': 3000},
}

# ─── 颜色 ──────────────────────────────────────────
COLOR_BG = (10, 10, 10)
COLOR_SKY = (26, 26, 46)
COLOR_MENU_BG = (8, 8, 20)
COLOR_GROUND = (58, 92, 62)
COLOR_GROUND_TOP = (74, 124, 89)
COLOR_PLAT = (107, 68, 35)
COLOR_PLAT_LIGHT = (139, 101, 51)
COLOR_WALL = (85, 85, 85)
COLOR_WALL_LIGHT = (119, 119, 119)
COLOR_BARREL = (204, 51, 51)
COLOR_EXIT = (0, 255, 0)
COLOR_HP = (255, 51, 51)
COLOR_HP_BG = (34, 34, 34)
COLOR_HUD_BG = (0, 0, 0, 170)
COLOR_HUD_BORDER = (85, 85, 85)
COLOR_GOLD = (255, 170, 0)
COLOR_WHITE = (255, 255, 255)
COLOR_GRAY = (136, 136, 136)
COLOR_DARK_GRAY = (85, 85, 85)
COLOR_P1 = (204, 51, 51)
COLOR_P2 = (51, 136, 238)
COLOR_SKIN = (238, 187, 153)