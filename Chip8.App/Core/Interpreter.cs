using System;
using System.Diagnostics;

namespace LandoWorks.Emulation.Chip8.Core
{
    public class Interpreter
    {
        #region Private Variables
        private const ushort RAM_SIZE = 0x1000; // 4096
        private const ushort ENTRY_POINT = 0x200;
        private const ushort FONT_ADDRESS = 0x50;

        private byte[] _rom = [];
        private byte[] _ram = [];
        private Display _display;

        private ushort _programCounter;
        private ushort _indexRegister;
        private byte _delayRegister;
        private byte _soundRegister;

        private readonly Stack<ushort> stack = new(); 

        private readonly Dictionary<byte, byte> registers = new()
        {
            { 0x0, 0x00 }, { 0x1, 0x00 }, { 0x2, 0x00 }, { 0x3, 0x00 }, 
            { 0x4, 0x00 }, { 0x5, 0x00 }, { 0x6, 0x00 }, { 0x7, 0x00 }, 
            { 0x8, 0x00 }, { 0x9, 0x00 }, { 0xA, 0x00 }, { 0xB, 0x00 }, 
            { 0xC, 0x00 }, { 0xD, 0x00 }, { 0xE, 0x00 }, { 0xF, 0x00 },
        };

        private readonly static byte[][] font =
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

        private Timer _timer;
        #endregion
        
        public Display Display => _display;

        public Interpreter()
        {
            _timer = new Timer(60);
            _display = new Display(width: 64, height: 32);
        }

        public void LoadRom(string path)
        {
            _rom = File.ReadAllBytes(path);
            
            if (_rom.Length + ENTRY_POINT > RAM_SIZE) {
                throw new Exception("Invalid rom; too large.");
            }
        }

        public void Start()
        {
            InitializeRam(_rom);
            InitializeRegisters();
            _programCounter = ENTRY_POINT;
            _timer.Start();
        }

        private void InitializeRam(byte[] rom)
        {
            _ram = new byte[RAM_SIZE];
            _display.Clear();
            
            // Copy the rom into ram at the program entry point. (0x200)
            rom.CopyTo(_ram, ENTRY_POINT);

            // Copy font into ram at FONT_ADDRESS. (0x50)
            ushort currentAddress = FONT_ADDRESS;

            foreach(var character in font)
            {
                Debug.WriteLine($"Copying font chr, length: { character.Length } to address 0x{currentAddress:X4}...");
                character.CopyTo(_ram, currentAddress);
                currentAddress += (ushort)character.Length;
            }
        }

        private void InitializeRegisters()
        {
            _indexRegister = 0x00;
            _delayRegister = 0x00;
            _soundRegister = 0x00;
            _programCounter = ENTRY_POINT;
        }

        private static long TICK_TIMER_FREQUENCY = Stopwatch.Frequency / 60;
        private long nextTick = TICK_TIMER_FREQUENCY;
            
        public void Step()
        {
            //var y = Display.Height / 2;

            var firstChar = font[1];
            var y = 0;

            // for(var rowIndex = 0; rowIndex < firstChar.Length; rowIndex++)
            // {   
            //     var row = firstChar[rowIndex];

            //     var pixels = GetSpritePixels(row);

            //     for(int x = 0; x < pixels.Length; x++)
            //     {   
            //         _display.Plot(x, y, pixels[x]);
            //     }

            //     y+=2;
            // }
            
            foreach(var row in firstChar)
            {
                //var pixels = GetSpritePixels2(row);

                for(int x = 0; x < 8; x++)
                {   
                    _display.Plot(x, y, GetSpriteBit(row, x));
                }

                y+=2;
            }
            
            // for(int x = 0; x < Display.Width; x++)
            // {
            //     _display.Plot(x, y, true);
            // }
        }

