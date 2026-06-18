using System;
using System.Collections.Generic;
using DY01.Game;
using static DY01.Data.Config;

namespace DY01.Engine;

public static class Physics
{
    public static float ApplyGravity(float vy)
    {
        vy += GRAVITY;
        if (vy > MAX_FALL) vy = MAX_FALL;
        return vy;
    }

    public static float ApplyFriction(float vx, float friction = FRICTION, float threshold = 0.1f)
    {
        vx *= friction;
        if (Math.Abs(vx) < threshold) vx = 0;
        return vx;
    }

    public static float Accelerate(float vx, float targetSpeed, float accel, float direction)
    {
        float remaining = targetSpeed * direction - vx;
        if (Math.Abs(remaining) < 0.01f)
            return targetSpeed * direction;
        float step = accel;
        if (Math.Abs(remaining) < accel * 2)
            step = Math.Abs(remaining) * 0.3f;
        vx += step * (remaining > 0 ? 1 : -1);
        if (direction > 0 && vx > targetSpeed) vx = targetSpeed;
        else if (direction < 0 && vx < -targetSpeed) vx = -targetSpeed;
        return vx;
    }

    public static (float x, float y, float vx, float vy, bool grounded) ResolveCollision(
        float x, float y, int w, int h, float vx, float vy, Level level)
    {
        bool grounded = false;
        var tiles = level.GetTiles(x, y, w, h);
        foreach (var t in tiles)
        {
            if (!t.Solid) continue;
            float tx = t.X * TILE;
            float ty = t.Y * TILE;
            float ox = (w + TILE) / 2f - Math.Abs(x - (tx + TILE / 2f));
            float oy = (h + TILE) / 2f - Math.Abs(y - (ty + TILE / 2f));

            if (ox > 0 && oy > 0)
            {
                if (ox < oy)
                {
                    // Horizontal collision - push out sideways
                    x += (x < tx + TILE / 2f ? -ox : ox);
                    vx = 0;
                }
                else
                {
                    // Vertical collision
                    if (y < ty + TILE / 2f)
                    {
                        // Landing on top of tile - only ground if falling down
                        grounded = vy >= 0;
                        y = ty - h / 2f;
                        vy = 0;
                    }
                    else
                    {
                        // Hitting bottom of tile from below
                        y = ty + TILE + h / 2f;
                        vy = 0;
                    }
                }
            }
        }
        return (x, y, vx, vy, grounded);
    }

    public static (int? tx, int? ty) AabbVsTiles(float bx, float by, float bw, float bh, Level level)
    {
        var tiles = level.GetTiles(bx, by, bw, bh);
        foreach (var t in tiles)
        {
            if (t.Solid)
            {
                float ttx = t.X * TILE;
                float tty = t.Y * TILE;
                if (ttx < bx && bx < ttx + TILE && tty < by && by < tty + TILE)
                    return (t.X, t.Y);
            }
        }
        return (null, null);
    }

    public static float Distance(float x1, float y1, float x2, float y2)
    {
        return MathF.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }
}