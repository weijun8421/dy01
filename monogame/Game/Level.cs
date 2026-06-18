using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DY01.Engine;

namespace DY01.Game;

public class TileData
{
    public string Type = "air";
    public bool Solid;
    public int HP;
    public int X, Y;
}

public class BarrelData
{
    public float X, Y;
    public bool Ok = true;
}

public class MapTheme
{
    public string Name = "";
    public Dictionary<string, Color> Colors = new();

    public Color this[string key]
    {
        get => Colors.GetValueOrDefault(key, Color.White);
        set => Colors[key] = value;
    }
}

public class Level
{
    public List<List<TileData>> Tiles = new();
    public int W, H;
    public List<BarrelData> Barrels = new();
    public float ExitX, ExitY;
    public MapTheme Theme = new();
    private Random _rng = new();

    public static readonly string[] MAP_ORDER = { "plains", "desert", "snow", "volcano" };

    public static readonly Dictionary<string, MapTheme> MAP_THEMES = new()
    {
        ["plains"] = new MapTheme
        {
            Name = "平原",
            Colors = new()
            {
                ["sky"] = new Color(26, 26, 46),
                ["ground"] = new Color(58, 92, 62),
                ["ground_top"] = new Color(74, 124, 89),
                ["plat"] = new Color(107, 68, 35),
                ["plat_top"] = new Color(139, 101, 51),
                ["wall"] = new Color(85, 85, 85),
                ["wall_top"] = new Color(119, 119, 119),
                ["bg_hill"] = new Color(15, 15, 26),
                ["bg_build"] = new Color(21, 21, 37),
            }
        },
        ["desert"] = new MapTheme
        {
            Name = "沙地",
            Colors = new()
            {
                ["sky"] = new Color(46, 46, 26),
                ["ground"] = new Color(170, 140, 60),
                ["ground_top"] = new Color(210, 180, 80),
                ["plat"] = new Color(160, 120, 40),
                ["plat_top"] = new Color(200, 160, 60),
                ["wall"] = new Color(140, 100, 40),
                ["wall_top"] = new Color(180, 140, 60),
                ["bg_hill"] = new Color(30, 30, 15),
                ["bg_build"] = new Color(40, 35, 20),
            }
        },
        ["snow"] = new MapTheme
        {
            Name = "雪山",
            Colors = new()
            {
                ["sky"] = new Color(34, 55, 85),
                ["ground"] = new Color(200, 210, 220),
                ["ground_top"] = new Color(230, 240, 250),
                ["plat"] = new Color(170, 180, 190),
                ["plat_top"] = new Color(200, 210, 220),
                ["wall"] = new Color(140, 150, 160),
                ["wall_top"] = new Color(170, 180, 190),
                ["bg_hill"] = new Color(20, 30, 45),
                ["bg_build"] = new Color(25, 35, 50),
            }
        },
        ["volcano"] = new MapTheme
        {
            Name = "火山",
            Colors = new()
            {
                ["sky"] = new Color(46, 20, 15),
                ["ground"] = new Color(90, 40, 30),
                ["ground_top"] = new Color(130, 60, 40),
                ["plat"] = new Color(80, 35, 25),
                ["plat_top"] = new Color(120, 55, 35),
                ["wall"] = new Color(70, 30, 20),
                ["wall_top"] = new Color(110, 50, 30),
                ["bg_hill"] = new Color(25, 10, 8),
                ["bg_build"] = new Color(30, 15, 10),
            }
        },
    };

    public MapTheme GetThemeForMap(int mapIdx)
    {
        string key = MAP_ORDER[Math.Min(mapIdx, MAP_ORDER.Length - 1)];
        return MAP_THEMES[key];
    }

