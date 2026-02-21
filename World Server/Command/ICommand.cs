namespace Word_Sever.Command
{
    /// <summary>
    /// 命令接口
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令名称（如 /addmoney）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命令描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 命令用法
        /// </summary>
        string Usage { get; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="args">命令参数</param>
        void Execute(string[] args);
    }
}
