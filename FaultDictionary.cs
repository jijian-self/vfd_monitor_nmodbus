using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfd_monitor_nmodbus
{
    internal class FaultDictionary
    {
        public static readonly Dictionary<ushort, string> LowByteFaults = new Dictionary<ushort, string>
        {
            { 0x01, "加速中过流" },
            { 0x02, "减速中过流" },
            { 0x03, "恒速中过流" },
            { 0x04, "接地保护" },
            { 0x05, "IGBT上下桥短路" },
            { 0x06, "停止中过流" },
            { 0x07, "加速中过压" },
            { 0x08, "减速中过压" },
            { 0x09, "恒速中过压" },
            { 0x0A, "停止中过压" },
            { 0x0B, "加速中欠压" },
            { 0x0C, "减速中欠压" },
            { 0x0D, "恒速中欠压" },
            { 0x0E, "停止中欠压" },
            { 0x0F, "输入缺相保护" },
            { 0x10, "IGBT温度过高" },
            { 0x11, "环境温度过高" },
            { 0x15, "变频器过载" },
            { 0x16, "电机1过载保护" },
            { 0x17, "电机2过载保护" },
            { 0x18, "电机过热PTC" },
            { 0x1A, "过转矩l" },
            { 0x1B, "过转矩2" },
            { 0x1C, "低电流" },
            { 0x1D, "到达限位" },
            { 0x1F, "存储器读出异常" },
            { 0x21, "U相电流检测错误" },
            { 0x22, "V相电流检测错误" },
            { 0x23, "W相电流检测错误" },
            { 0x24, "cc硬件线路异常" },
            { 0x25, "oc硬件线路异常" },
            { 0x33, "外部中断" },
            { 0x34, "密码输入三次错误" },
            { 0x36, "非法通讯命令" },
            { 0x37, "非法通讯地址" },
            { 0x38, "通讯数据错误" },
            { 0x39, "通讯写入只读地址" },
            { 0x3A, "Modbus传输超时" },
            { 0x3E, "减速再生制动动作" },
            { 0x3F, "转差过大" },
            { 0x40, "请重设机种代码" },
            { 0x41, "PG卡硬件错误" },
            { 0x45, "反馈转速发散" },
            { 0x46, "反馈转速偏差过大" },
            { 0x48, "STO1故障" },
            { 0x4C, "STO " },
            { 0x4D, "STO2故障" },
            { 0x4F, "U相过流" },
            { 0x50, "V相过流" },
            { 0x51, "W相过流" },
            { 0x52, "U相输出欠相" },
            { 0x53, "V相输出欠相" },
            { 0x54, "W相输出欠相" },
            { 0x57, "低频过载保护" },
            { 0x65, "CANopen断线" },
            { 0x66, "CANopen断线" },
            { 0x68, "CANopen硬件断线" },
            { 0x69, "CANopen索引错误" },
            { 0x6A, "CANopen站号错误" },
            { 0x6B, "CANopen存储器错误" },
            { 0x78, "EMS过流" },
            { 0x80, "过转矩3" },
            { 0x26, "ov硬件线路异常" },
            { 0x28, "电机参数辨识错误" },
            { 0x29, "PID断线AI2" },
            { 0x2A, "PG反馈设定错误" },
            { 0x2B, "PG反馈断线" },
            { 0x2C, "PG反馈失速" },
            { 0x2D, "PG转差异常" },
            { 0x31, "外部端子异常" },
            { 0x32, "外部端子紧急停止" },
            { 0x81, "过转矩4" },
            { 0x86, "电机3过载保护" },
            { 0x87, "电机4过载保护" },
            { 0x8D, "运行前接地故障" },
            { 0x8E, "参数辨识错误l" },
            { 0x8F, "参数辨识错误2" },
            { 0x92, "断带检测" },
            { 0x93, "张力PID反馈误差检测" }
        };

        private static readonly Dictionary<byte, string> HighByteFaults = new Dictionary<byte, string>
        {
            { 0x01, "通讯指令错误" },
            { 0x02, "通讯地址错误" },
            { 0x03, "通讯数据错误" },
            { 0x04, "变频器无法处理" },
            { 0x05, "通讯传输超时" },
            { 0x07, "参数复制错误" },
            { 0x08, "参数复制错误" },
            { 0x09, "IGBT过热警告" },
            { 0x0A, "环境过热警告" },
            { 0x0B, "PID反馈信号警告" },
            { 0x0D, "低电流警告" },
            { 0x0F, "PG反馈错误" },
            { 0x11, "超速警告" },
            { 0x12, "速度偏差过大" },
            { 0x13, "输入缺相" },
            { 0x14, "过转矩l" },
            { 0x15, "过转矩2" },
            { 0x16, "电机过热" },
            { 0x18, "过转差" },
            { 0x19, "参数辨识中" },
            { 0x1C, "输出缺相警告" },
            { 0x1E, "机种不同复制错误" },
            { 0x1F, "过转矩3" },
            { 0x20, "过转矩4" },
            { 0x24, "CANopen软件断线" },
            { 0x25, "CANopen软件断线" },
            { 0x27, "CANopen硬件断线" },
            { 0x28, "CANopen索引错误" },
            { 0x29, "CANopen站号错误" },
            { 0x2A, "CANopen存储器错误" },
            { 0x2B, "CANopen SDO传输超时" },
            { 0x2C, "CANopen SDO接收溢出" },
            { 0x2D, "CANopen启动错误" },
            { 0x2E, "CANopen格式错误" }
        };

        private static readonly Dictionary<byte, string> ModbusException = new Dictionary<byte, string>
        {
            {0x01, "非法功能码"},
            {0x02, "寄存器地址非法"},
            {0x03, "数据值非法"},
            {0x04, "设备故障"},
            {0x05, "请求被拒绝"},
            {0x06, "设备忙"}
        };

        public static string GetFaultMessage(ushort faultCode)
        {
            if (faultCode == 0)
                return null;

            // 拆分高低字节
            byte high = (byte)(faultCode >> 8);    // 高字节 01
            byte low = (byte)(faultCode & 0xFF);  // 低字节 01

            StringBuilder sb = new StringBuilder();
            if (HighByteFaults.TryGetValue(high, out string hMsg))
                sb.AppendLine(hMsg);
            if (LowByteFaults.TryGetValue(low, out string lMsg))
                sb.AppendLine(lMsg);

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? $"未知故障：0x{faultCode:X4}" : result;
        }

        public static string GetModbusMessage(byte faultCode)
        {
            string errorlog = ModbusException.ContainsKey(faultCode)
                ? $"Modbus 异常：{ModbusException[faultCode]}"
                : $"Modbus 未知异常：{faultCode:X2}";
            return errorlog + " 任务取消";
        }
    }
}
