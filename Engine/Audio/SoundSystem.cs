using System;
using System.IO;
using NAudio.Wave;

namespace Engine.Audio
{
    public static class SoundSystem
    {
        /// <summary>
        /// Воспроизводит короткий звук (WAV или MP3).
        /// Для эффектов лучше WAV — быстрее запускается.
        /// </summary>
        public static void Play(string path, float volume = 1.0f)
        {
            if (!File.Exists(path))
            {
                DebugConsole.Log($"[SoundSystem] Файл не найден: {path}");
                return;
            }

            try
            {
                var audioFile = new AudioFileReader(path) { Volume = Math.Clamp(volume, 0f, 1f) };
                var outputDevice = new WaveOutEvent();

                outputDevice.Init(audioFile);
                outputDevice.Play();

                // очистка после проигрывания
                outputDevice.PlaybackStopped += (s, e) =>
                {
                    audioFile.Dispose();
                    outputDevice.Dispose();
                };
            }
            catch (Exception ex)
            {
                //DebugConsole.Log("[SoundSystem] Ошибка воспроизведения: " + ex.Message);
            }
        }
    }
}
