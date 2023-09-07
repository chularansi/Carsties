using AuctionService.Controllers;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AuctionService.UnitTests.Utils;
using AutoFixture;
using AutoMapper;
using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuctionService.UnitTests
{

    public class AuctionControllerTests
    {
        private readonly Mock<IAuctionRepository> auctionRepo;
        private readonly Mock<IPublishEndpoint> publishEndpoint;
        private readonly Fixture fixture;
        private readonly AuctionsController controller;
        private readonly IMapper mapper;

        public AuctionControllerTests()
        {
            fixture = new Fixture();
            auctionRepo = new Mock<IAuctionRepository>();
            publishEndpoint = new Mock<IPublishEndpoint>();

            var mockMapper = new MapperConfiguration(mc =>
            {
                mc.AddMaps(typeof(MappingProfiles).Assembly);
            }).CreateMapper().ConfigurationProvider;

            mapper = new Mapper(mockMapper);

            controller = new AuctionsController(auctionRepo.Object, mapper, publishEndpoint.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = Helpers.GetClaimsPrincipal() }
                }
            };
        }

        [Fact]
        public async Task GetAuctions_WithNoParams_Returns10Auctions()
        {
            // arrange
            var auctions = fixture.CreateMany<AuctionDto>(10).ToList();
            auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auctions);

            // act
            var result = await controller.GetAllAuctions(null);

            // assert
            Assert.Equal(10, result.Value.Count);
            Assert.IsType<ActionResult<List<AuctionDto>>>(result);
        }

        [Fact]
        public async Task GetAuctionById_WithValidGuid_ReturnsAuction()
        {
            // arrange
            var auction = fixture.Create<AuctionDto>();
            auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);

            // act
            var result = await controller.GetAuctionById(auction.Id);

            // assert
            Assert.Equal(auction.Make, result.Value.Make);
            Assert.IsType<ActionResult<AuctionDto>>(result);
        }

        [Fact]
        public async Task GetAuctionById_WithInvalidGuid_ReturnsNotFound()
        {
            // arrange
            auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            // act
            var result = await controller.GetAuctionById(Guid.NewGuid());

            // assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateAuction_WithValidCreateAuctionDto_ReturnsCreatedAtAction()
        {
            // arrange
            var auction = fixture.Create<CreateAuctionDto>();
            auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
            auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

            // act
            var result = await controller.CreateAuction(auction);
            var createdResult = result.Result as CreatedAtActionResult;

            // assert
            Assert.NotNull(createdResult);
            Assert.Equal("GetAuctionById", createdResult.ActionName);
            Assert.IsType<AuctionDto>(createdResult.Value);
        }

        [Fact]
        public async Task CreateAuction_FailedSave_Returns400BadRequest()
        {
            // arrange
            var auction = fixture.Create<CreateAuctionDto>();
            auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
            auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

            // act
            var result = await controller.CreateAuction(auction);

            // assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test";
            var updateAuctionDto = fixture.Create<UpdateAuctionDto>();

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(auction);
            auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);


            // act
            var result = await controller.UpdateAuction(auction.Id, updateAuctionDto);

            //assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test2";
            var updateAuctionDto = fixture.Create<UpdateAuctionDto>();

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(auction);

            // act
            var result = await controller.UpdateAuction(auction.Id, updateAuctionDto);

            //assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test";
            var updateAuctionDto = fixture.Create<UpdateAuctionDto>();

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            // act
            var result = await controller.UpdateAuction(auction.Id, updateAuctionDto);

            //assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test";

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(auction);
            auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

            // act
            var result = await controller.DeleteAuction(auction.Id);

            //assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test";

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            // act
            var result = await controller.DeleteAuction(auction.Id);

            //assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteAuction_WithInvalidUser_Returns403Response()
        {
            // arrange
            var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
            auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
            auction.Seller = "test1";

            auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
                .ReturnsAsync(auction);

            // act
            var result = await controller.DeleteAuction(auction.Id);

            //assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
