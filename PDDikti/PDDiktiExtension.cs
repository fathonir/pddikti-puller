using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDDikti
{
    public static class PDDiktiExtension
    {
        public static int TotalPage<T>(this IRestResponse<T> response) where T : class
        {
            var totalPage = 0;

            var headerTotalPage = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Page");
            if (headerTotalPage != null)
                totalPage = int.Parse(headerTotalPage.Value.ToString());

            return totalPage;
        }

        public static int TotalCount<T>(this IRestResponse<T> response) where T : class
        {
            var count = 0;

            var headerTotalCount = response.Headers.SingleOrDefault(h => h.Name == "X-Total-Count");

            if (headerTotalCount != null)
                count = int.Parse(headerTotalCount.Value.ToString());

            return count;
        }
    }
}
