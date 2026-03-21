using System;

namespace OpalStudio.CodePreview.Editor.Data
{
      [Serializable]
      internal class FileInfo
      {
            internal DateTimeOffset LastModifiedTime;
            internal long FileSize;
            internal int TotalLines;
            internal int TotalWords;
            internal int TotalChars;
            internal int CommentLines;

            internal string FormattedSize => FormatFileSize(FileSize);

            private static string FormatFileSize(long bytes)
            {
                  string[] sizes = { "B", "KB", "MB", "GB" };
                  double len = bytes;
                  int order = 0;

                  while (len >= 1024 && order < sizes.Length - 1)
                  {
                        order++;
                        len /= 1024;
                  }

                  return $"{len:0.##} {sizes[order]}";
            }
      }
}