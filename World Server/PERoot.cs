using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Word_Sever.Tools;

namespace Word_Sever
{
    public class PERoot
    {
        public static int WaitTime=10;
        static void Main(string[] args)
        {
            // 设置控制台 UTF-8 编码（支持中文和特殊字符）
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // 表格形式的启动信息
            var banner = new ConsoleTableFormatter();
            banner.AddColumn("GameCompany 游戏服务器", "值");
            banner.SetHeaderColor(ConsoleColor.Cyan);
            banner.AddRow("版本", "v2.0 (HTTP + JSON)");
            banner.AddRow("启动时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            banner.Render();
            Console.WriteLine();

            OpenProgress progress= new OpenProgress();
            OpenProgress.Instance= progress;
            progress.Start();

            // 等待命令系统初始化完成
            while (OpenProgress.Instance?.commandSystem == null)
            {
                Thread.Sleep(100);
            }

            // 创建增强的控制台输入处理器（支持自动补全）
            ConsoleInputHandler inputHandler = new ConsoleInputHandler(OpenProgress.Instance.commandSystem);

            var tipTable = new ConsoleTableFormatter();
            tipTable.AddColumn("提示");
            tipTable.SetHeaderColor(ConsoleColor.Green);
            tipTable.AddRow("输入 / 自动显示命令，Tab/上下箭头选择，Enter确认");
            tipTable.Render();
            Console.WriteLine();

            // 启动控制台输入监听线程（使用增强输入）
            Thread consoleThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        string input = inputHandler.ReadLine();
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            // 执行命令
                            if (OpenProgress.Instance != null && OpenProgress.Instance.commandSystem != null)
                            {
                                OpenProgress.Instance.commandSystem.ExecuteCommand(input);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PERoot] 控制台输入处理异常: {ex.Message}");
                    }
                }
            });
            consoleThread.IsBackground = true;
            consoleThread.Start();

            while(true)
            {
                progress.Update();
                Thread.Sleep(WaitTime);
            }
        }
    }
}
