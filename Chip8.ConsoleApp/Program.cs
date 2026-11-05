using System.Text;
using LandoWorks.Emulation.Chip8.Core;

namespace LandoWorks.Emulation.Chip8.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var options = ParseArguments(args);

            if (string.IsNullOrWhiteSpace(options.RomPath))
                options.RomPath = Path.Combine(AppContext.BaseDirectory, "roms", "Space Invaders [David Winter].ch8");

            var interpreter = new Interpreter();
            var rom = interpreter.LoadRom(options.RomPath);
            
            if (options.ViewRam)
            {
                interpreter.Initialize();
                interpreter.ViewRam(0x0);
                return;
            }

            var frontend = new ConsoleFrontend();

            if (IsSpaceInvadersRom(rom))
            {
                frontend.DrawMode = DrawMode.BruteForce;
                frontend.CursorVisible = true;
            } else
            {
                frontend.DrawMode = DrawMode.FullScreen;
                frontend.CursorVisible = false;
            }

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

        private static bool IsSpaceInvadersRom(byte[] rom)
        {
            const string SPACE_INVADERS_ID = "SPACE INVADERS";
            return rom.Length >= 0x02 + SPACE_INVADERS_ID.Length && Encoding.ASCII.GetString(rom, 0x02, 14) == SPACE_INVADERS_ID;
        }
    }
}