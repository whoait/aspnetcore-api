using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GoogleTranslateAPI;
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
    public partial class GoogleController : Controller
    {

        private static readonly GoogleTranslator Translator = new GoogleTranslator();

        private readonly string connectionString;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IMemoryCache memoryCache;

        private IHostingEnvironment hostingEnvironment;

        public GoogleController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IHostingEnvironment hostingEnvironment)
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

            Language english = Language.English; // or even this way
            Language vietnamese = Language.Vietnamese; // or even this way

            TranslationResult result = await Translator.TranslateAsync(text, english, vietnamese);

            var response = new
            {
                ok = true,
                result = new 
                {
                    result.OriginalText,
                    result.OriginalTextTranscription,
                    result.FragmentedTranslation,
                    result.MergedTranslation,
                    result.ExtraTranslations
                }
            };

            return new OkObjectResult(response);
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