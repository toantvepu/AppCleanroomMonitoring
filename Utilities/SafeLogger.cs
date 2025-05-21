using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppCleanRoom.Utilities
{
    public static class SafeLogger
    {
        private static readonly Queue<string> _logQueue = new Queue<string>();
        private static readonly object _logLock = new object();
        private static readonly Thread _loggerThread;
        private static volatile bool _isRunning = true;

        static SafeLogger()
        {
            // Khởi tạo thread xử lý log
            _loggerThread = new Thread(ProcessLogQueue);
            _loggerThread.IsBackground = true;
            _loggerThread.Start();
        }

        public static void Log(string message)
        {
            lock (_logLock) {
                _logQueue.Enqueue(message);
                Monitor.Pulse(_logLock);
            }
        }

        private static void ProcessLogQueue()
        {
            while (_isRunning) {
                string message = null;

                lock (_logLock) {
                    while (_logQueue.Count == 0 && _isRunning) {
                        Monitor.Wait(_logLock, 1000);
                    }

                    if (_logQueue.Count > 0) {
                        message = _logQueue.Dequeue();
                    }
                }

                if (message != null) {
                    try {
                        // Ghi log vào file
                        File.AppendAllText("application.log", message + Environment.NewLine);
                    }
                    catch { /* Bỏ qua lỗi ghi file */ }
                }
            }
        }

        public static void Shutdown()
        {
            _isRunning = false;
            lock (_logLock) {
                Monitor.PulseAll(_logLock);
            }
        }
    }
}
