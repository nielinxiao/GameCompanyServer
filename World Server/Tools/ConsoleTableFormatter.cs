using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Word_Sever
{
    /// <summary>
    /// 控制台表格美化工具 - 用于漂亮地显示结构化数据
    /// </summary>
    public class ConsoleTableFormatter
    {
        private readonly List<string> _columns;
        private readonly List<List<string>> _rows;
        private ConsoleColor _headerColor = ConsoleColor.Cyan;
        private ConsoleColor _borderColor = ConsoleColor.DarkGray;
        private bool _showBorder = true;
        private List<int> _columnWidths;  // 持久化的列宽

        public ConsoleTableFormatter()
        {
            _columns = new List<string>();
            _rows = new List<List<string>>();
        }

        /// <summary>
        /// 计算字符串在控制台中的显示宽度（中文/全角字符占2列）
        /// </summary>
        private static int GetDisplayWidth(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            int width = 0;
            foreach (char c in str)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)      // CJK统一汉字
                    width += 2;
                else if (c >= 0x3000 && c <= 0x303F)  // CJK标点
                    width += 2;
                else if (c >= 0xFF00 && c <= 0xFFEF)  // 全角字符
                    width += 2;
                else if (c >= 0x3400 && c <= 0x4DBF)  // CJK扩展A
                    width += 2;
                else if (c >= 0xF900 && c <= 0xFAFF)  // CJK兼容
                    width += 2;
                else
                    width += 1;
            }
            return width;
        }

        /// <summary>
        /// 用空格补齐到指定显示宽度
        /// </summary>
        private static string PadRightDisplay(string str, int totalWidth)
        {
            int currentWidth = GetDisplayWidth(str);
            int padding = totalWidth - currentWidth;
            if (padding <= 0) return str;
            return str + new string(' ', padding);
        }

        /// <summary>
        /// 截断字符串到指定显示宽度（考虑中文双宽度）
        /// </summary>
        private static string TruncateDisplay(string str, int maxWidth)
        {
            if (string.IsNullOrEmpty(str)) return str;

            int currentWidth = 0;
            int charCount = 0;

            foreach (char c in str)
            {
                int charWidth = (c >= 0x4E00 && c <= 0x9FFF) ||
                                (c >= 0x3000 && c <= 0x303F) ||
                                (c >= 0xFF00 && c <= 0xFFEF) ||
                                (c >= 0x3400 && c <= 0x4DBF) ||
                                (c >= 0xF900 && c <= 0xFAFF) ? 2 : 1;

                if (currentWidth + charWidth > maxWidth - 2)  // 留2个字符给省略号
                    break;

                currentWidth += charWidth;
                charCount++;
            }

            if (charCount < str.Length)
                return str.Substring(0, charCount) + "..";

            return str;
        }

        /// <summary>
        /// 截断并补齐字符串到指定显示宽度
        /// </summary>
        private static string FitToWidth(string str, int width)
        {
            string truncated = TruncateDisplay(str, width);
            return PadRightDisplay(truncated, width);
        }

        /// <summary>
        /// 添加列
        /// </summary>
        public ConsoleTableFormatter AddColumn(params string[] names)
        {
            _columns.AddRange(names);
            return this;
        }

        /// <summary>
        /// 添加行
        /// </summary>
        public ConsoleTableFormatter AddRow(params object[] values)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("请先添加列");

            var row = values.Select(v => v?.ToString() ?? "").ToList();
            _rows.Add(row);

            // 更新列宽（如果新行内容更宽）
            UpdateColumnWidths(row);

            return this;
        }

        /// <summary>
        /// 更新列宽（处理新添加的行）
        /// 注意：如果使用 SetColumnWidths 设置了固定列宽，此方法不会更新列宽
        /// </summary>
        private void UpdateColumnWidths(List<string> newRow)
        {
            // 如果列宽为空，或者是通过 RenderHeader 自动初始化的，则可以更新
            // 但如果是通过 SetColumnWidths 手动设置的，则保持不变
            // 这里简化处理：不自动更新，因为我们使用 FitToWidth 来截断内容
            // 如果需要自动扩展，可以在这里添加逻辑
        }

        /// <summary>
        /// 设置表头颜色
        /// </summary>
        public ConsoleTableFormatter SetHeaderColor(ConsoleColor color)
        {
            _headerColor = color;
            return this;
        }

        /// <summary>
        /// 设置边框颜色
        /// </summary>
        public ConsoleTableFormatter SetBorderColor(ConsoleColor color)
        {
            _borderColor = color;
            return this;
        }

        /// <summary>
        /// 显示或隐藏边框
        /// </summary>
        public ConsoleTableFormatter ShowBorder(bool show)
        {
            _showBorder = show;
            return this;
        }

        /// <summary>
        /// 设置固定的列宽（用于确保表格对齐）
        /// </summary>
        public ConsoleTableFormatter SetColumnWidths(params int[] widths)
        {
            _columnWidths = new List<int>(widths);
            return this;
        }

        /// <summary>
        /// 渲染表格到控制台
        /// </summary>
        public void Render()
        {
            if (_columns.Count == 0)
                return;

            // 计算每列的最大显示宽度（考虑中文双宽度）
            var columnWidths = new List<int>();
            for (int i = 0; i < _columns.Count; i++)
            {
                int maxWidth = GetDisplayWidth(_columns[i]);
                foreach (var row in _rows)
                {
                    if (i < row.Count)
                        maxWidth = Math.Max(maxWidth, GetDisplayWidth(row[i]));
                }
                columnWidths.Add(maxWidth + 2); // 两边各留1个空格
            }

            // 绘制表格
            if (_showBorder)
                DrawBorder(columnWidths, '┌', '─', '┬', '┐');

            DrawHeader(columnWidths);

            if (_showBorder)
                DrawBorder(columnWidths, '├', '─', '┼', '┤');

            foreach (var row in _rows)
                DrawRow(row, columnWidths);

            if (_showBorder)
                DrawBorder(columnWidths, '└', '─', '┴', '┘');

            Console.ResetColor();
        }

        /// <summary>
        /// 只渲染表头（用于首次初始化）
        /// </summary>
        /// <param name="minColumnWidth">每列的最小宽度（默认20，如果已通过SetColumnWidths设置则忽略）</param>
        public void RenderHeader(int minColumnWidth = 20)
        {
            if (_columns.Count == 0)
                return;

            // 如果没有手动设置列宽，则初始化列宽（基于表头，但至少为最小宽度）
            if (_columnWidths == null)
            {
                _columnWidths = new List<int>();
                for (int i = 0; i < _columns.Count; i++)
                {
                    int width = Math.Max(GetDisplayWidth(_columns[i]) + 2, minColumnWidth);
                    _columnWidths.Add(width);
                }
            }

            // 绘制表格头部
            if (_showBorder)
                DrawBorder(_columnWidths, '┌', '─', '┬', '┐');

            DrawHeader(_columnWidths);

            if (_showBorder)
                DrawBorder(_columnWidths, '├', '─', '┼', '┤');

            Console.ResetColor();
        }

        /// <summary>
        /// 只渲染最后添加的一行
        /// </summary>
        public void RenderLastRow()
        {
            if (_columns.Count == 0 || _rows.Count == 0)
                return;

            // 使用持久化的列宽
            if (_columnWidths == null)
                _columnWidths = GetColumnWidths();

            DrawRow(_rows[_rows.Count - 1], _columnWidths);
            Console.ResetColor();
        }

        /// <summary>
        /// 渲染表格底部边框（用于表格结束）
        /// </summary>
        public void RenderFooter()
        {
            if (_columns.Count == 0)
                return;

            // 使用持久化的列宽
            if (_columnWidths == null)
                _columnWidths = GetColumnWidths();

            if (_showBorder)
                DrawBorder(_columnWidths, '└', '─', '┴', '┘');

            Console.ResetColor();
        }

        /// <summary>
        /// 计算每列的最大显示宽度
        /// </summary>
        private List<int> GetColumnWidths()
        {
            var columnWidths = new List<int>();
            for (int i = 0; i < _columns.Count; i++)
            {
                int maxWidth = GetDisplayWidth(_columns[i]);
                foreach (var row in _rows)
                {
                    if (i < row.Count)
                        maxWidth = Math.Max(maxWidth, GetDisplayWidth(row[i]));
                }
                columnWidths.Add(maxWidth + 2); // 两边各留1个空格
            }
            return columnWidths;
        }

        private void DrawHeader(List<int> columnWidths)
        {
            Console.ForegroundColor = _headerColor;
            if (_showBorder)
            {
                Console.ForegroundColor = _borderColor;
                Console.Write("│");
            }

            for (int i = 0; i < _columns.Count; i++)
            {
                Console.ForegroundColor = _headerColor;
                Console.Write(" " + FitToWidth(_columns[i], columnWidths[i] - 1));

                if (_showBorder)
                {
                    Console.ForegroundColor = _borderColor;
                    Console.Write("│");
                }
            }
            Console.WriteLine();
        }

        private void DrawRow(List<string> row, List<int> columnWidths)
        {
            if (_showBorder)
            {
                Console.ForegroundColor = _borderColor;
                Console.Write("│");
            }

            for (int i = 0; i < _columns.Count; i++)
            {
                Console.ResetColor();
                string value = i < row.Count ? row[i] : "";

                Console.Write(" " + FitToWidth(value, columnWidths[i] - 1));

                if (_showBorder)
                {
                    Console.ForegroundColor = _borderColor;
                    Console.Write("│");
                }
            }
            Console.WriteLine();
        }

        private void DrawBorder(List<int> columnWidths, char left, char middle, char cross, char right)
        {
            Console.ForegroundColor = _borderColor;
            Console.Write(left);

            for (int i = 0; i < columnWidths.Count; i++)
            {
                Console.Write(new string(middle, columnWidths[i]));
                if (i < columnWidths.Count - 1)
                    Console.Write(cross);
            }

            Console.WriteLine(right);
        }

        /// <summary>
        /// 快速创建并显示表格
        /// </summary>
        public static void QuickRender(string[] columns, List<object[]> rows, string title = null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n╔═══ {title} ═══");
                Console.ResetColor();
            }

            var table = new ConsoleTableFormatter();
            table.AddColumn(columns);

            foreach (var row in rows)
                table.AddRow(row);

            table.Render();
        }
    }

    /// <summary>
    /// 简化的键值对展示工具
    /// </summary>
    public class ConsoleKeyValueFormatter
    {
        private readonly List<KeyValuePair<string, object>> _items;
        private ConsoleColor _keyColor = ConsoleColor.Cyan;
        private ConsoleColor _valueColor = ConsoleColor.White;
        private string _separator = ": ";

        public ConsoleKeyValueFormatter()
        {
            _items = new List<KeyValuePair<string, object>>();
        }

        public ConsoleKeyValueFormatter Add(string key, object value)
        {
            _items.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        public ConsoleKeyValueFormatter SetKeyColor(ConsoleColor color)
        {
            _keyColor = color;
            return this;
        }

        public ConsoleKeyValueFormatter SetValueColor(ConsoleColor color)
        {
            _valueColor = color;
            return this;
        }

        public ConsoleKeyValueFormatter SetSeparator(string separator)
        {
            _separator = separator;
            return this;
        }

        public void Render()
        {
            if (_items.Count == 0)
                return;

            int maxKeyLength = _items.Max(item => item.Key.Length);

            foreach (var item in _items)
            {
                Console.ForegroundColor = _keyColor;
                Console.Write("  " + item.Key.PadRight(maxKeyLength));

                Console.ResetColor();
                Console.Write(_separator);

                Console.ForegroundColor = _valueColor;
                Console.WriteLine(item.Value?.ToString() ?? "(null)");
            }

            Console.ResetColor();
        }

        /// <summary>
        /// 快速显示键值对
        /// </summary>
        public static void QuickRender(Dictionary<string, object> data, string title = null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n{title}");
                Console.ResetColor();
            }

            var formatter = new ConsoleKeyValueFormatter();
            foreach (var item in data)
                formatter.Add(item.Key, item.Value);

            formatter.Render();
        }
    }
}
