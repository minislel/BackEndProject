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

/// <summary>
/// Controller for managing Spotify listening history of the authenticated user.
/// Provides endpoints to retrieve details about individual playbacks, to create,
/// update, and delete playback records, as well as to manage songs and search functionality.
/// </summary>
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


        // GET: api/SpotifyHistory/playbacks/{id}
        /// <summary>
        /// Retrieves a specific song play record by its unique ID.
        /// </summary>
        /// <param name="id">The ID of the playback record.</param>
        /// <response code="200">Returns the wrapped DTO with playback data and HATEOAS links.</response>
        /// <response code="404">Playback record not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpGet("playbacks/{id:int}")]
        [ProducesResponseType(typeof(HateoasSongPlayWrapperDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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



        // PUT: api/SpotifyHistory/playbacks/{id}
        /// <summary>
        /// Updates an existing song play record.
        /// </summary>
        /// <param name="id">The ID of the playback record to update.</param>
        /// <param name="songPlay">DTO containing updated playback details.</param>
        /// <response code="200">Returns the updated wrapped DTO with HATEOAS links.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="404">Playback record not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpPut("playbacks/{id}")]
        [ProducesResponseType(typeof(HateoasSongPlayWrapperDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // POST: api/SpotifyHistory/playbacks
        /// <summary>
        /// Creates a new song play record for the authenticated user.
        /// </summary>
        /// <param name="songPlay">DTO containing playback details.</param>
        /// <response code="201">Returns the created wrapped DTO with HATEOAS links.</response>
        /// <response code="400">Invalid data or song not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpPost("playbacks")]
        [ProducesResponseType(typeof(HateoasSongPlayWrapperDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // DELETE: api/SpotifyHistory/playbacks/{id}
        /// <summary>
        /// Deletes a song play record by its ID.
        /// </summary>
        /// <param name="id">The ID of the playback record to delete.</param>
        /// <response code="204">Playback record deleted successfully.</response>
        /// <response code="404">Playback record not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpDelete("playbacks/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // GET: api/SpotifyHistory/playbacks/{URI}
        /// <summary>
        /// Retrieves all playbacks for a specified song URI.
        /// </summary>
        /// <param name="URI">Spotify URI of the song.</param>
        /// <response code="200">Returns list of playback records.</response>
        /// <response code="404">No playback records found.</response>
        [HttpGet("playbacks/{URI}")]
        [ProducesResponseType(typeof(IEnumerable<SongPlay>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        // GET: api/SpotifyHistory/songs/{URI}
        /// <summary>
        /// Retrieves details of a specific song by Spotify URI.
        /// </summary>
        /// <param name="URI">Spotify URI of the song.</param>
        /// <response code="200">Returns the wrapped DTO with song data and HATEOAS links.</response>
        /// <response code="404">Song not found.</response>
        [HttpGet("songs/{URI}")]
        [ProducesResponseType(typeof(HateoasSongWrapperDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        // GET: api/SpotifyHistory/songs/top10
        /// <summary>
        /// Retrieves the top 10 songs by play count.
        /// </summary>
        /// <response code="200">Returns list of top 10 songs with play count.</response>
        [HttpGet("songs/top10")]
        [ProducesResponseType(typeof(IEnumerable<SongRankElementDto>), StatusCodes.Status200OK)]
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
        // POST: api/SpotifyHistory/songs
        /// <summary>
        /// Creates a new song record.
        /// </summary>
        /// <param name="song">DTO with song metadata.</param>
        /// <response code="201">Returns the wrapped DTO with created song and HATEOAS links.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpPost("songs")]
        [ProducesResponseType(typeof(HateoasSongWrapperDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // PUT: api/SpotifyHistory/songs
        /// <summary>
        /// Updates metadata of an existing song.
        /// </summary>
        /// <param name="URI">URI of the song to update.</param>
        /// <param name="song">DTO with updated song details.</param>
        /// <response code="200">Returns the wrapped DTO with updated song and HATEOAS links.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="404">Song not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpPut("songs")]
        [ProducesResponseType(typeof(HateoasSongWrapperDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // DELETE: api/SpotifyHistory/songs/{URI}
        /// <summary>
        /// Deletes a song and its associated playback records.
        /// </summary>
        /// <param name="URI">URI of the song to delete.</param>
        /// <response code="200">Returns HATEOAS links for related actions.</response>
        /// <response code="404">Song not found.</response>
        /// <response code="403">Forbidden: user not authorized.</response>
        [HttpDelete("songs/{URI}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        // GET: api/SpotifyHistory/songs/search/{query}
        /// <summary>
        /// Searches for songs by query with optional filter and sort.
        /// </summary>
        /// <param name="query">Search term (track, artist, or album).</param>
        /// <param name="filter">Optional filter: "Artist", "Album", or "Track".</param>
        /// <param name="sort">Optional sort: "Artist", "Album", or "Track".</param>
        /// <response code="200">Returns list of matching songs.</response>
        /// <response code="400">Invalid query or filter.</response>
        /// <response code="404">No songs found.</response>
        [HttpGet("songs/search/{query}")]
        [ProducesResponseType(typeof(IEnumerable<Song>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <summary>
        /// Retrieves all song play records for the authenticated user.
        /// </summary>
        /// <returns>List of <see cref="SongPlay"/> entities belonging to the current user.</returns>
        

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
