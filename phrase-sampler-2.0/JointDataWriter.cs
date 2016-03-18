using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    class JointDataWriter
    {
        private int phrase_file_count;
        private string current_phrase;
        private bool shouldStop = false;
        private FileStream sourceStream;
        private string filePath;
        private bool paused;
        private bool writeOn;

        public JointDataWriter(){
            this.sourceStream = null;
        }

        public void pause(){    this.paused = true;     }
        public void unpause(){  this.paused = false;    }

        public void setCurrentPhrase(string p)
        {
            //if (!this.paused){
                this.current_phrase = p;
                this.phrase_file_count = 1;
            //}
        }

        public void endPhrase()
        {
            //if (!this.paused){
                if (this.sourceStream != null)
                {
                    this.sourceStream.Close();
                    this.phrase_file_count++;
                }
                this.writeOn = false;
            //}
        }

        public void deleteLastSample()
        {
            //if (!this.paused){
                if (this.sourceStream != null)
                {
                    this.sourceStream.Close();
                }
                this.phrase_file_count--;
                File.Delete(@"C:\Users\aslr\Documents\aslr-data\" + this.current_phrase + "\\" + this.current_phrase + "_" + this.phrase_file_count + ".txt");
            //}
        }

        public void startNewPhrase()
        {
            //if (!this.paused){
                this.writeOn = true;
                this.filePath = @"C:\Users\aslr\Documents\aslr-data\" + this.current_phrase + "\\" + this.current_phrase + "_" + this.phrase_file_count + ".txt";
                this.sourceStream = new FileStream(this.filePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 25000, useAsync: true);
            //}
        }

        public async void writeData(string s)
        {
            //await WriteTextAsync(this.filePath, b);
            //if (!this.paused){
            if (this.writeOn)
            {
                byte[] b = Encoding.Unicode.GetBytes(s);
                await sourceStream.WriteAsync(b, 0, b.Length);
            }
            //}
        }

        /*
        private async Task WriteTextAsync(string filePath, byte[] b)
        {
            //byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
            FileMode.Append, FileAccess.Write, FileShare.None,
            bufferSize: 25000, useAsync: true))
            {
                await sourceStream.WriteAsync(b, 0, b.Length);
                //sourceStream.Close();
            };
        }
        */
        

    }
}
