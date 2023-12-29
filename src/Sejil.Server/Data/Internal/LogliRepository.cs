using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sejil.Models.Internal;

namespace Sejil.Data.Internal;

public class LogliRepository : ISejilRepository
{
    public async Task<bool> SaveQueryAsync(LogQuery logQuery)
    {
        return true;
    }

    public async Task<IEnumerable<LogQuery>> GetSavedQueriesAsync()
    {
        string[] strings = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        Dictionary<string, LogQuery> doc = new ();
        foreach (string s in strings)
        {
            string file_name = Path.GetFileName(s);
            string[] split = file_name.Split('_');

        }
        string match = @"\[(.*?)\]\s+\[(.*?)\]\s+(.*)";

        MatchCollection match_collection = Regex.Matches("", match, RegexOptions.Multiline);

        return doc.Values.AsEnumerable();

    }

    public async Task<IEnumerable<LogEntry>> GetEventsPageAsync(int page, DateTime? startingTimestamp, LogQueryFilter queryFilter)
    {
        string[] strings = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        Dictionary<string, LogQuery> doc = new ();
        List<LogEntry> log_entries = new List<LogEntry>();
        string match = @"\[(.*?)\]\s+\[(.*?)\]\s+(.*)";
        foreach (string s in strings)
        {
            string file_name = Path.GetFileName(s);
            string[] split = file_name.Split('_');

            await using FileStream file_stream = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using MemoryStream ms = new MemoryStream();
            using StreamReader stream_reader = new StreamReader(file_stream);
            while (true)
            {
                if (stream_reader.EndOfStream)
                    break;
                var text = stream_reader.ReadLine();
                if(string.IsNullOrEmpty(text))
                    continue;
                MatchCollection match_collection = Regex.Matches(text, match, RegexOptions.Multiline);

                foreach (Match o in match_collection)
                {
                    string value1 = o.Groups[1].Value;
                    string value2 = o.Groups[2].Value.Trim() switch
                    {
                        "INFO"  => "Information",
                        "Info"  => "Information",
                        "DEBUG" => "Debug",
                        "Debug" => "Debug",
                        "WARN"  => "Warning",
                        "Warn"  => "Warning",
                        "ERROR" => "Error",
                        "Error" => "Error",
                        "FATAL" => "Fatal",
                        "Fatal" => "Fatal",
                        _       => o.Groups[2].Value
                    };
                    string value3 = o.Groups[3].Value;

                    if (string.IsNullOrEmpty(value3))
                        value3 = "Empty";
                    if(!string.IsNullOrEmpty(queryFilter.LevelFilter) && !queryFilter.LevelFilter.Contains(value2))
                        continue;
                    var time = DateTime.ParseExact(value1, "yyyy-MM-dd HH:mm:ss,fff", null);
                    if (!string.IsNullOrEmpty(queryFilter.DateFilter))
                    {
                        DateTime _time = (queryFilter.DateFilter) switch
                        {
                            "5m"  => DateTime.Now.AddMinutes(-5),
                            "1h"  => DateTime.Now.AddHours(-1),
                            "6h"  => DateTime.Now.AddHours(-6),
                            "12h" => DateTime.Now.AddHours(-12),
                            "24h" => DateTime.Now.AddHours(-24),
                            "2d"  => DateTime.Now.AddDays(-2),
                            "5d"  => DateTime.Now.AddDays(-5),
                            _ => throw new ArgumentOutOfRangeException(nameof(queryFilter.DateFilter))
                        };

                        if(time < _time)
                            continue;
                    }
                    else if (queryFilter.DateRangeFilter != null)
                    {
                        if (!(time >= queryFilter.DateRangeFilter[0] && time <= queryFilter.DateRangeFilter[1]))
                            continue;
                    }


                    // if(, out var time))
                    log_entries.Add(new LogEntry()
                        { Id = Guid.NewGuid().ToString(), Message = value3, Level = value2, MessageTemplate = value3, Timestamp =  time, Properties = new List<LogEntryProperty>()
                        {
                            new LogEntryProperty()
                            {
                                Id = 1,
                                LogId = Guid.NewGuid().ToString(),
                                Name = "File",
                                Value = s
                            }
                        }});
                }
            }
        }


        return log_entries.OrderByDescending(t=>t.Timestamp).AsEnumerable();
    }

    public async Task<bool> DeleteQueryAsync(string queryName)
    {
        Console.WriteLine("Delete");

        return true;
    }
}