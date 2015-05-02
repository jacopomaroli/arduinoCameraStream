using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.ComponentModel;

namespace arduinoCameraStream
{
    public partial class MainWindow : Window
    {
        private static readonly BackgroundWorker worker = new BackgroundWorker();
        static List<String> tList = new List<String>();
        static int selectIndex = -1;
        public MainWindow()
        {
            InitializeComponent();
            setWindowHandle();
            attachEventHandlers();
            attachCanvasEvents();
            addBackgroundWorkerEvts();
            getSerialPortList();
        }
        void setWindowHandle()
        {
            console.hConsole = rTBConsole;
            console.append("\\fs22 \\cf3 \\f0 Welcome to \\i\\ul\\b ArduinoCameraStream\\b0\\ul0\\i0 !!!");

            App.hSerialProgress = serialProgress;
            App.hSerialProgressText = serialProgressText;
        }
        void addBackgroundWorkerEvts()
        {
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!isCompleted)
                System.Threading.Thread.Sleep(100);
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            COMListLoadingSpinner.Visibility = System.Windows.Visibility.Hidden;
            COMList.ItemsSource = tList;
            if (selectIndex != -1)
                COMList.SelectedIndex = selectIndex;
            COMList.IsEnabled = true;
            openSerialSettings.IsEnabled = true;
            openSerial.IsEnabled = true;
        }
        void attachEventHandlers()
        {
            openSerial.Click += App.openSerialHandler;
            COMList.SelectionChanged += App.serialSelectorChanged;
        }
        void attachCanvasEvents()
        {
            App.iCanvas = uiCanvas;
        }
        public void getSerialPortList()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort");
            ManagementOperationObserver results = new ManagementOperationObserver();
            results.ObjectReady += new ObjectReadyEventHandler(this.NewObject);
            results.Completed += new CompletedEventHandler(this.Done);
            searcher.Get(results);
        }
        private bool isCompleted = false;
        int i = 0;
        private void NewObject(object sender, ObjectReadyEventArgs obj)
        {
            String deviceID = obj.NewObject["DeviceID"].ToString();
            String description = obj.NewObject["Description"].ToString();
            tList.Add(deviceID + " - " + description);
            App.LDeviceId.Add(deviceID);
            if (description.Contains("Arduino") && selectIndex == -1)
                selectIndex = i;
            i++;
        }
        private void Done(object sender, CompletedEventArgs obj)
        {
            isCompleted = true;
        }
        void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        void New_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("New Invoker");
        }
        void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        void Open_Executed(object target, ExecutedRoutedEventArgs e)
        {
            /*newDialog _newDialog = new newDialog();
            _newDialog.Owner = Application.Current.MainWindow;
            _newDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _newDialog.ShowDialog();*/
        }
        void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        void Save_Executed(object target, ExecutedRoutedEventArgs e)
        {
            //fileManager.save();
        }
        void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        void SaveAs_Executed(object target, ExecutedRoutedEventArgs e)
        {
            //fileManager.saveAs();
        }
    }
}
