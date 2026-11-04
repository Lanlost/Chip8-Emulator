namespace LandoWorks.Emulation.Chip8.ConsoleApp
{
    /// <summary>
    /// Console rendering modes.
    /// See <see href="../Docs/DrawModes.md">Draw Modes - Notes</see>.
    /// </summary>
    public enum DrawMode
    {
        /// <summary>
        /// Draws every pixel individually.
        /// </summary>
        BruteForce,

        /// <summary>
        /// Draws successive pixels without setting position. [AVOID!]
        /// Super flickery due to needing to clear the screen every time.
        /// </summary>
        Strips,

        /// <summary>
        /// Draws the entire screen in a single write. [RECOMMENDED w/ CursorVisible = false]
        /// </summary>
        FullScreen,

        /// <summary>
        /// Draws each line individually. [RECOMMENDED w/ CursorVisible = true]
        /// </summary>
        Scanlines,

        /// <summary>
        /// Draws each line individually, but only if it changed.
        /// </summary>
        DirtyScanlines
    }
}