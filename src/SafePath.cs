using System;
using System.IO;

namespace IUSaveBridge
{
    public static class SafePath
    {
        public static void EnsureSafeOutputPath(string targetPath, string inputPath = null)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentException("Target path cannot be null or empty.", "targetPath");

            string fullTarget = Path.GetFullPath(targetPath).TrimEnd('\\');
            string fullInput = !string.IsNullOrEmpty(inputPath) ? Path.GetFullPath(inputPath).TrimEnd('\\') : null;

            // Rule 1: In CLI modes, prevent accidental destructive overwrite when specifying distinct files
            if (fullInput != null && string.Equals(fullTarget, fullInput, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SAFETY ERROR: Target output file cannot be the same as the input file!\n" +
                    "Input:  " + fullInput + "\n" +
                    "Target: " + fullTarget);
            }

            string dir = Path.GetDirectoryName(fullTarget);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void AtomicSave(byte[] data, string targetPath)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException("targetPath");

            string fullTarget = Path.GetFullPath(targetPath);
            string dir = Path.GetDirectoryName(fullTarget);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempPath = fullTarget + ".tmp_" + Guid.NewGuid().ToString("N");

            try
            {
                // Write to temp file first
                File.WriteAllBytes(tempPath, data);

                // Verify temp file on disk
                FileInfo fi = new FileInfo(tempPath);
                if (fi.Length != data.Length)
                {
                    throw new IOException(string.Format("Temp write verification failed. Wrote {0} bytes, expected {1}", fi.Length, data.Length));
                }

                // Overwrite target file
                if (File.Exists(fullTarget))
                {
                    File.Delete(fullTarget);
                }
                File.Move(tempPath, fullTarget);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }
}
