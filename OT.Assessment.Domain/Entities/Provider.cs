using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OT.Assessment.Domain.Entities
{
    public class Provider
    {
        public string? Name { get; set; }
        public List<Game>? Games { get; set; }
    }
}
