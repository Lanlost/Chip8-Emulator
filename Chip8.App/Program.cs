using System;
using LandoWorks.Emulation.Chip8.Core;
using LandoWorks.Emulation.Chip8.Frontend;

namespace LandoWorks.Emulation.Chip8
{
    public class Program
    {
        const int STEPS_PER_SECOND = 10;

        public static void Main()
        {

            var interpreter = new Interpreter();
            //interpreter.LoadRom("./roms/IBM Logo.ch8");
            //interpreter.LoadRom("./roms/test_opcode.ch8");
            //interpreter.LoadRom("./roms/Space Invaders [David Winter].ch8");
            interpreter.LoadRom("./roms/pong.ch8");
            
            interpreter.Start();

            var frontend = new RaylibFrontend(64 * 10, 32 * 10);

            frontend.Run((r) =>
            {
                for(var i = 0; i < STEPS_PER_SECOND; i++) {
                    interpreter.Step();
                }

                r.Draw(interpreter.Display);
            });

            //interpreter.ViewRam(0x0);
        }
    }
}