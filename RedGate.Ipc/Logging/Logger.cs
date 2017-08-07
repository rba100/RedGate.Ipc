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

            return new MappedLogger(debug, info, warn, error, debuge, infoe, warne, errore);
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

        private class MappedLogger : ILogger
        {
            private readonly Action<string> _debug;
            private readonly Action<string> _info;
            private readonly Action<string> _warn;
            private readonly Action<string> _error;

            private readonly Action<string, Exception> _debug_exception;
            private readonly Action<string, Exception> _info_exception;
            private readonly Action<string, Exception> _warn_exception;
            private readonly Action<string, Exception> _error_exception;

            public MappedLogger(
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
                if (ex == null)
                    _debug?.Invoke(message);
                else
                {
                    if (_debug_exception != null) _debug_exception(message, ex);
                    else _debug?.Invoke($"{message} - {ex}");
                }
            }

            public void Info(string message, Exception ex = null)
            {
                if (ex == null)
                {
                    if (_info != null) _info(message);
                    else Debug(message);
                }
                else
                {
                    if (_info_exception != null) _info_exception(message, ex);
                    else if (_info != null) _info($"{message} - {ex}");
                    else Debug(message, ex);
                }
            }

            public void Warn(string message, Exception ex = null)
            {
                if (ex == null)
                {
                    if (_warn != null) _warn(message);
                    else Info(message);
                }
                else
                {
                    if (_warn_exception != null) _warn_exception(message, ex);
                    else if (_warn != null) _warn($"{message} - {ex}");
                    else Info(message, ex);
                }
            }

            public void Error(string message, Exception ex = null)
            {
                if (ex == null)
                {
                    if (_error != null) _error(message);
                    else Warn(message);
                }
                else
                {
                    if (_error_exception != null) _error_exception(message, ex);
                    else if (_error != null) _error($"{message} - {ex}");
                    else Warn(message, ex);
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
