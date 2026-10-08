using System;
using System.Diagnostics;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8
{
    public class Interpreter
    {
        private const int ENTRY_POINT = 0x200;
        private const int FONT_ADDRESS = 0x50;
        private const int DISPLAY_WIDTH = 64;
        private const int DISPLAY_HEIGHT = 32;

        private byte[] ram = new Byte[4096];
        private byte[] display = new byte[DISPLAY_WIDTH * DISPLAY_HEIGHT / 8]; // Each pixel is one bit.

        private readonly Stack<byte> stack = new(); 

        private static byte[][] font =
        [
            [ 0xF0, 0x90, 0x90, 0x90, 0xF0 ], // 0
            [ 0x20, 0x60, 0x20, 0x20, 0x70 ], // 1
            [ 0xF0, 0x10, 0xF0, 0x80, 0xF0 ], // 2
            [ 0xF0, 0x10, 0xF0, 0x10, 0xF0 ], // 3
            [ 0x90, 0x90, 0xF0, 0x10, 0x10 ], // 4
            [ 0xF0, 0x80, 0xF0, 0x10, 0xF0 ], // 5
            [ 0xF0, 0x80, 0xF0, 0x90, 0xF0 ], // 6
            [ 0xF0, 0x10, 0x20, 0x40, 0x40 ], // 7
            [ 0xF0, 0x90, 0xF0, 0x90, 0xF0 ], // 8
            [ 0xF0, 0x90, 0xF0, 0x10, 0xF0 ], // 9
            [ 0xF0, 0x90, 0xF0, 0x90, 0x90 ], // A
            [ 0xE0, 0x90, 0xE0, 0x90, 0xE0 ], // B
            [ 0xF0, 0x80, 0x80, 0x80, 0xF0 ], // C
            [ 0xE0, 0x90, 0x90, 0x90, 0xE0 ], // D
            [ 0xF0, 0x80, 0xF0, 0x80, 0xF0 ], // E
            [ 0xF0, 0x80, 0xF0, 0x80, 0x80 ]  // F
        ];

        public Interpreter(string path)
        {
            var rom = File.ReadAllBytes(path);
            
            if (rom.Length + ENTRY_POINT > ram.Length) {
                Console.WriteLine("Invalid rom; too large. Exiting...");
                Environment.Exit(666);
                return;
            }

            InitializeRam(rom);
        }

        private void InitializeRam(byte[] rom)
        {
            // Copy the rom into ram at the program entry point. (0x200)
            rom.CopyTo(ram, ENTRY_POINT);

            // Copy font into ram at FONT_ADDRESS. (0x50)
            var currentAddress = FONT_ADDRESS;

            foreach(var character in font)
            {
                Debug.WriteLine($"Copying font chr, length: { character.Length } to address 0x{currentAddress:X4}...");
                character.CopyTo(ram, currentAddress);
                currentAddress += character.Length;
            }
        }

        private int delayRegister = 60;
        private int soundRegister = 60;
        private static long TICK_TIMER_FREQUENCY = Stopwatch.Frequency / 60;
        private long nextTick = TICK_TIMER_FREQUENCY;
            
        public void Start()
        {
            var timer = new Timer(60);
            timer.Start();

            var seconds = 0;
            var delayRegister = 60;

            while (seconds < 5) 
            {
                //var ticksPassed = timer.TicksPassed();
                var haveTicked = timer.HasTicked();

                if (haveTicked)
                    delayRegister -= 1;

                if (delayRegister <= 0)
                {
                    Console.WriteLine($"{++seconds} second(s)...");
                    delayRegister = 60;
                }
            }
        }

        public void ViewRam(int start, int? length = null)
        {
            var end = length.HasValue 
                ? Math.Min(ram.Length, start + length.Value)
                : ram.Length;

            for(var i = start; i < end; i+=0x10) {
                Console.WriteLine();
                Console.Write($"0x{i:X4}: ");

                var remaining = ram.Length - i;
                var rowLength = remaining >= 0x10 ? 0x10 : remaining;

                for(var rowI = 0; rowI < rowLength; rowI++) {
                    Console.Write($"{ram[rowI + i]:X2}");
                    Console.Write(" ");
                }

                Console.Write("| ");

                for(var rowI = 0; rowI < rowLength; rowI++) {
                    var chr = (char)ram[rowI + i];

                    if (chr == '\0')
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(". ");
                        Console.ForegroundColor = oldColor;
                    }  else
                    {
                        Console.Write($"{chr} ");
                    }
                }
            }
        }
    }
}


/*
public void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var seconds = 0;
            long last = 0;

            var remainingTicks = 60;

            while(++seconds < 5) 
            {
                while(remainingTicks >= 0) 
                {
                    var cur = stopwatch.ElapsedTicks % (Stopwatch.Frequency / 60); 

                    // If cur < last then we had to have looped around.
                    if (cur < last)
                        remainingTicks--;

                    last = cur;
                }

                Console.WriteLine("One second...");
                remainingTicks = 60;
            }
        }*/