using System;

namespace WGestures.App.Migrate
{
    internal class MigrateException : Exception
    {
        public MigrateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MigrateException(string message)
            : base(message)
        {
        }
    }
}
