using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JwtDemo.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using JwtDemo.Models;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
//需要导入依赖包
using System.IdentityModel.Tokens.Jwt;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace JwtDemo.Controllers
{
    [Route("api/[controller]")]
    public class JwtController : Controller
    {
		private readonly JwtIssuerOptions _jwtOptions;
		private readonly ILogger _logger;
		/// <summary>
		/// json 序列化方法, 控制器初始化后可以在每个方法返回时使用
		/// </summary>
		private readonly JsonSerializerSettings _seriallizerSettings;

		/// <summary>
		/// 构造函数, 使用依赖注入进行相关配置
		/// </summary>
		/// <param name="jwtoptions"></param>
		/// <param name="loggerFactory"></param>
		public JwtController(IOptions<JwtIssuerOptions> jwtoptions,
						ILoggerFactory loggerFactory)
		{
			_jwtOptions = jwtoptions.Value;
			ThrowIfInvalidOptions(_jwtOptions);
			_logger = loggerFactory.CreateLogger<JwtController>();
			_seriallizerSettings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			};
		}

		//请求方法
		/// <summary>
		/// Post方法, 相当于登录
		/// 这里用户信息使用了.net core 的模型绑定, 直接从表单中绑定用户信息, 所有测试的时候应该使用 'xxx-form-urlendcode'类型
		/// </summary>
		/// <param name="applicationUser">用户信息</param>
		/// <returns>登录结果</returns>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Get([FromForm] ApplicationUser applicationUser)
		{
			var identity = await LoginValidate(applicationUser);
			if(identity == null)
			{
				_logger.LogInformation($"Invalid username({applicationUser}) or password({applicationUser.Password}");
				return BadRequest("Invalid Credentials");
			}

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, applicationUser.UserName),
				new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
				new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssueAt).ToString(), ClaimValueTypes.Integer64),
				identity.FindFirst("LoginCharacter")
			};

			//生成 jwt 安全token, 并编码
			var jwt = new JwtSecurityToken(
					issuer: _jwtOptions.Issuer,
					audience: _jwtOptions.Audience,
					claims: claims,
					notBefore: _jwtOptions.NotBefore,
					expires: _jwtOptions.Expiration,
					signingCredentials:_jwtOptions.SigningCredentials
				);
			string encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

			//序列化返回的对象
			var response = new
			{
				access_token = encodedJwt,
				expires_in = (int)_jwtOptions.ValidFor.TotalSeconds
			};
			var resJson = JsonConvert.SerializeObject(response, _seriallizerSettings);
			return new OkObjectResult(resJson);
		}


		//相关静态方法

		/// <summary>
		/// 验证jwt配置是否合法
		/// </summary>
		/// <param name="options">配置对象</param>
		private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.ValidFor <= TimeSpan.Zero)
				throw new ArgumentException("Must be a non-zero TimeSpan", nameof(JwtIssuerOptions.ValidFor));
			if (options.SigningCredentials == null)
				throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
			if (options.JtiGenerator == null)
				throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
		}

		/// <summary>
		/// 从1970年到某时间的毫秒数
		/// </summary>
		/// <param name="date">计算时间</param>
		/// <returns></returns>
		private static long ToUnixEpochDate(DateTime date)
			=> (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

		/// <summary>
		/// 模拟登录验证操作
		/// </summary>
		/// <param name="user">用户</param>
		/// <returns>带有权限的对象</returns>
		/// <remarks>此方法为验证方法, 在正式项目中作为授权和配置权限使用, 注意与start up中的权限对应</remarks>
		private static Task<ClaimsIdentity> LoginValidate(ApplicationUser user)
		{
			//登录用户
			if(user.UserName == "zhaokuo" && user.Password == "zhaokuo12345")
			{
				return Task.FromResult(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"),
					new[]
					{
						new Claim("LoginCharacter","I_am_zhaokuo")
					}));
			}
			//访客用户 这里不同的用户可以看成不同的角色
			if (user.UserName == "guest" && user.Password == "guest")
			{
				return Task.FromResult(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"),
					new[]
					{
						new Claim("LoginCharacter","I_am_guest")
					}));
			}
			//身份证不通过
			return Task.FromResult<ClaimsIdentity>(null);
		}
    }
}
