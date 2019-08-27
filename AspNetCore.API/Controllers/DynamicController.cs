using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.API.Controllers
{
    [ApiVersion("1.0")]    
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class DynamicController : ControllerBase
    {

        private readonly string connectionString;
        private readonly IConfiguration configuration;

        public DynamicController(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        // Endpoint: http://localhost:58638/api/v1/dynamic/hello-world
        [HttpGet("hello-world")]
        [Produces("application/json")]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public IActionResult HelloWorld()
        {
            return new OkObjectResult(new { ok = true, message = "Hello World!" });
        }

        // ------------------------------------------------------------------------------------------------------------
        [HttpPost("query-sql")]
        [Produces("application/json")]
        [ProducesResponseType(200, Type = typeof(JsonResult))]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public async Task<IActionResult> QuerySql([FromBody] dynamic body)
        {
            try
            {
                using (var db = new SqlConnection(this.connectionString))
                {
                    var sql = body.sqlCommand.Value as string;

                    var jbody = new JObject(body);

                    var parameters = new DynamicParameters();

                    foreach (var p in jbody.GetValue("parameters").Children())
                    {
                        var jparameter = (JProperty)p;
                        parameters.Add(jparameter.Name, jparameter.Value.ToString());
                    }

                    var results = await db.QueryAsync<dynamic>(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);

                    var response = new
                    {
                        ok = true,
                        results = results
                    };

                    return new OkObjectResult(response);
                }
            }
            catch (Exception e)
            {
                var response = new
                {
                    ok = false,
                    error = e.Message,
                    data = body
                };

                return new BadRequestObjectResult(response);
            }
        }      
    }
}