    public void Generate(int lv, int mapIdx = 0)
    {
        Theme = GetThemeForMap(mapIdx);
        W = 80 + lv * 6;
        H = 32;
        int gy = H - 4;
        int exitTile = W - 5;

        Tiles = new();
        for (int y = 0; y < H; y++)
        {
            var row = new List<TileData>();
            for (int x = 0; x < W; x++)
                row.Add(new TileData { Type = "air", Solid = false, HP = 0, X = x, Y = y });
            Tiles.Add(row);
        }

        // Ground profile - smoother terrain with gentle hills
        var groundProfile = new int[W];
        int currentH = gy;
        for (int x = 0; x < W; x++)
        {
            if (x > 8 && x < W - 8 && _rng.NextDouble() < 0.04)
            {
                int change = _rng.Next(3) - 1;
                currentH = Math.Clamp(currentH + change, gy - 2, gy + 1);
            }
            groundProfile[x] = currentH;
        }
        // Flat area near exit
        for (int x = exitTile - 4; x < W; x++)
            groundProfile[x] = gy;
        // Flat spawn area
        for (int x = 0; x < 10; x++)
            groundProfile[x] = gy;

        for (int x = 0; x < W; x++)
            for (int y = groundProfile[x]; y < H; y++)
            {
                // Bottom 2 rows are indestructible bedrock
                bool bedrock = y >= H - 2;
                Tiles[y][x] = new TileData { Type = bedrock ? "bedrock" : "ground", Solid = true, HP = bedrock ? 999 : 5, X = x, Y = y };
            }

        // Stairs - ensure reachable platforms
        int maxJumpTiles = 3;
        var stairZones = new List<int>();
        int zoneW = 10 + lv;
        for (int zoneStart = 12; zoneStart < W - 20; zoneStart += zoneW + _rng.Next(3, 7))
            stairZones.Add(zoneStart);

        foreach (int zx in stairZones)
        {
            if (zx > exitTile - 8) continue;
            MakeStairs(zx, gy, groundProfile, maxJumpTiles, lv);
        }

        // Floating platforms - thicker with staircase access
        for (int i = 0; i < 12 + lv * 2; i++)
        {
            int px = _rng.Next(8, W - 12);
            int baseY = groundProfile[Math.Min(px, groundProfile.Length - 1)];
            int py = baseY - _rng.Next(2, maxJumpTiles + 2);
            if (py < 3) continue;
            int pw = _rng.Next(5, 10);
            if (px + pw > exitTile - 3) px = Math.Max(8, exitTile - 3 - pw);

            bool valid = true;
            for (int x = px; x < Math.Min(px + pw, W); x++)
                if (Tiles[py][x].Solid) { valid = false; break; }
            
            if (valid && py > 3)
            {
                // Create thicker platforms (2-3 layers)
                int thickness = _rng.Next(2, 4);
                for (int layer = 0; layer < thickness; layer++)
                {
                    int layerY = py + layer;
                    if (layerY >= H) break;
                    for (int x = px; x < Math.Min(px + pw, W); x++)
                    {
                        if (!Tiles[layerY][x].Solid)
                            Tiles[layerY][x] = new TileData { Type = "plat", Solid = true, HP = 2, X = x, Y = layerY };
                    }
                }
                
                // Add staircase on one side
                bool stairOnLeft = _rng.NextDouble() < 0.5;
                int stairStartX = stairOnLeft ? px - 3 : px + pw;
                int stairDir = stairOnLeft ? 1 : -1;
                
                // Create 3-step staircase
                for (int step = 0; step < 3; step++)
                {
                    int stepX = stairStartX + step * stairDir;
                    int stepY = py + thickness + step;
                    if (stepX >= 0 && stepX < W && stepY < H)
                    {
                        for (int x = stepX; x < Math.Min(stepX + 2, W); x++)
                        {
                            if (!Tiles[stepY][x].Solid)
                                Tiles[stepY][x] = new TileData { Type = "plat", Solid = true, HP = 2, X = x, Y = stepY };
                        }
                    }
                }
            }
        }

        // Walls - cover positions
        for (int i = 0; i < 5 + lv / 2; i++)
        {
            int wx = _rng.Next(12, W - 15);
            if (Math.Abs(wx - exitTile) < 6) continue;
            int baseY = groundProfile[Math.Min(wx, groundProfile.Length - 1)];
            int wy = baseY - _rng.Next(2, 5);
            int wh = Math.Min(_rng.Next(2, 5), baseY - wy);
            if (wh < 2) continue;
            for (int y = wy; y < wy + wh; y++)
            {
                if (y >= 0 && y < H && wx < W)
                {
                    if (!Tiles[y][wx].Solid)
                        Tiles[y][wx] = new TileData { Type = "wall", Solid = true, HP = 5, X = wx, Y = y };
                    if (wx + 1 < W && !Tiles[y][wx + 1].Solid)
                        Tiles[y][wx + 1] = new TileData { Type = "wall", Solid = true, HP = 5, X = wx + 1, Y = y };
                }
            }
        }

        // Barrels
        Barrels = new();
        for (int i = 0; i < 4 + lv; i++)
        {
            int bx = _rng.Next(10, W - 8);
            int by = _rng.Next(gy - 8, gy - 1);
            if (!Tiles[by][bx].Solid)
                Barrels.Add(new BarrelData { X = bx * Data.Config.TILE + 8, Y = by * Data.Config.TILE + 8, Ok = true });
        }

        // Exit - clear area around exit
        ExitX = exitTile * Data.Config.TILE;
        ExitY = (gy - 1) * Data.Config.TILE;
        for (int y = gy - 4; y < gy; y++)
            for (int x = exitTile - 3; x < exitTile + 4; x++)
                if (y >= 0 && y < H && x >= 0 && x < W)
                    if (Tiles[y][x].Type is "wall" or "plat")
                        Tiles[y][x] = new TileData { Type = "air", Solid = false, HP = 0, X = x, Y = y };
    }

