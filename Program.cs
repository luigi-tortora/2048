using System;
using System.Collections.Generic;
using System.Threading;

public class Program
{
    public static void Main(string[] args)
    {
        const int Max = 2048;

        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.CursorVisible = false;

        Console.TreatControlCAsInput = true;

        Console.SetWindowPosition(0, 0);

        Console.WindowWidth = 1 + Grid.GridWidth + 1;
        Console.WindowHeight = 1 + Grid.GridHeight + 1;

        Console.BufferWidth = Console.WindowWidth;
        Console.BufferHeight = Console.WindowHeight;

        Console.Clear();

        Grid grid = new();

        grid.PrintInit(1, 1);

        //grid.Dbg(0, 4, 4, 2, 2); // TODO: .

        grid.TryFill();
        grid.TryFill();

        grid.PrintUpdate(1, 1);

        int totalScore = 0;
        int max = grid.GetMax();

        Console.Title = $"- {Max} - [Score: {totalScore}] [Max: {max}]";

        bool exit = false;
        GameOver gameOver = GameOver.None;

        while (!exit && gameOver != GameOver.YouLose)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo cKI = Console.ReadKey(true);
                ThreadPool.QueueUserWorkItem((_) => { while (Console.KeyAvailable && Console.ReadKey(true) == cKI); });

                switch (cKI.Key)
                {
                    //case ConsoleKey.UpArrow: // TODO: .
                    //case ConsoleKey.DownArrow:
                    //case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                    {
                        if (grid.TryMove(GetDirectionByConsoleKey(cKI.Key), out int score))
                        {
                            grid.TryFill();

                            grid.PrintUpdate(1, 1);

                            totalScore += score;
                            max = grid.GetMax();

                            if (max != Max) // max < Max || max > Max
                            {
                                Console.Title = $"- {Max} - [Score: {totalScore}] [Max: {max}]";

                                Program.BeepAsync(frequencyA: score == 0 ? 800 : 1000, duration: 250);
                            }
                            else // max == Max
                            {
                                gameOver = GameOver.YouWin;

                                Console.Title = $"- {Max} - [Score: {totalScore}] [You Win!]";

                                BeepAsync(frequencyA: 1000, frequencyB: 1500, duration: 500, ramp: true);
                            }
                        }
                        else
                        {
                            if (false) // TODO: Simulate TryMove() for the other directions; if they all return false:
                            {
                                gameOver = GameOver.YouLose;

                                Console.Title = $"- {Max} - [Score: {totalScore}] [Max: {max}] [Game Over: You Lose!]";

                                BeepAsync(frequencyA: 1000, frequencyB: 500, duration: 500, ramp: true);

                                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                            }
                        }

                        break;
                    }

                    case ConsoleKey.Escape:
                    {
                        exit = true;

                        break;
                    }
                }
            }
        }

        Console.OutputEncoding = System.Text.Encoding.Default;
        Console.CursorVisible = true;

        Console.TreatControlCAsInput = false;

        Console.Clear();
        Console.ResetColor();
    }

    public static Direction GetDirectionByConsoleKey(ConsoleKey consoleKey)
    {
        return consoleKey switch
        {
            ConsoleKey.UpArrow    => Direction.Up,
            ConsoleKey.DownArrow  => Direction.Down,
            ConsoleKey.LeftArrow  => Direction.Left,
            ConsoleKey.RightArrow => Direction.Right,
            _ => throw new ArgumentException(nameof(consoleKey))
        };
    }

    public static void Write(string str, int left, int top,
        ConsoleColor foregroundColor = ConsoleColor.Gray,
        ConsoleColor backgroundColor = ConsoleColor.Black)
    {
        Console.ForegroundColor = foregroundColor;
        Console.BackgroundColor = backgroundColor;

        Console.SetCursorPosition(left, top);
        Console.Write(str);

        Console.ResetColor();
    }

    public static void BeepAsync(int frequencyA = 800, int frequencyB = 0, int duration = 200, bool ramp = false)
    {
        ThreadPool.QueueUserWorkItem((_) =>
        {
            if (!ramp)
            {
                if (frequencyA != 0)
                {
                    Console.Beep(frequencyA, frequencyB == 0 ? duration : duration / 2);
                }

                if (frequencyB != 0)
                {
                    Console.Beep(frequencyB, frequencyA == 0 ? duration : duration / 2);
                }
            }
            else
            {
                const int Steps = 4;

                int frequencyStep = (frequencyA - frequencyB) / (Steps - 1);
                duration /= Steps;

                for (int i = 1; i <= Steps; i++)
                {
                    if (frequencyA != 0)
                    {
                        Console.Beep(frequencyA, duration);
                    }

                    frequencyA -= frequencyStep;
                }
            }
        });
    }
}

