using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace AuctionService.IntegrationTests
{
    //If we use this way without collection, it will create two postgres db servers when run testing.
    //public class AuctionControllerTests : IClassFixture<CustomWebAppFactory>, IAsyncLifetime

    //This method only use one postgres db server
    [Collection("Shared collection")]
    public class AuctionControllerTests : IAsyncLifetime
    {
        private readonly CustomWebAppFactory factory;
        private readonly HttpClient httpClient;
        private const string GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

        public AuctionControllerTests(CustomWebAppFactory factory)
        {
            this.factory = factory;
            this.httpClient = factory.CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            DbHelper.ReinitDbForTests(db);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetAuctions_ShouldReturn3Auctions()
        {
            // arrange? 

            // act
            var response = await httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");

            // assert
            Assert.Equal(3, response.Count);
        }

        [Fact]
        public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
        {
            // arrange? 

            // act
            var response = await httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{GT_ID}");

            // assert
            Assert.Equal("GT", response.Model);
        }

        [Fact]
        public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
        {
            // arrange? 

            // act
            var response = await httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAuctionById_WithInvalidGuid_ShouldReturn400()
        {
            // arrange? 

            // act
            var response = await httpClient.GetAsync($"api/auctions/notaguid");

            // assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateAuction_WithNoAuth_ShouldReturn401()
        {
            // arrange? 
            var auction = new CreateAuctionDto { Make = "test" };

            // act
            var response = await httpClient.PostAsJsonAsync($"api/auctions", auction);

            // assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateAuction_WithAuth_ShouldReturn201()
        {
            // arrange? 
            var auction = GetAuctionForCreate();
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

            // act
            var response = await httpClient.PostAsJsonAsync($"api/auctions", auction);

            // assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            // Here, response.Content mean response body.
            var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
            Assert.Equal("bob", createdAuction.Seller);
        }

        [Fact]
        public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
        {
            // arrange? 
            var auction = GetAuctionForCreate();
            auction.Make = null;
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

            // act
            var response = await httpClient.PostAsJsonAsync($"api/auctions", auction);

            // assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
        {
            // arrange? 
            var updateAuction = new UpdateAuctionDto { Make = "Updated" };
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

            // act
            var response = await httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", updateAuction);

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
        {
            // arrange? 
            var updateAuction = new UpdateAuctionDto { Make = "Updated" };
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notbob"));

            // act
            var response = await httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", updateAuction);

            // assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
