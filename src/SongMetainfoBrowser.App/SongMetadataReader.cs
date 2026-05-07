using System.IO.Compression;
using System.Xml.Linq;

namespace SongMetainfoBrowser.App;

public static class SongMetadataReader
{
    public static SongMetadata Read(string songPath)
    {
        using var archive = ZipFile.OpenRead(songPath);
        var entry = archive.GetEntry("metainfo.xml") ?? throw new InvalidOperationException("metainfo.xml was not found.");

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var songDocument = LoadSongDocument(archive);
        var mediaTrackNames = ReadMediaTrackNames(songDocument);
        var trackInstruments = ReadTrackInstruments(archive, songDocument);
        var musicParts = ReadMusicParts(archive);
        var notesText = ReadArchiveText(archive, "notes.txt");
        var attributes = document
            .Descendants("Attribute")
            .Where(element => element.Attribute("id") is not null)
            .ToDictionary(
                element => element.Attribute("id")!.Value,
                element => element.Attribute("value")?.Value ?? "",
                StringComparer.OrdinalIgnoreCase);

        FormatDisplayAttributes(attributes);

        attributes.TryGetValue("Media:TimeSignatureNumerator", out var numerator);
        attributes.TryGetValue("Media:TimeSignatureDenominator", out var denominator);
        var timeSignature = !string.IsNullOrWhiteSpace(numerator) && !string.IsNullOrWhiteSpace(denominator)
            ? $"{numerator}/{denominator}"
            : "";

        attributes.TryGetValue("Media:Length", out var lengthSeconds);

        return new SongMetadata
        {
            Path = Path.GetFullPath(songPath),
            FileName = Path.GetFileName(songPath),
            Folder = Path.GetDirectoryName(songPath) ?? "",
            Title = Get(attributes, "Document:Title"),
            Artist = Get(attributes, "Media:Artist"),
            Year = Get(attributes, "Media:Year"),
            DateCreated = DateTimeDisplay.Format(File.GetCreationTime(songPath)),
            LastModified = DateTimeDisplay.Format(File.GetLastWriteTime(songPath)),
            Tempo = FormatTempo(Get(attributes, "Media:Tempo")),
            KeySignature = Get(attributes, "Media:KeySignature"),
            TimeSignature = timeSignature,
            SampleRate = Get(attributes, "Media:SampleRate"),
            BitDepth = Get(attributes, "Media:BitDepth"),
            TrackCount = Get(attributes, "Media:TrackCount"),
            Length = FormatDuration(lengthSeconds),
            Generator = Get(attributes, "Document:Generator"),
            FormatVersion = Get(attributes, "Document:FormatVersion"),
            NotesFile = Get(attributes, "Document:Notes"),
            NotesText = string.IsNullOrWhiteSpace(notesText) ? null : notesText.Trim(),
            ArtworkFile = Get(attributes, "Media:Artwork"),
            Comment = Get(attributes, "Media:Comment"),
            MediaTrackNames = mediaTrackNames,
            TrackInstruments = trackInstruments,
            MusicParts = musicParts,
            Attributes = attributes
        };
    }

    public static bool IsRegularSongFile(string fileName)
    {
        return !fileName.EndsWith(" (Autosaved).song", StringComparison.OrdinalIgnoreCase);
    }

