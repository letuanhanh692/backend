using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;
using BEPrj3.Models.DTO;

namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceListsController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public PriceListsController(BusBookingContext context)
        {
            _context = context;
        }

        // GET: api/PriceLists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriceList>>> GetPriceLists()
        {
            return await _context.PriceLists.ToListAsync();
        }

        // GET: api/PriceLists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PriceList>> GetPriceList(int id)
        {
            var priceList = await _context.PriceLists.FindAsync(id);

            if (priceList == null)
            {
                return NotFound();
            }

            return priceList;
        }

        // PUT: api/PriceLists/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPriceList(int id, PriceList priceList)
        {
            if (id != priceList.Id)
            {
                return BadRequest();
            }

            _context.Entry(priceList).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceListExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/PriceLists
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PriceList>> PostPriceList([FromBody] PriceListDto priceListDto)
        {
            if (priceListDto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Kiểm tra Route có tồn tại không
            var route = await _context.Routes.FindAsync(priceListDto.RouteId);
            if (route == null)
            {
                return BadRequest(new { message = "RouteId không tồn tại." });
            }

            if (route.PriceRoute == null)
            {
                return BadRequest(new { message = "PriceRoute chưa được thiết lập cho tuyến đường này." });
            }

            // Tính hệ số giá dựa trên BusTypeId
            decimal multiplier = priceListDto.BusTypeId switch
            {
                1 => 1.0m,
                2 => 1.1m,
                3 => 1.2m,
                4 => 1.3m,
                _ => 1.0m // Mặc định nếu BusTypeId không hợp lệ
            };

            // Tạo PriceList mới
            var priceList = new PriceList
            {
                RouteId = priceListDto.RouteId,
                BusTypeId = priceListDto.BusTypeId,
                Price = (decimal)(route.PriceRoute * multiplier)
            };

            _context.PriceLists.Add(priceList);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
        }


        // DELETE: api/PriceLists/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePriceList(int id)
        {
            var priceList = await _context.PriceLists.FindAsync(id);
            if (priceList == null)
            {
                return NotFound();
            }

            _context.PriceLists.Remove(priceList);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PriceListExists(int id)
        {
            return _context.PriceLists.Any(e => e.Id == id);
        }
    }
}
