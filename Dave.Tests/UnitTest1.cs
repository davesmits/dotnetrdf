using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Storage;

namespace Dave.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var _store = new FusekiConnector($"http://localhost:3030/Test/data");

            var tasklist = new List<Task<Stopwatch>>();
            for (int i = 0; i < 30; i++)
            {
                string query = @"SELECT * WHERE
                           {
                             ?s ?p ?o.
                             ?s <http://www.pandoraintelligence.com/esc12/typeinfo/type> 'Pandora.Domain.Relationship, Pandora.Domain'.
                             ?s <http://www.pandoraintelligence.com/esc12/relation/has_ledger> <http://www.pandoraintelligence.com/esc12/ledger/00d3e344-1f72-4f60-8cb2-6559170236c4>.
                     
                }
                ";

                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);
                query = regex.Replace(query, " ");

                var t = Task.Run(() =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    _store.Query(query);

                    stopwatch.Stop();
                    return stopwatch;
                });
                tasklist.Add(t);
            }

            var stopwatches = await Task.WhenAll(tasklist);
            var sorted = stopwatches.OrderByDescending(x => x.Elapsed).Select(x => x.Elapsed).ToList();
            var average = Avg(sorted);

            Assert.AreEqual(0, stopwatches.Where(x => x.Elapsed > TimeSpan.FromSeconds(2)).Count());
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            string url = "http://localhost:3030/Test/query?query=SELECT%20%2A%20WHERE%0D%0A%20%7B%0D%0A%20%3Fs%20%3Fp%20%3Fo.%0D%0A%20%3Fs%20%3Chttp%3A%2F%2Fwww.pandoraintelligence.com%2Fesc12%2Ftypeinfo%2Ftype%3E%20%27Pandora.Domain.Relationship%2C%20Pandora.Domain%27.%0D%0A%20%3Fs%20%3Chttp%3A%2F%2Fwww.pandoraintelligence.com%2Fesc12%2Frelation%2Fhas_ledger%3E%20%3Chttp%3A%2F%2Fwww.pandoraintelligence.com%2Fesc12%2Fledger%2F00d3e344-1f72-4f60-8cb2-6559170236c4%3E.%0D%0A%20%0D%0A%20%7D%0D%0A%20";
            var tasklist = new List<Task<Stopwatch>>();

            HttpClient client = new HttpClient();

            for (int i = 0; i < 30; i++)
            {
                var t = Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();

                    var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                    request.Headers.TryAddWithoutValidation("accept", MimeTypesHelper.HttpRdfOrSparqlAcceptHeader);

                    await client.SendAsync(request);
                    stopwatch.Stop();

                    return stopwatch;
                });
                tasklist.Add(t);
            }

            var stopwatches = await Task.WhenAll(tasklist);

            var sorted = stopwatches.OrderByDescending(x => x.Elapsed).Select(x => x.Elapsed).ToList();
            var average = Avg(sorted);

            Assert.AreEqual(0, stopwatches.Where(x => x.Elapsed > TimeSpan.FromSeconds(2)).Count());

        }

        public static TimeSpan Avg(List<TimeSpan> input)
        {
            var time = TimeSpan.Zero;
            foreach (var t in input)
                time += t;

            return time / input.Count;
        }
    }
}
