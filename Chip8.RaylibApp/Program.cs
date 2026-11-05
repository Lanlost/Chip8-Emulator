using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Frontend;

/*
    interpreter.LoadRom("./roms/IBM Logo.ch8");
    interpreter.LoadRom("./roms/test_opcode.ch8");
    interpreter.LoadRom("./roms/pong.ch8");
*/

namespace LandoWorks.Emulation.Chip8
{
    public class Program
    {
        const int STEPS_PER_SECOND = 10;

        public static async Task Main(string[] args)
        {
            var options = ParseArguments(args);

            if (string.IsNullOrWhiteSpace(options.RomPath))
                options.RomPath = Path.Combine(AppContext.BaseDirectory, "roms", "Space Invaders [David Winter].ch8");

            var interpreter = new Interpreter();
            interpreter.LoadRom(options.RomPath);
            
            if (options.ViewRam)
            {
                interpreter.Initialize();
                interpreter.ViewRam(0x0);
                return;
            }

            var frontend = new RaylibFrontend(64 * 10, 32 * 10);
            frontend.Run(interpreter);
        }

        private static (string RomPath, bool ViewRam) ParseArguments(string[] args)
        {
		    var romPath = "";
            var viewRam = false;
		
            foreach(var arg in args)
            {
                if (arg.ToLower() == "--ram") {
                    viewRam = true;
                } else {
                    romPath = arg;
                }
            }

            return (romPath, viewRam);
        }
    }
}