using Modbus;
using Modbus.Device;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vfd_monitor_nmodbus
{
    public partial class VfdMonitorForm : Form
    {
        #region 全局常量
        public static VfdMonitorForm Instance;
        public static int REFRESH_FREQUENCY = 50;
        public bool isRunning;
        public bool haveSaved;
        public IODb database;

        private SemaphoreSlim[] _modbusLock;

        private const int MAX_WRITE_RETRY = 10;     // 最大写入重试次数
        private const int MAX_CONNECT_RETRY = 10;     // 最大连接重试次数
        private const int MAX_CHANNEL = 96;
        private const int SHOW_CHANNEL = 64;
        private const int GROUP_SIZE = 8;           // 每组同IP8台
        private readonly string[] GROUP_IPS = {      // 12个网口IP
            "192.168.1.201",
            "192.168.1.202",
            "192.168.1.203",
            "192.168.1.204",
            "192.168.1.205",
            "192.168.1.206",
            "192.168.1.207",
            "192.168.1.208",
            "192.168.1.209",
            "192.168.1.210",
            "192.168.1.211",
            "192.168.1.212"
        };
        private const int TCP_PORT = 5001;
        private const byte SLAVE_ID = 0x01;
        private const byte FUN_READ = 0x03;
        private const byte FUN_WRITE = 0x06;
        private const byte FUN_MULTI_WRITE = 0x10;
        private readonly int REG_START_FREQ_ADDR = 0x0006; //停机下修改频率源为485
        private readonly int REG_START_CMD_ADDR = 0x0005; //停机下修改命令源为485
        private readonly int REG_FORWARD_BREAK_TORQUE_MAX = 0x0321; //修改正转制动转矩上限为50%
        private readonly int REG_BACKWARD_BREAK_TORQUE_MAX = 0x0323; //修改反转制动转矩上限为50%
        private readonly int REG_FORWARD_TORQUE_MAX = 0x0320; //修改正转电动转矩上限为120%
        private readonly int REG_BACKWARD_TORQUE_MAX = 0x0322; //修改反转电动转矩上限为120%
        private readonly int REG_MAX_FREQ = 0x0402; //修改M1最高频率
        private readonly int REG_PM_MAX = 0x0419; //修改PM辨识电流阈值100%
        private readonly int REG_PM_A = 0x0411;  //修改PM额定电流1500
        private readonly int REG_CARRIER_FREQ = 0x000F; //停机下修改载波频率
        private readonly int REG_FORWARD_RUN_CMD = 0x0012;
        private readonly int REG_BACKWARD_RUN_CMD = 0x0022;
        private readonly int REG_STOP_CMD = 0x0001;
        private readonly int REG_RESET_ADDR = 0x2002; // 复位
        private readonly int REG_CTRL_ADDR = 0x2000; // 运行控制
        public readonly int REG_ADDR_WRITE =  0x2001; // 2001-设定频率
        private readonly int WRITE_WAIT_DELAY = 10;

        // 2001-设定频率 2103-当前频率 2203-母线电压 2100-故障 2200-输出电流 2204-输出电压 2206-输出功率 2202-实际转速 220E-IGBT温度
        private readonly int[] REG_ADDR_OPTIONAL = { 0x2103, 0x2001, 0x2203, 0x2200, 0x2204, 0x2206, 0x2202, 0x220E };
        private readonly string[] REG_TEXT_OPTIONAL =
        {
            "当前频率",
            "设定频率",
            "母线电压",
            "输出电流",
            "输出电压",
            "输出功率",
            "实际转速",
            "IGBT温度"
        };
        private readonly double[] REG_DATA_FACTOR = { 0.01, 0.1 }; // 频率/功率-0.01 电压/电流-0.1 温度不变
        private string _logFilePath;
        private readonly object _logFileLock = new object();

        private const int LAYOUT_BASE_WIDTH = 1448;
        private const int LAYOUT_BASE_HEIGHT = 1011;
        private const int LAYOUT_BUTTON_TOP = 830;
        private const int LAYOUT_BUTTON_BOTTOM_MARGIN = LAYOUT_BASE_HEIGHT - LAYOUT_BUTTON_TOP;
        private const int LAYOUT_TOP_MARGIN = 10;
        private const int LAYOUT_LEFT_MARGIN = 20;
        private const int LAYOUT_CHART_LEFT = 150;
        private const int LAYOUT_RIGHT_MARGIN = 13;
        private const int LAYOUT_RIGHT_TEXT_WIDTH = 275;
        private const int LAYOUT_GAP = 10;
        private readonly int[] _chartBaseHeights = { 190, 190, 200 };
        private const int LAYOUT_DATA_BASE_HEIGHT = 200;
        private const double PLOT_X_MIN = 0.5;
        private const double PLOT_X_MAX = MAX_CHANNEL + 0.5;
        private const double PLOT_MIN_VISIBLE_CHANNELS = 16;
        private const string PLOT_FONT_NAME = "Microsoft YaHei";
        #endregion

        #region 通道启用状态（用户可控制）
        private int allChannelRunning; // 运行状态，与UI的点击绑定，正转为1，反转为2，停止为0
        private int[] isChannelReading; // 通道是否正在读取
        private bool[] isChannelFault; // 通道是否故障
        private int[] channelRetryTime; // 通道重启次数
        #endregion

        #region 私有属性
        private int[] REG_ADDR = { 0x2103, 0x2001, 0x2203, 0x2100 };
        private TcpClient[] vfdSockets;
        private ModbusSerialMaster[] modbusMasters;
        private int[] readPollCounter;
        private double[,] readDataBuffer;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.Timer logTimer;
        private bool autoReconnectEnabled = true; // 是否自动重连
        //private DateTime[] lastCommTime; // 记录每个通道最后一次通信时间
        private ConcurrentQueue<string> logBuffer;
        private ScottPlot.WinForms.FormsPlot _panningPlot;
        private int _panStartPixelX;
        private double _panStartLeft;
        private double _panStartRight;
        #endregion

        public VfdMonitorForm()
        {
            Instance = this;
            InitializeComponent();
            InitVariables();
            InitChart();
            InitLogFile();
            this.DoubleBuffered = true;
            this.Resize += VfdMonitorForm_Resize;
            ApplyResponsiveLayout();
        }

        #region 初始化
        private void InitVariables()
        {
            _modbusLock = new SemaphoreSlim[MAX_CHANNEL];
            for (int ch = 0;ch < MAX_CHANNEL; ch++)
            {
                _modbusLock[ch] = new SemaphoreSlim(1);
            }

            vfdSockets = new TcpClient[MAX_CHANNEL];
            modbusMasters = new ModbusSerialMaster[MAX_CHANNEL];
            readPollCounter = new int[MAX_CHANNEL];
            readDataBuffer = new double[MAX_CHANNEL, 4];
            isChannelReading = new int[MAX_CHANNEL];
            isChannelFault = new bool[MAX_CHANNEL];
            //lastCommTime = new DateTime[MAX_CHANNEL];
            channelRetryTime = new int[MAX_CHANNEL];
            logBuffer = new ConcurrentQueue<string>();

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                readPollCounter[i] = 0;
                isChannelReading[i] = 0;
                isChannelFault[i] = false;
                //lastCommTime[i] = DateTime.Now;
                channelRetryTime[i] = 0;
            }
            database = new IODb();

            mainTimer = new System.Windows.Forms.Timer
            {
                Interval = REFRESH_FREQUENCY
            };
            mainTimer.Tick += TimerTick;
            logTimer = new System.Windows.Forms.Timer
            {
                Interval = REFRESH_FREQUENCY / 2
            };
            logTimer.Tick += (s, e) => PrintLog();

            allChannelRunning = 0;
            isRunning = false;
            haveSaved = false;
            UpdateUiState(false);
        }

        private void InitData()
        {
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                isChannelReading[i] = 0;
                isChannelFault[i] = false;
                //lastCommTime[i] = DateTime.Now;
            }
            allChannelRunning = 0;
            //logBuffer = new ConcurrentQueue<string>();
        }

        private void InitLogFile()
        {
            try
            {
                string logDir = Path.Combine(Application.StartupPath, "Logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                // 文件名：启动时间精确到秒，保证唯一
                string fileName = $"Log_{DateTime.Now:yyyyMMddHHmmss}.txt";
                _logFilePath = Path.Combine(logDir, fileName);
            }
            catch { }
        }

        private void VfdMonitorForm_Resize(object sender, EventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            if (chartA == null || txtData == null || txtLog == null) return;

            int extraWidth = Math.Max(0, ClientSize.Width - LAYOUT_BASE_WIDTH);
            int extraHeight = Math.Max(0, ClientSize.Height - LAYOUT_BASE_HEIGHT);
            int buttonOffsetY = extraHeight;
            int buttonTop = LAYOUT_BUTTON_TOP + buttonOffsetY;
            int logX = 1160 + extraWidth;
            int chartWidth = Math.Max(100, logX - LAYOUT_CHART_LEFT - LAYOUT_GAP);
            int textWidth = Math.Max(100, logX - LAYOUT_LEFT_MARGIN - LAYOUT_GAP);
            int contentHeight = Math.Max(100, buttonTop - LAYOUT_TOP_MARGIN - LAYOUT_GAP);
            int scaledAreaHeight = Math.Max(100, contentHeight - LAYOUT_GAP * 3);
            int baseAreaHeight = _chartBaseHeights.Sum() + LAYOUT_DATA_BASE_HEIGHT;

            int chartAHeight = _chartBaseHeights[0] * scaledAreaHeight / baseAreaHeight;
            int chartBHeight = _chartBaseHeights[1] * scaledAreaHeight / baseAreaHeight;
            int chartCHeight = _chartBaseHeights[2] * scaledAreaHeight / baseAreaHeight;
            int dataHeight = scaledAreaHeight - chartAHeight - chartBHeight - chartCHeight;

            SuspendLayout();

            txtLog.Location = new Point(logX, LAYOUT_TOP_MARGIN);
            txtLog.Size = new Size(LAYOUT_RIGHT_TEXT_WIDTH, Math.Max(100, buttonTop - LAYOUT_TOP_MARGIN - LAYOUT_GAP));

            chartA.Location = new Point(LAYOUT_CHART_LEFT, LAYOUT_TOP_MARGIN);
            chartA.Size = new Size(chartWidth, chartAHeight);
            comboBox1.Location = new Point(LAYOUT_LEFT_MARGIN, chartA.Top);

            chartB.Location = new Point(LAYOUT_CHART_LEFT, chartA.Bottom + LAYOUT_GAP);
            chartB.Size = new Size(chartWidth, chartBHeight);
            comboBox2.Location = new Point(LAYOUT_LEFT_MARGIN, chartB.Top);

            chartC.Location = new Point(LAYOUT_CHART_LEFT, chartB.Bottom + LAYOUT_GAP);
            chartC.Size = new Size(chartWidth, chartCHeight);
            comboBox3.Location = new Point(LAYOUT_LEFT_MARGIN, chartC.Top);

            txtData.Location = new Point(LAYOUT_LEFT_MARGIN, chartC.Bottom + LAYOUT_GAP);
            txtData.Size = new Size(textWidth, dataHeight);

            MoveBottomControls(buttonOffsetY);

            ResumeLayout(false);
        }

        private void MoveBottomControls(int offsetY)
        {
            lampPanel.Location = new Point(20, 830 + offsetY);
            btnStartStop.Location = new Point(60, 830 + offsetY);
            btnClearFault.Location = new Point(170, 830 + offsetY);
            btnChannelSelect.Location = new Point(280, 830 + offsetY);

            btnImport.Location = new Point(60, 870 + offsetY);
            btnFreqOption.Location = new Point(170, 870 + offsetY);
            btnSaveFreq.Location = new Point(280, 870 + offsetY);

            btnForward.Location = new Point(60, 910 + offsetY);
            btnBackward.Location = new Point(170, 910 + offsetY);
            btnStopOperation.Location = new Point(280, 910 + offsetY);

            btnRefreshFreq.Location = new Point(60, 950 + offsetY);
            textBoxRefreshFreq.Location = new Point(170, 953 + offsetY);
            btnInitReg.Location = new Point(280, 950 + offsetY);
        }

        private async void InitReg()
        {
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                //SendCmd(i, REG_START_CMD_ADDR, 0x0002);
                //SendCmd(i, REG_START_FREQ_ADDR, 0x0001);
                //SendCmd(i, REG_CARRIER_FREQ, 0x0002);
                //SendCmd(i, REG_FORWARD_BREAK_TORQUE_MAX, 0x0032);
                //SendCmd(i, REG_BACKWARD_BREAK_TORQUE_MAX, 0x0032);
                SendCmd(i, REG_FORWARD_TORQUE_MAX, 0x0064);
                SendCmd(i, REG_BACKWARD_TORQUE_MAX, 0x0064);
                //SendCmd(i, REG_MAX_FREQ, 0xC350);
                //SendCmd(i, REG_PM_MAX, 0x2710);
                //SendCmd(i, REG_PM_A, 0x05DC);
            }
        }

        private bool GetChannelIp(int ch, out string ip)
        {
            ip = "";
            try
            {
                int group = ch / GROUP_SIZE;
                int index = ch % GROUP_SIZE;
                ip = GROUP_IPS[group];
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ConnectTcp()
        {
            AppendLog("开始连接启用的变频器...");
            int group_cnt = MAX_CHANNEL / GROUP_SIZE;
            
            for (int iter = 0; iter < group_cnt; iter++)
            {
                var tasks = new List<Task>();
                for (int ch = iter * GROUP_SIZE; ch < (iter + 1) * GROUP_SIZE; ch++)
                {
                    int channel = ch;
                    tasks.Add(ConnectSingleChannelTcp(channel));
                }
                await Task.WhenAll(tasks);
                await Task.Delay(20);
            }
        }

        private async Task ConnectSingleChannelTcp(int ch)
        {
            try
            {
                if (ch >= MAX_CHANNEL) return;
                int index = ch % GROUP_SIZE;
                if (!database.ChannelFreqDict[ch].Enabled)
                {
                    vfdSockets[ch] = null;
                    return;
                }

                if (GetChannelIp(ch, out string ip))
                {
                    vfdSockets[ch] = new TcpClient
                    {
                        ReceiveTimeout = 3000,
                        SendTimeout = 3000
                    };
                    var connectTask = vfdSockets[ch].ConnectAsync(ip, TCP_PORT + index);
                    if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                    {
                        throw new Exception("TCP 连接超时（3s）");
                    }
                    if (vfdSockets[ch] == null || !vfdSockets[ch].Connected)
                    {
                        throw new Exception("socket连接异常");
                    }

                    // 连接成功
                    AppendLog($"通道{ch + 1} 连接成功：{ip}:{TCP_PORT + index}");
                    channelRetryTime[ch] = 0;

                    modbusMasters[ch] = ModbusSerialMaster.CreateRtu(vfdSockets[ch]);
                    modbusMasters[ch].Transport.ReadTimeout = 1500;
                    modbusMasters[ch].Transport.WriteTimeout = 1500;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"通道 {ch + 1} 连接失败：{ex.Message}，准备重试");
                vfdSockets[ch] = null;
                modbusMasters[ch] = null;
                channelRetryTime[ch]++;
            }
        }
        #endregion

        #region 图表初始化（核心）
        private void InitChart()
        {
            InitSinglePlot(chartA, "数据A - 当前频率");
            InitSinglePlot(chartB, "数据B - 设定频率");
            InitSinglePlot(chartC, "数据C - 母线电压");
            SetAllPlotXLimits(PLOT_X_MIN, PLOT_X_MAX);
        }

        private void InitSinglePlot(ScottPlot.WinForms.FormsPlot plot, string title)
        {
            plot.Plot.Title(title);
            plot.Plot.XLabel("通道号");
            plot.Plot.YLabel("数值");
            ApplyPlotFont(plot);
            plot.Plot.Axes.SetLimits(PLOT_X_MIN, PLOT_X_MAX, -1, 1);
            plot.Interaction.Disable();
            plot.TabStop = true;
            plot.MouseWheel += ScottPlot_MouseWheel;
            plot.MouseDown += ScottPlot_MouseDown;
            plot.MouseMove += ScottPlot_MouseMove;
            plot.MouseUp += ScottPlot_MouseUp;
            plot.MouseDoubleClick += ScottPlot_MouseDoubleClick;
            plot.DoubleClick += ScottPlot_DoubleClick;
            plot.MouseEnter += (s, e) => ((Control)s).Focus();
            plot.Refresh();
        }

        private void ApplyPlotFont(ScottPlot.WinForms.FormsPlot plot)
        {
            plot.Plot.Axes.Title.Label.Font.Name = PLOT_FONT_NAME;
            plot.Plot.Axes.Bottom.Label.FontName = PLOT_FONT_NAME;
            plot.Plot.Axes.Bottom.TickLabelStyle.FontName = PLOT_FONT_NAME;
            plot.Plot.Axes.Left.Label.FontName = PLOT_FONT_NAME;
            plot.Plot.Axes.Left.TickLabelStyle.FontName = PLOT_FONT_NAME;
        }
        #endregion

        #region 核心定时器
        private async void TimerTick(object sender, EventArgs e)
        {
            if (!isRunning) return;

            await ProcessAllChannel();

            UpdateDataText();
            UpdateChartUI();
        }

        private async Task ProcessAllChannel()
        {
            if (!isRunning) return;
            var tasks = new List<Task>();

            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                int channel = ch;
                try
                {
                    if (!database.ChannelFreqDict[ch].Enabled) continue;
                    if (IsChannelDisconnected(ch))
                    {
                        await ConnectSingleChannelTcp(ch);
                        continue; // 本次不通信，等重连成功
                    }
                }
                catch (Exception ex) { AppendLog("通道连接异常：" + ex.Message); }
                tasks.Add(ProcessChannel(channel));
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessChannel(int ch)
        {
            if (Interlocked.CompareExchange(ref isChannelReading[ch], 1, 0) == 1)
                return;
            readPollCounter[ch]++;

            int read_index = readPollCounter[ch];
            int idx = read_index % 4;
            int addr = REG_ADDR[idx];

            ushort value = await SendReadCmd(ch, (ushort)addr);
            if (idx == 3)
            {
                byte low_err = (byte)(value & 0xFF);
                if (low_err == 0x2A)
                {
                    SendCmd(ch, REG_RESET_ADDR, 0x0002);
                    int run_dir;
                    if (allChannelRunning == 1) run_dir = database.ChannelFreqDict[ch].Opposite ? REG_BACKWARD_RUN_CMD : REG_FORWARD_RUN_CMD;
                    else if (allChannelRunning == 2) run_dir = database.ChannelFreqDict[ch].Opposite ? REG_FORWARD_RUN_CMD : REG_BACKWARD_RUN_CMD;
                    else run_dir = REG_STOP_CMD;
                    await SendCmdRunning(ch, run_dir);
                    AppendLog($"通道{ch + 1} 已重发命令：" + run_dir);
                }

                string faultInfo = GetFaultInfo(value);
                if (faultInfo != null)
                {
                    AppendLog($"通道{ch + 1} 返回故障：" + faultInfo + " 故障码：" + value);
                    isChannelFault[ch] = true;
                    readPollCounter[ch] = 0;
                }
                else isChannelFault[ch] = false;
            }
            else
            {
                readDataBuffer[ch, idx] = DataUnitCalc(value, REG_ADDR[idx]);
            }
            
            Interlocked.Exchange(ref isChannelReading[ch], 0);
        }
        #endregion

        #region 发送命令
        public async Task SendCmdRunning(int ch, int value)
        {
            int sendCmdCount = 0;
            ushort freq = 0;
            while (sendCmdCount < MAX_WRITE_RETRY)
            {
                await Task.Delay(ch % GROUP_SIZE + 1); // 稍微错开一点时间，避免同时启动的高并发压力
                SendCmd(ch, REG_CTRL_ADDR, value);
                sendCmdCount++;
                freq = await SendReadCmd(ch, (ushort)REG_ADDR_OPTIONAL[0]);
                AppendLog($"ch = {ch} 收到freq-{freq}");

                if (value != REG_STOP_CMD && freq > 0) { AppendLog($"通道{ch + 1} 开始运行"); break; }
                if (value == REG_STOP_CMD && freq == 0) { AppendLog($"通道{ch + 1} 停止运行"); break; }
            }
            AppendLog($"通道{ch + 1} 共发送命令{sendCmdCount}次");
        }
        public async void SendCmd(int ch, int addr, int value)
        {
            if (!database.ChannelFreqDict[ch].Enabled) return;
            if (IsChannelDisconnected(ch)) return;

            await _modbusLock[ch].WaitAsync();
            await Task.Delay(WRITE_WAIT_DELAY);
            try
            {
                await modbusMasters[ch].WriteSingleRegisterAsync(SLAVE_ID, (ushort)addr, (ushort)value);
                AppendLog($"通道{ch + 1} 发送指令： addr-{addr} val-{value}");
            }
            catch (SlaveException ex)
            {
                AppendLog($"通道{ch + 1} 输入错误命令，收到故障读写信息" + FaultDictionary.GetModbusMessage(ex.SlaveExceptionCode));
            }
            catch (TimeoutException ex)
            {
                AppendLog($"通道{ch + 1} 发送超时：{ex.Message}");
            }
            catch (SocketException ex)
            {
                AppendLog($"通道{ch + 1} TCP连接失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                if (addr == REG_CTRL_ADDR && (ex.Message.Contains("Checksums failed") || ex.Message.Contains("even number of bytes") || ex.Message.Contains("Function code"))) {}
                else AppendLog($"通道{ch + 1} 写入发生错误：{ex.Message}");
            }
            finally
            {
                await Task.Delay(WRITE_WAIT_DELAY);
                _modbusLock[ch].Release();
            }
        }

        private async Task<ushort> SendReadCmd(int ch, int addr)
        {
            ushort value = 0;
            await _modbusLock[ch].WaitAsync();
            try
            {
                if (modbusMasters[ch] == null) return 0;
                await Task.Delay(ch % GROUP_SIZE + 1); // 稍微错开一点时间，避免同时启动的高并发压力
                await Task.Delay(WRITE_WAIT_DELAY);
                ushort[] registers = await modbusMasters[ch].ReadHoldingRegistersAsync(SLAVE_ID, (ushort)addr, 1);
                value = registers[0];
                AppendLog($"ch = {ch} 收到value-{value}");
            }
            catch (SlaveException ex)
            {
                AppendLog($"通道{ch + 1} 输入错误命令，收到故障读写信息" + FaultDictionary.GetModbusMessage(ex.SlaveExceptionCode));
            }
            catch (TimeoutException ex)
            {
                AppendLog($"通道{ch + 1} 发送超时：{ex.Message}");
            }
            catch (SocketException ex)
            {
                AppendLog($"通道{ch + 1} TCP连接失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                AppendLog($"通道{ch + 1} 发生错误：{ex.Message}");
            }
            finally
            {
                _modbusLock[ch].Release();
            }
            return value;
        }
        #endregion

        #region UI 刷新（图表 + 数据）
        private void UpdateChartUI()
        {
            var limits = chartA.Plot.Axes.GetLimits();
            double left = limits.Left;
            double right = limits.Right;

            UpdateSinglePlot(chartA, 0, left, right);
            UpdateSinglePlot(chartB, 1, left, right);
            UpdateSinglePlot(chartC, 2, left, right);
        }

        private void UpdateSinglePlot(ScottPlot.WinForms.FormsPlot plot, int dataIndex, double left, double right)
        {
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();
            int showChannel = 0;

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                if (!database.ChannelFreqDict[i].Enabled) continue;
                showChannel++;
                if (showChannel > SHOW_CHANNEL) break;

                xs.Add(i + 1);
                ys.Add(readDataBuffer[i, dataIndex]);
            }

            plot.Plot.Clear();
            if (xs.Count > 0)
            {
                plot.Plot.Add.Bars(xs.ToArray(), ys.ToArray());
            }

            SetPlotYLimits(plot, dataIndex, left, right);
            plot.Plot.Axes.SetLimitsX(left, right);
            plot.Refresh();
        }

        private void SetPlotYLimits(ScottPlot.WinForms.FormsPlot plot, int dataIndex, double left, double right)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            double nonZeroMin = double.MaxValue;
            int showChannel = 0;
            bool hasValue = false;
            bool hasNonZeroValue = false;

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                if (!database.ChannelFreqDict[i].Enabled) continue;
                showChannel++;
                if (showChannel > SHOW_CHANNEL) break;

                double x = i + 1;
                if (x < left || x > right) continue;

                double value = readDataBuffer[i, dataIndex];
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                hasValue = true;
                if (Math.Abs(value) > 0.0001)
                {
                    nonZeroMin = Math.Min(nonZeroMin, value);
                    hasNonZeroValue = true;
                }
            }

            if (!hasValue)
            {
                plot.Plot.Axes.SetLimitsY(-1, 1);
                return;
            }

            if (hasNonZeroValue)
            {
                min = nonZeroMin;
            }

            if (Math.Abs(max - min) < 0.0001)
            {
                min -= 1;
                max += 1;
            }
            else
            {
                double padding = (max - min) * 0.1;
                min -= padding;
                max += padding;
            }

            plot.Plot.Axes.SetLimitsY(min, max);
        }

        private void SetAllPlotXLimits(double left, double right)
        {
            ClampPlotXLimits(ref left, ref right);
            chartA.Plot.Axes.SetLimitsX(left, right);
            chartB.Plot.Axes.SetLimitsX(left, right);
            chartC.Plot.Axes.SetLimitsX(left, right);
            SetPlotYLimits(chartA, 0, left, right);
            SetPlotYLimits(chartB, 1, left, right);
            SetPlotYLimits(chartC, 2, left, right);
            chartA.Refresh();
            chartB.Refresh();
            chartC.Refresh();
        }

        private void ClampPlotXLimits(ref double left, ref double right)
        {
            double span = right - left;
            if (span < PLOT_MIN_VISIBLE_CHANNELS)
            {
                double center = (left + right) / 2;
                left = center - PLOT_MIN_VISIBLE_CHANNELS / 2;
                right = center + PLOT_MIN_VISIBLE_CHANNELS / 2;
                span = PLOT_MIN_VISIBLE_CHANNELS;
            }
            if (span > PLOT_X_MAX - PLOT_X_MIN)
            {
                left = PLOT_X_MIN;
                right = PLOT_X_MAX;
                return;
            }
            if (left < PLOT_X_MIN)
            {
                right += PLOT_X_MIN - left;
                left = PLOT_X_MIN;
            }
            if (right > PLOT_X_MAX)
            {
                left -= right - PLOT_X_MAX;
                right = PLOT_X_MAX;
            }
            if (left < PLOT_X_MIN) left = PLOT_X_MIN;
            if (right > PLOT_X_MAX) right = PLOT_X_MAX;
        }

        private double GetMouseXCoordinate(ScottPlot.WinForms.FormsPlot plot, MouseEventArgs e)
        {
            ScottPlot.Pixel pixel = new ScottPlot.Pixel(e.X, e.Y);
            ScottPlot.Coordinates coordinates = plot.GetCoordinates(pixel, plot.Plot.Axes.Bottom, plot.Plot.Axes.Left);
            return coordinates.X;
        }

        private void ScottPlot_MouseWheel(object sender, MouseEventArgs e)
        {
            var plot = (ScottPlot.WinForms.FormsPlot)sender;
            plot.Focus();

            var limits = chartA.Plot.Axes.GetLimits();
            double span = limits.Right - limits.Left;
            double mouseX = GetMouseXCoordinate(plot, e);
            if (mouseX < limits.Left || mouseX > limits.Right)
                mouseX = (limits.Left + limits.Right) / 2;

            double zoomRatio = e.Delta > 0 ? 0.8 : 1.25;
            double newSpan = span * zoomRatio;
            newSpan = Math.Max(PLOT_MIN_VISIBLE_CHANNELS, Math.Min(PLOT_X_MAX - PLOT_X_MIN, newSpan));

            double mouseRatio = (mouseX - limits.Left) / span;
            double left = mouseX - newSpan * mouseRatio;
            double right = left + newSpan;
            SetAllPlotXLimits(left, right);
        }

        private void ScottPlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _panningPlot = (ScottPlot.WinForms.FormsPlot)sender;
            _panningPlot.Focus();
            var limits = chartA.Plot.Axes.GetLimits();
            _panStartPixelX = e.X;
            _panStartLeft = limits.Left;
            _panStartRight = limits.Right;
        }

        private void ScottPlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (_panningPlot == null || e.Button != MouseButtons.Left) return;

            double span = _panStartRight - _panStartLeft;
            double unitsPerPixel = span / Math.Max(1, _panningPlot.Width);
            double offset = (e.X - _panStartPixelX) * unitsPerPixel;
            SetAllPlotXLimits(_panStartLeft - offset, _panStartRight - offset);
        }

        private void ScottPlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _panningPlot = null;
        }

        private void ScottPlot_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SetAllPlotXLimits(PLOT_X_MIN, PLOT_X_MAX);
        }

        private void ScottPlot_DoubleClick(object sender, EventArgs e)
        {
            SetAllPlotXLimits(PLOT_X_MIN, PLOT_X_MAX);
        }

        private void UpdateDataText()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                if (!database.ChannelFreqDict[i].Enabled) continue;
                sb.AppendLine($"通道{i + 1} → {REG_TEXT_OPTIONAL[comboBox1.SelectedIndex]}:{readDataBuffer[i, 0]}  {REG_TEXT_OPTIONAL[comboBox2.SelectedIndex]}:{readDataBuffer[i, 1]}  {REG_TEXT_OPTIONAL[comboBox3.SelectedIndex]}:{readDataBuffer[i, 2]}");
            }
            txtData.Text = sb.ToString();
        }
        #endregion

        #region 重连接
        public async void RestartConnection()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RestartConnection));
                return;
            }

            try
            {
                CloseAllConnections();
                await ConnectTcp();
                mainTimer.Start();
                AppendLog("通道配置已更新，自动重连完成");
            }
            catch (Exception ex)
            {
                AppendLog("自动重连失败：" + ex.Message);
            }
        }

        private void CloseAllConnections()
        {
            try
            {
                mainTimer.Stop();
                for (int ch = 0; ch < MAX_CHANNEL; ch++)
                {
                    readPollCounter[ch] = 0;
                    readDataBuffer[ch, 0] = 0;
                    readDataBuffer[ch, 1] = 0;
                    readDataBuffer[ch, 2] = 0;
                    readDataBuffer[ch, 3] = 0;
                }
                UpdateChartUI();
                CloseAllSockets();
                AppendLog("所有TCP连接通道已经关闭");
            }
            catch (Exception ex)
            {
                AppendLog("关闭所有通道失败：" + ex.Message);
            }
        }
        #endregion

        #region 工具方法
        public double DataUnitCalc(ushort data, int addr)
        {
            if (addr == 0x2103 || addr == 0x2001 || addr == 0x2206)
            {
                return data * REG_DATA_FACTOR[0];
            }
            else if (addr == 0x2203 || addr == 0x2200)
            {
                return data * REG_DATA_FACTOR[1];
            }
            else if (addr == 0x220E) // modbus协议温度采用补码输出
            {
                if (data <= 32767) { return data; }
                else { return 65536 - data; }
            }
            else { return data; }
        }

        public int DataUnitCalc(double data, int addr) // 写入功能单位转换，只允许写入频率
        {
            double input_data;
            if (addr == 0x2103 || addr == 0x2001 || addr == 0x2206)
            {
                input_data = data / REG_DATA_FACTOR[0];
            }
            else if (addr == 0x2203 || addr == 0x2200)
            {
                input_data = data / REG_DATA_FACTOR[1];
            }
            else if (addr == 0x220E) // modbus协议温度采用补码输出
            {
                if (data <= 32767) { input_data = data; }
                else { input_data = 65536 - data; }
            }
            else { input_data = data; }
            return ((int)input_data);
        }

        private string GetFaultInfo(ushort data)
        {
            if (data == 0x00) return null;
            return FaultDictionary.GetFaultMessage(data);
        }

        private void UpdateUiState(bool running)
        {
            btnStartStop.Text = running ? "停止控制" : "启动控制";
            lampPanel.BackColor = running ? Color.Lime : Color.Red;
        }

        private async void CloseAllSockets()
        {
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                await _modbusLock[i].WaitAsync();
                CloseTcpClient(vfdSockets[i]);
                vfdSockets[i] = null;
                modbusMasters[i] = null;
                _modbusLock[i].Release();
            }
        }
        private void CloseTcpClient(TcpClient client)
        {
            if (client == null) return;
            if (client.Connected)
            {
                client.GetStream().Close();
                client.Close();
            }
            client.Dispose();
        }
        private bool IsChannelDisconnected(int ch)
        {
            if (vfdSockets[ch] == null) return true;
            if (!vfdSockets[ch].Connected) return true;
            if (modbusMasters[ch] == null) return true;
            //if ((DateTime.Now - lastCommTime[ch]).TotalSeconds > 60)
            //    return true;
            return false;
        }

        public void PrintLog()
        {
            if (logBuffer.IsEmpty) return;

            // 一次性取出所有日志批量加入
            StringBuilder sb = new StringBuilder();
            while (logBuffer.TryDequeue(out string msg))
            {
                sb.Append(msg);
                WriteLogToFile(msg);
            }

            // 批量写入UI
            txtLog.AppendText(sb.ToString());

            // 自动滚动到底部
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        public void AppendLog(string msg)
        {
            logBuffer.Enqueue($"{DateTime.Now:HH:mm:ss} {msg}\r\n");
        }

        private void WriteLogToFile(string msg)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            try
            {
                lock (_logFileLock)
                {
                    using (StreamWriter sw = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                    {
                        sw.WriteLine(msg.TrimEnd());
                    }
                }
            }
            catch { }
        }

        private void RestartChannel(int ch)
        {
            // 用于处理单个通道出现超时后的重启
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    RestartChannel(ch);
                }));
                return;
            }
            
            if (!database.ChannelFreqDict[ch].Enabled) return;
            mainTimer.Stop();
            CloseTcpClient(vfdSockets[ch]);
            vfdSockets[ch] = null;
            modbusMasters[ch] = null;

            isChannelReading[ch] = 0;
            isChannelFault[ch] = false;

            int index = ch % GROUP_SIZE;
            AppendLog($"通道{ch + 1} 准备重连");
            try
            {
                if (GetChannelIp(ch, out string ip))
                {
                    vfdSockets[ch] = new TcpClient
                    {
                        ReceiveTimeout = 2000,
                        SendTimeout = 2000
                    };
                    vfdSockets[ch].Connect(ip, TCP_PORT + index);
                    modbusMasters[ch] = ModbusSerialMaster.CreateRtu(vfdSockets[ch]);
                    AppendLog($"通道{ch + 1} 连接成功：{ip}:{TCP_PORT + index}");
                    channelRetryTime[ch] = 0;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"通道 {ch + 1} 连接失败：{ex.Message}");
                vfdSockets[ch] = null;
                modbusMasters[ch] = null;
                channelRetryTime[ch]++;
                if (channelRetryTime[ch] > MAX_CONNECT_RETRY)
                {
                    database.ChannelFreqDict[ch].Enabled = false;
                    AppendLog($"通道 {ch + 1} 重连次数超过上限，已暂时禁用");
                }
            }
            mainTimer.Start();
        }

        private void StopAllChannel()
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                SendCmd(ch, REG_CTRL_ADDR, REG_STOP_CMD);
            }
        }

        private async Task StopAllChannelAsync()
        {
            var tasks = new List<Task>();

            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                int channel = ch;
                tasks.Add(Task.Run(() => SendCmd(channel, REG_CTRL_ADDR, REG_STOP_CMD)));
            }

            await Task.WhenAll(tasks);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            isRunning = false; mainTimer.Stop(); logTimer.Stop();
            mainTimer.Dispose(); logTimer.Dispose();
            //StopAllChannel();
            CloseAllSockets();
            if (!haveSaved)
            {
                if (MessageBox.Show("未保存，是否需要保存写入频率设置", "提示：", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    OpenFileDialog ofd = new OpenFileDialog { Filter = "数据文件|*.csv" };
                    if (ofd.ShowDialog() != DialogResult.OK) { e.Cancel = true; return; }
                    database.WriteChannelFreq(ofd.FileName);
                    haveSaved = true;
                }
                // 如果强制保存请打开下一行注释
                //else e.Cancel = true;
            }
        }
        #endregion

        #region 按钮事件
        private async void btnStartStop_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                InitData();
                isRunning = true;
                UpdateUiState(true);
                btnBackward.Enabled = true;
                btnForward.Enabled = true;
                btnStopOperation.Enabled = false;
                await ConnectTcp();
                mainTimer.Start();
                logTimer.Start();
            }
            else
            {
                mainTimer.Stop();
                isRunning = false;
                UpdateUiState(false);
                btnBackward.Enabled = false;
                btnForward.Enabled = false;
                btnStopOperation.Enabled = false;
                await StopAllChannelAsync();
                CloseAllSockets();
                //logTimer.Stop();
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "数据文件|*.csv" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            try
            {
                database.LoadChannelFreq(ofd.FileName);
                AppendLog($"导入成功");
            }
            catch (Exception ex)
            {
                AppendLog($"导入失败：{ex.Message}");
            }
        }

        private void btnChannelSelect_Click(object sender, EventArgs e)
        {
            ChannelSelectForm form = new ChannelSelectForm();
            form.ShowDialog();
        }

        private void btnRefreshFreq_Click(object sender, EventArgs e)
        {
            try
            {
                int number = int.Parse(textBoxRefreshFreq.Text);
                if (number < 50 || number > 2000)
                {
                    AppendLog("刷新间隔需要设置为50-2000ms");
                    return;
                }
                REFRESH_FREQUENCY = number;
                mainTimer.Interval = REFRESH_FREQUENCY;
                logTimer.Interval = REFRESH_FREQUENCY;
                AppendLog("刷新间隔设置为" + number.ToString() + "ms");
            }
            catch (FormatException)
            {
                AppendLog("未输入有效刷新间隔");
            }
        }

        private void btnClearFault_Click(object sender, EventArgs e)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (!database.ChannelFreqDict[ch].Enabled || !isChannelFault[ch]) continue;
                SendCmd(ch, REG_RESET_ADDR, 0x0002);
                isChannelFault[ch] = false;
            }
        }

        private async void btnForward_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                AppendLog("请先点击启动控制启动系统");
                return;
            }
            btnBackward.Enabled = false;
            //btnForward.Enabled = false;
            btnStopOperation.Enabled = true;
            var tasks = new List<Task>();
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (database.ChannelFreqDict[ch].Enabled && vfdSockets[ch] != null && vfdSockets[ch].Connected)
                {
                    int forward = database.ChannelFreqDict[ch].Opposite ? REG_BACKWARD_RUN_CMD : REG_FORWARD_RUN_CMD;
                    tasks.Add(SendCmdRunning(ch, forward));
                }
            }
            await Task.WhenAll(tasks);
            allChannelRunning = 1;
        }

        private async void btnBackward_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                AppendLog("请先点击启动控制启动系统");
                return;
            }
            //btnBackward.Enabled = false;
            btnForward.Enabled = false;
            btnStopOperation.Enabled = true;
            var tasks = new List<Task>();
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (database.ChannelFreqDict[ch].Enabled && vfdSockets[ch] != null && vfdSockets[ch].Connected)
                {
                    int forward = database.ChannelFreqDict[ch].Opposite ? REG_FORWARD_RUN_CMD : REG_BACKWARD_RUN_CMD;
                    tasks.Add(SendCmdRunning(ch, forward));
                }
            }
            await Task.WhenAll(tasks);
            allChannelRunning = 2;
        }

        private async void btnStopOperation_Click(object sender, EventArgs e)
        {
            btnBackward.Enabled = true;
            btnForward.Enabled = true;
            var tasks = new List<Task>();
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (database.ChannelFreqDict[ch].Enabled && vfdSockets[ch] != null && vfdSockets[ch].Connected)
                    tasks.Add(SendCmdRunning(ch, REG_STOP_CMD));
            }
            await Task.WhenAll(tasks);
            allChannelRunning = 0;
        }

        private void btnSaveFreq_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "数据文件|*.csv" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            database.WriteChannelFreq(ofd.FileName);
            haveSaved = true;
        }

        private void btnFreqOption_Click(object sender, EventArgs e)
        {
            SetFreqForm form = new SetFreqForm();
            form.Show();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            REG_ADDR[0] = REG_ADDR_OPTIONAL[comboBox1.SelectedIndex];
            txtData.Clear();
            chartA.Plot.Title("数据A - " + REG_TEXT_OPTIONAL[comboBox1.SelectedIndex]);
            ApplyPlotFont(chartA);
            chartA.Refresh();
            AppendLog("读取信息已修改");
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            REG_ADDR[1] = REG_ADDR_OPTIONAL[comboBox2.SelectedIndex];
            txtData.Clear();
            chartB.Plot.Title("数据B - " + REG_TEXT_OPTIONAL[comboBox2.SelectedIndex]);
            ApplyPlotFont(chartB);
            chartB.Refresh();
            AppendLog("读取信息已修改");
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            REG_ADDR[2] = REG_ADDR_OPTIONAL[comboBox3.SelectedIndex];
            txtData.Clear();
            chartC.Plot.Title("数据C - " + REG_TEXT_OPTIONAL[comboBox3.SelectedIndex]);
            ApplyPlotFont(chartC);
            chartC.Refresh();
            AppendLog("读取信息已修改");
        }

        private void VfdMonitorForm_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged -= comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged -= comboBox3_SelectedIndexChanged;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 1;
            comboBox3.SelectedIndex = 2;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            textBoxRefreshFreq.Text = REFRESH_FREQUENCY.ToString();
        }

        private void btnInitReg_Click(object sender, EventArgs e)
        {
            InitReg();
        }
        #endregion
    }
}
