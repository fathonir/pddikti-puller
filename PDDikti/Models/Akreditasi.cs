using System;
using System.Collections.Generic;
using System.Text;

namespace PDDikti.Models
{
    public class Akreditasi
    {
        public Guid ID { get; set; }
        public ProgramStudi Prodi { get; set; }
        public string Nilai { get; set; }
        public string Lembaga_Akreditasi { get; set; }
        public string SK_Akreditasi { get; set; }
        public DateTime Tgl_SK_Akreditasi { get; set; }
        
        /// <summary>Terhitung-Sampai-Tanggal</summary>
        public DateTime TST_SK_Akreditasi { get; set; }
        public DateTime Last_Update { get; set; }
        public PerguruanTinggi PT { get; set; }
    }
}
