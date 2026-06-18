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
    private SpriteFont? _font;

    // UI
    private MenuScreen _menu = null!;
    private HUD _hud = null!;
    private BuffSelectScreen _buffScreen = null!;

    // Game state
    private enum State { Menu, Playing, Paused, Buff, GameOver, Victory }
    private State _state = State.Menu;
    private string _mode = "campaign";
    private Player? _player;
    private Player? _player2;
    private Level? _level;
    private List<Enemy> _enemies = new();
    private List<Bullet> _bullets = new();
    private List<Explosion> _explosions = new();
    private List<BarrelExplosion> _barrelExplosions = new();
    private int _score, _kills, _levelNum = 1, _waveNum = 1, _waveRemain;
    private bool _waveDone;
    private int _hitstop;
    private List<BuffDef> _buffChoices = new();
    private Random _rng = new();

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
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        switch (_state)
        {
            case State.Menu:
                UpdateMenu();
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
            ShowBuffs();
        }
    }

    private void UpdatePlaying()
    {
        if (_input.Pressed("escape")) { _state = State.Paused; return; }

        if (_hitstop > 0) { _hitstop--; return; }

        // Check death
        bool p1Dead = _player != null && _player.Dead && _player.RespawnTimer <= 0;
        bool p2Dead = _player2 == null || (_player2.Dead && _player2.RespawnTimer <= 0);
        if (_mode == "coop" && p1Dead && p2Dead) { _state = State.GameOver; return; }
        else if (_mode != "coop" && p1Dead) { _state = State.GameOver; return; }

        // Update players
        var keys = _input.KeyStates;

        var p1Weps = _input.GetWeaponSwitch(0);
        _player?.Update(keys, _level!, _bullets, _particles, _renderer.Camera);
        if (_player != null)
            foreach (var wi in p1Weps) _player.SwitchWeapon(wi);

        if (_player2 != null)
        {
            var p2Weps = _input.GetWeaponSwitch(1);
            _player2.Update(keys, _level!, _bullets, _particles, _renderer.Camera);
            foreach (var wi in p2Weps) _player2.SwitchWeapon(wi);
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
            bool dead = !e.Update(_player, _player2, _level!, _particles, _renderer.Camera) || e.Hp <= 0;
            if (dead && wasBoss) _bossAlive = false;
            return dead;
        });

        if (_waveRemain > 0 && _enemies.Count < 7)
            SpawnEnemies(Math.Min(3, _waveRemain));

        // Turret
        UpdateTurret();

        // Particles and explosions
        _particles.Update();
        _explosions.RemoveAll(e => !e.Update());
        ApplyExplosionDamage();

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

    private string MapName()
    {
        if (_mapIdx < Level.MAP_ORDER.Length)
            return Level.MAP_THEMES[Level.MAP_ORDER[_mapIdx]].Name;
        return "??";
    }

    private void Begin()
    {
        _state = State.Playing;
        _score = Math.Max(0, _score);
        _waveDone = false;

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
        }
        else
            _player = new Player(spawnTileX * Config.TILE, groundY - 16, false);

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
            }
            else
                _player2 = new Player((spawnTileX + 3) * Config.TILE, groundY - 16, true);
        }
        else
            _player2 = null;

        _enemies.Clear();
        _bullets.Clear();
        _particles.Clear();
        _explosions.Clear();
        _barrelExplosions.Clear();
        _renderer.Camera.Reset();
        _hitstop = 0;
        SpawnWave();
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
                else if (roll < 0.28) etype = "elite";
                else if (roll < 0.42) etype = "flyer";
            }
            else if (_levelNum > 3 || _mapIdx > 0)
            {
                if (roll < 0.15) etype = "elite";
                else if (roll < 0.28) etype = "flyer";
                else if (roll < 0.35 && _levelNum > 5) etype = "heavy";
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
            _player = new Player(80, gy * Config.TILE - 16, false);
            if (_mode == "coop") _player2 = new Player(120, gy * Config.TILE - 16, true);
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

        Begin();
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
                        _score += e.Score;
                        var owner = b.OwnerId == "p1" ? _player : _player2;
                        if (owner != null && owner.BuffVampire > 0) owner.Heal(owner.BuffVampire);
                        _renderer.Camera.AddShake(4);
                        _hitstop = 3;
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
                        _score += e.Score;
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
        else
        {
            sb.Begin(samplerState: SamplerState.PointClamp);

            // Background
            if (_level != null)
            {
                var th = _level.Theme;
                _renderer.DrawBackground(_renderer.Camera.OffsetX, th.Colors);
                _renderer.DrawLevel(_level);
            }

            // Bullets
            foreach (var b in _bullets) b.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Enemies
            foreach (var e in _enemies) e.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Explosions
            foreach (var ex in _explosions) ex.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Players
            _player?.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);
            _player2?.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX);

            // Particles
            _particles.Draw(sb, _renderer.Pixel, _renderer.Camera.OffsetX, _font);

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
                _hud.Draw(_renderer, _player, _player2, _score, _kills, waveLabel, _mode.ToUpper());
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