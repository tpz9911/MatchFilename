using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MatchFilename
{
    /// <summary>
    /// 与 Windows 资源管理器一致的文件名自然排序比较器
    /// </summary>
    public class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new();

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            return StrCmpLogicalW(x, y);
        }
    }
}