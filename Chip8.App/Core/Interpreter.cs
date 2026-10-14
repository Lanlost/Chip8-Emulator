using System;
using System.Diagnostics;
using System.Formats.Asn1;
using Raylib_cs;

namespace LandoWorks.Emulation.Chip8.Core
{
    public class Interpreter
    {
        #region Support Config
        const bool _FX1E_OVERFLOW_VF = true; // False for COSMAC VIP, but some games require it and it shouldn't be harmful. DEFAULT: TRUE
        const bool _BNNN_OFFSET_BUG = false; // False for COSMAC VIP. CHIP-48 and SUPER-CHIP introduced this bug. DEFAULT: false.
        
        // True for COSMAC VIP. It incremented the value of index register when storing/loading registers. Modern programs do not. DEFAULT: false.
        // Use the modern behavior (false) as it will let you run the common CHIP-8 roms you find, as well as the test suites which depend on it. 
        const bool _FX55_FX65_INCREMENT_I = false;   

        // True for COSMAC VIP. For the shift left & right functions it would first copy VY into VX before shifting.
        // CHIP-48 and SUPER-CHIP shift in place. No real recommendation. Some ROMs use it, some don't! 
        const bool _8XY6_8XYE_SHIFT_VY = false;

        #endregion

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

        private readonly Stack<ushort> _stack = new(); 

        private readonly Dictionary<byte, byte> _registers = new()
        {
            { 0x0, 0x00 }, { 0x1, 0x00 }, { 0x2, 0x00 }, { 0x3, 0x00 }, 
            { 0x4, 0x00 }, { 0x5, 0x00 }, { 0x6, 0x00 }, { 0x7, 0x00 }, 
            { 0x8, 0x00 }, { 0x9, 0x00 }, { 0xA, 0x00 }, { 0xB, 0x00 }, 
            { 0xC, 0x00 }, { 0xD, 0x00 }, { 0xE, 0x00 }, { 0xF, 0x00 },
        };

