using LandoWorks.Emulation.Chip8.Core;

namespace LandoWorks.Emulation.Chip8.ConsoleApp
{
    public class ConsoleFrontend
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

        private readonly Dictionary<byte, bool> _controlState = new();

        public DrawMode DrawMode { get; set; } = DrawMode.FullScreen;
        public bool CursorVisible { set => Console.CursorVisible = value; }

        #region Public Methods
        public void Run(Interpreter interpreter)
        {
            const int STEPS_PER_ITERATION = 10;
            var isConsoleAttached = IsConsoleAttached();
            
            #if DEBUG
            {
                // If no console is attached this will throw an exception.
                if (isConsoleAttached)
                    Console.Clear();
            }
            #endif

            interpreter.Start();

            interpreter.OnSoundChanged += (played) =>
            {
                if (played)
                    Console.Beep();
            };

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
                
                for(var i = 0; i < STEPS_PER_ITERATION; i++) {
                    interpreter.Step(_controlState);
                }

                Draw(interpreter.Display);

                #if DEBUG
                {
                    // If no console is attached this will throw an exception.
                    if (isConsoleAttached)
                        Console.SetCursorPosition(0, interpreter.Display.Height + 2);

                    // Even when no console is attached these will still
                    // show up in VsCode's debug console, which can be useful.
                    Console.WriteLine($"Real Seconds:  {interpreter.RealSeconds:F3}             ");
                    Console.WriteLine($"Emu Seconds:   {interpreter.EmuSeconds:F3}             ");
                    Console.WriteLine($"Diff Seconds:  {interpreter.RealSeconds - interpreter.EmuSeconds:F3}             ");
                }
                #endif
            }
        }

        public void Draw(Display display)
        {
            switch (DrawMode)
            {
                case DrawMode.BruteForce:
                    DrawBruteForce(display);
                    break;

                case DrawMode.FullScreen:
                    DrawFullScreen(display);
                    break;

                case DrawMode.Scanlines:
                    DrawScanlines(display);
                    break;

                case DrawMode.DirtyScanlines:
                    DrawDirtyScanlines(display);
                    break;

                case DrawMode.Strips:
                    DrawStrips(display);
                    break;
            }
        }
        #endregion

        #region Private Methods
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

        private void UpdateControlState(ConsoleKeyInfo? key)
        {
            foreach(var entry in _controlMap)
            {
                _controlState[entry.Key] = key.HasValue && entry.Value == key.Value.Key;
            }
        }
        #endregion

        #region [Draw Modes] Private Methods
        // --------------------------------------------------------
        // NOTE: SEE 'Docs/DrawModes.md' FOR HISTORY / EXPLANATION!  
        // --------------------------------------------------------
        
        /// <summary>
        /// Full screen, brute-force.
        /// </summary><remarks>
        /// When using "Console.CursorVisible" this looks like a starfield background. ;-)
        /// </remarks>
        /// <param name="display"></param>
        private void DrawBruteForce(Display display)
        {
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

        /// <summary><para>
        /// Draws strips of consecutive illuminated pixels. [AVOID!]
        /// </para><para>
        /// Clears the screen, then positions the cursor at the start of each strip
        /// and draws consecutive illuminated pixels without repositioning.
        /// </para></summary>
        private void DrawStrips(Display display)
        {
            int lastX = 0;

            Console.Clear();

            for(var i = 0; i < display.Memory.Length; i++)
            {
                var value = display.Memory[i];
                
                if (value == 0xFF)
                {
                    var x = i % display.Width;
                    var y = i / display.Width; 

                    if (lastX != x - 1) {
                        Console.SetCursorPosition(x, y);
                    }
                    
                    Console.Write('█');

                    lastX = x;
                }
            }
        }

        /// <summary>
        /// Full-screen in a single write. [RECOMMENDED w/ CursorVisible = false]
        /// </summary>
        /// <param name="display"></param>
        private void DrawFullScreen(Display display)
        {
            Console.SetCursorPosition(0,0);
            var entireScreen = string.Empty;

            for(var i = 0; i < display.Memory.Length; i++)
            {
                entireScreen += display.Memory[i] == 0xFF ? '█' : " ";

                if ((i + 1) % display.Width == 0)
                    entireScreen += '\n';
            }

            Console.Write(entireScreen);
        }

        /// <summary>
        /// Draws one row at a time. [RECOMMENDED w/ CursorVisible = true]
        /// </summary>
        private void DrawScanlines(Display display)
        {
            Console.SetCursorPosition(0,0);
            var entireRow = string.Empty;
            var row = 0;

            for(var i = 0; i < display.Memory.Length; i++)
            {
                entireRow += display.Memory[i] == 0xFF ? '█' : " ";

                if (entireRow.Length == display.Width)
                {
                    Console.WriteLine(entireRow);
                    entireRow = string.Empty;
                }
            }
        }

        /// <summary>
        /// Draws only changed rows, one row at a time.
        /// </summary>
        private string[] _previousRows = [];
        
        private void DrawDirtyScanlines(Display display)
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
        }
        #endregion
    }
}