using ApplicationCore.Models;
using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;

namespace Infrastructure.Data;

public class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.SongPlays.AnyAsync())
            return;

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "spotify.csv");
        if (!File.Exists(path))
        {
            Console.WriteLine($"[Seeder] File not found: {path}");
            Console.WriteLine("[Seeder] Please ensure the file exists in the Data folder.");
            return;
        }

        var lines = await File.ReadAllLinesAsync(path);
        var songPlays = new List<SongPlay>();
        var songs = new Dictionary<int, Song>();
        foreach (var line in lines.Skip(1))
        {
            bool inQuotes = false;
            string newLine = line;
            for (int i = 0; i < newLine.Length; i++)
            {
                if (newLine[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                if (newLine[i] == ',' && inQuotes)
                {
                    // Replace comma inside quotes with a placeholder
                    newLine = newLine.Remove(i, 1).Insert(i, "，︀");

                }

            }


            var columns = newLine.Split(',');
            var songid = (columns[4], columns[5], columns[6]).GetHashCode();

            if (!songs.TryGetValue(songid, out Song song))
            {
                song = new Song()
                {
                    Id = songid,
                    TrackName = columns[4],
                    ArtistName = columns[5],
                    AlbumName = columns[6],
                };
                songs[songid] = song;
            }

                
                var songPlay = new SongPlay
                {
                    URI = columns[0],
                    PlayTime = DateTime.ParseExact(columns[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    Platform = columns[2],
                    MsPlayed = int.Parse(columns[3]),
                    SongId = song.Id,
                    Song = song,
                    ReasonStart = columns[7],
                    ReasonEnd = columns[8],
                    Shuffle = bool.Parse(columns[9]),
                    Skip = bool.Parse(columns[10])
                };

                songPlays.Add(songPlay);
            }
            context.ChangeTracker.Clear();
            context.SongPlays.AddRange(songPlays);
            context.Songs.AddRange(songs.Values);
            context.SaveChanges();
        }
    }

