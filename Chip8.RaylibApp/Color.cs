namespace LandoWorks.Emulation.Chip8
{
    public record struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(byte r, byte g, byte b, byte a = 0xFF)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            R = Convert.ToByte(r);
            G = Convert.ToByte(g);
            B = Convert.ToByte(b);
            A = Convert.ToByte(a);
        }
    }
}