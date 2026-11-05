using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Frontend;
using LandoWorks.Emulation.Chip8.Shared;

namespace LandoWorks.Emulation.Chip8.RaylibApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var options = AppSetup.ParseArguments(args);
                var rom = AppSetup.LoadRom(options.RomPath);

                var interpreter = new Interpreter();
                interpreter.LoadRom(rom);

                if (options.ViewRam)
                {
                    interpreter.ViewRam(0x0);
                    return 0;
                }

                var frontend = new RaylibFrontend(64 * 10, 32 * 10);
                frontend.Run(interpreter);
                return 0;
            }
            catch(Exception ex)
            {
                AppSetup.LogError(ex.Message);

                return ex switch
                {
                    FileNotFoundException => 666,
                    InvalidDataException => 999,
                    _ => 1
                };   
            }
        }
    }
}