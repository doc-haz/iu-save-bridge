using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IUSaveBridge
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0] == "--gui" || args[0] == "-g")))
            {
                IntPtr hWnd = GetConsoleWindow();
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_HIDE);
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                return 0;
            }

            // Attach to caller's console if executed from CLI
            AttachConsole(ATTACH_PARENT_PROCESS);

            Console.WriteLine("=================================================");
            Console.WriteLine("  Infinite Undiscovery Recomp Save Editor v2.3.0");
            Console.WriteLine("  Official Save Editor Companion for IU Recomp");
            Console.WriteLine("=================================================");

            if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
            {
                PrintUsage();
                return 0;
            }

            string command = args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "verify":
                        return HandleVerify(args);

                    case "set-fol":
                        return HandleSetFol(args);

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unknown command: " + args[0]);
                        Console.ResetColor();
                        PrintUsage();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERROR] " + ex.Message);
                Console.ResetColor();
                return 1;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  IU_Save_Bridge.exe                      (Launches Graphical User Interface)");
            Console.WriteLine("  IU_Save_Bridge.exe verify <file.dat>    (Inspects metadata, Fol, and checksums)");
            Console.WriteLine("  IU_Save_Bridge.exe set-fol <in> <out> <amount>");
            Console.WriteLine("\nFeatures:");
            Console.WriteLine("  - Official companion editor for Infinite Undiscovery Recomp");
            Console.WriteLine("  - Supports Recomp profiles: USA, USA-UNDUB, EUROPE, JAPAN, ASIA");
            Console.WriteLine("  - Legacy folder compatibility: NTSC-U (USA) and PAL (EUROPE)");
            Console.WriteLine("  - Edits Fol, 18 Characters (Level, EXP, HP, MP, Stats, AP, Party), and 1,023 Items");
            Console.WriteLine("  - Automated timestamped backups before writing");
            Console.WriteLine("  - 100% portable with zero external system footprint");
        }

        static int HandleVerify(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: IU_Save_Bridge.exe verify <file.dat>");
                return 1;
            }

            string path = args[1];
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found: " + path);
                return 1;
            }

            Console.WriteLine("\n[ACTION: VERIFY] " + Path.GetFullPath(path));
            byte[] header = new byte[4];
            using (FileStream fs = File.OpenRead(path))
            {
                fs.Read(header, 0, 4);
            }
            uint magic = SavePayload.ReadUInt32BE(header, 0);

            if (magic == SavePayload.ExpectedMagic)
            {
                Console.WriteLine("Format detected: Raw UDSV Save Payload (InfiniteUndiscovery.dat)");
                SavePayload p = SavePayload.FromFile(path);
                Console.WriteLine(string.Format("  Size:       {0} bytes", p.Data.Length));
                Console.WriteLine(string.Format("  Magic:      0x{0:X8} (UDSV)", p.Magic));
                Console.WriteLine(string.Format("  Version:    0x{0:X8}", p.Version));
                Console.WriteLine(string.Format("  Title ID:   0x{0:X8}", p.TitleId));
                Console.WriteLine(string.Format("  Fol:        {0:N0}", p.Fol));
                Console.WriteLine(string.Format("  CRC1:       0x{0:X8} (Match={1})", p.StoredCrc1, p.StoredCrc1 == p.CalculatedCrc1));
                Console.WriteLine(string.Format("  CRC2:       0x{0:X8} (Match={1})", p.StoredCrc2, p.StoredCrc2 == p.CalculatedCrc2));
                Console.WriteLine(string.Format("  SHA-256:    {0}", p.Sha256Hash));
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unknown format! Magic: 0x" + magic.ToString("X8") + " (expected 0x55445356 / UDSV)");
                Console.ResetColor();
                return 1;
            }
        }

        static int HandleSetFol(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: IU_Save_Bridge.exe set-fol <input.dat> <output.dat> <amount>");
                return 1;
            }

            string inputPath = args[1];
            string outputPath = args[2];
            uint newFol = uint.Parse(args[3]);

            Console.WriteLine("\n[ACTION: SET-FOL]");
            Console.WriteLine("Input:  " + Path.GetFullPath(inputPath));
            Console.WriteLine("Output: " + Path.GetFullPath(outputPath));

            SafePath.EnsureSafeOutputPath(outputPath, inputPath);

            SavePayload p = SavePayload.FromFile(inputPath);
            Console.WriteLine(string.Format("Previous Fol: {0:N0}", p.Fol));
            p.SetFol(newFol);
            Console.WriteLine(string.Format("New Fol:      {0:N0}", p.Fol));
            Console.WriteLine(string.Format("New CRC1:     0x{0:X8}", p.StoredCrc1));
            Console.WriteLine(string.Format("New CRC2:     0x{0:X8}", p.StoredCrc2));
            Console.WriteLine(string.Format("New SHA256:   {0}", p.Sha256Hash));

            p.SaveToFile(outputPath, inputPath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SUCCESS] Updated save written safely to: " + Path.GetFullPath(outputPath));
            Console.ResetColor();
            return 0;
        }
    }
}
