using System.Text;
using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Shared;

namespace LandoWorks.Emulation.Chip8.ConsoleApp
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

                var frontend = new ConsoleFrontend();

                if (IsSpaceInvadersRom(rom))
                {
                    frontend.DrawMode = DrawMode.BruteForce;
                    frontend.CursorVisible = true;
                } 
                else
                {
                    frontend.DrawMode = DrawMode.FullScreen;
                    frontend.CursorVisible = false;
                }

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
        
        private static bool IsSpaceInvadersRom(byte[] rom)
        {
            const string SPACE_INVADERS_ID = "SPACE INVADERS";
            return rom.Length >= 0x02 + SPACE_INVADERS_ID.Length && Encoding.ASCII.GetString(rom, 0x02, 14) == SPACE_INVADERS_ID;
        }
    }
}