using System;
using LandoWorks.Emulation.Chip8;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8
{
    public class Program
    {
        public static void Main()
        {
            var interpreter = new Interpreter("Space Invaders [David Winter].ch8");
            interpreter.Start();
            //interpreter.ViewRam(0x0);
        }
    }
}


// Raylib.InitWindow(WindowWidth, WindowHeight, "Chip 8 Interpreter");
// Raylib.SetTargetFPS(60);

// while (!Raylib.WindowShouldClose())
// {
//     Raylib.BeginDrawing();
//     Raylib.ClearBackground(Color.Black);
//     Raylib.DrawText("Hello, Raylib!", 10, 10, 20, Color.White);
//     Raylib.EndDrawing();
// }

// Raylib.CloseWindow();