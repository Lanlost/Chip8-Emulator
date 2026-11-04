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
            interpreter.Initialize();
            
            if (options.ViewRam)
            {
                interpreter.ViewRam(0x0);
                return;
            }

            var isConsoleAttached = IsConsoleAttached();
            var frontend = new RaylibFrontend(64 * 10, 32 * 10);

            #if DEBUG
            {
                // If no console is attached this will throw an exception.
                if (isConsoleAttached)
                    Console.Clear();
            }
            #endif

            interpreter.Start();
            frontend.Run((keys, renderer) =>
            {
                for(var i = 0; i < STEPS_PER_SECOND; i++) {
                    interpreter.Step(keys);
                }

                renderer.Draw(interpreter.Display);

                #if DEBUG
                {
                    // If no console is attached this will throw an exception.
                    if (isConsoleAttached)
                        Console.SetCursorPosition(0, 0);

                    // Even when no console is attached these will still
                    // show up in VsCode's debug console, which can be useful.
                    Console.WriteLine($"Real Seconds:  {interpreter.RealSeconds:F3}             ");
                    Console.WriteLine($"Emu Seconds:   {interpreter.EmuSeconds:F3}             ");
                    Console.WriteLine($"Diff Seconds:  {interpreter.RealSeconds - interpreter.EmuSeconds:F3}             ");
                }
                #endif
            });
        }

        /// <summary>
        /// OS independent hack to detect if a console is actually attached or not.
        /// </summary>
        private static bool IsConsoleAttached()
        {
            try
            {
                return Console.WindowHeight > 0;
            }
            catch (IOException)
            {
                return false;
            }
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