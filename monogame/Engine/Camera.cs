using System;
using Microsoft.Xna.Framework;
using DY01.Data;

namespace DY01.Engine;

public class Camera
{
    public float X { get; set; }
    public float TargetX { get; set; }
    public float ShakeAmount { get; set; }
    public float ShakeX { get; private set; }
    public float ShakeY { get; private set; }
    private Random _rng = new();

    public float OffsetX => X + ShakeX;
    public float OffsetY => ShakeY;

    public void Reset()
    {
        X = 0; TargetX = 0; ShakeAmount = 0; ShakeX = 0; ShakeY = 0;
    }

    public void Follow(float targetX, float smooth = 0.07f)
    {
        TargetX = targetX - Config.W / 2f;
        X += (TargetX - X) * smooth;
    }

    public void Clamp(float minX, float maxX)
    {
        if (X < minX) X = minX;
        if (X > maxX) X = maxX;
    }

    public void AddShake(float amount)
    {
        ShakeAmount = Math.Max(ShakeAmount, amount);
    }

    public void Update()
    {
        if (ShakeAmount > 0)
        {
            ShakeX = (float)(_rng.NextDouble() * 2 - 1) * ShakeAmount;
            ShakeY = (float)(_rng.NextDouble() * 2 - 1) * ShakeAmount;
            ShakeAmount *= 0.88f;
            if (ShakeAmount < 0.4f)
            {
                ShakeAmount = 0;
                ShakeX = 0;
                ShakeY = 0;
            }
        }
        else
        {
            ShakeX = 0;
            ShakeY = 0;
        }
    }
}