        public void Step2()
        {
            var haveTicked = _timer.HasTicked();

            if (haveTicked) {
                _delayRegister = (byte)Math.Max(0, _delayRegister - 1);
                _soundRegister = (byte)Math.Max(0, _soundRegister - 1);
            }

            // Fetch
            byte b1 = _ram[_programCounter];
            byte b2 = _ram[_programCounter+1];
            ushort bits = (ushort)(b1 << 8 | b2);
            _programCounter += 0x2;

            // Decode
            // O: The first nibble. Used for opcode categories.
            // X: The second nibble. Used to look up one of the 16 registers (VX) from V0 through VF.
            // Y: The third nibble. Also used to look up one of the 16 registers (VY) from V0 through VF.
            // N: The fourth nibble. A 4-bit number.
            // NN: The second byte (third and fourth nibbles). An 8-bit immediate number.
            // NNN: The second, third and fourth nibbles. A 12-bit immediate memory address.

            byte O     = (byte)((bits & 0xF000) >> 12);
            byte X     = (byte)((bits & 0x0F00) >> 8);
            byte Y     = (byte)((bits & 0x00F0) >> 4);
            byte N     = (byte)(bits & 0x000F);
            byte NN    = (byte)(bits & 0x00FF);
            ushort NNN = (ushort)(bits & 0x0FFF);

            switch(O)
            {
                /*
                00E0 (clear screen)
                1NNN (jump)
                6XNN (set register VX)
                7XNN (add value to register VX)
                ANNN (set index register I)
                DXYN (display/draw)
                */

                case 0x0:
                    // 00E0 (clear screen)
                    if (X == 0x0 && Y == 0xE && N == 0x0)
                    {
                        // Clear screen
                    }
                    break;

                // 1NNN (jump)
                case 0x1:
                    _programCounter = NNN;
                    break;

                // 6XNN (set register VX)
                case 0x6:
                    registers[X] = NN;
                    break;

                // 7XNN (add value to register VX)
                case 0x7:
                    registers[X] += NN;
                    break;

                //ANNN (set index register I)
                case 0xA:
                    _indexRegister = NNN;
                    break;

                case 0xD:
                    /* DXYN: Display
                    This is the most involved instruction. It will draw an N pixels tall sprite 
                    from the memory location that the I index register is holding to the screen, 
                    at the horizontal X coordinate in VX and the Y coordinate in VY. All the pixels 
                    that are “on” in the sprite will flip the pixels on the screen that it is drawn 
                    to (from left to right, from most to least significant bit). If any pixels on the
                    screen were turned “off” by this, the VF flag register is set to 1. 
                    Otherwise, it’s set to 0.
                    */
                    
                    
                    var cx = registers[X];
                    var cy = registers[Y];

                    break;

                default:
                    break;
            }
        }

        private bool[] GetSpritePixels(byte spriteByte)
        {
            // 0xF0 = 1111 0000 → "████    "
            var pixels = new bool[8];

            for(var i = 0; i < pixels.Length; i++)
            {
                // 'AND' the sprite bits with '1000 0000' so only the
                // MSB exists, and if it is '1000 0000' then we know
                // that bit was on. Then shift to the left for the next
                // bit.
                var isOn = (spriteByte & 0x80) == 0x80;
                spriteByte <<=1;

                pixels[i] = isOn;
            } 

            return pixels;
        }

        private bool GetSpriteBit(byte spriteByte, int xIndex)
        {
            // 0xF0 = 1111 0000 → "████    "
            return (spriteByte << xIndex & 0x80) == 0x80;
        }

        public void ViewRam(int start, int? length = null)
        {
            var end = length.HasValue 
                ? Math.Min(_ram.Length, start + length.Value)
                : _ram.Length;

            for(var i = start; i < end; i+=0x10) {
                Console.WriteLine();
                Console.Write($"0x{i:X4}: ");

                var remaining = _ram.Length - i;
                var rowLength = remaining >= 0x10 ? 0x10 : remaining;

                for(var rowI = 0; rowI < rowLength; rowI++) {
                    Console.Write($"{_ram[rowI + i]:X2}");
                    Console.Write(" ");
                }

                Console.Write("| ");

                for(var rowI = 0; rowI < rowLength; rowI++) {
                    var chr = (char)_ram[rowI + i];

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