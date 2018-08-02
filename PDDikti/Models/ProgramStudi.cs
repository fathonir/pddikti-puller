using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDDikti.Models
{
    [Serializable]
    public class ProgramStudi
    {
        public Guid ID { get; set; }
        public string Kode { get; set; }
        public string Nama { get; set; }
        public string Status { get; set; }
        public string Visi { get; set; }
        public string Misi { get; set; }
        public string Kompetensi { get; set; }
        public string Telepon { get; set; }
        public string Faksimile { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public Guid Perguruan_Tinggi_ID { get; set; }
        public PerguruanTinggi PT { get; set; }
        public Jenjang Jenjang_Didik { get; set; }
        public DateTime Tgl_Berdiri { get; set; }
        public int SKS_Lulus { get; set; }
        public string Kode_Pos { get; set; }
        public DateTime Last_Update { get; set; }
    }
}
