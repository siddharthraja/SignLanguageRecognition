using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    
    public class ColorFrameWriter
    {
        private int image_count;
        private string current_phrase;
        private int old_session_number;
        public void setCurrentPhrase(string p)
        {
            current_phrase = p;
        }

        public ColorFrameWriter()
        {
            // To Do
            image_count = 1;
            old_session_number = 0;
        }
        /*
        public async void ProcessWrite(BitmapFrame b)
        {
            string filename = "temp_" + image_count + ".png";
            image_count++;
            string filePath = @"C:\\Users\\ASLR\\Documents\\z-aslr-data\\"+filename;

            await WriteTextAsync(filePath, b);
        }

        private async Task WriteTextAsync(string filePath, BitmapFrame b)
        {
            //byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 12000, useAsync: true))
            {
                //await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                BitmapEncoder encoder = new PngBitmapEncoder();
                //_rw.EnterReadLock();
                //encoder.Frames.Add(imageQueue.Dequeue());
                encoder.Frames.Add(b);
                //Thread.Sleep(100);
                //_rw.ExitReadLock();
                
                encoder.Save(sourceStream);
                await sourceStream.FlushAsync();
                sourceStream.Close();
            };
        }
        */

        public async void ProcessWrite(byte[] b, int session_number)
        {
            if (session_number != old_session_number)
            {
                old_session_number = session_number;
                image_count = 1;
            }
            string filename = current_phrase + "_color_" + image_count + ".bytes";
            image_count++;
            string filePath = @"D:\z-alsr-data\" + current_phrase + "\\" + session_number + "\\color\\" + filename;

            await WriteTextAsync(filePath, b);
        }

        private async Task WriteTextAsync(string filePath, byte[] b)
        {
            //byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(b, 0, b.Length);
                sourceStream.Close();
                
            };
        }
        
        

    }
}
