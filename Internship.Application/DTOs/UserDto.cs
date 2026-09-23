using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internship.Application.DTOs
{
    public class UserDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required IList<string> Roles { get; set; }
    }
}
