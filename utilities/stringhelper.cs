using System.IO;
using System;

namespace utilities
{
	public class stringhelper
	{		
		/// <summary>
		/// Finds the first trimmed line that equals s
		/// </summary>
		/// <param name="sr">StreamReader of a VRML 1.0 compliant file</param>
		/// <param name="s">string to search for within the StreamReader</param>
		/// <returns>true if string was found and StreamReader was set 
		/// successfully, false otherwise.</returns>
		public static bool LookFor(StreamReader sr, string s, ref int nLineCount)
		{
			// Keep reading from the StreamReader until the string s is found
			// or EOF.  Return true if the string is found, false if not.
			// Hints:  To read in a line:   string inLine = sr.ReadLine();
			// If  inLine is null, that means EOF.  
			// To check is the string appears:  if( inLine.IndexOf(s) >= 0 )

			string inLine = null;
			int nOrigLineCount = nLineCount;
			string sTrimmed = s.Trim();

			inLine = sr.ReadLine();
			while (inLine != null)
			{
				nLineCount++;
				inLine = inLine.Trim();
				if (inLine.Equals(sTrimmed))
				{
					return true;
				}
				inLine = sr.ReadLine();
			}

			nLineCount = nOrigLineCount;

			ReadThroughLine(sr, nLineCount);

			return false;
		}

		/// <summary>
		/// Finds first occurence of sTarget in file sr is attached to. Returns the full line.
		/// <returns> true if found, false otherwise </returns>
		/// </summary>
		public static bool FindFirstOccurence(StreamReader sr, string sTarget, ref string sFullLine, ref int nLineCount)
		{
			string inLine = null;
			int nOrigLineCount = nLineCount;

			inLine = sr.ReadLine();
			while (inLine != null)
			{
				nLineCount++;
				if (inLine.Contains(sTarget))
				{
					sFullLine = inLine;
					return true;
				}

				inLine = sr.ReadLine();
			}
			nLineCount = nOrigLineCount;

			ReadThroughLine(sr, nLineCount);

			return false;
		}

		/// <summary>
		/// Read certain number of lines starting from beginning of stream
		/// </summary>
		/// <param name="sr">stream</param>
		/// <param name="nLine">number of lines to read</param>
		public static void ReadThroughLine(StreamReader sr, int nLine)
		{
			sr.BaseStream.Seek(0, 0);

			int n = 0;
			while(n < nLine) 
			{
				sr.ReadLine();
				n++;
			}
		}

		/// <summary>
		/// Breaks apart a string on the delimiter
		/// </summary>
		/// <param name="sValue">string to break up</param>
		/// <param name="delimiter">character to break string over</param>
		/// <returns>array of tokens</returns>
		public static string[] Tokenize(string sValue, char delimiter) 
		{
			return sValue.Split(new Char[] { delimiter });
		}

		/// <summary>
		/// Returns index of a specified token in a list of strings.
		/// </summary>
		/// <param name="?"></param>
		/// <returns>Index if found and -1 if target is missing</returns>
		public static int FindToken(string[] tokens, string sTarget)
		{
			for (int i = 0; i < tokens.Length; i++)
			{
				if (tokens[i] == sTarget) return i;
			}
			return -1;
		}

		/// <summary>
		/// Count the number of lines in the file specified.
		/// </summary>
		/// <param name="f">The filename to count lines in.</param>
		/// <returns>The number of lines in the file.</returns>
		public static long CountLinesInFile(string f)
		{
			long count = 0;
			using (StreamReader r = new StreamReader(f))
			{
				string line;
				while ((line = r.ReadLine()) != null)
				{
					count++;
				}
			}
			return count;
		}
	}
}
