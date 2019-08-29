using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.Modules.BCM.Controllers
{
    public partial class BcmController
    {
        private IList<string> GetSafeProcedures()
        {
            IList<string> cacheEntry;

            string key = "SafeStoreProcedures";
            if (!this.memoryCache.TryGetValue(key, out cacheEntry))
            {
                var physicalDirectoryPath = this.hostingEnvironment.ContentRootPath + "\\Modules\\AspNetCore.Modules.BCM\\Data\\";
                var physicalFilePath = Path.Combine(physicalDirectoryPath, "data.json");

                using (StreamReader r = new StreamReader(physicalFilePath))
                {
                    string jsonString = r.ReadToEnd();
                    var json = JsonConvert.DeserializeObject<dynamic>(jsonString);
                    var s = new JObject(json);
                    var ss = s.GetValue("safeStoredProcedures");
                    cacheEntry = JsonConvert.DeserializeObject<ExpandoObject>(json);
                    //var safeStoredProcedures = new JArray(cacheEntry);

                    // Set cache options.
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(60));
                    // Set cache
                    this.memoryCache.Set(key, cacheEntry);
                }
            }

            return new List<string>();
        }

        private bool ValidateStoredProcedure(string storedprocedureName)
        {
            //var name = storedprocedureName.ToUpper();
            //if (name != null && (name.Contains(";") || name.Contains("SELECT") || name.Contains("DROP") || name.Contains("ALTER") || name.Contains("CREATE") || name.Contains("UPDATE") || name.Contains("DELETE") || name.Contains("INSERT") || name.Contains("EXEC")))
            //{
            //    return false;
            //}

            return true;
            //return this.GetSafeProcedures().Any(x => x == storedprocedureName);
        }

        // ------------------------------------------------------------------------------------------------------------
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        [HttpPost("query-sql")]
        public async Task<IActionResult> QuerySql(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].First();
                var key = authorizationHeader.Split(' ')[1];
                if (string.IsNullOrEmpty(key))
                {
                    return new BadRequestResult();
                }
                if (ValidateSecret(key) == false)
                {
                    return new BadRequestResult();
                }


                using (var db = new SqlConnection(this.connectionString))
                {
                    var sql = body.sqlCommand.Value as string;
                    
                    if (apiVersion.MajorVersion > 1)
                    {
                        sql = sql + "_V" + apiVersion.MajorVersion.ToString().ToUpper();
                    }                    

                    if (this.ValidateStoredProcedure(sql) == false)
                    {
                        return new BadRequestResult();
                    }

                    var jbody = new JObject(body);

                    var parameters = new DynamicParameters();

                    foreach (var p in jbody.GetValue("parameters").Children())
                    {
                        var jparameter = (JProperty)p;
                        parameters.Add(jparameter.Name, jparameter.Value.ToString());
                    }

                    var items = await db.QueryAsync<dynamic>(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);

                    // FORMAT OUTPUT
                    var serializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };


                    var finalResults = new List<ExpandoObject>();

                    foreach (var result in items)
                    {
                        dynamic finalResult = new ExpandoObject();
                        IDictionary<string, object> finalProperties = (IDictionary<string, object>)finalResult;
                        IDictionary<string, object> propertyValues = (IDictionary<string, object>)result;

                        foreach (var x in propertyValues)
                        {
                            finalProperties.Add(x.Key, x.Value);

                            if (x.Value != null)
                            {
                                if (x.Value.ToString().StartsWith("{") || (x.Value.ToString().StartsWith("[{")))
                                {
                                    var json = JsonConvert.DeserializeObject(x.Value.ToString(), serializerSettings);
                                    finalProperties[x.Key] = json;
                                }
                            }

                        }

                        finalResults.Add((ExpandoObject)finalProperties);
                    }

                    var response = new
                    {
                        ok = true,
                        results = finalResults
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

        // ------------------------------------------------------------------------------------------------------------
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        [HttpPost("query-sql/first")]
        public async Task<IActionResult> QueryFirstSql(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].First();
                var key = authorizationHeader.Split(' ')[1];
                if (string.IsNullOrEmpty(key))
                {
                    return new BadRequestResult();
                }
                if (ValidateSecret(key) == false)
                {
                    return new BadRequestResult();
                }

                using (var db = new SqlConnection(this.connectionString))
                {
                    var sql = body.sqlCommand.Value as string;
                    if (apiVersion.MajorVersion > 1)
                    {
                        sql = sql + "_V" + apiVersion.MajorVersion.ToString().ToUpper();
                    }

                    if (this.ValidateStoredProcedure(sql) == false)
                    {
                        return new BadRequestResult();
                    }

                    var jbody = new JObject(body);

                    var parameters = new DynamicParameters();

                    foreach (var p in jbody.GetValue("parameters").Children())
                    {
                        var jparameter = (JProperty)p;
                        parameters.Add(jparameter.Name, jparameter.Value.ToString());
                    }

                    var result = await db.QueryFirstOrDefaultAsync<dynamic>(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);

                    // FORMAT OUTPUT
                    var serializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    dynamic finalResult = new ExpandoObject();
                    IDictionary<string, object> finalProperties = (IDictionary<string, object>)finalResult;
                    IDictionary<string, object> propertyValues = (IDictionary<string, object>)result;

                    foreach (var x in propertyValues)
                    {
                        finalProperties.Add(x.Key, x.Value);

                        if (x.Value != null)
                        {
                            if (x.Value.ToString().StartsWith("{") || (x.Value.ToString().StartsWith("[{")))
                            {
                                var json = JsonConvert.DeserializeObject(x.Value.ToString(), serializerSettings);
                                finalProperties[x.Key] = json;
                            }
                        }
                    }


                    var response = new
                    {
                        ok = true,
                        result = (ExpandoObject)finalProperties
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

        // ------------------------------------------------------------------------------------------------------------
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        [HttpPost("execute-sql")]
        public async Task<IActionResult> ExecuteSql(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].First();
                var key = authorizationHeader.Split(' ')[1];
                if (string.IsNullOrEmpty(key))
                {
                    return new BadRequestResult();
                }
                if (ValidateSecret(key) == false)
                {
                    return new BadRequestResult();
                }


                using (var db = new SqlConnection(this.connectionString))
                {
                    var sql = body.sqlCommand.Value as string;
                    if (apiVersion.MajorVersion > 1)
                    {
                        sql = sql + "_V" + apiVersion.MajorVersion.ToString().ToUpper();
                    }

                    if (this.ValidateStoredProcedure(sql) == false)
                    {
                        return new BadRequestResult();
                    }

                    var jbody = new JObject(body);

                    var parameters = new DynamicParameters();

                    foreach (var p in jbody.GetValue("parameters").Children())
                    {
                        var jparameter = (JProperty)p;
                        parameters.Add(jparameter.Name, jparameter.Value.ToString());
                    }

                    var result = await db.ExecuteAsync(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);
                    var response = new
                    {
                        ok = true,
                        result
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

        // ------------------------------------------------------------------------------------------------------------
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        [HttpPost("query-sql/first/advanced")]
        public async Task<IActionResult> QueryFirstSqlWithAdvancedParameters(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].First();
                var key = authorizationHeader.Split(' ')[1];
                if (string.IsNullOrEmpty(key))
                {
                    return new BadRequestResult();
                }
                if (ValidateSecret(key) == false)
                {
                    return new BadRequestResult();
                }

                using (var db = new SqlConnection(this.connectionString))
                {
                    var sql = body.sqlCommand.Value as string;
                    if (apiVersion.MajorVersion > 1)
                    {
                        sql = sql + "_V" + apiVersion.MajorVersion.ToString().ToUpper();
                    }

                    if (this.ValidateStoredProcedure(sql) == false)
                    {
                        return new BadRequestResult();
                    }

                    var jbody = new JObject(body);

                    var parameters = new DynamicParameters();

                    foreach (var p in jbody.GetValue("parameters").Children())
                    {
                        var jparameter = (JObject)p;
                        parameters.Add(name: jparameter.GetValue("name").ToString(), value: jparameter.GetValue("value").ToString());
                    }

                    dynamic result = await db.QueryFirstOrDefaultAsync<dynamic>(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);


                    // FORMAT OUTPUT
                    var serializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    dynamic finalResult = new ExpandoObject();
                    IDictionary<string, object> finalProperties = (IDictionary<string, object>)finalResult;
                    IDictionary<string, object> propertyValues = (IDictionary<string, object>)result;

                    foreach (var x in propertyValues)
                    {
                        finalProperties.Add(x.Key, x.Value);

                        if (x.Value != null)
                        {
                            if (x.Value.ToString().StartsWith("{") || (x.Value.ToString().StartsWith("[{")))
                            {
                                var json = JsonConvert.DeserializeObject(x.Value.ToString(), serializerSettings);
                                finalProperties[x.Key] = json;
                            }
                        }
                    }


                    var response = new
                    {
                        ok = true,
                        result = (ExpandoObject)finalProperties
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