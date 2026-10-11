using Landoworks.Emulation.Chip8.Interfaces;
using LandoWorks.Emulation.Chip8.Core;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8.Frontend
{
    public class RaylibFrontend : IFrontend, IRenderer
    {
        private int _width;
        private int _height;

        //private int _rectWidth;
        //private int _rectHeight;

        public RaylibFrontend(int width, int height)
        {
            _width = width;
            _height = height;
        }
        
        public void Run(Action<IRenderer> loop)
        {
            Raylib.InitWindow(_width, _height, "Chip 8 Interpreter");
            Raylib.SetTargetFPS(60);
            
            while(!Raylib.WindowShouldClose())
            {
                loop(this);                
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