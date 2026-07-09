using System;
using System.Collections.Generic;
using System.Text;

namespace Skanly.Application.Interfaces.Services
{
    public  interface IJwtTokenService
    {
        string GenerateToken(string userId, string email);
        
    }
     
}

