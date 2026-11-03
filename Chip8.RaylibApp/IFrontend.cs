namespace LandoWorks.Emulation.Chip8.Interfaces
{
    public interface IFrontend
    {
        public void Run(Action<Dictionary<byte, bool>, IRenderer> Loop);

        /// <summary>
        /// Which row to start the debug info output in the console.
        /// </summary>
        public int DebugConsoleRow { get; }
    }
}