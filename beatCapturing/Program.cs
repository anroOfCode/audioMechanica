using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioMechanica
{
    public static class Program
    {
        public static void Main()
        {
            var latency = new LatencyMeasurement().RunMeasurementRoutine();
            var c = new CaptureLogic(latency, @"E:\projects\trainingData\beatSamples.v1");
            c.Run();
            Console.ReadKey();
        }
    }
}