public enum Direction { Up, Down, Left, Right };

public enum GameOver { None, YouWin, YouLose }

public class Grid
{
    public const int GridWidth = 29;
    public const int GridHeight = 17;
    public const int Size = 4;

    private readonly int[,] _grid;

    private readonly Random _rnd;
    private readonly List<int> _lst;

    public Grid()
    {
        _grid = new int[Size, Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                _grid[y, x] = 0;
            }
        }

        _rnd = new();
        _lst = new();
    }

    public void Dbg(int y, int a, int b, int c, int d) // TODO: .
    {
        _grid[y, 0] = a;
        _grid[y, 1] = b;
        _grid[y, 2] = c;
        _grid[y, 3] = d;
    }

    public bool TryMove(Direction direction, out int score, bool simulate = false) // TODO: Try & simulate.
    {
        SeparationStep(direction);
        MergingStep(direction, out score);
        SeparationStep(direction);

        return true;
    }

    public bool TryFill()
    {
        if (GetEmptyCells() >= 1)
        {
            while (true)
            {
                int y = _rnd.Next(0, Size);
                int x = _rnd.Next(0, Size);

                if (_grid[y, x] == 0)
                {
                    int value = _rnd.Next(0, Size);

                    _grid[y, x] = value <= 2 ? 2 : 4;

                    return true;
                }
            }
        }

        return false;
    }

    public int GetMax()
    {
        int max = 0;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                max = Math.Max(max, _grid[y, x]);
            }
        }

        return max;
    }

    private void SeparationStep(Direction direction) // of non-zero values, preserving their order, from the others.
    {
        switch(direction)
        {
            case Direction.Up:
            {
                // TODO: .

                break;
            }

            case Direction.Down:
            {
                // TODO: .

                break;
            }

            case Direction.Left:
            {
                // TODO: .

                break;
            }

            case Direction.Right:
            {
                for (int y = 0; y < Size; y++)
                {
                    _lst.Clear();

                    for (int x = 0; x < Size; x++)
                    {
                        int value = _grid[y, x];

                        if (value != 0)
                        {
                            _lst.Add(value);
                        }
                        else
                        {
                            _lst.Insert(0, 0);
                        }
                    }

                    for (int x = 0; x < Size; x++)
                    {
                        _grid[y, x] = _lst[x];
                    }
                }

                break;
            }
        }
    }

    private void MergingStep(Direction direction, out int score) // of adjacent pairs of equal values.
    {
        score = 0;

        switch(direction)
        {
            case Direction.Up:
            {
                // TODO: .

                break;
            }

            case Direction.Down:
            {
                // TODO: .

                break;
            }

            case Direction.Left:
            {
                // TODO: .

                break;
            }

            case Direction.Right:
            {
                for (int y = 0; y < Size; y++)
                {
                    _lst.Clear();

                    for (int x = Size - 2; x >= 0; x--)
                    {
                        int left  = _grid[y, x];
                        int right = _grid[y, x + 1];

                        if (left == right)
                        {
                            _grid[y, x] = 0;
                            _grid[y, x + 1] = left + right;

                            x--;

                            score += left + right;
                        }
                    }
                }

                break;
            }
        }
    }

    private int GetEmptyCells()
    {
        int emptyCells = 0;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (_grid[y, x] == 0)
                {
                    emptyCells++;
                }
            }
        }

        return emptyCells;
    }

    public void PrintInit(int left, int top)
    {
        Program.Write("╔══════╤══════╤══════╤══════╗", left, top + 0);
        Program.Write("║      │      │      │      ║", left, top + 1);
        Program.Write("║      │      │      │      ║", left, top + 2);
        Program.Write("║      │      │      │      ║", left, top + 3);
        Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 4);
        Program.Write("║      │      │      │      ║", left, top + 5);
        Program.Write("║      │      │      │      ║", left, top + 6);
        Program.Write("║      │      │      │      ║", left, top + 7);
        Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 8);
        Program.Write("║      │      │      │      ║", left, top + 9);
        Program.Write("║      │      │      │      ║", left, top + 10);
        Program.Write("║      │      │      │      ║", left, top + 11);
        Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 12);
        Program.Write("║      │      │      │      ║", left, top + 13);
        Program.Write("║      │      │      │      ║", left, top + 14);
        Program.Write("║      │      │      │      ║", left, top + 15);
        Program.Write("╚══════╧══════╧══════╧══════╝", left, top + 16);
    }

    public void PrintUpdate(int left, int top) // TODO: Centered values & Colors.
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int value = _grid[y, x];
                string strValue = value != 0 ? $"{value}" : "   ";

                Program.Write(strValue, left + (x * 7) + 2, top + (y * 4) + 2);
            }
        }
    }
}