namespace LandoWorks.Emulation.Chip8.Shared
{
    public static class AppSetup
    {
        private const string DEFAULT_ROM = "Space Invaders [David Winter].ch8";
        private const string ERROR_LOG_NAME = "error.log";

        public static (string RomPath, bool ViewRam) ParseArguments(string[] args)
        {   
            var romPath = "";
            var viewRam = false;

            foreach (var arg in args)
            {
                if (arg.Equals("--ram", StringComparison.OrdinalIgnoreCase))
                    viewRam = true;
                else
                    romPath = arg;
            }

            return (romPath, viewRam);
        }

        public static byte[] LoadRom(string romPath)
        {
            if (string.IsNullOrWhiteSpace(romPath))
                romPath = Path.Combine(AppContext.BaseDirectory, "roms", DEFAULT_ROM);

            if (!File.Exists(romPath))
                throw new FileNotFoundException($"ROM file not found: {romPath}", romPath);

            var rom = File.ReadAllBytes(romPath);
            return rom;
        }

        public static void LogError(string message)
        {
            try
            {
                Console.Error.WriteLine(message);

                var path = Path.Combine(AppContext.BaseDirectory, ERROR_LOG_NAME);
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}