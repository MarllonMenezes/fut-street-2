using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FifaStreetPc;

public static class Win32GameHost
{
    private const int Width = 960;
    private const int Height = 540;
    private const string WindowClassName = "FifaStreetPcCleanRoomWindow";
    private static readonly NativeMethods.WndProc WindowProcedure = WindowCallback;
    private static Win32GameHostState? state;

    public static int Run(
        GameAssetCatalog catalog,
        PcTextureResource? titleTexture = null,
        EaglCandidateMesh? candidateMesh = null,
        PcShapeImageTexture? candidateTexture = null,
        Func<string, Stream>? audioOpener = null,
        bool cleanRoomCharacters = false,
        bool candidateHomeCharacter = false,
        bool candidateAwayCharacter = false,
        double matchDurationSeconds = 300.0,
        bool cleanRoomRoundReset = false,
        CleanRoomMatchEndCondition endCondition = CleanRoomMatchEndCondition.Timed,
        int targetGoals = 0,
        int targetPannas = 0,
        string? captureFramePath = null,
        double runSeconds = 0.0)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("O host windowed atual requer Windows.");
            return 2;
        }

        var instance = NativeMethods.GetModuleHandle(null);
        var windowClass = new NativeMethods.WndClassEx
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),
            Style = NativeMethods.CsHRedraw | NativeMethods.CsVRedraw,
            WindowProcedure = WindowProcedure,
            Instance = instance,
            Cursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IdcArrow),
            ClassName = WindowClassName,
        };

        if (NativeMethods.RegisterClassEx(ref windowClass) == 0)
        {
            Console.Error.WriteLine($"Não foi possível registrar a janela Win32 (erro {Marshal.GetLastWin32Error()}).");
            return 2;
        }

        var window = NativeMethods.CreateWindowEx(
            0,
            WindowClassName,
            GameIdentity.WindowTitle,
            NativeMethods.WsOverlappedWindow,
            NativeMethods.CwUseDefault,
            NativeMethods.CwUseDefault,
            Width + 16,
            Height + 39,
            IntPtr.Zero,
            IntPtr.Zero,
            instance,
            IntPtr.Zero);
        if (window == IntPtr.Zero)
        {
            Console.Error.WriteLine($"Não foi possível criar a janela Win32 (erro {Marshal.GetLastWin32Error()}).");
            NativeMethods.UnregisterClass(WindowClassName, instance);
            return 2;
        }

        state = new Win32GameHostState(
            catalog,
            window,
            titleTexture,
            candidateMesh,
            candidateTexture,
            audioOpener,
            cleanRoomCharacters,
            candidateHomeCharacter,
            candidateAwayCharacter,
            matchDurationSeconds,
            cleanRoomRoundReset,
            endCondition,
            targetGoals,
            targetPannas,
            captureFramePath,
            runSeconds);
        NativeMethods.ShowWindow(window, NativeMethods.SwShow);
        NativeMethods.UpdateWindow(window);
        state.RunLoop();
        state = null;
        NativeMethods.DestroyWindow(window);
        NativeMethods.UnregisterClass(WindowClassName, instance);
        return 0;
    }

    internal static MatchInput BuildAuthoredInput(Func<int, bool> isDown)
    {
        ArgumentNullException.ThrowIfNull(isDown);
        var moveX = isDown('D') ? 1.0f : isDown('A') ? -1.0f : 0.0f;
        var moveY = isDown('S') ? 1.0f : isDown('W') ? -1.0f : 0.0f;
        return new MatchInput(
            moveX,
            moveY,
            isDown('P'),
            isDown(NativeMethods.VkSpace),
            isDown('T'),
            Panna: isDown('N'),
            Gamebreaker: isDown('G'),
            Juggle: isDown('J'),
            Tackle: isDown('K'));
    }

    private static IntPtr WindowCallback(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case NativeMethods.WmPaint:
                if (state is not null)
                {
                    var paint = NativeMethods.BeginPaint(window, out var paintStruct);
                    state.Present(paint);
                    NativeMethods.EndPaint(window, ref paintStruct);
                    return IntPtr.Zero;
                }
                break;
            case NativeMethods.WmClose:
                NativeMethods.DestroyWindow(window);
                return IntPtr.Zero;
            case NativeMethods.WmDestroy:
                NativeMethods.PostQuitMessage(0);
                return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private sealed class Win32GameHostState
    {
        private readonly GameAssetCatalog catalog;
        private readonly CatalogAudioMixer audio;
        private readonly IntPtr window;
        private readonly PcTextureResource? titleTexture;
        private readonly EaglCandidateMesh? candidateMesh;
        private readonly PcShapeImageTexture? candidateTexture;
        private readonly bool cleanRoomCharacters;
        private readonly bool candidateHomeCharacter;
        private readonly bool candidateAwayCharacter;
        private readonly string? captureFramePath;
        private readonly double runSeconds;
        private readonly StreetMatch match;
        private int lastAudioActionSequence;
        private readonly SoftwareFrameBuffer frame = new(Width, Height);
        private readonly byte[] bgra = new byte[Width * Height * 4];
        private bool frameCaptureWritten;
        private NativeMethods.BitmapInfo bitmapInfo;

        public Win32GameHostState(
            GameAssetCatalog catalog,
            IntPtr window,
            PcTextureResource? titleTexture,
            EaglCandidateMesh? candidateMesh,
            PcShapeImageTexture? candidateTexture,
            Func<string, Stream>? audioOpener,
            bool cleanRoomCharacters,
            bool candidateHomeCharacter,
            bool candidateAwayCharacter,
            double matchDurationSeconds,
            bool cleanRoomRoundReset,
            CleanRoomMatchEndCondition endCondition,
            int targetGoals,
            int targetPannas,
            string? captureFramePath,
            double runSeconds)
        {
            this.catalog = catalog;
            audio = new CatalogAudioMixer(
                catalog,
                audioOpener,
                enablePlayback: true,
                prepareObservedBankCandidates: true);
            audio.Initialize();
            this.window = window;
            this.titleTexture = titleTexture;
            this.candidateMesh = candidateMesh;
            this.candidateTexture = candidateTexture;
            this.cleanRoomCharacters = cleanRoomCharacters;
            this.candidateHomeCharacter = candidateHomeCharacter;
            this.candidateAwayCharacter = candidateAwayCharacter;
            this.captureFramePath = string.IsNullOrWhiteSpace(captureFramePath)
                ? null
                : Path.GetFullPath(captureFramePath);
            if (runSeconds < 0 || double.IsNaN(runSeconds) || double.IsInfinity(runSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(runSeconds));
            }
            this.runSeconds = runSeconds;
            match = new StreetMatch(
                enableOpponentAi: true,
                enableCleanRoomRoundReset: cleanRoomRoundReset,
                matchDurationSeconds: matchDurationSeconds,
                endCondition: endCondition,
                targetGoals: targetGoals,
                targetPannas: targetPannas);
            // Preserve the initial authored KICKOFF event (sequence 0) for
            // the clean-room audio sink.
            lastAudioActionSequence = -1;
            bitmapInfo = new NativeMethods.BitmapInfo
            {
                Header = new NativeMethods.BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(),
                    Width = Width,
                    Height = -Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = NativeMethods.BiRgb,
                },
            };
        }

        public void RunLoop()
        {
            try
            {
                var clock = Stopwatch.StartNew();
                var previousSeconds = clock.Elapsed.TotalSeconds;
                while (true)
                {
                    while (NativeMethods.PeekMessage(out var message, IntPtr.Zero, 0, 0, NativeMethods.PmRemove))
                    {
                        if (message.MessageId == NativeMethods.WmQuit)
                        {
                            return;
                        }
                        NativeMethods.TranslateMessage(ref message);
                        NativeMethods.DispatchMessage(ref message);
                    }

                    var currentSeconds = clock.Elapsed.TotalSeconds;
                    var delta = Math.Clamp(currentSeconds - previousSeconds, 0.0, 0.1);
                    previousSeconds = currentSeconds;
                    match.Tick(delta, ReadInput());
                    foreach (var matchEvent in match.EventHistory.Where(matchEvent =>
                                 matchEvent.Sequence > lastAudioActionSequence))
                    {
                        audio.OnGameplayEvent(matchEvent.Action);
                    }
                    lastAudioActionSequence = match.ActionSequence;
                    audio.Update(delta);
                    PreviewRenderer.Render(
                        match,
                        frame,
                        titleTexture,
                        candidateMesh,
                        candidateTexture,
                        cleanRoomCharacters: cleanRoomCharacters,
                        candidateHomeCharacter: candidateHomeCharacter,
                        candidateAwayCharacter: candidateAwayCharacter,
                        showAuthoredHud: true);
                    if (!frameCaptureWritten && captureFramePath is not null)
                    {
                        frame.SavePpm(captureFramePath);
                        frameCaptureWritten = true;
                        Console.WriteLine($"windowed_frame_capture={captureFramePath}");
                        Console.WriteLine($"windowed_frame_checksum={frame.ComputeChecksum():X16}");
                    }
                    PresentWindow();
                    if (runSeconds > 0 && clock.Elapsed.TotalSeconds >= runSeconds)
                    {
                        return;
                    }
                    Thread.Sleep(8);
                }
            }
            finally
            {
                audio.Dispose();
            }
        }

        public void Present(IntPtr deviceContext)
        {
            frame.CopyToBgra32(bgra);
            NativeMethods.GetClientRect(window, out var client);
            var width = Math.Max(1, client.Right - client.Left);
            var height = Math.Max(1, client.Bottom - client.Top);
            NativeMethods.StretchDibits(
                deviceContext,
                0,
                0,
                width,
                height,
                0,
                0,
                Width,
                Height,
                bgra,
                ref bitmapInfo,
                NativeMethods.DibRgbColors,
                NativeMethods.Srccopy);
        }

        private void PresentWindow()
        {
            var deviceContext = NativeMethods.GetDC(window);
            if (deviceContext == IntPtr.Zero)
            {
                return;
            }

            try
            {
                Present(deviceContext);
            }
            finally
            {
                NativeMethods.ReleaseDC(window, deviceContext);
            }
        }

        private MatchInput ReadInput()
        {
            if (NativeMethods.GetForegroundWindow() != window)
            {
                return default;
            }

            return BuildAuthoredInput(IsDown);
        }

        private static bool IsDown(int key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;
    }

    private static class NativeMethods
    {
        internal const int CwUseDefault = unchecked((int)0x80000000);
        internal const uint CsHRedraw = 0x0002;
        internal const uint CsVRedraw = 0x0001;
        internal const uint WsOverlappedWindow = 0x00CF0000;
        internal const int SwShow = 5;
        internal const int IdcArrow = 32512;
        internal const uint WmPaint = 0x000F;
        internal const uint WmClose = 0x0010;
        internal const uint WmDestroy = 0x0002;
        internal const uint WmQuit = 0x0012;
        internal const uint PmRemove = 0x0001;
        internal const uint BiRgb = 0;
        internal const uint DibRgbColors = 0;
        internal const uint Srccopy = 0x00CC0020;
        internal const int VkSpace = 0x20;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr WndProc(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WndClassEx
        {
            internal uint Size;
            internal uint Style;
            internal WndProc WindowProcedure;
            internal int ClassExtra;
            internal int WindowExtra;
            internal IntPtr Instance;
            internal IntPtr Icon;
            internal IntPtr Cursor;
            internal IntPtr Background;
            [MarshalAs(UnmanagedType.LPWStr)] internal string? MenuName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string ClassName;
            internal IntPtr SmallIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Message
        {
            internal IntPtr Window;
            internal uint MessageId;
            internal UIntPtr WParam;
            internal IntPtr LParam;
            internal uint Time;
            internal Point Point;
            internal uint Private;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PaintStruct
        {
            internal IntPtr DeviceContext;
            internal int Erase;
            internal Rect Paint;
            internal int Restore;
            internal int IncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] internal byte[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfoHeader
        {
            internal uint Size;
            internal int Width;
            internal int Height;
            internal ushort Planes;
            internal ushort BitCount;
            internal uint Compression;
            internal uint ImageSize;
            internal int XPelsPerMeter;
            internal int YPelsPerMeter;
            internal uint ColorsUsed;
            internal uint ColorsImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RgbQuad
        {
            internal byte Blue;
            internal byte Green;
            internal byte Red;
            internal byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfo
        {
            internal BitmapInfoHeader Header;
            internal RgbQuad Colors;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string? moduleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ushort RegisterClassEx(ref WndClassEx windowClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool UnregisterClass(string className, IntPtr instance);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            IntPtr parent,
            IntPtr menu,
            IntPtr instance,
            IntPtr parameter);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyWindow(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProc(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr instance, int cursorName);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        internal static extern bool UpdateWindow(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern void PostQuitMessage(int exitCode);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(out Message message, IntPtr window, uint filterMin, uint filterMax, uint removeMessage);

        [DllImport("user32.dll")]
        internal static extern bool TranslateMessage(ref Message message);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage(ref Message message);

        [DllImport("user32.dll")]
        internal static extern IntPtr BeginPaint(IntPtr window, out PaintStruct paint);

        [DllImport("user32.dll")]
        internal static extern bool EndPaint(IntPtr window, ref PaintStruct paint);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr window, out Rect rectangle);

        [DllImport("gdi32.dll", EntryPoint = "StretchDIBits")]
        internal static extern int StretchDibits(
            IntPtr deviceContext,
            int destinationX,
            int destinationY,
            int destinationWidth,
            int destinationHeight,
            int sourceX,
            int sourceY,
            int sourceWidth,
            int sourceHeight,
            byte[] bits,
            ref BitmapInfo bitmapInfo,
            uint colorUsage,
            uint rasterOperation);
    }
}
