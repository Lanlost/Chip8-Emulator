using LandoWorks.Emulation.Chip8.Interfaces;
using LandoWorks.Emulation.Chip8.Core;

namespace LandoWorks.Emulation.Chip8.Frontend
{
    public class ConsoleFrontend : IFrontend, IRenderer
    {
        //     // 1 2 3 C      1 2 3 4
        //     // 4 5 6 D  ->  q w e r
        //     // 7 8 9 E  ->  a s d f
        //     // A 0 B F      z x c v
        private static Dictionary<byte, ConsoleKey> _controlMap = new()
        {
            { 0x1, ConsoleKey.D1 },
            { 0x2, ConsoleKey.D2 },
            { 0x3, ConsoleKey.D3 },
            { 0xC, ConsoleKey.D4 },
            
            { 0x4, ConsoleKey.Q },
            { 0x5, ConsoleKey.W },
            { 0x6, ConsoleKey.E },
            { 0xD, ConsoleKey.R },
            
            { 0x7, ConsoleKey.A },
            { 0x8, ConsoleKey.S },
            { 0x9, ConsoleKey.D },
            { 0xE, ConsoleKey.F },
            
            { 0xA, ConsoleKey.Z },
            { 0x0, ConsoleKey.X },
            { 0xB, ConsoleKey.C },
            { 0xF, ConsoleKey.V }
        };

        private static Dictionary<byte, bool> _controlState = new();

        private int _debugConsoleRow = 0;
        public int DebugConsoleRow => _debugConsoleRow;

        public void Run(Action<Dictionary<byte, bool>, IRenderer> Loop)
        {
            Console.Clear();
            Console.CursorVisible = false;
            
            while(true) 
            {
                ConsoleKeyInfo? key = null;

                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true);

                    if (key?.Key == ConsoleKey.Escape) 
                        break;
                }

                UpdateControlState(key);
                Loop(_controlState, this);
            }
        }

        private void UpdateControlState(ConsoleKeyInfo? key)
        {
            foreach(var entry in _controlMap)
            {
                _controlState[entry.Key] = key.HasValue && entry.Value == key.Value.Key;
            }
        }

        // Full screen brute-force.
        public void Draw2(Display display)
        {
            //Console.Clear(); // WAYYYY slower. Use SetCursorPosition.
            Console.SetCursorPosition(0, 0);

            var column = 0;

            foreach(var b in display.Memory)
            {
                Console.Write(b == 0xFF ? "█" : " ");
                
                if (++column >= display.Width)
                {
                    column = 0;
                    Console.WriteLine();
                }
            }

        }
        
        // Striped
        // public void Draw(Display display)
        // {
        //     int lastX = 0;
        //     int lastY = 0;

        //     Console.Clear();

        //     for(var i = 0; i < display.Memory.Length; i++)
        //     {
        //         var value = display.Memory[i];
                
        //         if (value == 0xFF)
        //         {
        //             var x = i % display.Width;
        //             var y = i / display.Width; 

        //             if (lastX != x - 1) {
        //                 Console.SetCursorPosition(x, y);
        //             }
                    
        //             Console.Write('█');

        //             lastX = x;
        //             lastY = y;
        //         }
        //     }
        // }

        // Entire Scanlines
        // public void Draw(Display display)
        // {
        //     Console.SetCursorPosition(0,0);
        //     var entireRow = string.Empty;
        //     var row = 0;

        //     for(var i = 0; i < display.Memory.Length; i++)
        //     {
        //         entireRow += display.Memory[i] == 0xFF ? '█' : " ";

        //         if (entireRow.Length == display.Width)
        //         {
        //             Console.WriteLine(entireRow);
        //             entireRow = string.Empty;
        //         }
        //     }
        // }

        // Entire Dirty Scanlines
        private string[] _previousRows = [];
        
        public void Draw(Display display)
        {
            if (_previousRows.Length == 0)
                _previousRows = new string[display.Height];

            Console.SetCursorPosition(0,0);
            var entireRow = string.Empty;
            var row = 0;

            for(var i = 0; i < display.Memory.Length; i++)
            {
                entireRow += display.Memory[i] == 0xFF ? '█' : " ";

                if (entireRow.Length == display.Width)
                {
                    if (_previousRows[row] != entireRow)
                    {
                        Console.SetCursorPosition(0, row);
                        Console.WriteLine(entireRow);
                    }

                    _previousRows[row] = entireRow;
                    entireRow = string.Empty;
                    row++;
                }
            }

            // If for some reason the loop ended and never printed the last row, do it now.
            if (!string.IsNullOrEmpty(entireRow)) {
                Console.SetCursorPosition(0, row);
                Console.WriteLine(entireRow);
            }

            // For debug output to always display on bottom, no matter how many dirty rows acutally displayed...
            _debugConsoleRow = display.Height + 2;
        }

        // Full Screen
        // public void Draw(Display display)
        // {
        //     Console.SetCursorPosition(0,0);
        //     var entireScreen = string.Empty;

        //     for(var i = 0; i < display.Memory.Length; i++)
        //     {
        //         entireScreen += display.Memory[i] == 0xFF ? '█' : " ";

        //         if ((i - 10) % display.Width == 0)
        //             entireScreen += '\n';
        //     }

        //     Console.Write(entireScreen);
        // }
    }
}


// using System;

// // Ensure the console supports UTF-8 characters if rendering custom symbols
// Console.OutputEncoding = System.Text.Encoding.UTF8;

// // ANSI escape sequence prefix
// const string ESC = "\u001b[";
// const string RESET = ESC + "0m";

// // Style definitions
// const string BOLD = ESC + "1m";
// const string RED = ESC + "31m";
// const string GREEN = ESC + "32m";
// const string YELLOW = ESC + "33m";
// const string BLUE = ESC + "34m";
// const string WHITE = ESC + "37m";  
// const string BG_DARK_GRAY = ESC + "100m";

// // Outputting formatted text
// Console.WriteLine($"{BOLD}{BLUE}=== ANSI Console App Demo ==={RESET}\n");

// Console.WriteLine($"This is {RED}Red Text{RESET} and this is {GREEN}Green Text{RESET}.");
// Console.WriteLine($"You can combine {BOLD}{YELLOW}Bold Yellow{RESET} easily.");
// Console.WriteLine($"{BG_DARK_GRAY}{WHITE} This line has a dark gray background. {RESET}");

// Console.WriteLine("\nPress any key to exit...");
// Console.ReadKey();
