using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Text;

namespace arduinoCameraStream
{
    public partial class App : Application
    {
        static SerialPort _serialPort = new SerialPort();
        static public List<String> LDeviceId = new List<String>();
        static public int deviceIdIndex = -1;
        static public DateTime epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        static public Canvas iCanvas;
        static public bool isRecording = false;
        static public bool isPlaying = false;
        static public uint recordingFrameCount = 0;
        static string serialBuffer = "";
        static string newDataBuffer = "";
        static string frameBuffer = "";
        static string imageBuffer = "";
        static public ProgressBar hSerialProgress;
        static public TextBlock hSerialProgressText;
        static public string recordingFolder = "";
        static int totalFrameLength = 640 * 480 + "*FRAME_START*".Length + "*FRAME_STOP*".Length;
        static List<String> rawFilesToShow;
        static string rawStreamDirectory = "";
        static public int rawViewerIndex = 0;
        static public void openSerial()
        {
            if (deviceIdIndex == -1)
                return;

            _serialPort.PortName = LDeviceId[deviceIdIndex];
            _serialPort.BaudRate = 250000; //115200
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DtrEnable = true;
            _serialPort.RtsEnable = true;
            _serialPort.NewLine = "\r\n";

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);
            _serialPort.Open();
        }
        public static void setPixel(WriteableBitmap wbm, int x, int y, Color c)
        {
             if (y > wbm.PixelHeight - 1 || x > wbm.PixelWidth - 1) return;
             if (y < 0 || x < 0) return;
             if (!wbm.Format.Equals(PixelFormats.Bgra32))return;
             wbm.Lock();
             IntPtr buff = wbm.BackBuffer;
             int Stride = wbm.BackBufferStride;
             unsafe
             {
                  byte* pbuff = (byte*)buff.ToPointer();
                  int loc= y * Stride  + x*4;
                  pbuff[ loc]=c.B;
                  pbuff[loc+1]=c.G;
                  pbuff[loc+2]=c.R;
                  pbuff[loc+3]=c.A;
             }
             wbm.AddDirtyRect(new Int32Rect(x,y,1,1));
             wbm.Unlock();
        }
        static public void openSerialHandler(object sender, RoutedEventArgs e)
        {
            openSerial();

            /*List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
            colors.Add(System.Windows.Media.Colors.Red);
            colors.Add(System.Windows.Media.Colors.Blue);
            colors.Add(System.Windows.Media.Colors.Green);
            BitmapPalette myPalette = new BitmapPalette(colors);
            WriteableBitmap writeableBmp = new WriteableBitmap(640, 480, 72, 72, PixelFormats.Bgra32, myPalette);

            int z = 0;
            for (int y = 0; y < 480; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    setPixel(writeableBmp, x, y, Color.FromArgb(255, 255, 255, 255));
                    z += 3;
                }
            }

            Image img = new Image();
            img.Source = writeableBmp;
            img.Width = 640;
            img.Height = 480;
            Canvas.SetLeft(img, 0);
            Canvas.SetTop(img, 0);
            iCanvas.Children.Add(img);*/
        }
        static void setProgress(int current, int total)
        {
            double percent = Math.Round(current * 100.0 / total);
            hSerialProgressText.Text = current + "/" + total + " (" + percent + "%)";
            hSerialProgress.Value = percent;
        }
        static string GetRtfUnicodeEscapedString(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                if (c == '\\' || c == '{' || c == '}')
                    sb.Append(@"\" + c);
                /*else if (c <= 0x7f)
                    sb.Append(c);
                else*/
                    sb.Append("\\u" + Convert.ToUInt32(c) + "?");
            }
            return sb.ToString();
        }
        static byte[] getByteFrameBuffer(string frameBuffer)
        {
            byte[] bFrameBuffer = new byte[640*480];
            for (int i = 0; i < 640 * 480; i++)
            {
                bFrameBuffer[i] = (i < frameBuffer.Length && frameBuffer[i] < 256) ? Convert.ToByte(frameBuffer[i]) : Convert.ToByte(0);
            }
            return bFrameBuffer;
        }
        static void drawImgFromByteArray(byte[] AimgRGB)
        {
            List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
            colors.Add(System.Windows.Media.Colors.Red);
            colors.Add(System.Windows.Media.Colors.Blue);
            colors.Add(System.Windows.Media.Colors.Green);
            BitmapPalette myPalette = new BitmapPalette(colors);
            WriteableBitmap writeableBmp = new WriteableBitmap(640, 480, 72, 72, PixelFormats.Bgra32, myPalette);

            int z = 0;
            for (int y = 0; y < 480; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    setPixel(writeableBmp, x, y, Color.FromArgb(255, AimgRGB[z], AimgRGB[z + 1], AimgRGB[z + 2]));
                    z += 3;
                }
            }

