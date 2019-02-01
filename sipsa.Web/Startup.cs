﻿using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using sipsa.Dados;
using sipsa.Dominio.Usuarios;
using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon;
using sipsa.Dados.Repositorios;
using sipsa.Dominio.Membros;

namespace sipsa.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Injeção das classes do sistema.
            services.AddScoped(typeof(IUsuarioRepositorio), typeof(UsuarioRepositorio));
            services.AddScoped(typeof(IMembroRepositorio), typeof(MembroRepositorio));
            services.AddScoped<UsuarioAutenticacao>();

            // Serviço de autenticação e armazenamento em cookie.
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Logon/Login";
                    options.AccessDeniedPath = "/Logon/AccessDenied";
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Administrador", policy =>
                    policy.RequireClaim(ClaimTypes.Role, "Administrador"));
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Serviço de comunicação com o banco de dados.
            var credenciais = new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("AWSDynamoDB_AccessKey"), 
                Environment.GetEnvironmentVariable("AWSDynamoDB_SecretKey"));
            var client = new AmazonDynamoDBClient(credenciais, RegionEndpoint.SAEast1);
            services.AddSingleton(new SipsaContexto(client));

            // Mvc
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
