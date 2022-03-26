using System;

namespace TwitchLib.Api.Core.Exceptions
{
    /// <inheritdoc />
    /// <summary>Exception representing a provided scope was not permitted.</summary>
    public class UnauthorizedException : Exception
    {
        /// <inheritdoc />
        /// <summary>Exception constructor</summary>
        public UnauthorizedException(string data)
            : base(data)
        {
        }
    }
}