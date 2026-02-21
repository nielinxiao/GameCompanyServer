using System;
using System.Collections;
using System.Collections.Generic;

public class LogTool
{
    protected Action<string> logAction;
    private ConsoleColor DefaultColor;
    public LogTool(Action<string> logAction,ConsoleColor defaultColor)
    {
        this.logAction = logAction;
        DefaultColor=defaultColor;
        LogGreen("LogTools Loading Successfull...");
    }
    public void LogGreen(string msg)
    {
        PrintBorderedLog("INFO", msg, ConsoleColor.Green);
    }
    public void LogYellow(string msg)
    {
        PrintBorderedLog("INFO", msg, ConsoleColor.Yellow);
    }
    public  void LogWhite(string msg)
    {
        PrintBorderedLog("INFO", msg, ConsoleColor.White);
    }
    public  void LogError(string msg)
    {
        PrintBorderedLog("ERROR", msg, ConsoleColor.Red);
    }
    public  void LogWarning(string msg)
    {
        PrintBorderedLog("WARN", msg, ConsoleColor.DarkRed);
    }

    private void PrintBorderedLog(string level, string msg, ConsoleColor color)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        // 计算显示宽度（中文占2列）
        int msgWidth = GetDisplayWidth(msg);
        int levelWidth = level.Length;
        int timeWidth = time.Length;

        // 各列内容宽度 + 两侧各1空格
        int col1 = Math.Max(levelWidth, 5) + 2;
        int col2 = Math.Max(timeWidth, 8) + 2;
        int col3 = Math.Max(msgWidth, 10) + 2;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("│");
        Console.ForegroundColor = color;
        Console.Write(" " + PadRightDisplay(level, col1 - 1));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("│");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" " + PadRightDisplay(time, col2 - 1));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("│");
        Console.ForegroundColor = color;
        Console.Write(" " + PadRightDisplay(msg, col3 - 1));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("│");
        Console.ForegroundColor = DefaultColor;
    }

    private static int GetDisplayWidth(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        int width = 0;
        foreach (char c in str)
        {
            if (c >= 0x4E00 && c <= 0x9FFF || c >= 0x3000 && c <= 0x303F ||
                c >= 0xFF00 && c <= 0xFFEF || c >= 0x3400 && c <= 0x4DBF ||
                c >= 0xF900 && c <= 0xFAFF)
                width += 2;
            else
                width += 1;
        }
        return width;
    }

    private static string PadRightDisplay(string str, int totalWidth)
    {
        int currentWidth = GetDisplayWidth(str);
        int padding = totalWidth - currentWidth;
        if (padding <= 0) return str;
        return str + new string(' ', padding);
    }
}
