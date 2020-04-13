﻿using System;
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
            FALL, FALL_MINOR, LAND, FOOTSTEP1, FOOTSTEP2, FOOTSTEP3, FOOTSTEP4,
            JUMPPAD, JUMP };
        public enum ESongs { SONIC4, SONIC3, SONIC6, SONIC5, SONIC1, SONIC2, FLA22K_03, FLA22K_06, FLA22K_05, FLA22K_04, FLA22K_02 };

        private Dictionary<EEffects, string> m_dictEffects = new Dictionary<EEffects, string>();
        private Dictionary<ESongs, string> m_dictSongs = new Dictionary<ESongs, string>();
        private Zipper m_zipper = new Zipper();

        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

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
            m_dictEffects[EEffects.SPAWN] = "Sounds/sound/world/telein.wav";

            m_dictEffects[EEffects.ROCKET_AWAY] = "Sounds/sound/weapons/rocket/rocklf1a.wav";
            m_dictEffects[EEffects.PLASMA_AWAY] = "Sounds/sound/weapons/plasma/hyprbf1a.wav";

            m_dictEffects[EEffects.JUMPPAD] = "Sounds/sound/world/jumppad.wav";
            m_dictEffects[EEffects.FALL] = "Sounds/sound/player/sarge/fall1.wav";
            m_dictEffects[EEffects.FALL_MINOR] = "Sounds/sound/player/sarge/pain100_1.wav";
            m_dictEffects[EEffects.JUMP] = "Sounds/sound/player/sarge/jump1.wav";
            m_dictEffects[EEffects.LAND] = "Sounds/sound/player/land1.wav";

            m_dictEffects[EEffects.FOOTSTEP1] = "Sounds/sound/player/footsteps/clank1.wav";
            m_dictEffects[EEffects.FOOTSTEP2] = "Sounds/sound/player/footsteps/clank2.wav";
            m_dictEffects[EEffects.FOOTSTEP3] = "Sounds/sound/player/footsteps/clank3.wav";
            m_dictEffects[EEffects.FOOTSTEP4] = "Sounds/sound/player/footsteps/clank4.wav";

            // songs
            m_dictSongs[ESongs.SONIC4] = "Sounds/music/sonic4.wav";
            m_dictSongs[ESongs.SONIC3] = "Sounds/music/sonic3.wav";
            m_dictSongs[ESongs.SONIC6] = "Sounds/music/sonic6.wav";
            m_dictSongs[ESongs.SONIC5] = "Sounds/music/sonic5.wav";
            m_dictSongs[ESongs.SONIC1] = "Sounds/music/sonic1.wav";
            m_dictSongs[ESongs.SONIC2] = "Sounds/music/sonic2.wav";
            m_dictSongs[ESongs.FLA22K_03] = "Sounds/music/fla22k_03.wav";
            m_dictSongs[ESongs.FLA22K_06] = "Sounds/music/fla22k_06.wav";
            m_dictSongs[ESongs.FLA22K_05] = "Sounds/music/fla22k_05.wav";
            m_dictSongs[ESongs.FLA22K_04] = "Sounds/music/fla22k_04_loop.wav";
            m_dictSongs[ESongs.FLA22K_02] = "Sounds/music/fla22k_02.wav";
        }

        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            m_bPlaybackStopped = true;
        }

        public bool GetPlaybackStopped()
        {
            return m_bPlaybackStopped;
        }

        private void PlaySound(string sPath)
        {
            var input = new AudioFileReader(sPath);
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
                PlaySound(m_zipper.ExtractSoundTextureOther(m_dictEffects[effect]));
        }

        public void PlaySong(ESongs song)
        {            
            PlaySound(m_zipper.ExtractSoundTextureOther(m_dictSongs[song]));
        }

        public void PlayRandomSong()
        {
            Array values = Enum.GetValues(typeof(ESongs));
            Random random = new Random();
            ESongs randomSong = (ESongs)values.GetValue(random.Next(values.Length));
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