    private void MakeStairs(int startX, int gy, int[] groundProfile, int maxJumpTiles, int lv)
    {
        bool goingUp = _rng.NextDouble() < 0.6;
        int steps = _rng.Next(3, Math.Max(4, 6 + lv / 2) + 1);
        int cx = startX;
        int cy = groundProfile[Math.Min(cx, groundProfile.Length - 1)];

        for (int i = 0; i < steps; i++)
        {
            if (cx >= W - 10) break;
            if (cy < 4) break;

            if (goingUp && _rng.NextDouble() < 0.7) cy -= _rng.Next(1, maxJumpTiles + 1);
            else if (!goingUp && _rng.NextDouble() < 0.5 && cy < gy) cy += _rng.Next(1, 3);

            if (cy < gy - 8) cy = gy - 8;
            if (cy < 3) cy = 3;

            int pw = _rng.Next(3, 7);
            for (int x = cx; x < Math.Min(cx + pw, W); x++)
                if (!Tiles[cy][x].Solid)
                    Tiles[cy][x] = new TileData { Type = "plat", Solid = true, HP = 2, X = x, Y = cy };

            cx += pw + _rng.Next(1, 4);
        }
    }

    public void DamageTile(int tx, int ty, int dmg, ParticleSystem particles)
    {
        if (ty < 0 || ty >= Tiles.Count || tx < 0 || tx >= Tiles[0].Count) return;
        var t = Tiles[ty][tx];
        if (!t.Solid) return;
        t.HP -= dmg;
        if (t.HP <= 0)
        {
            t.Solid = false;
            t.Type = "air";
            particles.SpawnBurst(tx * Data.Config.TILE + 8, ty * Data.Config.TILE + 8, 5, 3,
                Theme["ground"], (15, 30), (2, 5), (-4, 0));
        }
    }

    public void Explode(float cx, float cy, float radius, int dmg, ParticleSystem particles,
        List<Entities.Explosion> explosions, List<BarrelExplosion> barrelExplosions)
    {
        int cxt = (int)(cx / Data.Config.TILE);
        int cyt = (int)(cy / Data.Config.TILE);
        int r = (int)Math.Ceiling(radius / Data.Config.TILE);
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (Math.Sqrt(dx * dx + dy * dy) <= r)
                    DamageTile(cxt + dx, cyt + dy, dmg, particles);

        foreach (var b in Barrels)
        {
            if (!b.Ok) continue;
            if (Math.Sqrt((cx - b.X) * (cx - b.X) + (cy - b.Y) * (cy - b.Y)) < radius + 20)
            {
                b.Ok = false;
                barrelExplosions.Add(new BarrelExplosion
                {
                    Timer = 9,
                    X = b.X, Y = b.Y,
                    Radius = 50, Damage = 40,
                });
            }
        }
    }

    // Reusable buffer for tile queries to reduce allocations
    private static TileData[] _tileBuffer = new TileData[256];
    private static int _tileBufferCount = 0;

    public ReadOnlySpan<TileData> GetTiles(float x, float y, float w, float h)
    {
        _tileBufferCount = 0;
        int sx = (int)((x - w / 2) / Data.Config.TILE);
        int ex = (int)((x + w / 2) / Data.Config.TILE);
        int sy = (int)((y - h / 2) / Data.Config.TILE);
        int ey = (int)((y + h / 2) / Data.Config.TILE);

        for (int ty = sy; ty <= ey; ty++)
        {
            for (int tx = sx; tx <= ex; tx++)
            {
                if (ty >= 0 && ty < Tiles.Count && tx >= 0 && tx < Tiles[0].Count)
                {
                    if (_tileBufferCount < _tileBuffer.Length)
                    {
                        var src = Tiles[ty][tx];
                        // Reuse buffer slot if possible, otherwise create new
                        if (_tileBuffer[_tileBufferCount] == null)
                            _tileBuffer[_tileBufferCount] = new TileData();
                        var t = _tileBuffer[_tileBufferCount];
                        t.Type = src.Type;
                        t.Solid = src.Solid;
                        t.HP = src.HP;
                        t.X = tx;
                        t.Y = ty;
                        _tileBufferCount++;
                    }
                }
            }
        }
        return new ReadOnlySpan<TileData>(_tileBuffer, 0, _tileBufferCount);
    }
}

public class BarrelExplosion
{
    public int Timer;
    public float X, Y, Radius, Damage;
}