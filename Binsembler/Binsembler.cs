using System;
using System.IO;

namespace Icedream.Binsembler
{
    public enum BitFormat
    {
        EightBit = 8,
        SixteenBit = 16
    }

    public enum ValueFormat
    {
        Binary = 2,

        Octal = 8,
        Decimal = 10,

        ShortHexadecimal = 16,
        FullHexadecimal = 17
    }

    public class Binsembler
    {
        // Tipp: "propfull" tippen und 2x die Tabulatortaste drücken :-)
        private int _bufferLength = 1024;
        private BitFormat _bitFormat = BitFormat.EightBit;
        private ValueFormat _valueFormat = ValueFormat.ShortHexadecimal;
        private string format;
        private bool _outputZeroEnd = true;

        /// <summary>
        /// Should a zero byte be outputted at the end? (Only 8-bit)
        /// </summary>
        /// <value>true</value>
        public bool OutputZeroEnd
        {
            get { return _outputZeroEnd; }
            set { _outputZeroEnd = value; }
        }
        

        /// <summary>
        /// The value format, or how the value should be outputted.
        /// </summary>
        /// <value><seealso cref="ValueFormat.ShortHexadecimal"/></value>
        public ValueFormat ValueFormat
        {
            get { return _valueFormat; }
            set { _valueFormat = value; }
        }
        
        /// <summary>
        /// The bit format, or how many bits should be defined in one value.
        /// </summary>
        /// <value><seealso cref="BitFormat.EightBit"/></value>
        public BitFormat BitFormat
        {
            get { return _bitFormat; }
            set { _bitFormat = value; }
        }
        
        /// <summary>
        /// IO buffer length in bytes.
        /// </summary>
        /// <value>1024</value>
        public int BufferLength
        {
            get { return _bufferLength; }
            set { _bufferLength = value; }
        }
        
        
         

        public delegate void StatusHandler(LogEventArgs e);
        public event StatusHandler Status;

        public void OnStatus(string status, bool up = false, string module = "bin2db")
        {
            int u = Console.CursorTop;
            Console.WriteLine("[" + module + "] " + status);
            if (up) Console.CursorTop = u;
            if (Status != null)
            {
                LogEventArgs e = new LogEventArgs();
                e.Module = module;
                e.Text = status;
                Status(e);
            }
        }

        internal int _getStructureNumberHex(BitFormat bfmt)
        {
            return (
                
                bfmt == Icedream.Binsembler.BitFormat.SixteenBit ? 4

                : 2);
        }

