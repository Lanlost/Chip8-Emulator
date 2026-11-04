# Console Rendering Modes
This explains why the different draw modes exist, for future reference.

### BruteForce
Initially when writing this I did not know about the "CursorVisible" setting. I cleared the screen and then wrote each pixel individually, which was incredibly slow and flickery. This was largely mitigated by simply using `SetCursorPosition(0,0)` instead of clearing the screen, however this then gave a noisy 'snow' like appearance from the visible cursor randomly displaying during drawing.

I tried to mitigate this by using various methods:

### Strips
I tried drawing only illuminated pixels using `SetCursorPosition`, then drawing successive illuminated pixels without repositioning the cursor.

Unfortunately, this required clearing the screen every frame to remove old pixels, making it the worst option due to flickering.

### Scanlines
The first breakthrough was constructing each row as a string and writing it with a single `Console.Write`.

This produced a much more stable image. Cursor noise remained, but was confined to a thin strip along the left edge of the display.

### BruteForce
Initially when writing this I did not know about the "CursorVisible" setting. I cleared the screen and then wrote each pixel individually, which was incredibly slow and flickery. This was largely mitigated by simply using `SetCursorPosition(0,0)` instead of clearing the screen, however this then gave a noisy 'snow' like appearance from the visible cursor randomly displaying during drawing.

I tried to mitigate this by using various methods:

### Strips
I tried drawing only illuminated pixels using `SetCursorPosition`, then drawing successive illuminated pixels without repositioning the cursor.

Unfortunately, this required clearing the screen every frame to remove old pixels, making it the worst option due to flickering.


### Scanlines
The first breakthrough was constructing each row as a string and writing it with a single `Console.Write`.

This produced a much more stable image. Cursor noise remained, but was confined to a thin strip along the left edge of the display.

### DirtyScanlines
Attempted improvement over scanlines approach by only redrawing rows whose contents had changed.

This was the fastest and cleanest approach so far, but resizing the console caused problems: unchanged rows weren't redrawn, leaving the display corrupted until those rows changed again.

### FullScreen
Finally, I tried constructing the entire screen as one string and writing it with a single `Console.Write`, hoping to eliminate the remaining cursor noise.

This reduced the noise but produced a thin diagonal trail of cursor artifacts across the display.

At this point, **Scanlines was the preferred approach**, since its cursor artifacts were confined to the left edge.

### Final Discovery
I eventually discovered `Console.CursorVisible = false`, which eliminated the cursor artifacts entirely.

With the cursor hidden, **FullScreen and Scanlines became the preferred rendering modes**, making the more complicated approaches largely unnecessary.

The original BruteForce mode remains available because its cursor artifacts produce an amusing starfield effect in *Space Invaders*.
