using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDDikti.Models
{
    [Serializable]
    public class Mahasiswa
    {
        public Guid ID { get; set; }
        public string Nama { get; set; }
        public string Jenis_Kelamin { get; set; }
        public string NIK { get; set; }
        public DateTime Tgl_Lahir { get; set; }
        public string Tempat_Lahir { get; set; }
        public Alamat Alamat { get; set; }
        public string Telepon { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public Agama Agama { get; set; }
        public string Ibu_Kandung { get; set; }
        public string Kewarganegaraan { get; set; }
        public bool WNA { get; set; }
        public bool Penerima_KPS { get; set; }
        public JenisTinggal Jenis_Tinggal { get; set; }
        public MahasiswaPT Terdaftar { get; set; }
        public DateTime Last_Update { get; set; }
    }
}
