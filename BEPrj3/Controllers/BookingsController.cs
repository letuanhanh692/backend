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
    public class BookingsController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public BookingsController(BusBookingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetBookings(int page = 1, int pageSize = 4)
        {
            // Nếu page và pageSize bằng 0, lấy tất cả các bản ghi
            if (page == 0 && pageSize == 0)
            {
                var bookings = await _context.Bookings
                      .OrderByDescending(b => b.Id)
                       .Skip((page - 1) * pageSize)  
                        .Take(pageSize)               
                    .ToListAsync();

                var bookingResponses = bookings.Select(b => new BookingResponseDto
                {
                    BookingId = b.Id, // Đổi tên trường từ Id thành BookingId
                    UserId = b.UserId,
                    ScheduleId = b.ScheduleId,
                    Name = b.Name,
                    Age = b.Age,
                    Phone = b.User?.Phone ?? "Unknown",
                    Email = b.User?.Email ?? "Unknown",
                    SeatNumber = b.SeatNumber,
                    BookingDate = b.BookingDate ?? DateTime.MinValue,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status,

                    // Thông tin chuyến đi
                    BusNumber = b.Schedule?.Bus?.BusNumber ?? "N/A",
                    BusType = b.Schedule?.Bus?.BusType?.TypeName ?? "N/A",
                    DepartTime = b.Schedule?.DepartureTime ?? DateTime.MinValue,
                    ArrivalTime = b.Schedule?.ArrivalTime ?? DateTime.MinValue,
                    StartingPlace = b.Schedule?.Route?.StartingPlace ?? "N/A",
                    DestinationPlace = b.Schedule?.Route?.DestinationPlace ?? "N/A",
                    Distance = (double)(b.Schedule?.Route?.Distance ?? 0)
                }).ToList();

                return Ok(new
                {
                    TotalRecords = bookingResponses.Count,
                    TotalPages = 1, // Không có phân trang, chỉ có một trang
                    CurrentPage = 1,
                    PageSize = bookingResponses.Count,
                    Bookings = bookingResponses
                });
            }

            // Nếu pageSize khác 0 và page khác 0, thực hiện phân trang bình thường
            var totalRecords = await _context.Bookings.CountAsync(); // Tổng số bản ghi
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Tính tổng số trang

            var bookingsPaged = await _context.Bookings
                .OrderByDescending(b => b.Id) // Sắp xếp giảm dần để bản ghi mới nhất lên đầu
                 .Skip((page - 1) * pageSize)  // Bỏ qua các bản ghi trước trang hiện tại
                 .Take(pageSize)               // Lấy số bản ghi theo kích thước trang
                  .ToListAsync();

            var bookingResponsesPaged = bookingsPaged.Select(b => new BookingResponseDto
            {
                BookingId = b.Id,
                UserId = b.UserId,
                ScheduleId = b.ScheduleId,
                Name = b.Name,
                Age = b.Age,
                Phone = b.User?.Phone ?? "Unknown",
                Email = b.User?.Email ?? "Unknown",
                SeatNumber = b.SeatNumber,
                BookingDate = b.BookingDate ?? DateTime.MinValue,
                TotalAmount = b.TotalAmount,
                Status = b.Status,

                BusNumber = b.Schedule?.Bus?.BusNumber ?? "N/A",
                BusType = b.Schedule?.Bus?.BusType?.TypeName ?? "N/A",
                DepartTime = b.Schedule?.DepartureTime ?? DateTime.MinValue,
                ArrivalTime = b.Schedule?.ArrivalTime ?? DateTime.MinValue,
                StartingPlace = b.Schedule?.Route?.StartingPlace ?? "N/A",
                DestinationPlace = b.Schedule?.Route?.DestinationPlace ?? "N/A",
                Distance = (double)(b.Schedule?.Route?.Distance ?? 0)
            }).ToList();

            var response = new
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Bookings = bookingResponsesPaged
            };

            return Ok(response);
        }



        // GET: api/Bookings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User) // Bổ sung User để tránh lỗi null
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                    .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Kiểm tra null trước khi truy cập thuộc tính
            var bookingResponse = new BookingResponseDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId, // Thêm dòng này
                ScheduleId = booking.ScheduleId, // Thêm dòng này
                Name = booking.Name ?? "Unknown",
                Age = booking.Age,
                Phone = booking.User?.Phone ?? "Unknown",
                Email = booking.User?.Email ?? "Unknown",
                SeatNumber = booking.SeatNumber,
                BookingDate = booking.BookingDate ?? DateTime.MinValue,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,

                // Thông tin chuyến đi
                BusNumber = booking.Schedule?.Bus?.BusNumber ?? "N/A",
                BusType = booking.Schedule?.Bus?.BusType?.TypeName ?? "N/A",
                DepartTime = booking.Schedule?.DepartureTime ?? DateTime.MinValue,
                ArrivalTime = booking.Schedule?.ArrivalTime ?? DateTime.MinValue,
                StartingPlace = booking.Schedule?.Route?.StartingPlace ?? "N/A",
                DestinationPlace = booking.Schedule?.Route?.DestinationPlace ?? "N/A",
                Distance = (double)(booking.Schedule?.Route?.Distance ?? 0)
            };


            return Ok(bookingResponse);
        }

        // PUT: api/Bookings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return BadRequest();
            }

            _context.Entry(booking).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
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

        // POST: api/Bookings
        [HttpPost]
        public async Task<ActionResult<BookingResponseDto>> PostBooking(BookingRequestDto bookingRequestDto)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(s => s.Id == bookingRequestDto.ScheduleId);

            if (schedule == null)
            {
                return BadRequest("Chuyến đi không tồn tại.");
            }

            int bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == bookingRequestDto.ScheduleId)
                .SumAsync(b => b.SeatNumber);

            int availableSeats = schedule.Bus.TotalSeats - bookedSeats;

            if (availableSeats < bookingRequestDto.SeatNumber)
            {
                return BadRequest("Không đủ ghế để đặt.");
            }

            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.RouteId == schedule.RouteId && pl.BusTypeId == schedule.Bus.BusTypeId);

            if (priceList == null)
            {
                return BadRequest("Không tìm thấy giá vé cho chuyến đi này.");
            }
            if (schedule.DepartureTime < DateTime.Now)
            {
                return BadRequest("Cannot book a trip that has already passed.");
            }

            decimal pricePerSeat = priceList.Price;
            if (bookingRequestDto.Age < 5) pricePerSeat = 0;
            else if (bookingRequestDto.Age >= 5 && bookingRequestDto.Age <= 12) pricePerSeat *= 0.5M;
            else if (bookingRequestDto.Age > 50) pricePerSeat *= 0.3M;

            decimal totalAmount = pricePerSeat * bookingRequestDto.SeatNumber;

            var booking = new Booking
            {
                UserId = bookingRequestDto.UserId,
                ScheduleId = bookingRequestDto.ScheduleId,
                SeatNumber = bookingRequestDto.SeatNumber,
                Name = bookingRequestDto.Name,
                Age = bookingRequestDto.Age,
                BookingDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Booked"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Cập nhật số ghế còn lại trong lịch trình
            schedule.AvailableSeats -= bookingRequestDto.SeatNumber;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            var bookingResponse = new BookingResponseDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                ScheduleId = booking.ScheduleId,
                SeatNumber = booking.SeatNumber,
                Age = booking.Age,
                BookingDate = (DateTime)booking.BookingDate,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                Name = booking.Name,

                Phone = bookingRequestDto.Phone,
                Email = bookingRequestDto.Email,

                BusNumber = schedule.Bus.BusNumber,
                BusType = schedule.Bus.BusType.TypeName,
                DepartTime = schedule.DepartureTime,
                ArrivalTime = schedule.ArrivalTime,
                StartingPlace = schedule.Route.StartingPlace,
                DestinationPlace = schedule.Route.DestinationPlace,
                Distance = (double)schedule.Route.Distance
            };

            return CreatedAtAction("GetBooking", new { id = booking.Id }, bookingResponse);
        }

        // DELETE: api/Bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            // Cập nhật lại số ghế còn lại khi hủy vé
            var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == booking.ScheduleId);
            if (schedule != null)
            {
                schedule.AvailableSeats += booking.SeatNumber;
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // GET: api/Bookings/Search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BookingResponseDto>>> SearchBookings(string searchQuery, int pageNumber = 1, int pageSize = 4)
        {
            var bookingsQuery = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                    .ThenInclude(b => b.BusType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Name.Contains(searchQuery) ||  // Tìm theo tên người dùng
                    b.SeatNumber.ToString().Contains(searchQuery) ||  // Tìm theo số ghế đặt
                    b.TotalAmount.ToString().Contains(searchQuery) || // Tìm theo số tiền thanh toán
                    b.Status.Contains(searchQuery)  // Tìm theo trạng thái
                );
            }

            // Phân trang
            var totalRecords = await bookingsQuery.CountAsync();  // Số lượng tổng các bản ghi
            var bookings = await bookingsQuery
                .Skip((pageNumber - 1) * pageSize)  // Bỏ qua số lượng bản ghi trước đó
                .Take(pageSize)  // Lấy số bản ghi theo pageSize
                .ToListAsync();

            var bookingResponses = bookings.Select(b => new BookingResponseDto
            {
                BookingId = b.Id,
                UserId = b.UserId,
                ScheduleId = b.ScheduleId,
                SeatNumber = b.SeatNumber,
                Age = b.Age,
                BookingDate = (DateTime)b.BookingDate,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                Name = b.Name,
                Phone = b.User.Phone,
                Email = b.User.Email,
                BusNumber = b.Schedule.Bus.BusNumber,
                BusType = b.Schedule.Bus.BusType.TypeName,
                DepartTime = b.Schedule.DepartureTime,
                ArrivalTime = b.Schedule.ArrivalTime,
                StartingPlace = b.Schedule.Route.StartingPlace,
                DestinationPlace = b.Schedule.Route.DestinationPlace,
                Distance = (double)b.Schedule.Route.Distance
            }).ToList();

            var paginationResult = new
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageNumber,
                PageSize = pageSize,
                Bookings = bookingResponses
            };

            return Ok(paginationResult);
        }


    }
}

   