        public void Compile(string path, string target = null,
            long start = 0, long end = -1)
        {
            // Some checks before we start
            if (this._bitFormat == Icedream.Binsembler.BitFormat.SixteenBit && this._bufferLength / 2 != Math.Round((decimal)this._bufferLength / 2))
                throw new ArgumentException("Buffer length (set to: " + this.BufferLength.ToString() + " must be a multiplication with two in 16-bit-mode.");
			if (target == null)
				target = path + ".txt";

            this.format = _compileFormat(this._valueFormat);
            this.format = this.format.Replace("[hex-structure]", _getStructureNumberHex(this._bitFormat).ToString());

            StreamWriter sw = null;
            try
            {
                // Opening input file
                OnStatus("\tOPENING\t" + path);
                Stream str = File.OpenRead(path);

                // Creating output file
                OnStatus("\tCREATING\t" + target);
                sw = new StreamWriter(target);
                sw.AutoFlush = true;

                // Auto-filling optional argument for end position (it's usually -1 => end of file)
                if (end == -1) end = (long)str.Length;
               
                // Seek to given start (it's usually 0)
                str.Seek((long)start, SeekOrigin.Begin);

                // Writing the command
                sw.Write(_compileCommand(this._bitFormat) + " ");

                // Here we mark the current total position for later stats
                int gpos = 0;
                string komma = ""; // later, the , is stored here.
                int OldBufferLength = BufferLength; // we need this just for console purpose
                while (str.Position < end)
                {
                    // Tolerating buffer length
                    if (end - str.Position < BufferLength)
                        BufferLength = (int)(end - str.Position);
					if(this.BitFormat == BitFormat.SixteenBit && BufferLength / 2 != Math.Round((decimal)BufferLength / 2))
						BufferLength++;// Adjustment for 16-bit (length must be n x 2)
					
                    // Generating buffer & marking current position
                    byte[] buff = new byte[BufferLength];
                    gpos += buff.Length;
						
					
                    // Stats
                    if (gpos != 0)
                    {
                        OnStatus("\tSTATUS\tcbuflen=" + buff.Length + " size=" + gpos + " percent=" + Math.Round((decimal)(100 * gpos / (end - start))).ToString() + " pos=" + str.Position + " len=" + str.Length, BufferLength == OldBufferLength);
                    }

                    // Read into buffer
                    str.Read(buff, 0, (int)buff.Length);

                    // Now outputting the bytes & bits :-)
                    if (this._bitFormat == Icedream.Binsembler.BitFormat.EightBit)
                    {
                        foreach (byte b in buff)
                        {
                            // Convert byte
                            string f = string.Format(format, b, Convert.ToString(b, 2), Convert.ToString(b, 8), null, null);
                            // Output
                            sw.Write(komma + f);
                            if(komma == "") komma = ",";
                        }
                    }
                    else
                    {
                        for (int p = 0; p < buff.Length; p ++)
                        {
                            // Concat 2 bytes into 16 bit
                            int fv = this._concatBytes16(buff[p], buff[++p]);
                            // Convert
                            string f = string.Format(format, fv, Convert.ToString(fv, 2), Convert.ToString(fv, 8), null, null);
                            // Output
                            sw.Write(komma + f);
                            if(komma == "") komma = ",";
                        }
                    }
					
					_bufferLength = OldBufferLength;
                }

                // Last byte, can be disabled
                if(_outputZeroEnd && _bitFormat == Icedream.Binsembler.BitFormat.EightBit)
                    sw.Write("," + string.Format(format, 0, "00", "0"));


                long l = sw.BaseStream.Length;
                sw.Close();
                OnStatus("\tCLOSING\t" + path);
                OnStatus("\tFINISHED\tOriginalBytes=" + (end - start) + " OutputBytes=" + l.ToString() + " Ratio=" + (l / (end - start)) + " OriginalPath=" + path + " TargetPath=" + target);
            }
            catch (Exception x)
            {
                if (sw != null)
                    if (sw.BaseStream != null)
                        if (sw.BaseStream.CanWrite)
                        {
                            OnStatus("\tCLOSING\t" + path);
                            sw.Close();
                        }
                OnStatus("\tERROR\t" + x.Message);
                throw x;
            }
        }

        internal static string _compileFormat(ValueFormat vfmt)
        {

            /**
             * Available Inputs
             * ----------------------------------
             * 0: Standard value
             * 1: Binary value
             * 2: Octal value
             * ----------------------------------
             * 3: Base32 value (not implemented)
             * 4: Base64 value (not implemented)
             * ----------------------------------
             * [hex-structure]: 2 or 4,
             *   dependent from structure being
             *   used.
             */
            switch (vfmt)
            {
                case ValueFormat.ShortHexadecimal:
                    //Console.WriteLine("Format: $hex (base: 16)");
                    return "${0:x[hex-structure]}";

                case ValueFormat.FullHexadecimal:
                    //Console.WriteLine("Format: 0xHEX (base: 16)");
                    return "0x{0:x[hex-structure]}";

                case ValueFormat.Binary:
                    //Console.WriteLine("Format: Binary (base: 2)");
                    return "b{1}";

                case ValueFormat.Octal:
                    //Console.WriteLine("Format: Octal (base: 8)");
                    return "o{2}";

                case ValueFormat.Decimal:
                default:
                    return "{0}";
            }
        }

        internal string _compileCommand(BitFormat bfmt)
        {
            return (

                bfmt == Icedream.Binsembler.BitFormat.SixteenBit ? ".dw"  // 16 bit => 1 Word => w => .dw

                : ".db");
        }

        /// <summary>
        /// Converts to bytes into 16-bit as an integer.
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        /// <returns>16-bit converted data as Integer</returns>
        internal int _concatBytes16(byte b1, byte b2)
        {
            return Convert.ToInt32(
                // first we convert all bytes to binaries, while filling them till 8 bits.
                // then we concat all binary data, so that we have 16 bits.
                _fillBits(Convert.ToString(b1, 2), 8) + _fillBits(Convert.ToString(b2, 2), 8),

                // now we convert the binary into an integer for easy use
                2

                // and that's all :-)
                );
        }

        /// <summary>
        /// This fills up the binary to a specific count of bits
        /// </summary>
        /// <param name="binary">The binary string (00101...)</param>
        /// <param name="targetbits">Target binary bit count.
        /// <value>8</value>
        /// </param>
        /// <returns></returns>
        internal string _fillBits(string binary, int targetbits = 8)
        {
            while (binary.Length < targetbits)
                binary = "0" + binary;
            return binary;
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Module = "bin2db";
        public string Text = "Ready";
    }
}
