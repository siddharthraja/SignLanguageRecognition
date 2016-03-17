using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    class WriterThread
    {
        private bool _shouldStop = false;

        public void WriteRGB()
        {
            while (!_shouldStop)
            {
                Console.WriteLine("worker thread: working...");
            }
            Console.WriteLine("worker thread: terminating gracefully.");
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }
    }
}


/* For main window

 */
