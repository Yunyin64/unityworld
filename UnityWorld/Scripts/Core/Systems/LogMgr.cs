using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityWorld.Core
{
    /// <summary>
    /// 日志等级
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Warn = 1,
        Error = 2,
        Off = 99
    }

    /// <summary>
    /// 单条日志记录
    /// </summary>
    public struct LogEntry
    {
        /// <summary>日志等级</summary>
        public LogLevel Level;
        /// <summary>已格式化的消息文本</summary>
        public string Message;
        /// <summary>模块 Tag（如 "[NpcMgr]"）</summary>
        public string Tag;
        /// <summary>产生时的 UTC 时间戳</summary>
        public DateTime Timestamp;
        /// <summary>产生时的游戏 Tick</summary>
        public int GameTick;

        public override string ToString()
        {
            string lvl = Level switch
            {
                LogLevel.Debug => "DBG",
                LogLevel.Warn => "WRN",
                LogLevel.Error => "ERR",
                _ => "???"
            };
            return $"[{Timestamp.Minute:D2}:{Timestamp.Second:D2}.{Timestamp.Millisecond:D3}] [{lvl}] {Message}";
        }
    }

    /// <summary>
    /// log 控制器，用于官方调试。
    /// 支持日志等级过滤、Tag 过滤、环形缓冲历史、文件写入、帧内聚合。
    /// </summary>
    public class LogMgr : IDomainMgrBase
    {

        public static LogMgr Instance { get; private set; }
        // ────────── IDomainMgrBase ──────────
        public string Name => "LogMgr";
        public string Desc => $"Level={MinLevel} Ring={_ringBuffer.Count}/{RingCapacity}";

        public LogMgr(int seed)
        {
            Instance = this;
        }

        // ────────── 配置 ──────────

        /// <summary>最低输出等级，低于此等级的日志将被丢弃</summary>
        public static LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>环形缓冲容量</summary>
        public static int RingCapacity { get; set; } = 2048;

        /// <summary>是否启用文件写入</summary>
        public static bool FileWriteEnabled { get; set; } = false;

        /// <summary>日志文件路径（相对于工作目录）</summary>
        public static string LogFilePath { get; set; } = "Logs/game.log";

        // ────────── Tag 过滤 ──────────

        /// <summary>
        /// 当非空时，只有 Tag 命中此集合的日志才会输出。
        /// 空集合 = 不过滤（全部输出）。
        /// </summary>
        public static HashSet<string> TagWhitelist { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 黑名单中的 Tag 会被静默丢弃（优先级高于白名单）。
        /// </summary>
        public static HashSet<string> TagBlacklist { get; } = new(StringComparer.OrdinalIgnoreCase);

        // ────────── 环形缓冲 ──────────
        private static LogEntry[] _ringArray = new LogEntry[2048];
        private static int _ringHead = 0;
        private static int _ringCount = 0;

        /// <summary>外部只读访问环形缓冲（最旧 → 最新）</summary>
        private static RingView _ringBuffer = new();

        /// <summary>环形缓冲只读视图</summary>
        public class RingView
        {
            public int Count => _ringCount;
            public LogEntry this[int i]
            {
                get
                {
                    if (i < 0 || i >= _ringCount)
                        throw new IndexOutOfRangeException();
                    int idx = (_ringHead - _ringCount + i + _ringArray.Length) % _ringArray.Length;
                    return _ringArray[idx];
                }
            }

            /// <summary>取最近 n 条日志</summary>
            public List<LogEntry> GetRecent(int n)
            {
                n = Math.Min(n, _ringCount);
                var list = new List<LogEntry>(n);
                for (int i = _ringCount - n; i < _ringCount; i++)
                    list.Add(this[i]);
                return list;
            }
        }

        /// <summary>获取环形缓冲只读视图</summary>
        public static RingView History => _ringBuffer;

        // ────────── 帧内聚合 ──────────
        private static readonly Dictionary<string, int> _frameAggregation = new();
        private static int _lastFlushTick = -1;

        // ────────── 当前游戏 Tick（由外部 Tick 驱动） ──────────
        private static int _currentTick = 0;

        // ────────── 文件写入 ──────────
        private static StreamWriter _fileWriter;

        // ────────── 外部回调（给 Unity Console 或 UI 挂接） ──────────

        /// <summary>
        /// 外部可注册的日志回调，每条日志产生时触发。
        /// 适合桥接 UnityEngine.Debug.Log 或 UI 面板。
        /// </summary>
        public static Action<LogEntry> OnLogEmitted;

        // ================================================================
        //  公共 API（静态，保持现有调用不变）
        // ================================================================

        /// <summary>输出 Debug 级别日志</summary>
        public static void Dbg(string fmt, params object[] objs)
        {
            Log(LogLevel.Debug, fmt, objs);
        }

        /// <summary>输出 Warn 级别日志</summary>
        public static void Warn(string fmt, params object[] objs)
        {
            Log(LogLevel.Warn, fmt, objs);
        }

        /// <summary>输出 Error 级别日志</summary>
        public static void Err(string fmt, params object[] objs)
        {
            Log(LogLevel.Error, fmt, objs);
        }

        // ================================================================
        //  核心日志管线
        // ================================================================

        private static void Log(LogLevel level, string fmt, object[] objs)
        {
            // 1) 等级过滤
            if (level < MinLevel) return;

            // 2) 格式化
            string message;
            try
            {
                message = (objs != null && objs.Length > 0)
                    ? string.Format(fmt, objs)
                    : fmt;
            }
            catch (FormatException)
            {
                message = fmt; // 格式串异常时退化为原始文本
            }

            // 3) 提取 Tag（约定格式 "[XxxMgr] ..."）
            string tag = ExtractTag(message);

            // 4) Tag 过滤
            if (tag.Length > 0)
            {
                if (TagBlacklist.Count > 0 && TagBlacklist.Contains(tag)) return;
                if (TagWhitelist.Count > 0 && !TagWhitelist.Contains(tag)) return;
            }

            // 6) 构造 LogEntry
            var entry = new LogEntry
            {
                Level = level,
                Message = message,
                Tag = tag,
                Timestamp = DateTime.Now,
                GameTick = _currentTick
            };

            // 7) 写入环形缓冲
            PushRing(entry);

            // 8) 输出到 Console
            WriteConsole(entry);

            // 9) 写入文件
            WriteFile(entry);

            // 10) 外部回调
            OnLogEmitted?.Invoke(entry);
        }

        // ================================================================
        //  内部实现
        // ================================================================

        private static string ExtractTag(string msg)
        {
            if (msg.Length < 3 || msg[0] != '[') return string.Empty;
            int end = msg.IndexOf(']', 1);
            if (end < 2) return string.Empty;
            return msg.Substring(1, end - 1);
        }

        private static void PushRing(LogEntry entry)
        {
            if (_ringArray.Length != RingCapacity)
            {
                // 容量变更，重建
                var old = _ringBuffer.GetRecent(_ringCount);
                _ringArray = new LogEntry[RingCapacity];
                _ringHead = 0;
                _ringCount = 0;
                foreach (var e in old) PushRing(e);
                // 再写入当前条
            }
            _ringArray[_ringHead] = entry;
            _ringHead = (_ringHead + 1) % _ringArray.Length;
            if (_ringCount < _ringArray.Length) _ringCount++;
        }

        private static void WriteConsole(LogEntry entry)
        {
            // 彩色输出（仅在支持 ANSI 的终端生效，Unity Editor 会忽略）
            string line = entry.ToString();
            switch (entry.Level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(line);
                    Console.ResetColor();
                    break;
                case LogLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(line);
                    Console.ResetColor();
                    break;
                default:
                    Console.WriteLine(line);
                    break;
            }
        }

        private static void WriteFile(LogEntry entry)
        {
            if (!FileWriteEnabled || _fileWriter == null) return;
            try
            {
                _fileWriter.WriteLine(entry.ToString());
                // Error 级别立即 flush
                if (entry.Level >= LogLevel.Error)
                    _fileWriter.Flush();
            }
            catch
            {
                // 文件写入失败不应中断游戏逻辑
            }
        }

        /// <summary>帧结束时刷出聚合信息</summary>
        private static void FlushAggregation()
        {
            if (_frameAggregation.Count == 0) return;
            foreach (var kv in _frameAggregation)
            {
                if (kv.Value > 1)
                {
                    var summary = new LogEntry
                    {
                        Level = LogLevel.Debug,
                        Message = $"(x{kv.Value}) {kv.Key}",
                        Tag = "Aggregation",
                        Timestamp = DateTime.Now,
                        GameTick = _lastFlushTick
                    };
                    PushRing(summary);
                    WriteConsole(summary);
                    WriteFile(summary);
                }
            }
            _frameAggregation.Clear();
        }

        // ================================================================
        //  文件系统
        // ================================================================

        private static void OpenLogFile()
        {
            if (!FileWriteEnabled) return;
            try
            {
                string dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                _fileWriter = new StreamWriter(LogFilePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = false
                };
                _fileWriter.WriteLine($"=== Log session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _fileWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogMgr] 无法打开日志文件: {ex.Message}");
                _fileWriter = null;
            }
        }

        private static void CloseLogFile()
        {
            if (_fileWriter == null) return;
            try
            {
                FlushAggregation();
                _fileWriter.WriteLine($"=== Log session ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _fileWriter.Flush();
                _fileWriter.Dispose();
            }
            catch { }
            _fileWriter = null;
        }

        // ================================================================
        //  IDomainMgrBase 生命周期
        // ================================================================

        public void Init()
        {
            _ringArray = new LogEntry[RingCapacity];
            _ringHead = 0;
            _ringCount = 0;
            _frameAggregation.Clear();
            _lastFlushTick = -1;
            _currentTick = 0;
            OpenLogFile();
            Dbg("[LogMgr] 初始化完成 Level={0} Ring={1} File={2}",
                MinLevel, RingCapacity, FileWriteEnabled ? LogFilePath : "OFF");
        }

        public void Begin()
        {
        }

        public void Tick(float deltaTime)
        {
            _currentTick++;
            FlushAggregation();
        }

        public void Update()
        {
        }

        public void Render(float dt)
        {
        }

        public void End()
        {
            FlushAggregation();
            CloseLogFile();
            OnLogEmitted = null;
            TagWhitelist.Clear();
            TagBlacklist.Clear();
            Instance = null;
        }

        public IEnumerator Save()
        {
            // 持久化最近日志到存档
            var recent = _ringBuffer.GetRecent(Math.Min(512, _ringCount));
            var sb = new StringBuilder();
            foreach (var e in recent)
                sb.AppendLine(e.ToString());

            string savePath = "Logs/save_log.txt";
            try
            {
                string dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                Dbg("[LogMgr] Save 完成，写出 {0} 条日志", recent.Count);
            }
            catch (Exception ex)
            {
                Err("[LogMgr] Save 失败: {0}", ex.Message);
            }
            yield break;
        }

        public IEnumerator Load()
        {
            string savePath = "Logs/save_log.txt";
            if (File.Exists(savePath))
            {
                Dbg("[LogMgr] Load 发现存档日志文件: {0}", savePath);
            }
            yield break;
        }

        // ================================================================
        //  便捷工具
        // ================================================================

        /// <summary>
        /// 导出环形缓冲中所有日志为纯文本（用于 Debug 面板或复制粘贴）
        /// </summary>
        public static string DumpHistory(int maxLines = int.MaxValue, LogLevel minLevel = LogLevel.Debug)
        {
            var sb = new StringBuilder();
            int count = Math.Min(maxLines, _ringCount);
            int start = _ringCount - count;
            for (int i = start; i < _ringCount; i++)
            {
                var e = _ringBuffer[i];
                if (e.Level >= minLevel)
                    sb.AppendLine(e.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// 按 Tag 查询最近日志
        /// </summary>
        public static List<LogEntry> QueryByTag(string tag, int maxCount = 100)
        {
            var result = new List<LogEntry>();
            for (int i = _ringCount - 1; i >= 0 && result.Count < maxCount; i--)
            {
                var e = _ringBuffer[i];
                if (string.Equals(e.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    result.Add(e);
            }
            result.Reverse();
            return result;
        }

        /// <summary>
        /// 按等级查询最近日志
        /// </summary>
        public static List<LogEntry> QueryByLevel(LogLevel level, int maxCount = 100)
        {
            var result = new List<LogEntry>();
            for (int i = _ringCount - 1; i >= 0 && result.Count < maxCount; i--)
            {
                var e = _ringBuffer[i];
                if (e.Level == level)
                    result.Add(e);
            }
            result.Reverse();
            return result;
        }
    }
}