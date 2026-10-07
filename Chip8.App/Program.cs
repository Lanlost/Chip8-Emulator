using System;
using Raylib_cs;

namespace Chip8Interpreter
{
    public class Program
    {
        private const int WindowWidth = 800;
        private const int WindowHeight = 600;

        public static void Main()
        {
            Raylib.InitWindow(WindowWidth, WindowHeight, "Chip 8 Interpreter");
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                Raylib.DrawText("Hello, Raylib!", 10, 10, 20, Color.White);
                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}