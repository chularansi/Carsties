using AuctionService.Data;
using AuctionService.IntegrationTests.Util;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using WebMotions.Fake.Authentication.JwtBearer;

namespace AuctionService.IntegrationTests.Fixtures
{
    public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder().Build();

        public async Task InitializeAsync()
        {
            await postgreSqlContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // **** Move this part to ServiceCollectionExtensions.cs file

                //var descriptor = services.SingleOrDefault(d => 
                //    d.ServiceType == typeof(DbContextOptions<AuctionDbContext>));

                //if (descriptor != null) 
                //{ 
                //    services.Remove(descriptor); 
                //}
                services.RemoveDbContext<AuctionDbContext>();

                services.AddDbContext<AuctionDbContext>(options =>
                {
                    options.UseNpgsql(postgreSqlContainer.GetConnectionString());
                });

                services.AddMassTransitTestHarness();

                // **** Move this part to ServiceCollectionExtensions.cs file

                //var sp = services.BuildServiceProvider();

                //using (sp) 
                //{
                //    var scope = sp.CreateScope();
                //    var scopedService = scope.ServiceProvider;

                //    var db = scopedService.GetRequiredService<AuctionDbContext>();
                //    db.Database.Migrate();
                //    DbHelper.InitDbForTests(db);
                //}
                services.EnsureCreated<AuctionDbContext>();

                services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme)
                    .AddFakeJwtBearer(opt =>
                    {
                        opt.BearerValueType = FakeJwtBearerBearerValueType.Jwt;
                    });
            });
        }

        Task IAsyncLifetime.DisposeAsync() => postgreSqlContainer.DisposeAsync().AsTask();
    }
}

internal class PostgresSqlContainer
{
}