﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;

namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public RoutesController(BusBookingContext context)
        {
            _context = context;
        }

        // GET: api/Routes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Route>>> GetRoutes()
        {
            return await _context.Routes.ToListAsync();
        }

        // GET: api/Routes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Models.Route>> GetRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);

            if (route == null)
            {
                return NotFound();
            }

            return route;
        }

        // PUT: api/Routes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoute(int id, Models.Route route)
        {
            if (id != route.Id)
            {
                return BadRequest();
            }

            _context.Entry(route).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RouteExists(id))
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

        // POST: api/Routes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Models.Route>> PostRoute(Models.Route route)
        {
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRoute", new { id = route.Id }, route);
        }

        // DELETE: api/Routes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound();
            }

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RouteExists(int id)
        {
            return _context.Routes.Any(e => e.Id == id);
        }

    }
}
