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
    public class BusTypesController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public BusTypesController(BusBookingContext context)
        {
            _context = context;
        }

        // GET: api/BusTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusType>>> GetBusTypes()
        {
            return await _context.BusTypes.ToListAsync();
        }

        // GET: api/BusTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BusType>> GetBusType(int id)
        {
            var busType = await _context.BusTypes.FindAsync(id);

            if (busType == null)
            {
                return NotFound();
            }

            return busType;
        }

        // PUT: api/BusTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBusType(int id, BusType busType)
        {
            if (id != busType.Id)
            {
                return BadRequest();
            }

            _context.Entry(busType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusTypeExists(id))
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

        // POST: api/BusTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // POST: api/BusTypes
        [HttpPost]
        public async Task<ActionResult<BusType>> PostBusType(BusTypeDTO busTypeDTO)
        {
            // Tạo đối tượng BusType từ DTO
            var busType = new BusType
            {
                TypeName = busTypeDTO.TypeName,
                Description = busTypeDTO.Description
            };

            // Thêm đối tượng BusType vào context và lưu vào database
            _context.BusTypes.Add(busType);
            await _context.SaveChangesAsync();

            // Trả về thông tin đối tượng busType đã được thêm vào
            return CreatedAtAction("GetBusType", new { id = busType.Id }, new { busType.TypeName, busType.Description });
        }


        // DELETE: api/BusTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusType(int id)
        {
            var busType = await _context.BusTypes.FindAsync(id);
            if (busType == null)
            {
                return NotFound();
            }

            _context.BusTypes.Remove(busType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BusTypeExists(int id)
        {
            return _context.BusTypes.Any(e => e.Id == id);
        }
    }
}
