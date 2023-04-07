using Exceptionless;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H.Necessaire.Exceptionless.Logging
{
    internal class ExceptionlessLogProcessor : LogProcessorBase, ImADependency
    {
        #region Construct
        static readonly LogConfig defaultConfig = new LogConfig { EnabledLevels = LogConfig.LevelsHigherOrEqualTo(LogEntryLevel.Trace, includeNone: false) };

        public ExceptionlessLogProcessor()
        {
            logConfig = defaultConfig;
        }

        ExceptionlessClient exceptionlessClient = null;
        public void ReferDependencies(ImADependencyProvider dependencyProvider)
        {
            string apiKey = dependencyProvider?.GetRuntimeConfig()?.Get("Exceptionless")?.Get("ApiKey")?.ToString();
            if (apiKey.IsEmpty())
                OperationResult.Fail("Exceptionless ApiKey is missing. Must be specified via Runtime Config: <CfgRoot>:Exceptionless:ApiKey").ThrowOnFail();

            string version = dependencyProvider?.Get<ImAVersionProvider>()?.GetCurrentVersion().GetAwaiter().GetResult()?.Number?.ToString();

            exceptionlessClient = new ExceptionlessClient(cfg =>
            {
                cfg.ApiKey = apiKey;
                if (!version.IsEmpty())
                    cfg.SetVersion(version);
            });
        }
        #endregion

        public override LoggerPriority GetPriority() => LoggerPriority.Delayed;

        public override async Task<OperationResult<LogEntry>> Process(LogEntry logEntry)
        {
            if (logEntry is null)
                return OperationResult.Win("Log Entry is null, nothing to log").WithPayload(logEntry);


            OperationResult<LogEntry> result = OperationResult.Fail("Not yet started").WithPayload(logEntry);

            await
                new Func<Task>(async () =>
                {
                    exceptionlessClient
                        .CreateEvent(await MapContextData(logEntry))
                        .SetType(await MapType(logEntry))
                        .SetProperty(Event.KnownDataKeys.Level, (await MapLevel(logEntry)).Trim())
                        .SetSource(await MapSource(logEntry))
                        .SetReferenceId(logEntry.ID.ToString())
                        .SetEventReference(nameof(logEntry.ScopeID), logEntry.ScopeID.ToString())
                        .SetMessage(logEntry.Message)
                        .And(x => x.Target.Date = logEntry.HappenedAt)
                        .And(x => { if (logEntry.Level >= LogEntryLevel.Critical) x.MarkAsCritical(); })
                        .And(x => { if (logEntry.Payload != null) x.AddObject(logEntry.Payload, nameof(logEntry.Payload)); })
                        .And(x => { if (logEntry.OperationContext != null) x.AddObject(logEntry.OperationContext, nameof(logEntry.OperationContext)); })
                        .And(x => { if (logEntry.Application.IsEmpty() == false) x.SetProperty(nameof(logEntry.Application), logEntry.Application); })
                        .And(x => { if (logEntry.Component.IsEmpty() == false) x.SetProperty(nameof(logEntry.Component), logEntry.Component); })
                        .And(x => { if (logEntry.Method.IsEmpty() == false) x.SetProperty(nameof(logEntry.Method), logEntry.Method); })
                        .And(x => { if (logEntry.StackTrace.IsEmpty() == false) x.SetProperty(nameof(logEntry.StackTrace), logEntry.StackTrace); })
                        .And(x => { if (logEntry.Notes?.Any() == true) x.SetProperty(nameof(logEntry.Notes), logEntry.Notes); })
                        .And(x =>
                        {
                            KeyValuePair<string, object>[] extraContextData = x.PluginContextData.Where(d => d.Key.StartsWith("@@_") == false).ToArray();
                            if (extraContextData.Any())
                            {
                                foreach (KeyValuePair<string, object> extraData in extraContextData)
                                {
                                    x.AddObject(extraData.Value, extraData.Key);
                                }
                            }
                        }
                        )
                        .And(x => x.Target.Date = logEntry.HappenedAt)
                        .Submit()
                        ;

                    result = OperationResult.Win().WithPayload(logEntry);
                })
                .TryOrFailWithGrace(
                    onFail: ex =>
                    {
                        result = OperationResult.Fail(ex, "Error occurred while trying to Process LogEntry for Exceptionless logging").WithPayload(logEntry);
                    }
                );

            return result;
        }

        private Task<string> MapSource(LogEntry logEntry)
        {
            StringBuilder printer = new StringBuilder();

            if (logEntry.Application.IsEmpty() == false)
                printer.Append(printer.Length == 0 ? string.Empty : "->").Append(logEntry.Application);

            if (logEntry.Component.IsEmpty() == false)
                printer.Append(printer.Length == 0 ? string.Empty : "->").Append(logEntry.Component);

            if (logEntry.Method.IsEmpty() == false)
                printer.Append(printer.Length == 0 ? string.Empty : "->").Append(logEntry.Method);

            return printer.ToString().NullIfEmpty().AsTask();
        }

        private Task<string> MapLevel(LogEntry logEntry)
        {
            string result;
            switch (logEntry.Level)
            {
                case LogEntryLevel.Trace:
                    result = LogLevel.Trace.ToString();
                    break;
                case LogEntryLevel.Debug:
                    result = LogLevel.Debug.ToString();
                    break;
                case LogEntryLevel.Info:
                    result = LogLevel.Info.ToString();
                    break;
                case LogEntryLevel.Warn:
                    result = LogLevel.Warn.ToString();
                    break;
                case LogEntryLevel.Error:
                    result = LogLevel.Error.ToString();
                    break;
                case LogEntryLevel.Critical:
                    result = LogLevel.Fatal.ToString();
                    break;
                default:
                    result = LogLevel.Other.ToString();
                    break;
            }
            return result.AsTask();
        }

        private Task<string> MapType(LogEntry logEntry)
        {
            string result;
            switch (logEntry.Level)
            {
                case LogEntryLevel.Warn:
                    result = Event.KnownTypes.Log;
                    break;
                case LogEntryLevel.Error:
                case LogEntryLevel.Critical:
                    result = Event.KnownTypes.Error;
                    break;
                case LogEntryLevel.Trace:
                case LogEntryLevel.Debug:
                case LogEntryLevel.Info:
                default:
                    result = logEntry.Level.ToString();
                    break;
            }
            return result.AsTask();
        }

        private Task<ContextData> MapContextData(LogEntry logEntry)
        {
            ContextData result = new ContextData();

            if (logEntry.OperationContext != null)
                result.Add(nameof(logEntry.OperationContext), logEntry.OperationContext);

            if (logEntry.Payload != null)
                result.Add(nameof(logEntry.Payload), logEntry.Payload);

            if (logEntry.Notes?.Any() == true)
                result.Add(nameof(logEntry.Notes), logEntry.Notes);

            if (logEntry.Application.IsEmpty() == false)
                result.Add(nameof(logEntry.Application), logEntry.Application);

            if (logEntry.StackTrace.IsEmpty() == false)
                result.Add(nameof(logEntry.StackTrace), logEntry.StackTrace);

            if (logEntry.Exception != null)
            {
                Exception[] exceptions = logEntry.Exception.Flatten();
                if (exceptions.First() is OperationResultException)
                {
                    exceptions[0] = new InvalidOperationException(exceptions.First().Message);
                }
                result.SetException(exceptions.First());
                result.SetSubmissionMethod(logEntry.Exception.GetType().Name);
                result.MarkAsUnhandledError();
                int index = 0;
                foreach (Exception ex in exceptions.Skip(1))
                {
                    index++;
                    result.Add($"Exception Addendum {index}", ex);
                }
            }

            return result.AsTask();
        }
    }
}
