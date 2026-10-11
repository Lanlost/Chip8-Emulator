using System;
using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Frontend;

namespace LandoWorks.Emulation.Chip8
{
    public class Program
    {
        public static void Main()
        {
            //interpreter.LoadRom("./roms/Space Invaders [David Winter].ch8");

            var interpreter = new Interpreter();
            interpreter.LoadRom("./roms/IBM Logo.ch8");
            interpreter.Start();

            var frontend = new RaylibFrontend(64 * 10, 32 * 10);

            frontend.Run((r) =>
            {
                interpreter.Step();
                r.Draw(interpreter.Display);
            });

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