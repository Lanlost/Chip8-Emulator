using System.Security.Cryptography.X509Certificates;

namespace LandoWorks.Emulation.Chip8.Core
{
    public class Display
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Memory { get; }

        public Display(int width, int height)
        {
            Width = width;
            Height = height;
            Memory = new byte[width * height];
        }

        public void Clear()
        {
            Array.Clear(Memory);
        }

        public void Plot(int x, int y, bool on)
        {
            // We have to cast to (byte) when using a ternary. Fun..
            Memory[(y * Width) + x] = (byte)(on ? 0xFF: 0x00);
        }
    }
}