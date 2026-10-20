using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vfd_monitor_nmodbus
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ConnectionSettings settings;
            try
            {
                settings = ConnectionSettings.Load(ConnectionSettings.DefaultPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"读取连接配置失败：\n{ConnectionSettings.DefaultPath}\n\n{ex.Message}",
                    "配置错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new VfdMonitorForm(settings));
        }
    }
}
