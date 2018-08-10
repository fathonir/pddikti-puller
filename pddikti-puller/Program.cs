using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using PDDikti;
using PDDikti.Models;
using RestSharp;

namespace pddikti_puller
{
    class Program
    {
        private static string ConnectionString { get; set; }

        private static string HelpText
        {
            get
            {
                return @"
Penggunaan: dotnet pddikti-puller.dll [options]

options:
  --config=<file>                   File config (wajib ada)

  --pull=<refdata>[,<refdata>]      Data referensi yang diambil :
                                    pt : Data referensi Perguruan Tinggi
                                    prodi : Data referensi Program Studi
                                    mahasiswa : Data referensi Mahasiswa
                                    dosen : Data referensi Dosen
                                    akr-pt : Data akreditasi Perguruan Tinggi
                                    akr-prodi : Data akreditasi Program Studi

  --filter-pt=<kode-pt>             Filter perguruan tinggi berdasarkan kode

  --filter-prodi=<kode-prodi>       Filter prodi berdasarkan kode (memerlukan filter pt)

  --per-page=<row-per-page>         Jumlah pengambilan (default: 500)

  --pt-per-process=<pt-per-process> Jumlah PT per sekali proses (default: 100)

  --max-retry=<max-retry>           Jumlah maksimal pengulangan jika terjadi gagal ambil (default: 3)

Contoh penggunaan :
  1. Mengambil semua mahasiswa
  dotnet pddikti-puller.dll --config=config.json --pull=mahasiswa
  2. Mengambil semua mahasiswa pada suatu perguruan tinggi (Misal: UGM)
  dotnet pddikti-puller.dll --config=config.json --pull=mahasiswa --filter-pt=001001
  3. Mengambil semua mahasiswa pada suatu program studi (Misal: S2 Ilmu Komputer, Universitas Indonesia)
  dotnet pddikti-puller.dll --config=config.json --pull=mahasiswa --filter-pt=001002 --filter-prodi=57101";
            }
        }

        private static int perPage = 500;
        private static int maxRetries = 3;

        static int Main(string[] args)
        {
            Console.WriteLine("PDDIKTI Data Reference Puller");

            if (args.Length == 0)
            {
                Console.WriteLine(Program.HelpText);
                return 0;
            }

            var configFile = string.Empty;

            bool
                isPullPerguruanTinggi = false,
                isPullProgramStudi = false,
                isPullMahasiswa = false,
                isPullDosen = false,
                isPullAkreditasiPT = false,
                isPullAkreditasiProdi = false;

            string
                filterPT = string.Empty,
                filterProdi = string.Empty;

            foreach (var arg in args)
            {
                if (arg.StartsWith("--config="))
                {
                    configFile = arg.Remove(0, "--config=".Length);
                }

                if (arg.StartsWith("--pull="))
                {
                    var datarefs = arg.Remove(0, "--pull=".Length).Split(",");

                    foreach (var dataref in datarefs)
                    {
                        if (dataref == "pt")
                            isPullPerguruanTinggi = true;

                        if (dataref == "prodi")
                            isPullProgramStudi = true;

                        if (dataref == "mahasiswa")
                            isPullMahasiswa = true;

                        if (dataref == "dosen")
                            isPullDosen = true;

                        if (dataref == "akr-pt")
                            isPullAkreditasiPT = true;

                        if (dataref == "akr-prodi")
                            isPullAkreditasiProdi = true;
                    }
                }

                if (arg.StartsWith("--filter-pt="))
                {
                    filterPT = arg.Remove(0, "--filter-pt=".Length);
                }

                if (arg.StartsWith("--filter-prodi="))
                {
                    // Memastikan filter pt ada, karena URL API memerlukan kode pt untuk filter prodi
                    if (filterPT != string.Empty)
                    {
                        filterProdi = arg.Remove(0, "--filter-prodi=".Length);
                    }
                    else
                    {
                        Console.WriteLine("Melakukan filter prodi memerlukan filter-pt");
                        // Console.ReadKey();
                        return 0;
                    }
                }

                if (arg.StartsWith("--per-page="))
                {
                    perPage = int.Parse(arg.Remove(0, "--per-page=".Length));
                }

                if (arg.StartsWith("--max-retry="))
                {
                    maxRetries = int.Parse(arg.Remove(0, "--max-retry=".Length));
                }
            }

            if (configFile == string.Empty)
            {
                Console.WriteLine("File config belum ditentukan");
                // Console.ReadKey();
                return 0;
            }

            Console.WriteLine("Baca file config : config.json");

            var config = (new ConfigurationBuilder().AddJsonFile("config.json")).Build();
            var endpoint = new Uri(config["pddikti-endpoint"]);
            var accessToken = new Guid(config["pddikti-access-token"]);

            Program.ConnectionString = config["connection-string"];

            Console.WriteLine("PDDikti Endpoint : {0}", endpoint.ToString());
            Console.WriteLine("PDDikti Access Token : {0}", accessToken);
            Console.WriteLine("Connection String : {0}", config["connection-string"]);

            // create pddikticlient
            var pddiktiClient = new PDDiktiClient(endpoint, accessToken);

            // create mysql connection
            var connection = new MySqlConnection(Program.ConnectionString);

            if (isPullPerguruanTinggi)
                PullPerguruanTinggiAsync(connection, pddiktiClient, filterPT).GetAwaiter().GetResult();

            if (isPullProgramStudi)
                PullProgramStudiAsync(connection, pddiktiClient, filterPT).GetAwaiter().GetResult();

            if (isPullMahasiswa)
                PullMahasiswaAsync(connection, pddiktiClient, filterPT, filterProdi).GetAwaiter().GetResult();

            if (isPullDosen)
                PullDosenAsync(connection, pddiktiClient, filterPT, filterProdi).GetAwaiter().GetResult();

            if (isPullAkreditasiPT)
                PullAkreditasiPTAsync(connection, pddiktiClient, filterPT).GetAwaiter().GetResult();

            if (isPullAkreditasiProdi)
                PullAkreditasiProdiAsync(connection, pddiktiClient, filterPT, filterProdi).GetAwaiter().GetResult();

            Console.WriteLine("{0} Selesai.", DateTime.Now);
            // Console.ReadKey();
            return 0;
        }