    public static SearchResult? GetSearchMatch(SongMetadata metadata, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var fields = new (string Label, string? Value)[]
        {
            ("Filename", metadata.FileName),
            ("Title", metadata.Title),
            ("Artist", metadata.Artist),
            ("Year", metadata.Year),
            ("Tempo", metadata.Tempo),
            ("Key", metadata.KeySignature),
            ("Time signature", metadata.TimeSignature),
            ("Comment", metadata.Comment),
            ("Notes", metadata.NotesText)
        };

        foreach (var field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field.Value) &&
                field.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = field.Label,
                    MatchValue = FormatMatchValue(field.Value)
                };
            }
        }

        return null;
    }

    private static string? Get(IReadOnlyDictionary<string, string> attributes, string key)
    {
        return attributes.TryGetValue(key, out var value) ? value : null;
    }

    private static string FormatMatchValue(string value)
    {
        var singleLineValue = value.ReplaceLineEndings(" ").Trim();
        return singleLineValue.Length <= 120
            ? singleLineValue
            : $"{singleLineValue[..117]}...";
    }

    private static string FormatDuration(string? seconds)
    {
        if (!double.TryParse(seconds, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return seconds ?? "";
        }

        var time = TimeSpan.FromSeconds(value);
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture)
            : time.ToString(@"m\:ss", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? FormatTempo(string? tempo)
    {
        if (string.IsNullOrWhiteSpace(tempo))
        {
            return tempo;
        }

        if (!double.TryParse(tempo, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return tempo;
        }

        return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void FormatDisplayAttributes(IDictionary<string, string> attributes)
    {
        if (attributes.TryGetValue("Media:Tempo", out var tempo))
        {
            attributes["Media:Tempo"] = FormatTempo(tempo) ?? "";
        }

        if (attributes.TryGetValue("Media:Length", out var length))
        {
            attributes["Media:Length"] = FormatDuration(length);
        }
    }

    private static IReadOnlyList<string> ReadMediaTrackNames(XDocument? document)
    {
        if (document is null)
        {
            return Array.Empty<string>();
        }

        return document
            .Descendants("MediaTrack")
            .Select(element => (string?)element.Attribute("name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();
    }

    private static IReadOnlyList<TrackInstrumentInfo> ReadTrackInstruments(ZipArchive archive, XDocument? songDocument)
    {
        if (songDocument is null)
        {
            return Array.Empty<TrackInstrumentInfo>();
        }

        var musicTrackDocument = LoadArchiveDocument(archive, "Devices/musictrackdevice.xml");
        if (musicTrackDocument is null)
        {
            return songDocument
                .Descendants("MediaTrack")
                .Select(track => new TrackInstrumentInfo
                {
                    TrackName = (string?)track.Attribute("name") ?? "",
                    InstrumentName = null
                })
                .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
                .ToArray();
        }

        var synthNameByDeviceId = LoadSynthNameByDeviceId(archive);
        var instrumentByChannelId = musicTrackDocument
            .Descendants("MusicTrackChannel")
            .Select(channel =>
            {
                var channelId = (string?)channel.Elements("UID").FirstOrDefault(element => string.Equals((string?)element.Attribute("x_id"), "uniqueID", StringComparison.Ordinal))?.Attribute("uid");
                var destinationObjectId = (string?)channel.Elements("Connection").FirstOrDefault(element => string.Equals((string?)element.Attribute("x_id"), "destination", StringComparison.Ordinal))?.Attribute("objectID");
                if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(destinationObjectId))
                {
                    return null;
                }

                var deviceId = destinationObjectId.Split('/')[0];
                synthNameByDeviceId.TryGetValue(deviceId, out var instrumentName);
                return new { ChannelId = channelId, InstrumentName = instrumentName };
            })
            .Where(item => item is not null)
            .ToDictionary(item => item!.ChannelId, item => item!.InstrumentName, StringComparer.OrdinalIgnoreCase);

        return songDocument
            .Descendants("MediaTrack")
            .Select(track =>
            {
                var trackName = (string?)track.Attribute("name") ?? "";
                var channelId = (string?)track.Elements("UID").FirstOrDefault(element => string.Equals((string?)element.Attribute("x_id"), "channelID", StringComparison.Ordinal))?.Attribute("uid");
                instrumentByChannelId.TryGetValue(channelId ?? "", out var instrumentName);
                return new TrackInstrumentInfo
                {
                    TrackName = trackName,
                    InstrumentName = instrumentName
                };
            })
            .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
            .ToArray();
    }

    private static IReadOnlyList<MusicPartInfo> ReadMusicParts(ZipArchive archive)
    {
        var document = LoadSongDocument(archive);
        if (document is null)
        {
            return Array.Empty<MusicPartInfo>();
        }

        return document
            .Descendants("MediaTrack")
            .SelectMany(track =>
            {
                var trackName = (string?)track.Attribute("name");
                if (string.IsNullOrWhiteSpace(trackName))
                {
                    return Enumerable.Empty<MusicPartInfo>();
                }

                return track
                    .Elements("List")
                    .Where(list => string.Equals((string?)list.Attribute("x_id"), "Events", StringComparison.Ordinal))
                    .Elements("MusicPart")
                    .Select(part => new MusicPartInfo
                    {
                        TrackName = trackName,
                        PartName = (string?)part.Attribute("name") ?? ""
                    });
            })
            .ToArray();
    }

    private static XDocument? LoadSongDocument(ZipArchive archive)
    {
        var xmlText = ReadArchiveText(archive, "Song/song.xml");
        if (string.IsNullOrWhiteSpace(xmlText))
        {
            return null;
        }

        // Some .song files use an undeclared x: prefix in song.xml, so normalize it before parsing.
        var sanitizedXml = xmlText.Replace("x:", "x_", StringComparison.Ordinal);
        return XDocument.Parse(sanitizedXml, LoadOptions.None);
    }

    private static Dictionary<string, string> LoadSynthNameByDeviceId(ZipArchive archive)
    {
        var synthDocument = LoadArchiveDocument(archive, "Devices/audiosynthfolder.xml");
        if (synthDocument is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return synthDocument.Root?
            .Elements("Attributes")
            .Select(device =>
            {
                var deviceData = device.Elements("Attributes").FirstOrDefault(element => string.Equals((string?)element.Attribute("x_id"), "deviceData", StringComparison.Ordinal));
                var deviceId = (string?)deviceData?.Elements("UID").FirstOrDefault(element => string.Equals((string?)element.Attribute("x_id"), "uniqueID", StringComparison.Ordinal))?.Attribute("uid");
                var deviceName = (string?)deviceData?.Attribute("name");
                return new { DeviceId = deviceId, DeviceName = deviceName };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId) && !string.IsNullOrWhiteSpace(item.DeviceName))
            .ToDictionary(item => item.DeviceId!, item => item.DeviceName!, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static XDocument? LoadArchiveDocument(ZipArchive archive, string entryPath)
    {
        var xmlText = ReadArchiveText(archive, entryPath);
        if (string.IsNullOrWhiteSpace(xmlText))
        {
            return null;
        }

        var sanitizedXml = xmlText.Replace("x:", "x_", StringComparison.Ordinal);
        return XDocument.Parse(sanitizedXml, LoadOptions.None);
    }

    private static string? ReadArchiveText(ZipArchive archive, string entryPath)
    {
        var entry = archive.GetEntry(entryPath);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
