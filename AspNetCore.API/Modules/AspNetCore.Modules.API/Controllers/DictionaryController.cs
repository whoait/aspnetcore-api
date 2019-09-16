using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GoogleTranslateAPI;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Modules.BCM.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public partial class DictionaryController : Controller
    {

        private static readonly GoogleTranslator Translator = new GoogleTranslator();

        private readonly string connectionString;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IMemoryCache memoryCache;

        private IHostingEnvironment hostingEnvironment;

        public DictionaryController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IHostingEnvironment hostingEnvironment)
        {
            this.configuration = configuration;
            this.connectionString = configuration.GetConnectionString("DefaultConnection");

            this.httpClientFactory = httpClientFactory;

            this.hostingEnvironment = hostingEnvironment;

            this.memoryCache = memoryCache;
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public async Task<IActionResult> Post(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            var text = body.text.Value as string;

            var url = $"http://tratu.coviet.vn/hoc-tieng-anh/tu-dien/lac-viet/A-V/{text}.html";
            try
            {

                var web = new HtmlWeb();
                var document = web.Load(url);
                var root = document.DocumentNode.SelectSingleNode("//div[@id='mtd_0']");

                dynamic result = new ExpandoObject();
                // Text
                result.text = text;
                result.phonetic = root.SelectSingleNode("//div[@class='p5l fl cB']").InnerText.Replace("[", "/").Replace("]", "/");
                // Types
                var types = new List<ExpandoObject>();

                var nodes = root.SelectNodes("//div[@class='p10']");

                foreach (var part in nodes.Descendants("div"))
                {
                    foreach (var ub in part.Descendants("div").Where(x => x.GetAttributeValue("class", "").Equals("ub")))
                    {
                        dynamic typeItem = new ExpandoObject();
                        if (ub.InnerText.Trim() != "Từ liên quan")
                        {
                            typeItem.wordType = ub.InnerText.Trim();
                            var means = part.Descendants("div").Where(x => x.GetAttributeValue("class", "").Equals("m"))
                                .Select(m => m.InnerText.Trim().Replace("( ", "(").Replace(" )", ")"))
                                .ToList();
                            typeItem.means = means;
                            types.Add(typeItem);
                        }
                    }
                }

                result.description = types;

                return Json(new
                {
                    ok = true,
                    result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    error = ex
                });
            }
        }


        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public IActionResult Get()
        {
            var response = new
            {
                ok = true,
            };

            return new OkObjectResult(response);
        }
    }
}