using System;
using System.IO;

try
{
    Console.WriteLine("DY01 starting...");
    using var game = new DY01.GameMain();
    Console.WriteLine("Game created, running...");
    game.Run();
    Console.WriteLine("Game exited normally.");
}
catch (Exception ex)
{
    var msg = $"EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
    Console.WriteLine(msg);
    try { File.WriteAllText("crash.log", msg); } catch { }
}