        static async Task PullPerguruanTinggiAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "")
        {
            // inisialisasi variabel kebutuhan
            var page = 0;
            var count = 0;
            var totalPage = 0;
            var retry = 0;

            // Daftar PT
            var ptList = new List<PerguruanTinggi>();

            IRestResponse<List<PerguruanTinggi>> firstResponse;

            if (filterPT == "")
            {
                // Ambil untuk halaman pertama
                Console.Write("{0} Ambil data PT dari Forlap ... ", DateTime.Now);
                firstResponse = await client.GetListPerguruanTinggiAsync(page, Program.perPage);
                ptList.AddRange(firstResponse.Data);
            }
            else
            {
                // Ambil untuk halaman pertama
                Console.Write("{0} Ambil data PT {1} dari Forlap ... ", DateTime.Now, filterPT);
                firstResponse = await client.GetListPerguruanTinggiAsync(page, Program.perPage, filterPT);
                ptList.AddRange(firstResponse.Data);
            }

            // Ambil jumlah data dan total halaman
            count = firstResponse.TotalCount();
            totalPage = firstResponse.TotalPage();

            // Mengulang pengambilan next page apabila tidak sama dengan `count`
            // pada pengambilan pertama
            do
            {
                // Persiapan multi task pengambilan
                var getPTTaskList = new List<Task<IRestResponse<List<PerguruanTinggi>>>>();

                // Ambil semua PT dari page selanjutnya
                for (page = 1; page < totalPage; page++)
                    getPTTaskList.Add(client.GetListPerguruanTinggiAsync(page, Program.perPage));

                // Mulai & Tunggu (Wait) semua beres
                Task.WhenAll(getPTTaskList.ToArray()).GetAwaiter().GetResult();

                // Tambahkan data ke PT List
                foreach (var task in getPTTaskList)
                    ptList.AddRange(task.Result.Data);

                // Jika jumlah total yang didapat berbeda dengan count
                if (ptList.Count != count)
                {
                    // Kosongi dan ambil isi data pada respon awal lagi
                    ptList.Clear();
                    ptList.AddRange(firstResponse.Data);

                    // Ulangi
                    retry++;
                }
                else
                    break;  // keluar dari loop jika sudah sama

            } while (retry < Program.maxRetries);

            // Print jumlah data
            Console.WriteLine("{0} dari {1} total data", ptList.Count, count);

            // data referensi untuk agregate
            var kotaList = new List<Kota>();
            var provinsiList = new List<Provinsi>();
            var statusMilikList = new List<StatusMilik>();
            var pembinaList = new List<Institusi>();
            var bentukPendidikanList = new List<BentukInstitusi>();

            // Loop perguruan tinggi
            foreach (var pt in ptList)
            {
                // Ambil agregat kota
                if (!kotaList.Exists(kota => kota.ID == pt.Alamat.Kab_Kota.ID))
                    kotaList.Add(pt.Alamat.Kab_Kota);

                // Ambil agregate propinsi
                if (!provinsiList.Exists(provinsi => provinsi.ID == pt.Propinsi.ID))
                    provinsiList.Add(pt.Propinsi);

                // Ambil agregate status milik
                if (!statusMilikList.Exists(statusMilik => statusMilik.ID == pt.Status_Milik.ID))
                    statusMilikList.Add(pt.Status_Milik);

                // Ambil agregate pembina
                if (!pembinaList.Exists(pembina => pembina.ID == pt.Pembina.ID))
                    pembinaList.Add(pt.Pembina);

                // Ambil agregate bentuk pendidikan
                if (!bentukPendidikanList.Exists(bp => bp.ID == pt.BentukPendidikan.ID))
                    bentukPendidikanList.Add(pt.BentukPendidikan);
            }

            Console.WriteLine("{0} Referensi Kota ", DateTime.Now);
            await connection.ExecuteAsync("INSERT INTO kota (id, nama) values (@id, @nama) ON DUPLICATE KEY UPDATE nama=@nama",
                kotaList.Where(kota => kota.ID != null));

            Console.WriteLine("{0} Referensi Provinsi ", DateTime.Now);
            await connection.ExecuteAsync("INSERT INTO provinsi (id, nama) values (@id, @nama) ON DUPLICATE KEY UPDATE nama=@nama",
                provinsiList.Where(provinsi => provinsi.ID != null));

            Console.WriteLine("{0} Referensi Status Milik ", DateTime.Now);
            await connection.ExecuteAsync("INSERT INTO status_milik (id, nama) values (@id, @nama) ON DUPLICATE KEY UPDATE nama=@nama", statusMilikList);

            Console.WriteLine("{0} Referensi Institusi ", DateTime.Now);
            await connection.ExecuteAsync("INSERT INTO institusi (id, nama) values (@id, @nama) ON DUPLICATE KEY UPDATE nama=@nama",
                pembinaList.Where(pembina => pembina.ID != null));

            Console.WriteLine("{0} Referensi Bentuk pendidikan ", DateTime.Now);
            await connection.ExecuteAsync("INSERT INTO bentuk_pendidikan (id, nama) values (@id, @nama) ON DUPLICATE KEY UPDATE nama=@nama", bentukPendidikanList);

            foreach (var pt in ptList)
            {
                var ptParam = new
                {
                    id = pt.ID,
                    kode = pt.Kode,
                    nama = pt.Nama,
                    nama_singkat = pt.Nama_Singkat,
                    sk_pendirian = pt.SK_Pendirian,
                    tgl_sk_pendirian = pt.Tgl_SK_Pendirian,
                    sk_operasional = pt.SK_Operasional,
                    status = pt.Status,
                    telepon = pt.Telepon,
                    faksimile = pt.Faksimile,
                    website = pt.Website,
                    email = pt.Email,
                    alamat_jalan = pt.Alamat.Jalan,
                    alamat_rt = pt.Alamat.Rt,
                    alamat_rw = pt.Alamat.Rw,
                    alamat_dusun = pt.Alamat.Dusun,
                    alamat_kelurahan = pt.Alamat.Kelurahan,
                    alamat_kode_pos = pt.Alamat.Kode_Pos,
                    kota_id = pt.Alamat.Kab_Kota.ID,
                    provinsi_id = pt.Propinsi.ID,
                    status_milik_id = pt.Status_Milik.ID,
                    bentuk_pendidikan_id = pt.BentukPendidikan.ID,
                    pembina_id = pt.Pembina.ID
                };

                // Check exist
                var exist = (await connection.ExecuteScalarAsync<int?>("select 1 from perguruan_tinggi where id = @id", new { id = pt.ID })) != null;

                // Insert if not exist
                if (!exist)
                {
                    Console.Write("{0} [{1}] Insert data PT ... ", DateTime.Now, pt.Nama);

                    var sql = @"
                        INSERT INTO perguruan_tinggi (
                            id, kode, nama, nama_singkat, sk_pendirian, tgl_sk_pendirian, sk_operasional,
                            status, telepon, faksimile, website, email,
                            alamat_jalan, alamat_rt, alamat_rw, alamat_dusun, alamat_kelurahan, alamat_kode_pos,
                            kota_id, provinsi_id, status_milik_id, bentuk_pendidikan_id, pembina_id)
                        VALUES (
                            @id, @kode, @nama, @nama_singkat, @sk_pendirian, @tgl_sk_pendirian, @sk_operasional,
                            @status, @telepon, @faksimile, @website, @email,
                            @alamat_jalan, @alamat_rt, @alamat_rw, @alamat_dusun, @alamat_kelurahan, @alamat_kode_pos,
                            @kota_id, @provinsi_id, @status_milik_id, @bentuk_pendidikan_id, @pembina_id)";

                    await connection.ExecuteAsync(sql, ptParam);
                }
                else
                {
                    Console.Write("{0} [{1}] Update data PT ... ", DateTime.Now, pt.Nama);

                    var sql = @"
                        UPDATE perguruan_tinggi SET
                            kode = @kode, nama = @nama, nama_singkat = @nama_singkat, sk_pendirian = @sk_pendirian,
                            tgl_sk_pendirian=@tgl_sk_pendirian, sk_operasional=@sk_operasional, status=@status, telepon=@telepon,
                            faksimile=@faksimile, website=@website, email=@email, alamat_jalan=@alamat_jalan, alamat_rt=@alamat_rt,
                            alamat_rw=@alamat_rw, alamat_dusun=@alamat_dusun, alamat_kelurahan=@alamat_kelurahan,
                            alamat_kode_pos=@alamat_kode_pos, kota_id=@kota_id, provinsi_id=@provinsi_id, status_milik_id=@status_milik_id,
                            bentuk_pendidikan_id=@bentuk_pendidikan_id, pembina_id=@pembina_id, updated_at = current_timestamp
                        WHERE id = @id";

                    await connection.ExecuteAsync(sql, ptParam);
                }

                Console.WriteLine("OK");
            }
        }

