namespace LandoWorks.Emulation.Chip8.Interfaces
{
    public interface IFrontend
    {
        public void Run(Action<Dictionary<byte, bool>, IRenderer> Loop);
    }
}