        private readonly static byte[,] _font =
        {
            { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, // 0
            { 0x20, 0x60, 0x20, 0x20, 0x70 }, // 1
            { 0xF0, 0x10, 0xF0, 0x80, 0xF0 }, // 2
            { 0xF0, 0x10, 0xF0, 0x10, 0xF0 }, // 3
            { 0x90, 0x90, 0xF0, 0x10, 0x10 }, // 4
            { 0xF0, 0x80, 0xF0, 0x10, 0xF0 }, // 5
            { 0xF0, 0x80, 0xF0, 0x90, 0xF0 }, // 6
            { 0xF0, 0x10, 0x20, 0x40, 0x40 }, // 7
            { 0xF0, 0x90, 0xF0, 0x90, 0xF0 }, // 8
            { 0xF0, 0x90, 0xF0, 0x10, 0xF0 }, // 9
            { 0xF0, 0x90, 0xF0, 0x90, 0x90 }, // A
            { 0xE0, 0x90, 0xE0, 0x90, 0xE0 }, // B
            { 0xF0, 0x80, 0x80, 0x80, 0xF0 }, // C
            { 0xE0, 0x90, 0x90, 0x90, 0xE0 }, // D
            { 0xF0, 0x80, 0xF0, 0x80, 0xF0 }, // E
            { 0xF0, 0x80, 0xF0, 0x80, 0x80 }  // F
        };

        private readonly Dictionary<byte, bool> _previousControlState = []; 

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
            for(var row = 0; row < _font.GetLength(0); row++)
            {
                var rowStartIndex = FONT_ADDRESS + row * _font.GetLength(1);
                Debug.WriteLine($"Copying font chr to address 0x{rowStartIndex:X4}...");

                for(var column = 0; column < _font.GetLength(1); column++) {
                    _ram[rowStartIndex + column] = _font[row, column];
                }
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
        private bool _fxoaSuccess = false;

        public void Step(Dictionary<byte, bool> currentControlState)
        {
            var haveTicked = _timer.HasTicked();

            if (haveTicked) {
                _delayRegister = (byte)Math.Max(0, _delayRegister - 1);
                _soundRegister = (byte)Math.Max(0, _soundRegister - 1);
            }

            // Fetch
            byte b1 = _ram[_programCounter];
            byte b2 = _ram[_programCounter + 1];

            ushort bits = (ushort)(b1 << 8 | b2);

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

            var handled = false;
            var isFX0ABlocking = O == 0xF && NN == 0x0A && !_fxoaSuccess;

            if (!isFX0ABlocking) {           
                IncrementOneInstruction();
                _fxoaSuccess = false;
            }

            switch(O)
            {
                case 0x0:
                    // 00E0 (clear screen)
                    if (NNN == 0x0e0)
                    {
                        Display.Clear();
                        handled = true;
                    }
                    
                    // 00EE: [SUBROUTINE] Pops a value from the stack and jumps to it.
                    else if (NNN == 0x0ee)
                    {
                        var returnAddress = _stack.Pop();
                        _programCounter = returnAddress;
                        handled = true;
                    }

                    break;

                // 1NNN (jump)
                case 0x1:
                    _programCounter = NNN;
                    handled = true;
                    break;

                // [SUBROUTINE] Calls a subroutine (with current address added to stack)
                case 0x2: 
                    _stack.Push(_programCounter);
                    _programCounter = NNN;
                    handled = true;
                    break;

                // [SKIP CONDITIONALLY] 3XNN skips one instruction if the value in VX is equal to NN.
                case 0x3: 
                    if (_registers[X] == NN)
                        IncrementOneInstruction();

                    handled = true;
                    break;

                // [SKIP CONDITIONALLY] 4XNN skips one instruction if the value in VX is NOT equal to NN.
                case 0x4: 
                    if (_registers[X] != NN)
                        IncrementOneInstruction();

                    handled = true;
                    break;

                // [SKIP CONDITIONALLY] 5XY0 skips one instruction if the values in VX and VY are equal.
                case 0x5: 
                    if (_registers[X] == _registers[Y])
                        IncrementOneInstruction();

                    handled = true;
                    break;

                // [SKIP CONDITIONALLY] 5XY0 skips one instruction if the values in VX and VY are NOT equal.
                case 0x9: 
                    if (_registers[X] != _registers[Y])
                        IncrementOneInstruction();

                    handled = true;
                    break;

                // 6XNN (set register VX)
                case 0x6:
                    _registers[X] = NN;
                    handled = true;
                    break;

                // 7XNN (add value to register VX)
                // Does not flip VF register if it overflows.
                case 0x7:
                    _registers[X] += NN;
                    handled = true;
                    break;

                case 0x8: 
                    // [LOGIC AND ARITHMETIC OPERATIONS]
                    // On the COSMAC VIP, all of these instructions changed the value of VF. 
                    // Some of them are mentioned below. For the ones that don’t mention affecting VF, 
                    // the resulting value in VF is undefined. This is because the CHIP-8 interpreter 
                    // dispatched these instructions to the 1802 CPU’s ALU circuit, and while doing
                    // so it would change the CPU’s flag register, which always gets copied to VF.

                    // 8XY0: Set - VF Undefined.
                    if (N == 0)
                    {
                        _registers[X] = _registers[Y];
                        handled = true;
                    }

                    // 8XY1: Binary OR - VF Undefined
                    // VX is set to the bitwise/binary logical disjunction (OR) of VX and VY. VY is not affected.
                    else if(N == 1)
                    {
                        _registers[X] = (byte)(_registers[X] | _registers[Y]);
                        handled = true;
                    }

                    // 8XY2: Binary AND - VF Undefined
                    else if(N == 2)
                    {
                        _registers[X] = (byte)(_registers[X] & _registers[Y]);
                        handled = true;
                    }

                    // 8XY3: Logical XOR - VF Undefined
                    else if(N == 3)
                    {
                        _registers[X] = (byte)(_registers[X] ^ _registers[Y]);
                        handled = true;
                    }

                    // 8XY4: Add - VX is set to the value of VX plus the value of VY. VY is not affected.
                    //
                    //       Unlike 7XNN, this addition will affect the carry flag. If the result is larger
                    //       than 255 (and thus overflows the 8-bit register VX), the flag register VF is 
                    //       set to 1. If it doesn’t overflow, VF is set to 0.
                    //
                    //       NOTE: Register "X" can be VF, the carry flag itself. According to a site
                    //       which decompiled the original COSMAC VIP code the VF flag is set AFTER 
                    //       the arithmetic. 
                    // 
                    //       See:
                    //       https://laurencescotford.net/2020/07/19/chip-8-on-the-cosmac-vip-arithmetic-and-logic-instructions/
                    else if(N == 4)
                    {
                        // I'm calculating the overflow by hand, that way we aren't dependent on language
                        // specific byte overflow behavior.
                        byte distanceFromMax = (byte)(byte.MaxValue - _registers[X]);

                        if (_registers[Y] > distanceFromMax)
                        {
                            _registers[X] = (byte)(_registers[Y] - distanceFromMax - 1);
                            _registers[0xF] = 0x1; 
                        }
                        else
                        {
                            _registers[X] += _registers[Y];
                            _registers[0xF] = 0x0;
                        }

                        handled = true;
                    }

                    // 8XY5: Sets VX to the result of VX - VY. VY is unaffected. VF is also set, see "SubtractToVX".
                    else if (N == 5)
                    {
                        SubtractToVX(VX: X, minuend:_registers[X], subtrahend: _registers[Y]);
                        handled = true;
                    }

                    // 8XY7: Sets VX to the result of VY - VX. VY is unaffected. VF is also set, see "SubtractToVX".
                    else if (N == 7)
                    {
                        SubtractToVX(VX: X, minuend: _registers[Y], subtrahend:_registers[X]);
                        handled = true;
                    }

                    // 8XY6: Shift Right [Ambiguous instruction!]
                    //
                    //       In the CHIP-8 interpreter for the original COSMAC VIP, this instruction did the following: 
                    //       It put the value of VY into VX, and then shifted the value in VX 1 bit to the right (8XY6) 
                    //       or left (8XYE). VY was not affected, but the flag register VF would be set to the bit that 
                    //       was shifted out.
                    //       
                    //       However, starting with CHIP-48 and SUPER-CHIP in the early 1990s, these instructions were 
                    //       changed so that they shifted VX in place, and ignored the Y completely.
                    else if (N == 6)
                    {
                        if (_8XY6_8XYE_SHIFT_VY)
                            _registers[X] = _registers[Y];
                        
                        var shiftedBit = (byte)(_registers[X] & 0x1);
                        _registers[X] >>= 1;
                        _registers[0xF] = shiftedBit;
                        handled = true;
                    }

                    // 8XYE: Shift Left
                    else if (N == 0xE)
                    {
                        if (_8XY6_8XYE_SHIFT_VY)
                            _registers[X] = _registers[Y];
                        
                        // Even though we are shifting to the left, shift the 
                        // bit to the right and AND it with 1 to read it.
                        var shiftedBit = (byte)(_registers[X] >> 7 & 0x1);
                        _registers[X] <<= 1;
                        _registers[0xF] = shiftedBit;
                        handled = true;
                    }
                    break;

                // ANNN: (set index register I)
                case 0xA:
                    _indexRegister = NNN;
                    handled = true;
                    break;

                // BNNN: Jump with offset
                case 0xB:
                    // [!] Ambiguous instruction - 
                    //     In the original COSMAC VIP interpreter, this instruction jumped to the address NNN plus the value in the register V0. 
                    //     This was mainly used for “jump tables”, to quickly be able to jump to different subroutines based on some input.
                    //     
                    //     Starting with CHIP-48 and SUPER-CHIP, it was (probably unintentionally) changed to work as BXNN: It will jump to the
                    //     address XNN, plus the value in the register VX. So the instruction B220 will jump to address 220 plus the value in the 
                    //     register V2.
                    //
                    //     The BNNN instruction was not widely used, so you might be able to just implement the first behavior (if you pick one, 
                    //     that’s definitely the one to go with). If you want to support a wide range of CHIP-8 programs, make this “quirk” 
                    //     configurable.
                    var offset = _BNNN_OFFSET_BUG ? _registers[X] : _registers[0x0];
                    _programCounter = (ushort)(NNN + offset);
                    handled = true;
                    break;

                // CXNN: Random
                case 0xC:
                    // Upper bounds here is exclusive, so it's 256 not 255.
                    _registers[X] = (byte)(Random.Shared.Next(256) & NN);
                    handled = true;
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
                    
                    var cx = _registers[X];
                    var cy = _registers[Y];
                    
                    var address = _indexRegister;
                    var spriteHeight = N;
                    
                    for(var row = 0; row < spriteHeight; row++) 
                    {
                        for(var column = 0; column < 8; column++)
                        {   
                            var pixelOn = GetSpritePixel(_ram[address], column);
                            var wasUnset = Display.PlotPixel(cx + column, cy + row, pixelOn);

                            if (wasUnset)
                                _registers[0xF] = 0x1;
                        }

                        address += 0x1;
                    }

                    handled = true;
                    break;

                // [SKIP IF KEY]: These instructions (unlike the later FX0A) don’t wait for input.
                //                They just check if the key is currently being held down.
                case 0xE:
                {
                    // EX9E: Skip instruction if key IS pressed.
                    // EXA1: Skip instruction if key is NOT pressed. 
                    var key = _registers[X];
                    var keyIsPressed = currentControlState[key];

                    if (NN == 0x9E && keyIsPressed || NN == 0xA1 && !keyIsPressed)
                        IncrementOneInstruction();
                            
                    handled = true;
                    break;
                }

                case 0xF:
                    // FX07: [TIMER] sets VX to the current value of the delay timer
                    if (NN == 0x07)
                    {
                        _registers[X] = _delayRegister;
                        handled = true;
                    }

                    // FX15: [TIMER] sets the delay timer to the value in VX
                    else if(NN == 0x15)
                    {
                        _delayRegister = _registers[X];
                        handled = true;
                    }

                    // FX18: [TIMER] sets the sound timer to the value in VX 
                    else if(NN == 0x18)
                    {
                        _soundRegister = _registers[X];
                        handled = true;
                    }

                    // FX0A: Get Key - Blocks until key is pressed.
                    //       On the original COSMAC VIP, the key was only registered when 
                    //       it was pressed and then released.
                    else if (NN == 0x0A)
                    {
                        bool _FX0A_WAIT_FOR_RELEASE = true;

                        var key = currentControlState
                            .Where(x => {
                                return _FX0A_WAIT_FOR_RELEASE 
                                    ? _previousControlState[x.Key] && !x.Value 
                                    : x.Value;
                            })
                            .Select(x => (byte?)x.Key)
                            .FirstOrDefault();

                        if (key.HasValue) {
                            _registers[X] = key.Value;
                            _fxoaSuccess = true;
                        }
                    }

                    // FX29: Font Character
                    //       The index register I is set to the address of the hexadecimal character in VX.
                    //       An 8-bit register can hold two hexadecimal numbers, but this would only point to one character.
                    //       original COSMAC VIP interpreter just took the last nibble of VX and used that as the character.
                    else if (NN == 0x29)
                    {
                        // NOTE: It kind of baffles me that when the font only consists of 0-F that they don't
                        // just use X directly. e.g. F029 would be character 0, FF29 would be character F...
                        var chrIndex = (byte)(_registers[X] & 0x0F);
                        _indexRegister = (ushort)(FONT_ADDRESS + (chrIndex * _font.GetLength(1))); 
                        handled = true;
                    }

                    // FX1E: Add to index
                    //  The index register I will get the value in VX added to it.
                    //
                    //  Unlike other arithmetic instructions, this did not affect VF on overflow on the original COSMAC VIP. 
                    //  However, it seems that some interpreters set VF to 1 if I “overflows” from 0FFF to above 1000 (outside
                    //  the normal addressing range). This wasn’t the case on the original COSMAC VIP, at least, but apparently 
                    //  the CHIP-8 interpreter for Amiga behaved this way. At least one known game, Spacefight 2091!, relies on
                    //  this behavior. I don’t know of any games that rely on this not happening, so perhaps it’s safe to do it
                    //  like the Amiga interpreter did.
                    else if (NN == 0x1E)
                    {
                        var newValue = (ushort)(_indexRegister + _registers[X]);

                        if (_FX1E_OVERFLOW_VF && newValue >= RAM_SIZE)
                            _registers[0xF] = 1;
                        
                        _indexRegister = newValue;

                        handled = true;
                    }

                    //FX33: Binary-coded decimal conversion
                    //      This instruction is a little involved. It takes the number in VX (which is one byte, 
                    //      so it can be any number from 0 to 255) and converts it to three decimal digits, storing
                    //      these digits in memory at the address in the index register I. For example, if VX contains
                    //      156 (or 9C in hexadecimal), it would put the number 1 at the address in I, 5 in address 
                    //      I + 1, and 6 in address I + 2.
                    else if (NN == 0x33)
                    {
                        var number = _registers[X];

                        _ram[_indexRegister + 0] = (byte)(number / 100); number %= 100;          
                        _ram[_indexRegister + 1] = (byte)(number / 10);  number %= 10;
                        _ram[_indexRegister + 2] = (byte)(number / 1);

                        handled = true;
                    }
                    
                    // FX55: Store V0 -> VX in successive memory locations starting at I.
                    //       For FX55, the value of each variable register from V0 to VX inclusive 
                    //       (if X is 0, then only V0) will be stored in successive memory addresses, 
                    //       starting with the one that’s stored in I. V0 will be stored at the address 
                    //       in I, V1 will be stored in I + 1, and so on, until VX is stored in I + X.
                    //
                    // NOTE: This one, unlike FX29 one, acutally DOES use "X" directly as which register to
                    //       use, as opposed to reading _registers[X] to get the value to use...
                    //
                    else if (NN == 0x55)
                    {
                        var originalIndex = _indexRegister;

                        for(byte i = 0; i <= X; i++)
                        {
                            // Modern implementations use an index and don't modify the index register.
                            _ram[originalIndex + i] = _registers[i];

                            // Original implementations do, however. This allows either approach via flag.
                            if (_FX55_FX65_INCREMENT_I) {
                                _indexRegister++;
                            }
                        }
                        
                        handled = true;
                    }

                    else if (NN == 0x65)
                    {
                        var originalIndex = _indexRegister;

                        for(byte i = 0; i <= X; i++)
                        {
                            // Modern implementations use an index and don't modify the index register.
                            _registers[i] = _ram[originalIndex + i];

                            // Original implementations do, however. This allows either approach via flag.
                            if (_FX55_FX65_INCREMENT_I) {
                                _indexRegister++;
                            }
                        }

                        handled = true;
                    }
                    break;

                default: 
                    break;
            }

            if (!handled)
            {
                Debug.WriteLine($"Unhandled Opcode: {bits:X4}");
            }

            CopyControlState(currentControlState, _previousControlState);
        }

        // This exists just so I don't forget that an instruction is 2 bytes.
        private void  IncrementOneInstruction()
        {
            _programCounter += 0x2;
        }

        private void SubtractToVX(byte VX, byte minuend, byte subtrahend)
        {
            //      This subtraction will also affect the carry flag, but note that it’s opposite
            //      from what you might think. If the minuend (the first operand) is larger than or
            //      equal to the subtrahend (second operand), VF will be set to 1. If the subtrahend
            //      is larger, and we “underflow” the result, VF is set to 0. 
            // 
            //      Another way of thinking of it is that VF is set to 1 before the subtraction, and 
            //      then the subtraction either borrows from VF (setting it to 0) or not.
            // 
            //      NOTE NOTE NOTE:
            //      The reason, by the way, that the flag seems opposite of what it should is likely
            //      because CPUs subtract by adding the twos compliment, which in the case of a subtraction
            //      which would overflow would cause said addition to NOT carry. 
             if (minuend >= subtrahend) {
                _registers[VX] = (byte)(minuend - subtrahend);
                _registers[0xF] = 1;
            } else {
                // We're supposed to be doing: X = minuend - subtrahend, but if the subtrahend 
                // is greater than the minuend then we need to figure out how much it would wrap 
                // around, without doing so, just so we aren't tied to a specific sized data type.
                //
                // The best way to do this is to flip the values and do subtrahend - minuend, 
                // which gives us a positive number for how far into the negatives we would go. 
                // Since we can't  start from 0 though (since that would be 256) we have to subtract
                // it from 255 instead and then add one back to compensate.
                var amount = (byte)(subtrahend - minuend);
                _registers[VX] = (byte)(byte.MaxValue - amount + 1);
                _registers[0xF] = 0;
            }
        }

        /// <summary>
        /// Gets an entire row of pixels from a byte.
        /// </summary>
        /// <param name="spriteByte"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets an individual pixel from a sprite byte.
        /// </summary>
        /// <param name="spriteByte"></param>
        /// <param name="xIndex"></param>
        /// <returns></returns>
        private bool GetSpritePixel(byte spriteByte, int xIndex)
        {
            // 0xF0 = 1111 0000 → "████    "
            return (spriteByte << xIndex & 0x80) == 0x80;
        }

        private static void CopyControlState(Dictionary<byte, bool> from, Dictionary<byte, bool> to)
        {
            foreach(var entry in from)
            {
                to[entry.Key] = entry.Value;
            }
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