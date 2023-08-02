using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext context;
        private readonly IMapper mapper;

        public AuctionsController(AuctionDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
        {
            var auctions = await context.Auctions
                .Include(x => x.Item)
                .OrderBy(x => x.Item.Make)
                .ToListAsync();

            return mapper.Map<List<AuctionDto>>(auctions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            return mapper.Map<AuctionDto>(auction);
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var auction = mapper.Map<Auction>(auctionDto);

            auction.Seller = "test";

            context.Auctions.Add(auction);

            var result = await context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Could not save changes to the DB");

            return CreatedAtAction(nameof(GetAuctionById),
                new { auction.Id }, mapper.Map<AuctionDto>(auction));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            //var auction = await _repo.GetAuctionEntityById(id);

            var auction = await context.Auctions.Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            //if (auction.Seller != User.Identity.Name) return Forbid();

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            //await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

            //var result = await _repo.SaveChangesAsync();
            var result = await context.SaveChangesAsync() > 0;

            if (result) return Ok();

            return BadRequest("Problem saving changes");
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            //var auction = await _repo.GetAuctionEntityById(id);

            var auction = await context.Auctions.FindAsync(id);

            if (auction == null) return NotFound();

            //if (auction.Seller != User.Identity.Name) return Forbid();

            //_repo.RemoveAuction(auction);

            //await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

            //var result = await _repo.SaveChangesAsync();

            context.Auctions.Remove(auction);
            var result = await context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Could not update DB");

            return Ok();
        }
    }
}
