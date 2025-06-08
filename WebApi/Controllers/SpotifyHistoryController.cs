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
using System.Security.Policy;
using Microsoft.IdentityModel.Tokens;

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
        [HttpGet("playbacks/{id:int}")]
        public async Task<ActionResult<HateoasSongPlayWrapperDto>> GetSongPlay(int id)
        {
            var songPlay = await _context.SongPlays.FindAsync(id);
            if (songPlay == null)
            {
                return NotFound();
            }
            songPlay.Song = await _context.Songs
                .Where(s => s.URI == songPlay.URI)
                .FirstOrDefaultAsync();
            var result = CreateHateoasForSongPlay(songPlay);

            return Ok(result);
        }



        // PUT: api/SpotifyHistory/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("playbacks/{id}")]
        public async Task<ActionResult<HateoasSongPlayWrapperDto>> PutSongPlay(int id, SongPlayPutDto songPlay)
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
            if( await _context.Songs.FindAsync(songPlay.URI) is null)
            {
                return BadRequest("Invalid Song URI");
            }


            existingSongPlay.Id = songPlay.Id;
            existingSongPlay.URI = songPlay.URI;
            existingSongPlay.PlayTime = songPlay.PlayTime;
            existingSongPlay.Platform = songPlay.Platform;
            existingSongPlay.MsPlayed = songPlay.MsPlayed;

            //existingSongPlay.Song = _context.Songs.Find(songPlay.URI);

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
            
            var result = CreateHateoasForSongPlay(existingSongPlay);
            return Ok(result);
        }

        // POST: api/SpotifyHistory
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("playbacks")]
        public async Task<ActionResult<HateoasSongPlayWrapperDto>> PostSongPlay(SongPlayPostDto songPlay)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            var songPlayEntity = new SongPlay
            {
                URI = songPlay.URI,
                PlayTime = songPlay.PlayTime,
                Platform = songPlay.Platform,
                MsPlayed = songPlay.MsPlayed,
                ReasonStart = songPlay.ReasonStart,
                ReasonEnd = songPlay.ReasonEnd,
                Shuffle = songPlay.Shuffle,
                Skip = songPlay.Skip
            };

            var existingSong = await _context.Songs.FindAsync(songPlay.URI);
            if (existingSong == null)
            {
                return BadRequest("The song with the specified URI does not exist.");
            }
            _context.SongPlays.Add(songPlayEntity);


            await _context.SaveChangesAsync();
            var result = CreateHateoasForSongPlay(songPlayEntity);
            return Ok(result);
        }

        // DELETE: api/SpotifyHistory/5
        [HttpDelete("playbacks/{id}")]
        public async Task<ActionResult<List<HateoasLinkDto>>> DeleteSongPlay(int id)
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
            var links = new List<HateoasLinkDto>
                {
                    new HateoasLinkDto(Url.Action("GetSongPlay", new { id }), "get_songplay", "GET"),
                    new HateoasLinkDto(Url.Action("PutSongPlay", new { id }), "update_songplay", "PUT"),
                    new HateoasLinkDto(Url.Action("DeleteSongPlay", new { id }), "delete_songplay", "DELETE"),
                    new HateoasLinkDto(Url.Action("PostSongPlay"), "post_songplay", "POST")
                };
            return NoContent();
        }
        [HttpGet("playbacks/{URI}")]
        public async Task<ActionResult<IEnumerable<SongPlay>>> GetSongPlaysByURI(string URI)
        {
            var songPlays = await _context.SongPlays
                .Where(sp => sp.URI == URI)
                .Include(sp => sp.Song)
                .ToListAsync();
            if (songPlays == null || !songPlays.Any())
            {
                return NotFound();
            }
            return songPlays;
        }


        private bool SongPlayExists(int id)
        {
            return _context.SongPlays.Any(e => e.Id == id);
        }
        #endregion
        #region Song methods
        [HttpGet("songs/{URI}")]
        public async Task<ActionResult<HateoasSongWrapperDto>> GetSong(string URI)
        {
            var song = await _context.Songs.FindAsync(URI);
            if (song == null)
            {
                return NotFound();
            }
            var result = CreateHateoasForSong(song);
            return Ok(result);
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
        public async Task<ActionResult<HateoasSongWrapperDto>> PostSong(SongPostDto song)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            if(song == null || string.IsNullOrEmpty(song.URI) || string.IsNullOrEmpty(song.TrackName) || string.IsNullOrEmpty(song.ArtistName) || string.IsNullOrEmpty(song.AlbumName))
            {
                return BadRequest("Song data is incomplete.");
            }
            Song newSong = new Song
            {
                URI = song.URI,
                TrackName = song.TrackName,
                ArtistName = song.ArtistName,
                AlbumName=song.AlbumName
            };
            _context.Songs.Add(newSong);
            await _context.SaveChangesAsync();
            var result = CreateHateoasForSong(newSong);
            return Ok(result);
        }
        [HttpPut("songs")]
        public async Task<ActionResult<HateoasSongWrapperDto>> PutSong(string URI, SongPutDto song)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            if (song == null || string.IsNullOrEmpty(URI) || string.IsNullOrEmpty(song.TrackName) || string.IsNullOrEmpty(song.ArtistName) || string.IsNullOrEmpty(song.AlbumName))
            {
                return BadRequest("Song data is incomplete.");
            }
            var existingSong = _context.Songs.Find(URI);
            if (existingSong == null)
            {
                return NotFound("Song not found.");
            }
            existingSong.TrackName = song.TrackName;
            existingSong.ArtistName = song.ArtistName;
            existingSong.AlbumName = song.AlbumName;
            
            await _context.SaveChangesAsync();
            var result = CreateHateoasForSong(existingSong);
            return Ok(result);
        }
        [HttpDelete("songs/{URI}")]
        public async Task<ActionResult<List<HateoasLinkDto>>> DeleteSong(string URI)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Forbid();
            }
            var song = await _context.Songs.FindAsync(URI);
            if (song == null)
            {
                return NotFound();
            }
            var songPlays = _context.SongPlays.Where(sp => sp.URI == URI);
            if (songPlays.Any())
            {
                _context.SongPlays.RemoveRange(songPlays);
            }

            _context.Songs.Remove(song);

            await _context.SaveChangesAsync();
            var result = new List<HateoasLinkDto>
            {
                new HateoasLinkDto(Url.Action("GetSong", new { URI }), "get_song", "GET"),
                new HateoasLinkDto(Url.Action("PostSong"), "post_song", "POST"),
                new HateoasLinkDto(Url.Action("DeleteSong", new { URI }), "delete_song", "DELETE"),
                new HateoasLinkDto(Url.Action("PutSong", new { URI }), "put_song", "PUT")
            };
            
            return Ok(result);


        }
        [HttpGet("songs/search/{query}")]
        public async Task<ActionResult<ICollection<Song>>> SearchSong(string query, string? filter, string? sort)
        {
            if (query.IsNullOrEmpty() || string.IsNullOrWhiteSpace(query))
                return BadRequest();
            var normalizedQuery = query.ToLower();
            List<Song> songs = new List<Song>();
            if (string.IsNullOrEmpty(filter))
            {
                songs = await _context.Songs
                   .Where(s => s.TrackName.ToLower().Contains(normalizedQuery) ||
                             s.AlbumName.ToLower().Contains(normalizedQuery) ||
                             s.ArtistName.ToLower().Contains(normalizedQuery)
                       )
                   .ToListAsync();
            }
            else if (filter == "Artist")
            {
                songs = await _context.Songs
                    .Where(s => s.ArtistName.ToLower().Contains(normalizedQuery)
                    )
                    .ToListAsync();
            }
            else if (filter == "Album")
            {
                songs = await _context.Songs
                            .Where(s => s.AlbumName.ToLower().Contains(normalizedQuery))
                            .ToListAsync();
            }
            else if (filter == "Track")
            {
                songs = await _context.Songs
            .Where(s => s.TrackName.ToLower().Contains(normalizedQuery))
            .ToListAsync();
            }
            else
            {
                return BadRequest();
            }
            if (songs == null || !songs.Any())
            {
                return NotFound();
            }
            if (sort == "Album")
                songs = songs.OrderBy(s => s.AlbumName).ToList();
            else if (sort == "Artist")
                songs = songs.OrderBy(s => s.ArtistName).ToList();
            else if (sort == "Track")
                songs = songs.OrderBy(s => s.TrackName).ToList();

            return songs;
        }


        #endregion

        private UserEntity? GetCurrentUser()
        {
            var user = HttpContext.User.Identity as ClaimsIdentity;
            if (user != null)
            {
                string username = user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                { return null; }

                var userEntity = _userManager.FindByNameAsync(username).Result;

                return userEntity;
            }
            return null;
        }
        private HateoasSongPlayWrapperDto CreateHateoasForSongPlay(SongPlay songPlay)
        {
            var id = songPlay.Id;
            var dto = new HateoasSongPlayWrapperDto
            {
                SongPlay = songPlay,
                Links = new List<HateoasLinkDto>
                {
                    new HateoasLinkDto(Url.Action("GetSongPlay", new { id }), "get_songplay", "GET"),
                    new HateoasLinkDto(Url.Action("PutSongPlay", new { id }), "update_songplay", "PUT"),
                    new HateoasLinkDto(Url.Action("DeleteSongPlay", new { id }), "delete_songplay", "DELETE"),
                    new HateoasLinkDto(Url.Action("PostSongPlay"), "post_songplay", "POST")
                }
            };
            return dto;
        }
        private HateoasSongWrapperDto CreateHateoasForSong(Song song)
        {
            var dto = new HateoasSongWrapperDto
            {
                Song = song,
                Links = new List<HateoasLinkDto>
                {
                    new HateoasLinkDto(Url.Action("GetSong", new { URI = song.URI }), "get_song", "GET"),
                    new HateoasLinkDto(Url.Action("PostSong"), "post_song", "POST"),
                    new HateoasLinkDto(Url.Action("DeleteSong", new { URI = song.URI }), "delete_song", "DELETE"),
                    new HateoasLinkDto(Url.Action("PutSong", new { URI = song.URI }), "put_song", "PUT")
                }
            };
            return dto;
        }
    }
}
