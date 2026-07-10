using System;
namespace Skanly.Domain.Exceptions
{
    public  class NotFoundException : Exception
    {
        public NotFoundException(string message) 
            : base(message) 
        {
        }
    }
}
