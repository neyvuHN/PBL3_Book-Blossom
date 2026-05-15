using System;

namespace BookBlossom.Core.Exceptions
{
    /// <summary>
    /// Exception dùng để ném ra khi người dùng thực hiện một hành động 
    /// không được phép hoặc chưa xác thực (Ví dụ: Sai mật khẩu, Token hết hạn).
    /// </summary>
    public class UnauthorizedActionException : Exception
    {
        public UnauthorizedActionException() 
            : base("Bạn không có quyền thực hiện hành động này.") 
        { 
        }

        public UnauthorizedActionException(string message) 
            : base(message) 
        { 
        }

        public UnauthorizedActionException(string message, Exception innerException) 
            : base(message, innerException) 
        { 
        }
    }
}