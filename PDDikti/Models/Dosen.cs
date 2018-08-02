using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDDikti.Models
{
    [Serializable]
    public class Dosen
    {
        public Guid ID { get; set; }
        public string NIDN { get; set; }
        public string NIP { get; set; }
        public string NPWP { get; set; }
        public string NIK { get; set; }
        public string Nama { get; set; }
        public string Gelar_Depan { get; set; }
        public string Gelar_Belakang { get; set; }
        public string Pendidikan_Terakhir { get; set; }
        public string Jenis_Kelamin { get; set; }
        public DateTime Tgl_Lahir { get; set; }
        public string Tempat_Lahir { get; set; }
        public string Telepon { get; set; }
        public string Handphone { get; set; }
        public Alamat Alamat { get; set; }
        public string Email { get; set; }
        public string Kode_PT { get; set; }
        public string Kode_Prodi { get; set; }
        public string SK_CPNS { get; set; }
        public DateTime Tgl_SK_CPNS { get; set; }
        public string SK_Pengangkatan { get; set; }
        public DateTime Tgl_SK_Pengangkatan { get; set; }
        public DateTime TMT_SK_Pengangkatan { get; set; }
        public DateTime TMT_PNS { get; set; }
        public string Kewarganegaraan { get; set; }
        public Agama Agama { get; set; }
        public IkatanKerja Ikatan_Kerja { get; set; }
        public StatusKepegawaian Status_Kepegawaian { get; set; }
        public StatusKeaktifan Status_Keaktifan { get; set; }
        public PangkatGolongan Pangkat_Golongan { get; set; }
        public JabatanFungsional Jabatan_Fungsional { get; set; }
        public DateTime Last_Update { get; set; }
    }
}
