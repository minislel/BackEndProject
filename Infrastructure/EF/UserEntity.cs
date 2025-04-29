using Microsoft.AspNetCore.Identity;
using ApplicationCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EF
{
    public class UserEntity : IdentityUser
    {
        public UserDetails Details { get; set; }
    }
}
