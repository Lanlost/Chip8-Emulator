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

/*
/// <summary>
/// Plots a pixel to x, y, unless outside the display range.
/// <paramref name="wasTurnedOff"/> is true if the pixel was 
/// turned off, indicating a collision.
/// </summary>
/// <remarks>
/// I'm using an 'out' variable instead of returning bool
/// so that the behavior is self-documented and named, 
/// ironically without a comment.
/// </remarks>
/// <param name="x"></param>
/// <param name="y"></param>
/// <param name="wasTurnedOff">
/// <returns></returns>
public void TogglePixel(int x, int y, out bool wasTurnedOff)
{
    wasTurnedOff = false;

    // Sprite starting coordinates wrap within their column/height
    // (i.e. X: 62, 63, 0, 1, Y: 30, 31, 0, 1), but individual pixels
    // never draw if they are outside the range.  
    if (x < 0 || x >= Width || y < 0 || y >= Height)
        return;

    var original = Memory[(y * Width) + x];
    Memory[(y * Width) + x] = (byte)(original ^ PIXEL_ON);

    // This is the only situation where a bit will be turned off.
    wasTurnedOff = original == PIXEL_ON;
}*/