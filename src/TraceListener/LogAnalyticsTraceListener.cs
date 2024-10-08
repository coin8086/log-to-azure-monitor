﻿using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Ingestion;
using System;
using System.Diagnostics;

namespace RzWork.AzureMonitor
{
    public class LogAnalyticsTraceListener : TraceListener
    {
        private static string[] CustomAttributes = new string[] { Config.MiClientIdKey, Config.DcrIdKey, Config.DcrStreamKey, Config.DceUrlKey };

        private EventStore _store;

        private bool _initialzed = false;

        private object _initLock = new object();

        private bool _shouldInit = true;

        public LogAnalyticsTraceListener()
        {
            //NOTE: No Init() here since Init() requires Attributes, which is empty in CTOR.
        }

        //For test purpose
        public LogAnalyticsTraceListener(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            //NOTE: Init(config) here since Init(config) doesn't requires Attributes, which is empty in CTOR.
            Init(config);
        }

        private bool Init(Config config = null)
        {
            if (!_initialzed)
            {
                if (!_shouldInit)
                {
                    return false;
                }
                lock (_initLock)
                {
                    if (!_initialzed)
                    {
                        if (config == null)
                        {
                            config = Config.Get(Attributes);
                        }
                        DebugLog.WriteInfo<LogAnalyticsTraceListener>($"Got config: {config}");

                        if (!config.Complete)
                        {
                            DebugLog.WriteWarning<LogAnalyticsTraceListener>("Incomplete config. Abort Init.");
                            _shouldInit = false;
                            return false;
                        }

                        try
                        {
                            var credential = new ManagedIdentityCredential(config.MiClientId);
                            var clientOpts = new LogsIngestionClientOptions();
                            clientOpts.Retry.MaxRetries = 5;
                            clientOpts.Retry.Mode = RetryMode.Exponential;
                            clientOpts.Retry.Delay = TimeSpan.FromSeconds(2);
                            var client = new LogsIngestionClient(new Uri(config.DceUrl), credential, clientOpts);
                            var sender = new LogAnalyticsSender(client, config.DcrId, config.DcrStream);
                            _store = new EventStore(sender);
                            _initialzed = true;

                            try
                            {
                                var source = typeof(LogAnalyticsTraceListener).FullName;

                                AppDomain.CurrentDomain.ProcessExit += (_, args) =>
                                {
                                    var msg = "Process is exiting.";
                                    DebugLog.WriteInfo<LogAnalyticsTraceListener>(msg);
                                    TraceEvent(new TraceEventCache(), source, TraceEventType.Information, 0, msg);
                                    Close();
                                };

                                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                                {
                                    var msg = $"Caught unhandled exception: {args.ExceptionObject}";
                                    DebugLog.WriteError<LogAnalyticsTraceListener>(msg);
                                    TraceEvent(new TraceEventCache(), source, TraceEventType.Warning, 0, msg);
                                    Close();
                                };
                            }
                            catch (Exception ex)
                            {
                                DebugLog.WriteError<LogAnalyticsTraceListener>($"Error when registering AppDomain event: {ex}");
                            }

                            DebugLog.WriteInfo<LogAnalyticsTraceListener>($"Init finished.");
                        }
                        catch (Exception ex)
                        {
                            DebugLog.WriteError<LogAnalyticsTraceListener>($"Error when Init: {ex}");
                            _shouldInit = false;
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        protected override string[] GetSupportedAttributes()
        {
            return CustomAttributes;
        }

        public override bool IsThreadSafe => true;

        //NOTE: Client should void the Write and WriteLine methods!
        public override void Write(string message)
        {
            TraceEvent(new TraceEventCache(), string.Empty, TraceEventType.Information, 0, message);
        }

        public override void WriteLine(string message)
        {
            TraceEvent(new TraceEventCache(), string.Empty, TraceEventType.Information, 0, message);
        }

        public override void Fail(string message, string detailMessage)
        {
            TraceEvent(new TraceEventCache(), string.Empty, TraceEventType.Error, 0, $"{message} {detailMessage}");
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            TraceEvent(eventCache, source, eventType, id, null, null);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, message, null);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (!Init())
            {
                return;
            }
            string message = null;
            if (format == null)
            {
                message = string.Empty;
            }
            else
            {
                if (args == null)
                {
                    message = format;
                }
                else
                {
                    message = string.Format(format, args);
                }
            }
            var ts = eventCache?.DateTime ?? DateTime.UtcNow;
            _store.Put(new Event(ts, eventType, id, source, message));
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            TraceEvent(eventCache, source, eventType, id, data?.ToString(), null);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            string message = null;
            if (data != null)
            {
                message = string.Join(", ", data);
            }
            TraceEvent(eventCache, source, eventType, id, message, null);
        }

        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            var msg = $"{relatedActivityId}: {message}";
            TraceEvent(eventCache, source, TraceEventType.Transfer, id, msg, null);
        }

        public override void Flush()
        {
            if (!Init())
            {
                return;
            }
            _store.Flush(false);
        }

        public override void Close()
        {
            if (!_initialzed)
            {
                return;
            }
            _store.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }
    }

}
