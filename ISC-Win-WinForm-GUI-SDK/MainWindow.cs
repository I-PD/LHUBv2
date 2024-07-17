using ISC_Win_CS_LIB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Xml;
using LiveCharts;
using LiveCharts.Geared;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;
using log4net;
using System.Data;
using System.Security.AccessControl;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Wpf.CartesianChart.CustomTooltipAndLegend;
using LiveCharts.Configurations;
using Microsoft.Win32;

namespace ISC_Win_WinForm_GUI
{
    public partial class MainWindow : Form
    {
        private static readonly ILog logFile = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String AppName = "ISC WinForms SDK GUI ";
        private bool AppLoaded = false;
        private bool IsSavedScanData = false;
        private String Version = "";
        private String Revision = "";
        private formResize formResize;

        private const Int32 MAX_CFG_SECTION = 5;
        private List<ScanConfig.SlewScanConfig> LocalConfig = new List<ScanConfig.SlewScanConfig>();
        private List<MyComboBox> ComboBox_CfgScanType = new List<MyComboBox>();
        private List<MyComboBox> ComboBox_CfgWidth = new List<MyComboBox>();
        private List<ComboBox> ComboBox_CfgExposure = new List<ComboBox>();
        private List<TextBox> TextBox_CfgRangeStart = new List<TextBox>();
        private List<TextBox> TextBox_CfgRangeEnd = new List<TextBox>();
        private List<TextBox> TextBox_CfgDigRes = new List<TextBox>();
        private List<Label> Label_overSampleRate = new List<Label>();

        private Int32 TargetCfg_SelIndex = -1;       // Rocord device selected config
        private Int32 TargetCfg_Last_SelIndex = -1;  // Rocord last device selected config
        private Boolean SelCfg_IsTarget = false;     // Record target config or local config
        private Int32 LocalCfg_SelIndex = -1;        // Record local selected config
        private Int32 LocalCfg_Last_SelIndex = -1;   // Rocord last local selected config
        Boolean NewConfig = false;                   // Record new config or existed config
        Boolean EditConfig = false;                  // Record config is editing or not    
        private Int32 DevCurCfg_Index = -1;          // Record current config which set to device
        private Boolean DevCurCfg_IsTarget = false;  // Record current config is device or local
        int EditSelectIndex = -1;                    // Record edit select index   

        private BackgroundWorker bwDLPCUpdate;
        private BackgroundWorker bwTivaUpdate;

        private String Dir_Scan_DataBase = String.Empty;
        private String Dir_Scan_For_New = String.Empty;
        private String CSV_Delimiter = String.Empty;
        public static String ConfigDir = String.Empty;
        private Int32 ScanFile_Formats = 0;

        // Saved Scans
        private List<bool> SavedScanSelectList = new List<bool>();
        private List<String> SavedScanFileList = new List<String>();
        private List<DateTime> SavedScanFileTimeList = new List<DateTime>();
        private String CurrentScanFileName = String.Empty;
        private String SelectScanFileName = String.Empty;
        List<Label> Label_SavedScanType = new List<Label>();
        List<Label> Label_SavedRangeStart = new List<Label>();
        List<Label> Label_SavedRangeEnd = new List<Label>();
        List<Label> Label_SavedWidth = new List<Label>();
        List<Label> Label_SavedDigRes = new List<Label>();
        List<Label> Label_SavedExposure = new List<Label>();
        private List<SavedScanData> SavedScanList = new List<SavedScanData>();
        private BindingList<SavedScanData> bindingSavedScanList;

        // Charts
        private List<GLineSeries> ChartData_RefIntensity = new List<GLineSeries>();
        private List<GLineSeries> ChartData_Intensity = new List<GLineSeries>();
        private List<GLineSeries> ChartData_Absorbance = new List<GLineSeries>();
        private List<GLineSeries> ChartData_Reflectance = new List<GLineSeries>();
        Random rColor = new Random(Guid.NewGuid().GetHashCode());
        private List<SolidColorBrush> StrokeColors = new List<SolidColorBrush>();
        private readonly int NumOfStrokeColors = 512;
        private bool Tooltips_Show_Details = false;
        private ZoomingOptions userZoomOption = ZoomingOptions.Y;

        private static bool IsActivated { get { if (Device.GetActivationResult() == 1) return true; else return false; } }
        private static bool IsFetchingDeviceInfo = false;
        private static bool IsFetchingDeviceInfoWithError = false;
        private static string FetchingDevInfoErrMsg = "";

        private BackgroundWorker bwRefScanProgress;
        private String Tiva_FWDir = String.Empty;
        private String DLPC_FWDir = String.Empty;

        // For Scan
        private DateTime TimeScanStart = new DateTime();
        private DateTime TimeScanEnd = new DateTime();
        private static UInt32 LampStableTime = 625;
        private Scan.SCAN_REF_TYPE ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_NEW;
        private BackgroundWorker bwScan;
        private Boolean ScanButtonPressed = false;

        private String pre_ref_time = "";
        public static String buildin_ref_time = "";
        private Boolean isCancellingConfigEdit = false;
        private Boolean isSavingConfig = false;
        private Boolean isSelectingConfig = false;
        private bool isScanReference = false;
        private bool isPrevScanReference = false;

        public static bool UserCancelScan = false;
        private int TargetScanCounts = 0;
        private int ScannedCounts = 0;
        private int ScanErrorCounts = 0;
        private bool SaveOneCSVFile = false;
        private String OneScanFileName = String.Empty;
        private String AverageScanFileName = String.Empty;
        private List<double> AverageIntensity = new List<double>();
        private List<double> AverageAbsorbance = new List<double>();

        private int previous_state = -1;
        private String BackupFacRef_Msg = "";
        private String RestoreFacRef_Msg = "";

        // For Utility
        private ScanConfig.SlewScanConfig tmpCfg;
        private int ModelNameGet_Click_Counts = 0;
        private int SerialNumberGet_Click_Counts = 0;
        private int SaveCSV_Click_Counts = 0;
        private int SaveDAT_Click_Counts = 0;

        // Special Controls
        private readonly List<string> Con_Dev_With_FAN = new List<string> { "R11", "R13" };
        private readonly List<string> Con_No_KeepLampOn = new List<string> { "R3", "R11", "R13", "R15", "R17" };
        private readonly List<string> Con_OneMin_WarmUp = new List<string> { }; //"R11", "R13" };
        private readonly List<string> Con_No_WarmUp = new List<string> { "R3", "R15", "R17" };
        private readonly List<string> Con_OneNM_PixWidth = new List<string> { "R13", "T13", "F13" };

        // Generic calibration coeffients
        private readonly double calib_coeffs_diff_limit = 0.001;
        private readonly double std_calib_coeffs_ShiftVectorCoeffs_0 = -2.74909;
        private readonly double std_calib_coeffs_ShiftVectorCoeffs_1 = 0.0278162;
        private readonly double std_calib_coeffs_ShiftVectorCoeffs_2 = -0.0000681733;
        private readonly double std_calib_coeffs_PixelToWavelengthCoeffs_0 = 1784.902664;
        private readonly double std_calib_coeffs_PixelToWavelengthCoeffs_1 = -0.874372;
        private readonly double std_calib_coeffs_PixelToWavelengthCoeffs_2 = -0.000278;

        public enum FW_LEVEL
        {
            LEVEL_0, // TI EVM
            LEVEL_1, // Tiva <= 2.0.22
            LEVEL_2, // Tiva >= 2.1.0.X
            LEVEL_3, // Tiva >= 2.1.2
            LEVEL_4, // Tiva >= 2.4.0
            LEVEL_5, // Tiva >= 3.3.0, extended wavelength version
            LEVEL_6, // Tiva >= 3.5.0, extended wavelength version
            LEVEL_7  // Tiva >= 2.4.7
        };
        public enum ScanConfigMode
        {
            INITIAL,
            NEW,
            EDIT,
            DELETE,
            SAVE,
            CANCEL,
        };

        public enum ScanReference
        {
            New,
            Previous,
            Built_in
        };
        public ScanReference userDefaultReference = ScanReference.New;

        public MainWindow(string[] args)
        {
            LogManager.GetRepository().Threshold = log4net.Core.Level.All;

            MainWindowArgsParse(args);
            InitializeComponent();

            formResize = new formResize(this);
            this.Load += Form_Load;
            this.SizeChanged += Form_Resize;
            this.ResizeBegin += new System.EventHandler(MainWindow_ResizeResizeBegin);
            this.ResizeEnd += new System.EventHandler(MainWindow_ResizeEnd);

            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Revision = this.Version.Substring(this.Version.LastIndexOf('.') + 1, this.Version.Length - this.Version.LastIndexOf('.') - 1);
            this.Version = this.Version.Substring(0, this.Version.LastIndexOf('.'));
            this.Text = AppName + string.Format("v{0}", this.Version);
            lb_GUI_Revision.Text = string.Format("rev.{0}", this.Revision);

            DBG.WriteLine("GUI Version: {0}", this.Version);
            logFile.InfoFormat("GUI Version: {0}", this.Version);

            // Initial event delegate
            this.FormClosing += Main_FormClosing;
            CheckForIllegalCrossThreadCalls = false; // Solve across thread is invalid
                                                     // Initial UI and preset values
            UI_no_connection();
            this.ComboBox_CfgExposure1.DroppedDown = false;
            initChart();
            RadioButton_Intensity.PerformClick();
            label_ref.Visible = false;
            Label_ContScan.Text = "";
            BackupFacRef_Msg = "";
            RestoreFacRef_Msg = "";
            Label_CurrentConfig.ForeColor = System.Drawing.Color.OrangeRed;
            Label_CurrentConfig.Font = new System.Drawing.Font(Label_CurrentConfig.Font, System.Drawing.FontStyle.Bold);
            Button_CopyCfgL2T.Text = "Copy" + System.Environment.NewLine + char.ConvertFromUtf32(8594);
            Button_CopyCfgT2L.Text = "Copy" + System.Environment.NewLine + char.ConvertFromUtf32(8592);
            Button_MoveCfgL2T.Text = "Move" + System.Environment.NewLine + char.ConvertFromUtf32(8594);
            Button_MoveCfgT2L.Text = "Move" + System.Environment.NewLine + char.ConvertFromUtf32(8592);

            MainWindow_Loaded();
#if DEBUG
            // Enable the CPP DLL debug output for development
            DBG.Enable_CPP_Console();
#endif
            // Load save scan 
            LoadSavedScanListByNewThread();
            CheckScanDirPath();
            // Initial background workers
            initBackgroundWorker();
            // Finished loading components
            AppLoaded = true;
        }
        private void MainWindowArgsParse(string[] args)
        {
            if (args == null)
                return;

            foreach (string arg in args)
            {
                if (arg.Substring(0, 1) != "/")
                    continue;
                else
                {
                    string thisArg = arg.Substring(1, arg.Length - 1);
                    string[] cmdParam = thisArg.Split(':');
                    switch (cmdParam[0])
                    {
                        case "Ref":
                            if (cmdParam[1] == "Previous")
                                userDefaultReference = ScanReference.Previous;
                            else if (cmdParam[1] == "Built-in")
                                userDefaultReference = ScanReference.Built_in;
                            else
                                userDefaultReference = ScanReference.New;
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            String HWRev = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;
            if ((GetFW_LEVEL() >= FW_LEVEL.LEVEL_2 && Device.ChkBleExist() == 1) || HWRev == String.Empty)
                Device.SetBluetooth(true);
            SaveSettings();
            Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
        }
        private void Form_Load(object sender, EventArgs e)
        {
            formResize.get_initial_size();
            Form_Resize(this, null);
        }

        int orgWidth = 0;
        int orgHeight = 0;
        bool reqSizeChange = true;
        private void MainWindow_ResizeResizeBegin(object sender, EventArgs e)
        {
            orgWidth = this.Width;
            orgHeight = this.Height;
            reqSizeChange = false;
            SuspendLayout();
        }
        private void Form_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized || (reqSizeChange && WindowState == FormWindowState.Normal))
            {
                var pga = (string)ComboBox_PGAGain.SelectedItem;
                ComboBox_PGAGain.SelectedItem = "1";
                MyChart.Visible = false;
                formResize.resize();
                formResize.resize(); 
                ComboBox_PGAGain.SelectedItem = pga;
                MyChart.Visible = true;
                label_ContinuousMode.Font = new Font(label_ContinueScan.Font.FontFamily, label_ContinueScan.Font.Size * 2, ((System.Drawing.FontStyle)(System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic)));
            }
        }
        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            var pga = (string)ComboBox_PGAGain.SelectedItem;
            ComboBox_PGAGain.SelectedItem = "1";
            MyChart.Visible = false;
            formResize.resize();
            formResize.resize();
            ComboBox_PGAGain.SelectedItem = pga;
            MyChart.Visible = true;
            reqSizeChange = true;
            ResumeLayout();
            label_ContinuousMode.Font = new Font(label_ContinueScan.Font.FontFamily, label_ContinueScan.Font.Size * 2, ((System.Drawing.FontStyle)(System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic)));
        }

        private void initBackgroundWorker()
        {
            bwDLPCUpdate = new BackgroundWorker();
            bwTivaUpdate = new BackgroundWorker();
            bwDLPCUpdate.WorkerReportsProgress = true;
            bwTivaUpdate.WorkerReportsProgress = true;
            bwDLPCUpdate.WorkerSupportsCancellation = true;
            bwTivaUpdate.WorkerSupportsCancellation = true;
            bwDLPCUpdate.DoWork += new DoWorkEventHandler(bwDLPCUpdate_DoWork);
            bwTivaUpdate.DoWork += new DoWorkEventHandler(bwTivaUpdate_DoWork);
            bwDLPCUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwDLPCUpdate_DoWorkCompleted);
            bwTivaUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwTivaUpdate_DoSacnCompleted);
            bwDLPCUpdate.ProgressChanged += new ProgressChangedEventHandler(bwDLPCUpdate_ProgressChanged);
            bwTivaUpdate.ProgressChanged += new ProgressChangedEventHandler(bwTivaUpdate_ProgressChanged);
            ProgressBar.UserCancelRequest += new Action(() => { UserCancelScan = true; });

            bwScan = new BackgroundWorker
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = true
            };
            bwScan.DoWork += new DoWorkEventHandler(bwScan_DoScan);
            bwScan.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwScan_DoSacnCompleted);
        }
        private void MainWindow_Loaded()
        {
            InitGUIItem();
            Device.Init();
            InitSavedScanCfgItems();

            SDK.OnDeviceConnectionLost += new Action<bool>(Device_Disconncted_Handler);
            SDK.OnDeviceConnected += new Action<string>(Device_Connected_Handler);
            SDK.OnDeviceFound += new Action(Device_Found_Handler);
            SDK.OnDeviceError += new Action<string>(Device_Error_Handler);
            SDK.OnButtonScan += new Action(StartButtonScan);
            SDK.OnErrorStatusFound += new Action(RefreshErrorStatus);
            SDK.OnBeginConnectingDevice += new Action<string>(Connecting_Device);
            SDK.OnBeginScan += new Action(BeginScan);
            SDK.OnScanCompleted += new Action(ScanCompleted);
            SDK.OnUSBConnectionBusy += new Action(USBIsBusy);
            SDK.AutoSearch = true;

            LoadConfigDir();
            LoadScanPageSetting();
            TextBox_SaveDirPath.Text = Dir_Scan_For_New;
            TextBox_SavedFileDirPath.Text = Dir_Scan_DataBase;

            if (LogManager.GetRepository().Threshold == log4net.Core.Level.All)
                Label_LogStatus.Text = "Log File Status: Enabled";
            else
                Label_LogStatus.Text = "Log File Status: Disabled";

            this.TopMost = true;
            this.BringToFront();
            this.TopMost = false;
            this.CenterToScreen();

            ColourGenerator generator = new ColourGenerator();
            for (int i = 0; i < NumOfStrokeColors; i++)
            {
                string colorHash = "#FF" + generator.NextColour();
                StrokeColors.Add((SolidColorBrush)new BrushConverter().ConvertFrom(colorHash));
            }
        }
        private void InitGUIItem()
        {
            //init GUI item
            toolStripStatus_DeviceStatus.Image = Properties.Resources.Led_Gray;

            RadioButton_RefNew.Checked = true;
            ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_NEW;
            Button_Scan.Text = "Reference Scan";

            ComboBox_PGAGain.SelectedItem = "64";
            ComboBox_PGAGain.Enabled = false;
            CheckBox_AutoGain.Checked = true;

            var MyTooltipContentMapper = Mappers.Xy<CustomerVm>()
                                        .X(value => value.x)
                                        .Y(value => value.y);
            Charting.For<CustomerVm>(MyTooltipContentMapper);
            
            dataGridView_savescan.RowHeadersVisible = false;
            dataGridView_savescan.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView_savescan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader;
            dataGridView_savescan.AllowUserToResizeRows = false;
            RadioButton_SavedScanSelCsv.Checked = true;
            RadioButton_SavedScanSelDat.Checked = false;

            CheckBox_SaveCombCSV.Checked = true;
            CheckBox_SaveDAT.Checked = true;
        }
        #region Error 
        private String ErrMsg = String.Empty;
        private void RefreshErrorStatus()
        {
            if (Device.ReadErrorStatusAndCode() != 0)
                return;

            ErrMsg = String.Empty; // Clear previous erro strings

            if ((Device.ErrStatus & 0x00000001) > 0)  // Scan Error
            {
                ErrMsg += "Scan Error: ";
                if ((Device.ErrCode[0] & 0x01) > 0)
                    ErrMsg += "DLPC150 Boot Error Detected.    ";
                if ((Device.ErrCode[0] & 0x02) > 0)
                    ErrMsg += "DLPC150 Init Error Detected.    ";
                if ((Device.ErrCode[0] & 0x04) > 0)
                    ErrMsg += "DLPC150 Lamp Driver Error Detected.    ";
                if ((Device.ErrCode[0] & 0x08) > 0)
                    ErrMsg += "DLPC150 Crop Image Failed.    ";
                if ((Device.ErrCode[0] & 0x10) > 0)
                    ErrMsg += "Scan ADC Data Overflow.    ";
                if ((Device.ErrCode[0] & 0x20) > 0)
                    ErrMsg += "Scan Config Invalid.    ";
                if ((Device.ErrCode[0] & 0x40) > 0)
                    ErrMsg += "Scan Pattern Streaming Error.    ";
                if ((Device.ErrCode[0] & 0x80) > 0)
                    ErrMsg += "DLPC150 Read Error.    ";
            }

            if ((Device.ErrStatus & 0x00000002) > 0)  // ADC Error
            {
                if (Device.ErrCode[1] == 1)
                    ErrMsg += "ADC Error: Timeout Error.    ";
                else if (Device.ErrCode[1] == 2)
                    ErrMsg += "ADC Error: PowerDown Error.    ";
                else if (Device.ErrCode[1] == 3)
                    ErrMsg += "ADC Error: PowerUp Error.    ";
                else if (Device.ErrCode[1] == 4)
                    ErrMsg += "ADC Error: Standby Error.    ";
                else if (Device.ErrCode[1] == 5)
                    ErrMsg += "ADC Error: WakeUp Error.    ";
                else if (Device.ErrCode[1] == 6)
                    ErrMsg += "ADC Error: Read Register Error.    ";
                else if (Device.ErrCode[1] == 7)
                    ErrMsg += "ADC Error: Write Register Error.    ";
                else if (Device.ErrCode[1] == 8)
                    ErrMsg += "ADC Error: Configure Error.    ";
                else if (Device.ErrCode[1] == 9)
                    ErrMsg += "ADC Error: Set Buffer Error.    ";
                else if (Device.ErrCode[1] == 10)
                    ErrMsg += "ADC Error: Command Error.    ";
                else if (Device.ErrCode[1] == 11)
                    ErrMsg += "ADC Error: Set PGA Error.    ";
            }

            if ((Device.ErrStatus & 0x00000004) > 0)  // SD Card Error
            {
                ErrMsg += "SD Card Error.    ";
            }

            if ((Device.ErrStatus & 0x00000008) > 0)  // EEPROM Error
            {
                ErrMsg += "EEPROM Error.    ";
            }

            if ((Device.ErrStatus & 0x00000010) > 0)  // BLE Error
            {
                ErrMsg += "Bluetooth Error.    ";
            }

            if ((Device.ErrStatus & 0x00000020) > 0)  // Spectrum Library Error
            {
                ErrMsg += "Spectrum Library Error.    ";
            }

            if ((Device.ErrStatus & 0x00000040) > 0)  // Hardware Error
            {
                if (Device.ErrCode[7] == 1)
                    ErrMsg += "HW Error: DLPC150 Error.    ";
                else if (Device.ErrCode[7] == 2)
                    ErrMsg += "HW Error: Read UUID Error.    ";
                else if (Device.ErrCode[7] == 3)
                    ErrMsg += "HW Error: Flash Initial Error.    ";
            }

            if ((Device.ErrStatus & 0x00000080) > 0)  // TMP Sensor Error
            {
                if (GetFW_LEVEL() == FW_LEVEL.LEVEL_1)
                {
                    // Reset error status because TMP sensor phased out, but older Tiva FW still exist this error status.
                    Device.ResetErrorStatus(0x00000080);
                    RefreshErrorStatus();
                }
                else
                {
                    if (Device.ErrCode[8] == 1)
                        ErrMsg += "TMP Error: Invalid Manufacturing ID.    ";
                    else if (Device.ErrCode[8] == 2)
                        ErrMsg += "TMP Error: Invalid Device ID.    ";
                    else if (Device.ErrCode[8] == 3)
                        ErrMsg += "TMP Error: Reset Error.    ";
                    else if (Device.ErrCode[8] == 4)
                        ErrMsg += "TMP Error: Read Register Error.    ";
                    else if (Device.ErrCode[8] == 5)
                        ErrMsg += "TMP Error: Write Register Error.    ";
                    else if (Device.ErrCode[8] == 6)
                        ErrMsg += "TMP Error: Timeout Error.    ";
                    else if (Device.ErrCode[8] == 7)
                        ErrMsg += "TMP Error: I2C Error.    ";
                }
            }

            if ((Device.ErrStatus & 0x00000100) > 0)  // HDC Sensor Error
            {
                if (Device.ErrCode[9] == 1)
                    ErrMsg += "HDC Error: Invalid Manufacturing ID.    ";
                else if (Device.ErrCode[9] == 2)
                    ErrMsg += "HDC Error: Invalid Device ID.    ";
                else if (Device.ErrCode[9] == 3)
                    ErrMsg += "HDC Error: Reset Error.    ";
                else if (Device.ErrCode[9] == 4)
                    ErrMsg += "HDC Error: Read Register Error.    ";
                else if (Device.ErrCode[9] == 5)
                    ErrMsg += "HDC Error: Write Register Error.    ";
                else if (Device.ErrCode[9] == 6)
                    ErrMsg += "HDC Error: Timeout Error.    ";
                else if (Device.ErrCode[9] == 7)
                    ErrMsg += "HDC Error: I2C Error.    ";
            }

            if ((Device.ErrStatus & 0x00000200) > 0)  // Battery Error
            {
                if (Device.ErrCode[10] == 0x01)
                    ErrMsg += "Battery Error: Battery Low.    ";
            }

            if ((Device.ErrStatus & 0x00000400) > 0)  // Insufficient Memory Error
            {
                ErrMsg += "Not Enough Memory.    ";
            }

            if ((Device.ErrStatus & 0x00000800) > 0)  // UART Error
            {
                ErrMsg += "UART Error.    ";
            }

            if ((Device.ErrStatus & 0x00001000) > 0)  // System Error
            {
                ErrMsg += "System Error: ";
                if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_6)
                {
                    // Byte[13]: High Byte, Byte[14]: Low Byte
                    if ((Device.ErrCode[14] & 0x01) > 0)
                        ErrMsg += "Unstable Lamp ADC.    ";
                    if ((Device.ErrCode[14] & 0x02) > 0)
                        ErrMsg += "Unstable Peak Intensity.    ";
                    if ((Device.ErrCode[14] & 0x04) > 0)
                        ErrMsg += "ADS1255 Error.    ";
                    if ((Device.ErrCode[14] & 0x08) > 0)
                        ErrMsg += "Auto PGA Error.    ";
                    if ((Device.ErrCode[14] & 0x10) > 0)
                        ErrMsg += "Unstable Scan in Repeated times.    ";
                }
                else
                {
                    if ((Device.ErrCode[13] & 0x01) > 0)
                        ErrMsg += "Unstable Lamp ADC.    ";
                    if ((Device.ErrCode[13] & 0x02) > 0)
                        ErrMsg += "Unstable Peak Intensity.    ";
                    if ((Device.ErrCode[13] & 0x04) > 0)
                        ErrMsg += "ADS1255 Error.    ";
                    if ((Device.ErrCode[13] & 0x08) > 0)
                        ErrMsg += "Auto PGA Error.    ";
                    if ((Device.ErrCode[14] & 0x01) > 0)
                        ErrMsg += "Unstable Scan in Repeated times.    ";
                }
            }

            label_ErrorStatus.Text = ErrMsg;
            label_ErrorStatus.ForeColor = System.Drawing.Color.Red;
        }

        private void Device_Error_Handler(string error)
        {
            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2)
            {
                Message.ShowWarning(error);  // Device Information, Calibration Coefficients, Configuration Lists       
            }
        }

        public void ShowWarning(String Text)
        {
            String text = Text;
            MessageBox.Show(text, "Warning");
        }
        #endregion

        #region connect device
        private void Device_Disconncted_Handler(bool error)
        {
            BeginInvoke((Action)(() => //Invoke at UI thread
            {
                this.Text = AppName + string.Format("v{0}: No device connected", this.Version);
                ListBox_LocalCfgs.Items.Clear();
                ListBox_TargetCfgs.Items.Clear();
                toolStripStatus_DeviceStatus.Image = Properties.Resources.Led_R;
                toolStripStatus_DeviceStatus.Text = "Device Disconnect!";
                ClearScanPlotsUI();
                if (error)
                {
                    SDK.AutoSearch = false;
                    SDK.IsConnectionChecking = false;
                    DialogResult result = Message.ShowQuestion("Device disconnection detected !\n\nThe GUI will restart to sanitize cache.\n\nClick \"Yes\" to restart, \"No\" to close this GUI.", "Device Disconnected");
                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                        Application.Restart();
                    }
                    else
                        this.Close();
                }
                else
                {
                    DBG.WriteLine("Device disconnected successfully !");
                    logFile.Info("Device disconnected successfully !");
                }
                UI_no_connection();
            }), null);
        }
        private void Device_Found_Handler()
        {
            SDK.AutoSearch = false;
            int devCounts = Device.DeviceFound.Count();
            if (devCounts > 1) 
            {
                string[,] devList = new string[devCounts, 2];
                for(int i = 0; i < devCounts; i++)
                {
                    devList[i, 0] = Device.DeviceFound[i].ProductString;
                    devList[i, 1] = Device.DeviceFound[i].SerialNumber;
                }
                var frm = new DeviceSelection(devList);
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.ShowDialog(this);
                Device.Open(frm.SelectedDeviceSerialNumber);
                frm.Dispose();
            }
            else 
            { 
                Device.Open(null); 
            }
        }
        private void Enumerate_Devices()
        {
            //Device.Enumerate();
        }
        private void Connecting_Device(String ModelnSN)
        {
            ProgressWindowStart("Device Open", "Connecting to the device... \r\nPlease Wait!", false);
        }

        private void Device_Connected_Handler(String SerialNumber)
        {
            IsFetchingDeviceInfo = true;
            IsFetchingDeviceInfoWithError = false;
            FetchingDevInfoErrMsg = "";

            if (SerialNumber == null)
            {
                ProgressWindowCompleted();
                SDK.AutoSearch = true;
                SDK.IsConnectionChecking = true;
                IsFetchingDeviceInfo = false;
                return;
            }

            this.Text = AppName + string.Format("v{0}: {1} (Wavelength Range: {2} - {3}nm)",
                this.Version,
                Device.DevInfo.MinWavelength == 900 ? "Standard" : Device.DevInfo.MinWavelength == 1350 ? "Extended" : "Extended Plus",
                Device.DevInfo.MinWavelength, Device.DevInfo.MaxWavelength);

            bool settingFail = false;
            String warningMsg = String.Empty;

            try
            {
                // Clear old information
                BackupFacRef_Msg = "";
                RestoreFacRef_Msg = "";
                Label_SensorBattCapacity.Text = "";
                Label_SensorBattStatus.Text = "";
                Label_SensorHumidity.Text = "";
                Label_SensorSysTemp.Text = "";
                Label_SensorTivaTemp.Text = "";
                Label_SensorLampVM1Value.Text = "";
                Label_SensorLampCM1Value.Text = "";
                Label_SensorLampVM2Value.Text = "";
                Label_SensorLampCM2Value.Text = "";
                Label_CalCoeffVer.Text = "";
                Label_RefCalVer.Text = "";
                Label_ScanCfgVer.Text = "";
                TextBox_P2WCoeff0.Text = "";
                TextBox_P2WCoeff1.Text = "";
                TextBox_P2WCoeff2.Text = "";
                TextBox_ShiftVectCoeff0.Text = "";
                TextBox_ShiftVectCoeff1.Text = "";
                TextBox_ShiftVectCoeff2.Text = "";
                TextBox_Key.Text = "";
                TextBox_ModelName.Text = "";
                TextBox_SerialNumber.Text = "";
                TextBox_DateTime.Text = "";
                TextBox_LampUsage.Text = "";
                TextBox_BLE_Display_Name.Text = "";

                do
                {
                    DBG.WriteLine("Device <{0}> connected successfullly !", SerialNumber);
                    logFile.InfoFormat("Device <{0}> connected successfullly !", SerialNumber);

                    // Check HW version valid or not
                    byte MB_Ver = (Device.IsConnected() && !string.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev).First() : (byte)0;
                    byte DB_Ver = (Device.IsConnected() && !string.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev).ElementAt(4) : (byte)0;
                    if (MB_Ver == 'X' || MB_Ver == '?')
                        warningMsg += "Main board version check: unknown!\n";
                    if (DB_Ver == 'X' || DB_Ver == '?')
                        warningMsg += "Detector board version check: unknown!\n";
                    if (MB_Ver == 'X' || MB_Ver == '?' || DB_Ver == 'X' || DB_Ver == '?')
                        warningMsg += "If the scan cannot be performed correctly, please contact your agent for more information.\n\n";

                    // Checking if a valid scan config flag
                    if (Device.DevInfo.CfgRev == 0 || Device.DevInfo.CfgRev == 255 || ScanConfig.GetTargetCfgListNum() == 0)
                    {
                        warningMsg += "There is no scan config in the device!\n";
                    }

                    // Checking if a valid cal coeff flag
                    if (Device.DevInfo.CalRev == 0 || Device.DevInfo.CalRev == 255)
                    {
                        warningMsg += "There is no valid calibration coefficients in the device!\n";
                    }

                    // Checking if a valid ref cal flag
                    if (Device.DevInfo.RefCalRev == 0 || Device.DevInfo.RefCalRev == 255)
                    {
                        warningMsg += "There is no valid reference calibration data in the device!\nPlease do the reference calibration before a scan.\n\n";
                    }

                    // Checking if a valid model name
                    if (Device.DevInfo.ModelName == "")
                    {
                        warningMsg += "There is no model name data in the device!\n";
                    }

                    // Checking if a valid serial number
                    if (Device.DevInfo.SerialNumber == "")
                    {
                        warningMsg += "There is no serial number data in the device!\n";
                    }

                    if (GetFW_LEVEL(true) == FW_LEVEL.LEVEL_1)
                    {
                        warningMsg += "The version is too old.\nPlease update your TIVA FW.\n\n";
                    }
                    else if (GetFW_LEVEL() == FW_LEVEL.LEVEL_0)
                    {
                        warningMsg += "The device is not ISC product. Functions may be abnormal!\n\n";
                    }

                    if ((GetFW_LEVEL() >= FW_LEVEL.LEVEL_2 && Device.ChkBleExist() == 1) || MB_Ver == 0)
                        Device.SetBluetooth(false);

                    // Sync device date time
                    DateTime Current = DateTime.Now;
                    Device.DeviceDateTime DevDateTime = new Device.DeviceDateTime
                    {
                        Year = Current.Year,
                        Month = Current.Month,
                        Day = Current.Day,
                        DayOfWeek = (Int32)Current.DayOfWeek,
                        Hour = Current.Hour,
                        Minute = Current.Minute,
                        Second = Current.Second
                    };
                    Device.SetDateTime(DevDateTime);

                    // Scan Config
                    LoadLocalCfgList();
                    PopulateCfgDetailItems();
                    RefreshTargetCfgList();  // Only refresh UI because target config list has been loaded after device opened

                    // Check activation status and automatically activate if we have the key in database
                    GetActivationKeyStatus();
                    AutoSetKey();

                    // Get device information
                    GetDeviceInfo();

                    // Backup Factory Reference Data
                    DeviceConnectBackUpRef();

                    // Refresh error status
                    RefreshErrorStatus();

                    // Set config table enable
                    EnableCfgItem(false);

                    // Scan Plot Area
                    Int32 ActiveIndex = ScanConfig.GetTargetActiveScanIndex();
                    if (ActiveIndex >= 0)
                    {
                        ListBox_TargetCfgs.SelectedIndex = ActiveIndex;
                        String setCfgMsg = SetScanConfig(ScanConfig.TargetConfig[ActiveIndex], true, ActiveIndex);

                        if (setCfgMsg != "")
                            warningMsg += "Apply active scan config failed!";
                    }
                    else if (ActiveIndex == SDK.RETURN_FAIL)
                    {
                        Int32 ret = ScanConfig.SetTargetActiveScanIndex(0);
                        settingFail = (ret != SDK.RETURN_PASS) ? true : false;
                        warningMsg += "There is no valid scan config index in the device!\nSet default scan config index to device: " + (ret == SDK.RETURN_PASS ? "Success\n\n" : "Fail\n\n");
                    }

                    if (Scan.IsLocalRefExist)
                        RadioButton_RefPre.Enabled = true;
                    else
                        RadioButton_RefPre.Enabled = false;
                } while (false);

                Thread.Sleep(SDK.ConnectionCheckInterval);  //Check if the lost info is caused by connection lost or read failed
                if (!Device.IsConnected())
                    settingFail = true;
            }
            catch (Exception e)
            {
                BeginInvoke((Action)(() => //Invoke at UI thread
                {
                    ProgressWindowCompleted();
                    Message.ShowError("Something wrong during device reading...\n{0}", e.Message);
                }), null);
            }

            BeginInvoke((Action)(() => //Invoke at UI thread
            {
                UI_Setting_Connected();
                Chart_Refresh();
                if (userDefaultReference == ScanReference.Built_in)
                    RadioButton_RefFac.Checked = true;
                else if (userDefaultReference == ScanReference.Previous)
                    RadioButton_RefPre.Checked = true;
                else
                    RadioButton_RefNew.Checked = true;

                ProgressWindowCompleted();

                // Notify user if the device is using generic coefficients
                try
                {
                    if (Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[0] - std_calib_coeffs_PixelToWavelengthCoeffs_0) / std_calib_coeffs_PixelToWavelengthCoeffs_0) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[1] - std_calib_coeffs_PixelToWavelengthCoeffs_1) / std_calib_coeffs_PixelToWavelengthCoeffs_1) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[2] - std_calib_coeffs_PixelToWavelengthCoeffs_2) / std_calib_coeffs_PixelToWavelengthCoeffs_2) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[0] - std_calib_coeffs_ShiftVectorCoeffs_0) / std_calib_coeffs_ShiftVectorCoeffs_0) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[1] - std_calib_coeffs_ShiftVectorCoeffs_1) / std_calib_coeffs_ShiftVectorCoeffs_1) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[2] - std_calib_coeffs_ShiftVectorCoeffs_2) / std_calib_coeffs_ShiftVectorCoeffs_2) < calib_coeffs_diff_limit)
                    {
                        Message.ShowWarning("The device's coefficients seem to have problems.\n\n" +
                            "You can try to Restore Factory Calibration Data\n" +
                            "Or\n" +
                            "Please contact ISC for supporting.");
                    }
                }
                catch { }

                if (warningMsg != String.Empty)
                    Message.ShowWarning(warningMsg);

                if (IsFetchingDeviceInfoWithError) // So far only error type is connected with config errors
                {
                    Message.ShowError(FetchingDevInfoErrMsg, "Config Error", this);
                    tabScanPage.SelectedIndex = 1;
                    TargetCfg_SelIndex = ScanConfig.GetTargetActiveScanIndex();
                    Button_CfgEdit.PerformClick();
                    Button_CfgCancel.Enabled = false;
                    GroupBox_CfgDetails.BackColor = Color.LightYellow;
                    Button_CfgSave.BackColor = Color.LightYellow;
                }
            }), null);
            SDK.AutoSearch = true;
            SDK.IsConnectionChecking = true;
            IsFetchingDeviceInfo = false;
        }
        private void UI_Setting_Connected()
        {
            Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
            Int32 MB_Ver = HWRev[0];

            //Device status
            toolStripStatus_DeviceStatus.Image = Properties.Resources.Led_G;
            UpdateDeviceStatusToolTip();
            OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));

            UI_On_Connection();//已經連線，會開啟GUI使用     

            if (IsFetchingDeviceInfo && Device.ErrStatus != 0)
            {
                DateTime current = DateTime.Now;
                String FileName;

                if (Device.DevInfo.Manufacturing_SerialNumber.Length >= 18)
                    FileName = Device.DevInfo.Manufacturing_SerialNumber;
                else
                    FileName = Device.DevInfo.ModelName + "_" + Device.DevInfo.SerialNumber;

                FileName = Path.Combine(Dir_Scan_For_New, FileName + "_Connected_Error_Found_" + current.ToString("yyyyMMdd_HHmmss") + ".csv");

                FileStream fs = new FileStream(@FileName, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                sw.Flush();
                sw.Close();
            }

            if (!CheckBox_AutoGain.Checked)
            {
                CheckBox_AutoGain.Checked = false;
                CheckBox_AutoGain.Enabled = true;
                ComboBox_PGAGain.Enabled = true;
            }
            else
            {
                CheckBox_AutoGain.Checked = true;
                CheckBox_AutoGain.Enabled = true;
                ComboBox_PGAGain.Enabled = false;
            }

            EnableCfgItem(false);
            if (!CheckBox_Cal_WriteEnable.Checked)
            {
                //utility item
                Button_Cal_WriteCoeffs.Enabled = false;
                Button_Cal_WriteGenCoeffs.Enabled = false;
                Button_Cal_RestoreDefaultCoeffs.Enabled = false;
            }
            else
            {
                //utility item
                Button_Cal_WriteCoeffs.Enabled = true;
                Button_Cal_WriteGenCoeffs.Enabled = true;
                if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2 && IsActivated)
                    Button_Cal_RestoreDefaultCoeffs.Enabled = true;
                else
                    Button_Cal_RestoreDefaultCoeffs.Enabled = false;
            }

            Button_CfgSave.Enabled = false;
            Button_CfgCancel.Enabled = false;
            if (GetFW_LEVEL() < FW_LEVEL.LEVEL_2)
            {
                groupBox_ActivationKey.Enabled = false;
                button_DeviceRestoreFacRef.Enabled = false;
            }
            else
            {
                if (IsFetchingDeviceInfo)
                    groupBox_ActivationKey.Enabled = true;

                //check Factory reference can backup and restore or not 
                if (!CheckFactoryRefData())
                {
                    button_DeviceRestoreFacRef.Enabled = false;
                    label_RestoreFacRef.Enabled = false;
                    button_DeviceRestoreFacRef.Text = "N/A";
                    RestoreFacRef_Msg = "Can not find the factory reference backup file locally!\n";
                    RestoreFacRef_Msg += BackupFacRef_Msg;
                    button_restore_fac_ref_warning.Visible = true;
                }
                else
                {
                    button_DeviceRestoreFacRef.Enabled = true;
                    label_RestoreFacRef.Enabled = true;
                    button_DeviceRestoreFacRef.Text = "Restore";
                    RestoreFacRef_Msg = "";
                    button_restore_fac_ref_warning.Visible = false;
                }
            }

            if (GetFW_LEVEL() < FW_LEVEL.LEVEL_1)
            {
                GroupBox_ModelName.Enabled = false;
            }
            else
            {
                GroupBox_ModelName.Enabled = true;
            }
            if (GetFW_LEVEL() == FW_LEVEL.LEVEL_1)
            {
                label_MFC_Seri_Num.Enabled = false;
            }
            else
            {
                label_MFC_Seri_Num.Enabled = true;
            }

            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4)
            {
                if (MB_Ver == 'E')
                {
                    Label_ButtonStatus.Visible = false;
                    Button_LockButton.Visible = false;
                    Button_UnlockButton.Visible = false;
                    GroupBox_BleName.Visible = false;
                    Label_Blename.Visible = false;
                    Label_BleNameValue.Visible = false;
                }
                else
                {
                    Label_ButtonStatus.Visible = true;
                    Button_LockButton.Visible = true;
                    Button_UnlockButton.Visible = true;
                    GroupBox_BleName.Visible = true;
                    Label_Blename.Visible = true;
                    Label_BleNameValue.Visible = true;

                    Label_ButtonStatus.Enabled = IsActivated;
                    Button_LockButton.Enabled = IsActivated;
                    Button_UnlockButton.Enabled = IsActivated;
                    GroupBox_BleName.Enabled = IsActivated;
                    Label_Blename.Enabled = IsActivated;
                    Label_BleNameValue.Enabled = IsActivated;

                    if (IsActivated)
                    {
                        Int32 status = Device.GetButtonLockStatus();
                        if (status == 1)
                            Label_ButtonStatus.Text = "Button Status: Locked!";
                        else if (status == 0)
                            Label_ButtonStatus.Text = "Button Status: Unlocked!";
                        else
                            Label_ButtonStatus.Text = "Button Status: Read Failed!";
                    }
                    else
                        Label_ButtonStatus.Text = "Button Status: NA";
                }
            }
            else
            {
                Label_ButtonStatus.Visible = false;
                Button_LockButton.Visible = false;
                Button_UnlockButton.Visible = false;
                GroupBox_BleName.Visible = false;
                Label_Blename.Visible = false;
                Label_BleNameValue.Visible = false;
            }

            //init scan config list UI 
            if (SelCfg_IsTarget)
            {
                ListBox_LocalCfgs.BackColor = System.Drawing.Color.White;
                ListBox_TargetCfgs.BackColor = System.Drawing.Color.AliceBlue;
                Button_SetActive.Enabled = true;
            }
            else
            {
                ListBox_TargetCfgs.BackColor = System.Drawing.Color.White;
                ListBox_LocalCfgs.BackColor = System.Drawing.Color.AliceBlue;
                Button_SetActive.Enabled = false;
            }
        }
        private void UpdateDeviceStatusToolTip()
        {
            toolStripStatus_DeviceStatus.Image = Properties.Resources.Led_G;
            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2 && !IsActivated)
            {
                toolStripStatus_DeviceStatus.Text = (Device.DevInfo.MinWavelength == 900 ? "Standard Wavelength " : Device.DevInfo.MinWavelength == 1350 ? "Extended Wavelength " : "Extended Plus Wavelength ") +
                    "Device: " + Device.DevInfo.ModelName + " (" + Device.DevInfo.SerialNumber + "), advanced functions locked!";
            }
            else
            {
                toolStripStatus_DeviceStatus.Text = (Device.DevInfo.MinWavelength == 900 ? "Standard Wavelength " : Device.DevInfo.MinWavelength == 1350 ? "Extended Wavelength " : "Extended Plus Wavelength ") +
                    "Device: " + Device.DevInfo.ModelName + " (" + Device.DevInfo.SerialNumber + ")";
            }
        }
        private void GetBuildInRefTime()
        {
            Scan.GetRefTime(Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN);
            Byte[] buildintime = Scan.ReferenceScanDateTime;
            String refname = Scan.ReferenceScanConfigData.head.config_name;
            if (buildintime[0] != 0)
            {
                if (refname == "SystemTest")
                {
                    buildin_ref_time = "Factory Reference : 20" + buildintime[0].ToString() + "/" + buildintime[1].ToString() + "/" + buildintime[2].ToString()
                            + " @ " + buildintime[3].ToString() + ":" + buildintime[4].ToString() + ":" + buildintime[5].ToString();
                }
                else
                {
                    buildin_ref_time = "User Reference : 20" + buildintime[0].ToString() + "/" + buildintime[1].ToString() + "/" + buildintime[2].ToString()
                            + " @ " + buildintime[3].ToString() + ":" + buildintime[4].ToString() + ":" + buildintime[5].ToString();
                }
            }
        }
        private Boolean CheckFactoryRefData()
        {
            String FacRefFile = Device.DevInfo.SerialNumber + "_FacRef.dat";
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String FilePath = Path.Combine(path, "InnoSpectra\\Reference Data", FacRefFile);

            if (File.Exists(FilePath))
                return true;
            else if (DeviceConnectBackUpRef())
                return true;
            else
                return false;
        }
        private void StartButtonScan()
        {
            ScanButtonPressed = true;
            BeginInvoke((Action)(() => //Invoke at UI thread
            {
                if (tabControl_MainFunctions.SelectedIndex != 0)
                    tabControl_MainFunctions.SelectedIndex = 0;
                Button_Scan_Click(null, null);
            }), null);
        }
        #endregion

        #region init
        private void InitSavedScanCfgItems()
        {
            Label_SavedScanType.Clear();
            Label_SavedScanType.Add(Label_SavedScanType1);
            Label_SavedScanType.Add(Label_SavedScanType2);
            Label_SavedScanType.Add(Label_SavedScanType3);
            Label_SavedScanType.Add(Label_SavedScanType4);
            Label_SavedScanType.Add(Label_SavedScanType5);
            Label_SavedRangeStart.Clear();
            Label_SavedRangeStart.Add(Label_SavedRangeStart1);
            Label_SavedRangeStart.Add(Label_SavedRangeStart2);
            Label_SavedRangeStart.Add(Label_SavedRangeStart3);
            Label_SavedRangeStart.Add(Label_SavedRangeStart4);
            Label_SavedRangeStart.Add(Label_SavedRangeStart5);
            Label_SavedRangeEnd.Clear();
            Label_SavedRangeEnd.Add(Label_SavedRangeEnd1);
            Label_SavedRangeEnd.Add(Label_SavedRangeEnd2);
            Label_SavedRangeEnd.Add(Label_SavedRangeEnd3);
            Label_SavedRangeEnd.Add(Label_SavedRangeEnd4);
            Label_SavedRangeEnd.Add(Label_SavedRangeEnd5);
            Label_SavedWidth.Clear();
            Label_SavedWidth.Add(Label_SavedWidth1);
            Label_SavedWidth.Add(Label_SavedWidth2);
            Label_SavedWidth.Add(Label_SavedWidth3);
            Label_SavedWidth.Add(Label_SavedWidth4);
            Label_SavedWidth.Add(Label_SavedWidth5);
            Label_SavedDigRes.Clear();
            Label_SavedDigRes.Add(Label_SavedDigRes1);
            Label_SavedDigRes.Add(Label_SavedDigRes2);
            Label_SavedDigRes.Add(Label_SavedDigRes3);
            Label_SavedDigRes.Add(Label_SavedDigRes4);
            Label_SavedDigRes.Add(Label_SavedDigRes5);
            Label_SavedExposure.Clear();
            Label_SavedExposure.Add(Label_SavedExposure1);
            Label_SavedExposure.Add(Label_SavedExposure2);
            Label_SavedExposure.Add(Label_SavedExposure3);
            Label_SavedExposure.Add(Label_SavedExposure4);
            Label_SavedExposure.Add(Label_SavedExposure5);

            ClearSavedScanCfgItems();
        }

        private void PopulateCfgDetailItems()
        {
            ComboBox_CfgScanType.Clear();
            ComboBox_CfgScanType1.Items.Clear();
            ComboBox_CfgScanType2.Items.Clear();
            ComboBox_CfgScanType3.Items.Clear();
            ComboBox_CfgScanType4.Items.Clear();
            ComboBox_CfgScanType5.Items.Clear();
            ComboBox_CfgScanType.Add(ComboBox_CfgScanType1);
            ComboBox_CfgScanType.Add(ComboBox_CfgScanType2);
            ComboBox_CfgScanType.Add(ComboBox_CfgScanType3);
            ComboBox_CfgScanType.Add(ComboBox_CfgScanType4);
            ComboBox_CfgScanType.Add(ComboBox_CfgScanType5);
            ComboBox_CfgWidth.Clear();
            ComboBox_CfgWidth1.Items.Clear();
            ComboBox_CfgWidth2.Items.Clear();
            ComboBox_CfgWidth3.Items.Clear();
            ComboBox_CfgWidth4.Items.Clear();
            ComboBox_CfgWidth5.Items.Clear();
            ComboBox_CfgWidth.Add(ComboBox_CfgWidth1);
            ComboBox_CfgWidth.Add(ComboBox_CfgWidth2);
            ComboBox_CfgWidth.Add(ComboBox_CfgWidth3);
            ComboBox_CfgWidth.Add(ComboBox_CfgWidth4);
            ComboBox_CfgWidth.Add(ComboBox_CfgWidth5);
            ComboBox_CfgExposure.Clear();
            ComboBox_CfgExposure1.Items.Clear();
            ComboBox_CfgExposure2.Items.Clear();
            ComboBox_CfgExposure3.Items.Clear();
            ComboBox_CfgExposure4.Items.Clear();
            ComboBox_CfgExposure5.Items.Clear();
            ComboBox_CfgExposure.Add(ComboBox_CfgExposure1);
            ComboBox_CfgExposure.Add(ComboBox_CfgExposure2);
            ComboBox_CfgExposure.Add(ComboBox_CfgExposure3);
            ComboBox_CfgExposure.Add(ComboBox_CfgExposure4);
            ComboBox_CfgExposure.Add(ComboBox_CfgExposure5);
            TextBox_CfgRangeStart.Clear();
            TextBox_CfgRangeStart.Add(TextBox_CfgRangeStart1);
            TextBox_CfgRangeStart.Add(TextBox_CfgRangeStart2);
            TextBox_CfgRangeStart.Add(TextBox_CfgRangeStart3);
            TextBox_CfgRangeStart.Add(TextBox_CfgRangeStart4);
            TextBox_CfgRangeStart.Add(TextBox_CfgRangeStart5);
            TextBox_CfgRangeEnd.Clear();
            TextBox_CfgRangeEnd.Add(TextBox_CfgRangeEnd1);
            TextBox_CfgRangeEnd.Add(TextBox_CfgRangeEnd2);
            TextBox_CfgRangeEnd.Add(TextBox_CfgRangeEnd3);
            TextBox_CfgRangeEnd.Add(TextBox_CfgRangeEnd4);
            TextBox_CfgRangeEnd.Add(TextBox_CfgRangeEnd5);
            TextBox_CfgDigRes.Clear();
            TextBox_CfgDigRes.Add(TextBox_CfgDigRes1);
            TextBox_CfgDigRes.Add(TextBox_CfgDigRes2);
            TextBox_CfgDigRes.Add(TextBox_CfgDigRes3);
            TextBox_CfgDigRes.Add(TextBox_CfgDigRes4);
            TextBox_CfgDigRes.Add(TextBox_CfgDigRes5);
            Label_overSampleRate.Clear();
            Label_overSampleRate.Add(label_overSampleRate1);
            Label_overSampleRate.Add(label_overSampleRate2);
            Label_overSampleRate.Add(label_overSampleRate3);
            Label_overSampleRate.Add(label_overSampleRate4);
            Label_overSampleRate.Add(label_overSampleRate5);

            for (Int32 i = 0; i < MAX_CFG_SECTION; i++)
            {
                // Initialize combobox items
                for (Int32 j = 0; j < 2; j++)
                {
                    String Type = Helper.ScanTypeIndexToMode(j).Substring(0, 3);
                    ComboBox_CfgScanType[i].Items.Add(Type);
                }
                for (Int32 j = 0; j < Helper.CfgWidthItemsCount(); j++)
                {
                    Double WidthNM = Helper.CfgWidthIndexToNM(j);
                    ComboBox_CfgWidth[i].Items.Add(Math.Round(WidthNM, 2));
                }
                for (Int32 j = 0; j < Helper.CfgExpItemsCount(); j++)
                {
                    Double ExpTime = Helper.CfgExpIndexToTime(j);
                    ComboBox_CfgExposure[i].Items.Add(ExpTime);
                }
            }
        }

        private void SetDetailColorWhite()
        {
            TextBox_CfgName.BackColor = System.Drawing.Color.White;
            TextBox_CfgAvg.BackColor = System.Drawing.Color.White;
            for (int i = 0; i < TextBox_CfgRangeStart.Count; i++)
            {
                TextBox_CfgRangeStart[i].BackColor = System.Drawing.Color.White;
                TextBox_CfgRangeEnd[i].BackColor = System.Drawing.Color.White;
                TextBox_CfgDigRes[i].BackColor = System.Drawing.Color.White;
            }
        }

        private void EnableCfgItem(bool enable)
        {
            TextBox_CfgName.Enabled = enable;
            TextBox_CfgAvg.Enabled = enable;
            comboBox_cfgNumSec.Enabled = enable;
            for (int i = 0; i < 5; i++)
            {
                ComboBox_CfgScanType[i].Enabled = enable;
                TextBox_CfgRangeStart[i].Enabled = enable;
                TextBox_CfgRangeEnd[i].Enabled = enable;
                ComboBox_CfgWidth[i].Enabled = enable;
                TextBox_CfgDigRes[i].Enabled = enable;
                ComboBox_CfgExposure[i].Enabled = enable;
            }
        }

        private void InitCfgDetailsContent()
        {
            TextBox_CfgName.Clear();
            TextBox_CfgAvg.Clear();
            comboBox_cfgNumSec.SelectedIndex = 0;

            for (Int32 i = 0; i < MAX_CFG_SECTION; i++)
            {
                ComboBox_CfgScanType[i].SelectedIndex = 0;
                TextBox_CfgRangeStart[i].Clear();
                TextBox_CfgRangeEnd[i].Clear();
                ComboBox_CfgWidth[i].SelectedIndex = 4;
                ComboBox_CfgExposure[i].SelectedIndex = 0;
                TextBox_CfgDigRes[i].Clear();
                Label_overSampleRate[i].Text = String.Empty;
            }
        }
        private void LoadConfigDir()
        {
            // Config Directory
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ConfigDir = Path.Combine(path, "InnoSpectra\\Config Data");

            if (Directory.Exists(ConfigDir) == false)
            {
                Directory.CreateDirectory(ConfigDir);
                try { AddDirectorySecurity(ConfigDir); }
                catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }

                DBG.WriteLine("The directory {0} was created.", ConfigDir);
                logFile.InfoFormat("The directory {0} was created.", ConfigDir);
            }
        }
        private void LoadScanPageSetting()
        {
            String FilePath = Path.Combine(ConfigDir, "ScanPageSettings.xml");
            if (!File.Exists(FilePath))
            {
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Dir_Scan_For_New = Path.Combine(path, "InnoSpectra\\Scan Results");
                Dir_Scan_DataBase = Dir_Scan_For_New;
                TextBox_SavedFileDirPath.Text = Dir_Scan_DataBase;
                ScanFile_Formats = 0x81;
                CSV_Delimiter = ",";

                if (Directory.Exists(Dir_Scan_For_New) == false)
                {
                    Directory.CreateDirectory(Dir_Scan_For_New);
                    try { AddDirectorySecurity(Dir_Scan_For_New); }
                    catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }

                    DBG.WriteLine("The directory {0} was created.", Dir_Scan_For_New);
                    logFile.InfoFormat("The directory {0} was created.", Dir_Scan_For_New);
                }
            }
            else
            {
                Dir_Scan_For_New = Path.Combine(Directory.GetCurrentDirectory(), "Scan Results");
                Dir_Scan_DataBase = Dir_Scan_For_New;
                ScanFile_Formats = 0x81;
                CSV_Delimiter = ",";

                try
                {
                    XmlDocument XmlDoc = new XmlDocument();
                    XmlDoc.Load(FilePath);

                    XmlNode ScanDir = XmlDoc.SelectSingleNode("/Settings/ScanDir");
                    if (ScanDir.InnerText != String.Empty)
                        Dir_Scan_For_New = ScanDir.InnerText;

                    XmlNode DisplayDir = XmlDoc.SelectSingleNode("/Settings/DisplayDir");
                    if (DisplayDir.InnerText != String.Empty)
                        Dir_Scan_DataBase = DisplayDir.InnerText;

                    XmlNode FileFormats = XmlDoc.SelectSingleNode("/Settings/FileFormats");
                    if (FileFormats.InnerText != String.Empty)
                        ScanFile_Formats = Int32.Parse(FileFormats.InnerText);

                    string readDelimiter = "";
                    XmlNode CSVDelimiter = XmlDoc.SelectSingleNode("/Settings/CSVDelimiter");
                    if (CSVDelimiter != null)
                    {
                        if (FileFormats.InnerText != String.Empty)
                            readDelimiter = CSVDelimiter.InnerText;
                        if (readDelimiter == "TAB")
                            CSV_Delimiter = "\t";
                        else
                            CSV_Delimiter = readDelimiter;
                    }
                    else
                        CSV_Delimiter = ",";
                }
                catch (UnauthorizedAccessException UAEx) { DBG.WriteLine(UAEx.Message); logFile.Error(UAEx.Message); }
                catch (PathTooLongException PathEx) { DBG.WriteLine(PathEx.Message); logFile.Error(PathEx.Message); }
                catch (Exception e) { DBG.WriteLine(e.Message); logFile.Error(e.Message); }
            }
            Int32 buf_ScanFile_Formats = ScanFile_Formats;
            //CheckBox_SaveCombCSV.Checked = ((buf_ScanFile_Formats & 0x01) >> 0 == 1) ? true : false;
            CheckBox_SaveICSV.Checked = ((buf_ScanFile_Formats & 0x02) >> 1 == 1) ? true : false;
            CheckBox_SaveACSV.Checked = ((buf_ScanFile_Formats & 0x04) >> 2 == 1) ? true : false;
            CheckBox_SaveRCSV.Checked = ((buf_ScanFile_Formats & 0x08) >> 3 == 1) ? true : false;
            CheckBox_SaveIJDX.Checked = ((buf_ScanFile_Formats & 0x10) >> 4 == 1) ? true : false;
            CheckBox_SaveAJDX.Checked = ((buf_ScanFile_Formats & 0x20) >> 5 == 1) ? true : false;
            CheckBox_SaveRJDX.Checked = ((buf_ScanFile_Formats & 0x40) >> 6 == 1) ? true : false;
            //CheckBox_SaveDAT.Checked = ((buf_ScanFile_Formats & 0x80) >> 7 == 1) ? true : false;

            if (CheckBox_SaveIJDX.Checked == true || CheckBox_SaveAJDX.Checked == true || CheckBox_SaveRJDX.Checked == true)
                CheckBox_SaveJDX.Checked = true;
            else if (CheckBox_SaveIJDX.Checked == false && CheckBox_SaveAJDX.Checked == false && CheckBox_SaveRJDX.Checked == false)
                CheckBox_SaveJDX.Checked = false;
        }
        #endregion

        #region Scan
        private void BeginScan()
        {
            if (!PBW.IsDisposed)
                return;
            else if (ScanButtonPressed)
            {
                if ((TargetScanCounts - ScannedCounts) == 1)
                    ProgressWindowStart("Scan Button Pressed", "Scan in progress... Please Wait!", false);
                else
                    ProgressWindowStart("Scan Button Pressed", "Scan in progress... Please Wait!", true);
                ScanButtonPressed = false;
            }
            else if (ReferenceSelect == Scan.SCAN_REF_TYPE.SCAN_REF_NEW || (TargetScanCounts - ScannedCounts) == 1)
            {
                ProgressWindowStart("Scan", "Scan in progress... \r\nPlease Wait!", false);
            }
            else
            {
                ProgressWindowStart("Scan", "Continuous scan in progress... \r\nPlease Wait!", true);
            }
        }
        private void ScanCompleted()
        {
            if ((TargetScanCounts - ScannedCounts) > 0 && !UserCancelScan)
            { }
            else
                ProgressWindowCompleted();
        }
        private void USBIsBusy()
        {
            BleMsgForm frm = new BleMsgForm(false);

            //Close Progress Bar but don't enable controls
            if (PBW != null)
            {
                PBW.TopMost = false;
                PBW.Close();
                PBW.Dispose();
            }

            this.Invoke(new Action(() =>
            {
                this.Activate();
                frm.ShowDialog(this);
                this.TopMost = true;
                this.BringToFront();
                this.TopMost = false;
            }));

            if (frm.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                frm.Dispose();
                Thread t = new Thread(BluetoothWait);
                t.Start();
            }
            else if (frm.DialogResult == System.Windows.Forms.DialogResult.Abort)
                this.Close();
        }
        private void BluetoothWait()
        {
            BleMsgForm frm = new BleMsgForm(true);
            frm.ShowDialog(this);
            if (frm.DialogResult == System.Windows.Forms.DialogResult.Abort)
                this.Close();
            else
            {
                Device.Close();
                Device.Open(null);
            }
        }
        private void RadioButton_RefNew_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_RefNew.Checked == true)
            {
                Manual_ContScan_UI_Con(false);
                Button_Scan.Text = "Reference Scan";
                ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_NEW;
                CheckBox_AutoGain.Checked = true;
                RadioButton_Intensity.Checked = true;
                RadioButton_Reflectance.Enabled = false;
                RadioButton_Absorbance.Enabled = false;
                RadioButton_Reference.Enabled = false;
                RadioButton_LampOff.Enabled = false;
                Check_Overlay.Checked = false;
                Check_Overlay.Enabled = false;
                GroupBox_ContScan.Enabled = false;
                GroupBox_SaveScan.Enabled = false;
                CheckBox_SaveOneCSV.Enabled = false;
                CheckBox_AverageCSV.Enabled = false;
                checkBox_StopOnError.Enabled = false;
                Text_ContScan.Text = "1";
                checkBox_AutoScan.Enabled = false;
                label_ref.Visible = false;
                Clear_Chart();
            }
            else
            {
                GroupBox_SaveScan.Enabled = true;
                RadioButton_Reflectance.Enabled = true;
                RadioButton_Absorbance.Enabled = true;
                RadioButton_Reference.Enabled = true;
                RadioButton_LampOff.Enabled = true;
                Check_Overlay.Enabled = true;
            }
        }


        private void RadioButton_RefPre_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_RefPre.Checked == false && sender != null)
                return;
            else if (Scan.IsLocalReferenceExist() == SDK.RETURN_PASS)
            {
                Scan.GetRefTime(Scan.SCAN_REF_TYPE.SCAN_REF_PREV);
                Byte[] time = Scan.ReferenceScanDateTime;
                if (time[0] != 0)
                {
                    pre_ref_time = "Previous reference was set: 20" + time[0].ToString() + "/" + time[1].ToString() + "/" + time[2].ToString()
                    + " T " + time[3].ToString() + ":" + time[4].ToString() + ":" + time[5].ToString() + ", PGA = " + Scan.ReferencePGA.ToString();
                    int localRefAutoPGAFlag = Scan.IsLocalRefScanAutoPGA();
                    if (localRefAutoPGAFlag == 1)
                        pre_ref_time += " (AutoPGA)";
                    else if (localRefAutoPGAFlag == -1)
                        pre_ref_time += " (FixedPGA)";
                }

                CheckBox_AutoGain.Checked = false;
                ComboBox_PGAGain.SelectedItem = Scan.ReferencePGA.ToString();

                Button_Scan.Text = "Scan";
                Manual_ContScan_UI_Con(false);
                ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_PREV;
                GroupBox_ContScan.Enabled = true;
                Text_ContScan_TextChanged(null, null);

                if (!String.IsNullOrEmpty(pre_ref_time))
                {
                    label_ref.Visible = true;
                    label_ref.Text = pre_ref_time;
                }
                else
                {
                    label_ref.Visible = false;
                }
                RadioButton_RefPre.Enabled = true;
                RadioButton_RefPre.Checked = true;
            }
            else
            {
                RadioButton_RefNew.Checked = true;
                RadioButton_RefPre.Enabled = false;
                label_ref.Text = "";
                label_ref.Visible = false;
            }
        }

        private void RadioButton_RefFac_CheckedChanged(object sender, EventArgs e)
        {
            if (Device.IsConnected() && RadioButton_RefFac.Checked == true)
            {
                GetBuildInRefTime();
                // Checking if a valid ref cal flag
                if (Device.DevInfo.RefCalRev == 0 || Device.DevInfo.RefCalRev == 255)
                {
                    Message.ShowWarning("There is no valid reference calibration data in the device!\n\nPlease do the reference calibration before a scan.\n\nSet to New/Previous Reference Scan Mode!");
                    RadioButton_RefNew.Checked = true;
                    return;
                }

                CheckBox_AutoGain.Checked = true;
                Button_Scan.Text = "Scan";
                Manual_ContScan_UI_Con(false);
                ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN;
                GroupBox_ContScan.Enabled = true;
                Text_ContScan_TextChanged(null, null);
                if (!String.IsNullOrEmpty(buildin_ref_time))
                {
                    label_ref.Visible = true;
                    label_ref.Text = buildin_ref_time;
                }
                else
                {
                    label_ref.Visible = false;
                }
            }
        }

        #endregion
        #region Scan Config
        private void Button_SetActive_Click(object sender, EventArgs e)
        {
            if (TargetCfg_SelIndex < 0)
            {
                Message.ShowError("No item selected!");
                return;
            }
            else if (ListBox_TargetCfgs.SelectedItems.Count > 1)
            {
                Message.ShowError("More than one item are selected!");
                return;
            }

            ScanConfig.SetTargetActiveScanIndex(TargetCfg_SelIndex);
            label_ActiveConfig.Text = ScanConfig.TargetConfig[TargetCfg_SelIndex].head.config_name;

            SetScanConfig(ScanConfig.TargetConfig[TargetCfg_SelIndex], true, TargetCfg_SelIndex);
        }
        private String SetScanConfig(ScanConfig.SlewScanConfig Config, Boolean IsTarget, Int32 index)
        {
            String msg = "";
            ClearScanPlotsUI();
            if (ScanConfig.SetScanConfig(Config) == SDK.RETURN_FAIL)
            {
                if (IsFetchingDeviceInfo)
                {
                    msg = "Error";
                }
                else
                {
                    String text = "Device config (" + Config.head.config_name + ") is not correct, please check it again!";
                    MessageBox.Show(text, "Error");
                }
            }
            else
            {
                DevCurCfg_Index = index;
                DevCurCfg_IsTarget = IsTarget;

                if (IsTarget)
                    Label_CurrentConfig.Text = "Current Config: Device -> " + Config.head.config_name;
                else
                    Label_CurrentConfig.Text = "Current Config: Local -> " + Config.head.config_name;

                textBox_ScanAvg.Text = Config.head.num_repeats.ToString();
                Double ScanTime = Scan.GetEstimatedScanTime();
                if (ScanTime > 0)
                    Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";

                Button_Scan.Enabled = true;
            }

            return msg;
        }
        private void ListBox_TargetCfgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (NewConfig || EditConfig) return;

            int selInx = -1;
            if (sender.GetType() == typeof(String))
                selInx = int.Parse((string)sender);

            isSelectingConfig = true;
            if (NewConfig == true || EditConfig == true)
            {
                EditConfig = false;
                NewConfig = false;
                Button_CfgCancel_Click(this, e);
                return;
            }
            
            if (selInx == -1)
            {
                TargetCfg_Last_SelIndex = TargetCfg_SelIndex;
                TargetCfg_SelIndex = ListBox_TargetCfgs.SelectedIndex;
            }
            else
            {
                TargetCfg_SelIndex = selInx;
            }

            if (TargetCfg_SelIndex < 0 || ScanConfig.TargetConfig.Count == 0)
            {
                if (ListBox_TargetCfgs.SelectedIndex == -1 && ListBox_LocalCfgs.SelectedIndex == -1)//new config situation
                {
                    return;
                }
                else
                {
                    ListBox_LocalCfgs.BackColor = System.Drawing.Color.AliceBlue;
                    ListBox_TargetCfgs.BackColor = System.Drawing.Color.White;
                    Button_SetActive.Enabled = false;
                    return;
                }
            }
            if (ListBox_LocalCfgs.Items.Count == 0)
            {
                ListBox_LocalCfgs.BackColor = System.Drawing.Color.White;
                ListBox_TargetCfgs.BackColor = System.Drawing.Color.AliceBlue;
            }
            SelCfg_IsTarget = true;
            FillCfgDetailsContent();
            OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
            // Clear target listbox index after local config data refreshed.
            ListBox_TargetCfgs.SelectedIndexChanged -= ListBox_TargetCfgs_SelectedIndexChanged;
            ListBox_LocalCfgs.SelectedIndexChanged -= ListBox_LocalCfgs_SelectedIndexChanged;
            if (ListBox_LocalCfgs.SelectedIndex != -1)
                ListBox_LocalCfgs.SelectedIndex = -1;
            SetDetailColorWhite();
            isSelectingConfig = false;
            Update_Scan_Resolution_and_Pattern_Label();
            ListBox_TargetCfgs.SelectedIndexChanged += ListBox_TargetCfgs_SelectedIndexChanged;
            ListBox_LocalCfgs.SelectedIndexChanged += ListBox_LocalCfgs_SelectedIndexChanged;
        }
        private void ListBox_DeviceScanConfig_MouseDoubleClick(object sender, EventArgs e)
        {
            SetScanConfig(ScanConfig.TargetConfig[TargetCfg_SelIndex], true, TargetCfg_SelIndex);
            TargetCfg_SelIndex = ListBox_TargetCfgs.SelectedIndex;
        }

        private void FillCfgDetailsContent()
        {
            Int32 i, NumSection = 0;
            ScanConfig.SlewScanConfig CurConfig = new ScanConfig.SlewScanConfig
            {
                section = new ScanConfig.SlewScanSection[5]
            };

            //InitCfgDetailsContent();
            if (SelCfg_IsTarget == true)
                CurConfig = ScanConfig.TargetConfig[TargetCfg_SelIndex];
            else
                CurConfig = LocalConfig[LocalCfg_SelIndex];


            NumSection = CurConfig.head.num_sections;

            TextBox_CfgName.Text = CurConfig.head.config_name;
            TextBox_CfgAvg.Text = CurConfig.head.num_repeats.ToString();
            comboBox_cfgNumSec.SelectedItem = NumSection.ToString();

            for (i = 0; i < NumSection; i++)
            {
                ComboBox_CfgScanType[i].SelectedIndex = CurConfig.section[i].section_scan_type;
                TextBox_CfgRangeStart[i].Text = CurConfig.section[i].wavelength_start_nm.ToString();
                TextBox_CfgRangeEnd[i].Text = CurConfig.section[i].wavelength_end_nm.ToString();
                if (Helper.CfgWidthPixelToIndex(CurConfig.section[i].width_px) > -1)
                    ComboBox_CfgWidth[i].SelectedIndex = Helper.CfgWidthPixelToIndex(CurConfig.section[i].width_px);
                else
                    ComboBox_CfgWidth[i].SelectedIndex = 5;
                TextBox_CfgDigRes[i].Text = CurConfig.section[i].num_patterns.ToString();
                ComboBox_CfgExposure[i].SelectedIndex = CurConfig.section[i].exposure_time;
            }
            EnableCfgItem(false);
        }
        private void RefreshTargetCfgList(bool IsSavingConfig = false)
        {
            if (!IsFetchingDeviceInfoWithError && !IsSavingConfig) TargetCfg_SelIndex = -1;

            ListBox_TargetCfgs.Items.Clear();
            if (ScanConfig.TargetConfig.Count > 0)
            {
                for (Int32 i = 0; i < ScanConfig.TargetConfig.Count; i++)
                {
                    ListBox_TargetCfgs.Items.Add(ScanConfig.TargetConfig[i].head.config_name);
                }
                Int32 ActiveIndex = ScanConfig.GetTargetActiveScanIndex();
                label_ActiveConfig.Text = ScanConfig.TargetConfig[ActiveIndex < 0 ? 0 : ActiveIndex].head.config_name;
            }
        }

        private void Button_CfgNew_Click(object sender, EventArgs e)
        {
            // Clear listbox index before someone set focus.
            if (ListBox_LocalCfgs.SelectedIndex != -1)
                ListBox_LocalCfgs.SelectedIndex = -1;
            if (ListBox_TargetCfgs.SelectedIndex != -1)
                ListBox_TargetCfgs.SelectedIndex = -1;
            
            NewConfig = true;

            InitCfgDetailsContent();
            EnableCfgItem(true);
            comboBox_cfgNumSec.SelectedItem = "1";
            OpenCloseScanConfigButton(nameof(ScanConfigMode.NEW));
            TextBox_CfgName.Focus();
            isSelectingConfig = false;
            ListBox_LocalCfgs.Enabled = false;
            ListBox_TargetCfgs.Enabled = false;
            Button_CopyCfgL2T.Enabled = false;
            Button_CopyCfgT2L.Enabled = false;
            Button_MoveCfgL2T.Enabled = false;
            Button_MoveCfgT2L.Enabled = false;
            if (SelCfg_IsTarget)
                Button_SetActive.Enabled = true;
            else
                Button_SetActive.Enabled = false;
            splitContainer1.Panel1.Enabled = false;
        }

        private void Button_CfgCancel_Click(object sender, EventArgs e)
        {
            isCancellingConfigEdit = true;
            OpenCloseScanConfigButton(nameof(ScanConfigMode.CANCEL));
            if (NewConfig || EditConfig)
            {
                int bufindex = 0;
                if (SelCfg_IsTarget == true)
                {
                    if (NewConfig)
                    {
                        bufindex = TargetCfg_Last_SelIndex;
                        TargetCfg_SelIndex = TargetCfg_Last_SelIndex;
                    }
                    else
                        bufindex = TargetCfg_SelIndex;
                }
                else
                {
                    if (NewConfig)
                    {
                        bufindex = LocalCfg_Last_SelIndex;
                        LocalCfg_SelIndex = LocalCfg_Last_SelIndex;
                    }
                    else
                        bufindex = LocalCfg_SelIndex;
                }

                //InitCfgDetailsContent();
                EnableCfgItem(false);

                if (EditConfig)//Edit config
                {
                    if (SelCfg_IsTarget == true)
                    {
                        ListBox_TargetCfgs.ClearSelected();
                        ListBox_TargetCfgs.SelectedIndex = bufindex;
                    }
                    else
                    {
                        ListBox_LocalCfgs.ClearSelected();
                        ListBox_LocalCfgs.SelectedIndex = bufindex;
                    }
                }
                else//New config
                {
                    if (SelCfg_IsTarget == true)
                    {
                        ListBox_TargetCfgs.ClearSelected();
                        ListBox_TargetCfgs.SelectedIndex = TargetCfg_Last_SelIndex;
                    }
                    else
                    {
                        ListBox_LocalCfgs.ClearSelected();
                        ListBox_LocalCfgs.SelectedIndex = LocalCfg_Last_SelIndex;
                    }
                }

                Button_CfgEdit.Enabled = true;
                Button_CfgDelete.Enabled = true;
                Button_CfgNew.Enabled = true;
                NewConfig = false;
                EditConfig = false;
            }
            isCancellingConfigEdit = false;
            isSelectingConfig = false;
            ListBox_LocalCfgs.Enabled = true;
            ListBox_TargetCfgs.Enabled = true;
            Button_CopyCfgL2T.Enabled = true;
            Button_CopyCfgT2L.Enabled = true;
            Button_MoveCfgL2T.Enabled = true;
            Button_MoveCfgT2L.Enabled = true;               
            splitContainer1.Panel1.Enabled = true;

            if (SelCfg_IsTarget == true)
            {
                Button_SetActive.Enabled = true;
                ListBox_TargetCfgs_SelectedIndexChanged(sender, EventArgs.Empty);
            }
            else
            {
                Button_SetActive.Enabled = false;
                ListBox_LocalCfgs_SelectedIndexChanged(sender, EventArgs.Empty);
            }
            GroupBox_CfgDetails.BackColor = TransparencyKey;
            Button_CfgSave.BackColor = TransparencyKey;
            Button_CfgCancel.BackColor = TransparencyKey;
        }

        private void Button_CfgSave_Click(object sender, EventArgs e)
        {
            isSavingConfig = true;
            EnableCfgItem(false);
            OpenCloseScanConfigButton(nameof(ScanConfigMode.SAVE));
            if (IsCfgLegal(true) == SDK.RETURN_FAIL)
            {
                Message.ShowError("Error configuration data can't be saved!");
                EnableCfgItem(true);
                EditConfig = false;
                NewConfig = false;
                isSelectingConfig = false;
                return;
            }
            if (NewConfig == true)
            {
                //NewConfig = false;
                if (SelCfg_IsTarget && ListBox_TargetCfgs.BackColor == System.Drawing.Color.AliceBlue)
                {
                    if (checkConfigName(false, false))//device and not edit
                    {
                        Message.ShowError("The name has exist in device list!");
                        EnableCfgItem(true);
                        OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
                        return;
                    }
                    if (ScanConfig.TargetConfig.Count >= 20)//Confirm the current number of device configuration before saving
                    {
                        Message.ShowWarning("Number of scan configs in device cannot exceed 20.");
                        EnableCfgItem(true);
                        //NewConfig = false;
                        OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
                        return;
                    }
                    SaveCfgToList(true, true);//target and new
                }
                else
                {
                    if (checkConfigName(true, false))//local and not edit
                    {
                        Message.ShowError("The name has exist in local list!");
                        EnableCfgItem(true);
                        //NewConfig = false;
                        OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
                        return;
                    }
                    SaveCfgToList(false, true);//Local and new
                }
            }
            else if (EditConfig == true)
            {
                //EditConfig = false;
                if (SelCfg_IsTarget == true)
                {
                    if (checkConfigName(false, true))//device and edit
                    {
                        Message.ShowError("The name has exist in device list!");
                        EnableCfgItem(true);
                        //EditConfig = false;
                        OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
                        return;
                    }
                    SaveCfgToList(true, false);//target and edit
                    if (DevCurCfg_IsTarget && DevCurCfg_Index == TargetCfg_SelIndex)//update device config
                    {
                        SetScanConfig(ScanConfig.TargetConfig[DevCurCfg_Index], true, DevCurCfg_Index);
                        ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN;
                        RadioButton_RefFac.PerformClick();
                    }
                }
                else
                {
                    if (checkConfigName(true, true))//local and edit
                    {
                        Message.ShowError("The name has exist in local list!");
                        EnableCfgItem(true);
                        //EditConfig = false;
                        OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
                        return;
                    }
                    SaveCfgToList(false, false);//Local and edit
                    if (!DevCurCfg_IsTarget && DevCurCfg_Index == LocalCfg_SelIndex)//update device config
                    {
                        SetScanConfig(LocalConfig[DevCurCfg_Index], false, DevCurCfg_Index);
                        ReferenceSelect = Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN;
                        RadioButton_RefFac.PerformClick();
                    }
                }
            }
            Button_CfgEdit.Enabled = true;
            Button_CfgDelete.Enabled = true;
            Button_CfgNew.Enabled = true;
            Button_CfgCancel.Enabled = true;
            isSelectingConfig = false;

            if (DevCurCfg_IsTarget == SelCfg_IsTarget && DevCurCfg_Index == (SelCfg_IsTarget ? TargetCfg_SelIndex : LocalCfg_SelIndex))
            {
                if (IsFetchingDeviceInfoWithError) IsFetchingDeviceInfoWithError = false;
                if (SelCfg_IsTarget)
                    ListBox_TargetCfgs_MouseDoubleClick(this, null);
                else
                    ListBox_LocalCfgs_MouseDoubleClick(this, null);
                Message.ShowInfo("The current config has been saved and updated for new scans.", "Configuration saved");
                tabScanPage.SelectedIndex = 0;
            }
            else
            {
                DialogResult askForCfgApply = Message.ShowQuestion("Apply the new config for scan?", "Configuration saved");
                if (NewConfig)
                {
                    if (SelCfg_IsTarget)
                    {
                        ListBox_TargetCfgs.SelectedIndex = ListBox_TargetCfgs.Items.Count - 1;
                        TargetCfg_SelIndex = ListBox_TargetCfgs.SelectedIndex;
                        if (askForCfgApply == DialogResult.Yes)
                        {
                            ListBox_TargetCfgs_MouseDoubleClick(this, null);
                            tabScanPage.SelectedIndex = 0;
                        }
                        else
                        {
                            ListBox_TargetCfgs_MouseClick(this, null);
                            ListBox_TargetCfgs_SelectedIndexChanged(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        ListBox_LocalCfgs.SelectedIndex = ListBox_LocalCfgs.Items.Count - 1;
                        LocalCfg_SelIndex = ListBox_LocalCfgs.SelectedIndex;
                        if (askForCfgApply == DialogResult.Yes)
                        {
                            ListBox_LocalCfgs_MouseDoubleClick(this, null);
                            tabScanPage.SelectedIndex = 0;
                        }
                        else
                        {
                            ListBox_LocalCfgs_MouseClick(this, null);
                            ListBox_LocalCfgs_SelectedIndexChanged(this, EventArgs.Empty);
                        }
                    }
                }
                else
                {
                    if (SelCfg_IsTarget)
                    {
                        ListBox_TargetCfgs.SelectedIndex = TargetCfg_SelIndex;
                        if (askForCfgApply == DialogResult.Yes)
                        {
                            ListBox_TargetCfgs_MouseDoubleClick(this, null);
                            tabScanPage.SelectedIndex = 0;
                        }
                        else
                        {
                            ListBox_TargetCfgs_MouseClick(this, null);
                            ListBox_TargetCfgs_SelectedIndexChanged(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        ListBox_LocalCfgs.SelectedIndex = LocalCfg_SelIndex;
                        if (askForCfgApply == DialogResult.Yes)
                        {
                            ListBox_LocalCfgs_MouseDoubleClick(this, null);
                            tabScanPage.SelectedIndex = 0;
                        }
                        else
                        {
                            ListBox_LocalCfgs_MouseClick(this, null);
                            ListBox_LocalCfgs_SelectedIndexChanged(this, EventArgs.Empty);
                        }
                    }
                }
            }

            EditConfig = false;
            NewConfig = false;
            ListBox_LocalCfgs.Enabled = true;
            ListBox_TargetCfgs.Enabled = true;
            Button_CopyCfgL2T.Enabled = true;
            Button_CopyCfgT2L.Enabled = true;
            Button_MoveCfgL2T.Enabled = true;
            Button_MoveCfgT2L.Enabled = true;
            if (SelCfg_IsTarget)
                Button_SetActive.Enabled = true;
            else
                Button_SetActive.Enabled = false;
            splitContainer1.Panel1.Enabled = true;
            GroupBox_CfgDetails.BackColor = TransparencyKey;
            Button_CfgSave.BackColor = TransparencyKey;
            Button_CfgCancel.BackColor = TransparencyKey;
            OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
            isSavingConfig = false;
        }
        private Boolean checkConfigName(Boolean isLocal, Boolean isEdit)
        {
            Boolean isExist = false;
            if (isLocal)
            {
                for (int i = 0; i < ListBox_LocalCfgs.Items.Count; i++)
                {
                    if (ListBox_LocalCfgs.Items[i].ToString() == TextBox_CfgName.Text)
                    {
                        if (isEdit && i == LocalCfg_SelIndex)
                        {
                        }
                        else
                        {
                            TextBox_CfgName.BackColor = System.Drawing.Color.LightPink;
                            isExist = true;
                            return isExist;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListBox_TargetCfgs.Items.Count; i++)
                {
                    if (ListBox_TargetCfgs.Items[i].ToString() == TextBox_CfgName.Text)
                    {
                        if (isEdit && i == TargetCfg_SelIndex)
                        {
                        }
                        else
                        {
                            TextBox_CfgName.BackColor = System.Drawing.Color.LightPink;
                            isExist = true;
                            return isExist;
                        }
                    }
                }
            }
            return isExist;
        }
        private Int32 SaveCfgToList(Boolean IsTarget, Boolean IsNew)
        {
            ScanConfig.SlewScanConfig CurConfig = new ScanConfig.SlewScanConfig
            {
                section = new ScanConfig.SlewScanSection[5]
            };

            CurConfig.head.config_name = Helper.CheckRegex(TextBox_CfgName.Text);
            CurConfig.head.scan_type = 2;
            CurConfig.head.num_sections = Byte.Parse(comboBox_cfgNumSec.SelectedItem.ToString());
            CurConfig.head.num_repeats = UInt16.Parse(TextBox_CfgAvg.Text);

            for (Int32 i = 0; i < CurConfig.head.num_sections; i++)
            {
                CurConfig.section[i].wavelength_start_nm = UInt16.Parse(TextBox_CfgRangeStart[i].Text);
                CurConfig.section[i].wavelength_end_nm = UInt16.Parse(TextBox_CfgRangeEnd[i].Text);
                CurConfig.section[i].num_patterns = UInt16.Parse(TextBox_CfgDigRes[i].Text);
                CurConfig.section[i].section_scan_type = (Byte)(ComboBox_CfgScanType[i].SelectedIndex);
                CurConfig.section[i].width_px = (Byte)Helper.CfgWidthIndexToPixel(ComboBox_CfgWidth[i].SelectedIndex);
                CurConfig.section[i].exposure_time = (UInt16)ComboBox_CfgExposure[i].SelectedIndex;
            }

            if (IsNew == true)
            {
                if (IsTarget)
                {
                    if (Device.DevInfo.CfgRev == 0 || Device.DevInfo.CfgRev == 255 || ScanConfig.GetTargetCfgListNum() == 0)
                        ScanConfig.TargetConfig.Clear();
                    
                    ScanConfig.TargetConfig.Add(CurConfig);
                    RefreshTargetCfgList(true);
                }
                else
                {
                    LocalConfig.Add(CurConfig);
                    RefreshLocalCfgList();
                }
            }
            else
            {
                if (IsTarget)
                {
                    ScanConfig.TargetConfig.RemoveAt(TargetCfg_SelIndex);
                    ScanConfig.TargetConfig.Insert(TargetCfg_SelIndex, CurConfig);
                    RefreshTargetCfgList(true);
                }
                else
                {
                    LocalConfig.RemoveAt(LocalCfg_SelIndex);
                    LocalConfig.Insert(LocalCfg_SelIndex, CurConfig);
                    RefreshLocalCfgList();
                }
            }
            return SaveCfgToLocalOrDevice(IsTarget);
        }

        private void Button_CfgDelete_Click(object sender, EventArgs e)
        {
            bool multipleDelete = false;
            bool confirmDelete = false;
            int restoreIdx = -1;
            int origIdx = -1;

            SystemBusy(true);

            if (SelCfg_IsTarget == true)
            {
                if (TargetCfg_SelIndex < 0)
                {
                    Message.ShowError("No item selected.");
                    SystemBusy(false);
                    return;
                }
                else if (ListBox_TargetCfgs.Items.Count < 2)
                {
                    Message.ShowError("The device configuration cannot be empty.");
                    SystemBusy(false);
                    return;
                }
            }
            else
            {
                if (LocalCfg_SelIndex < 0)
                {
                    Message.ShowError("No item selected.");
                    SystemBusy(false);
                    return;
                }
            }

            DialogResult dialogResult = Message.ShowQuestion("Are you sure to delete the selected item?", "Delete Config");
            if (dialogResult == DialogResult.No)
            {
                SystemBusy(false);
                return;
            }
            OpenCloseScanConfigButton(nameof(ScanConfigMode.DELETE));

            string curCfgName;
            bool curCfgDeleted = false;

            if (DevCurCfg_IsTarget)
                curCfgName = ScanConfig.TargetConfig[DevCurCfg_Index].head.config_name;
            else
                curCfgName = LocalConfig[DevCurCfg_Index].head.config_name;

            // Check local first
            if (ListBox_LocalCfgs.SelectedItems.Count > 0)
            {
                if (ListBox_LocalCfgs.SelectedItems.Count > 1)
                    multipleDelete = true;
                else
                {
                    origIdx = ListBox_LocalCfgs.SelectedIndex;
                    restoreIdx = origIdx - 1 >= 0 ? origIdx - 1 : 0;
                }

                var selectedIdx = new int[ListBox_LocalCfgs.SelectedItems.Count];
                ListBox_LocalCfgs.SelectedIndices.CopyTo(selectedIdx, 0);
                Array.Reverse(selectedIdx);

                foreach (var idx in selectedIdx)
                {
                    if (!DevCurCfg_IsTarget && idx < DevCurCfg_Index)
                        DevCurCfg_Index--;

                    var cfg = LocalConfig[idx];
                    if (cfg.head.config_name == curCfgName && SelCfg_IsTarget == DevCurCfg_IsTarget)
                    {
                        string msg = "The currently using scan configuration - " +
                            (DevCurCfg_IsTarget == true ? "Device: " : "Local: ") +
                            curCfgName + " will be deleted!\n\nAre you sure?"; 
                        DialogResult ret = Message.ShowQuestion(msg, "Confirm Delete");
                        if (ret == DialogResult.No || ret == DialogResult.Cancel)
                            continue;
                        else
                        {
                            Message.ShowInfo("The current config will be changed to device active config!", "Current Config");
                            curCfgDeleted = true;
                        }
                    }
                    LocalConfig.RemoveAt(idx);
                    confirmDelete = true;
                }

                RefreshLocalCfgList();
                SaveCfgToLocalOrDevice(false);
            }
            else if (ListBox_TargetCfgs.SelectedItems.Count > 0)
            {
                if (ListBox_TargetCfgs.Items.Count == 1 || ListBox_TargetCfgs.SelectedItems.Count == ListBox_TargetCfgs.Items.Count)
                {
                    Message.ShowError("The device configuration cannot be empty.");
                    SystemBusy(false);
                    return;
                }
                else if (ListBox_TargetCfgs.SelectedItems.Count > 1)
                    multipleDelete = true;
                else
                {
                    origIdx = ListBox_TargetCfgs.SelectedIndex;
                    restoreIdx = origIdx - 1 >= 0 ? origIdx - 1 : 0;
                }

                int activeCfgIdx = ScanConfig.GetTargetActiveScanIndex();
                int delta_activeCfgIdx = activeCfgIdx;
                string activeCfgName = ScanConfig.TargetConfig[activeCfgIdx].head.config_name;
                bool activeCfgDeleted = false;

                var selectedIdx = new int[ListBox_TargetCfgs.SelectedItems.Count];
                ListBox_TargetCfgs.SelectedIndices.CopyTo(selectedIdx, 0);
                Array.Reverse(selectedIdx);

                foreach (var idx in selectedIdx)
                {
                    if (ListBox_TargetCfgs.Items[idx].ToString() == activeCfgName)
                        activeCfgDeleted = true;

                    if (idx < activeCfgIdx)
                        delta_activeCfgIdx--;

                    if (DevCurCfg_IsTarget && idx < DevCurCfg_Index)
                        DevCurCfg_Index--;

                    var cfg = ScanConfig.TargetConfig[idx];
                    if (cfg.head.config_name == curCfgName && SelCfg_IsTarget == DevCurCfg_IsTarget)
                    {
                        string msg = "The currently using scan configuration - " +
                            (DevCurCfg_IsTarget == true ? "Device: " : "Local: ") +
                            curCfgName + " will be deleted!\n\nAre you sure?"; 
                        DialogResult ret = Message.ShowQuestion(msg, "Confirm Delete");
                        if (ret == DialogResult.No || ret == DialogResult.Cancel)
                            continue;
                        else
                        {
                            Message.ShowInfo("The current config will be changed to device active config!", "Current Config");
                            curCfgDeleted = true;
                        }
                    }
                    ScanConfig.TargetConfig.RemoveAt(idx);
                    confirmDelete = true;
                }

                if (activeCfgDeleted)
                {
                    ScanConfig.SetTargetActiveScanIndex(0);
                    SetScanConfig(ScanConfig.TargetConfig[0], true, 0);
                }
                else if (delta_activeCfgIdx != activeCfgIdx)
                {
                    ScanConfig.SetTargetActiveScanIndex(delta_activeCfgIdx);
                }

                RefreshTargetCfgList();
                SaveCfgToLocalOrDevice(true);
            }
            else
                Message.ShowError("No item selected.");

            if (curCfgDeleted)
            {
                int ActiveIndex = ScanConfig.GetTargetActiveScanIndex();
                SetScanConfig(ScanConfig.TargetConfig[ActiveIndex], true, ActiveIndex);
                ListBox_TargetCfgs.SelectedIndex = ActiveIndex;
                SelCfg_IsTarget = true;
                DevCurCfg_IsTarget = true;
            }
            else if (ListBox_LocalCfgs.Items.Count == 0)
            {
                Button_CfgNew.Enabled = true;
                Button_CfgEdit.Enabled = false;
                Button_CfgDelete.Enabled = false;
                Button_CfgSave.Enabled = false;
                Button_CfgCancel.Enabled = false;
                ClearDetailValue();
            }

            if (multipleDelete)
            {
                if (SelCfg_IsTarget)
                    ListBox_TargetCfgs.SelectedIndex = 0;
                else if (ListBox_LocalCfgs.Items.Count > 0)
                    ListBox_LocalCfgs.SelectedIndex = 0;
            }
            else if (restoreIdx >= 0 && !curCfgDeleted)
            {
                int selIdx = confirmDelete ? restoreIdx : origIdx;
                if (SelCfg_IsTarget)
                    ListBox_TargetCfgs.SelectedIndex = selIdx;
                else if (ListBox_LocalCfgs.Items.Count > 0)
                    ListBox_LocalCfgs.SelectedIndex = selIdx;
            }

            isSelectingConfig = false;
            SystemBusy(false);
        }

        private void Button_CfgEdit_Click(object sender, EventArgs e)
        {
            ListBox_TargetCfgs.SelectedIndexChanged -= ListBox_TargetCfgs_SelectedIndexChanged;
            ListBox_LocalCfgs.SelectedIndexChanged -= ListBox_LocalCfgs_SelectedIndexChanged;

            if (SelCfg_IsTarget == true)
            {
                if (TargetCfg_SelIndex < 0)
                {
                    Message.ShowWarning("No item selected!");
                    return;
                }
                EditSelectIndex = ListBox_TargetCfgs.SelectedIndex;
            }
            else
            {
                if (LocalCfg_SelIndex < 0)
                {
                    Message.ShowWarning("No item selected!");
                    return;
                }
                EditSelectIndex = ListBox_LocalCfgs.SelectedIndex;
            }
            EnableCfgItem(true);
            NewConfig = false;
            EditConfig = true;
            OpenCloseScanConfigButton(nameof(ScanConfigMode.EDIT));
            TextBox_CfgName.Focus();
            isSelectingConfig = false;
            ListBox_LocalCfgs.Enabled = false;
            ListBox_TargetCfgs.Enabled = false;
            Button_CopyCfgL2T.Enabled = false;
            Button_CopyCfgT2L.Enabled = false;
            Button_MoveCfgL2T.Enabled = false;
            Button_MoveCfgT2L.Enabled = false;
            Button_SetActive.Enabled = false;
            splitContainer1.Panel1.Enabled = false;

            ListBox_TargetCfgs.SelectedIndexChanged += ListBox_TargetCfgs_SelectedIndexChanged;
            ListBox_LocalCfgs.SelectedIndexChanged += ListBox_LocalCfgs_SelectedIndexChanged;
        }

        private static string CfgFieldPrevValue = "";
        private static int PrevTotalPatternUsed = 0;
        private static int TotalPatternUsed = 0;

        private void CfgDetails_KeyPressed(object sender, KeyPressEventArgs e)
        {

        }

        private void CfgDetails_KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                CfgDetails_Validated(sender, e);
        }

        private void CfgField_Enter(object sender, EventArgs e)
        {
            String senderName = sender.GetType().Name;

            if (senderName == "TextBox")
            {
                var curField = (TextBox)sender;
                CfgFieldPrevValue = curField.Text;
            }
            else if (senderName == "ComboBox" || senderName == "MyComboBox")
            {
                var curField = (ComboBox)sender;
                CfgFieldPrevValue = curField.SelectedIndex.ToString();
            }
        }

        private void CfgField_LostFocus(object sender, EventArgs e)
        {
            String senderName = sender.GetType().Name;

            if (senderName == "TextBox")
            {
                var curField = (TextBox)sender;
                CfgFieldPrevValue = curField.Text;
            }
            else if (senderName == "ComboBox" || senderName == "MyComboBox")
            {
                var curField = (ComboBox)sender;
                CfgFieldPrevValue = curField.SelectedIndex.ToString();
            }
        }

        private void CfgDetails_Validated(object sender, EventArgs e)
        {
            if (isSelectingConfig || IsFetchingDeviceInfo) return;

            Control nextFocus = FindFocusedControl(this);
            // Check if user want to give up the config editing
            if (nextFocus.Name == "Button_CfgCancel" || isCancellingConfigEdit)
                return;

            String senderName = sender.GetType().Name;

            if (senderName == "TextBox")
            {
                var cfgField = (TextBox)sender;
                var cfgFieldName = cfgField.Name;
                if (cfgFieldName.Contains("CfgName"))
                {
                    if (cfgField.Text == String.Empty)
                    {
                        Message.ShowError("Invalid input! Config name can not be empty.");
                        cfgField.Focus();
                        return;
                    }
                }
                else if (cfgFieldName.Contains("CfgAvg"))
                {
                    long numAvg = 0;
                    if (!long.TryParse(cfgField.Text, out numAvg))
                    {
                        Message.ShowError("Invalid input! Number average should be integer.");
                        cfgField.Text = CfgFieldPrevValue == "" ? "6" : CfgFieldPrevValue;
                        cfgField.Focus();
                        return;
                    }
                    else if (numAvg == 0)
                    {
                        Message.ShowError("Invalid input! Number average is Zero.");
                        cfgField.Text = "1";
                        cfgField.Focus();
                        return;
                    }
                    else if (numAvg > 255)
                    {
                        Message.ShowError("Invalid input! Number average is too large.");
                        cfgField.Text = "255";
                        cfgField.Focus();
                        return;
                    }
                }
                else if (cfgFieldName.Contains("CfgRangeStart"))
                {
                    long rangeStart = 0;
                    if (!long.TryParse(cfgField.Text, out rangeStart))
                    {
                        Message.ShowError("Invalid input! Wavelength start should be integer.");
                        cfgField.Text = CfgFieldPrevValue == "" ? Device.DevInfo.MinWavelength.ToString() : CfgFieldPrevValue;
                        return;
                    }
                    else if (rangeStart > Device.DevInfo.MaxWavelength - 50)
                    {
                        String errMsg = "Invalid input! Wavelength start should be less than " + (Device.DevInfo.MaxWavelength - 50).ToString();
                        Message.ShowError(errMsg);
                        cfgField.Text = CfgFieldPrevValue == "" ? (Device.DevInfo.MaxWavelength - 50).ToString() : CfgFieldPrevValue;
                        return;
                    }
                    else if (rangeStart < Device.DevInfo.MinWavelength)
                    {
                        String errMsg = "Invalid input! Wavelength start should be greater than " + Device.DevInfo.MinWavelength.ToString();
                        Message.ShowError(errMsg);
                        cfgField.Text = CfgFieldPrevValue == "" ? Device.DevInfo.MinWavelength.ToString() : CfgFieldPrevValue;
                        return;
                    }
                }
                else if (cfgFieldName.Contains("CfgRangeEnd"))
                {
                    long rangeEnd = 0;
                    if (!long.TryParse(cfgField.Text, out rangeEnd))
                    {
                        Message.ShowError("Invalid input! Wavelength end should be integer.");
                        cfgField.Text = CfgFieldPrevValue == "" ? Device.DevInfo.MaxWavelength.ToString() : CfgFieldPrevValue;
                        return;
                    }
                    else if (rangeEnd > Device.DevInfo.MaxWavelength)
                    {
                        String errMsg = "Invalid input! Wavelength end should be less than " + Device.DevInfo.MaxWavelength.ToString();
                        Message.ShowError(errMsg);
                        cfgField.Text = CfgFieldPrevValue == "" ? Device.DevInfo.MaxWavelength.ToString() : CfgFieldPrevValue;
                        return;
                    }
                    else if (rangeEnd < Device.DevInfo.MinWavelength + 50)
                    {
                        String errMsg = "Invalid input! Wavelength end should be greater than " + (Device.DevInfo.MinWavelength + 50).ToString();
                        Message.ShowError(errMsg);
                        cfgField.Text = CfgFieldPrevValue == "" ? (Device.DevInfo.MinWavelength + 50).ToString() : CfgFieldPrevValue;
                        return;
                    }
                }
                else if (cfgFieldName.Contains("CfgDigRes"))
                {
                    ushort digiRes = 0;
                    if (!ushort.TryParse(cfgField.Text, out digiRes))
                    {
                        Message.ShowError("Invalid input! Digital resolution should be integer.");
                        cfgField.Text = CfgFieldPrevValue == "" ? "3" : CfgFieldPrevValue;
                        return;
                    }
                    else if (digiRes < 3)
                    {
                        Message.ShowError("Invalid input! Minimum digital resolution should be equal or greater than 3.");
                        cfgField.Text = CfgFieldPrevValue == "" ? CfgFieldPrevValue == "" ? "3" : CfgFieldPrevValue : "3";
                        return;
                    }
                }

                if (Update_Scan_Resolution_and_Pattern_Label() > 624)
                {
                    int prevSet, patLimit;
                    int patDiff = TotalPatternUsed - PrevTotalPatternUsed;

                    int.TryParse(CfgFieldPrevValue, out prevSet);
                    if (prevSet == 0)
                        patLimit = 624 - PrevTotalPatternUsed;
                    else
                        patLimit = int.Parse(cfgField.Text) - patDiff + prevSet;

                    cfgField.Text = patLimit.ToString();
                    Message.ShowError("Exceed total scan patterns limit! The max number of total patterns is 624.");
                    Update_Scan_Resolution_and_Pattern_Label(); // Refresh the UI
                    return;
                }
                if (nextFocus.Name != cfgField.Name)
                    CfgFieldPrevValue = "";
                else
                    CfgFieldPrevValue = cfgField.Text;
            }
            else if (senderName == "ComboBox" || senderName == "MyComboBox")
            {
                var cfgField = (ComboBox)sender;
                int totalPatterns = Update_Scan_Resolution_and_Pattern_Label();
                if (totalPatterns > 624)
                {
                    Message.ShowError("Exceed total scan patterns limit! The max number of total patterns is 624.");
                    cfgField.SelectedIndex = int.Parse(CfgFieldPrevValue);
                    return;
                }
                if (nextFocus.Name != cfgField.Name)
                    CfgFieldPrevValue = "";
                else
                    CfgFieldPrevValue = cfgField.SelectedIndex.ToString();
            }
        }

        private int Update_Scan_Resolution_and_Pattern_Label()
        {
            int num_sections = int.Parse(comboBox_cfgNumSec.SelectedItem.ToString());
            int total_patterns = 0;
            PrevTotalPatternUsed = TotalPatternUsed;
            ushort rangeStart, rangeEnd, numPat;
            string msg = "";

            for (int i = 0; i < num_sections; i++)
            {
                if (!ushort.TryParse(TextBox_CfgRangeStart[i].Text, out rangeStart) || !ushort.TryParse(TextBox_CfgRangeEnd[i].Text, out rangeEnd))
                    break;
                if (rangeStart >= rangeEnd)
                {
                    for (int j = 0; j < MAX_CFG_SECTION; j++)
                        Label_overSampleRate[j].Visible = false;
                    msg = String.Format("Start wavelength should be smaller than end wavelength");
                    Message.ShowError(msg);
                    return -1;
                }
                if (rangeStart < Device.DevInfo.MinWavelength || rangeEnd > Device.DevInfo.MaxWavelength)
                {
                    for (int j = 0; j < MAX_CFG_SECTION; j++)
                        Label_overSampleRate[j].Visible = false;
                    Button_CfgEdit.Enabled = false;
                    msg = String.Format("Wavelength range is not applicable to the connected device!");
                    Message.ShowError(msg);
                    return -1;
                }
            }

            for (int i = 0; i < num_sections; i++)
            {
                Int32 PatternUsed = 0;
                Int32 MaxResolution = 0;

                Label_overSampleRate[i].Visible = true;

                //if (!ushort.TryParse(TextBox_CfgRangeStart[i].Text, out rangeStart) || !ushort.TryParse(TextBox_CfgRangeEnd[i].Text, out rangeEnd))
                //    break;
                //if (rangeStart >= rangeEnd)
                //{
                //    return -1;
                //}

                ushort.TryParse(TextBox_CfgRangeStart[i].Text, out rangeStart);
                ushort.TryParse(TextBox_CfgRangeEnd[i].Text, out rangeEnd);

                //if (rangeStart >= Device.DevInfo.MinWavelength && rangeEnd <= Device.DevInfo.MaxWavelength)
                //{
                    ScanConfig.SlewScanConfig cfg = new ScanConfig.SlewScanConfig
                    {
                        section = new ScanConfig.SlewScanSection[5]
                    };

                    cfg.section[0].section_scan_type = byte.Parse(ComboBox_CfgScanType[i].SelectedIndex.ToString());
                    cfg.section[0].wavelength_start_nm = rangeStart;
                    cfg.section[0].wavelength_end_nm = rangeEnd;
                    cfg.section[0].width_px = (Byte)Helper.CfgWidthIndexToPixel(ComboBox_CfgWidth[i].SelectedIndex);

                    MaxResolution = ScanConfig.GetMaxResolutions(cfg, 0);
                    int patWidth = 0;
                    double baseOverSampleRate = 0.0;
                    if (Con_OneNM_PixWidth.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                    {
                        patWidth = (int)Math.Ceiling(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex));
                        baseOverSampleRate = (double)(Math.Ceiling((double)(rangeEnd - rangeStart) / patWidth));
                    }
                    else
                    {
                        patWidth = (int)Math.Floor(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex));
                        baseOverSampleRate = (double)(Math.Floor((double)(rangeEnd - rangeStart) / patWidth));
                    }

                    if (ushort.TryParse(TextBox_CfgDigRes[i].Text, out numPat) && numPat != 0)
                    {
                        string s = TextBox_CfgDigRes[i].Text;
                        int inputDigitalResolution = (int.Parse(s));
                        double overSampleRate = 0.0;
                        if (Con_OneNM_PixWidth.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                            overSampleRate = (double)inputDigitalResolution / Math.Floor((double)(rangeEnd - rangeStart) / Math.Ceiling(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex)));
                        else
                            overSampleRate = (double)inputDigitalResolution / Math.Floor((double)(rangeEnd - rangeStart) / Math.Floor(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex)));

                        if ((numPat > MaxResolution) || (numPat > Math.Floor(baseOverSampleRate * 4.5)))
                        {
                            if ((int)Math.Floor(baseOverSampleRate * 4.5) > MaxResolution)
                            {
                                msg = String.Format("Exceed the section max resolution! (max = {0})", MaxResolution);
                                numPat = (ushort)MaxResolution;
                                cfg.section[0].num_patterns = (ushort)numPat;
                                if (Con_OneNM_PixWidth.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                                    overSampleRate = (double)(numPat / Math.Floor((double)(rangeEnd - rangeStart) / Math.Ceiling(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex))));
                                else
                                    overSampleRate = (double)(numPat / Math.Floor((double)(rangeEnd - rangeStart) / Math.Floor(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex))));
                            }
                            else
                            {
                                msg = String.Format("Exceed the max oversampling rate! (max = 4.5, set = {0:F1}x)", overSampleRate);
                                numPat = (ushort)Math.Floor(baseOverSampleRate * 4.5);
                                cfg.section[0].num_patterns = (ushort)numPat;
                                if (Con_OneNM_PixWidth.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                                    overSampleRate = (double)(numPat / Math.Floor((double)(rangeEnd - rangeStart) / Math.Ceiling(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex))));
                                else
                                    overSampleRate = (double)(numPat / Math.Floor((double)(rangeEnd - rangeStart) / Math.Floor(Helper.CfgWidthIndexToNM(ComboBox_CfgWidth[i].SelectedIndex))));
                            }
                            if (!IsFetchingDeviceInfo)
                            {
                                Message.ShowError(msg);
                                Button_CfgEdit.PerformClick();
                                Button_CfgCancel.Enabled = false;
                                Button_CfgNew.Enabled = false;
                                GroupBox_CfgDetails.BackColor = Color.LightYellow;
                                Button_CfgSave.BackColor = Color.LightYellow;
                                Button_CfgCancel.BackColor = Color.LightYellow;
                            }
                            else
                            {
                                IsFetchingDeviceInfoWithError = true;
                                FetchingDevInfoErrMsg = msg;
                            }
                        }

                        overSampleRate = Math.Round(overSampleRate, 1);

                        if ((overSampleRate > 3 || overSampleRate < 2) && patWidth > 7)
                        {
                            int upperLimit = (int)baseOverSampleRate * 3;
                            upperLimit = upperLimit > MaxResolution ? MaxResolution : upperLimit;
                            Label_overSampleRate[i].ForeColor = Color.Red;
                            toolTip1.SetToolTip(Label_overSampleRate[i], String.Format("The recommended oversampling is between 2.0 ~ 3.0 for this pattern width setting\ni.e. Digital resolution should be between {0:F0} ~ {1:F0}", baseOverSampleRate * 2, upperLimit));
                        }
                        else if (overSampleRate < 2 && patWidth > 4 && patWidth < 8)
                        {
                            Label_overSampleRate[i].ForeColor = Color.Red;
                            toolTip1.SetToolTip(Label_overSampleRate[i], String.Format("The recommended oversampling is above 2.0 for this pattern width setting\ni.e. Digital resolution should be between {0:F0} ~ {1:F0}", baseOverSampleRate * 2, MaxResolution));
                        }
                        else
                        {
                            Label_overSampleRate[i].ForeColor = Color.Blue;
                            toolTip1.SetToolTip(Label_overSampleRate[i], "");
                        }

                        Label_overSampleRate[i].Text = overSampleRate.ToString("F1");
                        TextBox_CfgDigRes[i].Text = numPat.ToString();

                        if (cfg.section[0].section_scan_type > 0)
                            PatternUsed = ScanConfig.GetHadamardUsedPatterns(cfg, 0);
                        else
                            PatternUsed = numPat;

                        total_patterns += PatternUsed;
                    }
                //}
            }
            TotalPatternUsed = total_patterns;
            return total_patterns;
        }

        private void CfgSection_SelectionChanged(object sender, EventArgs e)
        {
            int section = int.Parse(comboBox_cfgNumSec.SelectedItem.ToString());

            for (int i = 0; i < 5; i++)
            {
                if (i < section)
                {
                    ComboBox_CfgScanType[i].Visible = true;
                    TextBox_CfgRangeStart[i].Visible = true;
                    TextBox_CfgRangeEnd[i].Visible = true;
                    ComboBox_CfgWidth[i].Visible = true;
                    TextBox_CfgDigRes[i].Visible = true;
                    ComboBox_CfgExposure[i].Visible = true;
                    Label_overSampleRate[i].Visible = true;
                }
                else
                {
                    ComboBox_CfgScanType[i].Visible = false;
                    TextBox_CfgRangeStart[i].Visible = false;
                    TextBox_CfgRangeEnd[i].Visible = false;
                    ComboBox_CfgWidth[i].Visible = false;
                    TextBox_CfgDigRes[i].Visible = false;
                    ComboBox_CfgExposure[i].Visible = false;
                    Label_overSampleRate[i].Visible = false;
                }
            }
        }

        private Int32 IsCfgLegal(Boolean IsColored)
        {
            Int32 ret = SDK.RETURN_PASS;
            Int32 TotalPatterns = 0;
            ScanConfig.SlewScanConfig CurConfig = new ScanConfig.SlewScanConfig
            {
                section = new ScanConfig.SlewScanSection[5]
            };
            CurConfig.head.scan_type = 2;

            // Config Name
            if (TextBox_CfgName.Text == String.Empty)
            {
                if (IsColored) TextBox_CfgName.BackColor = System.Drawing.Color.LightPink;
                ret = SDK.RETURN_FAIL;
            }
            else
            {
                if (IsColored) TextBox_CfgName.BackColor = System.Drawing.Color.White;
                CurConfig.head.config_name = Helper.CheckRegex(TextBox_CfgName.Text);
            }

            // Num Scans to Average
            if (UInt16.TryParse(TextBox_CfgAvg.Text, out CurConfig.head.num_repeats) == false || CurConfig.head.num_repeats == 0)
            {
                if (IsColored) TextBox_CfgAvg.BackColor = System.Drawing.Color.LightPink;
                ret = SDK.RETURN_FAIL;
            }
            else
            {
                if (IsColored) TextBox_CfgAvg.BackColor = System.Drawing.Color.White;
            }

            // Sections
            CurConfig.head.num_sections = byte.Parse(comboBox_cfgNumSec.SelectedItem.ToString());
            for (Byte i = 0; i < CurConfig.head.num_sections; i++)
            {
                CurConfig.section[i].section_scan_type = (Byte)(ComboBox_CfgScanType[i].SelectedIndex);
                CurConfig.section[i].width_px = (Byte)Helper.CfgWidthIndexToPixel(ComboBox_CfgWidth[i].SelectedIndex);
                CurConfig.section[i].exposure_time = (UInt16)ComboBox_CfgExposure[i].SelectedIndex;

                // Start nm
                if (UInt16.TryParse(TextBox_CfgRangeStart[i].Text, out CurConfig.section[i].wavelength_start_nm) == false ||
                    CurConfig.section[i].wavelength_start_nm < Device.DevInfo.MinWavelength)
                {
                    if (IsColored) TextBox_CfgRangeStart[i].BackColor = System.Drawing.Color.LightPink;
                    ret = SDK.RETURN_FAIL;
                }
                else
                {
                    if (IsColored) TextBox_CfgRangeStart[i].BackColor = System.Drawing.Color.White;
                }

                // End nm
                if (UInt16.TryParse(TextBox_CfgRangeEnd[i].Text, out CurConfig.section[i].wavelength_end_nm) == false ||
                    CurConfig.section[i].wavelength_end_nm > Device.DevInfo.MaxWavelength || CurConfig.section[i].wavelength_end_nm < Device.DevInfo.MinWavelength)
                {
                    if (IsColored) TextBox_CfgRangeEnd[i].BackColor = System.Drawing.Color.LightPink;
                    ret = SDK.RETURN_FAIL;
                }
                else
                {
                    if (IsColored) TextBox_CfgRangeEnd[i].BackColor = System.Drawing.Color.White;
                }
                if (CurConfig.section[i].wavelength_start_nm >= CurConfig.section[i].wavelength_end_nm)
                {
                    if (IsColored) TextBox_CfgRangeStart[i].BackColor = System.Drawing.Color.LightPink;
                    if (IsColored) TextBox_CfgRangeEnd[i].BackColor = System.Drawing.Color.LightPink;
                    ret = SDK.RETURN_FAIL;
                }

                Int32 MaxPattern = 0;
                Int32 HadPattern = 0;
                // Check Max Patterns(user input start wav and end wav will check)
                if (UInt16.TryParse(TextBox_CfgRangeStart[i].Text, out CurConfig.section[i].wavelength_start_nm) == true &&
                    CurConfig.section[i].wavelength_start_nm >= Device.DevInfo.MinWavelength &&
                    UInt16.TryParse(TextBox_CfgRangeEnd[i].Text, out CurConfig.section[i].wavelength_end_nm) == true &&
                    CurConfig.section[i].wavelength_end_nm <= Device.DevInfo.MaxWavelength && CurConfig.section[i].wavelength_end_nm >= Device.DevInfo.MinWavelength)
                {

                    MaxPattern = ScanConfig.GetMaxResolutions(CurConfig, i);
                    if ((UInt16.TryParse(TextBox_CfgDigRes[i].Text, out CurConfig.section[i].num_patterns) == false) ||
                        (CurConfig.section[i].section_scan_type == 0 && CurConfig.section[i].num_patterns < 2) ||  // Column Mode
                        (CurConfig.section[i].section_scan_type == 1 && CurConfig.section[i].num_patterns < 3) ||  // Hadamard Mode
                        (CurConfig.section[i].num_patterns > MaxPattern) ||
                        (MaxPattern <= 0))
                    {
                        if (IsColored) TextBox_CfgDigRes[i].BackColor = System.Drawing.Color.LightPink;
                        if (MaxPattern < 0) MaxPattern = 0;
                        ret = SDK.RETURN_FAIL;
                    }
                    else
                    {
                        if (IsColored) TextBox_CfgDigRes[i].BackColor = System.Drawing.Color.White;
                        HadPattern = ScanConfig.GetHadamardUsedPatterns(CurConfig, i);

                        if (CurConfig.section[i].num_patterns > MaxPattern)
                        {
                            if (IsColored) TextBox_CfgDigRes[i].BackColor = System.Drawing.Color.LightPink;
                            ret = SDK.RETURN_FAIL;
                        }
                    }

                }
                if (HadPattern != -1)
                {
                    TotalPatterns += HadPattern;
                }
                else
                {
                    TotalPatterns += CurConfig.section[i].num_patterns;
                }

            }

            // Check total patterns
            if ((TotalPatterns > 624 && !isCancellingConfigEdit && !isSelectingConfig) || (TotalPatterns > 624 && NewConfig == true && !isCancellingConfigEdit))
            {
                String text = "Total number of patterns " + TotalPatterns.ToString() + " exceeds 624!";
                Message.ShowWarning(text);
                ret = SDK.RETURN_FAIL;
            }

            return ret;
        }

        private Int32 IsCfgValidForSaveToDevice(ScanConfig.SlewScanConfig cfg)
        {
            Int32 ret = SDK.RETURN_PASS;
            Int32 TotalPatterns = 0;

            // Config Name
            if (cfg.head.config_name == String.Empty)
                ret = SDK.RETURN_FAIL;

            // Num Scans to Average
            if (cfg.head.num_repeats == 0)
                ret = SDK.RETURN_FAIL;

            // Sections
            for (Byte i = 0; i < cfg.head.num_sections; i++)
            {
                // Start nm
                if (cfg.section[i].wavelength_start_nm < Device.DevInfo.MinWavelength)
                    ret = SDK.RETURN_FAIL;

                // End nm
                if (cfg.section[i].wavelength_end_nm > Device.DevInfo.MaxWavelength || cfg.section[i].wavelength_end_nm < Device.DevInfo.MinWavelength)
                    ret = SDK.RETURN_FAIL;
                if (cfg.section[i].wavelength_start_nm >= cfg.section[i].wavelength_end_nm)
                    ret = SDK.RETURN_FAIL;

                // Check Max Patterns
                Int32 MaxPattern = 0;
                Int32 HadPattern = 0;

                MaxPattern = ScanConfig.GetMaxResolutions(cfg, i);
                if ((cfg.section[i].section_scan_type == 0 && cfg.section[i].num_patterns < 2) ||  // Column Mode
                    (cfg.section[i].section_scan_type == 1 && cfg.section[i].num_patterns < 3) ||  // Hadamard Mode
                    (cfg.section[i].num_patterns > MaxPattern) ||
                    (MaxPattern <= 0))
                {
                    if (MaxPattern < 0) MaxPattern = 0;
                    ret = SDK.RETURN_FAIL;
                }
                else
                {
                    HadPattern = ScanConfig.GetHadamardUsedPatterns(cfg, i);

                    if (cfg.section[i].num_patterns > MaxPattern)
                        ret = SDK.RETURN_FAIL;
                }

                if (HadPattern != -1)
                    TotalPatterns += HadPattern;
                else
                    TotalPatterns += cfg.section[i].num_patterns;
            }

            // Check total patterns
            if (TotalPatterns > 624)
                ret = SDK.RETURN_FAIL;

            return ret;
        }
        #endregion
        #region Saved scan
        private void ClearSavedScanCfgItems()
        {
            for (Int32 i = 0; i < MAX_CFG_SECTION; i++)
            {
                Label_SavedScanType[i].Text = String.Empty;
                Label_SavedRangeStart[i].Text = String.Empty;
                Label_SavedRangeEnd[i].Text = String.Empty;
                Label_SavedWidth[i].Text = String.Empty;
                Label_SavedDigRes[i].Text = String.Empty;
                Label_SavedExposure[i].Text = String.Empty;
            }
            Label_SavedAvg.Text = String.Empty;
        }

        Boolean LoadFileSuccess = true;
        private List<String> EnumerateFiles(String SearchPattern)
        {
            List<String> ListFiles = new List<String>();
            if (SearchPattern == "*.dat")
                SavedScanFileTimeList.Clear();
            try
            {
                foreach (String Files in Directory.EnumerateFiles(Dir_Scan_DataBase, SearchPattern))
                {
                    String FileName = Files.Substring(Files.LastIndexOf("\\") + 1);

                    if (FileName.Contains("Lamp_Warm_Up") || FileName.Contains("Connected_Error_Found") || FileName.Contains("Error_Detected"))
                        continue;

                    if (SearchPattern == "*.csv")
                    {
                        bool isCorrectFormat = false;
                        try
                        {
                            foreach (string line in File.ReadLines(Files))
                            {
                                if (line.Contains("Scan Config Information") ||
                                    line.Contains("Scan Data") ||
                                    line.Contains("Wavelength"))
                                {
                                    isCorrectFormat = true;
                                    break;
                                }
                            }
                        }
                        catch { }
                        //catch(Exception e)
                        //{
                        //    Message.ShowWarning(e.Message);
                        //}

                        if (!isCorrectFormat)
                            continue;
                    }

                    ListFiles.Add(FileName);
                    DateTime FileTime = File.GetLastWriteTime(Files);
                    SavedScanFileTimeList.Add(FileTime);
                }
            }
            catch (UnauthorizedAccessException UAEx) { DBG.WriteLine(UAEx.Message); logFile.Error(UAEx.Message); }
            catch (PathTooLongException PathEx) { DBG.WriteLine(PathEx.Message); logFile.Error(PathEx.Message); }
            catch (Exception e)
            {
                Message.ShowWarning(e.Message);
                LoadFileSuccess = false;
                DBG.WriteLine(e.Message);
                logFile.Error(e.Message);
            }
            return ListFiles;
        }

        private void LoadSavedScanList()
        {
            SavedScanFileList = EnumerateFiles("*.dat");
            SavedScanFileList.AddRange(EnumerateFiles("*.csv"));
            SavedScanSelectList.Clear();
            for (int i = 0; i < SavedScanFileList.Count; i++)
                SavedScanSelectList.Add(false);

            if (!LoadFileSuccess)
            {
                LoadFileSuccess = true;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Dir_Scan_For_New = Path.Combine(path, "InnoSpectra\\Scan Results");
                Dir_Scan_DataBase = Dir_Scan_For_New;
                TextBox_SavedFileDirPath.Text = Dir_Scan_DataBase;
                TextBox_SaveDirPath.Text = Dir_Scan_For_New;
                SaveSettings();
                TextBox_SavedFileDirPath.Text = Dir_Scan_For_New;
                TextBox_SaveDirPath.Text = Dir_Scan_For_New;
                LoadSavedScanList();
            }
        }

        private void LoadSavedScanListByNewThread()
        {
            Thread t = new Thread(LoadSavedScanList);
            t.Start();
        }

        private void AddFileToSavedScanList(String filePath)
        {
            if (Dir_Scan_DataBase != Dir_Scan_For_New)
            {
                Dir_Scan_DataBase = Dir_Scan_For_New;
                TextBox_SavedFileDirPath.Text = Dir_Scan_DataBase;
                LoadSavedScanList();
            }

            DateTime FileTime = File.GetLastWriteTime(filePath);
            string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);

            // Remove duplicate files in the list
            if (File.Exists(filePath))
            {
                for (int i = 0; i < SavedScanFileList.Count; i++)
                {
                    if (filePath.Contains(SavedScanFileList[i]))
                    {
                        SavedScanSelectList.RemoveAt(i);
                        SavedScanFileList.RemoveAt(i);
                        SavedScanFileTimeList.RemoveAt(i);
                    }
                }
            }

            SavedScanSelectList.Add(false);
            SavedScanFileList.Add(fileName);
            SavedScanFileTimeList.Add(FileTime);

            SavedScan_RefreshDataGridView();
        }

        public bool StringContains(string source, string value, StringComparison comparisonType)
        {
            return (source.IndexOf(value, comparisonType) >= 0);
        }
        
        private void SavedScan_RefreshDataGridView()
        {
            SavedScanList.Clear();

            bindingSavedScanList = new BindingList<SavedScanData>(SavedScanList);
            var source = new BindingSource(bindingSavedScanList, null);
            dataGridView_savescan.DataSource = source;
            dataGridView_savescan.Columns[0].HeaderText = "";
            dataGridView_savescan.Columns[1].HeaderText = "File Name";
            dataGridView_savescan.Columns[2].HeaderText = "Time Stamp";

            if (SavedScanFileList.Count > 0)
            {
                bool nameFilter = !string.IsNullOrEmpty(textBox_filter.Text);
                for (int i = 0; i < SavedScanFileList.Count; i++)
                {
                    SavedScanData data = new SavedScanData();
                    if ((RadioButton_SavedScanSelCsv.Checked && StringContains(SavedScanFileList[i], ".csv", StringComparison.OrdinalIgnoreCase) && StringContains(SavedScanFileList[i].Substring(0, SavedScanFileList[i].Length - 4), textBox_filter.Text, StringComparison.OrdinalIgnoreCase)) ||
                        (RadioButton_SavedScanSelDat.Checked && StringContains(SavedScanFileList[i], ".dat", StringComparison.OrdinalIgnoreCase) && StringContains(SavedScanFileList[i].Substring(0, SavedScanFileList[i].Length - 4), textBox_filter.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        data.Select = SavedScanSelectList[i];
                        data.FileName = SavedScanFileList[i];
                        data.TimeStamp = SavedScanFileTimeList[i];
                        SavedScanList.Add(data);
                    }
                }
            }

            foreach (DataGridViewColumn col in dataGridView_savescan.Columns)
                col.SortMode = DataGridViewColumnSortMode.Programmatic;

            dataGridViewSort("TimeStamp", SortOrder.Descending);
            dataGridView_savescan.Columns[2].HeaderCell.SortGlyphDirection = SortOrder.Descending;

            if (dataGridView_savescan.RowCount > 0)
                foreach (DataGridViewRow row in dataGridView_savescan.Rows)
                    dataGridView_savescan.AutoResizeRow(row.Index, DataGridViewAutoSizeRowMode.AllCellsExceptHeader);
        }

        private void Button_DisplayDirChange_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = TextBox_SavedFileDirPath.Text
            };

            if (dlg.ShowDialog() == DialogResult.OK && Dir_Scan_DataBase != dlg.SelectedPath)
            {
                Dir_Scan_DataBase = dlg.SelectedPath;
                TextBox_SavedFileDirPath.Text = dlg.SelectedPath;
                LoadSavedScanList();
                SavedScan_RefreshDataGridView();
                ClearSavedScanCfgItems();
                SaveSettings();

                try { AddDirectorySecurity(Dir_Scan_DataBase); }
                catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }
            }
        }

        private void textBox_filter_TextChanged(object sender, EventArgs e)
        {
            SavedScan_RefreshDataGridView();
        }

        private void button_clear_Click(object sender, EventArgs e)
        {
            textBox_filter.Text = "";
            SavedScan_RefreshDataGridView();
        }

        private void RadioButton_SavedScanSelCsv_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_SavedScanSelCsv.Checked)
                SavedScan_RefreshDataGridView();
        }

        private void RadioButton_SavedScanSelDat_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_SavedScanSelDat.Checked)
                SavedScan_RefreshDataGridView();
        }

        private void dataGridView_savescan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                SavedScan_DeleteItems();
        }

        private void dataGridView_savescan_MouseClick(object sender, MouseEventArgs e)
        {
            if (dataGridView_savescan.Rows.Count == 0 || dataGridView_savescan.SelectedRows.Count <= 0 || (e != null && e.Y < 20))
                return;

            String item = dataGridView_savescan.Rows[dataGridView_savescan.SelectedRows[0].Index].Cells["FileName"].Value.ToString();

            DataGridView.HitTestInfo hitSelectedCell = dataGridView_savescan.HitTest(e.X, e.Y);

            if (e != null && e.Button == MouseButtons.Right)
            {
                int currentMouseOverRow = dataGridView_savescan.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow < 0)
                    return;

                ContextMenuStrip m = new ContextMenuStrip();
                m.Items.Add("Delete");
                if (RadioButton_SavedScanSelDat.Checked && item.Contains(".dat"))
                    m.Items.Add("Convert to CSV");
                m.Items.Add("Average Scan Data");
                m.ItemClicked += new ToolStripItemClickedEventHandler(dataGridView_savescan_contexMenu_ItemClicked);
                m.Show(dataGridView_savescan, new Point(e.X, e.Y));
            }
            else if (hitSelectedCell.Type == DataGridViewHitTestType.Cell)
            {
                String FileName = Path.Combine(TextBox_SavedFileDirPath.Text, item);
                bool fileExisted = File.Exists(FileName);
                if (!fileExisted)
                {
                    String text = "The select file was not existed!\nThe file list will be refeshed.";
                    btn_FileListRefresh_Click(this, null);
                    Message.ShowError(text);
                    return;
                }

                if (IsFileLocked(FileName))
                {
                    DBG.WriteLine("File locked!");
                    logFile.Error("File locked!");
                    String text = "The file can not be opened due to it is in use by another process!";
                    Message.ShowWarning(text);
                    return;
                }

                // Read scan result and populate to the buffer
                if (item.Contains(".dat") && Scan.ReadScanResultFromBinFile(FileName) == SDK.RETURN_FAIL)
                {
                    DBG.WriteLine("Read *.dat file failed!");
                    logFile.Error("Read *.dat file failed!");
                    dataGridView_savescan.Rows[dataGridView_savescan.CurrentCell.RowIndex].Cells["Select"].Value = false;
                    String text = "Read *.dat file failed!\nThis file may not match the format!";
                    Message.ShowWarning(text);
                    return;
                }
                else if (item.Contains(".csv") && Scan.ReadScanResultFromCsvFile(FileName) == SDK.RETURN_FAIL)
                {
                    DBG.WriteLine("Read *.csv file failed!");
                    logFile.Error("Read *.csv file failed!");
                    dataGridView_savescan.Rows[dataGridView_savescan.CurrentCell.RowIndex].Cells["Select"].Value = false;
                    String text = "Read *.csv file failed!\nThis file may not match the supported formats!";
                    Message.ShowWarning(text);
                    return;
                }

                if (dataGridView_savescan.Columns[hitSelectedCell.ColumnIndex].Name == "Select")// && !SelectNone)
                {
                    if ((bool)dataGridView_savescan.CurrentCell.Value == true)
                    {
                        String selectTitle = (String)dataGridView_savescan.Rows[dataGridView_savescan.CurrentCell.RowIndex].Cells["FileName"].Value;
                        selectTitle = selectTitle.Substring(0, selectTitle.LastIndexOf("."));

                        ChartData_RefIntensity.RemoveAll(x => x.Title == selectTitle);
                        ChartData_Intensity.RemoveAll(x => x.Title == selectTitle);
                        ChartData_Absorbance.RemoveAll(x => x.Title == selectTitle);
                        ChartData_Reflectance.RemoveAll(x => x.Title == selectTitle);
                        dataGridView_savescan.CurrentCell.Value = false;
                        RadioButton_SpectrumData_CheckedChanged(null, null);
                        return;
                    }
                    else
                    {
                        Check_Overlay.Checked = true;
                        dataGridView_savescan.CurrentCell.Value = true;
                        SelectScanFileName = item.Substring(0, item.LastIndexOf("."));
                    }
                }
                else
                {
                    dataGridView_savescan.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.EnableResizing;
                    // or even better, use .DisableResizing. Most time consuming enum is DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders

                    Clear_Chart(true);

                    // set it to false if not needed
                    dataGridView_savescan.RowHeadersVisible = false;
                    foreach (DataGridViewRow row in dataGridView_savescan.Rows)
                    {
                        row.Cells["Select"].Value = false;
                    }
                    dataGridView_savescan.Rows[hitSelectedCell.RowIndex].Cells["Select"].Value = true;
                    SelectScanFileName = item.Substring(0, item.LastIndexOf("."));
                }

                Scan.GetScanResult(IsSavedScanData);

                // Draw the scan result
                SpectrumPlot();

                // Populate config data
                ClearSavedScanCfgItems();

                for (Int32 i = 0; i < Scan.ScanConfigData.head.num_sections; i++)
                {
                    Label_SavedScanType[i].Text = Helper.ScanTypeIndexToMode(Scan.ScanConfigData.section[i].section_scan_type).Substring(0, 3);
                    Label_SavedRangeStart[i].Text = Scan.ScanConfigData.section[i].wavelength_start_nm.ToString();
                    Label_SavedRangeEnd[i].Text = Scan.ScanConfigData.section[i].wavelength_end_nm.ToString();
                    Label_SavedWidth[i].Text = Math.Round(Helper.CfgWidthPixelToNM(Scan.ScanConfigData.section[i].width_px, true), 2).ToString();
                    Label_SavedDigRes[i].Text = Scan.ScanConfigData.section[i].num_patterns.ToString();
                    Label_SavedExposure[i].Text = Helper.CfgExpIndexToTime(Scan.ScanConfigData.section[i].exposure_time).ToString();
                }
                Label_SavedAvg.Text = Scan.ScanConfigData.head.num_repeats.ToString();
            }
        }

        private void dataGridView_savescan_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridView gdv = (DataGridView)sender;
            SortOrder so = SortOrder.None;

            if (gdv.Columns[e.ColumnIndex].Name == "Select")
                return;

            if (gdv.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
                gdv.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                so = SortOrder.Descending;
            else
                so = SortOrder.Ascending;

            dataGridViewSort(gdv.Columns[e.ColumnIndex].Name, so);
            gdv.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = so;
        }

        private void SavedScan_DeleteItems()
        {
            // Calculate selected files
            List<string> selFile = new List<string>();
            for (int i = 0; i < dataGridView_savescan.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dataGridView_savescan.Rows[i].Cells["Select"].Value) == true)
                    selFile.Add(dataGridView_savescan.Rows[i].Cells["FileName"].Value.ToString());
            }
            if (selFile.Count == 0)
            {
                selFile.Add(dataGridView_savescan.Rows[dataGridView_savescan.CurrentCell.RowIndex].Cells["FileName"].Value.ToString());
            }

            for (int i = 0; i < selFile.Count; i++)
            {
                string FileNames = selFile[i];
                FileNames = FileNames.Substring(0, FileNames.Length - 4);
                FileNames += "*";
                try
                {
                    foreach (String FilesToDelete in Directory.EnumerateFiles(Dir_Scan_DataBase, FileNames))
                    {
                        File.Delete(FilesToDelete);
                    }
                }
                catch
                {
                    Message.ShowWarning("Cannot delete file [" + FileNames.Substring(0, FileNames.Length - 1) + "]!");
                }
            }

            LoadSavedScanList();
            SavedScan_RefreshDataGridView();

            dataGridView_savescan.ClearSelection();

            if (dataGridView_savescan.Rows.Count > 0)
                dataGridView_savescan.Rows[0].Selected = true;

            Message.ShowInfo("Delete file(s) successfully!");
        }

        private void SavedScan_ConvertToCSV()
        {
            // Calculate selected files
            List<string> selFile = new List<string>();
            for (int i = 0; i < dataGridView_savescan.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dataGridView_savescan.Rows[i].Cells["Select"].Value) == true)
                    selFile.Add(dataGridView_savescan.Rows[i].Cells["FileName"].Value.ToString());
            }
            if (selFile.Count == 0)
            {
                Message.ShowError("No selected files need to be converted into CSV files.");
                return;
            }

            string destFolder;
            FolderBrowserDialog dlg = new FolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                destFolder = dlg.SelectedPath;
                Cursor = Cursors.WaitCursor;
                if (selFile.Count >= 100)
                    ProgressWindowStart(".dat to .csv Conversion", "Convert files in progress... \r\nPlease Wait!", false);

                for (int i = 0; i < selFile.Count; i++)
                {
                    String bFileName = Path.Combine(TextBox_SavedFileDirPath.Text, selFile[i]);
                    String csvFileName = bFileName.Substring(bFileName.LastIndexOf("\\"));
                    csvFileName = csvFileName.Substring(1, csvFileName.Length - 5) + ".csv";
                    csvFileName = csvFileName.Insert(0, "Transfer_");
                    csvFileName = Path.Combine(destFolder, csvFileName);

                    // Read scan result and populate to the buffer
                    if (Scan.ReadScanResultFromBinFile(bFileName) == SDK.RETURN_FAIL)
                    {
                        Cursor = Cursors.Default;
                        if (selFile.Count >= 100)
                            ProgressWindowCompleted();

                        DBG.WriteLine("Read file failed!");
                        logFile.Error("Read file failed!");
                        String text = "Read file failed!\nThis file may not match the format!";
                        MessageBox.Show(text, "Warning");
                        return;
                    }

                    Scan.GetScanResult(IsSavedScanData);
                    FileStream fs = new FileStream(@csvFileName, FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    SaveHeader(sw, false, true);

                    sw.WriteLine("Wavelength (nm),Absorbance (AU),Reference Signal (unitless),Sample Signal (unitless)");
                    for (int j = 0; j < Scan.ScanDataLen; j++)
                    {
                        sw.WriteLine(Scan.WaveLength[j] + CSV_Delimiter + Scan.Absorbance[j] + CSV_Delimiter + Scan.ReferenceIntensity[j] + CSV_Delimiter + Scan.Intensity[j]);
                    }

                    sw.Flush();  // Clear buffer
                    sw.Close();  // Close file stream 
                }

                if (Dir_Scan_DataBase == destFolder)
                    LoadSavedScanList();

                Cursor = Cursors.Default;
                if (selFile.Count >= 100)
                    ProgressWindowCompleted();
                Message.ShowInfo("Convert .dat to .csv successfully!");

                // Need reading device information again after the file read.
                if (Device.IsConnected() && Device.Information() != 0)
                {
                    DBG.WriteLine("Device Information read failed!");
                    logFile.Error("Device Information read failed!");
                }
            }
        }

        private void SavedScan_AverageScanData()
        {
            TargetScanCounts = 0;
            List<string> selFile = new List<string>();
            for (int i = 0; i < dataGridView_savescan.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dataGridView_savescan.Rows[i].Cells["Select"].Value) == true)
                {
                    selFile.Add(dataGridView_savescan.Rows[i].Cells["FileName"].Value.ToString());
                    TargetScanCounts++;
                }
            }
            if (TargetScanCounts <= 1)
            {
                Message.ShowError("The scan data cannot be averaged because the number of selected files is insufficient.");
                return;
            }

            ScannedCounts = 0;
            for (int i = 0; i < selFile.Count; i++)
            {
                String FileName = Path.Combine(TextBox_SavedFileDirPath.Text, selFile[i]);
                
                if (IsFileLocked(FileName))
                {
                    DBG.WriteLine("File locked!");
                    logFile.Error("File locked!");
                    continue;
                }

                // Read scan result and populate to the buffer
                if (selFile[i].Contains(".dat") && Scan.ReadScanResultFromBinFile(FileName) == SDK.RETURN_FAIL)
                {
                    DBG.WriteLine("Read *.dat file failed!");
                    logFile.Error("Read *.dat file failed!");
                    String text = "Read *.dat file failed!\nThis file may not match the format!";
                    Message.ShowWarning(text);
                    return;
                }
                else if (selFile[i].Contains(".csv") && Scan.ReadScanResultFromCsvFile(FileName) == SDK.RETURN_FAIL)
                {
                    DBG.WriteLine("Read *.csv file failed!");
                    logFile.Error("Read *.csv file failed!");
                    String text = "Read *.csv file failed!\nThis file may not match the format!";
                    Message.ShowWarning(text);
                    return;
                }

                ScannedCounts++;
                Scan.GetScanResult(IsSavedScanData);
                SaveToAverageCSV(FileName);
            }

            Message.ShowInfo("Average selected files successfully!");

            // Need reading device information again after the file read.
            if (Device.IsConnected() && Device.Information() != 0)
            {
                DBG.WriteLine("Device Information read failed!");
                logFile.Error("Device Information read failed!");
            }
        }

        void dataGridView_savescan_contexMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;

            if (item.Text == "Delete")
            {
                SavedScan_DeleteItems();
            }
            else if (item.Text == "Convert to CSV")
            {
                SavedScan_ConvertToCSV();
            }
            else if (item.Text == "Average Scan Data")
            {
                SavedScan_AverageScanData();
            }
        }

        private void dataGridView_savescan_CheckBox_SelectAll_CheckedChanged(object sender, EventArgs e)
        {
            //Necessary to end the edit mode of the cell
            dataGridView_savescan.EndEdit();

            //Loop and check and uncheck all row CheckBoxes based on Header Cell CheckBox
            CheckBox headerBox = (CheckBox)dataGridView_savescan.Controls["SelectAll"];
            foreach (DataGridViewRow row in dataGridView_savescan.Rows)
                row.Cells["Select"].Value = headerBox.Checked;

            // Update to saved scan file list
            for (int i = 0; i < SavedScanSelectList.Count; i++)
                SavedScanSelectList[i] = headerBox.Checked;
        }
        #endregion
        #region Chart
        private void initChart()
        {
            // Initial Chart
            RadioButton_Intensity.Checked = true;
            MyChart.DataTooltip = null;
            MyChart.Zoom = ZoomingOptions.None;
            MyChart.DisableAnimations = true;
        }
        private void SpectrumPlot()
        {
            if (tabScanPage.SelectedIndex == 2 && Button_Scan.Text == "Reference Scan" && !IsSavedScanData)
                return;

            if (MyChart.Series.Count > 0)
            {
                if (!Check_Overlay.Checked)
                    Clear_Chart();
                else
                {
                    if (TargetScanCounts > 1 && ScannedCounts == 1)  //做continuous scan,掃描第一次要移除之前的serious,Y軸的圖才會有精確度 
                    {
                        Clear_Chart();
                    }
                    else if (MyChart.Series[0].Values.Count == 0)
                    {
                        MyChart.Series.RemoveAt(0);
                    }
                }
            }

            double[] valY = new double[Scan.ScanDataLen];
            double[] valX = new double[Scan.ScanDataLen];
            int dataCount = 0;
            valX = Scan.WaveLength.Select(d => Math.Round(d, 2, MidpointRounding.AwayFromZero)).ToArray();
            bool dataValid = false;

            String scanFileName = "";
            if (Button_Scan.Text != "Reference Scan")
                scanFileName = Path.GetFileName(CurrentScanFileName);
            else
                scanFileName = "Local Reference";

            if (Scan.ScanConfigData.head.config_name != null)
            {
                if (!Check_Overlay.Checked)
                {
                    Clear_Chart(true);
                }

                int secNum = Scan.ScanConfigData.head.num_sections == 0 ? 1 : Scan.ScanConfigData.head.num_sections;
                for (int i = 0; i < secNum; i++)
                {
                    var chartValues_ref = new GearedValues<CustomerVm>();
                    var chartValues_i = new GearedValues<CustomerVm>();
                    var chartValues_a = new GearedValues<CustomerVm>();
                    var chartValues_r = new GearedValues<CustomerVm>();

                    int patNum = secNum == 1 ? valY.Length : Scan.ScanConfigData.section[i].num_patterns;
                    for (int j = 0; j < patNum; j++)
                    {
                        double x = Math.Round(Scan.WaveLength[j + dataCount], 2, MidpointRounding.AwayFromZero);
                        if (Scan.ReferenceIntensity.Count > 0)
                            chartValues_ref.Add(new CustomerVm
                            {
                                x = x,
                                y = Scan.ReferenceIntensity[j + dataCount],
                                fileName = IsSavedScanData ? SelectScanFileName : scanFileName,
                                serialNumber = Device.DevInfo.SerialNumber,
                                temp = Scan.SensorData[0].ToString(),
                                humi = Scan.SensorData[2].ToString()
                            });
                        if (Scan.Intensity.Count > 0)
                            chartValues_i.Add(new CustomerVm
                            {
                                x = x,
                                y = Scan.Intensity[j + dataCount],
                                fileName = IsSavedScanData ? SelectScanFileName : scanFileName,
                                serialNumber = Device.DevInfo.SerialNumber,
                                temp = Scan.SensorData[0].ToString(),
                                humi = Scan.SensorData[2].ToString()
                            });

                        if (Scan.Absorbance.Count > 0)
                            chartValues_a.Add(new CustomerVm
                            {
                                x = x,
                                y = Math.Round(Scan.Absorbance[j + dataCount], 8, MidpointRounding.AwayFromZero),
                                fileName = IsSavedScanData ? SelectScanFileName : scanFileName,
                                serialNumber = Device.DevInfo.SerialNumber,
                                temp = Scan.SensorData[0].ToString(),
                                humi = Scan.SensorData[2].ToString()
                            });
                        if (Scan.Reflectance.Count > 0)
                            chartValues_r.Add(new CustomerVm
                            {
                                x = x,
                                y = Math.Round(Scan.Reflectance[j + dataCount], 8, MidpointRounding.AwayFromZero),
                                fileName = IsSavedScanData ? SelectScanFileName : scanFileName,
                                serialNumber = Device.DevInfo.SerialNumber,
                                temp = Scan.SensorData[0].ToString(),
                                humi = Scan.SensorData[2].ToString()
                            });
                    }
                    dataCount += Scan.ScanConfigData.section[i].num_patterns;

                    if (chartValues_ref.Count > 0)
                    {
                        ChartData_RefIntensity.Add(new GLineSeries
                        {
                            Values = chartValues_ref,
                            Title = IsSavedScanData ? SelectScanFileName : scanFileName,
                            Stroke = StrokeColors[ChartData_RefIntensity.Count % NumOfStrokeColors],
                            StrokeThickness = 1,
                            Fill = System.Windows.Media.Brushes.Transparent,
                            LineSmoothness = 0,
                            PointGeometry = null,
                            PointGeometrySize = 0,
                        });
                    }

                    if (chartValues_i.Count > 0)
                    {
                        ChartData_Intensity.Add(new GLineSeries
                        {
                            Values = chartValues_i,
                            Title = IsSavedScanData ? SelectScanFileName : scanFileName,
                            Stroke = StrokeColors[ChartData_Intensity.Count % NumOfStrokeColors],
                            StrokeThickness = 1,
                            Fill = System.Windows.Media.Brushes.Transparent,
                            LineSmoothness = 0,
                            PointGeometry = null,
                            PointGeometrySize = 0,
                        });
                    }

                    if (chartValues_a.Count > 0)
                    {
                        ChartData_Absorbance.Add(new GLineSeries
                        {
                            Values = chartValues_a,
                            Title = IsSavedScanData ? SelectScanFileName : scanFileName,
                            Stroke = StrokeColors[ChartData_Absorbance.Count % NumOfStrokeColors],
                            StrokeThickness = 1,
                            Fill = System.Windows.Media.Brushes.Transparent,
                            LineSmoothness = 0,
                            PointGeometry = null,
                            PointGeometrySize = 0,
                        });
                    }

                    if (chartValues_r.Count > 0)
                    {
                        ChartData_Reflectance.Add(new GLineSeries
                        {
                            Values = chartValues_r,
                            Title = IsSavedScanData ? SelectScanFileName : scanFileName,
                            Stroke = StrokeColors[ChartData_Reflectance.Count % NumOfStrokeColors],
                            StrokeThickness = 1,
                            Fill = System.Windows.Media.Brushes.Transparent,
                            LineSmoothness = 0,
                            PointGeometry = null,
                            PointGeometrySize = 0,
                        });
                    }
                }
            }

            if (tabScanPage.SelectedIndex == 2 && !Check_Overlay.Checked) // Save scans tab selected
            {
                RadioButton_Reference.Enabled = (Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0;
                RadioButton_Intensity.Enabled = (Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0;
                RadioButton_Absorbance.Enabled = (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Absorbance) > 0) || ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reflectance) > 0)) ||
                    (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0) && ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0));
                RadioButton_Reflectance.Enabled = (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Absorbance) > 0) || ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reflectance) > 0)) ||
                    (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0) && ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0));
                Scan.Overlap_Data_Content_Idx = 0;
            }
            else if (tabScanPage.SelectedIndex == 2 && Check_Overlay.Checked)
            {
                Scan.Overlap_Data_Content_Idx |= Scan.Imported_Data_Content_Idx;
                
                int dataCounter = 0;

                if (RadioButton_Intensity.Checked)
                    dataCounter = ChartData_Intensity.Count;
                else if (RadioButton_Reference.Checked)
                    dataCounter = ChartData_RefIntensity.Count;
                else if (RadioButton_Absorbance.Checked)
                    dataCounter = ChartData_Absorbance.Count;
                else if (RadioButton_Reflectance.Checked)
                    dataCounter = ChartData_Reflectance.Count;

                bool plotDataValid = (RadioButton_Reference.Checked && (Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0) ||
                                        (RadioButton_Intensity.Checked && (Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0) ||
                                        (RadioButton_Absorbance.Checked && (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Absorbance) > 0) || ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reflectance) > 0)) ||
                                        (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0) && ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0))) ||
                                        (RadioButton_Reflectance.Checked && (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Absorbance) > 0) || ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reflectance) > 0)) ||
                                        (((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Reference) > 0) && ((Scan.Imported_Data_Content_Idx & Scan.ImportedDataType.Sample) > 0)));

                if (!plotDataValid && dataCounter > 1)
                    Message.ShowError("The file does not have valid data for the selected spectrum overlap!");

                RadioButton_Reference.Enabled = ChartData_RefIntensity.Count > 0;
                RadioButton_Intensity.Enabled = ChartData_Intensity.Count > 0;
                RadioButton_Absorbance.Enabled = ChartData_Absorbance.Count > 0;
                RadioButton_Reflectance.Enabled = ChartData_Reflectance.Count > 0;
            }

            if ((!IsSavedScanData && RadioButton_Intensity.Checked) || (IsSavedScanData && RadioButton_Intensity.Checked && RadioButton_Intensity.Enabled))
            {
                if (Scan.Intensity != null && Scan.Intensity.Count != 0)
                {
                    List<double> doubleList = Scan.Intensity.ConvertAll(x => (double)x);
                    valY = doubleList.ToArray();
                    dataValid = true;
                }
                else
                {
                    dataValid = false;
                    if (AppLoaded && !Check_Overlay.Checked)
                        Message.ShowError("Sample intensity is not valid!");
                }
            }
            else if ((!IsSavedScanData && RadioButton_Absorbance.Checked) || (IsSavedScanData && RadioButton_Absorbance.Checked && RadioButton_Absorbance.Enabled))
            {
                if (Scan.Absorbance != null && Scan.Absorbance.Count != 0)
                {
                    valY = Scan.Absorbance.Select(d => Math.Round(d, 8, MidpointRounding.AwayFromZero)).ToArray();
                    dataValid = true;
                }
                else
                {
                    dataValid = false;
                    if (AppLoaded && !Check_Overlay.Checked)
                        Message.ShowError("Can not calculate absorbance!\nSample or reference intensity is not valid!");
                }
            }
            else if ((!IsSavedScanData && RadioButton_Reflectance.Checked) || (IsSavedScanData && RadioButton_Reflectance.Checked && RadioButton_Reflectance.Enabled))
            {
                if (Scan.Reflectance != null && Scan.Reflectance.Count != 0)
                {
                    valY = Scan.Reflectance.Select(d => Math.Round(d, 8, MidpointRounding.AwayFromZero)).ToArray();
                    dataValid = true;
                }
                else
                {
                    dataValid = false;
                    if (AppLoaded && !Check_Overlay.Checked)
                        Message.ShowError("Can not calculate reflectance!\nSample or reference intensity is not valid!");
                }
            }
            else if ((!IsSavedScanData && RadioButton_Reference.Checked) || (IsSavedScanData && RadioButton_Reference.Checked && RadioButton_Reference.Enabled))
            {
                if (Scan.ReferenceIntensity != null && Scan.ReferenceIntensity.Count != 0)
                {
                    List<double> doubleList = Scan.ReferenceIntensity.ConvertAll(x => (double)x);
                    valY = doubleList.ToArray();
                    dataValid = true;
                }
                else
                {
                    dataValid = false;
                    if (AppLoaded && !Check_Overlay.Checked)
                        Message.ShowError("Reference intensity is not valid!");
                }
            }
            else if (IsSavedScanData)
            {
                if (RadioButton_Intensity.Enabled)
                    RadioButton_Intensity.Checked = true;
                else if (RadioButton_Absorbance.Enabled)
                    RadioButton_Absorbance.Checked = true;
                else if (RadioButton_Reflectance.Enabled)
                    RadioButton_Reflectance.Checked = true;
                else if (RadioButton_Reference.Enabled)
                    RadioButton_Reference.Checked = true;
                return;
            }

            if (valY.Length != 0 && dataValid)
            {
                RadioButton_SpectrumData_CheckedChanged(null, null);
            }

            if (!dataValid && !Check_Overlay.Checked)
                Clear_Chart();
        }
        private void GetMaxMinWav(ref int min, ref int max)
        {
            if (min == 0)
                min = int.MaxValue;
            if (max == 0)
                max = int.MinValue;

            for (int i = 0; i < Scan.ScanConfigData.head.num_sections; i++)
            {
                if (Scan.ScanConfigData.section[i].wavelength_start_nm < min)
                {
                    min = Scan.ScanConfigData.section[i].wavelength_start_nm;
                }
                if (Scan.ScanConfigData.section[i].wavelength_end_nm > max)
                {
                    max = Scan.ScanConfigData.section[i].wavelength_end_nm;
                }
            }
        }

        private void RadioButton_SpectrumData_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rBtn = sender as RadioButton;
            if (rBtn == null || rBtn.Checked)
            {
                MyChart.Series.Clear();
                MyChart.AxisX.Clear();
                MyChart.AxisY.Clear();

                int max_X = int.MinValue;
                int min_X = int.MaxValue;
                double max_Y = double.MinValue;
                double min_Y = double.MaxValue;

                List<GLineSeries> s = new List<GLineSeries>();
                if (RadioButton_Reference.Checked)
                    s = ChartData_RefIntensity;
                else if (RadioButton_Intensity.Checked)
                    s = ChartData_Intensity;
                else if (RadioButton_Absorbance.Checked)
                    s = ChartData_Absorbance;
                else if (RadioButton_Reflectance.Checked)
                    s = ChartData_Reflectance;

                foreach (GLineSeries gl in s)
                {
                    foreach (CustomerVm v in gl.Values)
                    {
                        max_X = (v.x > max_X) ? (int)v.x : max_X;
                        min_X = (v.x < min_X) ? (int)v.x : min_X;
                        max_Y = (v.y > max_Y) ? v.y : max_Y;
                        min_Y = (v.y < min_Y) ? v.y : min_Y;
                    }
                }

                if (Scan.ScanConfigData.section != null)
                {
                    MyChart.AxisX.Add(new Axis
                    {
                        Title = "Wavelength (nm)",
                        MinValue = min_X,
                        MaxValue = max_X,
                        Separator = new Separator
                        {
                            Step = 50,
                            IsEnabled = false
                        }
                    });
                }
                else
                {
                    MyChart.AxisX.Add(new Axis
                    {
                        Title = "Wavelength (nm)",
                        MinValue = Device.DevInfo.MinWavelength == 0 ? 900 : Device.DevInfo.MinWavelength,
                        MaxValue = Device.DevInfo.MaxWavelength == 0 ? 1700 : Device.DevInfo.MaxWavelength,
                        Separator = new Separator
                        {
                            Step = 50,
                            IsEnabled = false
                        }
                    });
                }

                double amplitudeY = Math.Abs(max_Y - min_Y);
                max_Y = max_Y + (0.1 * amplitudeY);
                min_Y = min_Y - (0.1 * amplitudeY);

                String labelY = "";
                if (RadioButton_Intensity.Checked)
                {
                    labelY = "Intensity";
                    if (ChartData_Intensity.Count == 0)
                        Clear_Chart();
                    else
                        MyChart.Series.AddRange(ChartData_Intensity);
                }
                else if (RadioButton_Reference.Checked)
                {
                    labelY = "Reference";
                    if (ChartData_RefIntensity.Count == 0)
                        Clear_Chart();
                    else
                        MyChart.Series.AddRange(ChartData_RefIntensity);
                }
                else if (RadioButton_Absorbance.Checked)
                {
                    labelY = "Absorbance";
                    if (ChartData_Absorbance.Count == 0)
                        Clear_Chart();
                    else
                        MyChart.Series.AddRange(ChartData_Absorbance);
                }
                else if (RadioButton_Reflectance.Checked)
                {
                    labelY = "Reflectance";
                    if (ChartData_Reflectance.Count == 0)
                        Clear_Chart();
                    else
                        MyChart.Series.AddRange(ChartData_Reflectance);
                }

                if (RadioButton_Absorbance.Checked || RadioButton_Reflectance.Checked)
                {
                    if (max_Y != min_Y)
                        MyChart.AxisY.Add(new Axis
                        {
                        Title = labelY,
                        MinValue = min_Y,
                        MaxValue = max_Y,
                        LabelFormatter = chartLabelFormatFunc
                        });
                    else
                        MyChart.AxisY.Add(new Axis
                        {
                            Title = labelY,
                            LabelFormatter = chartLabelFormatFunc
                        });
                }
                else
                {
                    if (max_Y != min_Y)
                        MyChart.AxisY.Add(new Axis
                        {
                            Title = labelY,
                            MinValue = min_Y,
                            MaxValue = max_Y
                        });
                    else
                        MyChart.AxisY.Add( new Axis{ Title = labelY });
                }

                if (tabScanPage.SelectedIndex == 2)
                {
                    RadioButton_Reference.Enabled = ChartData_RefIntensity.Count > 0;
                    RadioButton_Intensity.Enabled = ChartData_Intensity.Count > 0;
                    RadioButton_Absorbance.Enabled = ChartData_Absorbance.Count > 0;
                    RadioButton_Reflectance.Enabled = ChartData_Reflectance.Count > 0;
                }
                else
                {
                    RadioButton_Reference.Enabled = true;
                    RadioButton_Intensity.Enabled = true;
                    RadioButton_Absorbance.Enabled = true;
                    RadioButton_Reflectance.Enabled = true;
                }
            }
        }

        #endregion
        #region scan item set
        private void CheckBox_AutoGain_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBox_AutoGain.Checked == true)
            {
                ComboBox_PGAGain.Enabled = false;
            }
            else
            {
                ComboBox_PGAGain.Enabled = true;
            }
        }
        private void CheckBox_LampOn_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBox_LampOn.Checked == true)
            {
                RadioButton_LampOn.Checked = true;
                RadioButton_LampOn_CheckedChanged(sender, e);
            }
            else
            {
                RadioButton_LampStableTime.Checked = true;
                RadioButton_LampStableTime_CheckedChanged(sender, e);
            }
        }
        private void RadioButton_LampOn_CheckedChanged(object sender, EventArgs e)
        {
            if (!RadioButton_LampOn.Checked)
                return;

            String HWRev = String.Empty;
            if (Device.IsConnected())
                HWRev = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;

            //GetActivationKeyStatus();
            if (GetFW_LEVEL() < FW_LEVEL.LEVEL_2 || label_ActivateStatus.Text.Equals("Activated") == false)
            {
                CheckBox_AutoGain.Checked = false;
                CheckBox_AutoGain.Enabled = false;
                CheckBox_AutoGain_CheckedChanged(sender, e);
            }
            else
            {
                CheckBox_AutoGain.Enabled = true;
                CheckBox_AutoGain.Checked = true;
                CheckBox_AutoGain_CheckedChanged(sender, e);
            }
            RadioButton_Absorbance.Enabled = true;
            RadioButton_Reflectance.Enabled = true;
            TextBox_LampStableTime.Enabled = false;
            Scan.SetLamp(Scan.LAMP_CONTROL.ON_SCAN);

            Double ScanTime = Scan.GetEstimatedScanTime();
            if (ScanTime > 0)
                Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";
        }

        private void RadioButton_LampOff_CheckedChanged(object sender, EventArgs e)
        {
            if (!RadioButton_LampOff.Checked)
                return;

            String HWRev = String.Empty;
            if (Device.IsConnected())
                HWRev = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;

            if (GetFW_LEVEL() < FW_LEVEL.LEVEL_2 || label_ActivateStatus.Text.Equals("Activated") == false)
            {
                CheckBox_AutoGain.Checked = false;
                CheckBox_AutoGain.Enabled = false;
                CheckBox_AutoGain_CheckedChanged(sender, e);
            }
            else
            {
                CheckBox_AutoGain.Enabled = true;
                CheckBox_AutoGain.Checked = true;
                CheckBox_AutoGain_CheckedChanged(sender, e);
            }
            if (RadioButton_LampOff.Checked)
            {
                RadioButton_Intensity.Checked = true;
            }
            TextBox_LampStableTime.Enabled = false;

            Scan.SetLamp(Scan.LAMP_CONTROL.OFF_SCAN);

            Double ScanTime = Scan.GetEstimatedScanTime();
            if (ScanTime > 0)
                Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";
        }

        private void RadioButton_WarmUp_CheckedChanged(object sender, EventArgs e)
        {
            if (!RadioButton_WarmUp.Checked)
                return;

            if (UInt32.TryParse(TextBox_WarmUpTime.Text, out UInt32 WarmUpTime) == true)
            {
                UserCancelScan = false;  // Clear this flag before progress bar starting
                string pbString = "Lamp warm-up in progress, " + (WarmUpTime * 60) + " seconds left... \r\nPlease Wait!";
                ProgressWindowStart("Lamp Warm-up", pbString, true);

                List<double> timeStamp = new List<double>();
                List<double> sysTemp = new List<double>();
                List<double> sysHumi = new List<double>();
                List<double> tivaTemp = new List<double>();
                List<uint> lampADC0 = new List<uint>();
                List<uint> lampADC1 = new List<uint>();
                List<uint> lampADC2 = new List<uint>();
                List<uint> lampADC3 = new List<uint>();
                TimeSpan ts;

                Scan.SetLamp(Scan.LAMP_CONTROL.ON_SCAN);
                Thread.Sleep(625); // Wait for lamp stable
                DateTime startTime = DateTime.Now;
                DateTime currentTime = startTime;
                DateTime endTime = startTime.AddMinutes(WarmUpTime);
                endTime = endTime.AddSeconds(1);  // To get the lastest data
                bool bSensorRead = false;

                while (currentTime <= endTime && !UserCancelScan)
                {
                    ts = currentTime - startTime;
                    if ((int)(ts.TotalSeconds) % 5 == 0 && !bSensorRead)
                    {
                        timeStamp.Add((int)(ts.TotalSeconds));
                        if (Device.ReadSensorsData() == SDK.RETURN_PASS)
                        {
                            sysTemp.Add(Device.DevSensors.HDCTemp);
                            sysHumi.Add(Device.DevSensors.Humidity);
                            tivaTemp.Add(Device.DevSensors.TivaTemp);
                        }
                        else
                        {
                            sysTemp.Add(-1);
                            sysHumi.Add(-1);
                            tivaTemp.Add(-1);
                        }

                        Thread.Sleep(100);

                        if (Device.ReadLampParam() == SDK.RETURN_PASS)
                        {
                            lampADC0.Add(Device.LampADC[0]);
                            lampADC1.Add(Device.LampADC[1]);
                            lampADC2.Add(Device.LampADC[2]);
                            lampADC3.Add(Device.LampADC[3]);
                        }
                        else
                        {
                            lampADC0.Add(0);
                            lampADC1.Add(0);
                            lampADC2.Add(0);
                            lampADC3.Add(0);
                        }
                        bSensorRead = true;
                    }
                    else if ((int)(ts.TotalSeconds) % 5 != 0)
                        bSensorRead = false;

                    currentTime = DateTime.Now;
                    Application.DoEvents();

                    pbString = "Lamp warm-up in progress, " + (int)((endTime - currentTime).TotalSeconds) + " seconds left... \r\nPlease Wait!";
                    ProgressWindowContentUpdate(pbString);
                }

                String FileName;
                Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
                Int32 MB_Ver = HWRev[0];

                FileName = Device.DevInfo.ModelName + "_" + Device.DevInfo.SerialNumber;
                FileName = Path.Combine(Dir_Scan_For_New, FileName + "_Lamp_Warm_Up_" + startTime.ToString("yyyyMMdd_HHmmss") + ".csv");

                FileStream fs = new FileStream(@FileName, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);
                if (MB_Ver >= 'F' && MB_Ver != 'N')
                {
                    sw.WriteLine("Time,System Temp,System Humidity,Tiva Temp,Lamp ADC0,Lamp ADC1,Lamp ADC2,Lamp ADC3");
                    for (int i = 0; i < timeStamp.Count; i++)
                        sw.WriteLine(timeStamp[i] + CSV_Delimiter + sysTemp[i] + CSV_Delimiter + sysHumi[i] + CSV_Delimiter + tivaTemp[i] + CSV_Delimiter + lampADC0[i] + CSV_Delimiter + lampADC1[i] + CSV_Delimiter + lampADC2[i] + CSV_Delimiter + lampADC3[i]);
                }
                else
                {
                    sw.WriteLine("Time,System Temp,System Humidity,Tiva Temp,Lamp Indicator");
                    for (int i = 0; i < timeStamp.Count; i++)
                        sw.WriteLine(timeStamp[i] + CSV_Delimiter + sysTemp[i] + CSV_Delimiter + sysHumi[i] + CSV_Delimiter + tivaTemp[i] + CSV_Delimiter + lampADC0[i]);
                }
                sw.Flush();
                sw.Close();

                RadioButton_LampStableTime.Checked = true;
                ProgressWindowCompleted();
            }
        }

        private void TextBox_WarmUpTime_TextChanged(object sender, EventArgs e)
        {
            if (UInt32.TryParse(TextBox_WarmUpTime.Text, out UInt32 WarmUpTime) == false)
            {
                String text = "Warm-up Time must be numeric!";
                MessageBox.Show(text, "Warning");

                Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
                Int32 MB_Ver = HWRev[0];

                if (Con_OneMin_WarmUp.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                {
                    TextBox_WarmUpTime.Text = "1";
                }
                else
                {
                    TextBox_WarmUpTime.Text = "3";
                }
            }
        }

        private void RadioButton_LampStableTime_CheckedChanged(object sender, EventArgs e)
        {
            if (!RadioButton_LampStableTime.Checked)
                return;

            String HWRev = String.Empty;
            if (Device.IsConnected())
                HWRev = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;

            if (label_ActivateStatus.Text.Equals("Activated") == true)
                TextBox_LampStableTime.Enabled = true;
            CheckBox_AutoGain.Enabled = true;
            RadioButton_Absorbance.Enabled = true;
            RadioButton_Reflectance.Enabled = true;

            Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
            Scan.SetLampDelay(LampStableTime);

            Double ScanTime = Scan.GetEstimatedScanTime();
            if (ScanTime > 0)
                Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";
        }

        private void TextBox_LampStableTime_TextChanged(object sender, EventArgs e)
        {
            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2)
            {
                if (UInt32.TryParse(TextBox_LampStableTime.Text, out LampStableTime) == false)
                {
                    String text = "Lamp Stable Time must be numeric!";
                    MessageBox.Show(text, "Warning");
                    TextBox_LampStableTime.Text = "625";
                }
            }
        }

        public enum GUI_State
        {
            DEVICE_ON,
            DEVICE_ON_SCANTAB_SELECT,
            DEVICE_OFF,
            DEVICE_OFF_SCANTAB_SELECT,
            SCAN,
            SCAN_FINISHED,
            FW_UPDATE,
            FW_UPDATE_FINISHED,
            REFERENCE_DATA_UPDATE,
            REFERENCE_DATA_UPDATE_FINISHED,
            KEY_ACTIVATE,
            KEY_NOT_ACTIVATE,
        };
        private void GUI_Handler(int state, bool reload = false)
        {
            if (previous_state == state)
                return;
            else
                previous_state = state;

            Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
            Int32 MB_Ver = HWRev[0];

            switch (state)
            {
                case (int)MainWindow.GUI_State.KEY_ACTIVATE:
                    {
                        CheckBox_LampOn.Visible = false;

                        if (Con_No_KeepLampOn.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                        {
                            RadioButton_LampOn.Visible = false;
                        }
                        else
                        {
                            RadioButton_LampOn.Visible = true;
                        }

                        if (Con_No_WarmUp.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                        {
                            RadioButton_WarmUp.Visible = false;
                            TextBox_WarmUpTime.Visible = false;
                        }
                        else
                        {
                            RadioButton_WarmUp.Visible = true;
                            TextBox_WarmUpTime.Visible = true;

                            if (Con_OneMin_WarmUp.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier())
                            {
                                TextBox_WarmUpTime.Text = "1";
                            }
                            else
                            {
                                TextBox_WarmUpTime.Text = "3";
                            }
                        }

                        RadioButton_LampOff.Visible = true;
                        RadioButton_LampStableTime.Visible = true;
                        TextBox_LampStableTime.Visible = true;

                        RadioButton_LampOff.Enabled = true;
                        RadioButton_LampStableTime.Enabled = true;
                        TextBox_LampStableTime.Enabled = true;

                        RadioButton_LampStableTime.Checked = true;
                        if (CheckBox_Cal_WriteEnable.Checked)
                        {
                            Button_Cal_RestoreDefaultCoeffs.Enabled = true;
                        }

                        GroupBox_BleName.Enabled = true;
                        Label_Blename.Enabled = true;
                        Label_BleNameValue.Enabled = true;

                        Label_ButtonStatus.Enabled = true;
                        Button_LockButton.Enabled = true;
                        Button_UnlockButton.Enabled = true;
                        Int32 status = Device.GetButtonLockStatus();
                        if (status == 1)
                            Label_ButtonStatus.Text = "Button Status: Locked!";
                        else if (status == 0)
                            Label_ButtonStatus.Text = "Button Status: Unlocked!";
                        else
                            Label_ButtonStatus.Text = "Button Status: Read Failed!";

                        toolStripStatus_DeviceStatus.Text = (Device.DevInfo.MinWavelength == 900 ? "Standard Wavelength " : Device.DevInfo.MinWavelength == 1350 ? "Extended Wavelength " : "Extended Plus Wavelength ") +
                            "Device: " + Device.DevInfo.ModelName + " (" + Device.DevInfo.SerialNumber + ")";
                        label_ActivateStatus.Text = "Activated";
                        break;
                    }
                case (int)MainWindow.GUI_State.KEY_NOT_ACTIVATE:
                    {
                        CheckBox_LampOn.Visible = true;
                        RadioButton_LampOn.Visible = false;
                        RadioButton_LampOff.Visible = false;
                        RadioButton_WarmUp.Visible = false;
                        TextBox_WarmUpTime.Visible = false;
                        RadioButton_LampStableTime.Visible = false;
                        TextBox_LampStableTime.Visible = false;

                        RadioButton_LampStableTime.Checked = false;

                        CheckBox_LampOn.Checked = false;
                        RadioButton_LampOn.Checked = false;
                        RadioButton_LampOff.Checked = false;
                        RadioButton_WarmUp.Checked = false;
                        Button_Cal_RestoreDefaultCoeffs.Enabled = false;

                        Label_ButtonStatus.Enabled = false;
                        Button_LockButton.Enabled = false;
                        Button_UnlockButton.Enabled = false;
                        Label_ButtonStatus.Text = "Button Status: NA";

                        GroupBox_BleName.Enabled = false;
                        Label_Blename.Enabled = false;
                        Label_BleNameValue.Enabled = false;
                        Label_BleNameValue.Text = "NA";

                        toolStripStatus_DeviceStatus.Text = (Device.DevInfo.MinWavelength == 900 ? "Standard Wavelength " : Device.DevInfo.MinWavelength == 1350 ? "Extended Wavelength " : "Extended Plus Wavelength ") +
                            "Device: " + Device.DevInfo.ModelName + " (" + Device.DevInfo.SerialNumber + "), advanced functions locked!";
                        label_ActivateStatus.Text = "Not Activated!";
                        break;
                    }
                default:
                    break;
            }
            if (reload)
            {
                this.Dispose();
                new MainWindow(null).ShowDialog();
            }
        }
        #endregion
        #region save scan to file
        private void SaveToFiles()
        {
            CurrentScanFileName = String.Empty;
            if (CheckBox_FileNamePrefix.Checked == true)
            {
                String Prefix1 = TextBox_FileNamePrefix1.Text.Substring(0, TextBox_FileNamePrefix1.Text.Length);
                String Prefix2 = TextBox_FileNamePrefix2.Text.Substring(0, TextBox_FileNamePrefix2.Text.Length);
                String Prefix3 = TextBox_FileNamePrefix3.Text.Substring(0, TextBox_FileNamePrefix3.Text.Length);

                String combinedPrefix = "";
                combinedPrefix = String.IsNullOrEmpty(Prefix1) ? "" : (Prefix1 + "_");
                combinedPrefix = String.IsNullOrEmpty(Prefix2) ? combinedPrefix : (combinedPrefix + Prefix2 + "_");
                combinedPrefix = String.IsNullOrEmpty(Prefix3) ? combinedPrefix : (combinedPrefix + Prefix3 + "_"); 

                CurrentScanFileName = Path.Combine(Dir_Scan_For_New, combinedPrefix + Scan.ScanConfigData.head.config_name + "_" + TimeScanStart.ToString("yyyyMMdd_HHmmss"));
                CurrentScanFileName = @"\\?\" + CurrentScanFileName;
            }
            else
            {
                CurrentScanFileName = Path.Combine(Dir_Scan_For_New, Scan.ScanConfigData.head.config_name + "_" + TimeScanStart.ToString("yyyyMMdd_HHmmss"));
            }

            //check path
            String dirpath = Path.GetDirectoryName(CurrentScanFileName);
            String file = Path.GetFileName(CurrentScanFileName);
            if (!Directory.Exists(dirpath))
            {
                DialogResult result = Message.ShowQuestion("The directory has not exist. Do you want to create?\n    Yes,\t\t create directory.\n    No,\t\t save to default directory.\n    Cancel,\t\t not create and save.", null, MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(dirpath);
                        try { AddDirectorySecurity(dirpath); }
                        catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }

                        TextBox_SaveDirPath.Text = dirpath;
                    }
                    catch (Exception e)
                    {
                        Message.ShowError("Create directroy failed!");
                        DBG.WriteLine(e.Message);
                        logFile.Error(e.Message);
                        return;
                    };
                }
                else if (result == DialogResult.No)
                {
                    try
                    {
                        String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        String defpath = Path.Combine(path, "InnoSpectra\\Scan Results");
                        if (!Directory.Exists(defpath))
                        {
                            Directory.CreateDirectory(defpath);
                            try { AddDirectorySecurity(defpath); }
                            catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }
                        }
                        TextBox_SaveDirPath.Text = defpath;
                        CurrentScanFileName = defpath + "\\" + file;
                    }
                    catch (Exception e)
                    {
                        Message.ShowError("Create directroy failed!");
                        DBG.WriteLine(e.Message);
                        logFile.Error(e.Message);
                        return;
                    };
                }
                else
                {
                    return;
                }
            }

            if (Device.ErrStatus > 0)
                CurrentScanFileName += "_Error_Detected";

            SaveToCSV(CurrentScanFileName + ".csv");
            SaveToJCAMP(CurrentScanFileName + ".jdx");

            if (CheckBox_SaveDAT.Checked == true)
            {
                Scan.SaveScanResultToBinFile(CurrentScanFileName + ".dat", 
                    ReferenceSelect == Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN ? 'N' : Scan.IsLocalRefScanAutoPGA() == 1 ? 'A' : 'F', 
                    CheckBox_AutoGain.Checked ? 'A' : 'F');  // For populating saved scan
                AddFileToSavedScanList(CurrentScanFileName + ".dat");
            }

            if (CheckBox_AverageCSV.Checked)
            {
                SaveToAverageCSV(CurrentScanFileName + ".csv");
            }
        }

        private void SaveToCSV(String FileName)
        {
            if (CheckBox_SaveCombCSV.Checked == true)
            {
                FileStream fs = new FileStream(@FileName, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("Wavelength (nm)" + CSV_Delimiter + "Absorbance (AU)" + CSV_Delimiter + "Reference Signal (unitless)" + CSV_Delimiter + "Sample Signal (unitless)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Absorbance[i] + CSV_Delimiter + Scan.ReferenceIntensity[i] + CSV_Delimiter + Scan.Intensity[i]);
                    }
                }
                if (checkBox_EnableBlackLevelData.Checked)
                {
                    if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4)
                    {
                        byte[] HW_Ver = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
                        int MB_Ver = HW_Ver[0];

                        sw.WriteLine("\n***Scan Black Level Data***");
                        for (int i = 0; i < Scan.BlackLevel.Count; i++)
                        {
                            sw.WriteLine((i + 1) + CSV_Delimiter + Scan.BlackLevel[i]);
                        }
                        sw.WriteLine("\n***Scan Raw ADC Data***");
                        for (int i = 0; i < Scan.RawData.Count; i++)
                        {
                            sw.WriteLine((i + 1) + CSV_Delimiter + Scan.RawData[i]);
                        }

                        if (MB_Ver > 'E' && MB_Ver != 'N' && !(Device.DevInfo.ModelType == "F"))
                        {
                            Device.ReadLampAdcTimeStamp();
                            Device.ReadLampRampUpData();
                            sw.WriteLine("\n***Lamp Ramp Up ADC***");
                            if (Device.DevInfo.ModelType == "R")
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                                else
                                    sw.WriteLine("ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                            }
                            else
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC");
                                else
                                    sw.WriteLine("ADC");
                            }
                            for (int i = 0; i < Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 && Device.LampRampUpADC[i * 4] != 0; i++)
                            {
                                if (Device.DevInfo.ModelType == "R")
                                {
                                    if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                        sw.WriteLine(Device.LampAdcTimeStamp[i] + CSV_Delimiter + Device.LampRampUpADC[i * 4] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 1] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 2] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 3]);
                                    else
                                        sw.WriteLine(Device.LampRampUpADC[i * 4] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 1] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 2] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 3]);
                                }
                                else
                                {
                                    if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                        sw.WriteLine(Device.LampAdcTimeStamp[i] + CSV_Delimiter + Device.LampRampUpADC[i * 4]);
                                    else
                                        sw.WriteLine(Device.LampRampUpADC[i * 4]);
                                }
                            }

                            Device.ReadLampRepeatedScanData();
                            sw.WriteLine("\n***Lamp ADC among repeated times***");
                            if (Device.DevInfo.ModelType == "R")
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                                else
                                    sw.WriteLine("ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                            }
                            else
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC");
                                else
                                    sw.WriteLine("ADC");
                            }
                            for (int i = 0; i < Device.MAX_LAMP_REPEATED_SCAN_ADC_SIZE / 4 && Device.LampRepeatedScanADC[i * 4] != 0; i++)
                            {
                                if (Device.DevInfo.ModelType == "R")
                                {
                                    if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                        sw.WriteLine(Device.LampAdcTimeStamp[Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 + i] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 1] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 2] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 3]);
                                    else
                                        sw.WriteLine(Device.LampRepeatedScanADC[i * 4] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 1] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 2] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 3]);

                                }
                                else
                                {
                                    if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                        sw.WriteLine(Device.LampAdcTimeStamp[Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 + i] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4]);
                                    else
                                        sw.WriteLine(Device.LampRepeatedScanADC[i * 4]);
                                }
                            }

                        }
                    }
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream 
                AddFileToSavedScanList(FileName);
            }

            if (CheckBox_SaveICSV.Checked == true)
            {
                String FileName_i = FileName.Insert(FileName.LastIndexOf(".csv"), "_i");
                FileStream fs = new FileStream(@FileName_i, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("Wavelength (nm)" + CSV_Delimiter + "Sample Signal (unitless)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Intensity[i]);
                    }
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream
            }

            if (CheckBox_SaveACSV.Checked == true)
            {
                String FileName_a = FileName.Insert(FileName.LastIndexOf(".csv"), "_a");
                FileStream fs = new FileStream(@FileName_a, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("Wavelength (nm)" + CSV_Delimiter + "Absorbance (AU)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Absorbance[i]);
                    }
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream
            }

            if (CheckBox_SaveRCSV.Checked == true)
            {
                String FileName_r = FileName.Insert(FileName.LastIndexOf(".csv"), "_r");
                FileStream fs = new FileStream(@FileName_r, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("Wavelength (nm)" + CSV_Delimiter + "Reflectance (unitless)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Reflectance[i]);
                    }
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream
            }

            if (SaveOneCSVFile)
            {
                if (OneScanFileName == String.Empty)
                    OneScanFileName = FileName;

                String FileName_one = OneScanFileName.Insert(OneScanFileName.LastIndexOf("_", OneScanFileName.Length - 20), "_combined");

                try
                {
                    using (FileStream fs = new FileStream(@FileName_one, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                        {
                            if (fs.Length == 0)
                            {
                                SaveHeader(sw, false);

                                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                                {
                                    sw.Write("Wavelength (nm)" + CSV_Delimiter);
                                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                                        sw.Write(Scan.WaveLength[i] + CSV_Delimiter);
                                    sw.Write("\n");

                                    sw.Write("Reference Signal (unitless)" + CSV_Delimiter);
                                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                                        sw.Write(Scan.ReferenceIntensity[i] + CSV_Delimiter);
                                    sw.Write("\n");
                                }
                            }

                            if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                            {
                                sw.Write("Sample Signal (unitless)" + CSV_Delimiter);
                                for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                                    sw.Write(Scan.Intensity[i] + CSV_Delimiter);
                                sw.Write("\n");
                            }
                        }
                    }
                }
                catch
                {
                    ProgressWindowCompleted();

                    ScannedCounts = 0;
                    UserCancelScan = true;
                    OneScanFileName = String.Empty;
                    SaveOneCSVFile = false;
                    Button_Scan.Text = "Scan";
                    Manual_ContScan_UI_Con(false);
                    SDK.IsConnectionChecking = true;
                    MessageBox.Show(this, "Open CSV file for saving failed!\nThe file might be corrupted or openned by other application.", "Save File Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }

                if (TargetScanCounts == ScannedCounts)
                {
                    SaveOneCSVFile = false;
                    OneScanFileName = String.Empty;
                }
            }
        }
        private void SaveToJCAMP(String FileName)
        {
            if (CheckBox_SaveIJDX.Checked == true)
            {
                String FileName_i = FileName.Insert(FileName.LastIndexOf("_", FileName.Length - 20), "_i");
                FileStream fs = new FileStream(@FileName_i, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, true);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("##XUNITS=Wavelength(nm)");
                    sw.WriteLine("##YUNITS=Intensity");
                    sw.WriteLine("##FIRSTX=" + Scan.WaveLength[0]);
                    sw.WriteLine("##LASTX=" + Scan.WaveLength[Scan.ScanDataLen - 1]);
                    sw.WriteLine("##XYPOINTS=(XY..XY)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Intensity[i]);
                    }
                    sw.WriteLine("##END=");
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream 
            }

            if (CheckBox_SaveAJDX.Checked == true)
            {
                String FileName_a = FileName.Insert(FileName.LastIndexOf("_", FileName.Length - 20), "_a");
                FileStream fs = new FileStream(@FileName_a, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, true);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("##XUNITS=Wavelength(nm)");
                    sw.WriteLine("##YUNITS=Absorbance(AU)");
                    sw.WriteLine("##FIRSTX=" + Scan.WaveLength[0]);
                    sw.WriteLine("##LASTX=" + Scan.WaveLength[Scan.ScanDataLen - 1]);
                    sw.WriteLine("##XYPOINTS=(XY..XY)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Absorbance[i]);
                    }
                    sw.WriteLine("##END=");
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream 
            }

            if (CheckBox_SaveRJDX.Checked == true)
            {
                String FileName_r = FileName.Insert(FileName.LastIndexOf("_", FileName.Length - 20), "_r");
                FileStream fs = new FileStream(@FileName_r, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, true);

                if (Device.ErrStatus == 0)  // Skip invalid scan data if error status received
                {
                    sw.WriteLine("##XUNITS=Wavelength(nm)");
                    sw.WriteLine("##YUNITS=Reflectance(unitless)");
                    sw.WriteLine("##FIRSTX=" + Scan.WaveLength[0]);
                    sw.WriteLine("##LASTX=" + Scan.WaveLength[Scan.ScanDataLen - 1]);
                    sw.WriteLine("##XYPOINTS=(XY..XY)");
                    for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                    {
                        sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + Scan.Reflectance[i]);
                    }
                    sw.WriteLine("##END=");
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream 
            }
        }
        private void SaveToAverageCSV(String FileName)
        {
            if (ScannedCounts == 1)
            {
                // Initialization
                AverageScanFileName = FileName;
                for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                {
                    AverageIntensity.Add(Scan.Intensity[i]);
                    AverageAbsorbance.Add(Scan.Absorbance[i]);
                }
                return;
            }

            if (Device.ErrStatus == 0)
            {
                //Average data
                double basePartition, addInPartition;

                basePartition = (double)(ScannedCounts - 1) / ScannedCounts;
                addInPartition = (double)1.0 / ScannedCounts;

                for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                {
                    AverageIntensity[i] = AverageIntensity[i] * basePartition + Scan.Intensity[i] * addInPartition;
                    AverageAbsorbance[i] = Math.Log10(Scan.ReferenceIntensity[i] / AverageIntensity[i]);
                }
            }

            if (Device.ErrStatus != 0 && ScannedCounts == 1) // Do not save average if error detect at first scan
            {
                AverageIntensity.Clear();
                AverageAbsorbance.Clear();
                AverageScanFileName = String.Empty;
                return;
            }

            if (Device.ErrStatus != 0 || (TargetScanCounts - ScannedCounts) == 0 || UserCancelScan)
            {
                if (AverageScanFileName == String.Empty)        //Error Detected and file has been saved
                    return;

                //Save file
                String FileName_average = AverageScanFileName.Insert(AverageScanFileName.LastIndexOf("_", AverageScanFileName.Length - 20), "_average");

                FileStream fs = new FileStream(@FileName_average, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                SaveHeader(sw, false);

                Int32 realCount = Device.ErrStatus == 0 ? ScannedCounts : ScannedCounts - 1;
                sw.WriteLine("Num Scan:," + realCount.ToString() + "\n");

                sw.WriteLine("Wavelength (nm)" + CSV_Delimiter + "Absorbance (AU)" + CSV_Delimiter + "Reference Signal (unitless)" + CSV_Delimiter + "Sample Signal (unitless)");
                for (Int32 i = 0; i < Scan.ScanDataLen; i++)
                {
                    sw.WriteLine(Scan.WaveLength[i] + CSV_Delimiter + AverageAbsorbance[i] + CSV_Delimiter + Scan.ReferenceIntensity[i] + CSV_Delimiter + AverageIntensity[i]);
                }
                if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4 && Device.IsConnected())
                {
                    byte[] HW_Ver = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
                    int MB_Ver = HW_Ver[0];

                    if (MB_Ver > 'E' && MB_Ver != 'N' && !(Device.DevInfo.ModelType == "F"))
                    {
                        Device.ReadLampAdcTimeStamp();
                        Device.ReadLampRampUpData();
                        sw.WriteLine("\n***Lamp Ramp Up ADC***");
                        if (Device.DevInfo.ModelType == "R")
                        {
                            if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                            else
                                sw.WriteLine("ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                        }
                        else
                        {
                            if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC");
                            else
                                sw.WriteLine("ADC");
                        }
                        for (int i = 0; i < Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 && Device.LampRampUpADC[i * 4] != 0; i++)
                        {
                            if (Device.DevInfo.ModelType == "R")
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine(Device.LampAdcTimeStamp[i] + CSV_Delimiter + Device.LampRampUpADC[i * 4] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 1] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 2] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 3]);
                                else
                                    sw.WriteLine(Device.LampRampUpADC[i * 4] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 1] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 2] + CSV_Delimiter + Device.LampRampUpADC[i * 4 + 3]);
                            }
                            else
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine(Device.LampAdcTimeStamp[i] + CSV_Delimiter + Device.LampRampUpADC[i * 4]);
                                else
                                    sw.WriteLine(Device.LampRampUpADC[i * 4]);
                            }
                        }

                        Device.ReadLampRepeatedScanData();
                        sw.WriteLine("\n***Lamp ADC among repeated times***");
                        if (Device.DevInfo.ModelType == "R")
                        {
                            if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                            else
                                sw.WriteLine("ADC0" + CSV_Delimiter + "ADC1" + CSV_Delimiter + "ADC2" + CSV_Delimiter + "ADC3");
                        }
                        else
                        {
                            if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                sw.WriteLine("Timestamp(ms)" + CSV_Delimiter + "ADC");
                            else
                                sw.WriteLine("ADC");
                        }
                        for (int i = 0; i < Device.MAX_LAMP_REPEATED_SCAN_ADC_SIZE / 4 && Device.LampRepeatedScanADC[i * 4] != 0; i++)
                        {
                            if (Device.DevInfo.ModelType == "R")
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine(Device.LampAdcTimeStamp[Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 + i] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 1] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 2] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 3]);
                                else
                                    sw.WriteLine(Device.LampRepeatedScanADC[i * 4] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 1] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 2] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4 + 3]);

                            }
                            else
                            {
                                if (Device.DevInfo.TivaRev[0] >= 2 && Device.DevInfo.TivaRev[1] >= 5)
                                    sw.WriteLine(Device.LampAdcTimeStamp[Device.MAX_LAMP_RAMP_UP_ADC_SIZE / 4 + i] + CSV_Delimiter + Device.LampRepeatedScanADC[i * 4]);
                                else
                                    sw.WriteLine(Device.LampRepeatedScanADC[i * 4]);
                            }
                        }
                    }
                }

                sw.Flush();  // Clear buffer
                sw.Close();  // Close file stream
                AddFileToSavedScanList(FileName_average);

                if (TargetScanCounts - ScannedCounts == 0)
                {
                    AverageScanFileName = String.Empty;
                    AverageIntensity.Clear();
                    AverageAbsorbance.Clear();
                }
            }
        }
        private void SaveHeader(StreamWriter sw, Boolean ifJCAMP, Boolean isFromDatFile = false)
        {
            String PreStr = String.Empty;
            if (ifJCAMP == true)
            {
                PreStr = "##";

                sw.WriteLine("##TITLE=" + Scan.ScanConfigData.head.config_name);
                sw.WriteLine("##JCAMP-DX=4.24");
                sw.WriteLine("##DATA TYPE=INFRARED SPECTRUM");
            }
            else
            {
                PreStr = String.Empty;
            }

            String TivaRev = String.Empty, DLPCRev = String.Empty;
            String HWRev_MB = String.Empty, HWRev_DB = String.Empty;
            Byte MB_Ver = 0;

            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_3 || GetFW_LEVEL() == FW_LEVEL.LEVEL_1)
            {
                TivaRev = Device.DevInfo.TivaRev[0].ToString() + "."
                        + Device.DevInfo.TivaRev[1].ToString() + "."
                        + Device.DevInfo.TivaRev[2].ToString();
            }
            else
            {
                TivaRev = Device.DevInfo.TivaRev[0].ToString() + "."
                        + Device.DevInfo.TivaRev[1].ToString() + "."
                        + Device.DevInfo.TivaRev[2].ToString();
                if (Device.DevInfo.TivaRev[3] != 0)
                    TivaRev += ("." + Device.DevInfo.TivaRev[3].ToString());
            }

            if (Device.DevInfo.DLPCRev.Length == 3)
            {
                DLPCRev = Device.DevInfo.DLPCRev[0].ToString() + "."
                        + Device.DevInfo.DLPCRev[1].ToString() + "."
                        + Device.DevInfo.DLPCRev[2].ToString();
            }
            else
            {
                DLPCRev = Device.DevInfo.DLPCRev[0].ToString() + "."
                        + Device.DevInfo.DLPCRev[1].ToString() + "."
                        + Device.DevInfo.DLPCRev[2].ToString() + "."
                        + Device.DevInfo.DLPCRev[3].ToString();
            }

            if (Device.IsConnected())
                MB_Ver = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev).First();

            String[,] CSV = new String[28, 15];

            // Section information field names
            CSV[0, 0] = "***Scan Config Information***";
            CSV[0, 7] = "***Reference Scan Information***";
            CSV[17, 0] = "***General Information***";
            CSV[17, 7] = "***Calibration Coefficients***";
            CSV[27, 0] = "***Scan Data***";
            // Config field names & values(Scan configuration and Reference scan configuration)
            for (int i = 0; i < 2; i++)
            {
                CSV[1, i * 7] = PreStr + "Scan Config Name:";
                CSV[2, i * 7] = PreStr + "Scan Config Type:";
                CSV[2, i * 7 + 2] = "Num Section:";
                CSV[3, i * 7] = PreStr + "Section Config Type:";
                CSV[4, i * 7] = PreStr + "Start Wavelength (nm):";
                CSV[5, i * 7] = PreStr + "End Wavelength (nm):";
                CSV[6, i * 7] = PreStr + "Pattern Width (nm):";
                CSV[7, i * 7] = PreStr + "Exposure (ms):";
                CSV[8, i * 7] = PreStr + "Digital Resolution:";
                CSV[9, i * 7] = PreStr + "Num Repeats:";
                CSV[10, i * 7] = PreStr + "PGA Gain:";
                CSV[11, i * 7] = PreStr + "System Temp (C):";
                CSV[12, i * 7] = PreStr + "Humidity (%):";
                if (MB_Ver >= 'F')
                    CSV[13, i * 7] = PreStr + "Lamp ADC:";
                else
                    CSV[13, i * 7] = PreStr + "Lamp Indicator:";
                CSV[14, i * 7] = PreStr + "Data Date-Time:";
            }

            if (IsFetchingDeviceInfo && Device.ErrStatus != 0)
            {
                if (Device.ReadSensorsData() == 0)
                {
                    CSV[11, 1] = (Device.DevSensors.HDCTemp != -1) ? (Device.DevSensors.HDCTemp.ToString()) : ("Read Failed!");
                    CSV[12, 1] = (Device.DevSensors.Humidity != -1) ? (Device.DevSensors.Humidity.ToString()) : ("Read Failed!");
                }
                else
                {
                    CSV[11, 1] = "Read Failed!";
                    CSV[12, 1] = "Read Failed!";
                }
            }

            for (int i = 0; i < Scan.ScanConfigData.head.num_sections; i++)
            {
                if (i == 0)
                {
                    // Scan config values
                    CSV[1, 1] = Scan.ScanConfigData.head.config_name;
                    CSV[2, 1] = "Slew";
                    CSV[2, 3] = Scan.ScanConfigData.head.num_sections.ToString();
                    CSV[9, 1] = Scan.ScanConfigData.head.num_repeats.ToString();
                    CSV[10, 1] = Scan.PGA.ToString();
                    if (isFromDatFile)
                    {
                        if (Device.DevInfo.DatFileSamplePGAFlag == 'A')
                            CSV[10, 2] = "(AutoPGA)";
                        else if (Device.DevInfo.DatFileSamplePGAFlag == 'F')
                            CSV[10, 2] = "(FixedPGA)";
                    }
                    else
                        CSV[10, 2] = CheckBox_AutoGain.Checked ? "(AutoPGA)" : "(FixedPGA)"; 
                    CSV[11, 1] = Scan.SensorData[0].ToString();
                    CSV[12, 1] = Scan.SensorData[2].ToString();
                    if (Device.DevInfo.ModelType == "F")
                    {
                        CSV[13, 1] = "No Built-In Lamp";
                    }
                    else
                    {
                        if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4 && MB_Ver >= 'F' && MB_Ver != 'N')
                        {
                            int dataNum = (Scan.ScanConfigData.head.num_repeats > 30) ? 30 : Scan.ScanConfigData.head.num_repeats;
                            double[] lampADC = new double[4];
                            for (int j = 0; j < 4; j++)
                                lampADC[j] = 0;

                            for (int j = 0; j < dataNum; j++)
                            {
                                lampADC[0] += Scan.SensorData[4 + j * 4];
                                lampADC[1] += Scan.SensorData[4 + j * 4 + 1];
                                lampADC[2] += Scan.SensorData[4 + j * 4 + 2];
                                lampADC[3] += Scan.SensorData[4 + j * 4 + 3];
                            }
                            for (int j = 0; j < 4; j++)
                            {
                                lampADC[j] /= dataNum;
                                CSV[13, 1 + j] = String.Format("{0}", ((Device.DevInfo.ModelType != "R" && j > 0) ? "" : lampADC[j].ToString("F0")));
                            }
                        }
                        else
                            CSV[13, 1] = Scan.SensorData[3].ToString();
                    }

                    CSV[14, 1] = String.Format("20{0:00}/{1:00}/{2:00}T{3:00}:{4:00}:{5:00}",
                                               Scan.ScanDateTime[0], Scan.ScanDateTime[1], Scan.ScanDateTime[2],
                                               Scan.ScanDateTime[3], Scan.ScanDateTime[4], Scan.ScanDateTime[5]);

                    if (Scan.ReferenceScanConfigData.head.config_name == "SystemTest")
                        CSV[1, 8] = "Built-in Factory Reference";
                    else if (Scan.ReferenceScanConfigData.head.config_name == "UserReference")
                        CSV[1, 8] = "Built-in User Reference";
                    else
                        CSV[1, 8] = "Local New Reference";
                    CSV[2, 8] = "Slew";
                    CSV[2, 10] = Scan.ReferenceScanConfigData.head.num_sections.ToString();
                    CSV[9, 8] = Scan.ReferenceScanConfigData.head.num_repeats.ToString();
                    CSV[10, 8] = Scan.ReferencePGA.ToString();

                    if (isFromDatFile)
                    {
                        if (Device.DevInfo.DatFileRefPGAFlag == 'A')
                            CSV[10, 9] = "(AutoPGA)";
                        else if (Device.DevInfo.DatFileRefPGAFlag == 'F')
                            CSV[10, 9] = "(FixedPGA)";
                    }
                    else
                    {
                        if (ReferenceSelect != Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN)
                        {
                            int localRefAutoPGAFlag = Scan.IsLocalRefScanAutoPGA();
                            if (localRefAutoPGAFlag == 1)
                                CSV[10, 9] = "(AutoPGA)";
                            else if (localRefAutoPGAFlag == -1)
                                CSV[10, 9] = "(FixedPGA)";
                        }
                    }

                    CSV[11, 8] = Scan.ReferenceSensorData[0].ToString();
                    CSV[12, 8] = Scan.ReferenceSensorData[2].ToString();
                    if (Device.DevInfo.ModelType == "F")
                    {
                        CSV[13, 8] = "No Built-In Lamp";
                    }
                    else
                    {
                        if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4 && MB_Ver >= 'F' && MB_Ver != 'N')
                        {
                            int dataNum = (Scan.ReferenceScanConfigData.head.num_repeats > 30) ? 30 : Scan.ReferenceScanConfigData.head.num_repeats;
                            double[] lampADC = new double[4];
                            for (int j = 0; j < 4; j++)
                                lampADC[j] = 0;

                            for (int j = 0; j < dataNum; j++)
                            {
                                lampADC[0] += Scan.ReferenceSensorData[4 + j * 4];
                                lampADC[1] += Scan.ReferenceSensorData[4 + j * 4 + 1];
                                lampADC[2] += Scan.ReferenceSensorData[4 + j * 4 + 2];
                                lampADC[3] += Scan.ReferenceSensorData[4 + j * 4 + 3];
                            }
                            for (int j = 0; j < 4; j++)
                            {
                                lampADC[j] /= dataNum;
                                CSV[13, 8 + j] = String.Format("{0}", ((Device.DevInfo.ModelType != "R" && j > 0) ? "" : lampADC[j].ToString("F0")));
                            }
                        }
                        else
                            CSV[13, 8] = Scan.ReferenceSensorData[3].ToString();
                    }

                    CSV[14, 8] = String.Format("20{0:00}/{1:00}/{2:00}T{3:00}:{4:00}:{5:00}",
                                               Scan.ReferenceScanDateTime[0], Scan.ReferenceScanDateTime[1], Scan.ReferenceScanDateTime[2],
                                               Scan.ReferenceScanDateTime[3], Scan.ReferenceScanDateTime[4], Scan.ReferenceScanDateTime[5]);
                }
                CSV[3, i + 1] = Helper.ScanTypeIndexToMode(Scan.ScanConfigData.section[i].section_scan_type);
                CSV[4, i + 1] = Scan.ScanConfigData.section[i].wavelength_start_nm.ToString();
                CSV[5, i + 1] = Scan.ScanConfigData.section[i].wavelength_end_nm.ToString();
                CSV[6, i + 1] = Math.Round(Helper.CfgWidthPixelToNM(Scan.ScanConfigData.section[i].width_px), 2).ToString();
                CSV[7, i + 1] = Helper.CfgExpIndexToTime(Scan.ScanConfigData.section[i].exposure_time).ToString();
                CSV[8, i + 1] = Scan.ScanConfigData.section[i].num_patterns.ToString();

                // Reference config section values
                if (i < Scan.ReferenceScanConfigData.head.num_sections)
                {
                    CSV[3, i + 8] = Helper.ScanTypeIndexToMode(Scan.ReferenceScanConfigData.section[i].section_scan_type);
                    CSV[4, i + 8] = Scan.ReferenceScanConfigData.section[i].wavelength_start_nm.ToString();
                    CSV[5, i + 8] = Scan.ReferenceScanConfigData.section[i].wavelength_end_nm.ToString();
                    CSV[6, i + 8] = Math.Round(Helper.CfgWidthPixelToNM(Scan.ReferenceScanConfigData.section[i].width_px), 2).ToString();
                    CSV[7, i + 8] = Helper.CfgExpIndexToTime(Scan.ReferenceScanConfigData.section[i].exposure_time).ToString();
                    CSV[8, i + 8] = Scan.ReferenceScanConfigData.section[i].num_patterns.ToString();
                }
            }

            // Measure Time field name & value
            if (!isFromDatFile)
            {
                CSV[15, 0] = PreStr + "Total Measurement Time in sec:";
                TimeSpan ts = new TimeSpan(TimeScanEnd.Ticks - TimeScanStart.Ticks);
                CSV[15, 1] = ts.TotalSeconds.ToString();
            }

            // Coefficients filed names & valus
            CSV[18, 7] = PreStr + "Shift Vector Coefficients:";
            CSV[18, 8] = Device.Calib_Coeffs.ShiftVectorCoeffs[0].ToString();
            CSV[18, 9] = Device.Calib_Coeffs.ShiftVectorCoeffs[1].ToString();
            CSV[18, 10] = Device.Calib_Coeffs.ShiftVectorCoeffs[2].ToString();
            CSV[19, 7] = PreStr + "Pixel to Wavelength Coefficients:";
            CSV[19, 8] = Device.Calib_Coeffs.PixelToWavelengthCoeffs[0].ToString();
            CSV[19, 9] = Device.Calib_Coeffs.PixelToWavelengthCoeffs[1].ToString();
            CSV[19, 10] = Device.Calib_Coeffs.PixelToWavelengthCoeffs[2].ToString();

            // General information field names & values
            CSV[18, 0] = PreStr + "Model Name:";
            CSV[18, 1] = Device.DevInfo.ModelName;
            CSV[19, 0] = PreStr + "Serial Number:";
            CSV[19, 1] = !String.IsNullOrEmpty(Scan.ScanSerialNumber) ? Scan.ScanSerialNumber : Device.DevInfo.SerialNumber;
            CSV[19, 2] = "(" + ((Device.DevInfo.Manufacturing_SerialNumber.Contains("70UB1") || Device.DevInfo.Manufacturing_SerialNumber.Contains("95UB1")) ? Device.DevInfo.Manufacturing_SerialNumber : "N/A") + ")";
            CSV[20, 0] = PreStr + "GUI Version:";
            CSV[20, 2] = PreStr + "Revision:";
            CSV[20, 3] = PreStr + this.Revision;
            String GUIRev = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GUIRev = GUIRev.Substring(0, GUIRev.LastIndexOf('.'));
            CSV[20, 1] = GUIRev;
            CSV[20, 7] = PreStr + "Versions (Cal/Ref/Cfg):";
            CSV[20, 8] = Device.DevInfo.CalRev.ToString();
            CSV[20, 9] = Device.DevInfo.RefCalRev.ToString();
            CSV[20, 10] = Device.DevInfo.CfgRev.ToString();
            CSV[21, 0] = PreStr + "TIVA Version:";
            CSV[21, 1] = TivaRev;
            CSV[21, 7] = "***Lamp Usage ***";
            CSV[22, 0] = PreStr + "DLPC Version:";
            CSV[22, 1] = DLPCRev;
            CSV[22, 7] = PreStr + "Total Time(hh:mm:ss):";
            String Lamp_Usage = "";
            if (Device.ReadLampUsage() == 0)
                Lamp_Usage = GetLampUsage();
            else
                Lamp_Usage = "N/A";
            CSV[22, 8] = Lamp_Usage;
            CSV[23, 0] = PreStr + "UUID:";
            CSV[23, 1] = BitConverter.ToString(Device.DevInfo.DeviceUUID).Replace("-", ":");
            CSV[23, 7] = "***Device/Error/Activation Status***";
            CSV[24, 0] = PreStr + "Main Board Version:";
            CSV[24, 1] = ((!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : "N/A");
            CSV[24, 7] = "Device Status:";
            CSV[24, 8] = "0x" + Device.DeviceStatus.ToString("X8");
            CSV[24, 9] = "Activation Status:";
            CSV[24, 10] = (IsActivated ? "Activated" : "Not activated");
            CSV[25, 0] = PreStr + "Detector Board Version:";
            CSV[25, 1] = ((!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(4, 1) : "N/A");
            CSV[25, 7] = "Error status:";
            CSV[25, 8] = "0x" + Device.ErrStatus.ToString("X8");
            CSV[25, 9] = "Error Code:";
            string errCode = "";
            for (int i = 0; i < 16; i++)
                errCode += Device.ErrCode[i].ToString("X2");
            CSV[25, 10] = "0x" + errCode;
            CSV[26, 9] = "Error Details:";
            if (Device.ErrStatus > 0)
                CSV[26, 10] = ErrMsg;
            else
                CSV[26, 10] = "Not found";
            string buf = "";
            for (int i = 0; i < 28; i++)
            {
                buf = "";
                for (int j = 0; j < 15; j++)
                    buf += (CSV[i, j] + CSV_Delimiter);
                sw.WriteLine(buf);
            }

            if (ifJCAMP)
            {
                UInt16 TotalScanPtns = 0;

                for (int i = 0; i < Scan.ScanConfigData.head.num_sections; i++)
                    TotalScanPtns += Scan.ScanConfigData.section[i].num_patterns;

                sw.WriteLine("##NPOINTS=" + TotalScanPtns);
            }
        }
        #endregion
        #region startScan
        private void Button_Scan_Click(object sender, EventArgs e)
        {
            UserCancelScan = false;  // Clear this flag before scanning
            SDK.IsConnectionChecking = false;
            if (Button_Scan.Text == "Scan")
                ScanErrorCounts = 0;

            if (NewConfig == true || EditConfig == true)
            {
                EditConfig = false;
                NewConfig = false;
                Button_CfgCancel_Click(this, e);
            }

            if (Device.IsConnected())
            {
                Button_ClearAllErrors_Click(this, null); // Clear previous scan error
                if (Button_Scan.Text == "Continuous" || Button_Scan.Text == "Scan Next")
                {
                    button_ExitCont.Visible = false;
                    tabScanPage.TabPages[0].Enabled = true;
                    tabScanPage.TabPages[1].Enabled = true;
                    tabControl_MainFunctions.TabPages[1].Enabled = true;
                }
                else
                {
                    TargetScanCounts = int.Parse(Text_ContScan.Text); // Set the target scan number
                }

                if (Text_ContScan.Text == "1" && Button_Scan.Text != "Scan Next")
                {
                    CheckBox_SaveOneCSV.Checked = false;
                    CheckBox_AverageCSV.Checked = false;
                }

                if (CheckBox_SaveOneCSV.Checked)
                    SaveOneCSVFile = true;

                if (RadioButton_RefNew.Checked || TargetScanCounts == 0) TargetScanCounts = 1;
                Text_ContScan.Text = (TargetScanCounts - ScannedCounts).ToString();

                if (CheckBox_AutoGain.Checked == false)
                {
                    if (GetFW_LEVEL() < FW_LEVEL.LEVEL_2 && CheckBox_LampOn.Checked == true)
                        Scan.SetPGAGain(GetPGA());
                    else
                        Scan.SetFixedPGAGain(true, GetPGA());
                }
                else
                {
                    if (GetFW_LEVEL() < FW_LEVEL.LEVEL_2)
                        Scan.SetFixedPGAGain(false, GetPGA());
                    else
                        Scan.SetFixedPGAGain(true, 0); // This is set to auto PGA
                }

                if (bwScan.IsBusy != true)
                    bwScan.RunWorkerAsync();
                else
                {
                    String text = "Scanning in progress... \r\nPlease Wait!";
                    MessageBox.Show(text, "Wait");
                }
                Label_ContScan.Text = string.Empty;
                if (!Check_Overlay.Checked && !checkBox_zoom.Checked)
                {
                    Chart_Refresh();
                }
            }
            else
            {
                String text = "Please connect a device before performing scan!";
                MessageBox.Show(text, "Warning");
            }
        }
        private void button_ExitCont_Click(object sender, EventArgs e)
        {
            OneScanFileName = String.Empty;
            AverageScanFileName = String.Empty;
            AverageIntensity.Clear();
            AverageAbsorbance.Clear();

            if (TargetScanCounts > 1)
            {
                string msg = string.Format("Continuous Scan Terminated.\n\nSuccess: {0}\nFailed: {1}", ScannedCounts - ScanErrorCounts, ScanErrorCounts);
                MessageBox.Show(msg, "Scan Completed", MessageBoxButtons.OK);
            }

            ScannedCounts = 0;
            TargetScanCounts = 1;
            Text_ContScan.Text = TargetScanCounts.ToString();
            Label_ContScan.Text = String.Empty;
            Button_Scan.Text = "Scan";
            Manual_ContScan_UI_Con(false);
            button_ClearPlots.Enabled = true;

            tabScanPage.TabPages[0].Enabled = true;
            tabScanPage.TabPages[1].Enabled = true;
            tabControl_MainFunctions.TabPages[1].Enabled = true;
            button_ExitCont.Visible = false;
        }
        private byte GetPGA()
        {
            byte pga = 64;
            if (ComboBox_PGAGain.Text.ToString().Equals("64"))
            {
                pga = 64;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("32"))
            {
                pga = 32;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("16"))
            {
                pga = 16;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("8"))
            {
                pga = 8;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("4"))
            {
                pga = 4;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("2"))
            {
                pga = 2;
            }
            else if (ComboBox_PGAGain.Text.ToString().Equals("1"))
            {
                pga = 1;
            }
            return pga;
        }

        private void bwScan_DoScan(object sender, DoWorkEventArgs e)
        {
            List<object> arguments = new List<object>();
            int result = 0;

            if (RadioButton_LampStableTime.Checked && TextBox_LampStableTime.Text != "625")
            {
                result = Scan.SetLampDelay(UInt32.Parse(TextBox_LampStableTime.Text));
                if (result < 0)
                    result = -10;
                DBG.WriteLine($"Set lamp stable time before scan: {TextBox_LampStableTime.Text}ms");
                logFile.InfoFormat($"Set lamp stable time before scan: {TextBox_LampStableTime.Text}ms");
            }

            if (result == 0)
            {
                DBG.WriteLine("Performing scan... Remained scans: {0}", TargetScanCounts - ScannedCounts);
                logFile.InfoFormat("Performing scan... Remained scans: {0}", TargetScanCounts - ScannedCounts);
                TimeScanStart = DateTime.Now;
                IsSavedScanData = false;

                if ((result = Scan.PerformScan(ReferenceSelect)) == 0)
                {
                    DBG.WriteLine("Scan completed!");
                    logFile.Info("Scan completed!");
                    TimeScanEnd = DateTime.Now;
                    TimeSpan ts = new TimeSpan(TimeScanEnd.Ticks - TimeScanStart.Ticks);

                    arguments.Add(result);
                    arguments.Add(ts);
                }
                else
                {
                    arguments.Add(result);
                    arguments.Add(TimeSpan.Zero);
                }
            }
            else
            {
                arguments.Add(result);
                arguments.Add(TimeSpan.Zero);
            }
            e.Result = arguments;

            Thread.Sleep(200);
        }

        private void bwScan_DoSacnCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<object> arguments = e.Result as List<object>;
            int result = (int)arguments[0];
            TimeSpan ts = (TimeSpan)arguments[1];

            Byte pga = Scan.PGA;  // If PGA is auto, it can only read the current value after scanning.
            RefreshErrorStatus();
            UpdateLampUsage();

            ScannedCounts++;

            if (result == SDK.RETURN_PASS)
            {
                if (tabScanPage.SelectedIndex == 2)
                    IsSavedScanData = false;

                ComboBox_PGAGain.SelectedItem = pga.ToString();
                Label_ScanStatus.Text = "Total Scan Time: " + String.Format("{0:0.000}", ts.TotalSeconds) + " secs.";

                if (ReferenceSelect == Scan.SCAN_REF_TYPE.SCAN_REF_NEW)  // Save scan results except new reference selection
                {
                    isPrevScanReference = isScanReference;
                    isScanReference = true;
                }
                else
                {
                    SaveToFiles();
                    isPrevScanReference = isScanReference;
                    isScanReference = false;
                }

                if (isScanReference && Scan.IsLocalRefExist)
                {
                    Byte[] time = Scan.ReferenceScanDateTime;
                    if (time[0] != 0)
                    {
                        pre_ref_time = "Previous reference was set: 20" + time[0].ToString() + "/" + time[1].ToString() + "/" + time[2].ToString()
                        + " T " + time[3].ToString() + ":" + time[4].ToString() + ":" + time[5].ToString();
                        int localRefAutoPGAFlag = Scan.IsLocalRefScanAutoPGA();
                        if (localRefAutoPGAFlag == 1)
                            pre_ref_time += " (AutoPGA)";
                        else if (localRefAutoPGAFlag == -1)
                            pre_ref_time += " (FixedPGA)";
                    }
                }

                if (TargetScanCounts - ScannedCounts == 0)
                    Text_ContScan.Text = "1";
                else
                    Text_ContScan.Text = (TargetScanCounts - ScannedCounts).ToString();
                Label_ContScan.Text = "(" + ScannedCounts.ToString() + "/" + TargetScanCounts.ToString() + ")";

                if (!(Scan.IsLocalRefExist && ReferenceSelect == Scan.SCAN_REF_TYPE.SCAN_REF_NEW))
                    Scan.GetScanResult(IsSavedScanData);

                if (TargetScanCounts == 1 || Device.ErrStatus == 0)
                {

                    if (isPrevScanReference)
                        Clear_Chart(true);

                    SpectrumPlot();

                    if (isScanReference)
                        Scan.ClearData();
                }

                if (TargetScanCounts > 1 && Device.ErrStatus != 0)
                {
                    Device.ResetErrorStatus();
                    ScanErrorCounts++;
                }

                if ((TargetScanCounts - ScannedCounts) > 0 && checkBox_StopOnError.Checked && Device.ErrStatus != 0)
                {
                    UserCancelScan = true;
                    SDK.IsConnectionChecking = true;
                    ProgressWindowCompleted();

                    Message.ShowError("Scan error found.\n\nStopped continuous scan!");

                    if (TargetScanCounts > 1)
                    {
                        ScanErrorCounts = 1;
                        string msg = string.Format("Continuous Scan Terminated.\n\nSuccess: {0}\nFailed: {1}", ScannedCounts - ScanErrorCounts, ScanErrorCounts);
                        MessageBox.Show(msg, "Scan Completed", MessageBoxButtons.OK);
                    }

                    ScannedCounts = 0;
                    Button_Scan.Text = "Scan";
                    button_ExitCont.Visible = false;
                    Manual_ContScan_UI_Con(false);
                    button_ClearPlots.Enabled = true;
                    Text_ContScan.Text = (TargetScanCounts - ScannedCounts).ToString();
                    Label_ContScan.Text = "(" + ScannedCounts.ToString() + "/" + TargetScanCounts.ToString() + ")";
                }
                else if ((TargetScanCounts - ScannedCounts) > 0 && !UserCancelScan)
                {
                    if (checkBox_AutoScan.Checked)
                    {
                        if (int.TryParse(Text_ContDelay.Text, out int DelaySec) == false)
                            DelaySec = 0;

                        DateTime ScanCurrent = DateTime.Now;
                        while (DateTime.Now < ScanCurrent.AddSeconds(DelaySec))
                        {
                            Application.DoEvents();
                        }

                        ProgressWindowContentUpdate("Continuous scan in progress... \r\nPlease Wait!");
                        bwScan.RunWorkerAsync();
                    }
                    else
                    {
                        Button_Scan.Text = "Scan Next";
                        button_ExitCont.Visible = true;
                        ProgressWindowCompleted();
                    }
                }
                else
                {
                    UserCancelScan = true;
                    SDK.IsConnectionChecking = true;
                    ProgressWindowCompleted();

                    if ((TargetScanCounts - ScannedCounts) > 0)
                    {
                        PBW.TopMost = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Activate();
                            this.TopMost = true;
                            this.BringToFront();
                            this.TopMost = false;
                        }));
                        DialogResult response = Message.ShowQuestion("Continuous Scan paused\nWould you want to keep going on continuous scan?", "Continuous Scan Stopped");
                        PBW.TopMost = true;
                        if (response == DialogResult.Yes)
                        {
                            Button_Scan.Text = "Continuous";

                            tabScanPage.TabPages[0].Enabled = false;
                            tabScanPage.TabPages[1].Enabled = false;
                            tabControl_MainFunctions.TabPages[1].Enabled = false;
                            button_ExitCont.Visible = true;
                        }
                        else
                        {
                            if (TargetScanCounts > 1)
                            {
                                string msg = string.Format("Continuous Scan Terminated.\n\nSuccess: {0}\nFailed: {1}", ScannedCounts - ScanErrorCounts, ScanErrorCounts);
                                MessageBox.Show(msg, "Scan Completed", MessageBoxButtons.OK);
                            }

                            OneScanFileName = String.Empty;
                            AverageScanFileName = String.Empty;
                            AverageIntensity.Clear();
                            AverageAbsorbance.Clear();

                            ScannedCounts = 0;
                            TargetScanCounts = 1;
                            Text_ContScan.Text = TargetScanCounts.ToString();
                            Label_ContScan.Text = String.Empty;
                            Button_Scan.Text = "Scan";
                            button_ExitCont.Visible = false;
                            Manual_ContScan_UI_Con(false);
                            button_ClearPlots.Enabled = true;
                        }
                    }
                    else
                    {
                        if (TargetScanCounts > 1)
                        {
                            string msg = string.Format("Continuous Scan Finished.\n\nSuccess: {0}\nFailed: {1}", TargetScanCounts - ScanErrorCounts, ScanErrorCounts);
                            MessageBox.Show(msg, "Scan Completed", MessageBoxButtons.OK);
                        }
                        ScannedCounts = 0;
                        Button_Scan.Text = "Scan";
                        Manual_ContScan_UI_Con(false);
                        button_ClearPlots.Enabled = true;
                    }
                }
            }
            else
            {
                UserCancelScan = true;
                ProgressWindowCompleted();
                SDK.IsConnectionChecking = true;

                ScannedCounts = 0;
                Button_Scan.Text = "Scan";
                Manual_ContScan_UI_Con(false);
                Text_ContScan.Text = (TargetScanCounts - ScannedCounts).ToString();
                Label_ContScan.Text = "(" + ScannedCounts.ToString() + "/" + TargetScanCounts.ToString() + ")";

                switch (result)
                {
                    case -1:
                        Message.ShowError("Scan failed!");
                        break;
                    case -2:
                        Message.ShowError("The scan data is invalid and cannot be interpreted!");
                        break;
                    case -3:
                        Message.ShowError("Insufficient memory!");
                        break;
                    case -4:
                        Message.ShowError("The scan data format is not TPL format!");
                        break;
                    case -5:
                        Message.ShowError("The config of scan data is invalid!");
                        break;
                    case -6:
                        Message.ShowError("The data pointer in the interpretation is NULL!");
                        break;
                    case -7:
                        Message.ShowError("The hardware (U21 ~ U28) may be damaged!");
                        break;
                    case -8:
                        Message.ShowError("Get scan data failed!");
                        break;
                    case -9:
                        Message.ShowError("Set PGA failed!");
                        break;
                    case -10:
                        Message.ShowError("Set lamp delay time failed!");
                        break;
                    default:
                        break;
                }
            }

            if (Scan.IsLocalRefExist && !RadioButton_RefFac.Checked)
            {
                RadioButton_RefPre.Enabled = true;
                RadioButton_RefPre.Checked = true;
            }
        }
        #endregion
        //------------------------------------------------------------------------------------
        #region Utility
        #region Model Name
        private void Button_ModelNameGet_Click(object sender, EventArgs e)
        {
            StringBuilder pOutBuf = new StringBuilder(128);

            if (Device.ReadModelName(pOutBuf) == 0)
                TextBox_ModelName.Text = pOutBuf.ToString();
            else
                TextBox_ModelName.Text = "Read Failed!";

            if (TextBox_BLE_Display_Name.Text != "")
                Button_Get_BLE_Display_Name_Click(null, null);

            pOutBuf.Clear();

            if (++ModelNameGet_Click_Counts > 6)
            {
                this.Button_ModelNameSet.Enabled = true;
                this.Button_ModelNameSet.Visible = true;
            }
        }

        private void Button_ModelNameSet_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(TextBox_ModelName.Text))
            {
                Message.ShowError("Invalid empty input will affect BLE advertising name!");
                return;
            }
            else if (TextBox_ModelName.Text.Substring(0, 4) != "NIR-")
            {
                Message.ShowError("Formatting error will affect the BLE advertising name, please enter a string starting with \"NIR-\"!");
                return;
            }

            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Model Name", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);

                if (Device.SetModelName(Helper.CheckRegex(TextBox_ModelName.Text.PadLeft(16, '\0'))) == 0)
                {
                    if (Device.Information() != 0)
                    {
                        DBG.WriteLine("Device Information read failed!");
                        logFile.Error("Device Information read failed!");
                    }
                    GetDeviceInfo();
                    UpdateDeviceStatusToolTip();
                    if (!String.IsNullOrEmpty(Device.DevInfo.ModelName))
                        TextBox_ModelName.Text = Device.DevInfo.ModelName;
                    else
                        TextBox_ModelName.Text = "Read Failed!";

                    CheckLampFuncUseful();
                }
                else
                    TextBox_ModelName.Text = "Write Failed!";

                SystemBusy(false);
            }
            if (TextBox_BLE_Display_Name.Text != "")
                Button_Get_BLE_Display_Name_Click(null, null);

            this.Button_ModelNameSet.Enabled = false;
            this.Button_ModelNameSet.Visible = false;
        }
        #endregion
        #region Serial Number
        //Serial Number
        private void Button_SerialNumberSet_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(TextBox_SerialNumber.Text))
            {
                Message.ShowError("Invalid empty input will affect BLE advertising name!");
                return;
            }

            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Serial Number", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);

                if (Device.SetSerialNumber(Helper.CheckRegex(TextBox_SerialNumber.Text.PadLeft(8, '\0'))) == 0)
                {
                    if (Device.Information() != 0)
                    {
                        DBG.WriteLine("Device Information read failed!");
                        logFile.Error("Device Information read failed!");
                    }
                    GetDeviceInfo();
                    UpdateDeviceStatusToolTip();
                    if (!String.IsNullOrEmpty(Device.DevInfo.SerialNumber))
                        TextBox_SerialNumber.Text = Device.DevInfo.SerialNumber;
                    else
                        TextBox_SerialNumber.Text = "Read Failed!";
                }
                else
                    TextBox_SerialNumber.Text = "Write Failed!";

                SystemBusy(false);
            }
            if (TextBox_BLE_Display_Name.Text != "")
                Button_Get_BLE_Display_Name_Click(null, null);

            this.Button_SerialNumberSet.Enabled = false;
            this.Button_SerialNumberSet.Visible = false;
        }

        private void Button_SerialNumberGet_Click(object sender, EventArgs e)
        {
            StringBuilder pOutBuf = new StringBuilder(128);

            if (Device.GetSerialNumber(pOutBuf) == 0)
                TextBox_SerialNumber.Text = pOutBuf.ToString();
            else
                TextBox_SerialNumber.Text = "Read Failed!";

            if (TextBox_BLE_Display_Name.Text != "")
                Button_Get_BLE_Display_Name_Click(null, null);

            pOutBuf.Clear();

            if(++SerialNumberGet_Click_Counts > 6)
            {
                this.Button_SerialNumberSet.Enabled = true;
                this.Button_SerialNumberSet.Visible = true;
            }
        }
        #endregion
        #region Date and Time
        //Date and Time
        private void Button_DateTimeSync_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to sync. it?", "Date and Time", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);
                Device.DeviceDateTime DevDateTime = new Device.DeviceDateTime();
                DateTime Current = DateTime.Now;

                DevDateTime.Year = Current.Year;
                DevDateTime.Month = Current.Month;
                DevDateTime.Day = Current.Day;
                DevDateTime.DayOfWeek = (Int32)Current.DayOfWeek;
                DevDateTime.Hour = Current.Hour;
                DevDateTime.Minute = Current.Minute;
                DevDateTime.Second = Current.Second;

                if (Device.SetDateTime(DevDateTime) == 0)
                    TextBox_DateTime.Text = Current.ToString("yyyy/M/d  H:m:s");
                else
                    TextBox_DateTime.Text = "Sync Failed!";
                SystemBusy(false);
            }
        }

        private void Button_DateTimeGet_Click(object sender, EventArgs e)
        {
            if (Device.GetDateTime() == 0)
            {
                TextBox_DateTime.Text = Device.DevDateTime.Year + "/"
                                      + Device.DevDateTime.Month + "/"
                                      + Device.DevDateTime.Day + "  "
                                      + Device.DevDateTime.Hour + ":"
                                      + Device.DevDateTime.Minute + ":"
                                      + Device.DevDateTime.Second;
            }
            else
                TextBox_DateTime.Text = "Get Failed!";
        }
        #endregion
        #region Lamp Usage
        //Lamp Usage
        private String GetLampUsage()
        {
            String lampusage = "";
            UInt64 buf = Device.LampUsage / 1000;

            if (buf / 86400 != 0)
            {
                lampusage += buf / 86400 + "day ";
                buf -= 86400 * (buf / 86400);
            }
            if (buf / 3600 != 0)
            {
                lampusage += buf / 3600 + "hr ";
                buf -= 3600 * (buf / 3600);
            }
            if (buf / 60 != 0)
            {
                lampusage += buf / 60 + "min ";
                buf -= 60 * (buf / 60);
            }
            lampusage += buf + "sec ";
            return lampusage;
        }

        private void Button_LampUsageSet_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Lamp Usage", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);
                if (Double.TryParse(TextBox_LampUsage.Text, out Double LampUsage) == false)
                {
                    TextBox_LampUsage.Text = "Not Numeric!";
                    return;
                }

                if (Device.WriteLampUsage((UInt64)(LampUsage * 3600000)) == 0)  // hour to milliseconds
                    Button_LampUsageGet_Click(sender, e);
                else
                    TextBox_LampUsage.Text = "Write Failed!";
                GetDeviceInfo();
                SystemBusy(false);
            }
        }

        private void Button_LampUsageGet_Click(object sender, EventArgs e)
        {
            if (Device.ReadLampUsage() == 0)
                TextBox_LampUsage.Text = ((Double)Device.LampUsage / 3600000).ToString();  // milliseconds to hour
            else
                TextBox_LampUsage.Text = "Read Failed!";
        }
        #endregion
        #region Sensors
        //Sensors
        private void Button_SensorRead_Click(object sender, EventArgs e)
        {
            SystemBusy(true);
            CheckLampFuncUseful();
            Label_SensorLampVM1Value.Text = String.Empty;
            Label_SensorLampCM1Value.Text = String.Empty;
            Label_SensorLampVM2Value.Text = String.Empty;
            Label_SensorLampCM2Value.Text = String.Empty;

            if (Device.ReadSensorsData() == 0)
            {
                Label_SensorBattStatus.Text = Device.DevSensors.BattStatus;
                Label_SensorBattCapacity.Text = (Device.DevSensors.BattCapicity != -1) ? (Device.DevSensors.BattCapicity.ToString() + " %") : ("Read Failed!");
                Label_SensorHumidity.Text = (Device.DevSensors.Humidity != -1) ? (Device.DevSensors.Humidity.ToString() + " %") : ("Read Failed!");
                Label_SensorSysTemp.Text = (Device.DevSensors.HDCTemp != -1) ? (Device.DevSensors.HDCTemp.ToString() + " C") : ("Read Failed!");
                Label_SensorTivaTemp.Text = (Device.DevSensors.TivaTemp != -1) ? (Device.DevSensors.TivaTemp.ToString() + " C") : ("Read Failed!");
                Label_SensorLampVM1Value.Text = (Device.DevSensors.PhotoDetector != -1) ? (Device.DevSensors.PhotoDetector.ToString()) : ("Read Failed!");
            }
            else
            {
                Label_SensorBattStatus.Text = "Read Failed!";
                Label_SensorBattCapacity.Text = "Read Failed!";
                Label_SensorHumidity.Text = "Read Failed!";
                Label_SensorSysTemp.Text = "Read Failed!";
                Label_SensorTivaTemp.Text = "Read Failed!";
                Label_SensorLampVM1Value.Text = "Read Failed!";
            }

            // Convert main board version to ASCII
            Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
            Int32 MB_Ver = HWRev[0];

            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4 && Device.DevInfo.ModelType != "F")
            {
                Scan.SetLamp(Scan.LAMP_CONTROL.ON_SCAN);
                Thread.Sleep(625); // Wait for lamp stable

                if (Device.ReadLampParam() == SDK.RETURN_PASS)
                {
                    if (MB_Ver >= 'F' && MB_Ver < 'N' && GetFW_LEVEL() >= FW_LEVEL.LEVEL_4)  // STD Version
                    {
                        Label_SensorLampVM1Value.Text = String.Format("{0:0.00} V", (Double)Device.LampADC[0] / 4096 * 3.3 * 2);
                        Label_SensorLampCM1Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[2] / 4096 * 3.3 / 50 / 0.1 * 1000);
                        if (Device.DevInfo.ModelType == "R")
                        {
                            Label_SensorLampVM2Value.Text = String.Format("{0:0.00} V", (Double)Device.LampADC[1] / 4096 * 3.3 * 2);
                            Label_SensorLampCM2Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[3] / 4096 * 3.3 / 50 / 0.1 * 1000);
                        }
                    }
                    else if (MB_Ver >= 'O' && GetFW_LEVEL() >= FW_LEVEL.LEVEL_5)  // EXT Version
                    {
                        Label_SensorLampVM1Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[0] / 4096 * 3.3 / 50 / 0.1 * 1000);
                        if (Device.DevInfo.ModelType == "R")
                        {
                            Label_SensorLampCM1Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[1] / 4096 * 3.3 / 50 / 0.1 * 1000);
                            Label_SensorLampVM2Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[2] / 4096 * 3.3 / 50 / 0.1 * 1000);
                            Label_SensorLampCM2Value.Text = String.Format("{0:0.00} mA", (Double)Device.LampADC[3] / 4096 * 3.3 / 50 / 0.1 * 1000);
                        }
                    }
                    else  // HW Version <= E or HW Version = N
                    {
                        Label_SensorLampVM1Value.Text = String.Format("{0}", Device.LampADC[0]);
                    }
                }
                else
                {
                    Label_SensorLampVM1Value.Text = "Read Failed!";
                    Label_SensorLampCM1Value.Text = "Read Failed!";
                    Label_SensorLampVM2Value.Text = "Read Failed!";
                    Label_SensorLampCM2Value.Text = "Read Failed!";
                }

                Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
            }
            SystemBusy(false);
        }
        #endregion
        #region Tiva FW update
        //Tiva FW update
        private UInt16 MAIN_USB_PID = 0x4200;
        private UInt16 MOTOR_USB_PID = 0x4210;

        private void Button_TivaFWBrowse_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = (Tiva_FWDir == String.Empty) ? (Directory.GetCurrentDirectory()) : (Tiva_FWDir),
                FileName = "",                  // Default file name
                DefaultExt = ".bin",            // Default file extension
                Filter = "Binary File|*.bin"    // Filter files by extension
            };

            // Show open file dialog box
            dlg.ShowDialog();
            // Process open file dialog box results
            if (dlg.FileName != "")
            {
                TextBox_TivaFWPath.Text = dlg.FileName;
                Tiva_FWDir = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf("\\"));
                ControlSingleControl(Button_TivaFWUpdate, true);
            }
        }
        private void Button_TivaFWUpdate_Click(object sender, EventArgs e)
        {
            if (((Device.IsConnected() && Device.Get_USB_Handler() == MAIN_USB_PID))
                && File.Exists(TextBox_TivaFWPath.Text))
            {
                UI_no_connection();
                SDK.AutoSearch = false;
                SDK.IsEnableNotify = false;
                SDK.IsConnectionChecking = false;

                int Ret = SDK.RETURN_PASS;
                int retry = 0;
                TimerCallback callback = new TimerCallback(TimerTask);
                TivaUpdateTime = 1;
                timer = new System.Threading.Timer(callback, null, 1000, 1000);

                ProgressBar_TivaFWUpdateStatus.Value = 10;
                if (Device.ReadDeviceStatus() == 0 && (Device.DeviceStatus & 0x00000001) == 1 && (Device.DeviceStatus & 0x00000002) == 0)
                    Device.Set_Tiva_To_Bootloader();

                while (!Device.IsDFUConnected())
                {
                    if (++retry > 50)
                    {
                        Ret = SDK.RETURN_FAIL;
                        break;
                    }
                    Thread.Sleep(100);
                }

                if (Ret == SDK.RETURN_PASS)
                {
                    bwTivaUpdate.RunWorkerAsync();
                }
                else
                {
                    SDK.AutoSearch = true;
                    SDK.IsEnableNotify = true;
                    MessageBox.Show("Can not find \"Tiva DFU\"!", "Error");
                    SDK.IsConnectionChecking = true;
                    ProgressBar_TivaFWUpdateStatus.Value = 0;
                    timer.Dispose();
                }

            }
            else if (Device.IsDFUConnected())
            {
                UI_no_connection();
                SDK.AutoSearch = false;
                SDK.IsEnableNotify = false;
                SDK.IsConnectionChecking = false;

                TimerCallback callback = new TimerCallback(TimerTask);
                TivaUpdateTime = 1;
                timer = new System.Threading.Timer(callback, null, 1000, 1000);
                bwTivaUpdate.RunWorkerAsync();
            }
            else
            {
                SDK.AutoSearch = true;
                SDK.IsEnableNotify = true;
                MessageBox.Show("Device does not exist or image file path error!", "Error");
                SDK.IsConnectionChecking = true;
                ProgressBar_TivaFWUpdateStatus.Value = 0;
            }
        }

        private int pValue = 30;
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (pValue < 99)
            {
                pValue += 1;
                bwTivaUpdate.ReportProgress(pValue);
            }
        }

        private void bwTivaUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            bwTivaUpdate.ReportProgress(30);

            pValue = 30;
            System.Timers.Timer pTimer = new System.Timers.Timer(200);
            pTimer.Elapsed += OnTimedEvent;
            pTimer.AutoReset = true;
            pTimer.Enabled = true;

            e.Result = Device.Tiva_FW_Update(TextBox_TivaFWPath.Text);

            if ((int)e.Result == 0)
            {
                Task.Run(() => Device.Close());
                // Wait for reboot
                int step = (100 - pValue) / 51;
                int counts = 0;
                while (counts < 50)
                {
                    Thread.Sleep(120);
                    bwTivaUpdate.ReportProgress(pValue);
                    counts++;
                }
            }

            pTimer.Enabled = false;
        }

        private void bwTivaUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int percentage = e.ProgressPercentage;
            ProgressBar_TivaFWUpdateStatus.Value = percentage;
        }

        private void bwTivaUpdate_DoSacnCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int ret = (int)e.Result;

            timer.Dispose();
            ProgressBar_TivaFWUpdateStatus.Value = 100;

            if (Device.Get_USB_Handler() == MOTOR_USB_PID)
                Device.Set_USB_Handler(MAIN_USB_PID);

            if (ret == 0)
            {
                String text = "Tiva FW updated successfully!";
                MessageBox.Show(text, "Success");
            }
            else
            {
                switch (ret)
                {
                    case -1:
                        String text = "The driver, lmdfu.dll, for the USB Device Firmware Upgrade device cannot be found!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -2:
                        text = "The driver for the USB Device Firmware Upgrade device was found but appears to be a version which this program does not support!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -3:
                        text = "An error was reported while attempting to load the device driver for the USB Device Firmware Upgrade device!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -4:
                        text = "Unable to open binary file.Copy binary file to a folder with Admin / read / write permission and try again.";
                        MessageBox.Show(text, "Error");
                        break;
                    case -5:
                        text = "Memory alloc for file read failed!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -6:
                        text = "This file does not appear to be valid for the target device.";
                        MessageBox.Show(text, "Error");
                        break;
                    case -7:
                        text = "This file is not correct FW for the device!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -8:
                        text = "Error reported during file download!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -9:
                        text = "Unable to open Device Firmware Upgrade device. Check the driver installed and try again!";
                        MessageBox.Show(text, "Error");
                        break;
                    case -10:
                        text = "This firmware is not for the device model. Please make sure you have a correct one!";
                        MessageBox.Show(text, "Error");
                        break;
                    default:
                        text = "Unknown error occured!";
                        MessageBox.Show(text, "Error");
                        break;
                }

            }
            ProgressBar_TivaFWUpdateStatus.Value = 0;

            String pbString = "Please wait few seconds for device reboot!\r\n";
            ProgressWindowStart("Device rebooting", pbString, false);

            for (int i = 0; i < 6; i++)
            {
                string doting = string.Format("  {0}  ", (6 - i).ToString());
                ProgressWindowContentUpdate(pbString + doting);
                for (int j = 0; j < 5; j++)
                {
                    doting = "<< " + doting + " >>";
                    ProgressWindowContentUpdate(pbString + doting);
                    SpinWait.SpinUntil(() => false, 200);
                }
            }
            ProgressWindowCompleted();
            SDK.IsEnableNotify = true;
            Device.Open(null);
        }
        #endregion
        #region DLPC150 FW Update
        //DLPC150 FW Update
        private void Button_DLPC150FWBrowse_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = (DLPC_FWDir == String.Empty) ? (Directory.GetCurrentDirectory()) : (DLPC_FWDir),
                FileName = "",              // Default file name
                DefaultExt = ".img",        // Default file extension
                Filter = "Image File|*.img" // Filter files by extension
            };

            dlg.ShowDialog();
            if (dlg.FileName != "")
            {
                TextBox_DLPC150FWPath.Text = dlg.FileName;
                DLPC_FWDir = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf("\\"));
            }
        }
        private void Button_DLPC150FWUpdate_Click(object sender, EventArgs e)
        {
            if (Device.IsConnected() && TextBox_DLPC150FWPath.Text != "")
            {
                //ControlAllControls(this, false);
                this.Enabled = false;
                SDK.AutoSearch = false;
                SDK.IsEnableNotify = false;
                SDK.IsConnectionChecking = false;

                bwDLPCUpdate.RunWorkerAsync(TextBox_DLPC150FWPath.Text);
            }
            else
            {
                String text = "Device dose not exist or image file path error!";
                MessageBox.Show(text, "Error");
            }
        }

        private void bwDLPCUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            int expectedChecksum = 0, chksum = 0, ret = 0;
            String fileName = (String)e.Argument;
            byte[] imgByteBuff = File.ReadAllBytes(fileName);
            e.Result = false;

            int dataLen = imgByteBuff.Length;

            if (!Device.DLPC_CheckSignature(imgByteBuff))
            {
                DBG.WriteLine("Invalid DLPC150 image file!");
                logFile.Warn("Invalid DLPC150 image file!");
                return;
            }

            ret = Device.DLPC_SetImageSize(dataLen);
            if (ret < 0)
            {
                DBG.WriteLine("Set DLPC150 image size failed! (error: {0})", ret);
                logFile.ErrorFormat("Set DLPC150 image size failed! (error: {0})", ret);
                return;
            }

            for (int i = 0; i < dataLen; i++)
            {
                expectedChecksum += imgByteBuff[i];
            }

            Thread.Sleep(1000);

            int bytesToSend = dataLen, bytesSent = 0;
            while (bytesToSend > 0)
            {
                byte[] byteArrayToSent = new byte[bytesToSend];
                Buffer.BlockCopy(imgByteBuff, dataLen - bytesToSend, byteArrayToSent, 0, bytesToSend);

                bytesSent = Device.DLPC_FW_Update_WriteData(byteArrayToSent, bytesToSend);

                if (bytesSent < 0)
                {
                    DBG.WriteLine("DLPC150 update: Data send Failed!");
                    logFile.Error("DLPC150 update: Data send Failed!");
                    break;
                }

                bytesToSend -= bytesSent;

                // Report the FW update status
                float updateProgress;
                updateProgress = ((float)(dataLen - bytesToSend) / dataLen) * 100;
                bwDLPCUpdate.ReportProgress((int)updateProgress);
            }

            chksum = Device.DLPC_Get_Checksum();

            if (chksum < 0)
            {
                DBG.WriteLine("Error Reading DLPC150 Flash Checksum! (error: {0})", chksum);
                logFile.ErrorFormat("Error Reading DLPC150 Flash Checksum! (error: {0})", chksum);
            }
            else if (chksum != expectedChecksum)
            {
                DBG.WriteLine("Checksum mismatched: (Expected: {0}, DLPC Flash: {1})", expectedChecksum, chksum);
                logFile.ErrorFormat("Checksum mismatched: (Expected: {0}, DLPC Flash: {1})", expectedChecksum, chksum);
            }
            else
            {
                DBG.WriteLine("DLPC150 updated successfully!");
                logFile.Info("DLPC150 updated successfully!");
                e.Result = true;
            }
        }

        private void bwDLPCUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int percentage = e.ProgressPercentage;
            ProgressBar_DLPC150FWUpdateStatus.Value = e.ProgressPercentage;
        }

        private void bwDLPCUpdate_DoWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Device.Close();

            if ((bool)e.Result)
            {
                String text = "DLPC150 FW updated successfully!";
                MessageBox.Show(text, "Success");
            }
            else
            {
                String text = "DLPC150 FW update failed!";
                MessageBox.Show(text, "Error");
            }

            ProgressBar_DLPC150FWUpdateStatus.Value = 0;

            SDK.IsEnableNotify = true;
            Device.Open(null);

            SDK.AutoSearch = true;
            SDK.IsConnectionChecking = true;
        }
        #endregion
        #region Calibration Coefficients
        //Calibration Coefficients
        private void Button_Cal_WriteGenCoeffs_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Generic Coefficients", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (Device.SetGenericCalibStruct() == SDK.RETURN_PASS)
                    Button_Cal_ReadCoeffs_Click(sender, e);
                else
                {
                    Message.ShowError("Write Failed!");
                }
            }

        }

        private void Button_Cal_ReadCoeffs_Click(object sender, EventArgs e)
        {
            if (Device.GetCalibStruct() == SDK.RETURN_PASS)
            {
                Label_CalCoeffVer.Text = Device.DevInfo.CalRev.ToString();
                Label_RefCalVer.Text = Device.DevInfo.RefCalRev.ToString();
                Label_ScanCfgVer.Text = Device.DevInfo.CfgRev.ToString();
                TextBox_P2WCoeff0.Text = Device.Calib_Coeffs.PixelToWavelengthCoeffs[0].ToString();
                TextBox_P2WCoeff1.Text = Device.Calib_Coeffs.PixelToWavelengthCoeffs[1].ToString();
                TextBox_P2WCoeff2.Text = Device.Calib_Coeffs.PixelToWavelengthCoeffs[2].ToString();
                TextBox_ShiftVectCoeff0.Text = Device.Calib_Coeffs.ShiftVectorCoeffs[0].ToString();
                TextBox_ShiftVectCoeff1.Text = Device.Calib_Coeffs.ShiftVectorCoeffs[1].ToString();
                TextBox_ShiftVectCoeff2.Text = Device.Calib_Coeffs.ShiftVectorCoeffs[2].ToString();
                
                try
                {
                    if (Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[0] - std_calib_coeffs_PixelToWavelengthCoeffs_0) / std_calib_coeffs_PixelToWavelengthCoeffs_0) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[1] - std_calib_coeffs_PixelToWavelengthCoeffs_1) / std_calib_coeffs_PixelToWavelengthCoeffs_1) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.PixelToWavelengthCoeffs[2] - std_calib_coeffs_PixelToWavelengthCoeffs_2) / std_calib_coeffs_PixelToWavelengthCoeffs_2) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[0] - std_calib_coeffs_ShiftVectorCoeffs_0) / std_calib_coeffs_ShiftVectorCoeffs_0) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[1] - std_calib_coeffs_ShiftVectorCoeffs_1) / std_calib_coeffs_ShiftVectorCoeffs_1) < calib_coeffs_diff_limit &&
                        Math.Abs((Device.Calib_Coeffs.ShiftVectorCoeffs[2] - std_calib_coeffs_ShiftVectorCoeffs_2) / std_calib_coeffs_ShiftVectorCoeffs_2) < calib_coeffs_diff_limit)
                    {
                        Message.ShowWarning("The device's coefficients are the same as generic setting.\n\n" +
                            "This usually means the device might be un-calibrated or recovered from an EEPROM data corrupted.\n\n" +
                            "You can try to Restore Factory Calibration Data\n" +
                            "Or\n" +
                            "Please contact ISC for supporting.");
                    }
                }
                catch { }
            }
            else
            {
                Label_CalCoeffVer.Text = "0";
                Label_RefCalVer.Text = "0";
                Label_ScanCfgVer.Text = "0";
                TextBox_P2WCoeff0.Text = "Read Failed!";
                TextBox_P2WCoeff1.Text = "Read Failed!";
                TextBox_P2WCoeff2.Text = "Read Failed!";
                TextBox_ShiftVectCoeff0.Text = "Read Failed!";
                TextBox_ShiftVectCoeff1.Text = "Read Failed!";
                TextBox_ShiftVectCoeff2.Text = "Read Failed!";
            }
        }

        private void Button_Cal_RestoreDefaultCoeffs_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to restore it?", "Coefficient", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                int ret = Device.RestoreDefaultCalibStruct();
                if (ret == 0)
                    Button_Cal_ReadCoeffs_Click(sender, e);
                else if (ret == -4)
                {
                    Message.ShowError("Device does not have backup data!");
                }
                else
                {
                    Message.ShowError("Restore Failed!");
                }
            }
        }

        private void Button_Cal_WriteCoeffs_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Coefficient", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Device.CalibCoeffs Calib_Coeffs = new Device.CalibCoeffs
                {
                    PixelToWavelengthCoeffs = new Double[3],
                    ShiftVectorCoeffs = new Double[3]
                };

                if ((Double.TryParse(TextBox_P2WCoeff0.Text, out Calib_Coeffs.PixelToWavelengthCoeffs[0]) == false) ||
                    (Double.TryParse(TextBox_P2WCoeff1.Text, out Calib_Coeffs.PixelToWavelengthCoeffs[1]) == false) ||
                    (Double.TryParse(TextBox_P2WCoeff2.Text, out Calib_Coeffs.PixelToWavelengthCoeffs[2]) == false) ||
                    (Double.TryParse(TextBox_ShiftVectCoeff0.Text, out Calib_Coeffs.ShiftVectorCoeffs[0]) == false) ||
                    (Double.TryParse(TextBox_ShiftVectCoeff1.Text, out Calib_Coeffs.ShiftVectorCoeffs[1]) == false) ||
                    (Double.TryParse(TextBox_ShiftVectCoeff2.Text, out Calib_Coeffs.ShiftVectorCoeffs[2]) == false))
                {
                    Message.ShowError("Not Numeric!");
                    return;
                }

                if (Device.SendCalibStruct(Calib_Coeffs) == SDK.RETURN_PASS)
                {
                    Button_Cal_ReadCoeffs_Click(sender, e);
                    //should set config to update DMD pattern
                    if (DevCurCfg_IsTarget)
                    {
                        SetScanConfig(ScanConfig.TargetConfig[DevCurCfg_Index], true, DevCurCfg_Index);
                    }
                    else
                    {
                        SetScanConfig(LocalConfig[DevCurCfg_Index], false, DevCurCfg_Index);
                    }
                }
                else
                {
                    Message.ShowError("Write Failed!");
                }
            }

        }

        private void CheckBox_Cal_WriteEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBox_Cal_WriteEnable.Checked == true)
            {
                Button_Cal_WriteCoeffs.Enabled = true;
                Button_Cal_WriteGenCoeffs.Enabled = true;
                if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2)
                    Button_Cal_RestoreDefaultCoeffs.Enabled = IsActivated;
                else
                    Button_Cal_RestoreDefaultCoeffs.Enabled = false;
            }
            else
            {
                Button_Cal_WriteCoeffs.Enabled = false;
                Button_Cal_WriteGenCoeffs.Enabled = false;
                Button_Cal_RestoreDefaultCoeffs.Enabled = false;
            }
        }
        #endregion
        #region Device Information
        //Device Information
        private void GetDeviceInfo()
        {
            if (!Device.IsConnected())
                return;

            SystemBusy(true);

            String GUIRev = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GUIRev = GUIRev.Substring(0, GUIRev.LastIndexOf('.'));

            String DLPCRev = "";
            DLPCRev = Device.DevInfo.DLPCRev[0].ToString() + "."
                    + Device.DevInfo.DLPCRev[1].ToString() + "."
                    + Device.DevInfo.DLPCRev[2].ToString();

            String HWRev_MB = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;
            String HWRev_DB = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(4, 1) : String.Empty;

            label_DevInfoGUIVer.Text = GUIRev;
            label_DevInfoDLPCVer.Text = DLPCRev;
            label_DevInfoMainBoardVer.Text = HWRev_MB;
            label_DevInfoDetectorBoardVer.Text = HWRev_DB;
            label_DevInfoModelName.Text = Device.DevInfo.ModelName;
            label_DevInfoDevSerNum.Text = Device.DevInfo.SerialNumber;

            String TivaRev = String.Empty;
            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_3)
            {
                TivaRev = Device.DevInfo.TivaRev[0].ToString() + "."
                        + Device.DevInfo.TivaRev[1].ToString() + "."
                        + Device.DevInfo.TivaRev[2].ToString();
            }
            else
            {
                if (Device.DevInfo.TivaRev[3] == 0)
                {
                    TivaRev = Device.DevInfo.TivaRev[0].ToString() + "."
                            + Device.DevInfo.TivaRev[1].ToString() + "."
                            + Device.DevInfo.TivaRev[2].ToString();
                }
                else
                {
                    TivaRev = Device.DevInfo.TivaRev[0].ToString() + "."
                            + Device.DevInfo.TivaRev[1].ToString() + "."
                            + Device.DevInfo.TivaRev[2].ToString() + "."
                            + Device.DevInfo.TivaRev[3].ToString();
                }
            }
            label_DevInfoTivaSWVer.Text = TivaRev;

            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2)
            {
                String Manu_Seri_Num = Device.DevInfo.Manufacturing_SerialNumber;
                if (!Manu_Seri_Num.Contains("70UB1") && !Manu_Seri_Num.Contains("95UB1"))
                    Manu_Seri_Num = "NA";
                label_DevInfoManfacSerNum.Text = Manu_Seri_Num;
                UpdateLampUsage();
            }
            else
            {
                label_DevInfoManfacSerNum.Text = String.Empty;
                label_DevInfoLampUsageValue.Text = String.Empty;
            }

            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_1)
            {
                String UUID = BitConverter.ToString(Device.DevInfo.DeviceUUID).Replace("-", ":");
                label_DevInfoUUID.Text = UUID;
            }
            else
            {
                label_DevInfoUUID.Text = String.Empty;
            }
            if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_4)
            {
                int ret;
                StringBuilder pOutBuf = new StringBuilder(128);
                if (IsActivated && (ret = Device.ReadBleDispName(pOutBuf)) == SDK.RETURN_PASS)
                    Label_BleNameValue.Text = pOutBuf.ToString();
                else
                    Label_BleNameValue.Text = "NA";
                if (!IsActivated)
                    Label_ButtonStatus.Text = "Button Status: NA";
                pOutBuf.Clear();
            }
            SystemBusy(false);
        }
        private void UpdateLampUsage()
        {
            String Lamp_Usage = "";
            if (Device.ReadLampUsage() == 0)
                Lamp_Usage = GetLampUsage();
            else
                Lamp_Usage = "NA";
            label_DevInfoLampUsageValue.Text = Lamp_Usage;
        }
        private void CheckLampFuncUseful()
        {
            if (!Device.IsConnected())
                return;

            // Convert main board version to ASCII
            Byte[] HWRev = Encoding.ASCII.GetBytes(Device.DevInfo.HardwareRev);
            Int32 MB_Ver = HWRev[0];

            // Battery Charger Information
            if (MB_Ver == 'N' || MB_Ver == 'E')
            {
                lb_BattChargerStatusTitle.Font = new Font(lb_BattChargerStatusTitle.Font.FontFamily, lb_BattChargerStatusTitle.Font.Size, FontStyle.Strikeout);
                lb_BattChargerStatusTitle.ForeColor = Color.LightGray;
                lb_BattCapTitle.Font = new Font(lb_BattCapTitle.Font.FontFamily, lb_BattCapTitle.Font.Size, FontStyle.Strikeout);
                lb_BattCapTitle.ForeColor = Color.LightGray;
                Label_SensorBattCapacity.Visible = false;
                Label_SensorBattStatus.Visible = false;
            }
            else
            {
                lb_BattChargerStatusTitle.Font = new Font(lb_BattChargerStatusTitle.Font.FontFamily, lb_BattChargerStatusTitle.Font.Size, FontStyle.Regular);
                lb_BattChargerStatusTitle.ForeColor = Color.Black;
                lb_BattCapTitle.Font = new Font(lb_BattCapTitle.Font.FontFamily, lb_BattCapTitle.Font.Size, FontStyle.Regular);
                lb_BattCapTitle.ForeColor = Color.Black;
                Label_SensorBattCapacity.Visible = true;
                Label_SensorBattStatus.Visible = true;
            }

            // Lamp Usage Information
            if (Device.DevInfo.ModelType == "F")
            {
                GroupBox_LampUsage.Visible = false;
                label_DevInfoLampUsage.Visible = false;
                label_DevInfoLampUsageValue.Visible = false;
            }
            else
            {
                GroupBox_LampUsage.Visible = true;
                label_DevInfoLampUsage.Visible = true;
                label_DevInfoLampUsageValue.Visible = true;

                if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_2)
                {
                    GroupBox_LampUsage.Enabled = IsActivated;
                    label_DevInfoLampUsage.Enabled = IsActivated;
                    label_DevInfoLampUsageValue.Enabled = IsActivated;
                }
            }

            // Lamp Sensors Information
            Label_SensorLampVM1.Visible = false;
            Label_SensorLampVM1Value.Visible = false;
            Label_SensorLampVM2.Visible = false;
            Label_SensorLampVM2Value.Visible = false;
            Label_SensorLampCM1.Visible = false;
            Label_SensorLampCM1Value.Visible = false;
            Label_SensorLampCM2.Visible = false;
            Label_SensorLampCM2Value.Visible = false;

            if (Device.DevInfo.ModelType != "F")
            {
                if (MB_Ver >= 'F' && MB_Ver < 'N' && GetFW_LEVEL() >= FW_LEVEL.LEVEL_4)  // STD Version
                {
                    Label_SensorLampVM1.Visible = true;
                    Label_SensorLampVM1Value.Visible = true;
                    Label_SensorLampCM1.Visible = true;
                    Label_SensorLampCM1Value.Visible = true;

                    if (Device.DevInfo.ModelType == "R")
                    {
                        Label_SensorLampVM2.Visible = true;
                        Label_SensorLampVM2Value.Visible = true;
                        Label_SensorLampCM2.Visible = true;
                        Label_SensorLampCM2Value.Visible = true;

                        Label_SensorLampVM1.Text = "Lamp 1 Voltage";
                        Label_SensorLampCM1.Text = "Lamp 1 Current";
                        Label_SensorLampVM2.Text = "Lamp 2 Voltage";
                        Label_SensorLampCM2.Text = "Lamp 2 Current";
                    }
                    else
                    {
                        Label_SensorLampVM1.Text = "Lamp Voltage";
                        Label_SensorLampCM1.Text = "Lamp Current";
                    }
                }
                else if (MB_Ver >= 'O' && GetFW_LEVEL() >= FW_LEVEL.LEVEL_5)  // EXT Version
                {
                    Label_SensorLampVM1.Visible = true;
                    Label_SensorLampVM1Value.Visible = true;

                    if (Device.DevInfo.ModelType == "R")
                    {
                        Label_SensorLampCM1.Visible = true;
                        Label_SensorLampCM1Value.Visible = true;
                        Label_SensorLampVM2.Visible = true;
                        Label_SensorLampVM2Value.Visible = true;
                        Label_SensorLampCM2.Visible = true;
                        Label_SensorLampCM2Value.Visible = true;

                        Label_SensorLampVM1.Text = "Lamp 1 Current";
                        Label_SensorLampCM1.Text = "Lamp 2 Current";
                        Label_SensorLampVM2.Text = "Lamp 3 Current";
                        if (GetFW_LEVEL() >= FW_LEVEL.LEVEL_6)
                            Label_SensorLampCM2.Text = "Fan Current";
                        else
                            Label_SensorLampCM2.Text = "Lamp 4 Current";
                    }
                    else
                    {
                        Label_SensorLampVM1.Text = "Lamp Current";
                    }
                }
                else  // HW Version <= E or HW Version = N or old Tiva FW
                {
                    Label_SensorLampVM1.Visible = true;
                    Label_SensorLampVM1Value.Visible = true;

                    Label_SensorLampVM1.Text = "Lamp Indicator";
                }
            }
        }
        #endregion

        #region Activation Key
        //Activation Key
        private void button_KeySet_Click(object sender, EventArgs e)
        {
            String[] StrKey = TextBox_Key.Text.Split(new char[] { ' ', ':', ';', '-', '_' });
            if (StrKey.Length != 12)
            {
                Message.ShowError("Input Key Length / Format Is Not Correct!");
                return;
            }

            DialogResult result = Message.ShowQuestion("Do you want to set it?\n\nThe GUI needs to re-launch for verification.", "Key", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);
                Byte[] ByteKey = new Byte[12];

                for (int i = 0; i < StrKey.Length; i++)
                {
                    try { ByteKey[i] = Convert.ToByte(StrKey[i], 16); }
                    catch { ByteKey[i] = 0; }
                }

                bool prevActState = IsActivated;
                Device.SetActivationKey(ByteKey);
                Application.Restart();
            }
        }

        private void GetActivationKeyStatus()
        {
            if (IsActivated)
            {
                label_ActivateStatus.Text = "Activated";
                GUI_Handler((int)MainWindow.GUI_State.KEY_ACTIVATE);
            }
            else
            {
                label_ActivateStatus.Text = "Not activated!";
                GUI_Handler((int)MainWindow.GUI_State.KEY_NOT_ACTIVATE);
            }
        }
        #endregion

        #region Reset Device
        //Device
        //Reset Device
        private void button_DeviceResetSys_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to reset it?", "Reset System", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (!Device.IsConnected())
                    return;

                bwTivaReset = new BackgroundWorker
                {
                    WorkerReportsProgress = false,
                    WorkerSupportsCancellation = true
                };
                bwTivaReset.DoWork += new DoWorkEventHandler(bwTivaReset_DoWork);
                bwTivaReset.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwTivaReset_DoWorkCompleted);

                SDK.IsConnectionChecking = false;
                bwTivaReset.RunWorkerAsync();
            }
        }
        private BackgroundWorker bwTivaReset;
        private static void bwTivaReset_DoWork(object sender, DoWorkEventArgs e)
        {
            int ret = Device.ResetTiva(false);
        }
        private static void bwTivaReset_DoWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SDK.IsConnectionChecking = true;
        }
        #endregion
        #region Update Reference Data
        //Update Reference Data
        private void button_DeviceUpdateRef_Click(object sender, EventArgs e)
        {
            if (Device.IsConnected())
            {
                DialogResult result = Message.ShowQuestion("IMPORTANT!!!\n\nThis will REPLACE your FACTORY REFERENCE DATA \nand could NOT be REVERTED.\n\nAre you sure you want to do this ? ", null, MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    result = Message.ShowQuestion("User Agreements:\n\n" +
                    "1. I am well aware of the purpose of factory reference data\n" +
                    "    and have been well trained to replace it.\n" +
                    "2. I fully understand that the factory reference data can be replaced\n" +
                    "    but not revertible.\n" +
                    "3. I agree to pay extra fee to recover the factory reference data\n" +
                    "    if I make anything wrong.\n\n" +
                    "I agree with above terms and would like to continue the process.\n"
                    , null, MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        result = Message.ShowQuestion("IMPORTANT!!!\n\nPlease confirm again with this process.\n\nDo you still want to do this?", null, MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            result = Message.ShowQuestion("Please place the reference sample and press 'OK' to start the reference scan...", null, MessageBoxButtons.OKCancel);
                            if (result == DialogResult.OK)
                            {
                                bwRefScanProgress = new BackgroundWorker
                                {
                                    WorkerReportsProgress = false,
                                    WorkerSupportsCancellation = true
                                };
                                bwRefScanProgress.DoWork += new DoWorkEventHandler(bwRefScanProgress_DoWork);
                                bwRefScanProgress.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwRefScanProgress_DoWorkCompleted);
                                bwRefScanProgress.RunWorkerAsync();
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

            }
            else
            {
                String text = "No device is connected!";
                MessageBox.Show(text, "Warning");
            }
        }

        private void bwRefScanProgress_DoWork(object sender, DoWorkEventArgs e)
        {
            Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
            tmpCfg = ScanConfig.GetCurrentConfig();  // Backup current config before update reference
            ScanConfig.SlewScanConfig scanCfg = ScanConfig.GetFactoryReferenceConfig();

            int ret = ScanConfig.SetScanConfig(scanCfg);
            if (ret != 0)
            {
                e.Result = -3;
                return;
            }

            Thread.Sleep(200);
            ret = Scan.PerformScan(Scan.SCAN_REF_TYPE.SCAN_REF_NEW);
            if (ret == 0)
            {
                ret = Scan.SaveReferenceScan();
                if (ret == 0)
                    e.Result = 0;
                else
                    e.Result = -2;
            }
            else
                e.Result = -1;
        }

        private void bwRefScanProgress_DoWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressWindowCompleted();
            int ret = (int)e.Result;
            if (ret == 0)
            {
                String text = "Reference Scan Completed Seccessfully!\n\nPlease start a new scan to check the result.";
                MessageBox.Show(text, "Success");
                GetBuildInRefTime();
                if (RadioButton_RefFac.Checked)
                {
                    RadioButton_RefFac_CheckedChanged(null, null);
                }
            }
            else if (ret == -1)
            {
                String text = "Scan Failed!";
                MessageBox.Show(text, "Error");
            }
            else if (ret == -2)
            {
                String text = "Save Reference Sacn Failed!";
                MessageBox.Show(text, "Error");
            }
            else if (ret == -3)
            {
                String text = "Set Reference Sacn Configuration Failed!";
                MessageBox.Show(text, "Error");
            }
            else
            {
                String text = "Unknow Error Occured!";
                MessageBox.Show(text, "Error");
            }
            ScanConfig.SetScanConfig(tmpCfg);  // Set current config after update reference
        }
        #endregion
        #region Back up factory reference
        //Back up factory reference
        private bool DeviceConnectBackUpRef()
        {
            int ret = SDK.RETURN_FAIL;
            if (Device.IsConnected())
            {
                string serNum = Device.DevInfo.SerialNumber.ToString();
                ret = Device.Backup_Factory_Reference(serNum);
                if (ret < 0)
                {
                    switch (ret)
                    {
                        case -1:
                            BackupFacRef_Msg = "Out of memory!";
                            break;
                        case -2:
                            BackupFacRef_Msg = "System I/O error!";
                            break;
                        case -3:
                            BackupFacRef_Msg = "Device communcation error!";
                            break;
                        case -4:
                            BackupFacRef_Msg = "Device does not have the original factory reference data!";
                            break;
                    }
                }
                else
                    BackupFacRef_Msg = "Device contains original factory reference data!";
            }
            else
                BackupFacRef_Msg = "No device connected for backup factory reference!";

            if (ret < 0)
                return false;
            else
                return true;
        }

        private void button_DeviceRestoreFacRef_Click(object sender, EventArgs e)
        {
            DialogResult result = Message.ShowQuestion("Do you want to restore it?", "Restore Factory Reference", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);
                if (Device.IsConnected())
                {
                    int ret;
                    string serNum = Device.DevInfo.SerialNumber.ToString();
                    ret = Device.Restore_Factory_Reference(serNum);
                    if (ret < 0)
                    {
                        switch (ret)
                        {
                            case -1:
                                String text = "Factory reference data restore FAILED!\n\nOut of memory.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -2:
                                text = "Factory reference data restore FAILED!\n\nBackup directory does not found.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -3:
                                text = "Factory reference data restore FAILED!\n\nRead file error.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -4:
                                text = "Factory reference data restore FAILED!\n\nReference data currupted.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -5:
                                text = "Factory reference data restore FAILED!\n\nDevice communcation error.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -6:
                                text = "Factory reference data restore FAILED!\n\nData was NOT the original factory reference data.";
                                MessageBox.Show(text, "Error");
                                break;
                            case -7:
                                text = "Factory reference data restore FAILED!\n\nBackup file does not found.";
                                MessageBox.Show(text, "Error");
                                break;
                        }
                    }
                    else
                    {
                        String text = "Factory reference data has been restored successfully!\n\nPlease start a new scan to check the result.";
                        MessageBox.Show(text, "Success");
                        GetBuildInRefTime();
                        if (RadioButton_RefFac.Checked)
                        {
                            RadioButton_RefFac_CheckedChanged(null, null);
                        }
                        //ClearScanPlotsEvent();
                    }
                }
                else
                {
                    String text = "No device connected for restoring factory reference!";
                    MessageBox.Show(text, "Warning");
                }
                SystemBusy(false);
            }

        }
        #endregion

        #region Button Lock/Unlock

        private void Button_LockButton_Click(object sender, EventArgs e)
        {
            if (Device.SetButtonLock(true) == SDK.RETURN_PASS)
            {
                Int32 status = Device.GetButtonLockStatus();
                if (status == 1)
                    Label_ButtonStatus.Text = "Button Status: Locked!";
                else if (status == 0)
                    Label_ButtonStatus.Text = "Button Status: Unlocked!";
                else
                    Label_ButtonStatus.Text = "Button Status: Read Failed!";
            }
            else
            {
                Label_ButtonStatus.Text = "Button Status: Lock Failed!";
            }
        }

        private void Button_UnlockButton_Click(object sender, EventArgs e)
        {
            if (Device.SetButtonLock(false) == SDK.RETURN_PASS)
            {
                Int32 status = Device.GetButtonLockStatus();
                if (status == 1)
                    Label_ButtonStatus.Text = "Button Status: Locked!";
                else if (status == 0)
                    Label_ButtonStatus.Text = "Button Status: Unlocked!";
                else
                    Label_ButtonStatus.Text = "Button Status: Read Failed!";
            }
            else
            {
                Label_ButtonStatus.Text = "Button Status: Unlock Failed!";
            }
        }

        #endregion

        #region BLE Advertising Name

        private void Button_Clear_BLE_Display_Name_Click(object sender, EventArgs e)
        {
            SystemBusy(true);
            if (Device.WriteBleDispName("") == SDK.RETURN_PASS)
                Button_Get_BLE_Display_Name_Click(sender, e);
            else
                TextBox_BLE_Display_Name.Text = "Clear Failed!";
            SystemBusy(false);
        }

        private void Button_Set_BLE_Display_Name_Click(object sender, EventArgs e)
        {
            String BLE_Name = TextBox_BLE_Display_Name.Text;

            DialogResult result = Message.ShowQuestion("Do you want to write it?", "Bluetooth LE Advertising Name", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SystemBusy(true);
                String RegularExpressions = "^[a-zA-Z0-9_<>{}-]*[^\r\t\n\f]*$";
                Match rgx = Regex.Match(BLE_Name, RegularExpressions);
                if (!rgx.Success)
                {
                    Message.ShowError("BLE Name can only be alpha numeric characters, please enter again in the correct format!");
                    TextBox_BLE_Display_Name.Text = String.Empty;
                    return;
                }
                else if (BLE_Name.Length > 24 - 1)
                {
                    Message.ShowError("The max. BLE Name length should be less than 23 characters!");
                    TextBox_BLE_Display_Name.Text = BLE_Name.Substring(0, 23);
                    TextBox_BLE_Display_Name.Text.PadLeft(24, '\0');
                    return;
                }
                RegularExpressions = "(?=.*[!@#$%^&+=*|/~`:;'?.])";
                rgx = Regex.Match(BLE_Name, RegularExpressions);
                if (rgx.Success)
                {
                    Message.ShowError("BLE Name can only be alpha numeric characters, please enter again in the correct format!");
                    TextBox_BLE_Display_Name.Text = String.Empty;
                    return;
                }
                if (BLE_Name.Contains(@"\") || BLE_Name.Contains(@""""))
                {
                    Message.ShowError("BLE Name can only be alpha numeric characters, please enter again in the correct format!");
                    TextBox_BLE_Display_Name.Text = String.Empty;
                    return;
                }
                if (Device.WriteBleDispName(BLE_Name) == SDK.RETURN_PASS)
                    Button_Get_BLE_Display_Name_Click(sender, e);
                else
                    TextBox_BLE_Display_Name.Text = "Write Failed!";
                SystemBusy(false);
            }
        }

        private void Button_Get_BLE_Display_Name_Click(object sender, EventArgs e)
        {
            int ret;
            StringBuilder pOutBuf = new StringBuilder(128);

            if ((ret = Device.ReadBleDispName(pOutBuf)) == SDK.RETURN_PASS)
            {
                Label_BleNameValue.Text = pOutBuf.ToString();
                TextBox_BLE_Display_Name.Text = pOutBuf.ToString();
            }
            else
            {
                TextBox_BLE_Display_Name.Text = "Read Failed! (" + ret.ToString() + ")";
                Label_BleNameValue.Text = "NA";
            }
            pOutBuf.Clear();
        }

        #endregion

        #region About
        //About
        private void button_AboutLicense_Click(object sender, EventArgs e)
        {
            LicenseWindow window = new LicenseWindow { Owner = this };
            window.ShowDialog();
        }

        private void button_About_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://www.inno-spectra.com/");
            }
            catch { }
        }

        #endregion

        #endregion

        private void AddDirectorySecurity(string dirPath)
        {
            DirectoryInfo dInfo = new DirectoryInfo(dirPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            string account = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
            dSecurity.AddAccessRule(new FileSystemAccessRule(account, FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }

        private void Button_SaveDirChange_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = Dir_Scan_For_New;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    TextBox_SaveDirPath.Text = fbd.SelectedPath;
                    Dir_Scan_For_New = fbd.SelectedPath;
                    SaveSettings();

                    try { AddDirectorySecurity(Dir_Scan_For_New); }
                    catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }
                }
            }
        }

        private void CheckScanDirPath()
        {
            if (!Directory.Exists(TextBox_SaveDirPath.Text))
            {
                Message.ShowWarning("The scan directory has not exist. Will set to default path.");
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Dir_Scan_For_New = Path.Combine(path, "InnoSpectra\\Scan Results");
                TextBox_SaveDirPath.Text = Dir_Scan_For_New;
            }
        }

        private int lastTabScanPageSelection = 0;
        private void tabScanPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Button_Scan.Text == "Scan Next")
                button_ExitCont.PerformClick();

            switch (tabScanPage.SelectedIndex)
            {
                case 0:
                    {
                        if (RadioButton_RefNew.Checked)
                            RadioButton_RefNew_CheckedChanged(this, null);
                        else if (RadioButton_RefPre.Checked && !Scan.IsLocalRefExist)
                            RadioButton_RefNew.PerformClick();
                        if (lastTabScanPageSelection == 2)
                        {
                            Device.Information();
                            Scan.ClearData();
                            Clear_Chart(true);
                            Check_Overlay.CheckedChanged -= Check_Overlay_CheckedChanged;
                            Check_Overlay.Checked = false;
                            Check_Overlay.Visible = true;
                            Check_Overlay.CheckedChanged += Check_Overlay_CheckedChanged;
                        }
                        Button_Scan.Visible = true;
                        IsSavedScanData = false;
                        break;
                    }
                case 1:
                    {
                        if (lastTabScanPageSelection == 2)
                        {
                            Device.Information();
                            Scan.ClearData();
                            Clear_Chart(true);
                            Check_Overlay.CheckedChanged -= Check_Overlay_CheckedChanged;
                            Check_Overlay.Checked = false;
                            Check_Overlay.CheckedChanged += Check_Overlay_CheckedChanged;
                        }
                        Check_Overlay.Visible = true;
                        Button_Scan.Visible = true;
                        IsSavedScanData = false;
                        if (TargetCfg_SelIndex == -1 && TargetCfg_Last_SelIndex == -1)
                            TargetCfg_SelIndex = 0;
                        else if (TargetCfg_SelIndex == -1 && TargetCfg_Last_SelIndex != -1)
                            TargetCfg_SelIndex = TargetCfg_Last_SelIndex;
                        else
                            TargetCfg_Last_SelIndex = TargetCfg_SelIndex;
                        if (LocalCfg_SelIndex == -1 && LocalCfg_Last_SelIndex == -1)
                            LocalCfg_SelIndex = 0;
                        else if (LocalCfg_SelIndex == -1 && LocalCfg_Last_SelIndex != -1)
                            LocalCfg_SelIndex = LocalCfg_Last_SelIndex;
                        else
                            LocalCfg_Last_SelIndex = LocalCfg_SelIndex;
                        break;
                    }
                case 2:
                    {
                        Scan.ClearData();
                        Clear_Chart(true);
                        RadioButton_Reflectance.Enabled = true;
                        RadioButton_Absorbance.Enabled = true;
                        RadioButton_Reference.Enabled = true;
                        RadioButton_Intensity.Enabled = true;
                        Button_Scan.Visible = false;
                        Check_Overlay.CheckedChanged -= Check_Overlay_CheckedChanged;
                        Check_Overlay.Checked = true;
                        Check_Overlay.Visible = false;
                        Check_Overlay.CheckedChanged += Check_Overlay_CheckedChanged;
                        IsSavedScanData = true;
                        SavedScan_RefreshDataGridView();
                        break;
                    }
                default:
                    break;
            }

            if (checkBox_tooltip.Checked)
            {
                checkBox_tooltip.Checked = false;
                checkBox_tooltip.Checked = true;
            }

            if (lastTabScanPageSelection != 1 && (NewConfig == true || EditConfig == true))
                Button_CfgCancel_Click(this, e);

            lastTabScanPageSelection = tabScanPage.SelectedIndex;
        }

        private void UI_no_connection()
        {
            ControlAllControls(this, false);
            ControlSingleControl(panel_Saved_Scan, true);
            ControlPanelContents(panel_Saved_Scan, true);
            //ControlSingleControl(dataGridView_savescan.Controls["SelectAll"], true);
            ControlSingleControl(MyChart, true);
            ControlSingleControl(RadioButton_Reflectance, true);
            ControlSingleControl(RadioButton_Absorbance, true);
            ControlSingleControl(RadioButton_Intensity, true);
            ControlSingleControl(RadioButton_Reference, true);
            ControlSingleControl(checkBox_tooltip, true);
            ControlSingleControl(checkBox_zoom, true);
            ControlSingleControl(label_about_us, true);
            ControlSingleControl(label_license_agree, true);
            ControlSingleControl(button_AboutLicense, true);
            ControlSingleControl(button_About, true);
            ControlSingleControl(Label_TivaFWName, true);
            ControlSingleControl(TextBox_TivaFWPath, true);
            ControlSingleControl(Button_TivaFWBrowse, true);
            ControlSingleControl(label_GUIVersion, true);
            ControlSingleControl(lb_GUI_Revision, true);
            ControlSingleControl(label_DisableUACAlert, true);
            ControlSingleControl(button_disableUACAlert, true);
            ListBox_LocalCfgs.BackColor = System.Drawing.Color.White;
            ListBox_TargetCfgs.BackColor = System.Drawing.Color.White;
        }

        private void UI_On_Connection()
        {
            ControlAllControls(this, true);
            if (RadioButton_RefNew.Checked)
            {
                label_ContinuousMode.Visible = false;
                GroupBox_ContScan.Enabled = false;
                RadioButton_Reflectance.Enabled = false;
                RadioButton_Absorbance.Enabled = false;
                RadioButton_Reference.Enabled = false;
                RadioButton_LampOff.Enabled = false;
                Check_Overlay.Enabled = false;
                GroupBox_SaveScan.Enabled = false;
            }
            if (!RadioButton_RefNew.Checked && (int.TryParse(Text_ContScan.Text, out int repeat) && repeat > 1))
            {
                Manual_ContScan_UI_Con(!checkBox_AutoScan.Checked);
                CheckBox_SaveOneCSV.Enabled = true;
                CheckBox_AverageCSV.Enabled = true;
                checkBox_AutoScan.Enabled = true;
            }
            else
            {
                Manual_ContScan_UI_Con(Button_Scan.Text == "Scan Next");
                CheckBox_SaveOneCSV.Enabled = false;
                CheckBox_AverageCSV.Enabled = false;
                checkBox_StopOnError.Enabled = false;
                checkBox_AutoScan.Enabled = false;
            }
            CheckBox_FileNamePrefix_CheckedChanged(CheckBox_FileNamePrefix, null);
            CheckLampFuncUseful();
            if (RadioButton_LampOff.Checked)
                TextBox_LampStableTime.Enabled = false;
        }

        private void tabControl_MainFunctions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Button_Scan.Text == "Scan Next")
                button_ExitCont_Click(this, e);

            if (tabControl_MainFunctions.SelectedIndex != 0)
            {
                if (NewConfig == true || EditConfig == true)
                    Button_CfgCancel_Click(this, e);
                if (!RadioButton_LampStableTime.Checked)
                {
                    Scan.SetLamp(Scan.LAMP_CONTROL.OFF_SCAN);
                    Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
                }
            }
            else if (tabControl_MainFunctions.SelectedIndex == 0)
            {
                CheckBox_LampOn.Checked = false;
                RadioButton_LampStableTime.Checked = true;
                if (!RadioButton_LampStableTime.Checked)
                    Scan.SetLamp(Scan.LAMP_CONTROL.AUTO);
            }
        }

        private void ControlAllControls(Control con, bool enable)
        {
            foreach (Control c in con.Controls)
            {
                ControlAllControls(c, enable);
            }
            con.Enabled = enable;
        }

        private void ControlSingleControl(Control con, bool enable)
        {
            if (con != null)
            {
                con.Enabled = enable;
                ControlSingleControl(con.Parent, enable);
            }
        }

        private void ControlPanelContents(Panel panel, bool enabled)
        {
            foreach (Control ctrl in panel.Controls)
            {
                ctrl.Enabled = enabled;
            }
        }
        #region TIVA FW update timer
        private static int TivaUpdateTime = 1;
        static System.Threading.Timer timer;
        private static void TimerTask(object obj)
        {
            TivaUpdateTime++;
            if (TivaUpdateTime >= 30)
            {
                timer.Dispose();
                if (Device.IsConnected())
                    Device.ResetTiva(true);
                else
                    Device.ResetTiva(null);
                MessageBox.Show("Tiva FW Update timeout!", "Tiva FW Update");
            }
        }
        #endregion

        private void Check_Overlay_CheckedChanged(object sender, EventArgs e)
        {
            if (!(RadioButton_RefNew.Checked && tabScanPage.SelectedIndex == 0))
            {
                if (tabScanPage.SelectedIndex == 2 && !Check_Overlay.Checked)
                {
                    foreach (DataGridViewRow row in dataGridView_savescan.Rows)
                        row.Cells["Select"].Value = false;
                    dataGridView_savescan.Rows[dataGridView_savescan.CurrentCell.RowIndex].Cells["Select"].Value = true;
                }
                Clear_Chart(true);
                SpectrumPlot();
            }
        }
        private void Chart_Refresh()
        {
            Clear_Chart();
            MyChart.AxisX.Clear();
            MyChart.AxisY.Clear();

            String labelY = "";

            if (RadioButton_Intensity.Checked)
                labelY = "Intensity";
            else if (RadioButton_Reference.Checked)
                labelY = "Reference";
            else if (RadioButton_Absorbance.Checked)
                labelY = "Absorbance";
            else if (RadioButton_Reflectance.Checked)
                labelY = "Reflectance";

            MyChart.Series.Add(new GLineSeries
            {
                Values = new GearedValues<ObservablePoint>(),
                Title = labelY,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.Transparent,
                LineSmoothness = 0,
                PointGeometry = null,
                PointGeometrySize = 0,
            });

            MyChart.AxisX.Add(new Axis
            {
                Title = "Wavelength (nm)",
                MinValue = Device.DevInfo.MinWavelength,
                MaxValue = Device.DevInfo.MaxWavelength,
                Separator = new Separator
                {
                    Step = 50,
                    IsEnabled = false
                }
            });

            MyChart.AxisY.Add(new Axis { Title = labelY });
        }

        private void ListBox_TargetCfgs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ListBox_TargetCfgs.SelectedItems.Count < 1)
                return;

            if (IsCfgLegal(true) == SDK.RETURN_FAIL)
            {
                Message.ShowError("Apply the selected config failed!\n\nPlease fix it and retry again!");
                return;
            }

            SetScanConfig(ScanConfig.TargetConfig[TargetCfg_SelIndex], true, TargetCfg_SelIndex);

            if (userDefaultReference != ScanReference.Built_in)
                RadioButton_RefPre_CheckedChanged(null, null);
        }
        #region Local Config
        private void LoadLocalCfgList()
        {
            // Following is changed due to customer would like to use generic local config to copy to multiple devices
            /*
            String FileNameWithSuffix = BitConverter.ToString(Device.DevInfo.DeviceUUID).Replace("-", "");
            FileNameWithSuffix = "ConfigList_" + FileNameWithSuffix + ".xml";
            String FileName = Path.Combine(ConfigDir, FileNameWithSuffix);
            */
            String FileName = Path.Combine(ConfigDir, "ConfigList.xml");

            if (File.Exists(FileName) == true)
            {
                XmlSerializer xml = new XmlSerializer(typeof(List<ScanConfig.SlewScanConfig>));
                TextReader reader = new StreamReader(FileName);
                LocalConfig = (List<ScanConfig.SlewScanConfig>)xml.Deserialize(reader);
                reader.Close();
                RefreshLocalCfgList();
            }
            else
            {
                LocalConfig.Clear();
            }
        }
        private void RefreshLocalCfgList()
        {
            ListBox_LocalCfgs.Items.Clear();
            if (LocalConfig.Count > 0)
            {
                for (Int32 i = 0; i < LocalConfig.Count; i++)
                {
                    ListBox_LocalCfgs.Items.Add(LocalConfig[i].head.config_name);
                }
            }
        }

        private void ListBox_LocalCfgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsFetchingDeviceInfo || NewConfig || EditConfig) return;

            int selInx = -1;
            if (sender.GetType() == typeof(String))
                selInx = int.Parse((string)sender);

            isSelectingConfig = true;
            if (NewConfig == true || EditConfig == true)
            {
                EditConfig = false;
                NewConfig = false;
                Button_CfgCancel_Click(this, e);
                return;
            }

            if (selInx == -1)
            {
                LocalCfg_Last_SelIndex = LocalCfg_SelIndex;
                LocalCfg_SelIndex = ListBox_LocalCfgs.SelectedIndex;
            }
            else
            {
                LocalCfg_SelIndex = selInx;
            }

            if (LocalCfg_SelIndex < 0 || LocalConfig.Count == 0)
            {
                if (ListBox_TargetCfgs.SelectedIndex == -1 && ListBox_LocalCfgs.SelectedIndex == -1)//new config situation
                {
                    return;
                }
                else
                {
                    ListBox_LocalCfgs.BackColor = System.Drawing.Color.White;
                    ListBox_TargetCfgs.BackColor = System.Drawing.Color.AliceBlue;
                    Button_SetActive.Enabled = true;
                    return;
                }
            }
            else
            {
                if (!Scan.IsLocalRefExist && ReferenceSelect != Scan.SCAN_REF_TYPE.SCAN_REF_BUILT_IN)
                {
                    RadioButton_RefPre.Checked = false;
                    RadioButton_RefNew.Checked = true;
                }
            }

            SelCfg_IsTarget = false;
            FillCfgDetailsContent();
            OpenCloseScanConfigButton(nameof(ScanConfigMode.INITIAL));
            // Clear target listbox index after local config data refreshed.
            if (ListBox_TargetCfgs.SelectedIndex != -1)
            {
                ListBox_TargetCfgs.SelectedIndex = -1;
            }
            SetDetailColorWhite();
            isSelectingConfig = false;
            Update_Scan_Resolution_and_Pattern_Label();
        }
        private void ListBox_LocalCfgs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ListBox_LocalCfgs.SelectedItems.Count < 1)
                return;

            if (IsCfgLegal(true) == SDK.RETURN_FAIL)
            {
                Message.ShowError("Apply the selected config failed!\n\nPlease fix it and retry again!");
                return;
            }

            SetScanConfig(LocalConfig[LocalCfg_SelIndex], false, LocalCfg_SelIndex);

            if (userDefaultReference != ScanReference.Built_in)
                RadioButton_RefPre_CheckedChanged(null, null);
        }
        private void Button_CopyCfgL2T_Click(object sender, EventArgs e)
        {
            int targetIdx = 0, localIdx = 0;
            if (LocalCfg_SelIndex < 0)
            {
                Message.ShowWarning("No item selected.");
                return;
            }

            if (ListBox_LocalCfgs.SelectedItems.Count + ScanConfig.TargetConfig.Count > 20)
            {
                Message.ShowError("There is not enough space (total 20) to copy configs into device!");
                return;
            }

            SystemBusy(true);

            var selectedItems = new object[ListBox_LocalCfgs.SelectedItems.Count];
            ListBox_LocalCfgs.SelectedItems.CopyTo(selectedItems, 0);

            foreach (var item in selectedItems)
            {
                CheckConfigName(item.ToString(), true, ref localIdx);

                if (IsCfgValidForSaveToDevice(LocalConfig[localIdx]) == SDK.RETURN_FAIL)
                {
                    String msg = "The config - \"" + item.ToString() + "\" is not applicable for the device!\n\nIgnore the copy action!";
                    Message.ShowError(msg);
                    continue;
                }

                if (CheckConfigName(item.ToString(), false, ref targetIdx))
                {
                    String msg = "The config - \"" + item.ToString() + "\" has exist, do you want to overwrite?";
                    DialogResult Result = Message.ShowQuestion(msg, null, MessageBoxButtons.YesNo);
                    if (Result == DialogResult.No)
                        continue;
                    else if (Result == DialogResult.Yes)
                        ScanConfig.TargetConfig[targetIdx] = LocalConfig[localIdx];
                }
                else
                    ScanConfig.TargetConfig.Add(LocalConfig[localIdx]);
            }

            RefreshTargetCfgList();
            SaveCfgToLocalOrDevice(true);
            ListBox_LocalCfgs.SelectedItem = localIdx;
            SystemBusy(false);
        }

        private Int32 SaveCfgToLocalOrDevice(Boolean IsTarget)
        {
            Int32 ret = SDK.RETURN_FAIL;

            if (IsTarget == true)
            {
                if ((ret = ScanConfig.SetConfigList()) != SDK.RETURN_PASS)
                    Message.ShowError("Device Config List Wrote Failed!");
                if (EditConfig)
                {
                    ListBox_TargetCfgs.SelectedIndex = EditSelectIndex;
                }
            }
            else
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                    try { AddDirectorySecurity(ConfigDir); }
                    catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }
                }

                // Following is changed due to customer would like to use generic local config to copy to multiple devices
                /*
                String FileNameWithSuffix = BitConverter.ToString(Device.DevInfo.DeviceUUID).Replace("-", "");
                FileNameWithSuffix = "ConfigList_" + FileNameWithSuffix + ".xml";
                String FileName = Path.Combine(ConfigDir, FileNameWithSuffix);
                */
                String FileName = Path.Combine(ConfigDir, "ConfigList.xml");
                XmlSerializer xml = new XmlSerializer(typeof(List<ScanConfig.SlewScanConfig>));
                TextWriter writer = new StreamWriter(FileName);
                xml.Serialize(writer, LocalConfig);
                writer.Close();
                ret = SDK.RETURN_PASS;
                if (EditConfig)
                {
                    ListBox_LocalCfgs.SelectedIndex = EditSelectIndex;
                }
                else
                {
                    //ListBox_LocalCfgs.SelectedIndex = ListBox_LocalCfgs.Items.Count - 1;
                }
            }
            return ret;
        }
        private void Button_CopyCfgT2L_Click(object sender, EventArgs e)
        {
            int localIdx = 0, targetIdx = 0;
            if (TargetCfg_SelIndex < 0)
            {
                Message.ShowWarning("No item selected.");
                return;
            }

            SystemBusy(true);
            var selectedItems = new object[ListBox_TargetCfgs.SelectedItems.Count];
            ListBox_TargetCfgs.SelectedItems.CopyTo(selectedItems, 0);

            foreach (var item in selectedItems)
            {
                CheckConfigName(item.ToString(), false, ref targetIdx);

                if (CheckConfigName(item.ToString(), true, ref localIdx))
                {
                    String msg = "The config - \"" + item.ToString() + "\" has exist, do you want to overwrite?";
                    DialogResult Result = Message.ShowQuestion(msg, null, MessageBoxButtons.YesNo);
                    if (Result == DialogResult.No)
                        continue;
                    else if (Result == DialogResult.Yes)
                        LocalConfig[localIdx] = ScanConfig.TargetConfig[targetIdx];
                }
                else
                    LocalConfig.Add(ScanConfig.TargetConfig[targetIdx]);
            }

            RefreshLocalCfgList();
            SaveCfgToLocalOrDevice(false);
            ListBox_TargetCfgs.SelectedIndex = targetIdx;
            SystemBusy(false);
        }
        private void Button_MoveCfgL2T_Click(object sender, EventArgs e)
        {
            int targetIdx = 0, lastIdx = 0;
            if (LocalCfg_SelIndex < 0)
            {
                Message.ShowWarning("No item selected.");
                return;
            }

            if (ListBox_LocalCfgs.SelectedItems.Count + ScanConfig.TargetConfig.Count > 20)
            {
                Message.ShowError("There is not enough space (total 20) to copy configs into device!");
                return;
            }

            SystemBusy(true);

            var selectedItems = new object[ListBox_LocalCfgs.SelectedItems.Count];
            List<string> itemToRemove = new List<string>();
            ListBox_LocalCfgs.SelectedItems.CopyTo(selectedItems, 0);

            foreach (var item in selectedItems)
            {
                int localIdx = 0;
                CheckConfigName(item.ToString(), true, ref localIdx);

                if (IsCfgValidForSaveToDevice(LocalConfig[localIdx]) == SDK.RETURN_FAIL)
                {
                    String msg = "The config - \"" + item.ToString() + "\" is not applicable for the device!\n\nIgnore the move action!";
                    Message.ShowError(msg);
                    continue;
                }

                if (CheckConfigName(item.ToString(), false, ref targetIdx))
                {
                    String msg = "The config - \"" + item.ToString() + "\" is existed, do you want to overwrite?";
                    DialogResult Result = Message.ShowQuestion(msg, null, MessageBoxButtons.YesNo);
                    if (Result == DialogResult.No)
                        continue;
                    else if (Result == DialogResult.Yes)
                    {
                        ScanConfig.TargetConfig[targetIdx] = LocalConfig[localIdx];
                        itemToRemove.Add(item.ToString());
                        lastIdx = targetIdx;
                    }
                }
                else
                {
                    ScanConfig.TargetConfig.Add(LocalConfig[localIdx]);
                    itemToRemove.Add(item.ToString());
                    lastIdx = ScanConfig.TargetConfig.Count - 1;
                }
            }

            foreach (var item in itemToRemove)
            {
                for (int i = 0; i < LocalConfig.Count; i++)
                {
                    var cfg = LocalConfig[i];
                    if (cfg.head.config_name == item)
                    {
                        LocalConfig.RemoveAt(i);
                        break;
                    }
                }
            }

            RefreshLocalCfgList();
            SaveCfgToLocalOrDevice(false);
            RefreshTargetCfgList();
            SaveCfgToLocalOrDevice(true);
            ListBox_TargetCfgs.SelectedIndex = lastIdx;
            SystemBusy(false);
        }
        private void Button_MoveCfgT2L_Click(object sender, EventArgs e)
        {
            int localIdx = 0, lastIdx = 0;
            if (TargetCfg_SelIndex < 0)
            {
                Message.ShowWarning("No item selected.");
                return;
            }

            if (ListBox_TargetCfgs.SelectedItems.Count == ListBox_TargetCfgs.Items.Count)
            {
                Message.ShowError("The device configuration cannot be empty after move.\n\nPlease leave one configuration at least!");
                return;
            }

            SystemBusy(true);
            int activeCfgIdx = ScanConfig.GetTargetActiveScanIndex();
            string activeCfgName = ScanConfig.TargetConfig[activeCfgIdx].head.config_name;
            bool activeCfgDeleted = false;

            var selectedItems = new object[ListBox_TargetCfgs.SelectedItems.Count];
            List<string> itemToRemove = new List<string>();
            ListBox_TargetCfgs.SelectedItems.CopyTo(selectedItems, 0);

            foreach (var item in selectedItems)
            {
                int targetIdx = 0;

                CheckConfigName(item.ToString(), false, ref targetIdx);

                if (CheckConfigName(item.ToString(), true, ref localIdx))
                {
                    String msg = "The config - \"" + item.ToString() + "\" is existed, do you want to overwrite?";
                    DialogResult Result = Message.ShowQuestion(msg, null, MessageBoxButtons.YesNo);
                    if (Result == DialogResult.No)
                        continue;
                    else if (Result == DialogResult.Yes)
                    {
                        LocalConfig[localIdx] = ScanConfig.TargetConfig[targetIdx];
                        itemToRemove.Add(item.ToString());
                        lastIdx = localIdx;
                    }
                }
                else
                {
                    LocalConfig.Add(ScanConfig.TargetConfig[targetIdx]);
                    itemToRemove.Add(item.ToString());
                    lastIdx = LocalConfig.Count - 1;
                }
            }

            foreach (var item in itemToRemove)
            {
                for (int i = 0; i < ScanConfig.TargetConfig.Count; i++)
                {
                    if (item.ToString() == activeCfgName)
                        activeCfgDeleted = true;

                    var cfg = ScanConfig.TargetConfig[i];
                    if (cfg.head.config_name == item)
                    {
                        ScanConfig.TargetConfig.RemoveAt(i);
                        break;
                    }
                }
            }

            if (activeCfgDeleted)
            {
                ScanConfig.SetTargetActiveScanIndex(0);
                SetScanConfig(ScanConfig.TargetConfig[0], true, 0);
            }

            RefreshTargetCfgList();
            SaveCfgToLocalOrDevice(true);
            RefreshLocalCfgList();
            SaveCfgToLocalOrDevice(false);
            ListBox_LocalCfgs.SelectedIndex = lastIdx;
            SystemBusy(false);
        }
        private Boolean CheckConfigName(String name, Boolean checklocal, ref int index)
        {
            Boolean isExist = false;
            if (checklocal)
            {
                for (int i = 0; i < ListBox_LocalCfgs.Items.Count; i++)
                {
                    if (name == ListBox_LocalCfgs.Items[i].ToString())
                    {
                        isExist = true;
                        index = i;
                        return isExist;
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListBox_TargetCfgs.Items.Count; i++)
                {
                    if (name == ListBox_TargetCfgs.Items[i].ToString())
                    {
                        isExist = true;
                        index = i;
                        return isExist;
                    }
                }
            }
            return isExist;
        }
        #endregion
        private static FW_LEVEL thisFwLevel = FW_LEVEL.LEVEL_0;
        public static FW_LEVEL GetFW_LEVEL()
        {
            return thisFwLevel;
        }
        public static FW_LEVEL GetFW_LEVEL(bool renew)
        {
            if (!renew)
                return thisFwLevel;

            if (Device.IsConnected())
            {
                String HWRev = String.Empty;
                UInt32 curVer = 0;

                curVer = (UInt32)Device.DevInfo.TivaRev[0] << 16 | (UInt32)Device.DevInfo.TivaRev[1] << 8 | Device.DevInfo.TivaRev[2];
                HWRev = (!String.IsNullOrEmpty(Device.DevInfo.HardwareRev)) ? Device.DevInfo.HardwareRev.Substring(0, 1) : String.Empty;

                if (HWRev != "D" && HWRev != "B" && HWRev != "F" && HWRev != "O" && HWRev != "E" && HWRev != "N")
                {
                    /* 
                     * TI EVM Board 
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_0;
                }
                else if (curVer >= (3 << 16 | 5 << 8 | 3))  // >= 3.5.3
                {
                    /*
                     * New Applications:
                     * 1. Support model with fan
                     * 2. Fix system error code parser
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_6;
                }
                else if (curVer >= (3 << 16 | 3 << 8 | 0))  // >= 3.3.0
                {
                    /*
                     * Extended version
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_5;
                }
                else if (curVer >= (2 << 16 | 5 << 8 | 2))  // >= 2.5.2
                {
                    /*
                     * New Applications:
                     * 1. Fix system error code parser
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_7;
                }
                else if (curVer >= (2 << 16 | 4 << 8 | 0))  // >= 2.4.0
                {
                    /*
                     * New Applications:
                     * 1. Support H/W Ver.F to store the four lamp ADC values
                     * 2. Bluetooth LE Advertising Name Read/Write
                     * 3. Button Lock/Unlock
                     * 4. Update error status and error code
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_4;
                }
                else if (curVer >= (2 << 16 | 1 << 8 | 2))  // >= 2.1.2
                {
                    /*
                     * New Applications:
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_3;
                }
                else if (curVer >= (2 << 16 | 1 << 8 | 0))  // >= 2.1.0.X
                {
                    /*
                     * New Applications:
                     * 1. Manufacture Serial Number Read
                     * 2. Activation Key Read/Write
                     * 3. Auto PGA Gain in Lamp On/Off
                     * 4. Check BLE Board Exist
                     * 5. Restore Calibration Coefficients
                     * 6. Lamp Usage Read/Write
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_2;
                }
                else if (curVer <= (2 << 16 | 0 << 8 | 22))  // <= 2.0.22
                {
                    /*
                     * New Applications:
                     * 1. Model Name Read/Write
                     * 2. Fixed PGA Gain Control
                     * 3. Flash UUID Read
                     */
                    thisFwLevel = FW_LEVEL.LEVEL_1;
                }
            }

            return thisFwLevel;
        }
        private void ClearScanPlotsUI()
        {
            Scan.Intensity.Clear();
            Scan.ReferenceIntensity.Clear();
            Scan.Reflectance.Clear();
            Scan.Absorbance.Clear();
            Label_ScanStatus.Text = String.Empty;
            Label_CurrentConfig.Text = String.Empty;
            Label_EstimatedScanTime.Text = String.Empty;
        }
        private void OpenCloseScanConfigButton(String mode)
        {
            switch (mode)
            {
                case nameof(ScanConfigMode.INITIAL):
                    Button_CfgNew.Enabled = true;
                    Button_CfgEdit.Enabled = true;
                    Button_CfgDelete.Enabled = true;
                    Button_CfgSave.Enabled = false;
                    Button_CfgCancel.Enabled = false;
                    break;
                case nameof(ScanConfigMode.NEW):
                    Button_CfgNew.Enabled = false;
                    Button_CfgEdit.Enabled = false;
                    Button_CfgDelete.Enabled = false;
                    Button_CfgSave.Enabled = true;
                    Button_CfgCancel.Enabled = true;
                    break;
                case nameof(ScanConfigMode.EDIT):
                    Button_CfgNew.Enabled = false;
                    Button_CfgEdit.Enabled = false;
                    Button_CfgDelete.Enabled = false;
                    Button_CfgSave.Enabled = true;
                    Button_CfgCancel.Enabled = true;
                    break;
                case nameof(ScanConfigMode.DELETE):
                    Button_CfgNew.Enabled = false;
                    Button_CfgEdit.Enabled = false;
                    Button_CfgSave.Enabled = false;
                    Button_CfgCancel.Enabled = false;
                    break;
                case nameof(ScanConfigMode.SAVE):
                    Button_CfgNew.Enabled = false;
                    Button_CfgEdit.Enabled = false;
                    Button_CfgDelete.Enabled = false;
                    Button_CfgCancel.Enabled = true;
                    break;
                case nameof(ScanConfigMode.CANCEL):
                    Button_CfgNew.Enabled = true;
                    Button_CfgEdit.Enabled = true;
                    Button_CfgDelete.Enabled = true;
                    Button_CfgSave.Enabled = false;
                    Button_CfgCancel.Enabled = false;
                    break;
            }
        }
        private void SaveSettings()
        {
            /*
             * <?xml version="1.0" encoding="utf-8"?>
             * <Settings>
             *   <ScanDir>     Scan_Dir     </ScanDir>
             *   <DisplayDir>  Display_Dir  </DisplayDir>
             *   <FileFormats> ScanFile_Formats </FileFormats>
             * </Settings>
             */

            if (Dir_Scan_For_New == String.Empty && Dir_Scan_DataBase == String.Empty && ScanFile_Formats == 0)
                return;

            XmlDocument XmlDoc = new XmlDocument();
            XmlDeclaration XmlDec = XmlDoc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlDoc.PrependChild(XmlDec);

            // Create root element
            XmlElement Root = XmlDoc.CreateElement("Settings");
            XmlDoc.AppendChild(Root);

            // Create scan dir node under root element
            XmlElement ScanDir = XmlDoc.CreateElement("ScanDir");
            ScanDir.AppendChild(XmlDoc.CreateTextNode(Dir_Scan_For_New));
            Root.AppendChild(ScanDir);

            // Create display dir node under root element
            XmlElement DisplayDir = XmlDoc.CreateElement("DisplayDir");
            DisplayDir.AppendChild(XmlDoc.CreateTextNode(Dir_Scan_DataBase));
            Root.AppendChild(DisplayDir);

            // Create file format node under root element
            XmlElement FileFormats = XmlDoc.CreateElement("FileFormats");
            FileFormats.AppendChild(XmlDoc.CreateTextNode(ScanFile_Formats.ToString()));
            Root.AppendChild(FileFormats);

            // Create csv delimiter node under root element
            string delimiterString = ""; 
            if (CSV_Delimiter == "\t")
                delimiterString = "TAB";
            else
                delimiterString = CSV_Delimiter;
            XmlElement CSVDelimiter = XmlDoc.CreateElement("CSVDelimiter");
            CSVDelimiter.AppendChild(XmlDoc.CreateTextNode(delimiterString));
            Root.AppendChild(CSVDelimiter);

            // Save XML file
            try
            {
                String FilePath = Path.Combine(ConfigDir, "ScanPageSettings.xml");
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                String dirpath = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                    try { AddDirectorySecurity(dirpath); }
                    catch (Exception ex) { DBG.WriteLine(ex.Message); logFile.Error(ex.Message); }
                }
                XmlDoc.Save(FilePath);
            }
            catch (UnauthorizedAccessException UAEx) { DBG.WriteLine(UAEx.Message); logFile.Error(UAEx.Message); }
            catch (PathTooLongException PathEx) { DBG.WriteLine(PathEx.Message); logFile.Error(PathEx.Message); }
            catch (Exception e) { DBG.WriteLine(e.Message); logFile.Error(e.Message); }
        }

        private void Text_ContScan_TextChanged(object sender, EventArgs e)
        {
            if (t_PBW.IsAlive)
                return;

            if (int.TryParse(Text_ContScan.Text, out int repeat) && repeat > 1)
            {
                Manual_ContScan_UI_Con(!checkBox_AutoScan.Checked);
                CheckBox_SaveOneCSV.Enabled = true;
                CheckBox_SaveOneCSV.Checked = true;
                CheckBox_AverageCSV.Enabled = true;
                CheckBox_AverageCSV.Checked = true;
                checkBox_AutoScan.Enabled = true;
            }
            else
            {
                Manual_ContScan_UI_Con(false);
                CheckBox_SaveOneCSV.Enabled = false;
                CheckBox_SaveOneCSV.Checked = false;
                CheckBox_AverageCSV.Enabled = false;
                CheckBox_AverageCSV.Checked = false;
                checkBox_StopOnError.Enabled = false;
                checkBox_AutoScan.Enabled = false;
            }
        }

        private void Button_ClearAllErrors_Click(object sender, EventArgs e)
        {
            if (!Device.IsConnected())
                return;

            if (Device.ResetErrorStatus() == 0)
                RefreshErrorStatus();
        }

        private void CheckBox_SaveFileFormat_Click(object sender, System.EventArgs e)
        {
            if (!AppLoaded) return;

            var checkBox = sender as CheckBox;

            if (checkBox.Name.ToString() == "CheckBox_SaveCombCSV")
            {
                if (checkBox.Checked == false)
                {
                    if (SaveCSV_Click_Counts++ < 9)
                        checkBox.Checked = true;
                }
                //if (CheckBox_SaveDAT.Checked == false && CheckBox_SaveCombCSV.Checked == false)
                //{
                //    DialogResult Result = Message.ShowQuestion("Are you sure to cancel saving both *.dat and *.csv?\n" +
                //                                                      "Your scan result will not be fully saved.", null, MessageBoxButtons.YesNo);
                //    if (Result == DialogResult.No)
                //        CheckBox_SaveCombCSV.Checked = true;
                //}
            }
            else if (checkBox.Name.ToString() == "CheckBox_SaveDAT")
            {
                if (checkBox.Checked == false)
                {
                    if (SaveDAT_Click_Counts++ < 9)
                        checkBox.Checked = true;
                }
                //if (CheckBox_SaveDAT.Checked == false && CheckBox_SaveCombCSV.Checked == false)
                //{
                //    DialogResult Result = Message.ShowQuestion("Are you sure to cancel saving both *.dat and *.csv?\n" +
                //                                                      "Your scan result will not be fully saved.", null, MessageBoxButtons.YesNo);
                //    if (Result == DialogResult.No)
                //        CheckBox_SaveDAT.Checked = true;
                //}
                //else if (CheckBox_SaveDAT.Checked == false && CheckBox_SaveCombCSV.Checked == true)
                //{
                //    CheckBox_SaveDAT.Checked = true;
                //}
            }
            else if (checkBox.Name.ToString() == "CheckBox_SaveJDX")
            {
                CheckBox_SaveIJDX.CheckedChanged -= CheckBox_SaveFileFormat_Click;
                CheckBox_SaveAJDX.CheckedChanged -= CheckBox_SaveFileFormat_Click;
                CheckBox_SaveRJDX.CheckedChanged -= CheckBox_SaveFileFormat_Click;
                if (checkBox.Checked == true)
                {
                    CheckBox_SaveIJDX.Checked = true;
                    CheckBox_SaveAJDX.Checked = true;
                    CheckBox_SaveRJDX.Checked = true;
                }
                else
                {
                    CheckBox_SaveIJDX.Checked = false;
                    CheckBox_SaveAJDX.Checked = false;
                    CheckBox_SaveRJDX.Checked = false;
                }
                CheckBox_SaveIJDX.CheckedChanged += CheckBox_SaveFileFormat_Click;
                CheckBox_SaveAJDX.CheckedChanged += CheckBox_SaveFileFormat_Click;
                CheckBox_SaveRJDX.CheckedChanged += CheckBox_SaveFileFormat_Click;
            }
            else if (checkBox.Name.ToString() == "CheckBox_SaveIJDX" || checkBox.Name.ToString() == "CheckBox_SaveAJDX" || checkBox.Name.ToString() == "CheckBox_SaveRJDX")
            {
                CheckBox_SaveJDX.CheckedChanged -= CheckBox_SaveFileFormat_Click;
                if (CheckBox_SaveJDX.Checked == false && (CheckBox_SaveIJDX.Checked == true || CheckBox_SaveAJDX.Checked == true || CheckBox_SaveRJDX.Checked == true))
                    CheckBox_SaveJDX.Checked = true;
                else if (CheckBox_SaveJDX.Checked == true && CheckBox_SaveIJDX.Checked == false && CheckBox_SaveAJDX.Checked == false && CheckBox_SaveRJDX.Checked == false)
                    CheckBox_SaveJDX.Checked = false;
                CheckBox_SaveJDX.CheckedChanged += CheckBox_SaveFileFormat_Click;
            }

            //ScanFile_Formats = (CheckBox_SaveCombCSV.Checked == true) ? (ScanFile_Formats | 0x01) : (ScanFile_Formats & (~0x01));
            ScanFile_Formats = (CheckBox_SaveICSV.Checked == true) ? (ScanFile_Formats | 0x02) : (ScanFile_Formats & (~0x02));
            ScanFile_Formats = (CheckBox_SaveACSV.Checked == true) ? (ScanFile_Formats | 0x04) : (ScanFile_Formats & (~0x04));
            ScanFile_Formats = (CheckBox_SaveRCSV.Checked == true) ? (ScanFile_Formats | 0x08) : (ScanFile_Formats & (~0x08));
            ScanFile_Formats = (CheckBox_SaveIJDX.Checked == true) ? (ScanFile_Formats | 0x10) : (ScanFile_Formats & (~0x10));
            ScanFile_Formats = (CheckBox_SaveAJDX.Checked == true) ? (ScanFile_Formats | 0x20) : (ScanFile_Formats & (~0x20));
            ScanFile_Formats = (CheckBox_SaveRJDX.Checked == true) ? (ScanFile_Formats | 0x40) : (ScanFile_Formats & (~0x40));
            //ScanFile_Formats = (CheckBox_SaveDAT.Checked == true) ? (ScanFile_Formats | 0x80) : (ScanFile_Formats & (~0x80));
        }

        private Int32 SetDefaultConfig()
        {
            List<ScanConfig.SlewScanConfig> CurConfig = ScanConfig.GetDefaultConfig();

            ScanConfig.TargetConfig.Clear();
            for (int i = 0; i < CurConfig.Count; i++)
            {
                ScanConfig.TargetConfig.Add(CurConfig[i]);
            }

            int ret;
            if ((ret = ScanConfig.SetConfigList()) == 0)
            {
                int retIdx = ScanConfig.SetDefaultActiveConfig();
                if (retIdx >= 0)
                { // If retrun a valid config index
                    SetScanConfig(ScanConfig.TargetConfig[retIdx], true, retIdx); //Apply the config
                    this.Invoke(new Action(() =>
                    {
                        RefreshTargetCfgList();
                        ListBox_TargetCfgs_MouseClick(this, null);
                        ListBox_TargetCfgs.SelectedIndex = retIdx;
                    }));
                }
            }
            else
            {
                ScanConfig.TargetConfig.Clear();
            }
            return ret;
        }

        private ProgressBar PBW;
        public static event Action RequestPBWClose = null;
        internal static bool SendPBWClose { set { RequestPBWClose(); } }
        private Thread t_PBW;
        public static event Action<String> RequestPBWContentChange = null;
        internal static String SendPBWContentChange { set { RequestPBWContentChange(value); } }

        private void ProgressWindowStart(String title, String content, Boolean cancellable)
        {
            SystemBusy(true);

            if (t_PBW != null && t_PBW.IsAlive)
            {
                ProgressWindow(title, content, cancellable);
            }
            else
            {
                try
                {
                    t_PBW = new Thread(() =>
                    {
                        ProgressWindow(title, content, cancellable);
                    });
                    t_PBW.IsBackground = true;
                    t_PBW.Start();
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("Thread - caught ThreadAbortException.");
                    Console.WriteLine("Exception message: {0}", e.Message);
                }
            }
        }

        private void ProgressWindow(String title, String content, Boolean cancellable)
        {
            PBW = new ProgressBar(title, content, cancellable, formResize.form_ratio_width, formResize.form_ratio_height) { };
            PBW.StartPosition = FormStartPosition.CenterScreen;
            PBW.TopMost = true;
            Application.Run(PBW);
            PBW.BringToFront();
        }

        private void ProgressWindowContentUpdate(String newContent)
        {
            if (t_PBW != null && t_PBW.IsAlive != false)
            {
                SendPBWContentChange = newContent;
            }
        }

        private void ProgressWindowCompleted()
        {
            if (PBW != null)
            {
                PBW.TopMost = false;
                PBW.Close();
                PBW.Dispose();
            }

            this.Invoke(new Action(() =>
            {
                this.Activate();
                SystemBusy(false);
                this.TopMost = true;
                this.BringToFront();
                this.TopMost = false;
            }));
        }

        private void checkBox_tooltip_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_tooltip.Checked)
            {
                rb_tooltip4single.Enabled = false;
                rb_tooltip4multi.Enabled = false;

                TooltipSelectionMode ttsm = new TooltipSelectionMode();
                if (rb_tooltip4single.Checked)
                    ttsm = TooltipSelectionMode.SharedXInSeries;
                else
                    ttsm = TooltipSelectionMode.SharedXValues;

                if (Tooltips_Show_Details)
                {
                    MyChart.DataTooltip = new CustomersTooltip()
                    {
                        SelectionMode = ttsm,
                    };
                }
                else
                {
                    MyChart.DataTooltip = new DefaultTooltip()
                    {

                        SelectionMode = ttsm,
                    };
                }

                MyChart.Hoverable = true;
                rb_tooltip4single.Enabled = false;
                rb_tooltip4multi.Enabled = false;
            }
            else
            {
                MyChart.DataTooltip = null;
                MyChart.Hoverable = false;
                rb_tooltip4single.Enabled = true;
                rb_tooltip4multi.Enabled = true;
            }
        }

        private void checkBox_zoom_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_zoom.Checked)
            {
                MyChart.Zoom = userZoomOption;
            }
            else
            {
                MyChart.Zoom = ZoomingOptions.None;
                MyChart.AxisX[0].MinValue = double.NaN;
                MyChart.AxisX[0].MaxValue = double.NaN;
                MyChart.AxisY[0].MinValue = double.NaN;
                MyChart.AxisY[0].MaxValue = double.NaN;
            }
        }

        private void ListBox_LocalCfgs_MouseClick(object sender, MouseEventArgs e)
        {
            if (ListBox_LocalCfgs.Items.Count == 0)
            {
                ListBox_LocalCfgs.BackColor = System.Drawing.Color.AliceBlue;
                ListBox_TargetCfgs.BackColor = System.Drawing.Color.White;
                ListBox_TargetCfgs.SelectedIndex = -1;
                Button_CfgEdit.Enabled = false;
                Button_CfgDelete.Enabled = false;
                ClearDetailValue();
                SelCfg_IsTarget = true;
            }
            else
            {
                ListBox_LocalCfgs.BackColor = System.Drawing.Color.AliceBlue;
                ListBox_TargetCfgs.BackColor = System.Drawing.Color.White;
                SelCfg_IsTarget = false;
            }
        }

        private void ListBox_TargetCfgs_MouseClick(object sender, MouseEventArgs e)
        {
            ListBox_LocalCfgs.BackColor = System.Drawing.Color.White;
            ListBox_TargetCfgs.BackColor = System.Drawing.Color.AliceBlue;
            SelCfg_IsTarget = true;
        }

        private void ClearDetailValue()
        {
            TextBox_CfgName.Text = "";
            TextBox_CfgAvg.Text = "";
            for (int i = 0; i < 5; i++)
            {
                TextBox_CfgRangeStart[i].Text = "";
                TextBox_CfgRangeEnd[i].Text = "";
                TextBox_CfgDigRes[i].Text = "";
                Label_overSampleRate[i].Text = "";
            }
        }

        private void Text_ContScan_Validated(object sender, EventArgs e)
        {
            ulong scanNum;
            if (!ulong.TryParse(Text_ContScan.Text, out scanNum))
            {
                Message.ShowError("Continuous scan input error!", "Input Error");
                Text_ContScan.Text = "1";
            }
        }

        private void button_restore_fac_ref_warning_Click(object sender, EventArgs e)
        {
            Message.ShowWarning(RestoreFacRef_Msg);
        }

        private void CheckBox_FileNamePrefix_CheckedChanged(object sender, EventArgs e)
        {
            String senderName = sender.GetType().Name;
            if (senderName != "CheckBox")
            {
                TextBox_FileNamePrefix1.Enabled = false;
                TextBox_FileNamePrefix2.Enabled = false;
                TextBox_FileNamePrefix3.Enabled = false;
                return;
            }

            var cbPrefix = (CheckBox)sender;
            if (cbPrefix.Checked)
            {
                TextBox_FileNamePrefix1.Enabled = true;
                TextBox_FileNamePrefix2.Enabled = true;
                TextBox_FileNamePrefix3.Enabled = true;
            }
            else
            {
                TextBox_FileNamePrefix1.Enabled = false;
                TextBox_FileNamePrefix2.Enabled = false;
                TextBox_FileNamePrefix3.Enabled = false;
            }
        }

        // For activation key managemant
        public class ActivationKey
        {
            public ActivationKey() { }
            public ActivationKey(String serNum, String aKey)
            {
                SerNum = serNum;
                AKey = aKey;
            }
            public String SerNum { get; set; }
            public String AKey { get; set; }
        }

        public IEnumerable<object> ReadAKeyFromFile()
        {
            String FileName = Path.Combine(ConfigDir, "ActivationKey.xml");
            DBG.WriteLine("Read key pairs from {0}", FileName);
            logFile.InfoFormat("Read key pairs from {0}", FileName);
            List<ActivationKey> rows = new List<ActivationKey>();

            if (File.Exists(FileName))
            {
                XmlSerializer xml = new XmlSerializer(typeof(List<ActivationKey>));
                TextReader reader = new StreamReader(FileName);
                rows = (List<ActivationKey>)xml.Deserialize(reader);
                reader.Close();
            }
            return rows;
        }
        public void SaveAKeyToFile(IEnumerable<object> rows)
        {
            // Delete old file if existed
            String OldFileName = Path.Combine(ConfigDir, "ActictionKey.xml");
            if (File.Exists(OldFileName))
                File.Delete(OldFileName);

            String FileName = Path.Combine(ConfigDir, "ActivationKey.xml");
            DBG.WriteLine("Save key pairs to {0}", FileName);
            logFile.InfoFormat("Save key pairs to {0}", FileName);

            // Save data to file
            XmlSerializer xml = new XmlSerializer(typeof(List<ActivationKey>));
            TextWriter writer = new StreamWriter(FileName);
            xml.Serialize(writer, rows);
            writer.Close();
        }

        public void AutoSetKey()
        {
            if (!IsActivated)
            {
                foreach (ActivationKey row in ReadAKeyFromFile())
                {
                    string[] arr = new string[2];
                    arr[0] = row.SerNum;
                    arr[1] = row.AKey;
                    if (row.SerNum == Device.DevInfo.SerialNumber)
                    {
                        String[] StrKey = arr[1].Split(new char[] { ' ', ':', ';', '-', '_' });
                        Byte[] ByteKey = new Byte[12];
                        for (int i = 0; i < StrKey.Length; i++)
                        {
                            try { ByteKey[i] = Convert.ToByte(StrKey[i], 16); }
                            catch { ByteKey[i] = 0; }
                        }
                        Device.SetActivationKey(ByteKey);
                        if (IsActivated)
                            GUI_Handler((int)GUI_State.KEY_ACTIVATE);
                    }
                }
            }
        }

        public static Control FindFocusedControl(Control control)
        {
            var container = control as ContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as ContainerControl;
            }
            return control;
        }

        private bool inputOverLengthLimit = false;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            int charNumLimit = 0;

            inputOverLengthLimit = false;

            if (textBox.Name == "TextBox_SerialNumber")
                charNumLimit = 8;
            else if (textBox.Name == "TextBox_ModelName")
                charNumLimit = 15;
            else if (textBox.Name == "TextBox_BLE_Display_Name")
                charNumLimit = 23;

            if (charNumLimit > 0 && textBox.Text.Length >= charNumLimit &&
                e.KeyCode != Keys.Home && e.KeyCode != Keys.End && e.KeyCode != Keys.Delete && e.KeyCode != Keys.Back &&
                 e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down &&
                  !(e.Control && e.KeyCode == Keys.A) && !(e.Control && e.KeyCode == Keys.Z) && !(e.Control && e.Shift && e.KeyCode == Keys.Z) &&
                   !(e.Control && e.KeyCode == Keys.C) && !(e.Control && e.KeyCode == Keys.V) && !(e.Control && e.KeyCode == Keys.X) &&
                    e.KeyCode != Keys.Tab && e.KeyCode != Keys.CapsLock && e.KeyCode != Keys.LWin && e.KeyCode != Keys.RWin)
            {
                inputOverLengthLimit = true;
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (inputOverLengthLimit)
                e.Handled = true;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            int charNumLimit = 0;

            if (textBox.Text == "")
                return;

            if (textBox.Name == "TextBox_SerialNumber")
                charNumLimit = 8;
            else if (textBox.Name == "TextBox_ModelName")
                charNumLimit = 15;
            else if (textBox.Name == "TextBox_BLE_Display_Name")
                charNumLimit = 26;
            else if (textBox.Name.Contains("TextBox_FileNamePrefix1"))
                charNumLimit = 200 - TextBox_FileNamePrefix2.Text.Length - TextBox_FileNamePrefix3.Text.Length;
            else if (textBox.Name.Contains("TextBox_FileNamePrefix2"))
                charNumLimit = 200 - TextBox_FileNamePrefix1.Text.Length - TextBox_FileNamePrefix3.Text.Length;
            else if (textBox.Name.Contains("TextBox_FileNamePrefix3"))
                charNumLimit = 200 - TextBox_FileNamePrefix1.Text.Length - TextBox_FileNamePrefix2.Text.Length;
            else if (textBox.Name.Contains("TextBox_CfgName"))
                charNumLimit = 40;

            int cursorLoc = textBox.SelectionStart, count = 0;
            if (textBox.Name == "TextBox_BLE_Display_Name")
                textBox.Text = Regex.Replace(textBox.Text, @"[^0-9a-zA-Z\-\<\>_.#@ ]+", m => { count++; return ""; });
            else
                textBox.Text = Regex.Replace(textBox.Text, @"[^0-9a-zA-Z\-_.#@ ]+", m => { count++; return ""; });
            textBox.SelectionStart = count >= 1 ? cursorLoc - 1 : cursorLoc;

            if (charNumLimit > 0 && textBox.Text.Length >= charNumLimit)
                textBox.Text = textBox.Text.Substring(0, charNumLimit);
        }

        private void Label_CurrentConfig_MouseClick(object sender, MouseEventArgs e)
        {
            if (Button_Scan.Text == "Continuous")
                return;

            if (e != null) //&& e.Button == MouseButtons.Right)
            {
                ContextMenuStrip m = new ContextMenuStrip();

                var localCfgItems = new object[ListBox_LocalCfgs.Items.Count];
                var targetCfgItems = new object[ListBox_TargetCfgs.Items.Count];

                ListBox_TargetCfgs.Items.CopyTo(targetCfgItems, 0);
                ListBox_LocalCfgs.Items.CopyTo(localCfgItems, 0);

                m.Items.Add("[Add new LOCAL config]");
                m.Items[0].Font = new Font(m.Items[0].Font.FontFamily, m.Items[0].Font.Size, FontStyle.Italic);
                m.Items.Add("[Add new DEVICE config]");
                m.Items[1].Font = new Font(m.Items[1].Font.FontFamily, m.Items[1].Font.Size, FontStyle.Italic);

                m.Items.Add(new ToolStripSeparator());

                foreach (var item in targetCfgItems)
                    m.Items.Add("Device: " + item.ToString());

                m.Items.Add(new ToolStripSeparator());

                foreach (var item in localCfgItems)
                    m.Items.Add("Local: " + item.ToString());

                try
                {
                    for (int i = 0; i < m.Items.Count; i++)
                    {
                        ToolStripMenuItem menuItem = m.Items[i] as ToolStripMenuItem;
                        string curCfgName = Label_CurrentConfig.Text;

                        if (menuItem == null)
                            continue;

                        int foundS1 = curCfgName.IndexOf("Current");
                        int foundS2 = curCfgName.IndexOf(":");
                        curCfgName = curCfgName.Remove(foundS1, foundS2 - foundS1 + 2);
                        curCfgName = curCfgName.Replace(" ->", ":");

                        if (menuItem.Text == curCfgName)
                        {
                            ((ToolStripMenuItem)m.Items[i]).Checked = true;
                            break;
                        }
                    }
                }
                catch { }

                m.MouseWheel += rootItem_MouseWheel;
                m.Opened += rootItem_DropDownOpened;
                m.Closed += rootItem_DropDownClosed;
                m.ItemClicked += new ToolStripItemClickedEventHandler(Label_CurrentConfig_ContexMenu_ItemClicked);
                m.MaximumSize = new Size(this.Width / 2, this.Height / 4);
                m.Show(Label_CurrentConfig, new Point(e.X, e.Y));
            }
        }

        void Label_CurrentConfig_ContexMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string fullCfgName = item.Text;
            
            if (fullCfgName == "")
                return;
            
            if (fullCfgName.Contains("[Add new "))
            {
                tabScanPage.SelectedIndex = 1;
                if (fullCfgName.Contains("LOCAL"))
                    ListBox_LocalCfgs_MouseClick(this, null);
                else
                    ListBox_TargetCfgs_MouseClick(this, null);
                Button_CfgNew_Click(this, null);
                return;
            }

            string[] cfgName = fullCfgName.Split(':');
            cfgName[1] = cfgName[1].Substring(1);
            int selIdx = 0;
            SystemBusy(true);
            switch (cfgName[0])
            {
                case "Device":
                    {
                        CheckConfigName(cfgName[1], false, ref selIdx);
                        SetScanConfig(ScanConfig.TargetConfig[selIdx], true, selIdx);
                    }
                    break;
                case "Local":
                    {
                        CheckConfigName(cfgName[1], true, ref selIdx);

                        if (IsCfgValidForSaveToDevice(LocalConfig[selIdx]) == SDK.RETURN_FAIL)
                        {
                            String msg = "The config - \"" + item.ToString() + "\" is not applicable for the device!\n\nSet config failed!";
                            Message.ShowError(msg);
                        }
                        else
                        {
                            SetScanConfig(LocalConfig[selIdx], false, selIdx);
                        }
                    }
                    break;
                default:
                    break;
            }
            SystemBusy(false);
            if (userDefaultReference != ScanReference.Built_in)
                RadioButton_RefPre_CheckedChanged(null, null);
        }

        static bool prevSysBusyState = false;
        private void SystemBusy(bool enable)
        {
            Console.WriteLine("Set system busy: " + enable);
            if (enable ^ prevSysBusyState)
            {
                prevSysBusyState = enable;
                BeginInvoke((Action)(() => //Invoke at UI thread
                {
                    if (enable)
                        SpecificCtrlIgnoreList.Clear();
                    //ControlSpecificControls(this, "Button", !enable);

                    if (enable)
                        //ControlAllControls(this, !enable);
                        this.Enabled = !enable;
                    else if (IsFetchingDeviceInfo)
                        //ControlAllControls(this, false);
                        this.Enabled = false;
                    else if (!Device.IsConnected())
                        UI_no_connection();
                    else
                        UI_Setting_Connected();
                }), null);
            }
            Cursor.Current = enable ? Cursors.WaitCursor : Cursors.Default;
        }

        static List<string> SpecificCtrlIgnoreList = new List<string>();
        private void ControlSpecificControls(Control con, String typeName, bool enable)
        {
            foreach (Control c in con.Controls)
            {
                ControlSpecificControls(c, typeName, enable);
            }
            if (con.GetType().Name == typeName)
            {
                if (!enable)
                {
                    if (con.Enabled == false)
                    {
                        SpecificCtrlIgnoreList.Add(con.Name);
                    }
                    else
                    {
                        con.Enabled = enable;
                    }
                }
                else
                {
                    if (!SpecificCtrlIgnoreList.Contains(con.Name))
                    {
                        con.Enabled = enable;
                    }
                }
            }
        }

        private void Button_EnableLog_Click(object sender, EventArgs e)
        {
            LogManager.GetRepository().Threshold = log4net.Core.Level.All;
            Label_LogStatus.Text = "Log File Status: Enabled";
        }

        private void Button_DisableLog_Click(object sender, EventArgs e)
        {
            LogManager.GetRepository().Threshold = log4net.Core.Level.Off;
            Label_LogStatus.Text = "Log File Status: Disabled";
        }

        private void Button_GetFanDelayOffTime_Click(object sender, EventArgs e)
        {
            textBox_FanOffTime.Text = Device.ReadFanDelayOffTime().ToString();
        }

        private void Button_SetFanDelayOffTime_Click(object sender, EventArgs e)
        {
            ushort t = 0;
            if (UInt16.TryParse(textBox_FanOffTime.Text, out t))
                Device.SetFanDelayOffTime(t);
            else
                ShowWarning("Not a correct input");
        }

        private void GroupBox_SaveScan_MouseDoubleClick(object sender, EventArgs e)
        {
            checkBox_EnableBlackLevelData.Visible = !checkBox_EnableBlackLevelData.Visible;
        }

        private void groupBox_Device_MouseDoubleClick(object sender, EventArgs e)
        {
            const int verControl = 3 * 0x10000 + 6 * 0x100 + 3; // Supported from v3.6.3
            int currVersion = Device.DevInfo.TivaRev[0] * 0x10000 + Device.DevInfo.TivaRev[1] * 0x100 + Device.DevInfo.TivaRev[1];
            if (Con_Dev_With_FAN.FirstOrDefault(stringToCheck => stringToCheck.Contains(Device.Get_Model_Identifier())) == Device.Get_Model_Identifier() && currVersion >= verControl)
            {
                label_FanOffTimeSetting.Visible = !label_FanOffTimeSetting.Visible;
                textBox_FanOffTime.Visible = !textBox_FanOffTime.Visible;
                Button_GetFanDelayOffTime.Visible = !Button_GetFanDelayOffTime.Visible;
                Button_SetFanDelayOffTime.Visible = !Button_SetFanDelayOffTime.Visible;
            }
        }

        private ToolTip tt;
        private void tt_FanOffTime_Leave(object sender, EventArgs e)
        {
            tt.Dispose();
        }

        private void tt_FanOffTime_MouseClick(object sender, MouseEventArgs e)
        {
            tt = new ToolTip();
            tt.InitialDelay = 0;
            tt.IsBalloon = false;
            tt.Show("Enter the FAN delay-off time in seconds (1~360, default = 30).", textBox_FanOffTime, 0);
        }

        // Codes hereunder are testing purpose for pressing the scan button clicking automatically
        static int btnScanTestCounts;
        private void timer_AutoClickScanButton_Tick(object sender, EventArgs e)
        {
            btnScanTestCounts++;
            this.BringToFront();
            Application.DoEvents();
            this.Button_Scan.Text = "Scan " + btnScanTestCounts;
            Button_Scan_Click(this, null);
        }
        private void label_ContinueScan_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (!timer_AutoClickScanButton.Enabled)
            {
                Double estScanTime = Scan.GetEstimatedScanTime();
                timer_AutoClickScanButton.Interval = ((int)estScanTime + 5) * 1000;
            }

            timer_AutoClickScanButton.Enabled = !timer_AutoClickScanButton.Enabled;
            btnScanTestCounts = 0;

            if (timer_AutoClickScanButton.Enabled)
            {
                timer_AutoClickScanButton_Tick(this, null);
            }
        }

        private void TextBox_LampStableTime_Leave(object sender, EventArgs e)
        {
            if (LampStableTime < 625)
            {
                String text = "Lamp Stable Time must be > 625ms to get stable scan result!";
                MessageBox.Show(text, "Warning");
                TextBox_LampStableTime.Text = "625";
                LampStableTime = 625;
            }
            Scan.SetLampDelay(LampStableTime);

            Double ScanTime = Scan.GetEstimatedScanTime();
            if (ScanTime > 0)
                Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";
        }

        private void Clear_Chart(bool RemoveChartData = false)
        {
            if (RemoveChartData)
            {
                ChartData_RefIntensity.Clear();
                ChartData_Intensity.Clear();
                ChartData_Absorbance.Clear();
                ChartData_Reflectance.Clear();
            }

            MyChart.Series.Clear();

            // For initial the chart to avoid the crazy axis numbers
            string title = "";
            if (RadioButton_Absorbance.Checked)
                title = "Absorbance";
            else if (RadioButton_Intensity.Checked)
                title = "Intensity";
            else if (RadioButton_Reflectance.Checked)
                title = "Reflectance";
            else if (RadioButton_Reference.Checked)
                title = "Reference";
            MyChart.Series.Add(new GLineSeries
            {
                Values = new GearedValues<ObservablePoint>(),
                Title = title,
                PointGeometry = null,
                Fill = System.Windows.Media.Brushes.Transparent,
                StrokeThickness = 1
            });
        }

        private void label_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var lb = (Label)sender;
            Clipboard.SetText(lb.Text);
            MessageBox.Show("Text has been copied to clipboard.");
        }

        private void CheckBox_SaveCombCSV_MouseClick(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right)
            {
                ContextMenuStrip m = new ContextMenuStrip();

                m.Items.Add("CSV Delimiter Options");
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add("0: comma (,)");
                m.Items.Add("1: semicolon (;)");
                m.Items.Add("2: tab (\\t)");
                m.Items.Add("3: pipe (|)");

                try
                {
                    for (int i = 2; i < m.Items.Count; i++)
                    {
                        ToolStripMenuItem menuItem = m.Items[i] as ToolStripMenuItem;
                        string compString = CSV_Delimiter == "\t" ? "\\t" : CSV_Delimiter;

                        string delimeter = menuItem.Text;
                        int foundS1 = delimeter.IndexOf("(");
                        int foundS2 = delimeter.IndexOf(")");
                        delimeter = delimeter.Substring(foundS1 + 1, foundS2 - foundS1 - 1);

                        if (delimeter == compString)
                        {
                            ((ToolStripMenuItem)m.Items[i]).Checked = true;
                            break;
                        }
                    }
                }
                catch { }

                m.ItemClicked += new ToolStripItemClickedEventHandler(CheckBox_SaveCombCSV_ContexMenu_ItemClicked);
                m.Show(CheckBox_SaveCombCSV, new Point(e.X, e.Y));
            }
        }

        private void Button_ApplyNumAvgToConfig_Click(object sender, EventArgs e)
        {
            Button_SaveNumAvgToConfig.Enabled = true;
        }

        private void Button_SaveNumAvgToConfig_Click(object sender, EventArgs e)
        {
            ScanConfig.SlewScanConfig CurConfig = ScanConfig.GetCurrentConfig();
            if (ushort.TryParse(textBox_ScanAvg.Text, out ushort scanAvg))
            {
                CurConfig.head.num_repeats = scanAvg;
            }
            else
            {
                Message.ShowError("Scan average number input error!");
                return;
            }

            if (DevCurCfg_IsTarget)
            {
                ScanConfig.TargetConfig.RemoveAt(DevCurCfg_Index);
                ScanConfig.TargetConfig.Insert(DevCurCfg_Index, CurConfig);
                RefreshTargetCfgList();
                SaveCfgToLocalOrDevice(DevCurCfg_IsTarget);
                SetScanConfig(ScanConfig.TargetConfig[DevCurCfg_Index], true, DevCurCfg_Index);
                object sndr = new object();
                sndr = DevCurCfg_Index.ToString();
                ListBox_TargetCfgs_SelectedIndexChanged(sndr, EventArgs.Empty);
            }
            else
            {
                LocalConfig.RemoveAt(DevCurCfg_Index);
                LocalConfig.Insert(DevCurCfg_Index, CurConfig);
                RefreshLocalCfgList();
                SaveCfgToLocalOrDevice(DevCurCfg_IsTarget);
                SetScanConfig(LocalConfig[DevCurCfg_Index], false, DevCurCfg_Index);
                object sndr = new object();
                sndr = DevCurCfg_Index.ToString();
                ListBox_LocalCfgs_SelectedIndexChanged(sndr, EventArgs.Empty);
            }

            GroupBox_ScanAvg.Text = "Scan Average";
            GroupBox_ScanAvg.ForeColor = Color.Black;
            textBox_ScanAvg.ForeColor = Color.Black;
            Button_SaveNumAvgToConfig.Enabled = false;
        }

        private void textBox_ScanAvg_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsControl(e.KeyChar))
                e.Handled = !char.IsDigit(e.KeyChar);
        }

        private void textBox_ScanAvg_Validated(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_ScanAvg.Text))
            {
                Message.ShowError("Input could not be empty!", "Invalid value");
                textBox_ScanAvg.Focus();
            }
        }

        private void textBox_ScanAvg_TextChanged(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                Button_Scan.Enabled = false;
                Button_SaveNumAvgToConfig.Enabled = false;
                return;
            }
            else
            {
                Button_Scan.Enabled = true;
                Button_SaveNumAvgToConfig.Enabled = true;
            }

            if (textBox.Text.Length > 0 && textBox.Text.Substring(0, 1) == "0")
            {
                int len = textBox.Text.Length;
                textBox.Text = textBox.Text.Substring(1, textBox.Text.Length - 1);
                if (len == 1)
                    return;
            }

            ScanConfig.SlewScanConfig CurConfig = ScanConfig.GetCurrentConfig();

            ushort.TryParse(textBox_ScanAvg.Text, out ushort scanAvg);

            if (scanAvg > 999)
                textBox_ScanAvg.Text = "999";

            ushort.TryParse(textBox_ScanAvg.Text, out scanAvg);

            if (CurConfig.head.num_repeats != scanAvg)
            {
                GroupBox_ScanAvg.Text = "Scan Average:  Temporarily set, not yet saved to current config.";
                GroupBox_ScanAvg.ForeColor = Color.DarkBlue;
                textBox.ForeColor = Color.DarkBlue;
                Button_SaveNumAvgToConfig.Enabled = true;
            }
            else
            {
                GroupBox_ScanAvg.Text = "Scan Average";
                GroupBox_ScanAvg.ForeColor = Color.Black;
                textBox.ForeColor = Color.Black;
                Button_SaveNumAvgToConfig.Enabled = false;
            }

            if (Scan.SetScanNumRepeats(ushort.Parse(textBox_ScanAvg.Text)) < SDK.RETURN_PASS)
                Message.ShowError("Set Scan Number Repeats Failed!");

            Double ScanTime = Scan.GetEstimatedScanTime();
            if (ScanTime > 0)
                Label_EstimatedScanTime.Text = "Est. Device Scan Time: " + String.Format("{0:0.000}", ScanTime) + " secs.";

            if (!IsFetchingDeviceInfo)
                RadioButton_RefPre_CheckedChanged(null, null);
        }

        void CheckBox_SaveCombCSV_ContexMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string selectDelimiterString = item.Text;
            if (selectDelimiterString == "")
                return;
            int foundS1 = selectDelimiterString.IndexOf("(");
            int foundS2 = selectDelimiterString.IndexOf(")");
            if (foundS1 > 0 && foundS2 > 0 && foundS2 > foundS1)
            {
                selectDelimiterString = selectDelimiterString.Substring(foundS1 + 1, foundS2 - foundS1 - 1);
                if (selectDelimiterString == "\\t")
                    CSV_Delimiter = "\t";
                else
                    CSV_Delimiter = selectDelimiterString;
            }
        }

        private void btn_FileListRefresh_Click(object sender, EventArgs e)
        {
            LoadSavedScanList();
            SavedScan_RefreshDataGridView();
            ClearSavedScanCfgItems();
            SaveSettings();

            if (dataGridView_savescan.RowCount > 0)
            {
                foreach (DataGridViewRow row in dataGridView_savescan.Rows)
                    row.Cells["Select"].Value = false;
            }
        }

        private void checkBox_zoom_MouseClick(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right)
            {
                ContextMenuStrip m = new ContextMenuStrip();

                m.Items.Add("[Zoom option]");
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add("X-Axis");
                m.Items.Add("Y-Axis");
                m.Items.Add("XY-Axes");

                if (MyChart.Zoom == ZoomingOptions.X)
                    ((ToolStripMenuItem)m.Items[2]).Checked = true;
                else if (MyChart.Zoom == ZoomingOptions.Y)
                    ((ToolStripMenuItem)m.Items[3]).Checked = true;
                else if (MyChart.Zoom == ZoomingOptions.Xy)
                    ((ToolStripMenuItem)m.Items[4]).Checked = true;
                else
                    return;

                m.ItemClicked += new ToolStripItemClickedEventHandler(checkBox_zoom_ContexMenu_ItemClicked);
                m.Show(checkBox_zoom, new Point(e.X, e.Y));
            }
        }

        void checkBox_zoom_ContexMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            String zoomOption = item.Text;

            if (zoomOption == String.Empty || zoomOption.Contains("option"))
                return;

            MyChart.AxisX[0].MinValue = double.NaN;
            MyChart.AxisX[0].MaxValue = double.NaN;
            MyChart.AxisY[0].MinValue = double.NaN;
            MyChart.AxisY[0].MaxValue = double.NaN;

            if (zoomOption.Contains("X-Axis"))
                MyChart.Zoom = ZoomingOptions.X;
            else if (zoomOption.Contains("Y-Axis"))
                MyChart.Zoom = ZoomingOptions.Y;
            else if (zoomOption.Contains("XY-Axes"))
                MyChart.Zoom = ZoomingOptions.Xy;

            userZoomOption = MyChart.Zoom;
        }

        private void panel_Tooltips_MouseClick(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right)
            {
                ContextMenuStrip m = new ContextMenuStrip();

                m.Items.Add("[Tooltips option]");
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add("Normal");
                m.Items.Add("Details");

                if (Tooltips_Show_Details)
                    ((ToolStripMenuItem)m.Items[3]).Checked = true;
                else 
                    ((ToolStripMenuItem)m.Items[2]).Checked = true;

                m.ItemClicked += new ToolStripItemClickedEventHandler(panel_Tooltips_ContexMenu_ItemClicked);
                m.Show(checkBox_zoom, new Point(e.X, e.Y));
            }
        }

        void panel_Tooltips_ContexMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            String tooltipsOption = item.Text;

            if (tooltipsOption == String.Empty || tooltipsOption.Contains("option"))
                return;

            if (tooltipsOption.Contains("Normal"))
            {
                Tooltips_Show_Details = false;
            }
            else if (tooltipsOption.Contains("Details"))
            {
                Tooltips_Show_Details = true;
            }
            checkBox_tooltip_CheckedChanged(null, null);
        }

        private void button_button_disableUACAlert_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = Message.ShowQuestion("Never ask UAC (User Access Control) for the GUI?");
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    String myPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    String myName = System.AppDomain.CurrentDomain.FriendlyName;
                    myPath += "\\" + myName;
                    RegistryKey myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\\", true);
                    if (myKey != null)
                    {
                        if (myKey.OpenSubKey(myPath, true) == null)
                        {
                            myKey.SetValue(myPath, "~ RUNASINVOKER", RegistryValueKind.String);
                        }
                    }
                    else
                    {
                        myKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\\", true);
                        myKey.CreateSubKey(myPath, true);
                        myKey.SetValue(myPath, "~ RUNASINVOKER", RegistryValueKind.String);
                    }
                    myKey.Close();
                }
                catch (Exception eX)
                {
                    Message.ShowError(eX.Message, "Write Registry Failed");
                    DBG.WriteLine(eX.Message);
                    logFile.Error(eX.Message);
                    return;
                };
            }
        }

        private void Button_ModelNameGet_MouseLeave(object sender, EventArgs e)
        {
            ModelNameGet_Click_Counts = 0;
        }

        private void Button_SerialNumberGet_MouseLeave(object sender, EventArgs e)
        {
            SerialNumberGet_Click_Counts = 0;
        }

        private void CheckBox_SaveDAT_MouseLeave(object sender, EventArgs e)
        {
            SaveDAT_Click_Counts = 0;
        }

        private void CheckBox_SaveCombCSV_MouseLeave(object sender, EventArgs e)
        {
            SaveCSV_Click_Counts = 0;
        }

        private void checkBox_AutoScan_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AutoScan.Checked)
            {
                checkBox_StopOnError.Checked = checkBox_AutoScan.Checked;
                checkBox_StopOnError.Enabled = true;
            }
            else
            {
                checkBox_StopOnError.Checked = false;
                checkBox_StopOnError.Enabled = false;
                label_ContinuousMode.Text = "Manual Continuous Scan Mode";
                label_ContinuousMode.Font = new Font(label_ContinueScan.Font.FontFamily, label_ContinueScan.Font.Size * 2, ((System.Drawing.FontStyle)(System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic)));
            }
            label_ContinuousMode.Visible = !checkBox_AutoScan.Checked;
        }

        private void button_ClearPlots_Click(object sender, EventArgs e)
        {
            Clear_Chart(true);
        }

        private void Manual_ContScan_UI_Con(bool on)
        {
            if (!(AppLoaded && Device.IsConnected())) return;
            
            label_ContinuousMode.Visible = on;
            checkBox_StopOnError.Enabled = !on & checkBox_AutoScan.Checked;
            GroupBox_RefSelect.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            GroupBox_LampControl.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            GroupBox_GainControl.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            GroupBox_ScanAvg.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            button_ClearPlots.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            if (RadioButton_RefNew.Checked)
            {
                GroupBox_ContScan.Enabled = false;
                GroupBox_SaveScan.Enabled = false;
            }
            else
            {
                GroupBox_ContScan.Enabled = !((Button_Scan.Text == "Scan Next") & on);
                GroupBox_SaveScan.Enabled = !((Button_Scan.Text == "Scan Next") & on);
            }
        }

        private void tabPage_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (NewConfig == true || EditConfig == true)
                TextBox_CfgName.Focus();

        }
        private void tabPage_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            if ((NewConfig == true || EditConfig == true) && !isSavingConfig)
            {
                e.Cancel = true;

                for (int i = 0; i < 7; i++)
                {
                    if (i % 2 == 0)
                    {
                        GroupBox_CfgDetails.BackColor = Color.LightYellow;
                        Button_CfgSave.BackColor = Color.LightYellow;
                        if (!IsFetchingDeviceInfoWithError)
                            Button_CfgCancel.BackColor = Color.LightYellow;
                    }
                    else
                    {
                        GroupBox_CfgDetails.BackColor = TransparencyKey;
                        Button_CfgSave.BackColor = TransparencyKey;
                        if (!IsFetchingDeviceInfoWithError)
                            Button_CfgCancel.BackColor = TransparencyKey;
                    }
                    Application.DoEvents();
                    SpinWait.SpinUntil(() => false, 250);
                    Application.DoEvents();
                }
            }
            else
            {
                GroupBox_CfgDetails.BackColor = TransparencyKey;
                Button_CfgSave.BackColor = TransparencyKey;
                if (!IsFetchingDeviceInfoWithError)
                    Button_CfgCancel.BackColor = TransparencyKey;
            }
        }

        private void button_SwitchDevice_Click(object sender, EventArgs e)
        {
            Device.Enumerate();
            if (Device.DeviceFound.Length < 2)
            {
                Message.ShowWarning("There is no other device to switch!");
                return;
            }
            this.Close();
            Application.Restart();
        }
    }
}