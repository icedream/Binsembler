using System;
using System.IO;
using System.Windows.Forms;

namespace Icedream.Binsembler
{

   
    public class Executable
    {

        static string AssemblyName
        {
            get { return System.Diagnostics.Process.GetCurrentProcess().ProcessName; }
        }
        static void Usage()
        {
            Console.WriteLine("Binsembler");
            Console.WriteLine("\t(c) 2011 Carl Kittelberger");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("\t" + AssemblyName + " [options] inputfile");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("\t-o=file, --output-file file");
            Console.WriteLine("\t\tSets output file.");
            Console.WriteLine("\t-b=value, --buffer-length value");
            Console.WriteLine("\t\tSets IO buffer length.");
            Console.WriteLine("\t-w, --use-word");
            Console.WriteLine("\t\tEnter 16-bit (word/.dw) mode.");
            Console.WriteLine("\t-f={$|0x|b|o|d}, --use-format {shex|hex|binary|octal|decimal}");
            Console.WriteLine("\t\tValue format to use.");
            Console.WriteLine("\t-s=value, --start=value");
            Console.WriteLine("\t\tWhere to start in input file. In bytes.");
            Console.WriteLine("\t-e=value, --end=value");
            Console.WriteLine("\t\tWhere to end in input file. In bytes.");
            Console.WriteLine("\t-l, --license");
            Console.WriteLine("\t\tOnly print license info and exit.");
            Console.WriteLine("\t-V, --version");
            Console.WriteLine("\t\tOnly print version info and exit.");
            return;
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [MTAThread]
        static int Main(string[] args)
        {

            foreach (string a in args)
            {
                string[] spl = a.Split('=');
                string name = spl[0].ToLower();
                //string value = spl.Length > 1 ? a.Substring(name.Length + 1) : null;
                switch (name)
                {
                    case "-V":
                    case "--version":
                        Console.WriteLine("Binsembler");
                        Console.WriteLine("\t(c) 2011 Carl Kittelberger");
                        Console.WriteLine("\tVersion " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                        Console.WriteLine("\tBuild for architecture " + System.Reflection.Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture);
                        Console.WriteLine("\tBuild flags: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Flags.ToString());
                        Console.WriteLine();
                        return 0;

                    case "-l":
                    case "--license":
                        if (File.Exists("LICENSE"))
                        {
                            Console.WriteLine(File.ReadAllText("LICENSE"));
                        }
                        else
                        {
                            Console.WriteLine("Warning: No license file found - deleted?");
                        }
                        return 0;

                    case "-h":
                    case "--help":
                        Usage();
                        return 0;
                }
            }

            

            if (args.Length == 0)
            {
                Console.WriteLine("Activating GUI...");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Console.WriteLine("GUI now running.");
                Application.Run(new Main());
            }
            else
            {
                string input = "";
                string output = "";
                int start = 0;
                int end = -1;


                Binsembler conv = new Binsembler();

                foreach (string a in args)
                {
                    string[] spl = a.Split('=');
                    string name = spl[0].ToLower();
                    string value = spl.Length > 1 ? a.Substring(name.Length + 1) : null;
                    switch (name)
                    {
                        case "--output-file":
                        case "--output":
                        case "-o":
                            output = value;
                            break;
                        case "--buffer-length":
                        case "--bufflen":
                        case "-b":
                            int x = 0;
                            int.TryParse(value, out x);
                            if (x <= 0)
                            {
                                Console.WriteLine("Warning: Invalid buffer length given, applying standard value.");
                            } else {
                                conv.BufferLength = x;
                            }
                            break;
                        case "-w":
                        case "--word":
                        case "--use-word":
                            Console.WriteLine("16-bit mode activated.");
                            conv.BitFormat = BitFormat.SixteenBit;
                            break;
                        case "--no-zero":
                        case "-0-":
                        case "--without-zero":
                            Console.WriteLine("No ending zero byte.");
                            conv.OutputZeroEnd = false;
                            break;
                        case "--zero":
                        case "-0+":
                        case "--with-zero":
                            Console.WriteLine("With ending zero byte.");
                            conv.OutputZeroEnd = true;
                            break;
                        case "--use-format":
                        case "--format":
                        case "-f":
                            /**
                             * $0: Standard value
                             * $1: Binary value
                             * $2: Octal value
                             * $3: Base32 value (not implemented)
                             * $4: Base64 value (not implemented)
                             */
                            switch (value.ToLower())
                            {
                                case "$hex":
                                case "$hexadecimal":
                                case "$":
                                    conv.ValueFormat = ValueFormat.ShortHexadecimal;
                                    break;
                                case "hex":
                                case "hexadecimal":
                                case "0x":
                                    conv.ValueFormat = ValueFormat.FullHexadecimal;
                                    break;
                                case "b":
                                case "bin":
                                case "binary":
                                    conv.ValueFormat = ValueFormat.Binary;
                                    break;
                                case "o":
                                case "octal":
                                    conv.ValueFormat = ValueFormat.Octal;
                                    break;
                                case "d":
                                case "decimal":
                                    conv.ValueFormat = ValueFormat.Decimal;
                                    break;
                                default:
                                    Console.WriteLine("WARNING: Unknown value format, applying standard value format.");
                                    break;
                            }
                            break;
                        case "--start-byte":
                        case "--start":
                        case "-s":
                            int y = 0;
                            int.TryParse(value, out y);
                            if (y <= 0)
                            {
                                Console.WriteLine("Warning: Invalid start position given, applying standard value.");
                            }
                            else
                            {
                                start = y;
                            }
                            break;
                        case "--end-byte":
                        case "--end":
                        case "-e":
                            int z = 0;
                            int.TryParse(value, out z);
                            if (z <= 0)
                            {
                                Console.WriteLine("Warning: Invalid end position given, applying standard value.");
                            }
                            else
                            {
                                end = z;
                            }
                            break;
                    }
                }

                


                input = args[args.Length - 1];
                if (output.Trim() == "")
                    output = input + ".txt";
                
                /*
                Console.WriteLine("Input file: \t" + input);
                Console.WriteLine("Output file: \t" + output);
                Console.WriteLine("Buffer length:\t" + bufflen.ToString() + " Bytes per buffer");
                Console.WriteLine();
                 */
				try
				{
                	conv.Compile(input, output, start, end);
				}
				catch(Exception n)
				{
					Console.WriteLine("Compiler error: " + n.Message);
				}
            }

            return 0;
        }
    }
}
