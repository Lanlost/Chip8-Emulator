using System.Globalization;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Landoworks.Emulation.Chip8.Interfaces;
using LandoWorks.Emulation.Chip8.Core;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8.Frontend
{
    public class RaylibFrontend : IFrontend, IRenderer
    {
        private int _width;
        private int _height;

        //     // 1 2 3 C      1 2 3 4
        //     // 4 5 6 D  ->  q w e r
        //     // 7 8 9 E  ->  a s d f
        //     // A 0 B F      z x c v
        private static (byte Button, KeyboardKey Key)[] _controlMap =
        {
            ( 0x1, KeyboardKey.One   ),
            ( 0x2, KeyboardKey.Two   ),
            ( 0x3, KeyboardKey.Three ),
            ( 0xC, KeyboardKey.Four  ),
            
            ( 0x4, KeyboardKey.Q ),
            ( 0x5, KeyboardKey.W ),
            ( 0x6, KeyboardKey.E ),
            ( 0xD, KeyboardKey.R ),
            
            ( 0x7, KeyboardKey.A ),
            ( 0x8, KeyboardKey.S ),
            ( 0x9, KeyboardKey.D ),
            ( 0xE, KeyboardKey.F ),
            
            ( 0xA, KeyboardKey.Z ),
            ( 0x0, KeyboardKey.X ),
            ( 0xB, KeyboardKey.C ),
            ( 0xF, KeyboardKey.V )
        };

        private Dictionary<byte, bool> _controlState = new();

        public RaylibFrontend(int width, int height)
        {
            _width = width;
            _height = height;
        }
        
        public void Run(Action<Dictionary<byte, bool>, IRenderer> loop)
        {
            Raylib.InitWindow(_width, _height, "Chip 8 Interpreter");
            Raylib.SetTargetFPS(60);
            
            while(!Raylib.WindowShouldClose())
            {
                UpdateControlState();
                loop(_controlState, this);                
            }

            Raylib.CloseWindow();
        }

        private void UpdateControlState()
        {
            foreach(var value in _controlMap)
            {
                _controlState[value.Button] = Raylib.IsKeyDown(value.Key);
            }
        }
        
        public void Draw(Display display)
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib_cs.Color.Black);
            Raylib.BeginDrawing();

            int rectWidth = _width / display.Width;
            int rectHeight = _height / display.Height;

            for(var i = 0; i < display.Memory.Length; i++)
            {
                var value = display.Memory[i];
                var X = i % display.Width;
                var Y = i / display.Width;

                // if (value == 0xFF)
                // {
                //     var bkpt = true;
                // }

                var color = value == 0xFF 
                    ? new Color(255, 0, 0)
                    : new Color(0, 0, 0);
                    
                Raylib.DrawRectangle(
                    X * rectWidth, 
                    Y * rectHeight, 
                    rectWidth, 
                    rectHeight, 
                    new Raylib_cs.Color(color.R, color.G, color.B, color.A)
                );
            }

            Raylib.EndDrawing();
        }

        public void Plot(int x, int y, Color color)
        {
            Raylib.BeginDrawing();
            Raylib.DrawPixel(x, y, new Raylib_cs.Color(color.R, color.G, color.B, color.A));
            Raylib.EndDrawing();
        }
    }
}