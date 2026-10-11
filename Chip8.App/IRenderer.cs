using LandoWorks.Emulation.Chip8;
using LandoWorks.Emulation.Chip8.Core;

namespace Landoworks.Emulation.Chip8.Interfaces
{   
    public interface IRenderer
    {
        public void Draw(Display display);
        //public void Plot(int x, int y, Color color);
    }
}