using System.Security.Cryptography.X509Certificates;

namespace LandoWorks.Emulation.Chip8.Core
{
    public class Display
    {
        #region Private Constants
        private const byte PIXEL_ON = 0xFF;
        #endregion

        #region Public Properties
        public byte Width { get; }
        public byte Height { get; }
        public byte[] Memory { get; }
        #endregion

        #region Constructor(s)
        public Display(byte width, byte height)
        {
            Width = width;
            Height = height;
            Memory = new byte[width * height];
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            Array.Clear(Memory);
        }

        /// <summary>
        /// Toggles a pixel at (x, y) unless outside the display range.
        /// Returned PixelToggleStatus.TurnedOff indicates a collision.
        /// </summary>
        public PixelToggleStatus TogglePixel(int x, int y)
        {
            // Sprite starting coordinates wrap within their column/height
            // (i.e. X: 62, 63, 0, 1, Y: 30, 31, 0, 1), but individual pixels
            // never draw if they are outside the range.  
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return PixelToggleStatus.Clipped;

            var original = Memory[(y * Width) + x];
            Memory[(y * Width) + x] = (byte)(original ^ PIXEL_ON);

            return original != PIXEL_ON 
                ? PixelToggleStatus.TurnedOn
                : PixelToggleStatus.TurnedOff;
        }
        #endregion

        #region Helper Enum
        public enum PixelToggleStatus
        {
            Clipped = 0,
            TurnedOff = 1,
            TurnedOn = 2,
        }
        #endregion    
    }
}