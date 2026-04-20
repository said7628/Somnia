using System;

namespace Somnia.Economy.DTOs
{
    [Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public string message;
        public T data;
        public ErrorResponse error;

        public static ApiResponse<T> Ok(T payload, string msg = "OK")
        {
            return new ApiResponse<T> { success = true, message = msg, data = payload };
        }

        public static ApiResponse<T> Fail(string code, string msg)
        {
            return new ApiResponse<T>
            {
                success = false,
                message = msg,
                error = new ErrorResponse { code = code, message = msg }
            };
        }
    }

    [Serializable]
    public class ErrorResponse
    {
        public string code;
        public string message;
        public string details;
    }
}
