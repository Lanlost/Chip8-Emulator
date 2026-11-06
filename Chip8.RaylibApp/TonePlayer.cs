using Raylib_cs;

namespace LandoWorks.Emulation.Chip8.RaylibApp
{
    public unsafe class TonePlayer : IDisposable
    {
        private readonly AudioStream _stream;
        private readonly short[] _samples;
        private bool _playing;

        private const int SAMPLE_RATE = 44100;

        public TonePlayer(int frequency = 440)
        {
            Raylib.InitAudioDevice();

            _stream = Raylib.LoadAudioStream(SAMPLE_RATE, 16, 1);

            _samples = new short[4096];

            for (int i = 0; i < _samples.Length; i++)
            {
                double t = (double)i / SAMPLE_RATE;
                _samples[i] = (short)(Math.Sin(2 * Math.PI * frequency * t) * 10000);
            }
        }

        public void Play()
        {
            if (_playing) {
                return;
            }

            _playing = true;
            Raylib.PlayAudioStream(_stream);
        }

        public void Stop()
        {
            _playing = false;
            Raylib.StopAudioStream(_stream);
        }

        public void Update()
        {
            if (!_playing || !Raylib.IsAudioStreamProcessed(_stream))
                return;

            fixed (short* ptr = _samples)
            {
                Raylib.UpdateAudioStream(_stream, ptr, _samples.Length);
            }
        }

        public void Dispose()
        {
            Raylib.UnloadAudioStream(_stream);
            Raylib.CloseAudioDevice();
        }
    }
}