using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDDikti.Models
{
    [Serializable]
    public class MahasiswaPT
    {
        public Guid ID { get; set; }
        public string Kode_PT { get; set; }
        public string Nama_PT { get; set; }
        public string Kode_Prodi { get; set; }
        public string Nama_Prodi { get; set; }
        public string Status_PT { get; set; }
        public Jenjang Jenjang_Didik { get; set; }
        public string NIM { get; set; }
        public DateTime Tgl_Masuk { get; set; }
        public DateTime Tgl_Keluar { get; set; }
        public string Smt_Mulai { get; set; }
        public string Smt_Tempuh { get; set; }
        public float SKS { get; set; }
        public float IPK { get; set; }
        public string No_Ijazah { get; set; }
        public DateTime Tgl_SK_Yudisium { get; set; }
        public string Status { get; set; }
        public JenisDaftar Jenis_Daftar { get; set; }
        public JenisKeluar Jenis_Keluar { get; set; }
    }
}
