using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Word_Sever.Command;

namespace Word_Sever.Tools
{
    /// <summary>
    /// 控制台输入处理器 - 支持命令自动补全（类似 Minecraft）
    /// </summary>
    public class ConsoleInputHandler
    {
        private CommandSystem _commandSystem;
        private StringBuilder _currentInput;
        private int _cursorPosition;
        private List<string> _suggestions;
        private int _selectedSuggestionIndex;
        private bool _showingSuggestions;

        public ConsoleInputHandler(CommandSystem commandSystem)
        {
            _commandSystem = commandSystem;
            _currentInput = new StringBuilder();
            _cursorPosition = 0;
            _suggestions = new List<string>();
            _selectedSuggestionIndex = -1;
            _showingSuggestions = false;
        }

        /// <summary>
        /// 读取一行输入（支持自动补全）
        /// </summary>
        public string ReadLine()
        {
            _currentInput.Clear();
            _cursorPosition = 0;
            _suggestions.Clear();
            _selectedSuggestionIndex = -1;
            _showingSuggestions = false;

            // 确保有足够的空间显示建议（至少需要8行：输入行 + 标题 + 5个命令 + "还有..."）
            if (Console.CursorTop > Console.BufferHeight - 10)
            {
                // 滚动到合适的位置
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.Write("> ");
            int promptLength = 2;

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        // 如果正在显示建议且选中了某个命令，使用选中的命令
                        if (_showingSuggestions && _selectedSuggestionIndex >= 0 && _selectedSuggestionIndex < _suggestions.Count)
                        {
                            _currentInput.Clear();
                            _currentInput.Append(_suggestions[_selectedSuggestionIndex]);
                            _cursorPosition = _currentInput.Length;
                            RedrawLine(promptLength);
                            ClearSuggestions();
                            _showingSuggestions = false;
                        }
                        Console.WriteLine();
                        return _currentInput.ToString();

                    case ConsoleKey.Backspace:
                        if (_cursorPosition > 0)
                        {
                            _currentInput.Remove(_cursorPosition - 1, 1);
                            _cursorPosition--;
                            RedrawLine(promptLength);
                            UpdateSuggestions();
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (_cursorPosition < _currentInput.Length)
                        {
                            _currentInput.Remove(_cursorPosition, 1);
                            RedrawLine(promptLength);
                            UpdateSuggestions();
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (_cursorPosition > 0)
                        {
                            _cursorPosition--;
                            try
                            {
                                int targetCol = Math.Min(promptLength + _cursorPosition, Console.BufferWidth - 1);
                                Console.SetCursorPosition(targetCol, Console.CursorTop);
                            }
                            catch { /* 忽略异常 */ }
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (_cursorPosition < _currentInput.Length)
                        {
                            _cursorPosition++;
                            try
                            {
                                int targetCol = Math.Min(promptLength + _cursorPosition, Console.BufferWidth - 1);
                                Console.SetCursorPosition(targetCol, Console.CursorTop);
                            }
                            catch { /* 忽略异常 */ }
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        if (_showingSuggestions && _suggestions.Count > 0)
                        {
                            _selectedSuggestionIndex--;
                            if (_selectedSuggestionIndex < 0)
                                _selectedSuggestionIndex = _suggestions.Count - 1;
                            ShowSuggestions(promptLength);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (_showingSuggestions && _suggestions.Count > 0)
                        {
                            _selectedSuggestionIndex++;
                            if (_selectedSuggestionIndex >= _suggestions.Count)
                                _selectedSuggestionIndex = 0;
                            ShowSuggestions(promptLength);
                        }
                        break;

                    case ConsoleKey.Tab:
                        // Tab 键触发自动补全
                        if (_suggestions.Count == 1)
                        {
                            // 只有一个建议，直接补全
                            _currentInput.Clear();
                            _currentInput.Append(_suggestions[0]);
                            _cursorPosition = _currentInput.Length;
                            RedrawLine(promptLength);
                            ClearSuggestions();
                            _showingSuggestions = false;
                        }
                        else if (_suggestions.Count > 1)
                        {
                            // 多个建议，切换到下一个
                            _selectedSuggestionIndex++;
                            if (_selectedSuggestionIndex >= _suggestions.Count)
                                _selectedSuggestionIndex = 0;
                            ShowSuggestions(promptLength);
                        }
                        break;

                    case ConsoleKey.Escape:
                        // ESC 键取消建议
                        ClearSuggestions();
                        _showingSuggestions = false;
                        break;

                    default:
                        // 普通字符输入
                        if (!char.IsControl(keyInfo.KeyChar))
                        {
                            _currentInput.Insert(_cursorPosition, keyInfo.KeyChar);
                            _cursorPosition++;
                            RedrawLine(promptLength);
                            UpdateSuggestions();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 更新命令建议
        /// </summary>
        private void UpdateSuggestions()
        {
            _suggestions.Clear();
            _selectedSuggestionIndex = -1;

            string input = _currentInput.ToString();

            // 只有以 / 开头才显示建议
            if (input.StartsWith("/"))
            {
                var allCommands = _commandSystem.GetAllCommands();
                foreach (var cmd in allCommands)
                {
                    if (cmd.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    {
                        _suggestions.Add(cmd);
                    }
                }

                if (_suggestions.Count > 0)
                {
                    _showingSuggestions = true;
                    _selectedSuggestionIndex = 0;
                    ShowSuggestions(2); // promptLength = 2 ("> ")
                }
                else
                {
                    _showingSuggestions = false;
                    ClearSuggestions();
                }
            }
            else
            {
                _showingSuggestions = false;
                ClearSuggestions();
            }
        }

        /// <summary>
        /// 显示命令建议列表
        /// </summary>
        private void ShowSuggestions(int promptLength)
        {
            if (!_showingSuggestions || _suggestions.Count == 0)
                return;

            // 保存当前光标位置
            int currentLine = Console.CursorTop;
            int currentCol = Console.CursorLeft;

            // 计算可用空间（避免超出缓冲区）
            int availableLines = Console.BufferHeight - currentLine - 2; // 减2留出余量
            if (availableLines < 3)
            {
                // 空间不足，不显示建议
                return;
            }

            // 限制显示数量（最多5个，但不能超过可用空间）
            int maxDisplay = Math.Min(5, availableLines - 2); // 减2：标题行 + 可能的"还有..."行
            int displayCount = Math.Min(maxDisplay, _suggestions.Count);

            // 清除之前的建议
            for (int i = 0; i < displayCount + 2; i++)
            {
                int lineIndex = currentLine + 1 + i;
                if (lineIndex < Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, lineIndex);
                    Console.Write(new string(' ', Console.BufferWidth - 1));
                }
            }

            // 显示标题
            if (currentLine + 1 < Console.BufferHeight)
            {
                Console.SetCursorPosition(0, currentLine + 1);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"━━ 可用命令 ({_suggestions.Count}) ━━");
                Console.ResetColor();
            }

            // 显示建议
            for (int i = 0; i < displayCount; i++)
            {
                int lineIndex = currentLine + 2 + i;
                if (lineIndex >= Console.BufferHeight)
                    break;

                Console.SetCursorPosition(2, lineIndex);

                if (i == _selectedSuggestionIndex)
                {
                    // 高亮选中的命令
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"▶ {_suggestions[i].PadRight(Math.Min(40, Console.BufferWidth - 5))}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"  {_suggestions[i]}");
                    Console.ResetColor();
                }
            }

            // 显示"还有..."提示
            if (_suggestions.Count > displayCount)
            {
                int moreLineIndex = currentLine + 2 + displayCount;
                if (moreLineIndex < Console.BufferHeight)
                {
                    Console.SetCursorPosition(2, moreLineIndex);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"... 还有 {_suggestions.Count - displayCount} 个命令");
                    Console.ResetColor();
                }
            }

            // 恢复光标位置
            if (currentLine < Console.BufferHeight && currentCol < Console.BufferWidth)
            {
                Console.SetCursorPosition(currentCol, currentLine);
            }
        }

        /// <summary>
        /// 清除建议列表
        /// </summary>
        private void ClearSuggestions()
        {
            if (!_showingSuggestions)
                return;

            int currentLine = Console.CursorTop;
            int currentCol = Console.CursorLeft;

            // 清除建议区域（最多 7 行：标题 + 5个命令 + 可能的"还有..."）
            int maxLinesToClear = Math.Min(7, Console.BufferHeight - currentLine - 1);
            for (int i = 0; i < maxLinesToClear; i++)
            {
                int lineIndex = currentLine + 1 + i;
                if (lineIndex >= Console.BufferHeight)
                    break;

                try
                {
                    Console.SetCursorPosition(0, lineIndex);
                    Console.Write(new string(' ', Console.BufferWidth - 1));
                }
                catch
                {
                    // 忽略超出范围的错误
                    break;
                }
            }

            // 恢复光标位置
            if (currentLine < Console.BufferHeight && currentCol < Console.BufferWidth)
            {
                try
                {
                    Console.SetCursorPosition(currentCol, currentLine);
                }
                catch
                {
                    // 如果恢复失败，设置到行首
                    Console.SetCursorPosition(0, currentLine);
                }
            }
        }

        /// <summary>
        /// 重绘当前输入行
        /// </summary>
        private void RedrawLine(int promptLength)
        {
            int currentLine = Console.CursorTop;

            // 边界检查
            if (currentLine >= Console.BufferHeight)
            {
                currentLine = Console.BufferHeight - 1;
            }

            try
            {
                // 清除当前行
                Console.SetCursorPosition(0, currentLine);
                Console.Write(new string(' ', Console.BufferWidth - 1));

                // 重新绘制
                Console.SetCursorPosition(0, currentLine);
                Console.Write("> ");
                Console.Write(_currentInput.ToString());

                // 设置光标位置（确保不超出行宽）
                int targetCol = Math.Min(promptLength + _cursorPosition, Console.BufferWidth - 1);
                Console.SetCursorPosition(targetCol, currentLine);
            }
            catch (ArgumentOutOfRangeException)
            {
                // 如果发生异常，重置到行首
                Console.SetCursorPosition(0, Math.Min(currentLine, Console.BufferHeight - 1));
                Console.Write("> ");
                Console.Write(_currentInput.ToString());
            }
        }
    }
}
