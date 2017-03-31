using LibraryInstaller.Contracts;

namespace LibraryInstaller
{
    internal class Error : IError
    {
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; set; }

        public string Message { get; set; }
    }
}
