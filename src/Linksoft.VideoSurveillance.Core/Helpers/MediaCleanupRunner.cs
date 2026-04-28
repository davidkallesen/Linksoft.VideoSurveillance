namespace Linksoft.VideoSurveillance.Helpers;

/// <summary>
/// Pure file-system cleanup pass shared by the WPF and server media-cleanup
/// services. No timers, no logging, no DI — input is a directory + retention
/// rules + active-path skip set; output is counts, bytes freed, and the list
/// of deleted files (for the caller to log).
/// </summary>
public static class MediaCleanupRunner
{
    /// <summary>
    /// Deletes files in <paramref name="rootPath"/> (recursive) whose
    /// extension is in <paramref name="extensions"/> and whose last-write
    /// time is strictly before <paramref name="cutoff"/>, except those whose
    /// normalized full path is in <paramref name="skipPaths"/>.
    /// </summary>
    /// <param name="rootPath">Directory to scan recursively.</param>
    /// <param name="extensions">
    /// Lowercase file extensions including the leading dot, e.g. <c>.mkv</c>.
    /// </param>
    /// <param name="cutoff">Files older than this are eligible for deletion.</param>
    /// <param name="skipPaths">
    /// Case-insensitive set of full paths to leave untouched (active recordings
    /// and their thumbnail companions).
    /// </param>
    /// <param name="deleteCompanionThumbnail">
    /// When <c>true</c>, deleting a media file also deletes the matching
    /// <c>.png</c> thumbnail next to it.
    /// </param>
    public static MediaCleanupRunResult CleanDirectory(
        string rootPath,
        IReadOnlyCollection<string> extensions,
        DateTime cutoff,
        IReadOnlySet<string> skipPaths,
        bool deleteCompanionThumbnail)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);
        ArgumentNullException.ThrowIfNull(extensions);
        ArgumentNullException.ThrowIfNull(skipPaths);

        var extensionSet = extensions as IReadOnlySet<string>
            ?? new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

        var deleted = new List<string>();
        var deletedThumbnails = new List<string>();
        var errors = new List<MediaCleanupRunError>();
        long bytesFreed = 0;

        if (!Directory.Exists(rootPath))
        {
            return new MediaCleanupRunResult(deleted, deletedThumbnails, bytesFreed, errors);
        }

        IEnumerable<string> files;
        try
        {
            files = Directory
                .EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extensionSet.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add(new MediaCleanupRunError(rootPath, ex));
            return new MediaCleanupRunResult(deleted, deletedThumbnails, bytesFreed, errors);
        }
        catch (IOException ex)
        {
            errors.Add(new MediaCleanupRunError(rootPath, ex));
            return new MediaCleanupRunResult(deleted, deletedThumbnails, bytesFreed, errors);
        }

        foreach (var file in files)
        {
            try
            {
                if (skipPaths.Contains(Path.GetFullPath(file)))
                {
                    continue;
                }

                var info = new FileInfo(file);
                if (info.LastWriteTime >= cutoff)
                {
                    continue;
                }

                var fileSize = info.Length;
                info.Delete();
                deleted.Add(file);
                bytesFreed += fileSize;

                if (deleteCompanionThumbnail)
                {
                    var thumbnailPath = Path.ChangeExtension(file, ".png");
                    if (File.Exists(thumbnailPath))
                    {
                        var thumbInfo = new FileInfo(thumbnailPath);
                        var thumbSize = thumbInfo.Length;
                        thumbInfo.Delete();
                        deletedThumbnails.Add(thumbnailPath);
                        bytesFreed += thumbSize;
                    }
                }
            }
            catch (IOException ex)
            {
                errors.Add(new MediaCleanupRunError(file, ex));
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add(new MediaCleanupRunError(file, ex));
            }
        }

        return new MediaCleanupRunResult(deleted, deletedThumbnails, bytesFreed, errors);
    }

    /// <summary>
    /// Removes every empty directory under <paramref name="rootPath"/>,
    /// deepest-first. The root itself is left in place. Failures are
    /// returned in the result; never thrown.
    /// </summary>
    public static MediaCleanupDirectoryResult RemoveEmptyDirectoriesBelow(
        string rootPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);

        var removed = new List<string>();
        var errors = new List<MediaCleanupRunError>();

        if (!Directory.Exists(rootPath))
        {
            return new MediaCleanupDirectoryResult(removed, errors);
        }

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory
                .EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add(new MediaCleanupRunError(rootPath, ex));
            return new MediaCleanupDirectoryResult(removed, errors);
        }
        catch (IOException ex)
        {
            errors.Add(new MediaCleanupRunError(rootPath, ex));
            return new MediaCleanupDirectoryResult(removed, errors);
        }

        foreach (var dir in dirs)
        {
            try
            {
                if (string.Equals(dir, rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    continue;
                }

                Directory.Delete(dir);
                removed.Add(dir);
            }
            catch (IOException ex)
            {
                errors.Add(new MediaCleanupRunError(dir, ex));
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add(new MediaCleanupRunError(dir, ex));
            }
        }

        return new MediaCleanupDirectoryResult(removed, errors);
    }
}