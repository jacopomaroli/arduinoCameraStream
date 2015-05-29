using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace arduinoCameraStream
{
    class fileManager
    {
        static public void openRawStream()
        {
            /*OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                dlg.FileName;
            }*/
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.RestoreDirectory = true;
            //dlg.RestoreDirectory = false;
            //dlg.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            CommonFileDialogResult result = dlg.ShowDialog();
            if(result == CommonFileDialogResult.Ok)
            {
                App.openRawStreamFolder(dlg.FileName);
            }
        }
    }
}
