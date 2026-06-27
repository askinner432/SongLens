using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Reads Studio One .song archives and translates their internal files into SongLens models.
/// </summary>
public static class SongMetadataReader
{
    /// <summary>
    /// Reads the primary Studio One metadata files from a .song archive and assembles
    /// a UI-friendly <see cref="SongMetadata"/> object.
    /// </summary>
    public static SongMetadata Read(string songPath)
    {
        using var archive = ZipFile.OpenRead(songPath);
        var entry = archive.GetEntry("metainfo.xml") ?? throw new InvalidOperationException("metainfo.xml was not found.");

        var document = LoadArchiveDocument(archive, "metainfo.xml")
            ?? throw new InvalidOperationException("metainfo.xml could not be parsed.");
        var songDocument = LoadSongDocument(archive);
        var mediaTrackNames = ReadMediaTrackNames(songDocument);
        var trackNotesByTitle = ReadTrackNotes(archive);
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
            TrackInstruments = MergeTrackNotes(trackInstruments, trackNotesByTitle),
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

        foreach (var track in metadata.TrackInstruments)
        {
            if (!string.IsNullOrWhiteSpace(track.TrackName) &&
                track.TrackName.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Track",
                    MatchValue = FormatMatchValue(track.TrackName)
                };
            }

            if (!string.IsNullOrWhiteSpace(track.InstrumentName) &&
                track.InstrumentName.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Instrument",
                    MatchValue = FormatMatchValue(track.InstrumentName)
                };
            }

            if (!string.IsNullOrWhiteSpace(track.TrackNote) &&
                track.TrackNote.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Track note",
                    MatchValue = FormatMatchValue(track.TrackNote)
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
                    InstrumentName = null,
                    TrackNote = null
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
                var channelId = (string?)track.Attribute("channel");
                instrumentByChannelId.TryGetValue(channelId ?? "", out var instrumentName);
                return new TrackInstrumentInfo
                {
                    TrackName = trackName,
                    InstrumentName = instrumentName,
                    TrackNote = null
                };
            })
            .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> LoadSynthNameByDeviceId(ZipArchive archive)
    {
        var synthDocument = LoadArchiveDocument(archive, "Devices/audiosynthfolder.xml");
        if (synthDocument is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return synthDocument
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "AudioSynth", StringComparison.Ordinal))
            .Select(element =>
            {
                var uid = (string?)element.Elements().FirstOrDefault(child => string.Equals(child.Name.LocalName, "UID", StringComparison.Ordinal)
                                                                               && string.Equals((string?)child.Attribute("x_id"), "uniqueID", StringComparison.Ordinal))?.Attribute("uid");
                var name = (string?)element.Attribute("name");
                return string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(name)
                    ? null
                    : new { Uid = uid, Name = name };
            })
            .Where(pair => pair is not null)
            .ToDictionary(pair => pair!.Uid, pair => pair!.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<TrackInstrumentInfo> MergeTrackNotes(IReadOnlyList<TrackInstrumentInfo> tracks, IReadOnlyDictionary<string, string> trackNotesByTitle)
    {
        return tracks
            .Select(track => new TrackInstrumentInfo
            {
                TrackName = track.TrackName,
                InstrumentName = track.InstrumentName,
                TrackNote = trackNotesByTitle.TryGetValue(track.TrackName, out var note) ? note : null
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadTrackNotes(ZipArchive archive)
    {
        var notepadDocument = LoadArchiveDocument(archive, "notepad.xml");
        if (notepadDocument is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return notepadDocument
            .Descendants("Section")
            .Select(section =>
            {
                var title = (string?)section.Attribute("title");
                var text = section.Value?.Trim();
                return string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text)
                    ? null
                    : new { Title = title, Text = text };
            })
            .Where(pair => pair is not null)
            .ToDictionary(pair => pair!.Title, pair => pair!.Text, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<MusicPartInfo> ReadMusicParts(ZipArchive archive)
    {
        var songDocument = LoadSongDocument(archive);
        if (songDocument is null)
        {
            return Array.Empty<MusicPartInfo>();
        }

        return songDocument
            .Descendants("MusicPart")
            .Select(part =>
            {
                var track = part.Ancestors("MediaTrack").FirstOrDefault();
                var trackName = (string?)track?.Attribute("name");
                var partName = (string?)part.Attribute("name");
                return string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(partName)
                    ? null
                    : new MusicPartInfo
                    {
                        TrackName = trackName,
                        PartName = partName
                    };
            })
            .Where(part => part is not null)
            .Cast<MusicPartInfo>()
            .ToArray();
    }

    private static XDocument? LoadSongDocument(ZipArchive archive)
    {
        return LoadArchiveDocument(archive, "Song/song.xml");
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

    private static XDocument? LoadArchiveDocument(ZipArchive archive, string entryPath)
    {
        var entry = archive.GetEntry(entryPath);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var xml = reader.ReadToEnd();
        xml = NormalizeUndeclaredXPrefix(xml);
        return XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
    }

    private static string NormalizeUndeclaredXPrefix(string xml)
    {
        const string xNamespace = "xmlns:x=\"http://www.w3.org/2001/XMLSchema-instance\"";
        if (!xml.Contains("x:", StringComparison.Ordinal) || xml.Contains(xNamespace, StringComparison.Ordinal))
        {
            return xml;
        }

        return new Regex(@"<([A-Za-z_][\w\-.]*)(\s|>)", RegexOptions.CultureInvariant)
            .Replace(xml, $"<$1 {xNamespace}$2", 1);
    }
}
