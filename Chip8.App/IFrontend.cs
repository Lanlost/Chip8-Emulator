namespace Landoworks.Emulation.Chip8.Interfaces
{
    public interface IFrontend
    {
        public void Run(Action<IRenderer> Loop);
    }
}