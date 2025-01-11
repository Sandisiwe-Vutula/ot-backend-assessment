using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OT.Assessment.Domain.Entities
{
    public class Player
    {
        public Guid AccountId { get; set; }
        public string? Username { get; set; }
    }
}