        static async Task PullProgramStudiAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "")
        {
            IEnumerable<PerguruanTinggi> ptList;

            if (filterPT == "")
            {
                // Get All PT from database
                Console.Write("{0} Ambil data semua PT dari DB ... ", DateTime.Now);
                ptList = await connection.QueryAsync<PerguruanTinggi>("select id, kode, nama from perguruan_tinggi");
                Console.WriteLine("OK");
            }
            else
            {
                // Get All PT from database
                Console.Write("{0} Ambil data PT [{1}] dari DB ... ", DateTime.Now, filterPT);
                ptList = await connection.QueryAsync<PerguruanTinggi>("select id, kode, nama from perguruan_tinggi where kode = @kode", new { kode = filterPT });
                Console.WriteLine("OK");
            }

            foreach (var pt in ptList)
            {
                // Inisialisasi variabel kebutuhan
                var page = 0;
                var count = 0;
                var totalPage = 0;
                var retry = 0;

                // Daftar prodi yang perlu di insert
                var prodiList = new List<ProgramStudi>();

                // Ambil untuk halaman pertama.
                Console.Write("{0} [{1}] Ambil data Program Studi dari Forlap ... ", DateTime.Now, pt.Nama);
                var firstResponse = await client.GetListProgramStudiAsync(pt.ID, page, Program.perPage);
                prodiList.AddRange(firstResponse.Data);

                // Ambil jumlah data dan total halaman
                count = firstResponse.TotalCount();
                totalPage = firstResponse.TotalPage();

                // Mengulang pengambilan next page apabila tidak sama dengan `count`
                // pada pengambilan pertama
                do
                {
                    // Persiapan multi task pengambilan
                    var getProdiTaskList = new List<Task<IRestResponse<List<ProgramStudi>>>>();

                    // Ambil semua PT dari page selanjutnya
                    for (page = 1; page < totalPage; page++)
                        getProdiTaskList.Add(client.GetListProgramStudiAsync(pt.ID, page, Program.perPage));

                    // Mulai & Tunggu (Wait) semua beres
                    Task.WhenAll(getProdiTaskList.ToArray()).GetAwaiter().GetResult();

                    // Tambahkan data ke PT List
                    foreach (var task in getProdiTaskList)
                        prodiList.AddRange(task.Result.Data);

                    // Jika jumlah total yang didapat berbeda dengan count
                    if (prodiList.Count != count)
                    {
                        // Kosongi dan ambil isi data pada respon awal lagi
                        prodiList.Clear();
                        prodiList.AddRange(firstResponse.Data);

                        // Ulangi
                        retry++;
                    }
                    else
                        break;  // keluar dari loop jika sudah sama

                } while (retry < Program.maxRetries);

                // Print jumlah data
                Console.WriteLine("{0} total data", prodiList.Count);

                // Loop prodi proses data prodi
                foreach (var prodi in prodiList)
                {
                    Console.Write("{0} [{1}] {2} {3} ... ", DateTime.Now, pt.Nama, prodi.Jenjang_Didik.Nama, prodi.Nama);

                    // Check Exist jenjang
                    var sqlCheckJenjang = "select 1 from jenjang_didik where id = " + prodi.Jenjang_Didik.ID;
                    var jenjangExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckJenjang) != null);

                    // Jika jenjang belum ada, jika sudah ada tidak perlu update
                    if (!jenjangExist)
                    {
                        // Insert jenjang
                        await connection.ExecuteAsync(
                            "INSERT INTO jenjang_didik (id, nama) values (@id, @nama)",
                            new { id = prodi.Jenjang_Didik.ID, nama = prodi.Jenjang_Didik.Nama });
                    }

                    // check prodi 
                    var sqlCheckProdi = "select 1 from program_studi where id = '" + prodi.ID + "' limit 1";
                    var prodiExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckProdi) != null);

                    // Jika prodi belum ada insert
                    if (!prodiExist)
                    {
                        // Insert prodi
                        var sqlInsertProdi = @"
                            INSERT INTO program_studi
                                (id, perguruan_tinggi_id, kode, nama, status, jenjang_didik_id)
                            VALUES
                                (@id, @perguruan_tinggi_id, @kode, @nama, @status, @jenjang_didik_id)";

                        var prodiParam = new
                        {
                            id = prodi.ID,
                            perguruan_tinggi_id = prodi.PT.ID,
                            kode = prodi.Kode,
                            nama = prodi.Nama,
                            status = prodi.Status,
                            jenjang_didik_id = Convert.ToInt32(prodi.Jenjang_Didik.ID)
                        };

                        await connection.ExecuteAsync(sqlInsertProdi, prodiParam);
                    }

                    Console.WriteLine("OK");
                }
            }
        }

        static async Task PullMahasiswaAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "", string filterProdi = "")
        {
            IEnumerable<ProgramStudi> prodiList;

            Console.Write("{0} Ambil data Program Studi dari DB ... ", DateTime.Now);

            string sqlSelectProdi =
                @"SELECT ps.id, ps.perguruan_tinggi_id, ps.kode, ps.nama, jd.id, jd.nama, pt.id, pt.kode, pt.nama FROM program_studi ps
                JOIN jenjang_didik jd ON jd.id = ps.jenjang_didik_id
                JOIN perguruan_tinggi pt ON pt.id = ps.perguruan_tinggi_id
                ";

            if (filterPT != "")
            {
                sqlSelectProdi += "WHERE pt.kode = '" + filterPT + "' ";

                if (filterProdi != "")
                {
                    sqlSelectProdi += "AND ps.kode = '" + filterProdi + "'";
                }
            }

            prodiList = await connection.QueryAsync<ProgramStudi, Jenjang, PerguruanTinggi, ProgramStudi>(sqlSelectProdi,
                (prodi, jenjang, pt) =>
                {
                    prodi.PT = pt;
                    prodi.Jenjang_Didik = jenjang;
                    return prodi;
                },
                splitOn: "id,id");

            Console.WriteLine("OK");

            foreach (var prodi in prodiList)
            {
                // Inisialisasi variabel kebutuhan
                var page = 0;
                var count = 0;
                var totalPage = 0;
                var retry = 0;

                var mahasiswaList = new List<Mahasiswa>();

                // Ambil untuk halaman pertama
                Console.Write("{0} [{1}] Ambil data mahasiswa {2} {3} dari Forlap ... ", DateTime.Now, prodi.PT.Nama, prodi.Jenjang_Didik.Nama, prodi.Nama);
                var firstResponse = await client.GetListMahasiswaAsync(prodi.PT.ID.ToString(), prodi.ID.ToString(), page, Program.perPage);
                var responseContent = firstResponse.Content;

                mahasiswaList.AddRange(firstResponse.Data);

                // Ambil jumlah data dan total halaman
                count = firstResponse.TotalCount();
                totalPage = firstResponse.TotalPage();

                // Mengulang pengambilan next page apabila tidak sama dengan `count`
                // pada pengambilan pertama
                do
                {
                    // Persiapan multi task pengambilan
                    var getMahasiswaTaskList = new List<Task<IRestResponse<List<Mahasiswa>>>>();

                    // Ambil semua PT dari page selanjutnya
                    for (page = 1; page < totalPage; page++)
                        getMahasiswaTaskList.Add(client.GetListMahasiswaAsync(prodi.PT.ID.ToString(), prodi.ID.ToString(), page, Program.perPage));

                    // Mulai & Tunggu (Wait) semua beres
                    Task.WhenAll(getMahasiswaTaskList.ToArray()).GetAwaiter().GetResult();

                    // Tambahkan data ke mahasiswa list
                    foreach (var task in getMahasiswaTaskList)
                        mahasiswaList.AddRange(task.Result.Data);

                    // Jika jumlah total yang didapat berbeda dengan count
                    if (mahasiswaList.Count != count)
                    {
                        // Kosongi dan ambil isi data pada respon awal lagi
                        mahasiswaList.Clear();
                        mahasiswaList.AddRange(firstResponse.Data);

                        // Ulangi
                        retry++;
                    }
                    else
                        break;  // keluar dari loop jika sudah sama

                } while (retry < Program.maxRetries);

                Console.WriteLine("{0} total data", mahasiswaList.Count);

                foreach (var mahasiswa in mahasiswaList)
                {
                    Console.Write("{0} [{1}] {2} {3} ... ", DateTime.Now, prodi.PT.Nama, mahasiswa.Terdaftar.NIM, mahasiswa.Nama);

                    // Check exist mahasiswa
                    var sqlCheckMahasiswa = "select 1 from mahasiswa where id = '" + mahasiswa.ID + "'";
                    var mahasiswaExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckMahasiswa) != null);

                    // Jika mahasiswa belum ada, insert
                    if (!mahasiswaExist)
                    {
                        var sqlInsertMahasiswa =
                            @"INSERT INTO mahasiswa (
                                id, perguruan_tinggi_id, nama, jenis_kelamin, nik, tgl_lahir, tempat_lahir,
                                alamat_jalan, alamat_rt, alamat_rw, alamat_dusun, alamat_kelurahan, alamat_kode_pos,
                                kota_id)
                            VALUES (
                                @id, @perguruan_tinggi_id, @nama, @jenis_kelamin, @nik, @tgl_lahir, @tempat_lahir,
                                @alamat_jalan, @alamat_rt, @alamat_rw, @alamat_dusun, @alamat_kelurahan, @alamat_kode_pos,
                                @kota_id)";

                        var mahasiswaParam = new
                        {
                            id = mahasiswa.ID,
                            perguruan_tinggi_id = prodi.PT.ID,
                            nama = mahasiswa.Nama,
                            jenis_kelamin = mahasiswa.Jenis_Kelamin,
                            nik = mahasiswa.NIK,
                            tgl_lahir = mahasiswa.Tgl_Lahir,
                            tempat_lahir = mahasiswa.Tempat_Lahir,
                            alamat_jalan = mahasiswa.Alamat.Jalan,
                            alamat_rt = mahasiswa.Alamat.Rt,
                            alamat_rw = mahasiswa.Alamat.Rw,
                            alamat_dusun = mahasiswa.Alamat.Dusun,
                            alamat_kelurahan = mahasiswa.Alamat.Kelurahan,
                            alamat_kode_pos = mahasiswa.Alamat.Kode_Pos,
                            kota_id = mahasiswa.Alamat.Kab_Kota.ID
                        };

                        await connection.ExecuteAsync(sqlInsertMahasiswa, mahasiswaParam);

                        // Pastikan ada terdaftar untuk bisa insert,
                        // karena memungkinkan mahasiswa sudah ada tetapi belum didaftarkan
                        if (mahasiswa.Terdaftar != null)
                        {
                            var sqlInsertMahasiswaPT =
                                @"INSERT INTO mahasiswa_pt (
                                    id, mahasiswa_id, kode_pt, kode_prodi, nim, tgl_masuk, tgl_keluar, smt_mulai, smt_tempuh,
                                    status, jenis_daftar_id, jenis_keluar_id)
                                VALUES (
                                    @id, @mahasiswa_id, @kode_pt, @kode_prodi, @nim, @tgl_masuk, @tgl_keluar, @smt_mulai, @smt_tempuh,
                                    @status, @jenis_daftar_id, @jenis_keluar_id)";

                            var mahasiswaPTParam = new
                            {
                                id = mahasiswa.Terdaftar.ID,
                                mahasiswa_id = mahasiswa.ID,
                                kode_pt = mahasiswa.Terdaftar.Kode_PT,
                                kode_prodi = mahasiswa.Terdaftar.Kode_Prodi,
                                nim = mahasiswa.Terdaftar.NIM,
                                tgl_masuk = mahasiswa.Terdaftar.Tgl_Masuk,
                                tgl_keluar = mahasiswa.Terdaftar.Tgl_Keluar,
                                smt_mulai = mahasiswa.Terdaftar.Smt_Mulai,
                                smt_tempuh = mahasiswa.Terdaftar.Smt_Tempuh,
                                status = mahasiswa.Terdaftar.Status,
                                jenis_daftar_id = mahasiswa.Terdaftar.Jenis_Daftar.ID,
                                jenis_keluar_id = mahasiswa.Terdaftar.Jenis_Keluar.ID
                            };

                            await connection.ExecuteAsync(sqlInsertMahasiswaPT, mahasiswaPTParam);
                        }
                    }

                    Console.WriteLine("OK");
                }

                mahasiswaList.Clear();
            }
        }

        static async Task PullDosenAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "", string filterProdi = "")
        {
            IEnumerable<ProgramStudi> prodiList;

            Console.Write("{0} Ambil data Program Studi dari DB ... ", DateTime.Now);

            string sqlSelectProdi =
                @"SELECT ps.id, ps.perguruan_tinggi_id, ps.kode, ps.nama, jd.id, jd.nama, pt.id, pt.kode, pt.nama FROM program_studi ps
                JOIN jenjang_didik jd ON jd.id = ps.jenjang_didik_id
                JOIN perguruan_tinggi pt ON pt.id = ps.perguruan_tinggi_id
                ";

            if (filterPT != "")
            {
                sqlSelectProdi += "WHERE pt.kode = '" + filterPT + "' ";

                if (filterProdi != "")
                {
                    sqlSelectProdi += "AND ps.kode = '" + filterProdi + "'";
                }
            }

            prodiList = await connection.QueryAsync<ProgramStudi, Jenjang, PerguruanTinggi, ProgramStudi>(sqlSelectProdi,
                (prodi, jenjang, pt) =>
                {
                    prodi.PT = pt;
                    prodi.Jenjang_Didik = jenjang;
                    return prodi;
                },
                splitOn: "id,id");

            Console.WriteLine("OK");

            foreach (var prodi in prodiList)
            {
                // Inisialisasi variabel kebutuhan
                var page = 0;
                var count = 0;
                var totalPage = 0;
                var retry = 0;

                var dosenList = new List<Dosen>();

                Console.Write("{0} [{1}] Ambil data dosen {2} {3} dari Forlap ... ", DateTime.Now, prodi.PT.Nama, prodi.Jenjang_Didik.Nama, prodi.Nama);
                var firstResponse = await client.GetListDosenAsync(prodi.Perguruan_Tinggi_ID.ToString(), prodi.ID.ToString(), page, Program.perPage);
                dosenList.AddRange(firstResponse.Data);

                // Ambil jumlah data dan total halaman
                count = firstResponse.TotalCount();
                totalPage = firstResponse.TotalPage();

                // Mengulang pengambilan next page apabila tidak sama dengan `count`
                // pada pengambilan pertama
                do
                {
                    // Persiapan multi task pengambilan
                    var getDosenTaskList = new List<Task<IRestResponse<List<Dosen>>>>();

                    // Ambil semua PT dari page selanjutnya
                    for (page = 1; page < totalPage; page++)
                        getDosenTaskList.Add(client.GetListDosenAsync(prodi.PT.ID.ToString(), prodi.ID.ToString(), page, Program.perPage));

                    // Mulai & Tunggu (Wait) semua beres
                    Task.WhenAll(getDosenTaskList.ToArray()).GetAwaiter().GetResult();

                    // Tambahkan data ke list
                    foreach (var task in getDosenTaskList)
                        dosenList.AddRange(task.Result.Data);

                    // Jika jumlah total yang didapat berbeda dengan count
                    if (dosenList.Count != count)
                    {
                        // Kosongi dan ambil isi data pada respon awal lagi
                        dosenList.Clear();
                        dosenList.AddRange(firstResponse.Data);

                        // Ulangi
                        retry++;
                    }
                    else
                        break;  // keluar dari loop jika sudah sama

                } while (retry < Program.maxRetries);

                Console.WriteLine("{0} total data", dosenList.Count);

                foreach (var dosen in dosenList)
                {
                    Console.Write("{0} [{1}] {2} {3} ... ", DateTime.Now, prodi.PT.Nama, dosen.NIDN, dosen.Nama);

                    // Check exist
                    var sqlCheckDosen = "select 1 from dosen where id = '" + dosen.ID + "'";
                    var dosenExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckDosen) != null);

                    // Jika dosen belum ada, insert
                    if (!dosenExist)
                    {
                        var sqlInsertDosen =
                            @"insert into dosen 
                            (id, nidn, nip, npwp, nik, nama, gelar_depan, gelar_belakang, pendidikan_terakhir,
                            jenis_kelamin, tgl_lahir, tempat_lahir, telepon, handphone, alamat_jalan, alamat_rt, alamat_rw, alamat_dusun,
                            alamat_kelurahan, alamat_kode_pos, kota_id, kode_pt, kode_prodi, sk_cpns, tgl_sk_cpns, sk_pengangkatan,
                            tgl_sk_pengangkatan, tmt_sk_pengangkatan, tmt_pns, kewarganegaraan, agama_id, ikatan_kerja_id,
                            status_kepegawaian_id, status_keaktifan_id, pangkat_golongan_id, jabatan_fungsional_id, last_update)
                            values
                            (@id,@nidn,@nip,@npwp,@nik,@nama,@gelar_depan,@gelar_belakang,@pendidikan_terakhir,
                            jenis_kelamin,@tgl_lahir,@tempat_lahir,@telepon,@handphone,@alamat_jalan,@alamat_rt,@alamat_rw,@alamat_dusun,
                            alamat_kelurahan,@alamat_kode_pos,@kota_id,@kode_pt,@kode_prodi,@sk_cpns,@tgl_sk_cpns,@sk_pengangkatan,
                            tgl_sk_pengangkatan,@tmt_sk_pengangkatan,@tmt_pns,@kewarganegaraan,@agama_id,@ikatan_kerja_id,
                            status_kepegawaian_id,@status_keaktifan_id,@pangkat_golongan_id,@jabatan_fungsional_id,@last_update)";

                        var dosenParam = new
                        {
                            id = dosen.ID,
                            nidn = dosen.NIDN,
                            nip = dosen.NIP,
                            npwp = dosen.NPWP,
                            nik = dosen.NIK,
                            nama = dosen.Nama,
                            gelar_depan = dosen.Gelar_Depan,
                            gelar_belakang = dosen.Gelar_Belakang,
                            pendidikan_terakhir = dosen.Pendidikan_Terakhir,
                            jenis_kelamin = dosen.Jenis_Kelamin,
                            tgl_lahir = dosen.Tgl_Lahir,
                            tempat_lahir = dosen.Tempat_Lahir,
                            telepon = dosen.Telepon,
                            handphone = dosen.Handphone,
                            alamat_jalan = dosen.Alamat.Jalan,
                            alamat_rt = dosen.Alamat.Rt,
                            alamat_rw = dosen.Alamat.Rw,
                            alamat_dusun = dosen.Alamat.Dusun,
                            alamat_kelurahan = dosen.Alamat.Kelurahan,
                            alamat_kode_pos = dosen.Alamat.Kode_Pos,
                            kota_id = dosen.Alamat.Kab_Kota.ID,
                            kode_pt = dosen.Kode_PT,
                            kode_prodi = dosen.Kode_Prodi,
                            sk_cpns = dosen.SK_CPNS,
                            tgl_sk_cpns = dosen.Tgl_SK_CPNS,
                            sk_pengangkatan = dosen.SK_Pengangkatan,
                            tgl_sk_pengangkatan = dosen.Tgl_SK_Pengangkatan,
                            tmt_sk_pengangkatan = dosen.TMT_SK_Pengangkatan,
                            tmt_pns = dosen.TMT_PNS,
                            kewarganegaraan = dosen.Kewarganegaraan,
                            agama_id = dosen.Agama.ID,
                            ikatan_kerja_id = dosen.Ikatan_Kerja.ID,
                            status_kepegawaian_id = dosen.Status_Kepegawaian.ID,
                            status_keaktifan_id = dosen.Status_Keaktifan.ID,
                            pangkat_golongan_id = dosen.Pangkat_Golongan.ID,
                            jabatan_fungsional_id = dosen.Jabatan_Fungsional.ID,
                            last_update = dosen.Last_Update
                        };

                        await connection.ExecuteAsync(sqlInsertDosen, dosenParam);
                    }

                    Console.WriteLine("OK");
                }

                // Kosongkan ram
                dosenList.Clear();
            }
        }

        static async Task PullAkreditasiPTAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "")
        {
            IEnumerable<PerguruanTinggi> ptList;

            if (filterPT == "")
            {
                // Get All PT from database
                Console.Write("{0} Ambil data semua PT dari DB ... ", DateTime.Now);
                ptList = await connection.QueryAsync<PerguruanTinggi>("select id, kode, nama from perguruan_tinggi");
                Console.WriteLine("OK");
            }
            else
            {
                // Get All PT from database
                Console.Write("{0} Ambil data PT [{1}] dari DB ... ", DateTime.Now, filterPT);
                ptList = await connection.QueryAsync<PerguruanTinggi>("select id, kode, nama from perguruan_tinggi where kode = '" + filterPT + "'");
                Console.WriteLine("OK");
            }

            foreach (var pt in ptList)
            {
                var akreditasiList = new List<Akreditasi>();

                Console.Write("{0} [{1}] Ambil data akreditasi dari Forlap ... ", DateTime.Now, pt.Nama);
                var response = await client.GetListAkreditasiPTAsync(pt.ID.ToString());
                akreditasiList.AddRange(response.Data);

                // Print jumlah data
                Console.WriteLine("{0} total data", akreditasiList.Count);

                foreach (var akreditasi in akreditasiList)
                {
                    Console.Write("{0} [{1}] Akreditasi PT No {2} ... ", DateTime.Now, pt.Nama, akreditasi.SK_Akreditasi);

                    var sqlCheckAkreditasi = "select 1 from akreditasi where id = '" + akreditasi.ID + "'";
                    var akreditasiExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckAkreditasi) != null);

                    if (!akreditasiExist)
                    {
                        var sqlInsertAkreditasi =
                            @"insert into akreditasi
                            (id, nilai, lembaga_akreditasi, sk_akreditasi, tgl_sk_akreditasi, tst_sk_akreditasi, last_update, perguruan_tinggi_id)
                            values
                            (@id,@nilai,@lembaga_akreditasi,@sk_akreditasi,@tgl_sk_akreditasi,@tst_sk_akreditasi,@last_update,@perguruan_tinggi_id)";

                        var akreditasiParam = new
                        {
                            id = akreditasi.ID,
                            nilai = akreditasi.Nilai,
                            lembaga_akreditasi = akreditasi.Lembaga_Akreditasi,
                            sk_akreditasi = akreditasi.SK_Akreditasi,
                            tgl_sk_akreditasi = akreditasi.Tgl_SK_Akreditasi,
                            tst_sk_akreditasi = akreditasi.TST_SK_Akreditasi,
                            last_update = akreditasi.Last_Update,
                            perguruan_tinggi_id = akreditasi.PT.ID
                        };

                        await connection.ExecuteAsync(sqlInsertAkreditasi, akreditasiParam);
                    }

                    Console.WriteLine("OK");
                }
            }
        }

        static async Task PullAkreditasiProdiAsync(IDbConnection connection, PDDiktiClient client, string filterPT = "", string filterProdi = "")
        {
            IEnumerable<ProgramStudi> prodiList;

            Console.Write("{0} Ambil data Program Studi dari DB ... ", DateTime.Now);

            string sqlSelectProdi =
                @"SELECT ps.id, ps.perguruan_tinggi_id, ps.kode, ps.nama, jd.id, jd.nama, pt.id, pt.kode, pt.nama FROM program_studi ps
                JOIN jenjang_didik jd ON jd.id = ps.jenjang_didik_id
                JOIN perguruan_tinggi pt ON pt.id = ps.perguruan_tinggi_id
                ";

            if (filterPT != "")
            {
                sqlSelectProdi += "WHERE pt.kode = '" + filterPT + "' ";

                if (filterProdi != "")
                {
                    sqlSelectProdi += "AND ps.kode = '" + filterProdi + "'";
                }
            }

            prodiList = await connection.QueryAsync<ProgramStudi, Jenjang, PerguruanTinggi, ProgramStudi>(sqlSelectProdi,
                (prodi, jenjang, pt) =>
                {
                    prodi.PT = pt;
                    prodi.Jenjang_Didik = jenjang;
                    return prodi;
                },
                splitOn: "id,id");

            Console.WriteLine("OK");

            foreach (var prodi in prodiList)
            {
                var akreditasiList = new List<Akreditasi>();

                Console.Write("{0} [{1}] Ambil data akreditasi {2} {3} dari Forlap ... ", DateTime.Now, prodi.PT.Nama, prodi.Jenjang_Didik.Nama, prodi.Nama);
                var response = await client.GetListAkreditasiProdiAsync(prodi.PT.ID.ToString(), prodi.ID.ToString());
                akreditasiList.AddRange(response.Data);

                // Print jumlah data
                Console.WriteLine("{0} total data", akreditasiList.Count);

                foreach (var akreditasi in akreditasiList)
                {
                    Console.Write("{0} [{1}] Akreditasi {2} {3} No {4} ... ", DateTime.Now, prodi.PT.Nama, prodi.Jenjang_Didik.Nama, prodi.Nama, akreditasi.SK_Akreditasi);

                    var sqlCheckAkreditasi = "select 1 from akreditasi where id = '" + akreditasi.ID + "'";
                    var akreditasiExist = (await connection.ExecuteScalarAsync<int?>(sqlCheckAkreditasi) != null);

                    if (!akreditasiExist)
                    {
                        var sqlInsertAkreditasi =
                            @"insert into akreditasi 
                            (id, prodi_id, nilai, lembaga_akreditasi, sk_akreditasi, tgl_sk_akreditasi, tst_sk_akreditasi, last_update, perguruan_tinggi_id)
                            values 
                            (@id,@prodi_id,@nilai,@lembaga_akreditasi,@sk_akreditasi,@tgl_sk_akreditasi,@tst_sk_akreditasi,@last_update,@perguruan_tinggi_id)";

                        var akreditasiParam = new
                        {
                            id = akreditasi.ID,
                            prodi_id = akreditasi.Prodi.ID,
                            nilai = akreditasi.Nilai,
                            lembaga_akreditasi = akreditasi.Lembaga_Akreditasi,
                            sk_akreditasi = akreditasi.SK_Akreditasi,
                            tgl_sk_akreditasi = akreditasi.Tgl_SK_Akreditasi,
                            tst_sk_akreditasi = akreditasi.TST_SK_Akreditasi,
                            last_update = akreditasi.Last_Update,
                            perguruan_tinggi_id = akreditasi.PT.ID
                        };

                        await connection.ExecuteAsync(sqlInsertAkreditasi, akreditasiParam);
                    }

                    Console.WriteLine("OK");
                }
            }
        }
    }
}
