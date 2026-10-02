using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace AmazTool;

internal class UpgradeApp
{
    public static void Upgrade(string fileName)
    {
        Console.WriteLine($"{Resx.Resource.StartUnzipping}\n{fileName}");

        Utils.Waiting(5);

        if (!File.Exists(fileName))
        {
            Console.WriteLine(Resx.Resource.UpgradeFileNotFound);
            return;
        }

        Console.WriteLine(Resx.Resource.TryTerminateProcess);
        try
        {
            var existing = Process.GetProcessesByName(Utils.V2rayN);
            foreach (var pp in existing)
            {
                var path = pp.MainModule?.FileName ?? "";
                if (path.StartsWith(Utils.GetPath(Utils.V2rayN)))
                {
                    pp?.Kill();
                    pp?.WaitForExit(1000);
                }
            }
        }
        catch (Exception ex)
        {
            // Access may be denied without admin right. The user may not be an administrator.
            Console.WriteLine(Resx.Resource.FailedTerminateProcess + ex.StackTrace);
        }

        Console.WriteLine(Resx.Resource.StartUnzipping);
        StringBuilder sb = new();
        var aborted = false;
        var selfRenamed = false;
        var thisAppOldFile = $"{Utils.GetExePath()}.tmp";
        try
        {
            File.Delete(thisAppOldFile);
            var splitKey = "/";

            using var archive = ZipFile.OpenRead(fileName);
            foreach (var entry in archive.Entries)
            {
                try
                {
                    if (entry.Length == 0)
                    {
                        continue;
                    }

                    Console.WriteLine(entry.FullName);

                    var lst = entry.FullName.Split(splitKey);
                    if (lst.Length == 1)
                    {
                        continue;
                    }

                    var fullName = string.Join(splitKey, lst[1..lst.Length]);

                    if (string.Equals(Utils.GetExePath(), Utils.GetPath(fullName), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Move(Utils.GetExePath(), thisAppOldFile);
                        selfRenamed = true;
                    }

                    var entryOutputPath = ArchivePathGuard.ResolveUnderRoot(Utils.StartupPath(), fullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(entryOutputPath)!);
                    //In the bin folder, if the file already exists, it will be skipped.
                    //Match the directory, not the prefix: "bin.zip" is not inside "bin/".
                    if ((fullName == "bin" || fullName.StartsWith("bin/")) && File.Exists(entryOutputPath))
                    {
                        continue;
                    }

                    if (!TryExtractToFile(entry, entryOutputPath))
                    {
                        throw new IOException($"Failed to extract update entry '{entry.FullName}'.");
                    }

                    Console.WriteLine(entryOutputPath);
                }
                catch (Exception ex)
                {
                    // A single failed entry leaves the install tree half-updated; that is not a
                    // successful update and must never reach StartV2RayN().
                    aborted = true;
                    sb.AppendLine(ex.ToString());
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            aborted = true;
            sb.Append(ex.ToString());
        }
        if (aborted)
        {
            RestoreSelfIfRenamed(selfRenamed, thisAppOldFile);
            Console.WriteLine(Resx.Resource.FailedUpgrade + sb.ToString());
            Utils.Waiting(3); // the caller may have closed the console before this is readable
            return;
        }

        Console.WriteLine(Resx.Resource.Restartv2rayN);
        Utils.Waiting(2);

        Utils.StartV2RayN();
    }

    /// <summary>
    /// The updater renames the running AmazTool.exe to AmazTool.exe.tmp before overwriting itself.
    /// If the update then fails, that rename has to be undone or the application is left without a
    /// launcher until the user restores it by hand.
    /// </summary>
    private static void RestoreSelfIfRenamed(bool selfRenamed, string thisAppOldFile)
    {
        if (!selfRenamed)
        {
            return;
        }

        try
        {
            if (!File.Exists(thisAppOldFile))
            {
                return;
            }

            var target = Utils.GetExePath();
            try
            {
                File.Move(thisAppOldFile, target);
            }
            catch
            {
                // The new AmazTool.exe is in the way (possibly only partially written). Delete it
                // only now, so there is never a moment where no executable exists at all.
                File.Delete(target);
                File.Move(thisAppOldFile, target);
            }
            Console.WriteLine(Resx.Resource.RestoreUpgradeSelf);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Resx.Resource.FailedRestoreUpgradeSelf + ex.Message);
        }
    }

    private static bool TryExtractToFile(ZipArchiveEntry entry, string outputPath)
    {
        var retryCount = 5;
        var delayMs = 1000;

        for (var i = 1; i <= retryCount; i++)
        {
            try
            {
                entry.ExtractToFile(outputPath, true);
                return true;
            }
            catch
            {
                Thread.Sleep(delayMs * i);
            }
        }
        return false;
    }
}
