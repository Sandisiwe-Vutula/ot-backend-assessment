using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OT.Assessment.Repository.Constants
{
    internal static class PlayerStoredProcedures
    {
        public const string InsertCasinoWager = "sp_InsertCasinoWager";
        public const string GetCasinoWagersByPlayer = "sp_GetCasinoWagersByPlayer";
        public const string GetTopSpenders = "sp_GetTopSpenders";
    }
}
