
using System;

namespace NextGenSoftware.CLI.Engine
{
	public class ConsoleSpiner
	{
		int counter;
		public ConsoleSpiner()
		{
			counter = 0;
		}
		public void Turn()
		{
			counter++;
			switch (counter % 4)
			{
				case 0: Console.Write("/"); break;
				case 1: Console.Write("-"); break;
				case 2: Console.Write("\\"); break;
				case 3: Console.Write("|"); break;
			}
			Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
		}
	}


	public class Animations
	{
		int counter;

		public Animations()
		{
			counter = 0;
		}

		public void Turn(string loadingText)
		{
			//Console.SetCursorPosition(loadingText.Length, Console.CursorTop);
			
			switch (counter % 4)
			{
				case 0: Console.Write("/"); break;
				case 1: Console.Write("-"); break;
				case 2: Console.Write("\\"); break;
				case 3: Console.Write("|"); break;
			}

			int left = Console.CursorLeft - 1;

			if (left < 0)
				left = 0;

			Console.SetCursorPosition(left, Console.CursorTop);
		}

		public void SequencedMatrix(int row, int column, int width, int height)
		{
			Console.SetCursorPosition(column, row);

			int spot = counter % width;
			int[,] array = new int[width, height];

			for (int x = 0; x < array.GetLength(0); x++)
			{
				for (int y = 0; y < array.GetLength(0); y++)
				{
					if (y == spot)
					{
						Console.Write("[X]");
						continue;
					}

					Console.Write("[ ]");
				}
				Console.WriteLine("");
			}
		}

		public void LoadingBar(string loadingTest, int row, int column)
		{
			Console.SetCursorPosition(column, row);
			string loadingText = loadingTest + " [";
			string loadingTextTerminator = "                   ]";
			Console.Write(loadingText + loadingTextTerminator);

			for (int i = 0; i < counter % 20; i++)
			{
				if (counter == 0)
				{
					Console.SetCursorPosition(column, row);
					Console.Write(loadingText + loadingTextTerminator);
				}

				Console.SetCursorPosition(loadingText.Length + i, row);
				Console.Write("*");
			}
		}

		public void Ready()
		{
			counter++;
			System.Threading.Thread.Sleep(75);
		}
	}
}
