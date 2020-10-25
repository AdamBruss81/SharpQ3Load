using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace engine
{    
    public class SoundManager : IDisposable
    {
        public enum EEffects { NONE, SPAWN, ROCKET_AWAY, PLASMA_AWAY, 
            FALL, FALL_MINOR, LAND, CLANK1, CLANK2, CLANK3, CLANK4, STEP1, STEP2, STEP3, STEP4,
            JUMPPAD, JUMP };
        public enum ESongs { SONIC4, SONIC3, SONIC6, SONIC5, SONIC1, SONIC2, FLA22K_03, FLA22K_06, FLA22K_05, FLA22K_04, FLA22K_02 };

        private Dictionary<EEffects, string> m_dictEffects = new Dictionary<EEffects, string>();
        private Dictionary<ESongs, string> m_dictSongs = new Dictionary<ESongs, string>();
        private Zipper m_zipper = new Zipper();

        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

        Random random = new Random(DateTime.Now.Second);

        private bool m_bPlaybackStopped = false;

        public SoundManager()
        {
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2));

            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();

            // effects
            m_dictEffects[EEffects.SPAWN] = "sound/world/telein.wav";

            m_dictEffects[EEffects.ROCKET_AWAY] = "sound/weapons/rocket/rocklf1a.wav";
            m_dictEffects[EEffects.PLASMA_AWAY] = "sound/weapons/plasma/hyprbf1a.wav";

            m_dictEffects[EEffects.JUMPPAD] = "sound/world/jumppad.wav";
            m_dictEffects[EEffects.FALL] = "sound/player/sarge/fall1.wav";
            m_dictEffects[EEffects.FALL_MINOR] = "sound/player/sarge/pain100_1.wav";
            m_dictEffects[EEffects.JUMP] = "sound/player/sarge/jump1.wav";
            m_dictEffects[EEffects.LAND] = "sound/player/land1.wav";

            m_dictEffects[EEffects.CLANK1] = "sound/player/footsteps/clank1.wav";
            m_dictEffects[EEffects.CLANK2] = "sound/player/footsteps/clank2.wav";
            m_dictEffects[EEffects.CLANK3] = "sound/player/footsteps/clank3.wav";
            m_dictEffects[EEffects.CLANK4] = "sound/player/footsteps/clank4.wav";

            m_dictEffects[EEffects.STEP1] = "sound/player/footsteps/step1.wav";
            m_dictEffects[EEffects.STEP2] = "sound/player/footsteps/step2.wav";
            m_dictEffects[EEffects.STEP3] = "sound/player/footsteps/step3.wav";
            m_dictEffects[EEffects.STEP4] = "sound/player/footsteps/step4.wav";

            // songs
            m_dictSongs[ESongs.SONIC4] = "music/sonic4.wav";
            m_dictSongs[ESongs.SONIC3] = "music/sonic3.wav";
            m_dictSongs[ESongs.SONIC6] = "music/sonic6.wav";
            m_dictSongs[ESongs.SONIC5] = "music/sonic5.wav";
            m_dictSongs[ESongs.SONIC1] = "music/sonic1.wav";
            m_dictSongs[ESongs.SONIC2] = "music/sonic2.wav";
            m_dictSongs[ESongs.FLA22K_03] = "music/fla22k_03.wav";
            m_dictSongs[ESongs.FLA22K_06] = "music/fla22k_06.wav";
            m_dictSongs[ESongs.FLA22K_05] = "music/fla22k_05.wav";
            m_dictSongs[ESongs.FLA22K_04] = "music/fla22k_04_loop.wav";
            m_dictSongs[ESongs.FLA22K_02] = "music/fla22k_02.wav";
        }

        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            m_bPlaybackStopped = true;
        }

        public bool GetPlaybackStopped()
        {
            return m_bPlaybackStopped;
        }

        private void PlaySound(string sPath, float fVolNormalized)
        {
            var input = new AudioFileReader(sPath);
            input.Volume = fVolNormalized;
            mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        public void PlayEffect(EEffects effect)
        {
            if(effect != EEffects.NONE)
                PlaySound(m_zipper.ExtractSoundTextureOther(m_dictEffects[effect]), 0.8f);
        }

        public void PlaySong(ESongs song)
        {            
            PlaySound(m_zipper.ExtractSoundTextureOther(m_dictSongs[song]), 0.35f);
        }

        public void PlayRandomSong()
        {
            Array values = Enum.GetValues(typeof(ESongs));
            ESongs randomSong = (ESongs)values.GetValue(random.Next(values.Length));
            LOGGER.Info("Playing song " + randomSong.ToString());
            PlaySong(randomSong);
        }

        public void Stop()
        {
            mixer.RemoveAllMixerInputs();          
        }

        public void Dispose()
        {
            Stop();
            outputDevice.Stop();
            outputDevice.Dispose();
        }
    }

    // ===================

    class AutoDisposeFileReader : ISampleProvider
    {
        private readonly AudioFileReader reader;
        private bool isDisposed;
        public AutoDisposeFileReader(AudioFileReader reader)
        {
            this.reader = reader;
            this.WaveFormat = reader.WaveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (isDisposed)
                return 0;
            int read = reader.Read(buffer, offset, count);
            if (read == 0)
            {
                reader.Dispose();
                isDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; private set; }
    }
}
