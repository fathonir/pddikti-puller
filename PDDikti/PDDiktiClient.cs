using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PDDikti.Models;
using RestSharp;

namespace PDDikti
{
    public class PDDiktiClient
    {
        public Guid AccessToken { get; set; }
        public Uri EndPoint { get; set; }

        private RestClient _restClient { get; set; }

        public PDDiktiClient(Uri endPoint, Guid accessToken)
        {
            this._restClient = new RestClient(endPoint);
            this._restClient.AddDefaultHeader("Accept", "application/json");
            this._restClient.AddDefaultHeader("Authorization", "Bearer " + accessToken.ToString());

            this.EndPoint = endPoint;
            this.AccessToken = accessToken;
        }

        public async Task<IRestResponse<List<PerguruanTinggi>>> GetListPerguruanTinggiAsync(int page, int perPage)
        {
            var resource = "pt?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);

            return await this._restClient.ExecuteTaskAsync<List<PerguruanTinggi>>(request);
        }

        public async Task<IRestResponse<List<PerguruanTinggi>>> GetListPerguruanTinggiAsync(int page, int perPage, string kodePT)
        {
            var resource = "pt/{id-pt}?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);

            return await this._restClient.ExecuteTaskAsync<List<PerguruanTinggi>>(request);
        }

        public async Task<IRestResponse<List<ProgramStudi>>> GetListProgramStudiAsync(Guid idPT, int page, int perPage)
        {
            var resource = "pt/{id-pt}/prodi?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", idPT.ToString());
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);
            return await this._restClient.ExecuteTaskAsync<List<ProgramStudi>>(request);
        }

        public List<PerguruanTinggi> ListPerguruanTinggi(int page, int perPage, out int totalPage, out int count)
        {
            var resource = "pt?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);
            var response = this._restClient.Execute<List<PerguruanTinggi>>(request);

            totalPage = 0;
            count = 0;
            
            var headerTotalPage = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Page");
            if (headerTotalPage != null)
                totalPage = int.Parse(headerTotalPage.Value.ToString());

            var headerTotalCount = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Count");
            if (headerTotalCount != null)
                count = int.Parse(headerTotalCount.Value.ToString());

            return response.Data;
        }

        public List<PerguruanTinggi> ListPerguruanTinggi(string q, int page, int perPage, out int totalPage, out int count)
        {
            var resource = "pt?q={q}&page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("q", q);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);
            var response = this._restClient.Execute<List<PerguruanTinggi>>(request);

            totalPage = 0;
            count = 0;

            var headerTotalPage = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Page");
            if (headerTotalPage != null)
                totalPage = int.Parse(headerTotalPage.Value.ToString());

            var headerTotalCount = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Count");
            if (headerTotalCount != null)
                count = int.Parse(headerTotalCount.Value.ToString());

            return response.Data;
        }

        public List<PerguruanTinggi> ListPerguruanTinggi(int page, int perPage)
        {
            var totalPage = -1;
            var count = -1;
            return ListPerguruanTinggi(page, perPage, out totalPage, out count);
        }

        public List<PerguruanTinggi> ListPerguruanTinggi(string q, int page, int perPage)
        {
            var totalPage = -1;
            var count = -1;
            return ListPerguruanTinggi(q, page, perPage, out totalPage, out count);
        }

        public PerguruanTinggi GetPerguruanTinggi(string kode)
        {
            var resource = "pt/{id-pt}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kode);
            var response = this._restClient.Execute<List<PerguruanTinggi>>(request);
            return response.Data.SingleOrDefault();
        }

        public PerguruanTinggi GetPerguruanTinggi(Guid idPT)
        {
            var resource = "pt/{id-pt}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", idPT.ToString());
            var response = this._restClient.Execute<List<PerguruanTinggi>>(request);
            return response.Data.SingleOrDefault();
        }

        public List<PerguruanTinggi> CariPerguruanTinggi(string keyword)
        {
            var resource = "pt?q=" + keyword;
            var request = new RestRequest(resource, Method.GET);
            var response = this._restClient.Execute<List<PerguruanTinggi>>(request);
            return response.Data;
        }

        public List<ProgramStudi> ListProgramStudi(string kodePT)
        {
            var resource = "pt/{id-pt}/prodi?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("page", 0);
            request.AddUrlSegment("per-page", 500); // prodi dalam sebuah pt tak lebih dari 500
            var response = this._restClient.Execute<List<ProgramStudi>>(request);
            return response.Data;
        }

        public List<ProgramStudi> ListProgramStudi(Guid idPT)
        {
            var resource = "pt/{id-pt}/prodi?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", idPT.ToString());
            request.AddUrlSegment("page", 0);
            request.AddUrlSegment("per-page", 500); // prodi dalam sebuah pt tak lebih dari 500
            var response = this._restClient.Execute<List<ProgramStudi>>(request);
            return response.Data;
        }

        

        public ProgramStudi GetProgramStudi(string kodePT, string kodeProdi)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            var response = this._restClient.Execute<List<ProgramStudi>>(request);
            return response.Data.SingleOrDefault();
        }

        public ProgramStudi GetProgramStudi(Guid idPT, Guid idProgramStudi)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", idPT.ToString());
            request.AddUrlSegment("id-prodi", idProgramStudi.ToString());
            var response = this._restClient.Execute<List<ProgramStudi>>(request);
            return response.Data.SingleOrDefault();
        }

        public List<Dosen> ListDosen(string kodePT, string kodeProdi)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/dosen";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            var response = this._restClient.Execute<List<Dosen>>(request);
            return response.Data;
        }

        public Mahasiswa GetMahasiswa(string kodePT, string kodeProdi, string nim)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/mahasiswa/{nim}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            request.AddUrlSegment("nim", nim);
            var response = this._restClient.Execute<List<Mahasiswa>>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return response.Data.FirstOrDefault();
            else
                return null;
        }

        public List<Mahasiswa> ListMahasiswa(string kodePT, string kodeProdi, int page, int perPage, out int totalPage, out int count)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/mahasiswa?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);

            var response = this._restClient.Execute<List<Mahasiswa>>(request);

            totalPage = 0;
            count = 0;

            var headerTotalPage = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Page");
            if (headerTotalPage != null)
                totalPage = int.Parse(headerTotalPage.Value.ToString());

            var headerTotalCount = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Count");
            if (headerTotalCount != null)
                count = int.Parse(headerTotalCount.Value.ToString());

            return response.Data;
        }

        public async Task<IRestResponse<List<Mahasiswa>>> GetListMahasiswaAsync(string kodePT, string kodeProdi, int page, int perPage)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/mahasiswa?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);

            return await this._restClient.ExecuteTaskAsync<List<Mahasiswa>>(request);
        }

        public async Task<IRestResponse<List<Dosen>>> GetListDosenAsync(string kodePT, string kodeProdi, int page, int perPage)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/dosen?page={page}&per-page={per-page}";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);
            request.AddUrlSegment("page", page);
            request.AddUrlSegment("per-page", perPage);

            return await this._restClient.ExecuteTaskAsync<List<Dosen>>(request);
        }

        public async Task<IRestResponse<List<Akreditasi>>> GetListAkreditasiProdiAsync(string kodePT, string kodeProdi)
        {
            var resource = "pt/{id-pt}/prodi/{id-prodi}/akreditasi";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);
            request.AddUrlSegment("id-prodi", kodeProdi);

            return await this._restClient.ExecuteTaskAsync<List<Akreditasi>>(request);
        }

        public async Task<IRestResponse<List<Akreditasi>>> GetListAkreditasiPTAsync(string kodePT)
        {
            var resource = "pt/{id-pt}/akreditasi";
            var request = new RestRequest(resource, Method.GET);
            request.AddUrlSegment("id-pt", kodePT);

            return await this._restClient.ExecuteTaskAsync<List<Akreditasi>>(request);
        }
    }
}
