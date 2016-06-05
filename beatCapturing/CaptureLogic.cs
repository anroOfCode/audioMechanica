using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioMechanica
{
    public class CaptureLogic
    {
        WasapiLoopbackCapture _capture = new WasapiLoopbackCapture();
        CaptureLatencyTracker _tracker = new CaptureLatencyTracker();
        WaveWriter _writer;
        Thread _periodicThread;

        long _endToEndLatency;
        string _destinationFolder;

        public CaptureLogic(long latency, string destinationFolder)
        {
            _endToEndLatency = latency;
            _destinationFolder = destinationFolder;
            _capture.Initialize();
            _capture.DataAvailable += OnData;
        }

        long _recordStartTime = 0;
        DateTime _recordStartSystemTimestamp;
        string _recordFileName;

        public void Run()
        {
            _periodicThread = new Thread(PeriodicCancelThread);
            _periodicThread.Start();
            _tracker.Start();
            _capture.Start();
            InterceptKeys.Instance.OnKey += OnKey;
            InterceptKeys.Start();
        }

        private void OnKey(object sender, InterceptKeys.KeyOfInterest e)
        {
            if (e == InterceptKeys.KeyOfInterest.BeatKey) OnBeatKey();
            if (e == InterceptKeys.KeyOfInterest.EndKey) OnEndKey();
        }

        void OnData(object sender, DataAvailableEventArgs e)
        {
            _tracker.OnData(e.Format.BytesToMilliseconds(e.ByteCount));

            if (_writer != null) {
                if (_recordStartTime == 0) {
                    _recordStartTime = _tracker.CurrentTimeAtBeginningOfLastRecordedSample;
                }
                lock (_writer) _writer.Write(e.Data, 0, e.ByteCount);
            }
        }

        public enum BeatState
        {
            Idle,
            FirstBeat,
            Running
        }

        BeatState _state = BeatState.Idle;
        List<long> _tickList = new List<long>();

        void OnBeatKey()
        {
            lock (this) {
                if (_state == BeatState.Idle) {
                    StartCapture();
                    _state = BeatState.FirstBeat;
                }
                if (_state == BeatState.FirstBeat) {
                    _state = BeatState.Running;
                }
                _tickList.Add(_tracker.CurrentTime);
            }
        }

        void OnEndKey()
        {
            lock(this) {
                if (_state == BeatState.Running) {
                    StopCapture(true);
                    _state = BeatState.Idle;
                }
            }
        }

        void PeriodicCancelThread()
        {
            while (true) {
                lock (this) {
                    if (_state == BeatState.Running &&
                        (_tracker.CurrentTime - _tickList.Last()) > 2000 * 10000
                    ) {
                        StopCapture(false);
                        _state = BeatState.Idle;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        void StartCapture()
        {
            _recordStartSystemTimestamp = DateTime.Now;
            _recordFileName = Path.Combine(_destinationFolder, _recordStartSystemTimestamp.ToString("yyyyddMMHHmmssfff") + ".wav");
            _writer = new WaveWriter(
                _recordFileName, 
                _capture.WaveFormat
            );
        }

        void StopCapture(bool cancel)
        {
            var w = _writer;
            _writer = null;
            lock (w) w.Dispose();

            if (cancel) {
                File.Delete(_recordFileName);
                _tickList.Clear();
                Console.WriteLine("Aborted.");
                return;
            }

            StringBuilder fileContents = new StringBuilder();
            long sum = 0;
            for (int i = 0; i < _tickList.Count; i++) {
                if (i > 0) sum += _tickList[i] - _tickList[i - 1];
                fileContents.AppendLine((_tickList[i] - _recordStartTime - _endToEndLatency).ToString());
            }
            File.WriteAllText(
                Path.Combine(_destinationFolder, _recordStartSystemTimestamp.ToString("yyyyddMMHHmmssfff") + ".beats.txt"), 
                fileContents.ToString()
            );

            sum = sum / (_tickList.Count - 1);
            var bpm = 1 / ((double)sum / 1000 / 10000) * 60;
            Console.WriteLine("Average bpm: " + bpm);
            _tickList.Clear();
            _recordStartTime = 0;
        }
    }
}
