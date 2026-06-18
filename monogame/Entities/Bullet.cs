using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;

namespace DY01.Entities;

public class Bullet
{
    public float X, Y, Vx, Vy;
    public WeaponDef Weapon;
    public string OwnerId;
    public int Life = 80;
    public HashSet<Enemy> Hits = new();

    // Trail buffer - fixed size to avoid allocations
    private float[] _trailX = new float[4];
    private float[] _trailY = new float[4];
    private int _trailCount = 0;
    private int _trailIndex = 0;

    public Bullet(float x, float y, float vx, float vy, WeaponDef weapon, string ownerId)
    {
        X = x; Y = y; Vx = vx; Vy = vy; Weapon = weapon; OwnerId = ownerId;
    }

    public bool Update()
    {
        // Circular buffer for trail
        _trailX[_trailIndex] = X;
        _trailY[_trailIndex] = Y;
        _trailIndex = (_trailIndex + 1) % 4;
        if (_trailCount < 4) _trailCount++;

        X += Vx; Y += Vy;
        Life--;
        return Life > 0 && X > -200 && X < 6000 && Y > -200 && Y < 800;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        var w = Weapon;

        // Draw trail from circular buffer
        for (int i = 0; i < _trailCount; i++)
        {
            int idx = (_trailIndex - _trailCount + i + 4) % 4;
            float a = (float)i / _trailCount * 0.35f;
            var c = w.Color * a;
            sb.Draw(pixel, new Rectangle((int)(_trailX[idx] - camX), (int)_trailY[idx], w.BulletW + 2, w.BulletH + 2), c);
        }

        int sx = (int)(X - camX), sy = (int)Y;
        if (w.Explosive)
        {
            sb.Draw(pixel, new Rectangle(sx - 3, sy - 3, 6, 6), w.Color);
            sb.Draw(pixel, new Rectangle(sx - 2, sy - 2, 4, 4), new Color(255, 136, 68));
        }
        else
        {
            sb.Draw(pixel, new Rectangle(sx, sy, w.BulletW, w.BulletH), w.Color);
        }
    }
}