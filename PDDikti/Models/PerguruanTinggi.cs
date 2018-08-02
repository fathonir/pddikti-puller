using System;
using System.Collections.Generic;

namespace PDDikti.Models
{
    [Serializable]
    public class PerguruanTinggi
    {
        public Guid ID { get; set; }
        public string Kode { get; set; }
        public string Nama { get; set; }
        public string Nama_Singkat { get; set; }
        public string SK_Pendirian { get; set; }
        public DateTime Tgl_SK_Pendirian { get; set; }
        public string SK_Operasional { get; set; }
        
        /// <summary>A: Aktif, B: Alih Bentuk, H: Tutup, L: Luar Negeri</summary>
        public string Status { get; set; }
        public Alamat Alamat { get; set; }
        public Provinsi Propinsi { get; set; }
        public string Telepon { get; set; }
        public string Faksimile { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public StatusMilik Status_Milik { get; set; }
        public Institusi Pembina { get; set; }
        public BentukInstitusi BentukPendidikan { get; set; }
        public DateTime Last_Update { get; set; }

        public IEnumerable<ProgramStudi> ProgramStudis { get; set; }
        public IEnumerable<Dosen> Dosens { get; set; }
    }
}
