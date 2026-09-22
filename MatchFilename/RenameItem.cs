namespace MatchFilename
{
    /// <summary>
    /// 重命名预览与执行条目
    /// </summary>
    public class RenameItem
    {
        /// <summary>
        /// 来源文件名（含扩展名）
        /// </summary>
        public string SourceFileName { get; set; } = string.Empty;

        /// <summary>
        /// 目标原文件名（含扩展名）
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// 目标新文件名（含扩展名）
        /// </summary>
        public string NewFileName { get; set; } = string.Empty;

        /// <summary>
        /// 目标原文件完整路径
        /// </summary>
        public string OriginalFullPath { get; set; } = string.Empty;

        /// <summary>
        /// 目标新文件完整路径
        /// </summary>
        public string NewFullPath { get; set; } = string.Empty;

        /// <summary>
        /// 两阶段重命名的中间临时路径
        /// </summary>
        public string TempFullPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否与其他项的新文件名产生冲突
        /// </summary>
        public bool HasConflict { get; set; }
    }
}