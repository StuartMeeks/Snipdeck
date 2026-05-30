using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.App
{
    /// <summary>
    /// Best-effort writer for unhandled-exception diagnostics. Designed to
    /// never throw — if logging itself fails, we silently give up rather
    /// than turning a recovered exception into a fatal crash.
    /// </summary>
    internal static class CrashLog
    {
        private const string _logFileName = "unhandled.log";
        private const int _maxLogBytes = 5 * 1024 * 1024;
        private static readonly Lock _writeLock = new();

        public static void Write(string source, Exception exception)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(exception);

                var paths = App.Services?.GetService<IPathProvider>();
                if (paths is null)
                {
                    return;
                }

                _ = Directory.CreateDirectory(paths.LogsDirectory);
                var logPath = Path.Combine(paths.LogsDirectory, _logFileName);
                var text = Format(source, exception);

                lock (_writeLock)
                {
                    RotateIfTooLarge(logPath);
                    File.AppendAllText(logPath, text);
                }
            }
            catch
            {
                // Logger of last resort: must not throw, even if disk is
                // full, the path is locked, or DI hasn't built yet.
            }
        }

        private static void RotateIfTooLarge(string logPath)
        {
            try
            {
                var info = new FileInfo(logPath);
                if (!info.Exists || info.Length < _maxLogBytes)
                {
                    return;
                }

                var rotated = logPath + ".1";
                if (File.Exists(rotated))
                {
                    File.Delete(rotated);
                }
                File.Move(logPath, rotated);
            }
            catch
            {
                // Best-effort rotation; if it fails we'll just keep appending.
            }
        }

        private static string Format(string source, Exception exception)
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine(new string('=', 72));
            _ = sb.Append(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture))
                  .Append("  ")
                  .AppendLine(source);
            AppendException(sb, exception, depth: 0);
            _ = sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendException(StringBuilder sb, Exception ex, int depth)
        {
            var indent = new string(' ', depth * 2);

            _ = sb.Append(indent).Append("Type: ").AppendLine(ex.GetType().FullName ?? ex.GetType().Name);
            if (ex is COMException)
            {
                _ = sb.Append(indent).Append("HRESULT: 0x")
                      .AppendLine(ex.HResult.ToString("X8", CultureInfo.InvariantCulture));
            }
            _ = sb.Append(indent).Append("Message: ").AppendLine(ex.Message);
            if (!string.IsNullOrEmpty(ex.Source))
            {
                _ = sb.Append(indent).Append("Source: ").AppendLine(ex.Source);
            }
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                _ = sb.Append(indent).AppendLine("Stack:");
                foreach (var line in ex.StackTrace.Split('\n'))
                {
                    _ = sb.Append(indent).Append("  ").AppendLine(line.TrimEnd('\r'));
                }
            }
            if (ex.InnerException is not null)
            {
                _ = sb.Append(indent).AppendLine("Inner:");
                AppendException(sb, ex.InnerException, depth + 1);
            }
        }
    }
}
