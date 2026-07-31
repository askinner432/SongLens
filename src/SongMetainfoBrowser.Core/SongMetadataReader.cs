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
        var readWarnings = new HashSet<SongReadWarning>();

        var document = LoadArchiveDocument(archive, "metainfo.xml")
            ?? throw new InvalidOperationException("metainfo.xml could not be parsed.");
        var songDocument = LoadSongDocument(archive, readWarnings);
        var mediaTrackNames = ReadMediaTrackNames(songDocument);
        var trackNotesByTitle = ReadTrackNotes(archive, readWarnings);
        var trackInstruments = ReadTrackInstruments(archive, songDocument, readWarnings);
        var groups = ReadGroups(songDocument);
        var mixerData = ReadMixerDetails(archive, readWarnings);
        var musicParts = ReadMusicParts(songDocument);
        var presets = ReadPresets(archive);
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
            Presets = presets,
            MediaTrackNames = mediaTrackNames,
            TrackInstruments = MergeTrackNotes(trackInstruments, trackNotesByTitle),
            Groups = groups,
            MixerMainChannels = mixerData.MainChannels,
            MixerInserts = mixerData.Inserts,
            MixerSends = mixerData.Sends,
            MusicParts = musicParts,
            Attributes = attributes,
            ReadWarnings = readWarnings
                .OrderBy(warning => warning.ArchiveEntry, StringComparer.OrdinalIgnoreCase)
                .ThenBy(warning => warning.Message, StringComparer.Ordinal)
                .ToArray()
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

        var normalizedQuery = query.Trim();

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
                ContainsWholeWord(field.Value, normalizedQuery))
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
                ContainsWholeWord(track.TrackName, normalizedQuery))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Track",
                    MatchValue = FormatMatchValue(track.TrackName)
                };
            }

            if (!string.IsNullOrWhiteSpace(track.InstrumentName) &&
                ContainsWholeWord(track.InstrumentName, normalizedQuery))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Instrument",
                    MatchValue = FormatMatchValue(track.InstrumentName)
                };
            }

            if (!string.IsNullOrWhiteSpace(track.TrackNote) &&
                ContainsWholeWord(track.TrackNote, normalizedQuery))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Track note",
                    MatchValue = FormatMatchValue(track.TrackNote)
                };
            }
        }

        foreach (var preset in metadata.Presets)
        {
            if (ContainsWholeWord(preset, normalizedQuery))
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = "Preset",
                    MatchValue = FormatMatchValue(preset)
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

    private static bool ContainsWholeWord(string value, string query)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(query)}(?![\p{{L}}\p{{N}}])";
        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

    private static IReadOnlyList<TrackInstrumentInfo> ReadTrackInstruments(
        ZipArchive archive,
        XDocument? songDocument,
        ISet<SongReadWarning> readWarnings)
    {
        if (songDocument is null)
        {
            return Array.Empty<TrackInstrumentInfo>();
        }

        var precedingFolderCount = 0;
        var songTracks = songDocument
            .Descendants()
            .Where(element => element.Name.LocalName is "MediaTrack" or "FolderTrack")
            .Select((element, index) =>
            {
                var originalPosition = index + 1;
                if (element.Name.LocalName == "FolderTrack")
                {
                    precedingFolderCount++;
                    return new { Element = element, TrackNumber = originalPosition, OriginalPosition = originalPosition };
                }

                var trackNumber = int.TryParse(
                    (string?)element.Attribute("trackNumber"),
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var mediaTrackNumber)
                    ? mediaTrackNumber + precedingFolderCount
                    : originalPosition;
                return new { Element = element, TrackNumber = trackNumber, OriginalPosition = originalPosition };
            })
            .OrderBy(track => track.TrackNumber)
            .ThenBy(track => track.OriginalPosition)
            .ToArray();
        var folderParentById = songTracks
            .Where(track => track.Element.Name.LocalName == "FolderTrack")
            .Select(track => new
            {
                TrackId = (string?)track.Element.Attribute("trackID"),
                ParentFolderId = (string?)track.Element.Attribute("parentFolder")
            })
            .Where(folder => !string.IsNullOrWhiteSpace(folder.TrackId))
            .ToDictionary(folder => folder.TrackId!, folder => folder.ParentFolderId, StringComparer.OrdinalIgnoreCase);
        var folderIdsWithChildFolders = folderParentById.Values
            .Where(parentFolderId => !string.IsNullOrWhiteSpace(parentFolderId))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var foldersContainingEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var eventTrack in songTracks.Where(track => TrackHasEvents(track.Element)))
        {
            var parentFolderId = (string?)eventTrack.Element.Attribute("parentFolder");
            while (!string.IsNullOrWhiteSpace(parentFolderId) && foldersContainingEvents.Add(parentFolderId))
            {
                parentFolderId = folderParentById.TryGetValue(parentFolderId, out var nextParentFolderId)
                    ? nextParentFolderId
                    : null;
            }
        }

        var musicTrackDocument = LoadArchiveDocument(archive, "Devices/musictrackdevice.xml", readWarnings);
        var audioMixerDocument = LoadArchiveDocument(archive, "Devices/audiomixer.xml", readWarnings);
        var disabledByChannelId = ReadChannelDisabledStates(musicTrackDocument, audioMixerDocument);
        if (musicTrackDocument is null)
        {
            return songTracks
                .Select(track =>
                {
                    var channelId = GetTrackChannelId(track.Element);
                    return new TrackInstrumentInfo
                    {
                        TrackNumber = track.TrackNumber,
                        TrackName = (string?)track.Element.Attribute("name") ?? "",
                        IsFolderTrack = track.Element.Name.LocalName == "FolderTrack",
                        HasChildFolders = TrackHasChildFolders(track.Element, folderIdsWithChildFolders),
                        HierarchyDepth = GetTrackHierarchyDepth(track.Element, folderParentById),
                        IsDisabled = IsChannelDisabled(channelId, disabledByChannelId),
                        InstrumentName = null,
                        TrackNote = null,
                        HasEvents = TrackHasEvents(track.Element) || FolderContainsEvents(track.Element, foldersContainingEvents)
                    };
                })
                .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
                .ToArray();
        }

        var instrumentByChannelId = musicTrackDocument
            .Descendants("MusicTrackChannel")
            .Select(channel =>
            {
                var channelId = (string?)channel.Elements("UID")
                    .FirstOrDefault(element => HasStudioOneXId(element, "uniqueID"))
                    ?.Attribute("uid");
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    return null;
                }

                var destinationFriendlyName = (string?)channel.Elements("Connection")
                    .FirstOrDefault(element => HasStudioOneXId(element, "destination"))
                    ?.Attribute("friendlyName");
                var instrumentName =
                    ExtractDestinationInstrumentName(destinationFriendlyName)
                    ?? (string?)channel.Attribute("label")
                    ?? (string?)channel.Elements("Connection")
                        .FirstOrDefault(element => HasStudioOneXId(element, "instrumentOut"))
                        ?.Attribute("friendlyName")
                    ?? (string?)channel.Attribute("name");

                return new { ChannelId = channelId, InstrumentName = instrumentName };
            })
            .Where(item => item is not null)
            .ToDictionary(item => item!.ChannelId, item => item!.InstrumentName, StringComparer.OrdinalIgnoreCase);

        return songTracks
            .Select(track =>
            {
                var trackName = (string?)track.Element.Attribute("name") ?? "";
                var channelId = GetTrackChannelId(track.Element);
                instrumentByChannelId.TryGetValue(channelId ?? "", out var instrumentName);
                return new TrackInstrumentInfo
                {
                    TrackNumber = track.TrackNumber,
                    TrackName = trackName,
                    IsFolderTrack = track.Element.Name.LocalName == "FolderTrack",
                    HasChildFolders = TrackHasChildFolders(track.Element, folderIdsWithChildFolders),
                    HierarchyDepth = GetTrackHierarchyDepth(track.Element, folderParentById),
                    IsDisabled = IsChannelDisabled(channelId, disabledByChannelId),
                    InstrumentName = instrumentName,
                    TrackNote = null,
                    HasEvents = TrackHasEvents(track.Element) || FolderContainsEvents(track.Element, foldersContainingEvents)
                };
            })
            .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
            .ToArray();
    }

    private static string? ExtractDestinationInstrumentName(string? destinationFriendlyName)
    {
        if (string.IsNullOrWhiteSpace(destinationFriendlyName))
        {
            return null;
        }

        var slashIndex = destinationFriendlyName.IndexOf('/');
        var withoutPortSuffix = slashIndex >= 0
            ? destinationFriendlyName[..slashIndex]
            : destinationFriendlyName;
        var trimmed = withoutPortSuffix.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var match = Regex.Match(trimmed, @"^\d+\s*-\s*(.+)$", RegexOptions.CultureInvariant);
        return match.Success
            ? match.Groups[1].Value.Trim()
            : trimmed;
    }

    private static IReadOnlyList<TrackInstrumentInfo> MergeTrackNotes(IReadOnlyList<TrackInstrumentInfo> tracks, IReadOnlyDictionary<string, string> trackNotesByTitle)
    {
        return tracks
            .Select(track => new TrackInstrumentInfo
            {
                TrackNumber = track.TrackNumber,
                TrackName = track.TrackName,
                IsFolderTrack = track.IsFolderTrack,
                HasChildFolders = track.HasChildFolders,
                HierarchyDepth = track.HierarchyDepth,
                IsDisabled = track.IsDisabled,
                InstrumentName = track.InstrumentName,
                TrackNote = trackNotesByTitle.TryGetValue(track.TrackName, out var note) ? note : null,
                HasEvents = track.HasEvents
            })
            .ToArray();
    }

    private static bool TrackHasEvents(XElement track)
    {
        return track.Elements("List")
            .FirstOrDefault(element => HasStudioOneXId(element, "Events"))
            ?.Elements()
            .Any() == true;
    }

    private static string? GetTrackChannelId(XElement track)
    {
        return (string?)track.Elements("UID")
            .FirstOrDefault(element => HasStudioOneXId(element, "channelID"))
            ?.Attribute("uid");
    }

    private static IReadOnlyDictionary<string, bool> ReadChannelDisabledStates(params XDocument?[] documents)
    {
        return documents
            .Where(document => document is not null)
            .SelectMany(document => document!.Descendants())
            .Select(element => new
            {
                ChannelId = (string?)element.Elements("UID")
                    .FirstOrDefault(uid => HasStudioOneXId(uid, "uniqueID"))
                    ?.Attribute("uid"),
                IsDisabled = ParseBoolAttribute(element, "disabled")
            })
            .Where(channel => !string.IsNullOrWhiteSpace(channel.ChannelId))
            .GroupBy(channel => channel.ChannelId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Any(channel => channel.IsDisabled),
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsChannelDisabled(string? channelId, IReadOnlyDictionary<string, bool> disabledByChannelId)
    {
        return !string.IsNullOrWhiteSpace(channelId)
            && disabledByChannelId.TryGetValue(channelId, out var isDisabled)
            && isDisabled;
    }

    private static bool FolderContainsEvents(XElement track, IReadOnlySet<string> foldersContainingEvents)
    {
        var trackId = (string?)track.Attribute("trackID");
        return track.Name.LocalName == "FolderTrack"
            && !string.IsNullOrWhiteSpace(trackId)
            && foldersContainingEvents.Contains(trackId);
    }

    private static bool TrackHasChildFolders(XElement track, IReadOnlySet<string> folderIdsWithChildFolders)
    {
        var trackId = (string?)track.Attribute("trackID");
        return track.Name.LocalName == "FolderTrack"
            && !string.IsNullOrWhiteSpace(trackId)
            && folderIdsWithChildFolders.Contains(trackId);
    }

    private static int GetTrackHierarchyDepth(XElement track, IReadOnlyDictionary<string, string?> folderParentById)
    {
        var depth = 0;
        var parentFolderId = (string?)track.Attribute("parentFolder");
        var visitedFolderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!string.IsNullOrWhiteSpace(parentFolderId) && visitedFolderIds.Add(parentFolderId))
        {
            depth++;
            parentFolderId = folderParentById.TryGetValue(parentFolderId, out var nextParentFolderId)
                ? nextParentFolderId
                : null;
        }

        return depth;
    }

    private static IReadOnlyDictionary<string, string> ReadTrackNotes(
        ZipArchive archive,
        ISet<SongReadWarning> readWarnings)
    {
        var notepadDocument = LoadArchiveDocument(archive, "notepad.xml", readWarnings);
        if (notepadDocument?.Root is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return notepadDocument.Root
            .DescendantsAndSelf()
            .Where(element => element.Name.LocalName is "NotepadItem" or "Section")
            .Select(element =>
            {
                var title = (string?)element.Attribute("title");
                var rawText = element.Name.LocalName == "NotepadItem"
                    ? (string?)element.Attribute("text")
                    : element.Value;
                var text = DecodeTrackNoteText(rawText);
                return string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text)
                    ? null
                    : new { Title = title, Text = text };
            })
            .Where(pair => pair is not null)
            .GroupBy(pair => pair!.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First()!.Text, StringComparer.OrdinalIgnoreCase);
    }

    private static string? DecodeTrackNoteText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : System.Net.WebUtility.HtmlDecode(value).Trim();
    }

    private static IReadOnlyList<SongGroupInfo> ReadGroups(XDocument? songDocument)
    {
        if (songDocument is null)
        {
            return Array.Empty<SongGroupInfo>();
        }

        var folderNodes = songDocument
            .Descendants("FolderTrack")
            .Select(folder => new
            {
                TrackId = (string?)folder.Attribute("trackID"),
                ParentFolderId = (string?)folder.Attribute("parentFolder"),
                GroupName = (string?)folder.Attribute("name")
            })
            .Where(folder => !string.IsNullOrWhiteSpace(folder.TrackId) && !string.IsNullOrWhiteSpace(folder.GroupName))
            .ToArray();

        var childFolderIdsByParentId = folderNodes
            .Where(folder => !string.IsNullOrWhiteSpace(folder.ParentFolderId))
            .GroupBy(folder => folder.ParentFolderId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(folder => folder.TrackId!).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var trackNamesByParentId = songDocument
            .Descendants("MediaTrack")
            .Select(track => new
            {
                ParentFolderId = (string?)track.Attribute("parentFolder"),
                TrackName = (string?)track.Attribute("name")
            })
            .Where(track => !string.IsNullOrWhiteSpace(track.ParentFolderId) && !string.IsNullOrWhiteSpace(track.TrackName))
            .GroupBy(track => track.ParentFolderId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(track => track.TrackName!).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var cache = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> GetDescendantTrackNames(string folderId)
        {
            if (cache.TryGetValue(folderId, out var cachedTrackNames))
            {
                return cachedTrackNames;
            }

            var trackNames = new List<string>();
            if (trackNamesByParentId.TryGetValue(folderId, out var directTrackNames))
            {
                trackNames.AddRange(directTrackNames);
            }

            if (childFolderIdsByParentId.TryGetValue(folderId, out var childFolderIds))
            {
                foreach (var childFolderId in childFolderIds)
                {
                    trackNames.AddRange(GetDescendantTrackNames(childFolderId));
                }
            }

            cache[folderId] = trackNames;
            return trackNames;
        }

        return folderNodes
            .Select(folder => new SongGroupInfo
            {
                GroupName = folder.GroupName!,
                TrackNames = GetDescendantTrackNames(folder.TrackId!)
            })
            .Where(group => group.TrackNames.Count > 0)
            .ToArray();
    }

    private static IReadOnlyList<MusicPartInfo> ReadMusicParts(XDocument? songDocument)
    {
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

    private static IReadOnlyList<string> ReadPresets(ZipArchive archive)
    {
        return archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.FullName)
                && entry.FullName.StartsWith("Presets/Channels/", StringComparison.OrdinalIgnoreCase)
                && !entry.FullName.EndsWith("/", StringComparison.Ordinal))
            .Select(entry => entry.FullName["Presets/".Length..].Replace('/', Path.DirectorySeparatorChar))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static (IReadOnlyList<MixerMainInfo> MainChannels, IReadOnlyList<MixerInsertInfo> Inserts, IReadOnlyList<MixerSendInfo> Sends) ReadMixerDetails(
        ZipArchive archive,
        ISet<SongReadWarning> readWarnings)
    {
        try
        {
            return ParseMixerDetails(archive, readWarnings);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or System.Xml.XmlException)
        {
            readWarnings.Add(new SongReadWarning("Devices/audiomixer.xml", exception.Message));
            return EmptyMixerDetails();
        }
    }

    private static (IReadOnlyList<MixerMainInfo> MainChannels, IReadOnlyList<MixerInsertInfo> Inserts, IReadOnlyList<MixerSendInfo> Sends) ParseMixerDetails(
        ZipArchive archive,
        ISet<SongReadWarning> readWarnings)
    {
        var mixerDocument = LoadArchiveDocument(archive, "Devices/audiomixer.xml", readWarnings);
        if (mixerDocument is null)
        {
            return EmptyMixerDetails();
        }

        var mainChannels = new List<MixerMainInfo>();
        var inserts = new List<MixerInsertInfo>();
        var sends = new List<MixerSendInfo>();
        var channelPresetNamesById = mixerDocument
            .Descendants()
            .Where(IsMixerChannelElement)
            .Select(channel => new
            {
                ChannelId = GetMixerChannelId(channel),
                PresetName = ReadMixerChannelPresetName(channel)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ChannelId))
            .GroupBy(item => item.ChannelId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.PresetName).FirstOrDefault(presetName => !string.IsNullOrWhiteSpace(presetName)),
                StringComparer.OrdinalIgnoreCase);

        foreach (var channel in mixerDocument.Descendants().Where(IsMixerChannelElement))
        {
            var channelName = (string?)channel.Attribute("label") ?? (string?)channel.Attribute("name");
            if (string.IsNullOrWhiteSpace(channelName))
            {
                continue;
            }

            if (IsVisibleMixerChannelElement(channel))
            {
                var preInsertSlots = GetMixerInsertSlots(channel, postFader: false)
                    .Select(item => item.Insert)
                    .ToArray();
                var postInsertSlots = GetMixerInsertSlots(channel, postFader: true)
                    .Select(item => item.Insert)
                    .ToArray();
                mainChannels.Add(new MixerMainInfo
                {
                    ChannelName = channelName,
                    PrePluginChain = BuildMixerChannelPluginChain(preInsertSlots),
                    PostPluginChain = BuildMixerChannelPluginChain(postInsertSlots),
                    IsMainOutput = channel.Name.LocalName is "AudioOutputChannel"
                });
            }

            foreach (var mixerInsert in GetMixerInsertSlots(channel))
            {
                var insert = mixerInsert.Insert;
                var presetsNode = insert.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "Presets"));
                inserts.Add(new MixerInsertInfo
                {
                    ChannelName = channelName,
                    SlotName = FormatMixerInsertSlotName(insert, mixerInsert.IsPostFader),
                    PluginName = (string?)insert.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "deviceData"))?.Attribute("name"),
                    PresetName = ReadMixerInsertPresetName(presetsNode),
                    PresetPath = (string?)insert.Elements("String").FirstOrDefault(element => HasStudioOneXId(element, "presetPath"))?.Attribute("text"),
                    IsBypassed = ParseBoolAttribute(insert, "bypass")
                });
            }

            var sendsNode = channel.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "Sends"));
            if (sendsNode is not null)
            {
                foreach (var send in sendsNode.Elements("Attributes").Where(IsMixerSendSlot))
                {
                    var destinationObjectId = (string?)send.Elements("Connection").FirstOrDefault(element => HasStudioOneXId(element, "destination"))?.Attribute("objectID");
                    sends.Add(new MixerSendInfo
                    {
                        ChannelName = channelName,
                        SlotName = (string?)send.Attribute("name") ?? "",
                        DestinationName = (string?)send.Elements("Connection").FirstOrDefault(element => HasStudioOneXId(element, "destination"))?.Attribute("friendlyName"),
                        PresetName = ReadMixerSendPresetName(destinationObjectId, channelPresetNamesById),
                        IsPreFader = ParseBoolAttribute(send, "prefader"),
                        Level = (string?)send.Attribute("level"),
                        Pan = (string?)send.Attribute("pan"),
                        IsBypassed = ParseBoolAttribute(send, "bypass")
                    });
                }
            }
        }

        return (
            mainChannels.OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            inserts.OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            sends.OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase).ToArray()
        );
    }

    private static (IReadOnlyList<MixerMainInfo> MainChannels, IReadOnlyList<MixerInsertInfo> Inserts, IReadOnlyList<MixerSendInfo> Sends) EmptyMixerDetails()
    {
        return (Array.Empty<MixerMainInfo>(), Array.Empty<MixerInsertInfo>(), Array.Empty<MixerSendInfo>());
    }

    private static bool IsMixerChannelElement(XElement element)
    {
        return element.Name.LocalName is "AudioInputChannel" or "AudioOutputChannel" or "AudioSynthChannel" or "AudioGroupChannel" or "AudioListenBusChannel" or "AudioEffectChannel";
    }

    private static bool IsVisibleMixerChannelElement(XElement element)
    {
        return element.Name.LocalName is "AudioOutputChannel" or "AudioSynthChannel" or "AudioGroupChannel" or "AudioListenBusChannel" or "AudioEffectChannel";
    }

    private static bool IsMixerInsertSlot(XElement element)
    {
        var slotName = (string?)element.Attribute("name");
        return !string.IsNullOrWhiteSpace(slotName)
            && slotName.StartsWith("FX", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMixerSendSlot(XElement element)
    {
        var slotName = (string?)element.Attribute("name");
        return !string.IsNullOrWhiteSpace(slotName)
            && slotName.StartsWith("Send", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParseBoolAttribute(XElement element, string attributeName)
    {
        return string.Equals((string?)element.Attribute(attributeName), "1", StringComparison.Ordinal);
    }

    private static string? ReadMixerInsertPresetName(XElement? presetsNode)
    {
        if (presetsNode is null)
        {
            return null;
        }

        var presetName = (string?)presetsNode.Attribute("pname");
        if (!string.IsNullOrWhiteSpace(presetName))
        {
            return presetName;
        }

        var presetUrl = (string?)presetsNode.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "url"))?.Attribute("url");
        if (string.IsNullOrWhiteSpace(presetUrl) || !Uri.TryCreate(presetUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : fileName;
    }

    private static string? GetMixerChannelId(XElement channel)
    {
        return (string?)channel.Elements("UID").FirstOrDefault(element => HasStudioOneXId(element, "uniqueID"))?.Attribute("uid");
    }

    private static string? ReadMixerChannelPresetName(XElement channel)
    {
        var presetNames = GetMixerInsertSlots(channel)
            .Select(item => item.Insert)
            .Select(insert => ReadMixerInsertPresetName(insert.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "Presets"))))
            .Where(presetName => !string.IsNullOrWhiteSpace(presetName))
            .ToArray();

        return presetNames.Length == 0
            ? null
            : string.Join(" | ", presetNames);
    }

    private static string? BuildMixerChannelPluginChain(IReadOnlyList<XElement> insertSlots)
    {
        var pluginNames = insertSlots
            .Select(insert => (string?)insert.Elements("Attributes").FirstOrDefault(element => HasStudioOneXId(element, "deviceData"))?.Attribute("name"))
            .Where(pluginName => !string.IsNullOrWhiteSpace(pluginName))
            .ToArray();

        return pluginNames.Length == 0
            ? null
            : string.Join(" | ", pluginNames);
    }

    private static IEnumerable<(XElement Insert, bool IsPostFader)> GetMixerInsertSlots(XElement channel, bool? postFader = null)
    {
        foreach (var section in channel.Elements("Attributes"))
        {
            var isInsertSection = HasStudioOneXId(section, "Inserts");
            var isPostFaderSection = HasStudioOneXId(section, "PostFaderInserts");
            if (!isInsertSection && !isPostFaderSection)
            {
                continue;
            }

            if (postFader is not null && isPostFaderSection != postFader.Value)
            {
                continue;
            }

            foreach (var insert in section.Elements("Attributes")
                         .Where(IsMixerInsertSlot)
                         .OrderBy(element => (string?)element.Attribute("name"), StringComparer.CurrentCultureIgnoreCase))
            {
                yield return (insert, isPostFaderSection);
            }
        }
    }

    private static string FormatMixerInsertSlotName(XElement insert, bool isPostFader)
    {
        var slotName = (string?)insert.Attribute("name") ?? "";
        return isPostFader
            ? $"{slotName} (Post)"
            : slotName;
    }

    private static string? ReadMixerSendPresetName(string? destinationObjectId, IReadOnlyDictionary<string, string?> channelPresetNamesById)
    {
        if (string.IsNullOrWhiteSpace(destinationObjectId))
        {
            return null;
        }

        var channelId = destinationObjectId.Split('/')[0];
        return channelPresetNamesById.TryGetValue(channelId, out var presetName)
            ? presetName
            : null;
    }

    private static bool HasStudioOneXId(XElement element, string expectedValue)
    {
        var normalizedValue = (string?)element.Attribute("x_id")
            ?? element.Attributes()
                .FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, "id", StringComparison.Ordinal))
                ?.Value;

        return string.Equals(normalizedValue, expectedValue, StringComparison.Ordinal);
    }

    private static XDocument? LoadSongDocument(ZipArchive archive, ISet<SongReadWarning> readWarnings)
    {
        return LoadArchiveDocument(archive, "Song/song.xml", readWarnings);
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

    private static XDocument? LoadArchiveDocument(
        ZipArchive archive,
        string entryPath,
        ISet<SongReadWarning>? readWarnings = null)
    {
        var entry = archive.GetEntry(entryPath);
        if (entry is null)
        {
            return null;
        }

        try
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();
            xml = NormalizeUndeclaredXPrefix(xml);
            xml = NormalizeHtmlStyleEntities(xml);
            var normalizedXml = NormalizeStudioOneElementNames(xml);
            if (readWarnings is not null && !string.Equals(normalizedXml, xml, StringComparison.Ordinal))
            {
                readWarnings.Add(new SongReadWarning(
                    entryPath,
                    "SongLens repaired malformed internal XML element names; some metadata may be unavailable."));
            }

            return XDocument.Parse(normalizedXml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (
            readWarnings is not null
            && exception is (InvalidDataException or IOException or System.Xml.XmlException))
        {
            readWarnings!.Add(new SongReadWarning(entryPath, exception.Message));
            return null;
        }
    }

    private static string NormalizeStudioOneElementNames(string xml)
    {
        // Some Studio One/Fender Studio Pro files serialize internal C++ class
        // names such as AudioFXGraph::SplitterNode as XML element names. A QName
        // may contain only one colon, so make those otherwise valid nodes parseable.
        return Regex.Replace(
            xml,
            @"(<\/?)([A-Za-z_][\w\-.]*(?:::[A-Za-z_][\w\-.]*)+)(?=[\s/>])",
            static match => match.Groups[1].Value + match.Groups[2].Value.Replace("::", "__", StringComparison.Ordinal),
            RegexOptions.CultureInvariant);
    }

    private static string NormalizeHtmlStyleEntities(string xml)
    {
        // Studio/Fender song XML sometimes contains HTML named entities even
        // though they are not declared in an XML DTD. Preserve XML's built-in
        // entities, decode known HTML entities, and retain unknown names as text.
        return Regex.Replace(
            xml,
            @"&([A-Za-z][A-Za-z0-9]+);",
            static match =>
            {
                var entityName = match.Groups[1].Value;
                if (entityName is "amp" or "lt" or "gt" or "apos" or "quot")
                {
                    return match.Value;
                }

                var decoded = System.Net.WebUtility.HtmlDecode(match.Value);
                return decoded == match.Value
                    ? $"&amp;{entityName};"
                    : System.Security.SecurityElement.Escape(decoded) ?? "";
            },
            RegexOptions.CultureInvariant);
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
