using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDDikti.Models
{
    [Serializable]
    public class Alamat
    {
        public string Jalan { get; set; }
        public string Rt { get; set; }
        public string Rw { get; set; }
        public string Dusun { get; set; }
        public string Kelurahan { get; set; }
        public string Kode_Pos { get; set; }
        public Kota Kab_Kota { get; set; }

        public override string ToString()
        {
            return Jalan + ", " + Kab_Kota.Nama;
        }
    }
}
