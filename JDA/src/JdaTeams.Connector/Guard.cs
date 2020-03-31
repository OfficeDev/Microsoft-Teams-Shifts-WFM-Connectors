using System;
using System.Diagnostics;

namespace JdaTeams.Connector
{
    [DebuggerStepThrough]
    public static class Guard
    {
        public static void ArgumentNotNull<T>(T value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void ArgumentNotEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(argumentName);
            }
        }

        public static void ArgumentNotNullOrEmpty(string value, string argumentName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(argumentName);
            }
        }

        public static int ArgumentIsInteger(string value, string argumentName)
        {
            if (!int.TryParse(value, out var integerValue))
            {
                throw new ArgumentException(argumentName);
            }

            return integerValue;
        }
    }
}
