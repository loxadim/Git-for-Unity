using System;

namespace Unity.VersionControl.Git
{
    class LogFacade : ILogging
    {
        private readonly string context;
        private readonly LogAdapterBase logger;
        private bool? traceEnabled;

        public bool TracingEnabled
        {
            get => traceEnabled.HasValue ? traceEnabled.Value : LogHelper.TracingEnabled;
            set
            {
                if (traceEnabled.HasValue)
                    traceEnabled = value;
                else
                    LogHelper.TracingEnabled = value;
            }
        }

        public LogFacade(string context)
        {
            this.context = context;
            logger = LogHelper.LogAdapter;
        }

        public LogFacade(string context, LogAdapterBase logger, bool traceEnabled = false)
        {
            this.context = context;
            this.logger = logger;
            this.traceEnabled = traceEnabled;
        }

        public void Info(string message)
        {
            logger.Info(context, message);
        }

        public void Debug(string message)
        {
#if GFU_DEBUG_BUILD
            logger.Debug(context, message);
#endif
        }

        public void Trace(string message)
        {
            if (!TracingEnabled) return;
            logger.Trace(context, message);
        }

        public void Info(string format, params object[] objects)
        {
            Info(string.Format(format, objects));
        }

        public void Info(Exception ex, string message)
        {
            Info(string.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Info(Exception ex)
        {
            Info(ex, string.Empty);
        }

        public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, string.Format(format, objects));
        }

        public void Debug(string format, params object[] objects)
        {
#if GFU_DEBUG_BUILD
            Debug(string.Format(format, objects));
#endif
        }

        public void Debug(Exception ex, string message)
        {
#if GFU_DEBUG_BUILD
            Debug(string.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
#endif
        }

        public void Debug(Exception ex)
        {
#if GFU_DEBUG_BUILD
            Debug(ex, string.Empty);
#endif
        }

        public void Debug(Exception ex, string format, params object[] objects)
        {
#if GFU_DEBUG_BUILD
            Debug(ex, string.Format(format, objects));
#endif
        }

        public void Trace(string format, params object[] objects)
        {
            if (!TracingEnabled) return;

            Trace(string.Format(format, objects));
        }

        public void Trace(Exception ex, string message)
        {
            if (!TracingEnabled) return;

            Trace(string.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Trace(Exception ex)
        {
            if (!TracingEnabled) return;

            Trace(ex, string.Empty);
        }

        public void Trace(Exception ex, string format, params object[] objects)
        {
            if (!TracingEnabled) return;

            Trace(ex, string.Format(format, objects));
        }

        public void Warning(string message)
        {
            logger.Warning(context, message);
        }

        public void Warning(string format, params object[] objects)
        {
            Warning(string.Format(format, objects));
        }

        public void Warning(Exception ex, string message)
        {
            Warning(string.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Warning(Exception ex)
        {
            Warning(ex, string.Empty);
        }

        public void Warning(Exception ex, string format, params object[] objects)
        {
            Warning(ex, string.Format(format, objects));
        }

        public void Error(string message)
        {
            logger.Error(context, message);
        }

        public void Error(string format, params object[] objects)
        {
            Error(string.Format(format, objects));
        }

        public void Error(Exception ex, string message)
        {
            Error(string.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Error(Exception ex)
        {
            Error(ex, string.Empty);
        }

        public void Error(Exception ex, string format, params object[] objects)
        {
            Error(ex, string.Format(format, objects));
        }
    }
}
