using System;
using System.Reflection;

namespace RedGate.Ipc.Logging
{
    public interface ILogger
    {
        void Debug(string message, Exception ex = null);
        void Info(string message, Exception ex = null);
        void Warn(string message, Exception ex = null);
        void Error(string message, Exception ex = null);
    }

    internal sealed class LogTranslator
    {
        internal static ILogger FromObject(object o)
        {
            if (o == null) return new NullLogger();

            var methods = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

            Action<string> debug = null;
            Action<string> info = null;
            Action<string> warn = null;
            Action<string> error = null;

            Action<string, Exception> debuge = null;
            Action<string, Exception> infoe = null;
            Action<string, Exception> warne = null;
            Action<string, Exception> errore = null;

            foreach (var method in methods)
            {
                if (method.Name.ToLowerInvariant() == "debug")
                {
                    if (HasStringExceptionInvocation(method)) debuge = (s, e) => method.Invoke(o, new object[] { s, e });
                    else if (HasStringInvocation(method)) debug = (s) => method.Invoke(o, new object[] { s });
                }
                else if (method.Name.ToLowerInvariant() == "info")
                {
                    if (HasStringExceptionInvocation(method)) infoe = (s, e) => method.Invoke(o, new object[] { s, e });
                    else if (HasStringInvocation(method)) info = (s) => method.Invoke(o, new object[] { s });
                }
                else if (method.Name.ToLowerInvariant() == "warn")
                {
                    if (HasStringExceptionInvocation(method)) warne = (s, e) => method.Invoke(o, new object[] { s, e });
                    else if (HasStringInvocation(method)) warn = (s) => method.Invoke(o, new object[] { s });
                }
                else if (method.Name.ToLowerInvariant() == "error")
                {
                    if (HasStringExceptionInvocation(method)) errore = (s, e) => method.Invoke(o, new object[] { s, e });
                    else if (HasStringInvocation(method)) error = (s) => method.Invoke(o, new object[] { s });
                }
            }

            return new FlexiLogger(debug, info, warn, error, debuge, infoe, warne, errore);
        }

        private static bool HasStringInvocation(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
        }

        private static bool HasStringExceptionInvocation(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Length == 2
                   && parameters[0].ParameterType == typeof(string)
                   && parameters[1].ParameterType.IsSubclassOf(typeof(Exception));
        }

        private class FlexiLogger : ILogger
        {
            private readonly Action<string> _debug;
            private readonly Action<string> _info;
            private readonly Action<string> _warn;
            private readonly Action<string> _error;

            private readonly Action<string, Exception> _debug_exception;
            private readonly Action<string, Exception> _info_exception;
            private readonly Action<string, Exception> _warn_exception;
            private readonly Action<string, Exception> _error_exception;

            public FlexiLogger(
                Action<string> debug,
                Action<string> info,
                Action<string> warn,
                Action<string> error,
                Action<string, Exception> debugException,
                Action<string, Exception> infoException,
                Action<string, Exception> warnException,
                Action<string, Exception> errorException)
            {
                _debug = debug;
                _info = info;
                _warn = warn;
                _error = error;
                _debug_exception = debugException;
                _info_exception = infoException;
                _warn_exception = warnException;
                _error_exception = errorException;
            }

            public void Debug(string message, Exception ex = null)
            {
                Handle(message, ex, _debug, _debug_exception, fallback: null);
            }

            public void Info(string message, Exception ex = null)
            {
                Handle(message, ex, _info, _info_exception, fallback: Debug);
            }

            public void Warn(string message, Exception ex = null)
            {
                Handle(message, ex, _warn, _warn_exception, fallback: Info);
            }

            public void Error(string message, Exception ex = null)
            {
                Handle(message, ex, _error, _error_exception, fallback: Warn);
            }

            private void Handle(
                string message,
                Exception ex,
                Action<string> reporter,
                Action<string, Exception> reporterException,
                Action<string, Exception> fallback)
            {
                if (reporter == null && reporterException == null)
                {
                    fallback?.Invoke(message, ex);
                    return;
                }

                if (ex == null)
                {
                    reporter?.Invoke(message);
                }
                else
                {
                    if (reporterException == null)
                        reporter.Invoke($"{message} - {ex}");
                    else
                        reporterException(message, ex);
                }
            }
        }

        private class NullLogger : ILogger
        {
            public void Warn(string message, Exception ex = null)
            {
            }

            public void Error(string message, Exception ex = null)
            {
            }

            public void Debug(string message, Exception ex = null)
            {
            }

            public void Info(string message, Exception ex = null)
            {
            }
        }
    }
}
