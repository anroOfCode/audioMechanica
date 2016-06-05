using CSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioMechanica
{
    public class MetronomeGenerator : ISampleSource
    {
        public double Frequency
        {
            get { return _frequency; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");
                _frequency = value;
            }
        }

        public double Amplitude
        {
            get { return _amplitude; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value");
                _amplitude = value;
            }
        }

        public double Bpm
        {
            get; set;
        }

        public double PingPhase { get; set; }

        private readonly WaveFormat _waveFormat;
        private double _frequency;
        private double _amplitude;

        public MetronomeGenerator()
            : this(120, 1000, 0.5)
        {
        }

        public MetronomeGenerator(double bpm, double frequency, double amplitude)
        {
            if (frequency <= 0)
                throw new ArgumentOutOfRangeException("frequency");
            if (amplitude < 0 || amplitude > 1)
                throw new ArgumentOutOfRangeException("amplitude");

            Frequency = frequency;
            Amplitude = amplitude;
            Bpm = bpm;
            _waveFormat = new WaveFormat(44100, 32, 1, AudioEncoding.IeeeFloat);
        }

        public double Phase { get; set; }
        public int Read(float[] buffer, int offset, int count)
        {
            if (Phase > 1) Phase = 0;

            double beatsPerSecond = Bpm / 60.0;
            double pingPhasePerSample = beatsPerSecond / WaveFormat.SampleRate;
            double phasePerSample = 1.0 / WaveFormat.SampleRate;

            for (int i = offset; i < count; i++) {
                double envolopeAmplitude = 1.0 * Math.Pow(Math.E, -10 * PingPhase);
                float sine = (float)(Amplitude * Math.Sin(Frequency * Phase * Math.PI * 2) * envolopeAmplitude);
                buffer[i] = sine;

                PingPhase += pingPhasePerSample;
                Phase += phasePerSample;
                if (PingPhase > 1.0) {
                    PingPhase = 0;
                    Phase = 0;
                    // We use 1.0 as sort of a sentinal value to detect the onset
                    // of the metronome beat.
                    buffer[i] = 1.0f;
                }
            }

            return count;
        }

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        public long Position
        {
            get { return 0; }
            set { throw new InvalidOperationException(); }
        }

        public long Length
        {
            get { return 0; }
        }

        public bool CanSeek
        {
            get { return false; }
        }

        public void Dispose()
        {}
    }
}
