using System;
using System.Collections.Generic;
using System.IO;
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
    public class BusesController : ControllerBase
    {
        private readonly BusBookingContext _context;
        private readonly string _imagePath = "wwwroot/images/buses/";

        public BusesController(BusBookingContext context)
        {
            _context = context;
        }

        // 📌 GET: Lấy danh sách tất cả xe bus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bus>>> GetBuses(int page = 1, int pageSize = 4)
        {
            // Nếu page và pageSize bằng 0, lấy tất cả dữ liệu
            if (page == 0 && pageSize == 0)
            {
                var allBuses = await _context.Buses.ToListAsync();
                return Ok(new
                {
                    Buses = allBuses,
                    TotalPages = 1,  // Vì đã lấy tất cả nên chỉ có một trang
                    CurrentPage = 1
                });
            }

            // Lấy tổng số bản ghi
            var totalCount = await _context.Buses.CountAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Lấy danh sách xe bus trong phạm vi phân trang
            var buses = await _context.Buses
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Trả về kết quả cùng với thông tin tổng số trang
            return Ok(new
            {
                Buses = buses,
                TotalPages = totalPages,
                CurrentPage = page
            });
        }



        // 📌 GET: Lấy thông tin chi tiết 1 xe bus
        [HttpGet("{id}")]
        public async Task<ActionResult<Bus>> GetBus(int id)
        {
            var bus = await _context.Buses
                                    .Include(b => b.BusType)  // Bao gồm thông tin BusType
                                    .FirstOrDefaultAsync(b => b.Id == id);

            if (bus == null)
            {
                return NotFound();
            }

            return bus;
        }


        // 📌 POST: Thêm mới 1 xe bus + Upload ảnh
        [HttpPost]
        public async Task<ActionResult<Bus>> PostBus([FromForm] BusDto busDto)
        {
            string fileName = null;

            // Xử lý ảnh nếu có upload
            if (busDto.File != null)
            {
                fileName = await SaveImage(busDto.File);
            }

            var bus = new Bus
            {
                BusNumber = busDto.BusNumber,
                BusTypeId = busDto.BusTypeId,
                TotalSeats = busDto.TotalSeats,
                ImageBus = fileName
            };


            _context.Buses.Add(bus);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBus), new { id = bus.Id }, bus);
        }

        // 📌 PUT: Cập nhật thông tin xe bus
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBus(int id, [FromForm] BusDto busDto)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin
            bus.BusNumber = busDto.BusNumber;
            bus.BusTypeId = busDto.BusTypeId;
            bus.TotalSeats = busDto.TotalSeats;

            // Nếu có upload ảnh mới, thay thế ảnh cũ
            if (busDto.File != null)
            {
                if (!string.IsNullOrEmpty(bus.ImageBus))
                {
                    DeleteImage(bus.ImageBus);  // Xóa ảnh cũ
                }

                bus.ImageBus = await SaveImage(busDto.File);
            }

            _context.Entry(bus).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusExists(id))
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

        // 📌 DELETE: Xóa xe bus + Ảnh liên quan
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(bus.ImageBus))
            {
                DeleteImage(bus.ImageBus);
            }

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 📌 Kiểm tra xe bus có tồn tại không
        private bool BusExists(int id)
        {
            return _context.Buses.Any(e => e.Id == id);
        }

        // 📌 Lưu ảnh vào thư mục wwwroot/images/buses/
        private async Task<string> SaveImage(IFormFile file)
        {
            if (!Directory.Exists(_imagePath))
            {
                Directory.CreateDirectory(_imagePath);
            }

            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(_imagePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{Request.Scheme}://{Request.Host}/images/buses/" + fileName;
        }

        // 📌 Xóa ảnh khỏi thư mục
        private void DeleteImage(string fileName)
        {
            string filePath = Path.Combine(_imagePath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Bus>>> SearchBuses(string searchQuery, int page = 1, int pageSize = 5)
        {
            var query = _context.Buses.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(b => b.BusNumber.Contains(searchQuery) ||  b.TotalSeats.ToString().Contains(searchQuery)); // Tìm theo số xe hoặc tên loại xe
            }

            var totalCount = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var buses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Buses = buses,
                TotalPages = totalPages,
                CurrentPage = page
            });
        }

    }
}
