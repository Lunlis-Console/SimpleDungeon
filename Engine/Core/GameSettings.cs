namespace Engine.Core
{
    public static class GameSettings
    {
        // Громкость музыки (0.0 – выкл, 1.0 – максимум)
        private static float _musicVolume = 0.7f;

        public static float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Math.Clamp(value, 0f, 1f);
                Engine.Audio.MusicSystem.Volume = _musicVolume;
            }
        }
    }
}
