using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageFileConvertor
{
    class Program
    {
        static void Main(string[] args)
        {
            string rgbDirectory = "C:\\Users\\ASLR\\Documents\\z-aslr-data";
            string[] rgbFilesList = Directory.GetFiles(rgbDirectory);
            int count = 0;
            foreach (string f in rgbFilesList){
                System.Diagnostics.Debug.Write("\n" + f);
                string filePath = rgbDirectory + "\\z_temp_" + (count++) + ".png";
                
                byte[] fileBytes = File.ReadAllBytes(f);
                int width = 1920;
                int height = 1080;
                PixelFormat format = PixelFormats.Bgr32;
                int stride = width * format.BitsPerPixel / 8;
                BitmapFrame b = BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, format, null, fileBytes, stride));

                using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 25000, useAsync: true))
                {
                    //await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    //_rw.EnterReadLock();
                    //encoder.Frames.Add(imageQueue.Dequeue());
                    encoder.Frames.Add(b);
                    //Thread.Sleep(100);
                    //_rw.ExitReadLock();

                    encoder.Save(sourceStream);
                    sourceStream.Flush();
                    sourceStream.Close();
                }
                 
            }

        }

    }
}
