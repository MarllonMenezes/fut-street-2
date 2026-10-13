using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FifaStreetPc;

public static class Win32WavePlayer
{
    private const uint SoundNoDefault = 0x0002;
    private const uint SoundMemory = 0x0004;
    private const uint SoundAsync = 0x0001;

    public static void PlaySynchronously(byte[] wave)
    {
        ArgumentNullException.ThrowIfNull(wave);
        ValidateWave(wave);

        var pinnedWave = GCHandle.Alloc(wave, GCHandleType.Pinned);
        try
        {
            if (!NativeMethods.PlaySound(pinnedWave.AddrOfPinnedObject(), IntPtr.Zero, SoundMemory | SoundNoDefault))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not play the decoded WAV stream.");
            }
        }
        finally
        {
            pinnedWave.Free();
        }
    }

    public static IDisposable PlayAsynchronously(byte[] wave)
    {
        ArgumentNullException.ThrowIfNull(wave);
        ValidateWave(wave);
        return new AsyncPlayback(wave);
    }

    private static void ValidateWave(byte[] wave)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WAV playback through WinMM is available only on Windows.");
        }
        if (wave.Length < 44 || !wave.AsSpan(0, 4).SequenceEqual("RIFF"u8) ||
            !wave.AsSpan(8, 4).SequenceEqual("WAVE"u8) ||
            !wave.AsSpan(36, 4).SequenceEqual("data"u8))
        {
            throw new InvalidDataException("The in-memory audio buffer is not a PCM WAV stream.");
        }
    }

    private sealed class AsyncPlayback : IDisposable
    {
        private readonly GCHandle pinnedWave;
        private bool disposed;

        public AsyncPlayback(byte[] wave)
        {
            pinnedWave = GCHandle.Alloc(wave, GCHandleType.Pinned);
            if (!NativeMethods.PlaySound(
                    pinnedWave.AddrOfPinnedObject(),
                    IntPtr.Zero,
                    SoundMemory | SoundNoDefault | SoundAsync))
            {
                pinnedWave.Free();
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not start asynchronous WAV playback.");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            NativeMethods.PlaySound(IntPtr.Zero, IntPtr.Zero, 0);
            pinnedWave.Free();
        }
    }

    private static class NativeMethods
    {
        [DllImport("winmm.dll", EntryPoint = "PlaySoundW", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PlaySound(IntPtr sound, IntPtr module, uint flags);
    }
}
