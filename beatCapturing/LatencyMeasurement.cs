using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.SampleConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioMechanica
{
    public class LatencyMeasurement
    {
        CaptureLatencyTracker _tracker = new CaptureLatencyTracker();
        WasapiLoopbackCapture _capture = new WasapiLoopbackCapture();
        WasapiOut _output = new WasapiOut();

        const int c_sampleCount = 25;

        ManualResetEvent _done = new ManualResetEvent(false);
        public long RunMeasurementRoutine()
        {
            Console.WriteLine($"We will take {c_sampleCount} samples to calculate the end-to-end system latency.");
            Console.WriteLine("Setting up audio devices...");

            InterceptKeys.Instance.OnKey += OnKey;
            InterceptKeys.Start();
            Thread.Sleep(1000);
            _capture.Initialize();
            _capture.DataAvailable += OnData;
            _tracker.Start();
            _capture.Start();

            _output.Initialize(new SampleToIeeeFloat32(new MetronomeGenerator()));
            _output.Play();

            _done.WaitOne();

            float delta = 0;
            for (int i = 0; i < c_sampleCount; i++) {
                delta += _keyTicks[i] - _audioTicks[i];
            }
            delta /= c_sampleCount;
            Console.WriteLine($"End-to-end latency: {delta / 10000}ms");
            Thread.Sleep(5000);
            return (long)(delta);
        }

        void OnKey(object sender, InterceptKeys.KeyOfInterest e)
        {
            _keyTicks.Add(_tracker.CurrentTime);

            if (_audioTicks.Count == _keyTicks.Count) {
                Console.WriteLine($"offset {(_keyTicks.Last()- _audioTicks.Last()) / 10000 }ms");
                if (_audioTicks.Count >= c_sampleCount) {
                    _capture.Stop();
                    InterceptKeys.Stop();
                    _output.Stop();
                    _capture.Dispose();
                    _output.Dispose();
                    _done.Set();
                }
            }
        }

        List<long> _audioTicks = new List<long>();
        List<long> _keyTicks = new List<long>();
        bool firstStart = true;
        void OnData(object sender, DataAvailableEventArgs e)
        {
            _tracker.OnData(e.Format.BytesToMilliseconds(e.ByteCount));
            var startStep = _tracker.CurrentTimeAtBeginningOfLastRecordedSample;
            var stepTime = 1 * 1000 * 10000 / e.Format.SampleRate / e.Format.Channels;
            long tick = (long)startStep;

            var target = new float[e.ByteCount / 4];
            System.Buffer.BlockCopy(e.Data, 0, target, 0, e.ByteCount);
            int i = 0;
            foreach (var ee in target)
            {
                i++;
                tick += stepTime;
                if (ee == 1.0f && i % 2 == 0) {
                    _audioTicks.Add(tick);
                    if (firstStart) {
                        Console.WriteLine($"Start in... {5 - _audioTicks.Count}");
                    }
                    if (firstStart && _audioTicks.Count == 5) {
                        _audioTicks.Clear();
                        firstStart = false;
                    }
                }
            }
        }
    }
}
