using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace MatchFilename
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<RenameItem> _previewList = new();
        private List<(string OriginalPath, string NewPath)> _lastExecutedRenames = new();

        public MainWindow()
        {
            InitializeComponent();
            DgPreview.ItemsSource = _previewList;
            UpdateState();
        }

        #region 状态管理

        private void UpdateState()
        {
            bool hasSource = Directory.Exists(TxtSourceFolder.Text?.Trim());
            bool hasTarget = Directory.Exists(TxtTargetFolder.Text?.Trim());

            if (!hasSource || !hasTarget)
            {
                // 待设定状态
                BtnAnalyze.IsEnabled = false;
                BtnExecute.IsEnabled = false;
                BtnUndo.IsEnabled = false;
                _previewList.Clear();
            }
            else
            {
                // 待分析状态
                BtnAnalyze.IsEnabled = true;
                BtnExecute.IsEnabled = false;
                BtnUndo.IsEnabled = _lastExecutedRenames.Count > 0;
            }
        }

        private void OnFolderSelectionChanged(object sender, TextChangedEventArgs e)
        {
            _previewList.Clear();
            _lastExecutedRenames.Clear();
            UpdateState();
        }

        #endregion

        #region 目录选择事件

        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择来源目录"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtSourceFolder.Text = dialog.FolderName;
            }
        }

        private void BtnBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择目标目录"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtTargetFolder.Text = dialog.FolderName;
            }
        }

        #endregion

        #region 文件检索（过滤隐藏/系统文件）

        private static List<FileInfo> GetVisibleFiles(string folderPath)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            return dirInfo.EnumerateFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
                .OrderBy(f => f.Name, NaturalStringComparer.Instance)
                .ToList();
        }

        #endregion

        #region 分析逻辑

        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            string sourceDir = TxtSourceFolder.Text.Trim();
            string targetDir = TxtTargetFolder.Text.Trim();

            if (!Directory.Exists(sourceDir) || !Directory.Exists(targetDir))
            {
                MessageBox.Show(this, "请先选择有效的来源目录和目标目录！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateState();
                return;
            }

            string suffix = (ChkAddSuffix.IsChecked == true) ? (TxtSuffix.Text ?? string.Empty) : string.Empty;

            // 检查后缀中是否包含 Windows 非法文件名字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (suffix.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show(this, "添加的后缀中包含系统不允许的非法文件名字符（如 \\ / : * ? \" < > | 等），请重新输入！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 获取并过滤隐藏/系统保护文件，使用 Windows 自然排序
                var sourceFiles = GetVisibleFiles(sourceDir);
                var targetFiles = GetVisibleFiles(targetDir);

                if (sourceFiles.Count != targetFiles.Count)
                {
                    MessageBox.Show(this,
                        $"来源目录与目标目录文件数量不一致！\n\n来源目录文件数：{sourceFiles.Count}\n目标目录文件数：{targetFiles.Count}",
                        "文件数不一致",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    _previewList.Clear();
                    BtnExecute.IsEnabled = false;
                    return;
                }

                if (sourceFiles.Count == 0)
                {
                    MessageBox.Show(this, "选定目录中没有检测到任何可见文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _previewList.Clear();
                    BtnExecute.IsEnabled = false;
                    return;
                }

                var items = new List<RenameItem>();
                for (int i = 0; i < sourceFiles.Count; i++)
                {
                    var srcFile = sourceFiles[i];
                    var tgtFile = targetFiles[i];

                    string srcNameWithoutExt = Path.GetFileNameWithoutExtension(srcFile.Name);
                    string tgtExt = tgtFile.Extension;
                    string originalFileName = tgtFile.Name;

                    string newFileName = $"{srcNameWithoutExt}{suffix}{tgtExt}";
                    string newFullPath = Path.Combine(targetDir, newFileName);

                    items.Add(new RenameItem
                    {
                        SourceFileName = srcFile.Name,
                        OriginalFileName = originalFileName,
                        NewFileName = newFileName,
                        OriginalFullPath = tgtFile.FullName,
                        NewFullPath = newFullPath,
                        HasConflict = false
                    });
                }

                // 检测新文件名之间是否存在重名冲突
                var duplicateGroups = items
                    .GroupBy(x => x.NewFileName, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .ToList();

                bool hasConflict = duplicateGroups.Count > 0;

                if (hasConflict)
                {
                    foreach (var group in duplicateGroups)
                    {
                        foreach (var item in group)
                        {
                            item.HasConflict = true;
                        }
                    }
                }

                _previewList.Clear();
                foreach (var item in items)
                {
                    _previewList.Add(item);
                }

                if (hasConflict)
                {
                    BtnExecute.IsEnabled = false;
                    BtnUndo.IsEnabled = false;
                    MessageBox.Show(this, "新文件名有冲突，请核对预览表格中的红色文件名！", "冲突警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    BtnExecute.IsEnabled = true;
                    BtnUndo.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"分析文件时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _previewList.Clear();
                BtnExecute.IsEnabled = false;
            }
        }

        #endregion

        #region 执行更名逻辑（两阶段安全事务执行）

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (_previewList.Count == 0)
            {
                return;
            }

            // 过滤出真正需要重命名（路径不完全一致）的条目
            var itemsToRename = _previewList
                .Where(x => !string.Equals(x.OriginalFullPath, x.NewFullPath, StringComparison.Ordinal))
                .ToList();

            if (itemsToRename.Count == 0)
            {
                MessageBox.Show(this, "所有文件名已符合规则，无需更改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 分配唯一的临时过渡路径
            string targetDir = TxtTargetFolder.Text.Trim();
            foreach (var item in itemsToRename)
            {
                item.TempFullPath = Path.Combine(targetDir, $"__tmp_{Guid.NewGuid():N}_{item.OriginalFileName}");
            }

            var stage1Success = new List<RenameItem>();
            var stage2Success = new List<RenameItem>();
            var failedList = new List<(string OriginalName, string TargetName, string Reason)>();

            // 【阶段 1】：将所有文件重命名为唯一的过渡文件名（解除链式占用与大小写锁定）
            foreach (var item in itemsToRename)
            {
                try
                {
                    File.Move(item.OriginalFullPath, item.TempFullPath);
                    stage1Success.Add(item);
                }
                catch (Exception ex)
                {
                    failedList.Add((item.OriginalFileName, item.NewFileName, ex.Message));
                }
            }

            // 【阶段 2】：如果阶段 1 全部成功，将临时文件重命名为最终名称
            if (failedList.Count == 0)
            {
                foreach (var item in stage1Success)
                {
                    try
                    {
                        File.Move(item.TempFullPath, item.NewFullPath);
                        stage2Success.Add(item);
                    }
                    catch (Exception ex)
                    {
                        failedList.Add((item.OriginalFileName, item.NewFileName, ex.Message));
                    }
                }
            }

            // 异常全量回滚处理
            if (failedList.Count > 0)
            {
                // 1. 将阶段 2 已落地的文件回滚到临时文件
                foreach (var item in stage2Success)
                {
                    try
                    {
                        if (File.Exists(item.NewFullPath))
                        {
                            File.Move(item.NewFullPath, item.TempFullPath);
                        }
                    }
                    catch { }
                }

                // 2. 将所有处于临时状态的文件回滚到原始文件
                foreach (var item in stage1Success)
                {
                    try
                    {
                        if (File.Exists(item.TempFullPath))
                        {
                            File.Move(item.TempFullPath, item.OriginalFullPath);
                        }
                    }
                    catch { }
                }

                var sb = new StringBuilder();
                sb.AppendLine("更名过程中发生错误，所有已修改的文件均已安全恢复原样。");
                sb.AppendLine();
                sb.AppendLine("失败文件列表：");
                foreach (var fail in failedList)
                {
                    sb.AppendLine($"- {fail.OriginalName} -> {fail.TargetName} (原因: {fail.Reason})");
                }

                MessageBox.Show(this, sb.ToString(), "执行失败", MessageBoxButton.OK, MessageBoxImage.Error);

                _previewList.Clear();
                _lastExecutedRenames.Clear();
                UpdateState();
            }
            else
            {
                // 执行成功
                _lastExecutedRenames = itemsToRename.Select(x => (x.OriginalFullPath, x.NewFullPath)).ToList();
                MessageBox.Show(this, $"批量重命名成功！共处理 {_previewList.Count} 个文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                BtnExecute.IsEnabled = false;
                BtnUndo.IsEnabled = _lastExecutedRenames.Count > 0;
            }
        }

        #endregion

        #region 撤销更名逻辑（两阶段安全撤销）

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_lastExecutedRenames.Count == 0)
            {
                MessageBox.Show(this, "没有可撤销的更名操作记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new UndoConfirmDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                string targetDir = TxtTargetFolder.Text.Trim();
                var undoItems = _lastExecutedRenames.Select(r => new
                {
                    CurrentPath = r.NewPath,
                    OriginalPath = r.OriginalPath,
                    TempPath = Path.Combine(targetDir, $"__undo_tmp_{Guid.NewGuid():N}_{Path.GetFileName(r.NewPath)}")
                }).ToList();

                var stage1 = new List<dynamic>();
                var undoErrors = new List<string>();

                // 撤销阶段 1：现文件 -> 临时文件
                foreach (var item in undoItems)
                {
                    try
                    {
                        if (File.Exists(item.CurrentPath))
                        {
                            File.Move(item.CurrentPath, item.TempPath);
                            stage1.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        undoErrors.Add($"文件 {Path.GetFileName(item.CurrentPath)}: {ex.Message}");
                    }
                }

                // 撤销阶段 2：临时文件 -> 原始文件
                int successCount = 0;
                foreach (var item in stage1)
                {
                    try
                    {
                        if (File.Exists(item.TempPath))
                        {
                            File.Move(item.TempPath, item.OriginalPath);
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        undoErrors.Add($"文件 {Path.GetFileName(item.TempPath)} 恢复原名失败: {ex.Message}");
                    }
                }

                _lastExecutedRenames.Clear();
                _previewList.Clear();

                if (undoErrors.Count > 0)
                {
                    MessageBox.Show(this, $"撤销部分完成，共恢复 {successCount} 个文件，以下文件发生错误：\n" + string.Join("\n", undoErrors), "撤销警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(this, $"成功撤销上一次更名操作，共恢复 {successCount} 个文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateState();
            }
        }

        #endregion

        #region 导航与超链接

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // .NET 8 中需显式设置 UseShellExecute = true 才能调用系统默认浏览器打开 URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"打开链接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}