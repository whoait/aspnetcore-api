# Requirements:

- AspNet Core 2.2

#Nuget:

- Microsoft.AspNetCore.Mvc.Versioning
- Dapper
- Newtonsoft.Json
- Swashbuckle.AspNetCore

# Startup:

```
public void ConfigureServices(IServiceCollection services)
{
  //CORS
  services.AddCors(o => o.AddPolicy("Allow-All", builder =>
  {
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
  }));

  // JSON Camel Case Property Names
  // Nuget: Newtonsoft.Json
  services.AddMvc().AddJsonOptions(options =>
  {
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
  });

  // API Versioning
  // Nuget: Microsoft.AspNetCore.Mvc.Versioning
  services.AddApiVersioning();

   // Register the Swagger generator, defining 1 or more Swagger documents
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Info
        {
            Version = "v1",
            Title = "ToDo API",
            Description = "A simple example ASP.NET Core Web API",
            TermsOfService = "None",
            Contact = new Contact
            {
                Name = "Tung Ngo",
                Email = "tungnt@softech.vn",
                Url = "https://softech.vn"
            },
            License = new License
            {
                Name = "Use under LICX",
                Url = "https://example.com/license"
            }
        });

        //Set the comments path for the Swagger JSON and UI.

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });
}

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET Core API");
        //c.RoutePrefix = string.Empty;
    });

    app.UseMvc();
}

```

# Controller:

```
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
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
    /// <summary>
    ///
    /// </summary>
    /// <param name="body">
    /// Dynamic value
    /// </param>
    /// <returns>JSON</returns>
    /// <response code="200">OK</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST
    ///     {
    ///         "sqlCommand": "[p_API_GetProducts]",
    ///         "parameters": {
    ///             "color": "Black"
    ///         }
    ///     }
    ///
    /// </remarks>
    [HttpPost("query-sql")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
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
```

# Postman (Call API tool):

- Download: https://www.getpostman.com/

# Run project:

- At browser, type url: http://localhost:58638/api/v1.0/dynamic/hello-world

# Prepare (Api will call a stored procedure):

- ConnectionString: look in appsettings.json
- Create a stored procedure:

```
ALTER PROCEDURE [p_API_GetProducts]
	@Color VARCHAR(50)
AS
BEGIN
	SELECT * FROM [SalesLT].[Product] WHERE Color = @Color
END
```

# Call api (testing):

- Endpoint url: http://localhost:58638/api/v1/dynamic/query-sql
- Method: POST
- Content-Type: application/json
- Body:

```
{
    "sqlCommand": "[p_API_GetProducts]",
    "parameters": {
    	"color": "Black"
    }
}
```

# Swagger: http://localhost:58638/swagger/index.html

# Test on Azure (within Postman tool):

- Endpoint url: https://dynamicaspnetcoreapi.azurewebsites.net/api/v1/dynamic/query-sql
- Method: POST
- Content-Type: application/json
- Body:

```
{
    "sqlCommand": "[p_API_GetProducts]",
    "parameters": {
    	"color": "Black"
    }
}
```
