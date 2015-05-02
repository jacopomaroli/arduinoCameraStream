using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading;

namespace arduinoCameraStream
{
    static class console
    {
        public static RichTextBox hConsole;
        static string RTFHeader;
        static string consoleBuffer;
        static console()
        {
            consoleBuffer = "";
            RTFHeader = "{\\rtf1\\ansi\\ansicpg0\\deff0 {\\fonttbl {\\f0 Times New Roman;}{\\f1 Courier;}}";
            RTFHeader += "{\\colortbl;\\red0\\green0\\blue0;\\red255\\green0\\blue0;\\red7\\green145\\blue90;}";
        }
        public static void append(string str)
        {
            consoleBuffer += str;
            MemoryStream stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(RTFHeader + consoleBuffer));
            hConsole.Selection.Load(stream, DataFormats.Rtf);
            hConsole.ScrollToEnd();
        }
    }
}