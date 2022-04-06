﻿// Distributed under the MIT license
// ===================================================
// SharpMC uses the permissive MIT license.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the “Software”), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software
// 
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ©Copyright Kenny van Vulpen - 2015

using System;

namespace SharpMC.Core
{
	internal class ConsoleFunctions
	{
		public static void WriteLine(string text)
		{
			Console.WriteLine(text);
		}

		public static void WriteLine(string text, ConsoleColor foreGroundColor)
		{
			Console.ForegroundColor = foreGroundColor;
			Console.WriteLine(text);
			Console.ResetColor();
		}

		public static void WriteLine(string text, ConsoleColor foreGroundColor, ConsoleColor backGroundColor)
		{
			Console.ForegroundColor = foreGroundColor;
			Console.BackgroundColor = backGroundColor;
			Console.WriteLine(text);
			Console.ResetColor();
		}

		public static void WriteInfoLine(string text)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("[INFO] ");
			Console.ResetColor();
			Console.Write(text + "\n");
		}

		public static void WriteErrorLine(string text)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("[ERROR] ");
			Console.ResetColor();
			Console.Write(text + "\n");
		}

		public static void WriteWarningLine(string text)
		{
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.Write("[WARNING] ");
			Console.ResetColor();
			Console.Write(text + "\n");
		}

		public static void WriteServerLine(string text)
		{
			WriteInfoLine(text);
		}

		public static void WriteDebugLine(string text)
		{
			if (ServerSettings.Debug)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[DEBUG] ");
				Console.ResetColor();
				Console.Write(text + "\n");
			}
		}
	}
}