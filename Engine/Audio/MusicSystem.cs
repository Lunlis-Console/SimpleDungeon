using System;
using NAudio.Wave;

namespace Engine.Audio
{
    public static class MusicSystem
    {
        private static IWavePlayer _outputDevice;
        private static AudioFileReader _audioFile;
        private static LoopStream _loopStream;

        // Сопоставление тем и файлов
        private static readonly Dictionary<MusicTheme, string> ThemeFiles = new()
        {
            { MusicTheme.Main,    "Assets/Music/main_theme.mp3" },
            { MusicTheme.Battle,  "Assets/Music/battle_theme.mp3" },
            { MusicTheme.Village, "Assets/Music/village_theme.mp3" }
        };

        public static void PlayTheme(MusicTheme theme, bool loop = true)
        {
            if (theme == MusicTheme.None)
            {
                Stop();
                return;
            }

            if (!ThemeFiles.TryGetValue(theme, out var path))
            {
                DebugConsole.Log($"[MusicSystem] Нет файла для темы {theme}");
                return;
            }

            if (!File.Exists(path))
            {
                DebugConsole.Log($"[MusicSystem] Файл не найден: {path}");
                return;
            }

            Play(path, loop);
        }

        public static void Play(string path, bool loop = true)
        {
            Stop(); // остановим, если уже играет

            try
            {
                _outputDevice = new WaveOutEvent();
                _audioFile = new AudioFileReader(path);

                if (loop)
                {
                    _loopStream = new LoopStream(_audioFile);
                    _outputDevice.Init(_loopStream);
                }
                else
                {
                    _outputDevice.Init(_audioFile);
                }

                _outputDevice.Play();
            }
            catch (Exception ex)
            {
                DebugConsole.Log("[MusicSystem] Ошибка при воспроизведении: " + ex.Message);
            }
        }

        public static void Stop()
        {
            try
            {
                _outputDevice?.Stop();
                _audioFile?.Dispose();
                _loopStream?.Dispose();
                _outputDevice?.Dispose();
            }
            catch { }
            finally
            {
                _audioFile = null;
                _loopStream = null;
                _outputDevice = null;
            }
        }

        public static void Pause() => _outputDevice?.Pause();
        public static void Resume() => _outputDevice?.Play();

        public static float Volume
        {
            get => _audioFile?.Volume ?? 0f;
            set { if (_audioFile != null) _audioFile.Volume = value; }
        }
    }
    /// <summary>
    /// Обёртка для зацикливания трека.
    /// </summary>
    public class LoopStream : WaveStream
    {
        private readonly WaveStream _sourceStream;

        public LoopStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;
        public override long Length => _sourceStream.Length;
        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _sourceStream.Read(buffer, offset, count);
            if (read == 0)
            {
                _sourceStream.Position = 0;
                read = _sourceStream.Read(buffer, offset, count);
            }
            return read;
        }
    }
}
