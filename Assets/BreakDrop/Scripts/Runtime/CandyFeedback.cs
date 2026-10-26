using UnityEngine;
namespace BreakDrop
{
    public sealed class CandyFeedback : MonoBehaviour
    {
        private AudioSource _music, _effects;
        private AudioClip _click, _break;
        private CandyEconomy _economy;
        public void Configure(CandyEconomy economy)
        {
            _economy = economy;
            _music = gameObject.AddComponent<AudioSource>();
            _effects = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.volume = .09f; _effects.volume = .2f;
            _music.clip = Melody(); _click = Tone(660, .07f); _break = Tone(440, .13f);
            Apply();
        }
        public void Apply()
        {
            if (_music == null) return;
            if (_economy.Data.music) { if (!_music.isPlaying) _music.Play(); }
            else _music.Stop();
            _effects.mute = !_economy.Data.effects;
        }
        public void Click() { if (_effects != null) _effects.PlayOneShot(_click); }
        public void Break()
        {
            if (_effects != null) _effects.PlayOneShot(_break);
#if UNITY_ANDROID || UNITY_IOS
            if (_economy != null && _economy.Data.vibration && Application.isMobilePlatform) Handheld.Vibrate();
#endif
        }
        private static AudioClip Tone(float frequency, float seconds)
        {
            int n = Mathf.RoundToInt(22050 * seconds); var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = Mathf.Sin(i * frequency * 2 * Mathf.PI / 22050) * (1f - (float)i / n);
            var clip = AudioClip.Create("Candy tone", n, 1, 22050, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip Melody()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 698.46f, 659.25f, 523.25f };
            int beat = 11025; var data = new float[beat * notes.Length];
            for (int b = 0; b < notes.Length; b++)
            for (int i = 0; i < beat; i++)
            {
                float envelope = Mathf.Sin(Mathf.PI * i / beat) * Mathf.Exp(-4f * i / beat);
                data[b * beat + i] = Mathf.Sin(2 * Mathf.PI * notes[b] * i / 22050) * envelope;
            }
            var clip = AudioClip.Create("Candy prototype melody", data.Length, 1, 22050, false);
            clip.SetData(data, 0); return clip;
        }
        private void OnDestroy()
        {
            if (_music != null && _music.clip != null) Destroy(_music.clip);
            if (_click != null) Destroy(_click);
            if (_break != null) Destroy(_break);
        }
    }
}

