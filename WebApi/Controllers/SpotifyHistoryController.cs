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
    //[Authorize("Bearer")]
    public class SpotifyHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UserEntity> _userManager;

        public SpotifyHistoryController(AppDbContext context, UserManager<UserEntity> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        #region Playback methods

        // GET: api/SpotifyHistory/5
        [HttpGet("playbacks/{id}")]
        public async Task<ActionResult<SongPlay>> GetSongPlay(int id)
        {
            var songPlay = await _context.SongPlays.FindAsync(id);
            songPlay.Song = await _context.Songs
                .Where(s => s.URI == songPlay.URI)
                .FirstOrDefaultAsync();
            if (songPlay == null)
            {
                return NotFound();
            }
            return songPlay;
        }



        // PUT: api/SpotifyHistory/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("playbacks/{id}")]
        public async Task<IActionResult> PutSongPlay(int id, SongPlayPutPostDto songPlay)
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

            var existingSongPlay = await _context.SongPlays.FindAsync(id);
            if (existingSongPlay == null)
            {
                return NotFound();
            }
            if (existingSongPlay.URI != songPlay.URI)
            {
                var oldSong = _context.Songs.Find(existingSongPlay.URI);
                var newSong = _context.Songs.Find(songPlay.URI);
            }

            existingSongPlay.Id = songPlay.Id;
            existingSongPlay.URI = songPlay.URI;
            existingSongPlay.PlayTime = songPlay.PlayTime;
            existingSongPlay.Platform = songPlay.Platform;
            existingSongPlay.MsPlayed = songPlay.MsPlayed;
            
            existingSongPlay.Song = _context.Songs.Find(songPlay.URI);
            existingSongPlay.ReasonStart = songPlay.ReasonStart;
            existingSongPlay.ReasonEnd = songPlay.ReasonEnd;
            existingSongPlay.Shuffle = songPlay.Shuffle;
            existingSongPlay.Skip = songPlay.Skip;
            
            
            //_context.Entry(existingSongPlay).State = EntityState.Modified;
            
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
        [HttpPost("playbacks")]
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
        [HttpDelete("playbacks/{id}")]
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
        #endregion
        #region Song methods
        [HttpGet("songs/{URI}")]
        public async Task<ActionResult<Song>> GetSong(string URI)
        {
            var song = await _context.Songs.FindAsync(URI);
            if (song == null)
            {
                return NotFound();
            }
            return song;
        }
        [HttpGet("songs/top10")]
        public async Task<ActionResult<IEnumerable<SongRankElementDto>>> GetTop10Songs()
        {
            var topSongs = await _context.SongPlays
                .Include(sp => sp.Song)
                .GroupBy(sp => sp.URI)
                .Select(g => new
                {
                    Song = g.First().Song,
                    PlayCount = g.Count()
                })
                .OrderByDescending(s => s.PlayCount)
                .Take(10)
                .ToListAsync();
            return Ok(topSongs);
        }
        [HttpPost("songs")]
        public async Task<ActionResult<Song>> PostSong(Song song)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetSong", new { Uri = song.URI }, song);
        }
        #endregion

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
