using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DY01.Data;
using DY01.Engine;
using DY01.Entities;
using DY01.Game;

namespace DY01;

public class GameMain : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Renderer _renderer = null!;
    private InputManager _input = null!;
    private ParticleSystem _particles = null!;
    private WeatherSystem _weather = null!;
    private LightingSystem _lighting = null!;
    private SpriteFont? _font;

    // UI
    private MenuScreen _menu = null!;
    private HUD _hud = null!;
    private BuffSelectScreen _buffScreen = null!;
    private WeaponSelectScreen _weaponScreen = null!;
    private SettingsScreen _settingsScreen = null!;

    // Game state
    private enum State { Menu, WeaponSelect, Playing, Paused, Buff, GameOver, Victory, Settings }
    private State _state = State.Menu;
    private string _mode = "campaign";
    private Player? _player;
    private Player? _player2;
    private Level? _level;
    private List<Enemy> _enemies = new();
    private List<Bullet> _bullets = new();
    private List<Bullet> _enemyBullets = new();
    private List<Explosion> _explosions = new();
    private List<BarrelExplosion> _barrelExplosions = new();
    private List<Pickup> _pickups = new();
    private int _score, _kills, _levelNum = 1, _waveNum = 1, _waveRemain;
    private bool _waveDone;
    private int _hitstop;
    private List<BuffDef> _buffChoices = new();
    private Random _rng = new();

    // Combo system
    private int _combo = 0;
    private int _comboTimer = 0;
    private float _comboMultiplier = 1f;
    private WeaponDef _selectedWeapon = Config.WEAPONS[0];

    // Damage flash
    private int _damageFlash = 0;

    // Level transition
    private int _transitionTimer = 0;
    private bool _transitioning = false;

    // Wave announcement
    private int _waveAnnounceTimer = 0;
    private string _waveAnnounceText = "";

    // Slow motion effect
    private int _slowMotionTimer = 0;
    private float _slowMotionScale = 1.0f;

    // Game over stats
    private int _gameStartTime;
    private int _gameEndTime;
    private int _maxCombo;
    private int _bossKills;

    // Campaign map system
    private int _mapIdx, _mapLevel = 1;
    private const int MAP_LEVELS = 5;
    private bool _firstBuff = true;
    private int _turretTimer;
    private bool _bossLevel;
    private bool _bossAlive;

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = Config.W;
        _graphics.PreferredBackBufferHeight = Config.H;
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        _input = new InputManager();
        _particles = new ParticleSystem();
        _weather = new WeatherSystem();
        _lighting = new LightingSystem();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Try to load spritefont, fall back to null
        try
        {
            _font = Content.Load<SpriteFont>("Font");
        }
        catch
        {
            _font = null;
        }

        _renderer = new Renderer(GraphicsDevice, _spriteBatch, _font!);
        _menu = new MenuScreen();
        _hud = new HUD();
        _buffScreen = new BuffSelectScreen();
        _weaponScreen = new WeaponSelectScreen();
        _settingsScreen = new SettingsScreen();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        switch (_state)
        {
            case State.Menu:
                UpdateMenu();
                break;
            case State.WeaponSelect:
                UpdateWeaponSelect();
                break;
            case State.Playing:
                UpdatePlaying();
                break;
            case State.Paused:
                UpdatePaused();
                break;
            case State.Buff:
                UpdateBuff();
                break;
            case State.GameOver:
                UpdateGameOver();
                break;
            case State.Victory:
                UpdateVictory();
                break;
            case State.Settings:
                UpdateSettings();
                break;
        }

        base.Update(gameTime);
    }

    private void UpdateMenu()
    {
        _menu.Update();
        if (_input.Pressed("up")) _menu.MoveUp();
        else if (_input.Pressed("down")) _menu.MoveDown();
        else if (_input.Pressed("enter"))
        {
            _mode = _menu.Confirm();
            _levelNum = 1; _waveNum = 1; _mapIdx = 0; _mapLevel = 1;
            _firstBuff = true;
            _state = State.WeaponSelect;
        }
        else if (_input.Pressed("key_s"))
        {
            _state = State.Settings;
        }
    }

    private void UpdateWeaponSelect()
    {
        _weaponScreen.Update();
        if (_input.Pressed("up")) _weaponScreen.MoveUp();
        else if (_input.Pressed("down")) _weaponScreen.MoveDown();
        else if (_input.Pressed("enter"))
        {
            _selectedWeapon = _weaponScreen.Confirm();
            Begin(_selectedWeapon);
        }
        else if (_input.Pressed("escape"))
        {
            _state = State.Menu;
        }
    }

    private void UpdatePlaying()
    {
        if (_input.Pressed("escape")) { _state = State.Paused; return; }

        AudioManager.Update();

        if (_hitstop > 0) { _hitstop--; return; }

        // Update combo timer
        if (_comboTimer > 0)
        {
            _comboTimer--;
            if (_comboTimer <= 0)
            {
                _combo = 0;
                _comboMultiplier = 1f;
            }
        }

        // Update damage flash
        if (_damageFlash > 0) _damageFlash--;

        // Update slow motion
        if (_slowMotionTimer > 0)
        {
            _slowMotionTimer--;
            _slowMotionScale = 0.3f;
            if (_slowMotionTimer <= 0) _slowMotionScale = 1.0f;
        }

        // Check death
        bool p1Dead = _player != null && _player.Dead && _player.RespawnTimer <= 0;
        bool p2Dead = _player2 == null || (_player2.Dead && _player2.RespawnTimer <= 0);
        if (_mode == "coop" && p1Dead && p2Dead) { _state = State.GameOver; return; }
        else if (_mode != "coop" && p1Dead) { _state = State.GameOver; return; }

        // Update players
        var keys = _input.KeyStates;

        _player?.Update(keys, _level!, _bullets, _particles, _renderer.Camera);

        if (_player2 != null)
        {
            _player2.Update(keys, _level!, _bullets, _particles, _renderer.Camera);
        }

        // Update player light
        if (_player != null && !_player.Dead)
        {
            _lighting.SetPlayerLight(_player.X, _player.Y - _player.H / 2, 120, new Color(255, 240, 200));
        }

        // Camera
        float tx = _player?.X ?? 0;
        if (_player2 != null && !_player2.Dead)
        {
            if (_player != null && !_player.Dead) tx = (_player.X + _player2.X) / 2;
            else tx = _player2.X;
        }
        _renderer.Camera.Follow(tx);
        _renderer.Camera.Clamp(0, (_level?.W ?? 70) * Config.TILE - Config.W);
        _renderer.Camera.Update();

        // Update bullets
        UpdateBullets();

        // Update enemies
        _enemies.RemoveAll(e =>
        {
            if (_player == null) return true;
            bool wasBoss = e.Type == "boss";
            bool dead = !e.Update(_player, _player2, _level!, _particles, _renderer.Camera, _enemyBullets) || e.Hp <= 0;
            if (dead && wasBoss) _bossAlive = false;
            return dead;
        });

        // 处理敌人呼叫支援
        foreach (var e in _enemies)
        {
            if (e.CallForHelp)
            {
                e.CallForHelp = false;
                // 在范围内寻找未警觉的敌人并提升其警觉等级
                foreach (var other in _enemies)
                {
                    if (other == e || other.AlertLevel >= 2) continue;
                    float dist = MathF.Sqrt((e.X - other.X) * (e.X - other.X) + (e.Y - other.Y) * (e.Y - other.Y));
                    if (dist < 180f)
                    {
                        other.AlertLevel = 2;
                        other.AlertTimer = 120;
                        // 显示呼叫支援的视觉效果
                        _particles.SpawnText(other.X, other.Y - other.H, "!", new Color(255, 100, 100), 12);
                    }
                }
            }
        }

        if (_waveRemain > 0 && _enemies.Count < 7)
            SpawnEnemies(Math.Min(3, _waveRemain));

        // Turret
        UpdateTurret();

        // Update enemy bullets
        UpdateEnemyBullets();

        // Particles and explosions
        _particles.Update();
        _weather.Update(_renderer.Camera);
        AudioManager.SetWeather(_weather.CurrentWeather.ToString());
        _lighting.Update();
        _explosions.RemoveAll(e => !e.Update());
        ApplyExplosionDamage();

        // Update pickups
        UpdatePickups();

        // Barrel explosions
        for (int i = _barrelExplosions.Count - 1; i >= 0; i--)
        {
            var be = _barrelExplosions[i];
            be.Timer--;
            if (be.Timer <= 0)
            {
                _explosions.Add(new Explosion(be.X, be.Y, be.Radius, be.Damage));
                _level?.Explode(be.X, be.Y, be.Radius, (int)be.Damage, _particles, _explosions, _barrelExplosions);
                _renderer.Camera.AddShake(14);
                _barrelExplosions.RemoveAt(i);
            }
        }

        // Check wave complete (endless mode only)
        if (_enemies.Count == 0 && _waveRemain <= 0 && !_waveDone)
        {
            _waveDone = true;
            if (_mode == "endless") ShowBuffs();
        }

        // Campaign: walk to exit (no need to kill all enemies)
        if (_mode == "campaign" && _player != null && !_player.Dead && _level != null)
        {
            // Boss level: must kill boss first
            if (_bossLevel && _bossAlive)
            {
                // Boss still alive, show hint
            }
            else if (Math.Abs(_player.X - _level.ExitX) < 80)
            {
                ShowBuffs();
            }
        }
    }

    private void UpdatePaused()
    {
        if (_input.Pressed("escape")) _state = State.Playing;
        else if (_input.Pressed("key_m"))
        {
            _player = null; _player2 = null;
            _state = State.Menu;
        }
    }

    private void UpdateBuff()
    {
        if (_input.Pressed("key_1")) SelectBuff(0);
        else if (_input.Pressed("key_2")) SelectBuff(1);
        else if (_input.Pressed("key_3")) SelectBuff(2);
    }

    private void UpdateGameOver()
    {
        if (_input.Pressed("enter"))
        {
            if (_mode == "campaign")
            {
                _levelNum = 1; _waveNum = 1; _mapIdx = 0; _mapLevel = 1;
            }
            _player = null; _player2 = null;
            _firstBuff = true;
            ShowBuffs();
        }
        else if (_input.Pressed("escape"))
        {
            _player = null; _player2 = null;
            _state = State.Menu;
        }
    }

    private void UpdateVictory()
    {
        if (_input.Pressed("enter"))
        {
            _mapIdx = (_mapIdx + 1) % Level.MAP_ORDER.Length;
            _mapLevel = 1;
            _levelNum++;
            ShowBuffs();
        }
        else if (_input.Pressed("escape"))
        {
            _player = null; _player2 = null;
            _state = State.Menu;
        }
    }

    private void UpdateSettings()
    {
        _settingsScreen.Update();
        if (_input.Pressed("up")) _settingsScreen.MoveUp();
        else if (_input.Pressed("down")) _settingsScreen.MoveDown();
        else if (_input.Pressed("left")) _settingsScreen.AdjustVolume(-0.05f);
        else if (_input.Pressed("right")) _settingsScreen.AdjustVolume(0.05f);
        else if (_input.Pressed("enter"))
        {
            if (_settingsScreen.Selected == 1) // 返回主菜单
            {
                _state = State.Menu;
            }
        }
        else if (_input.Pressed("escape"))
        {
            _state = State.Menu;
        }
    }

    private string MapName()
    {
        if (_mapIdx < Level.MAP_ORDER.Length)
            return Level.MAP_THEMES[Level.MAP_ORDER[_mapIdx]].Name;
        return "??";
    }

    private void Begin(WeaponDef weapon)
    {
        _state = State.Playing;
        _score = Math.Max(0, _score);
        _waveDone = false;

        // Initialize game stats
        _gameStartTime = Environment.TickCount;
        _gameEndTime = 0;
        _maxCombo = 0;
        _bossKills = 0;

        _level = new Level();
        _level.Generate(_levelNum, _mapIdx);

        // Calculate ground level at spawn position
        int spawnTileX = 4;
        int groundY = (_level.H - 4) * Config.TILE;

        if (_player != null)
        {
            _player.X = spawnTileX * Config.TILE;
            _player.Y = groundY - _player.H / 2;
            _player.Vx = _player.Vy = 0;
            _player.Dead = false;
            _player.RespawnTimer = 0;
            _player.Hp = _player.MaxHpActual;
            _player.Weapon = weapon.Clone();
        }
        else
            _player = new Player(spawnTileX * Config.TILE, groundY - 16, false, weapon);

        if (_mode == "coop")
        {
            if (_player2 != null)
            {
                _player2.X = (spawnTileX + 3) * Config.TILE;
                _player2.Y = groundY - _player2.H / 2;
                _player2.Vx = _player2.Vy = 0;
                _player2.Dead = false;
                _player2.RespawnTimer = 0;
                _player2.Hp = _player2.MaxHpActual;
                _player2.Weapon = weapon.Clone();
            }
            else
                _player2 = new Player((spawnTileX + 3) * Config.TILE, groundY - 16, true, weapon);
        }
        else
            _player2 = null;

        _enemies.Clear();
        _bullets.Clear();
        _enemyBullets.Clear();
        _particles.Clear();
        _explosions.Clear();
        _barrelExplosions.Clear();
        _pickups.Clear();
        _lighting.Clear();
        _renderer.Camera.Reset();
        _hitstop = 0;
        
        // Set theme for weather and lighting
        if (_level != null)
        {
            _weather.SetTheme(_level.Theme.Name);
            _lighting.SetTheme(_level.Theme.Name);
            AudioManager.SetTheme(_level.Theme.Name);
        }
        
        // Start transition animation
        _transitioning = true;
        _transitionTimer = 60; // 1 second at 60fps
        
        SpawnWave();
        
        // Show wave announcement
        _waveAnnounceTimer = 120; // 2 seconds
        _waveAnnounceText = _mode == "campaign" 
            ? $"关卡 {_levelNum} - {MapName()}" 
            : $"WAVE {_waveNum}";
    }

    private void SpawnWave()
    {
        _waveRemain = _mode == "endless" ? 6 + _waveNum * 3 : 10 + _levelNum * 5;
        _waveDone = false;
        
        // 判断是否是Boss关（每地图第5关）
        _bossLevel = _mode == "campaign" && _mapLevel == MAP_LEVELS;
        _bossAlive = false;
        
        SpawnEnemies(Math.Min(5, _waveRemain));
        
        if (_bossLevel)
        {
            SpawnBoss();
            _bossAlive = true;
        }
    }

    private void SpawnEnemies(int count)
    {
        if (_level == null) return;
        int gy = (_level.H - 4) * Config.TILE;
        
        // 难度缩放：根据关卡数和地图进度增加敌人强度
        float difficultyScale = 1.0f + (_levelNum - 1) * 0.15f + (_mapIdx * MAP_LEVELS + _mapLevel - 1) * 0.08f;
        
        for (int i = 0; i < count; i++)
        {
            if (_waveRemain <= 0) break;
            float ex = (float)(_rng.NextDouble() * ((_level.W - 5) * Config.TILE - 300) + 300);
            float ey = (float)(_rng.NextDouble() * (gy - 30 - 100) + 100);
            string etype = "soldier";
            double roll = _rng.NextDouble();
            
            // 根据关卡进度调整敌人类型分布
            if (_mode == "endless" && _waveNum > 3)
            {
                if (roll < 0.08) etype = "heavy";
                else if (roll < 0.20) etype = "bomber";
                else if (roll < 0.32) etype = "healer";
                else if (roll < 0.48) etype = "elite";
                else if (roll < 0.62) etype = "flyer";
                else if (roll < 0.75) etype = "shooter";
            }
            else if (_levelNum > 3 || _mapIdx > 0)
            {
                if (roll < 0.12) etype = "elite";
                else if (roll < 0.22) etype = "bomber";
                else if (roll < 0.32) etype = "healer";
                else if (roll < 0.45) etype = "flyer";
                else if (roll < 0.58) etype = "shooter";
                else if (roll < 0.68 && _levelNum > 5) etype = "heavy";
            }
            
            if (_mode == "endless" && _waveNum % 5 == 0 && _waveNum >= 5 && _enemies.Count == 0)
                etype = "boss";
                
            var enemy = new Enemy(ex, ey, etype);
            
            // 应用难度缩放
            enemy.Hp *= difficultyScale;
            enemy.MaxHp = enemy.Hp;
            enemy.Damage *= (1.0f + (difficultyScale - 1.0f) * 0.5f); // 伤害增长较慢
            
            _enemies.Add(enemy);
            _waveRemain--;
        }
    }

    private void SpawnBoss()
    {
        if (_level == null) return;
        float ex = _level.ExitX - 150;
        float ey = _level.ExitY - 20;
        
        // Boss难度随地图进度增加
        float bossScale = 1.0f + _mapIdx * 0.4f;
        var boss = new Enemy(ex, ey, "boss");
        boss.Hp *= 1.5f * bossScale;
        boss.MaxHp = boss.Hp;
        boss.Damage *= (1.0f + (_mapIdx * 0.2f));
        
        _enemies.Add(boss);
    }

    private void SpawnPickup(float x, float y, string enemyType)
    {
        // Drop chance based on enemy type
        double roll = _rng.NextDouble();
        float dropChance = enemyType switch
        {
            "boss" => 1.0f,
            "heavy" => 0.5f,
            "elite" => 0.35f,
            _ => 0.12f
        };

        if (roll > dropChance) return;

        // Determine pickup type
        bool needHealth = (_player != null && _player.Hp < _player.MaxHpActual * 0.7f) ||
                          (_player2 != null && _player2.Hp < _player2.MaxHpActual * 0.7f);
        
        string type;
        float value;
        double typeRoll = _rng.NextDouble();
        
        if (needHealth && typeRoll < 0.5f)
        {
            type = "health";
            value = enemyType == "boss" ? 50 : (enemyType == "heavy" ? 30 : 20);
        }
        else
        {
            type = "ammo";
            value = enemyType == "boss" ? 999 : (enemyType == "heavy" ? 30 : 15);
        }

        _pickups.Add(new Pickup(x, y, type, value));
    }

    private void UpdatePickups()
    {
        _pickups.RemoveAll(p => !p.Update(_level!, _particles));

        // Check player pickup
        for (int i = _pickups.Count - 1; i >= 0; i--)
        {
            var p = _pickups[i];
            if (_player != null && p.CheckPickup(_player))
            {
                p.Apply(_player, _particles);
                _pickups.RemoveAt(i);
            }
            else if (_player2 != null && p.CheckPickup(_player2))
            {
                p.Apply(_player2, _particles);
                _pickups.RemoveAt(i);
            }
        }
    }

    private void ShowBuffs()
    {
        _state = State.Buff;
        _buffChoices = BuffSystem.GetChoices(_levelNum, _player);
        _buffScreen.SetChoices(_buffChoices);
    }

    private void SelectBuff(int idx)
    {
        if (idx >= _buffChoices.Count) return;
        var buff = _buffChoices[idx];

        if (_player == null)
        {
            int gy = (_level?.H ?? 40) - 4;
            _player = new Player(80, gy * Config.TILE - 16, false, _selectedWeapon);
            if (_mode == "coop") _player2 = new Player(120, gy * Config.TILE - 16, true, _selectedWeapon);
        }

        var targets = new List<Player> { _player };
        if (_player2 != null) targets.Add(_player2);
        foreach (var t in targets) BuffSystem.Apply(t, buff);

        if (!_firstBuff)
        {
            if (_mode == "campaign")
            {
                _mapLevel++;
                if (_mapLevel > MAP_LEVELS)
                {
                    if (_mapIdx >= Level.MAP_ORDER.Length - 1)
                    {
                        _state = State.Victory;
                        return;
                    }
                    _mapIdx = (_mapIdx + 1) % Level.MAP_ORDER.Length;
                    _mapLevel = 1;
                }
                _levelNum++;
            }
            else
                _waveNum++;
        }
        _firstBuff = false;

        Begin(_selectedWeapon);
    }

    private void UpdateBullets()
    {
        var newBullets = new List<Bullet>();
        foreach (var b in _bullets)
        {
            if (!b.Update()) continue;

            bool hitSomething = false;
            foreach (var e in _enemies)
            {
                if (b.Hits.Contains(e)) continue;
                if (MathF.Sqrt((b.X - e.X) * (b.X - e.X) + (b.Y - e.Y) * (b.Y - e.Y)) < e.W / 2f + b.Weapon.BulletW + 2)
                {
                    b.Hits.Add(e);
                    float knockbackDir = b.Vx > 0 ? 1 : -1;
                    bool killed = e.Hit(b.Weapon.Damage, _particles, knockbackDir);
                    e.ApplyEffects(b.Weapon);
                    if (killed)
                    {
                        _kills++;
                        _combo++;
                        _comboTimer = 180; // 3 seconds at 60fps
                        _comboMultiplier = 1f + MathF.Min(_combo * 0.1f, 2f); // Max 3x multiplier
                        _score += (int)(e.Score * _comboMultiplier);
                        
                        // Track max combo
                        if (_combo > _maxCombo) _maxCombo = _combo;
                        
                        // Track boss kills
                        if (e.Type == "boss") _bossKills++;
                        
                        var owner = b.OwnerId == "p1" ? _player : _player2;
                        if (owner != null && owner.BuffVampire > 0) owner.Heal(owner.BuffVampire);
                        _renderer.Camera.AddShake(4);
                        _hitstop = 3;
                        
                        // Drop pickup chance
                        SpawnPickup(e.X, e.Y, e.Type);
                        
                        // Slow motion for last enemy
                        if (_enemies.Count == 1 && _waveRemain <= 0)
                        {
                            _slowMotionTimer = 90; // 1.5 seconds at 60fps
                            _renderer.Camera.AddShake(8);
                        }
                    }
                    if (b.Weapon.Explosive)
                    {
                        var owner = b.OwnerId == "p1" ? _player : _player2;
                        float r = b.Weapon.ExplosionRadius * (owner?.BuffExplosion ?? 1f);
                        float dmg = b.Weapon.Damage * (owner != null && owner.BuffNuke ? 2 : 1);
                        _explosions.Add(new Explosion(b.X, b.Y, r, dmg));
                        _level?.Explode(b.X, b.Y, r, 3, _particles, _explosions, _barrelExplosions);
                        _renderer.Camera.AddShake(14);
                        hitSomething = true;
                        break;
                    }
                    var own = b.OwnerId == "p1" ? _player : _player2;
                    if (own == null || !own.BuffPierce)
                    {
                        hitSomething = true;
                        break;
                    }
                }
            }

            if (hitSomething) continue;

            // Tile collision
            var (tx, ty) = Physics.AabbVsTiles(b.X, b.Y, 4, 4, _level!);
            if (tx.HasValue && ty.HasValue)
            {
                _level?.DamageTile(tx.Value, ty.Value, 1, _particles);
                _particles.SpawnBurst(b.X, b.Y, 3, 2, new Color(136, 136, 136), (5, 12), (1, 3), (-2, 1));
                if (b.Weapon.Explosive)
                {
                    var owner = b.OwnerId == "p1" ? _player : _player2;
                    float r = b.Weapon.ExplosionRadius * (owner?.BuffExplosion ?? 1f);
                    _explosions.Add(new Explosion(b.X, b.Y, r, b.Weapon.Damage));
                    _level?.Explode(b.X, b.Y, r, 3, _particles, _explosions, _barrelExplosions);
                    _renderer.Camera.AddShake(14);
                }
                continue;
            }

            newBullets.Add(b);
        }
        _bullets = newBullets;
    }

    private void UpdateEnemyBullets()
    {
        var newBullets = new List<Bullet>();
        foreach (var b in _enemyBullets)
        {
            if (!b.Update()) continue;

            // 治疗子弹：治疗附近的敌人
            if (b.OwnerId == "heal")
            {
                bool healed = false;
                foreach (var e in _enemies)
                {
                    if (e.Type == "healer") continue; // 不治疗自己
                    float dist = MathF.Sqrt((b.X - e.X) * (b.X - e.X) + (b.Y - e.Y) * (b.Y - e.Y));
                    if (dist < 30 && e.Hp < e.MaxHp)
                    {
                        // 治疗敌人
                        float healAmount = Math.Abs(b.Weapon.Damage);
                        e.Hp = Math.Min(e.MaxHp, e.Hp + healAmount);
                        _particles.SpawnBurst(e.X, e.Y - e.H / 2f, 5, 2, new Color(100, 255, 100), (10, 20), (2, 4), (-3, 0));
                        _particles.SpawnText(e.X, e.Y - e.H, $"+{(int)healAmount}", new Color(100, 255, 100), 12);
                        healed = true;
                        break;
                    }
                }
                if (healed) continue;
                
                // 治疗子弹碰到地形消失
                var (tx, ty) = Physics.AabbVsTiles(b.X, b.Y, 4, 4, _level!);
                if (tx.HasValue && ty.HasValue)
                {
                    _particles.SpawnBurst(b.X, b.Y, 3, 2, new Color(100, 255, 100), (5, 12), (1, 3), (-2, 1));
                    continue;
                }
                
                newBullets.Add(b);
                continue;
            }

            bool hitSomething = false;
            
            // Check collision with players
            if (_player != null && !_player.Dead)
            {
                float dist = MathF.Sqrt((b.X - _player.X) * (b.X - _player.X) + (b.Y - _player.Y) * (b.Y - _player.Y));
                if (dist < _player.W / 2f + b.Weapon.BulletW)
                {
                    _player.TakeDamage(b.Weapon.Damage, _particles, amt => _renderer.Camera.AddShake(amt));
                    _damageFlash = 30;
                    hitSomething = true;
                }
            }
            
            if (!hitSomething && _player2 != null && !_player2.Dead)
            {
                float dist = MathF.Sqrt((b.X - _player2.X) * (b.X - _player2.X) + (b.Y - _player2.Y) * (b.Y - _player2.Y));
                if (dist < _player2.W / 2f + b.Weapon.BulletW)
                {
                    _player2.TakeDamage(b.Weapon.Damage, _particles, amt => _renderer.Camera.AddShake(amt));
                    hitSomething = true;
                }
            }

            if (hitSomething) continue;

            // Tile collision
            var (tx2, ty2) = Physics.AabbVsTiles(b.X, b.Y, 4, 4, _level!);
            if (tx2.HasValue && ty2.HasValue)
            {
                _particles.SpawnBurst(b.X, b.Y, 3, 2, new Color(255, 153, 51), (5, 12), (1, 3), (-2, 1));
                continue;
            }

            newBullets.Add(b);
        }
        _enemyBullets = newBullets;
    }

    private void ApplyExplosionDamage()
    {
        foreach (var ex in _explosions)
        {
            if (ex.Life != ex.MaxLife - 1) continue;
            foreach (var e in _enemies)
            {
                float dist = MathF.Sqrt((ex.X - e.X) * (ex.X - e.X) + (ex.Y - e.Y) * (ex.Y - e.Y));
                if (dist < ex.Radius)
                {
                    bool killed = e.Hit(ex.Damage, _particles);
                    if (killed)
                    {
                        _kills++;
                        _combo++;
                        _comboTimer = 180;
                        _comboMultiplier = 1f + MathF.Min(_combo * 0.1f, 2f);
                        _score += (int)(e.Score * _comboMultiplier);
                        if (_player != null && _player.BuffVampire > 0) _player.Heal(_player.BuffVampire);
                    }
                }
            }
        }
    }

    private void UpdateTurret()
    {
        if (_player == null || _player.Dead || !_player.BuffTurret) return;
        _turretTimer++;
        if (_turretTimer < 30) return;
        _turretTimer = 0;
        if (_enemies.Count == 0) return;

        Enemy? nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var e in _enemies)
        {
            float d = MathF.Sqrt((_player.X - e.X) * (_player.X - e.X) + (_player.Y - e.Y) * (_player.Y - e.Y));
            if (d < nearestDist) { nearestDist = d; nearest = e; }
        }

        if (nearest != null && nearestDist < 500)
        {
            var w = _player.Weapon;
            float angle = MathF.Atan2(nearest.Y - _player.Y, nearest.X - _player.X);
            float dmg = w.Damage * _player.BuffDmg * 0.6f;
            string ownerId = _player.IsP2 ? "p2" : "p1";
            _bullets.Add(new Bullet(
                _player.X, _player.Y - 3,
                MathF.Cos(angle) * w.BulletSpeed,
                MathF.Sin(angle) * w.BulletSpeed,
                new WeaponDef
                {
                    Name = w.Name, Id = w.Id, Damage = dmg, FireRate = w.FireRate,
                    BulletSpeed = w.BulletSpeed, Spread = w.Spread,
                    Color = w.Color, BulletW = w.BulletW, BulletH = w.BulletH,
                    Pierce = w.Pierce, Explosive = w.Explosive, ExplosionRadius = w.ExplosionRadius,
                },
                ownerId));
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.BeginFrame();
        var sb = _renderer.SpriteBatch;

        if (_state == State.Menu)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);
            _menu.Draw(_renderer);
            sb.End();
        }
        else if (_state == State.WeaponSelect)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);
            _weaponScreen.Draw(_renderer);
            sb.End();
        }
        else
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            // Background
            if (_level != null)
            {
                var th = _level.Theme;
                _renderer.DrawBackground(_renderer.Camera.OffsetX, th.Colors, th.Name);
                _renderer.DrawLevel(_level);
            }

            // Bullets
            foreach (var b in _bullets) b.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);
            
            // Enemy bullets
            foreach (var b in _enemyBullets) b.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Enemies
            foreach (var e in _enemies) e.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Explosions
            foreach (var ex in _explosions) ex.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Pickups
            foreach (var p in _pickups) p.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Players
            _player?.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);
            _player2?.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Particles
            _particles.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX, _font);
            
            // Weather (drawn on top of everything)
            _weather.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);
            
            // Lighting effects
            _lighting.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX, Config.W, Config.H);

            // Crosshairs
            if (_state == State.Playing)
                DrawCrosshairs(sb);

            sb.End();

            // HUD (separate batch for alpha blending)
            sb.Begin(samplerState: SamplerState.PointClamp);

            if (_state is State.Playing or State.Buff)
            {
                string waveLabel = _mode == "campaign"
                    ? $"{MapName()} {_mapLevel}/{MAP_LEVELS}"
                    : $"WAVE {_waveNum}";
                
                // Calculate game time in seconds
                int elapsedSec = (_gameEndTime > 0 ? _gameEndTime : Environment.TickCount - _gameStartTime) / 1000;
                
                _hud.Draw(_renderer, _player, _player2, _score, _kills, waveLabel, _mode.ToUpper(), _combo, _comboMultiplier, _level, _enemies, elapsedSec, _maxCombo, _bossKills);
            }

            // Exit hint
            if (_state == State.Playing && _mode == "campaign")
            {
                if (_player != null && !_player.Dead && _level != null)
                {
                    float dist = Math.Abs(_player.X - _level.ExitX);
                    string hint;
                    Color color;
                    
                    if (_bossLevel && _bossAlive)
                    {
                        hint = ">>>  KILL THE BOSS  <<<";
                        color = (Environment.TickCount / 400) % 2 == 0 ? Color.Red : Color.DarkRed;
                    }
                    else if (dist < 200)
                    {
                        hint = $">>>  EXIT  <<<  ({(int)dist}px)";
                        color = (Environment.TickCount / 500) % 2 == 0 ? Color.Lime : Color.Green;
                    }
                    else
                    {
                        hint = ">>>  Go Right to Exit  >>>";
                        color = Color.Yellow;
                    }
                    
                    var size = _renderer.MeasureString(hint);
                    _renderer.DrawString(hint, new Vector2(Config.W / 2f - size.X / 2, Config.H - 20), color, 0.9f);
                }
            }

            // Overlays
            switch (_state)
            {
                case State.Paused: OverlayScreen.DrawPaused(_renderer); break;
                case State.GameOver: OverlayScreen.DrawGameOver(_renderer, _kills, _score); break;
                case State.Victory: OverlayScreen.DrawVictory(_renderer, MapName(), _kills, _score); break;
                case State.Buff:
                    _buffScreen.Update();
                    _buffScreen.Draw(_renderer);
                    break;
            }

            // Damage flash effect
            if (_damageFlash > 0)
            {
                float flashAlpha = _damageFlash / 30f * 0.3f;
                _renderer.DrawRect(new Color(255, 0, 0, (int)(flashAlpha * 255)), 0, 0, Config.W, Config.H);
            }

            // Level transition animation
            if (_transitioning && _transitionTimer > 0)
            {
                float progress = _transitionTimer / 60f;
                int barHeight = (int)(Config.H * progress);
                
                // Top bar
                _renderer.DrawRect(new Color(0, 0, 0, 220), 0, 0, Config.W, barHeight);
                // Bottom bar
                _renderer.DrawRect(new Color(0, 0, 0, 220), 0, Config.H - barHeight, Config.W, barHeight);
                
                // Transition text
                if (progress > 0.3f && progress < 0.8f)
                {
                    float textAlpha = Math.Min(1f, (progress - 0.3f) * 3f) * Math.Min(1f, (0.8f - progress) * 3f);
                    string transText = _mode == "campaign" ? $"关卡 {_levelNum}" : $"WAVE {_waveNum}";
                    var textSize = _renderer.MeasureString(transText);
                    _renderer.DrawStringCentered(transText, 
                        new Vector2(Config.W / 2f, Config.H / 2f - 20), 
                        new Color(255, 200, 100, (int)(textAlpha * 255)), 1.5f);
                    
                    string subText = MapName();
                    _renderer.DrawStringCentered(subText, 
                        new Vector2(Config.W / 2f, Config.H / 2f + 20), 
                        new Color(255, 255, 255, (int)(textAlpha * 200)), 1.0f);
                }
                
                _transitionTimer--;
                if (_transitionTimer <= 0) _transitioning = false;
            }

            // Wave announcement
            if (_waveAnnounceTimer > 0)
            {
                float progress = _waveAnnounceTimer / 120f;
                float alpha = Math.Min(1f, progress * 2f) * Math.Min(1f, (1f - progress) * 2f);
                
                // Background banner
                int bannerH = 60;
                int bannerY = Config.H / 2 - bannerH / 2;
                _renderer.DrawRect(new Color(0, 0, 0, (int)(alpha * 150)), 0, bannerY, Config.W, bannerH);
                _renderer.DrawRect(new Color(255, 51, 51, (int)(alpha * 200)), 0, bannerY, Config.W, 2);
                _renderer.DrawRect(new Color(255, 51, 51, (int)(alpha * 200)), 0, bannerY + bannerH - 2, Config.W, 2);
                
                // Announcement text
                float scale = 1.2f + (1f - alpha) * 0.3f;
                _renderer.DrawStringCentered(_waveAnnounceText, 
                    new Vector2(Config.W / 2f, Config.H / 2f - 5), 
                    new Color(255, 220, 100, (int)(alpha * 255)), scale);
                
                _waveAnnounceTimer--;
            }

            sb.End();
        }

        _renderer.EndFrame();
    }

    private void DrawCrosshairs(SpriteBatch sb)
    {
        foreach (var pl in new[] { _player, _player2 })
        {
            if (pl == null || pl.Dead) continue;
            float cx = pl.X - _renderer.Camera.OffsetX + pl.Facing * 40;
            float cy = pl.Y - 2;
            var cc = pl.IsP2 ? new Color(68, 136, 255, 136) : new Color(255, 0, 0, 136);
            _renderer.DrawCrosshair(cx, cy, cc);
        }
    }
}