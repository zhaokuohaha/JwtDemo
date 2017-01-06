using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using JwtDemo.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace JwtDemo
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

		//jwt密钥配置  --> 密钥为jwt安全性的关键, 正式项目中应该从配置文件读取
		private const string SecretKey = "need_to_get_this_from_enviroment";
		private readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
        {
			//1.  
			//确保所有内容都需要认证
            services.AddMvc(config => {
				var policy = new AuthorizationPolicyBuilder()
								.RequireAuthenticatedUser().Build();
				config.Filters.Add(new AuthorizeFilter(policy));
			});

			//2. 
			//添加 可配置功能
			services.AddOptions();
			//从配置文件都取jwt配置
			var jwtAppSeetingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));
			//配置jwt
			services.Configure<JwtIssuerOptions>(options =>
			{
				options.Issuer = jwtAppSeetingOptions[nameof(JwtIssuerOptions.Issuer)];
				options.Audience = jwtAppSeetingOptions[nameof(JwtIssuerOptions.Audience)];
				options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
			});


			//使用权限验证方案
			services.AddAuthorization(options =>
			{
				options.AddPolicy("LoginUser", policy => policy.RequireClaim("LoginCharacter", "I_am_zhaokuo"));
				options.AddPolicy("GuestUser", policy => policy.RequireClaim("LoginCharacter", "I_am_guest"));
			});
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }
    }
}