            Image img = new Image();
            img.Source = writeableBmp;
            img.Width = 640;
            img.Height = 480;
            Canvas.SetLeft(img, 0);
            Canvas.SetTop(img, 0);
            iCanvas.Children.Add(img);
        }
        private static void receiveMainThread(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            if (sp.BytesToRead != 0)
            {
                newDataBuffer = sp.ReadExisting();
                serialBuffer += newDataBuffer;
                //console.append(GetRtfUnicodeEscapedString(newDataBuffer));
                setProgress(serialBuffer.Length, totalFrameLength);
                if (serialBuffer.Length > "*FRAME_START*".Length + "*FRAME_STOP*".Length &&
                    serialBuffer.IndexOf("*FRAME_START*") > -1 && serialBuffer.IndexOf("*FRAME_STOP*") > -1)
                {
                    if (serialBuffer.IndexOf("*FRAME_START*") < serialBuffer.IndexOf("*FRAME_STOP*"))
                    {
                        setProgress(totalFrameLength, totalFrameLength);

                        int start = serialBuffer.IndexOf("*FRAME_START*") + "*FRAME_START*".Length;
                        int len = serialBuffer.IndexOf("*FRAME_STOP*") - start;
                        frameBuffer = serialBuffer.Substring(start, len);

                        //delete the current frame from the serialBuffer
                        serialBuffer = serialBuffer.Substring(serialBuffer.IndexOf("*FRAME_STOP*") + "*FRAME_STOP*".Length);

                        console.append("\\fs22 \\f0 \\cf0 \\par\\pard received a frame with a size of " + ((frameBuffer.Length == 640 * 480) ? "" + frameBuffer.Length : "\\cf2 \\ul\\b " + frameBuffer.Length + "\\b0\\ul0\\cf0 ") + " byte");

                        if (isRecording)
                        {
                            using (BinaryWriter writer = new BinaryWriter(File.Open(recordingFolder + "/" + recordingFrameCount + ".raw", FileMode.Create)))
                            {
                                writer.Write(frameBuffer);
                            }
                            recordingFrameCount++;
                        }

                        byte[] AimgRGB = new byte[640 * 480 * 3];
                        byte[] bFrameBuffer = getByteFrameBuffer(frameBuffer);
                        decoding.deBayerHQl(bFrameBuffer, AimgRGB);
                        drawImgFromByteArray(AimgRGB);
                    }
                    //if we have a part of a frame in the serial buffer we drop it
                    if (serialBuffer.IndexOf("*FRAME_START*") > serialBuffer.IndexOf("*FRAME_STOP*"))
                    {
                        serialBuffer.Substring(serialBuffer.IndexOf("*FRAME_START*"));
                    }
                }
            }
        }
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (Application.Current == null)
                return;

            var d = Application.Current.Dispatcher;

            if (d.CheckAccess())
                receiveMainThread(sender, e);
            else
                d.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => receiveMainThread(sender, e)));
        }
        private static void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.Write("ErrorReceivedHandler");
            SerialPort sp = (SerialPort)sender;
            Console.WriteLine("Data Received: " + sp.BytesToRead);
            string indata = sp.ReadExisting();
            Console.Write(indata);
        }
        static public void serialSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            var COMList = sender as ComboBox;
            //string text = COMList.SelectedItem as string;
            deviceIdIndex = COMList.SelectedIndex;
        }
        static void openRawStreamFolder_getFilesList(string folder)
        {
            rawStreamDirectory = folder;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            FileInfo[] info = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            rawFilesToShow = new List<String>();

            foreach (FileInfo f in info)
            {
                /*f.Name;
                Convert.ToUInt32(f.Length);
                f.DirectoryName;
                f.FullName;
                f.Extension;*/
                if (f.Extension == ".raw")
                {
                    rawFilesToShow.Add(f.Name);
                }
            }
        }
        static public void openRawStreamFolder(string folder)
        {
            openRawStreamFolder_getFilesList(folder);
            rawFilesToShow.Sort();
            rawViewerIndex = 0;
            if (rawFilesToShow.Count == 0)
                MessageBox.Show("No .raw files found in the selected folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            drawImgFromRawFile();
        }
        static public void drawImgFromRawFile()
        {
            if (rawFilesToShow.Count == 0)
                return;
            console.append("\\fs22 \\f0 \\cf0 \\par\\pard opening file: " + rawFilesToShow[rawViewerIndex]);
            var fs = new FileStream(rawStreamDirectory + "\\" + rawFilesToShow[rawViewerIndex], FileMode.Open);
            var len = (int)fs.Length;
            var fileBuf = new byte[len];
            fs.Read(fileBuf, 0, len);
            string frameB = System.Text.Encoding.UTF8.GetString(fileBuf);

            byte[] AimgRGB = new byte[640 * 480 * 3];
            byte[] bFrameBuffer = getByteFrameBuffer(frameB);
            decoding.deBayerHQl(bFrameBuffer, AimgRGB);
            drawImgFromByteArray(AimgRGB);
        }
        static public void rawViewerIndexNext()
        {
            rawViewerIndex++;
            if (rawViewerIndex > rawFilesToShow.Count - 1)
                rawViewerIndex = 0;
        }
        static public void rawViewerIndexPrev()
        {
            rawViewerIndex--;
            if (rawViewerIndex < 0)
                rawViewerIndex = rawFilesToShow.Count - 1;
        }
    }
}
