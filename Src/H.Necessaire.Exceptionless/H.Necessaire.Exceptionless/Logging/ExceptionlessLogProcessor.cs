using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace H.Necessaire.Exceptionless.Logging
{
    internal class ExceptionlessLogProcessor : LogProcessorBase
    {
        static readonly LogConfig defaultConfig = new LogConfig { EnabledLevels = LogConfig.LevelsHigherOrEqualTo(LogEntryLevel.Trace, includeNone: false) };

        public ExceptionlessLogProcessor()
        {
            logConfig = defaultConfig;
        }

        public override LoggerPriority GetPriority() => LoggerPriority.Delayed;

        public override Task<OperationResult<LogEntry>> Process(LogEntry logEntry)
        {
            throw new NotImplementedException();
        }
    }
}
