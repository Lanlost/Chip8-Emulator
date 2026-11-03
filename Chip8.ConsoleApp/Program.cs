using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Frontend;

namespace LandoWorks.Emulation.Chip8
{
    public class Program
    {
        const int STEPS_PER_SECOND = 10;

        public static async Task Main()
        {
            var interpreter = new Interpreter();
            //interpreter.LoadRom("./roms/IBM Logo.ch8");
            //interpreter.LoadRom("./roms/test_opcode.ch8");
            //interpreter.LoadRom("./roms/pong.ch8");
            interpreter.LoadRom("./roms/Space Invaders [David Winter].ch8");
            
            interpreter.Start();

            var frontend = new ConsoleFrontend();

            Console.Clear();

            frontend.Run((keys, renderer) =>
            {
                for(var i = 0; i < STEPS_PER_SECOND; i++) {
                    interpreter.Step(keys);
                }

                renderer.Draw(interpreter.Display);

                Console.SetCursorPosition(0, frontend.DebugConsoleRow);
                Console.WriteLine($"Real Seconds:  {interpreter.RealSeconds}             ");
                Console.WriteLine($"Emu Seconds:   {interpreter.EmuSeconds}             ");
                Console.WriteLine($"Diff Seconds:  {interpreter.RealSeconds - interpreter.EmuSeconds:F3}             ");
            });

            //interpreter.ViewRam(0x0);
        }

    }
}