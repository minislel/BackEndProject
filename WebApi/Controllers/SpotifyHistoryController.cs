using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationCore.Models;
using Infrastructure.EF;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore.Query.Internal;
using WebApi.Dto;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    public class SpotifyHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UserEntity> _userManager;

        public SpotifyHistoryController(AppDbContext context, UserManager<UserEntity> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/SpotifyHistory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SongPlay>> GetSongPlay(int id)
        {
            var songPlay = await _context.SongPlays.FindAsync(id);
            songPlay.Song = await _context.Songs
                .Where(s => s.Id == songPlay.SongId)
                .FirstOrDefaultAsync();
            if (songPlay == null)
            {
                return NotFound();
            }
            return songPlay;
        }
        [HttpGet("top10")]
        public async Task<ActionResult<IEnumerable<SongRankElementDto>>> GetTop10Songs()
        {
            var topSongs = await _context.SongPlays
                .Include(sp => sp.Song)
                .GroupBy(sp => sp.SongId)
                .Select(g => new
                {
                    Song = g.First().Song,
                    PlayCount = g.Count()
                })
                .OrderByDescending(s => s.PlayCount)
                .Take(10)
                .Select(s => new SongRankElementDto
                {
                    TrackName = s.Song.TrackName,
                    ArtistName = s.Song.ArtistName,
                    AlbumName = s.Song.AlbumName,
                    PlayCount = s.PlayCount  
                })
                .ToListAsync();

            return Ok(topSongs);
        }


        // PUT: api/SpotifyHistory/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSongPlay(int id, SongPlay songPlay)
        {
            if (id != songPlay.Id)
            {
                return BadRequest();
            }
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }

            _context.Entry(songPlay).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SongPlayExists(id))
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

        // POST: api/SpotifyHistory
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SongPlay>> PostSongPlay(SongPlay songPlay)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            _context.SongPlays.Add(songPlay);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSongPlay", new { id = songPlay.Id }, songPlay);
        }

        // DELETE: api/SpotifyHistory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSongPlay(int id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            var songPlay = await _context.SongPlays.FindAsync(id);
            if (songPlay == null)
            {
                return NotFound();
            }

            _context.SongPlays.Remove(songPlay);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SongPlayExists(int id)
        {
            return _context.SongPlays.Any(e => e.Id == id);
        }

        private UserEntity? GetCurrentUser()
        {
            var user = HttpContext.User.Identity as ClaimsIdentity;
            if (user != null)
            {
                string username = user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
                return _userManager.FindByNameAsync(username).Result;
            }
            return null;
        }
    }
}
