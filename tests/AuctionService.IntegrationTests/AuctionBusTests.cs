using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Contracts;
using MassTransit.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests
{
    //If we use this way without collection, it will create two postgres db servers when run testing.
    //public class AuctionBusTests : IClassFixture<CustomWebAppFactory>, IAsyncLifetime

    //This method only use one postgres db server
    [Collection("Shared collection")]
    public class AuctionBusTests : IAsyncLifetime
    {
        private readonly CustomWebAppFactory factory;
        private readonly HttpClient httpClient;
        private readonly ITestHarness testHarness;

        public AuctionBusTests(CustomWebAppFactory factory)
        {
            this.factory = factory;
            httpClient = factory.CreateClient();
            testHarness = factory.Services.GetTestHarness(); // For in-memory service bus
        }

        [Fact]
        public async Task CreateAuction_WithValidObject_ShouldPublishAuctionCreated()
        {
            // arrange
            var auction = GetAuctionForCreate();
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

            // act
            var response = await httpClient.PostAsJsonAsync("api/auctions", auction);

            // assert
            response.EnsureSuccessStatusCode();
            Assert.True(await testHarness.Published.Any<AuctionCreated>());
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            DbHelper.ReinitDbForTests(db);
            return Task.CompletedTask;
        }

        private static CreateAuctionDto GetAuctionForCreate()
        {
            return new CreateAuctionDto
            {
                Make = "test",
                Model = "testModel",
                ImageUrl = "test",
                Color = "test",
                Mileage = 10,
                Year = 10,
                ReservePrice = 10
            };
        }
    }
}
