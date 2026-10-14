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

        /// <summary>
        /// Plots a pixel to x, y. Returns if it was turned off.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public bool PlotPixel(int x, int y, bool on)
        {
            try {
                var original = Memory[(y * Width) + x];
                var now = (byte)(on ? 0xFF: 0x00);

                Memory[(y * Width) + x] = (byte)(original ^ now);

                // This is the only situation where a bit will be turned off.
                return original == 0xFF && on;
            }
            catch
            {
                return false;
            }
        }
    }
}