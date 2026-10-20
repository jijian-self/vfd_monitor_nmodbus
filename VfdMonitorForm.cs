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
        private const int CONNECT_TIMEOUT_MILLISECONDS = 3000;
        private const int COMMUNICATION_ERROR_LOG_INTERVAL_MILLISECONDS = 2000;
        private const int STOP_COMMAND_REPEAT_INTERVAL = 100; // 持续停止命令的轮询间隔
        private readonly ConnectionSettings _connectionSettings;
        private int MAX_CHANNEL => _connectionSettings.MaxChannels;
        private int GROUP_SIZE => _connectionSettings.PortsPerIp;
        private const byte SLAVE_ID = 0x01;
        private const byte FUN_READ = 0x03;
        private const byte FUN_WRITE = 0x06;
        private const byte FUN_MULTI_WRITE = 0x10;
        private readonly int REG_START_FREQ_ADDR = 0x0006; //停机下修改频率源为485
        private readonly int REG_START_CMD_ADDR = 0x0005; //停机下修改命令源为485
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
        private double PLOT_X_MAX => MAX_CHANNEL + 0.5;
        private double PLOT_MIN_VISIBLE_CHANNELS => Math.Min(16, MAX_CHANNEL);
        private const string PLOT_FONT_NAME = "Microsoft YaHei";

        public int GetMaxChannel()
        {
            return MAX_CHANNEL;
        }

        public int GetGroupSize()
        {
            return GROUP_SIZE;
        }
        #endregion

        #region 通道启用状态（用户可控制）
        private int[] isChannelReading; // 通道是否正在读取
        private bool[] isChannelFault; // 通道是否故障
        private int[] isChannelReconnecting;
        private DateTime[] lastCommunicationErrorLogTimeUtc;
        #endregion

        #region 私有属性
        private int[] REG_ADDR = { 0x2103, 0x2001, 0x2203, 0x2100 };
        private TcpClient[] vfdSockets;
        private ModbusSerialMaster[] modbusMasters;
        private int[] readPollCounter;
        private double[,] readDataBuffer;
        private bool[,] readDataValid;
        private bool[,] readDataStale;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.Timer logTimer;
        private CancellationTokenSource _continuousStopCancellation;
        private Task _continuousStopTask = Task.CompletedTask;
        private int _pollInProgress;
        private ModbusConnectionFactory _connectionFactory;
        private bool _closeInProgress;
        private bool _closeApproved;
        //private DateTime[] lastCommTime; // 记录每个通道最后一次通信时间
        private ConcurrentQueue<string> logBuffer;
        private ScottPlot.WinForms.FormsPlot _panningPlot;
        private int _panStartPixelX;
        private double _panStartLeft;
        private double _panStartRight;
        #endregion

        public VfdMonitorForm() : this(ConnectionSettings.Load(ConnectionSettings.DefaultPath))
        {
        }

        public VfdMonitorForm(ConnectionSettings settings)
        {
            _connectionSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            Instance = this;
            InitializeComponent();
            Text = $"{MAX_CHANNEL}路VFD变频器监控系统";
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
            _connectionFactory = new ModbusConnectionFactory();
            for (int ch = 0;ch < MAX_CHANNEL; ch++)
            {
                _modbusLock[ch] = new SemaphoreSlim(1);
            }

            vfdSockets = new TcpClient[MAX_CHANNEL];
            modbusMasters = new ModbusSerialMaster[MAX_CHANNEL];
            readPollCounter = new int[MAX_CHANNEL];
            readDataBuffer = new double[MAX_CHANNEL, 4];
            readDataValid = new bool[MAX_CHANNEL, 4];
            readDataStale = new bool[MAX_CHANNEL, 4];
            isChannelReading = new int[MAX_CHANNEL];
            isChannelFault = new bool[MAX_CHANNEL];
            //lastCommTime = new DateTime[MAX_CHANNEL];
            isChannelReconnecting = new int[MAX_CHANNEL];
            lastCommunicationErrorLogTimeUtc = new DateTime[MAX_CHANNEL];
            logBuffer = new ConcurrentQueue<string>();

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                readPollCounter[i] = 0;
                isChannelReading[i] = 0;
                isChannelFault[i] = false;
                //lastCommTime[i] = DateTime.Now;
                isChannelReconnecting[i] = 0;
                lastCommunicationErrorLogTimeUtc[i] = DateTime.MinValue;
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
                for (int dataIndex = 0; dataIndex < 4; dataIndex++)
                {
                    readDataValid[i, dataIndex] = false;
                    readDataStale[i, dataIndex] = false;
                }
                //lastCommTime[i] = DateTime.Now;
            }
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
            btnReadFault.Location = new Point(170, 830 + offsetY);
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
            btnWriteForm.Location = new Point(390, 830 + offsetY);
        }

        private async void InitReg()
        {
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                SendCmd(i, REG_START_CMD_ADDR, 0x0002);
                SendCmd(i, REG_START_FREQ_ADDR, 0x0001);
            }
        }

        private async Task ConnectTcp()
        {
            AppendLog("开始连接启用的变频器...");
            int group_cnt = _connectionSettings.IpAddresses.Length;
            
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
            if (ch < 0 || ch >= MAX_CHANNEL) return;
            if (!database.ChannelFreqDict[ch].Enabled) return;
            if (!IsChannelDisconnected(ch)) return;
            // Prevent duplicate reconnects for the same channel. There is no global
            // reconnect concurrency limit and no retry backoff.
            if (Interlocked.CompareExchange(ref isChannelReconnecting[ch], 1, 0) != 0) return;

            ModbusChannelConnection newConnection = null;
            try
            {
                if (!_connectionSettings.TryGetEndpoint(ch, out string ip, out int port))
                    throw new InvalidOperationException("通道IP映射失败");

                // Only replace transport objects here. Reconnection must never send
                // forward, reverse, or stop commands to the VFD.
                await _modbusLock[ch].WaitAsync();
                try
                {
                    CloseChannelConnectionNoLock(ch);
                }
                finally
                {
                    _modbusLock[ch].Release();
                }

                newConnection = await _connectionFactory.ConnectAsync(
                    ip,
                    port,
                    CONNECT_TIMEOUT_MILLISECONDS);

                await _modbusLock[ch].WaitAsync();
                try
                {
                    if (!isRunning || !database.ChannelFreqDict[ch].Enabled)
                    {
                        newConnection.Dispose();
                        newConnection = null;
                        return;
                    }

                    vfdSockets[ch] = newConnection.Client;
                    modbusMasters[ch] = newConnection.Master;
                    newConnection = null;
                }
                finally
                {
                    _modbusLock[ch].Release();
                }

                AppendLog($"通道{ch + 1} 通信连接成功：{ip}:{port}");
            }
            catch (Exception ex)
            {
                if (newConnection != null) newConnection.Dispose();
                LogCommunicationError(ch, "连接失败：" + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref isChannelReconnecting[ch], 0);
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
            if (Interlocked.CompareExchange(ref _pollInProgress, 1, 0) != 0) return;

            try
            {
                await ProcessAllChannel();
                UpdateDataText();
                UpdateChartUI();
            }
            finally
            {
                Interlocked.Exchange(ref _pollInProgress, 0);
            }
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
                        MarkChannelReadDataStale(ch);
                        _ = ConnectSingleChannelTcp(ch);
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
            try
            {
                readPollCounter[ch]++;

                int read_index = readPollCounter[ch];
                int idx = read_index % 4;
                int addr = REG_ADDR[idx];

                RegisterReadResult result = await SendReadCmdAsync(ch, addr);
                if (!result.TryGetValue(out ushort value))
                {
                    readDataStale[ch, idx] = true;
                    HandleReadFailure(ch, result.ErrorMessage);
                    return;
                }

                HandleReadSuccess(ch, idx);
                if (idx == 3)
                {
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
                    readDataValid[ch, idx] = true;
                }
            }
            finally
            {
                Interlocked.Exchange(ref isChannelReading[ch], 0);
            }
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
                await SendCmdAsync(ch, REG_CTRL_ADDR, value);
                sendCmdCount++;
                RegisterReadResult result = await SendReadCmdAsync(ch, REG_ADDR_OPTIONAL[0]);
                if (!result.TryGetValue(out freq))
                {
                    MarkReadAddressStale(ch, REG_ADDR_OPTIONAL[0]);
                    HandleReadFailure(ch, result.ErrorMessage);
                    continue;
                }

                AppendLog($"ch = {ch} 收到freq-{freq}");

                if (value != REG_STOP_CMD && freq > 0) { AppendLog($"通道{ch + 1} 开始运行"); break; }
                if (value == REG_STOP_CMD && freq == 0) { AppendLog($"通道{ch + 1} 停止运行"); break; }
            }
            AppendLog($"通道{ch + 1} 共发送命令{sendCmdCount}次");
        }

        // 保留给现有 UI 事件的 fire-and-forget 入口；需要等待或取消时使用 SendCmdAsync。
        public async void SendCmd(int ch, int addr, int value)
        {
            await SendCmdAsync(ch, addr, value);
        }

        private async Task SendCmdAsync(int ch, int addr, int value, CancellationToken cancellationToken = default(CancellationToken), bool logSuccess = true)
        {
            if (!database.ChannelFreqDict[ch].Enabled) return;

            await _modbusLock[ch].WaitAsync(cancellationToken);
            try
            {
                if (IsChannelDisconnected(ch)) return;

                ModbusSerialMaster master = modbusMasters[ch];
                await Task.Delay(WRITE_WAIT_DELAY, cancellationToken);
                await master.WriteSingleRegisterAsync(SLAVE_ID, (ushort)addr, (ushort)value);
                if (logSuccess)
                    AppendLog($"通道{ch + 1} 发送指令： addr-{addr} val-{value}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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

        private async Task<RegisterReadResult> SendReadCmdAsync(int ch, int addr)
        {
            await _modbusLock[ch].WaitAsync();
            try
            {
                if (IsChannelDisconnected(ch))
                    return RegisterReadResult.Failed("通信连接不可用");

                ModbusSerialMaster master = modbusMasters[ch];
                await Task.Delay(ch % GROUP_SIZE + 1); // 稍微错开一点时间，避免同时启动的高并发压力
                await Task.Delay(WRITE_WAIT_DELAY);
                ushort[] registers = await master.ReadHoldingRegistersAsync(SLAVE_ID, (ushort)addr, 1);
                if (registers == null || registers.Length == 0)
                    return RegisterReadResult.Failed("未收到寄存器数据");

                return RegisterReadResult.Succeeded(registers[0]);
            }
            catch (SlaveException ex)
            {
                return RegisterReadResult.Failed(
                    "收到故障读写信息：" + FaultDictionary.GetModbusMessage(ex.SlaveExceptionCode));
            }
            catch (TimeoutException ex)
            {
                return RegisterReadResult.Failed("读取超时：" + ex.Message);
            }
            catch (SocketException ex)
            {
                return RegisterReadResult.Failed("TCP连接失败：" + ex.Message);
            }
            catch (Exception ex)
            {
                return RegisterReadResult.Failed("读取错误：" + ex.Message);
            }
            finally
            {
                _modbusLock[ch].Release();
            }
        }

        public async Task<ushort> SendReadCmd(int ch, int addr)
        {
            RegisterReadResult result = await SendReadCmdAsync(ch, addr);
            if (!result.TryGetValue(out ushort value))
            {
                HandleReadFailure(ch, result.ErrorMessage);
                return 0;
            }
            return value;
        }

        private void HandleReadFailure(int ch, string errorMessage)
        {
            LogCommunicationError(ch, "读取失败：" + errorMessage);
        }

        private void MarkChannelReadDataStale(int ch)
        {
            for (int dataIndex = 0; dataIndex < 4; dataIndex++)
            {
                if (readDataValid[ch, dataIndex])
                    readDataStale[ch, dataIndex] = true;
            }
        }

        private void MarkReadAddressStale(int ch, int address)
        {
            for (int dataIndex = 0; dataIndex < 3; dataIndex++)
            {
                if (REG_ADDR[dataIndex] == address && readDataValid[ch, dataIndex])
                    readDataStale[ch, dataIndex] = true;
            }
        }

        private void HandleReadSuccess(int ch, int dataIndex)
        {
            bool wasStale = readDataStale[ch, dataIndex];
            readDataStale[ch, dataIndex] = false;
            if (wasStale)
                AppendLog($"通道{ch + 1} 通信读取已恢复");
        }

        private void LogCommunicationError(int ch, string message)
        {
            DateTime now = DateTime.UtcNow;
            DateTime lastLogTime = lastCommunicationErrorLogTimeUtc[ch];
            if ((now - lastLogTime).TotalMilliseconds <
                COMMUNICATION_ERROR_LOG_INTERVAL_MILLISECONDS)
                return;

            lastCommunicationErrorLogTimeUtc[ch] = now;
            AppendLog($"通道{ch + 1} {message}");
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

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                if (!database.ChannelFreqDict[i].Enabled) continue;
                if (!readDataValid[i, dataIndex]) continue;

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
            bool hasValue = false;
            bool hasNonZeroValue = false;

            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                if (!database.ChannelFreqDict[i].Enabled) continue;
                if (!readDataValid[i, dataIndex]) continue;

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
                sb.AppendLine(
                    $"通道{i + 1} → " +
                    $"{REG_TEXT_OPTIONAL[comboBox1.SelectedIndex]}:{FormatReadData(i, 0)}  " +
                    $"{REG_TEXT_OPTIONAL[comboBox2.SelectedIndex]}:{FormatReadData(i, 1)}  " +
                    $"{REG_TEXT_OPTIONAL[comboBox3.SelectedIndex]}:{FormatReadData(i, 2)}");
            }
            txtData.Text = sb.ToString();
        }

        private string FormatReadData(int ch, int dataIndex)
        {
            if (!readDataValid[ch, dataIndex]) return "--";

            string value = readDataBuffer[ch, dataIndex].ToString();
            if (readDataStale[ch, dataIndex])
                return value + "（上次有效值）";
            return value;
        }

        private void InvalidateReadDataIndex(int dataIndex)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                readDataValid[ch, dataIndex] = false;
                readDataStale[ch, dataIndex] = false;
            }
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
                await CloseAllConnectionsAsync();
                await ConnectTcp();
                mainTimer.Start();
                AppendLog("通道配置已更新，自动重连完成");
            }
            catch (Exception ex)
            {
                AppendLog("自动重连失败：" + ex.Message);
            }
        }

        private async Task CloseAllConnectionsAsync()
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
                    readDataValid[ch, 0] = false;
                    readDataValid[ch, 1] = false;
                    readDataValid[ch, 2] = false;
                    readDataValid[ch, 3] = false;
                    readDataStale[ch, 0] = false;
                    readDataStale[ch, 1] = false;
                    readDataStale[ch, 2] = false;
                    readDataStale[ch, 3] = false;
                }
                UpdateChartUI();
                await CloseAllSocketsAsync();
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

        private async Task CloseAllSocketsAsync()
        {
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                await _modbusLock[i].WaitAsync();
                try
                {
                    CloseChannelConnectionNoLock(i);
                }
                finally
                {
                    _modbusLock[i].Release();
                }
            }
        }

        private void CloseChannelConnectionNoLock(int ch)
        {
            try
            {
                if (modbusMasters[ch] != null) modbusMasters[ch].Dispose();
            }
            catch
            {
            }

            CloseTcpClient(vfdSockets[ch]);
            modbusMasters[ch] = null;
            vfdSockets[ch] = null;
        }

        private void CloseTcpClient(TcpClient client)
        {
            if (client == null) return;
            try
            {
                client.Close();
            }
            catch
            {
            }

            try
            {
                client.Dispose();
            }
            catch
            {
            }
        }
        private bool IsChannelDisconnected(int ch)
        {
            if (modbusMasters[ch] == null) return true;
            return TcpConnectionState.IsDisconnected(vfdSockets[ch]);
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
                tasks.Add(SendCmdAsync(channel, REG_CTRL_ADDR, REG_STOP_CMD));
            }

            await Task.WhenAll(tasks);
        }

        private async Task StartContinuousStopAsync()
        {
            while (_continuousStopTask != null && !_continuousStopTask.IsCompleted)
            {
                CancellationTokenSource cancellation = _continuousStopCancellation;
                if (cancellation != null && !cancellation.IsCancellationRequested)
                {
                    await _continuousStopTask;
                    return;
                }

                await _continuousStopTask;
            }

            _continuousStopCancellation = new CancellationTokenSource();
            _continuousStopTask = SendStopCommandsUntilCancelledAsync(_continuousStopCancellation);
            await _continuousStopTask;
        }

        private void CancelContinuousStop()
        {
            CancellationTokenSource cancellation = _continuousStopCancellation;
            if (cancellation == null || cancellation.IsCancellationRequested) return;

            cancellation.Cancel();
        }

        private async Task SendStopCommandsUntilCancelledAsync(CancellationTokenSource cancellation)
        {
            CancellationToken cancellationToken = cancellation.Token;
            AppendLog("已开始持续发送停止运行命令");

            try
            {
                var channelTasks = new List<Task>();
                for (int ch = 0; ch < MAX_CHANNEL; ch++)
                {
                    int channel = ch;
                    channelTasks.Add(SendStopCommandsForChannelUntilCancelledAsync(
                        channel,
                        cancellationToken));
                }

                await Task.WhenAll(channelTasks);
            }
            catch (OperationCanceledException)
            {
                // 点击其他按钮或关闭窗口时的正常退出路径。
            }
            finally
            {
                if (ReferenceEquals(_continuousStopCancellation, cancellation))
                    _continuousStopCancellation = null;
                cancellation.Dispose();
                AppendLog("已停止持续发送停止运行命令");
            }
        }

        private async Task SendStopCommandsForChannelUntilCancelledAsync(
            int ch,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (database.ChannelFreqDict[ch].Enabled && !IsChannelDisconnected(ch))
                {
                    await SendCmdAsync(
                        ch,
                        REG_CTRL_ADDR,
                        REG_STOP_CMD,
                        cancellationToken,
                        false);
                }

                await Task.Delay(STOP_COMMAND_REPEAT_INTERVAL, cancellationToken);
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (_closeApproved)
            {
                base.OnFormClosing(e);
                return;
            }

            e.Cancel = true;
            if (_closeInProgress) return;
            _closeInProgress = true;

            try
            {
                if (!haveSaved &&
                    MessageBox.Show(
                        "未保存，是否需要保存写入频率设置",
                        "提示：",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question) == DialogResult.OK)
                {
                    using (OpenFileDialog ofd = new OpenFileDialog { Filter = "数据文件|*.csv" })
                    {
                        if (ofd.ShowDialog() != DialogResult.OK) return;
                        database.WriteChannelFreq(ofd.FileName);
                    }
                    haveSaved = true;
                }

                CancelContinuousStop();
                isRunning = false;
                mainTimer.Stop();
                logTimer.Stop();
                await CloseAllSocketsAsync();
                mainTimer.Dispose();
                logTimer.Dispose();
                _closeApproved = true;
            }
            catch (Exception ex)
            {
                AppendLog("关闭程序时释放通信连接失败：" + ex.Message);
                _closeApproved = true;
            }
            finally
            {
                if (!_closeApproved) _closeInProgress = false;
            }

            if (_closeApproved) Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
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
                CancelContinuousStop();
                mainTimer.Stop();
                isRunning = false;
                UpdateUiState(false);
                btnBackward.Enabled = false;
                btnForward.Enabled = false;
                btnStopOperation.Enabled = false;
                await StopAllChannelAsync();
                await CloseAllSocketsAsync();
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

        private void btnReadFault_Click(object sender, EventArgs e)
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
            CancelContinuousStop();
            await _continuousStopTask;
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
        }

        private async void btnBackward_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                AppendLog("请先点击启动控制启动系统");
                return;
            }
            CancelContinuousStop();
            await _continuousStopTask;
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
        }

        private async void btnStopOperation_Click(object sender, EventArgs e)
        {
            btnBackward.Enabled = true;
            btnForward.Enabled = true;
            await StartContinuousStopAsync();
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
            InvalidateReadDataIndex(0);
            txtData.Clear();
            chartA.Plot.Title("数据A - " + REG_TEXT_OPTIONAL[comboBox1.SelectedIndex]);
            ApplyPlotFont(chartA);
            chartA.Refresh();
            AppendLog("读取信息已修改");
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            REG_ADDR[1] = REG_ADDR_OPTIONAL[comboBox2.SelectedIndex];
            InvalidateReadDataIndex(1);
            txtData.Clear();
            chartB.Plot.Title("数据B - " + REG_TEXT_OPTIONAL[comboBox2.SelectedIndex]);
            ApplyPlotFont(chartB);
            chartB.Refresh();
            AppendLog("读取信息已修改");
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            REG_ADDR[2] = REG_ADDR_OPTIONAL[comboBox3.SelectedIndex];
            InvalidateReadDataIndex(2);
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

        private void btnWriteForm_Click(object sender, EventArgs e)
        {
            WriteForm form = new WriteForm();
            form.Show();
        }
        #endregion
    }
}
