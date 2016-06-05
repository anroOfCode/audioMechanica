using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioMechanica
{
    public class CaptureLatencyTracker
    {
        // Measured in 100ns ticks.
        // We snap the time we start the capture in _startCaptureTime and then create 
        // a running estimate to track the delta between the start capture time and the
        // time as estimated by summing the duration of all the arrived samples. We do this
        // not to measure latency, but to be able to give each sample a stable real time
        // value that matches with the recorded BeatKey times.
        //
        // The OnData callback is invoked using a polling thread, and varies by a few milliseconds
        // each invocation, so we can't sample QPC at the invocation time to do this, we want to
        // instead sample QPC once at the beginning of the capture, and then sample it at every
        // OnData call to build a stable correction factor.
        //
        // This doesn't account for channel latency. Generally for the loopback audio device there
        // is none. The latency we care about is 
        long _startCaptureTime = 0;
        long _elapsedTotalSampleTime = 0;
        double _runningEstimate = 0.0;
        const double _emwaWeight = 0.05;


        public void Start()
        {
            // Ticks / (T/s) => S
            // S * 1000 (ms/s) * (10000 ticks / ms)
            _startCaptureTime = CurrentTime;
        }

        public void OnData(double sampleWidthInMs)
        {
            var ticks = (long)(sampleWidthInMs * 10000);

            // Because we're being called back right now, all the data in our buffer has
            // already happened. Our total elapsed sample time is slightly in the past.
            _elapsedTotalSampleTime += ticks;
            var captureTime = CurrentTime;

            // This is the delta between the 'real' timestamp we snapped when we started the
            // audio capture and our current 'real' timestamp, adjusted by the accumulated sample time.
            var delta = captureTime - _startCaptureTime - _elapsedTotalSampleTime;
            // We use this running estimate instead of using the current TimeStamp directly to achieve
            // a more stable timestamp- this OnData callback is called from a polling thread and has
            // a few milliseconds of jitter to it, by averaging out the last N samples we get a much
            // more stable estimate of the actual offset from real-time, robust against that jitter.
            // Note that the estimate might be negative- when we start the audio engine, if the output
            // buffer is already partially full it'll copy 10ms or so of data from before the Start call
            // into the output stream. Totally fine.
            _runningEstimate = _emwaWeight * delta + (1 - _emwaWeight) * _runningEstimate;
            //Console.WriteLine($"Delta: {delta / 10000}ms, Running: {_runningEstimate / 10000}ms");
        }

        public long CurrentTime { get { return Stopwatch.GetTimestamp() * 1000 * 10000 / Stopwatch.Frequency; } }

        public double OffsetIn100NsTicks { get { return _runningEstimate; } }
        public long CurrentTimeAtBeginningOfLastRecordedSample { get { return (long)(_startCaptureTime + _elapsedTotalSampleTime + _runningEstimate); } }
    }
}
