using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace JwtDemo.Controllers
{
    [Route("api/test")]
	//[AllowAnonymous]
	public class JwtAuthTestController : Controller
    {
		private readonly JsonSerializerSettings _serializerSettings;
		private readonly ILogger _logger;
		public JwtAuthTestController(ILoggerFactory fa)
		{
			_logger = fa.CreateLogger<JwtAuthTestController>();
			_serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
		}

		[HttpPost]
		[Authorize(Policy = "LoginUser")]
		public IActionResult Post()
		{
			_logger.LogInformation("In Post Method");
			var response = new
			{
				made_it = "Welcome To JWT ! you are loginuser:" + User.Identity.Name
			};
			var resJson = JsonConvert.SerializeObject(response, _serializerSettings);
			return new OkObjectResult(resJson);
		}

		[HttpGet]
		[Authorize(Policy = "GuestUser")]
		public IActionResult Get()
		{
			_logger.LogInformation("In Get Method");
			var response = new
			{
				made_it = "Welcome To JWT ! You are Guest"
			};
			var resJson = JsonConvert.SerializeObject(response, _serializerSettings);
			return new OkObjectResult(resJson);
		}
	}
}
