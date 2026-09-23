using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internship.Application.DTOs.Dispute
{
    public record AssignDisputeDto(
        [Required] string AssignedToUserId
    );
}