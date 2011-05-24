//  
//  Main.cs
//  
//  Author:
//       Carl Kittelberger <icedream2k9@die-optimisten.net>
// 
//  Copyright (c) 2011 (c) 2011 Carl Kittelberger
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.IO;
using Icedream.Binsembler;
using System.Diagnostics;

namespace bstest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Binsembler test tool");
			Console.WriteLine ();
			
			Console.WriteLine("Version info:");
			Process pr = new Process();
			
			pr.StartInfo = new ProcessStartInfo(System.IO.Path.Combine(Environment.CurrentDirectory, "binsembler.exe"), "--version");
			pr.StartInfo.UseShellExecute = false;
			pr.StartInfo.RedirectStandardOutput = true;
			pr.Start();
			while(!pr.StandardOutput.EndOfStream)
			{
				Console.WriteLine("[binsembler] " + pr.StandardOutput.ReadLine());
			}
			Console.WriteLine();
			
			Console.WriteLine("(A1) Library: Instanced standard compilation (One-Line-Conversion)");
			new Binsembler().Compile("melodies1.mp3");
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(A2) Library: Instanced 16-bit standard compilation");
			Binsembler conv = new Binsembler();
			conv.BitFormat = BitFormat.SixteenBit;
			conv.Compile("melodies1.mp3");
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(A3) Library: Instanced decimal compilation");
			conv.BitFormat = BitFormat.EightBit;
			conv.Compile("melodies1.mp3");
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(A4) Library: Instanced decimal standard compilation");
			conv.BitFormat = BitFormat.SixteenBit;
			conv.ValueFormat = ValueFormat.Decimal;
			conv.Compile("melodies1.mp3");
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(B1) Executable: Standard compilation");
			pr.StartInfo.Arguments = "melodies1.mp3";
			pr.Start();
			pr.WaitForExit();
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(B1) Executable: 16-bit standard compilation");
			pr.StartInfo.Arguments = "-w melodies1.mp3";
			pr.Start();
			pr.WaitForExit();
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(B1) Executable: Decimal compilation");
			pr.StartInfo.Arguments = "--format=d melodies1.mp3";
			pr.Start();
			pr.WaitForExit();
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("(B1) Executable: 16-bit decimal compilation");
			pr.StartInfo.Arguments = "--format=d -w melodies1.mp3";
			pr.Start();
			pr.WaitForExit();
			ParseFile("melodies1.mp3.txt");
			
			Console.WriteLine("========================== FINISHED =============================");
			
			Console.ReadKey();
		}
		
		public static void ParseFile (string outputFile)
		{
			long resultBytes;
			
			if(!File.Exists(outputFile))
			{
				Console.WriteLine("\tERROR: Output file does not exist - it has not been created eventually due to an error...");
				return;
			}
			if((resultBytes = new FileInfo(outputFile).Length) == 0)
			{
				Console.WriteLine("\tERROR: Output file contains too few bytes (" + resultBytes.ToString() + " Bytes) to be valid  - it has not been filled eventually due to an error...");
				return;
			}
			
			string[] contents = File.ReadAllLines(outputFile);
			string line = contents[0];
			string[] spl = line.Split(' ');
			string command = spl[0];
			string[] values = line.Substring(command.Length + 1).Split(',');
			
			int shex = 0;
			int hex = 0;
			int dec = 0;
			int octal = 0;
			int binary = 0;
			
			foreach(string v in values)
			{
				switch(v.Substring(0,1))
				{
				case "$":
					shex++;
					break;
				case "0":
					if(v.Substring(0,2).ToLower() == "0x")
						hex++;
					break;
				case "o":
					octal++;
					break;
				case "b":
					binary++;
					break;
				default:
					dec++;
					break;
				}
			}
			
			int total = shex + hex + octal + binary + dec;
			
			Console.WriteLine("\tContained line count:\t" + contents.Length);
			
			Console.Write("\tValue structure:\t");
			Console.WriteLine(
				contents[0].StartsWith(".dw ") ? "16-bit" :
				contents[0].StartsWith(".db ") ? "8-bit" :
				"unknown");
			Console.WriteLine("\tValue counts:\t");
			Console.WriteLine("\t\t  $hex    \t" + shex + "\t" + getPercent(shex, total) + "%");
			Console.WriteLine("\t\t+ 0xhex    \t" + hex + "\t" + getPercent(hex, total) + "%");
			Console.WriteLine("\t\t+ octal  \t" + octal + "\t" + getPercent(octal, total) + "%");
			Console.WriteLine("\t\t+ binary  \t" + binary + "\t" + getPercent(binary, total) + "%");
			Console.WriteLine("\t\t+ decimal/?\t" + dec + "\t" + getPercent(dec, total) + "%");
			Console.WriteLine("\t\t==========\t=========");
			Console.WriteLine("\t\t  TOTAL   \t" + total + "\t100%");
			
		}
		
		private static decimal getPercent(int v, int m)
		{
			return 100 * v / m;
		}
	}
}
