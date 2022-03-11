using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class Program
{
    public static void Main(string[] args)
    {
        const int Max = 2048;

        Console.Title = $"{Max}";

        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.CursorVisible = false;

        Console.TreatControlCAsInput = true;

        Console.SetWindowPosition(0, 0);

        Console.WindowWidth = 33;
        Console.WindowHeight = 27;

        Console.BufferWidth = Console.WindowWidth;
        Console.BufferHeight = Console.WindowHeight;

        Console.Clear();

        Grid grid = new();

        grid.TryFill();
        grid.TryFill();

        grid.Print(init: true);

        bool exit = false;
        GameOver gameOver = GameOver.None;

        PrintStats(grid, gameOver, init: true);

        while (!exit && gameOver != GameOver.YouLose)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo cKI = Console.ReadKey(true);
                ThreadPool.QueueUserWorkItem((_) => { while (Console.KeyAvailable && Console.ReadKey(true) == cKI); });

                switch (cKI.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                    {
                        if (grid.TryMove(GetDirectionByConsoleKey(cKI.Key), out bool isMerge))
                        {
                            Trace.Assert(grid.TryFill());

                            grid.Print();

                            PrintStats(grid, gameOver);

                            Program.BeepAsync(frequencyA: !isMerge ? 800 : 1000, duration: 250);
                        }
                        else
                        {
                            Program.BeepAsync(frequencyA: 600, duration: 250);
                        }

                        if (grid.Max == Max)
                        {
                            gameOver = GameOver.YouWin;

                            PrintStats(grid, gameOver);

                            BeepAsync(frequencyA: 1000, frequencyB: 1500, duration: 500, ramp: true);
                        }
                        else if (!grid.ThereAreEmptyCells() && !grid.ThereAreEqualAdjacentCells())
                        {
                            gameOver = GameOver.YouLose;

                            PrintStats(grid, gameOver);

                            BeepAsync(frequencyA: 1000, frequencyB: 500, duration: 500, ramp: true);

                            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
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

    public static void Write(
        string str, int left, int top,
        ConsoleColor fColor = ConsoleColor.Gray,
        ConsoleColor bColor = ConsoleColor.Black)
    {
        Console.ForegroundColor = fColor;
        Console.BackgroundColor = bColor;

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

    public static void PrintStats(Grid grid, GameOver gameOver, bool init = false) // TODO: May include the best score (to be persisted on disk).
    {
        const int width = 17;
        const int height = 7;

        int left = (Console.WindowWidth - width) / 2; // Center.
        int top = 1; // Up.

        if (init)
        {
            Program.Write("┌───────────────┐", left, top + 0,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("│ Score │       │", left, top + 1,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("│ Max   │       │", left, top + 2,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("╞═══════════════╡", left, top + 3,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("│ Arrows │ Move │", left, top + 4,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("│ Esc    │ Exit │", left, top + 5,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("└───────────────┘", left, top + 6,  ConsoleColor.Black, ConsoleColor.White);
        }

        Program.Write($"{grid.Score}".PadRight(5), left + 10, top + 1, ConsoleColor.Black, ConsoleColor.White);
        Program.Write($"{grid.Max}".PadRight(5),   left + 10, top + 2, ConsoleColor.Black, ConsoleColor.White);

        if (gameOver != GameOver.None)
        {
            string str1 = "Game Over".PadRight(13);
            string str2 = gameOver == GameOver.YouWin ? "You Win!".PadRight(13) : "You Lose!".PadRight(13);

            Program.Write(str1, left + 2, top + 4, ConsoleColor.Black, ConsoleColor.White);
            Program.Write(str2, left + 2, top + 5, ConsoleColor.Black, ConsoleColor.White);
        }
    }
}

public enum Direction { Up, Down, Left, Right };

public enum GameOver { None, YouWin, YouLose }

public class Grid
{
    public const int Size = 4;

    public int Score { get; private set; }
    public int Max { get; private set; }

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

    public bool TryFill() // TODO: Highlight cells added since last TryFill().
    {
        if (!ThereAreEmptyCells())
        {
            return false;
        }

        while (true)
        {
            int y = _rnd.Next(0, Size);
            int x = _rnd.Next(0, Size);

            if (_grid[y, x] == 0)
            {
                int value = _rnd.Next(0, Size) < (Size * 3) / 4 ? 2 : 4; // 3/4 -> 2, 1/4 -> 4.

                _grid[y, x] = value;

                Max = Math.Max(Max, value);

                break;
            }
        }

        return true;
    }

    public bool TryMove(Direction direction, out bool isMerge) // TODO: Try.
    {
        SeparationStep(direction);
        MergingStep(direction, out isMerge);
        SeparationStep(direction);

        return true;
    }

    public bool ThereAreEmptyCells()
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (_grid[y, x] == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool ThereAreEqualAdjacentCells()
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size - 1; x++)
            {
                int left  = _grid[y, x];
                int right = _grid[y, x + 1];

                if (left == right && left != 0)
                {
                    return true;
                }
            }
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size - 1; y++)
            {
                int up   = _grid[y, x];
                int down = _grid[y + 1, x];

                if (up == down && up != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SeparationStep(Direction direction) // of non-zero values, preserving their order, from the others.
    {
        switch(direction)
        {
            case Direction.Up:
            {
                for (int x = 0; x < Size; x++)
                {
                    _lst.Clear();

                    for (int y = Size - 1; y >= 0; y--)
                    {
                        int value = _grid[y, x];

                        if (value != 0)
                        {
                            _lst.Insert(0, value);
                        }
                        else
                        {
                            _lst.Add(0);
                        }
                    }

                    for (int y = 0; y < Size; y++)
                    {
                        _grid[y, x] = _lst[y];
                    }
                }

                break;
            }

            case Direction.Down:
            {
                for (int x = 0; x < Size; x++)
                {
                    _lst.Clear();

                    for (int y = 0; y <= Size - 1; y++)
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

                    for (int y = 0; y < Size; y++)
                    {
                        _grid[y, x] = _lst[y];
                    }
                }

                break;
            }

            case Direction.Left:
            {
                for (int y = 0; y < Size; y++)
                {
                    _lst.Clear();

                    for (int x = Size - 1; x >= 0; x--)
                    {
                        int value = _grid[y, x];

                        if (value != 0)
                        {
                            _lst.Insert(0, value);
                        }
                        else
                        {
                            _lst.Add(0);
                        }
                    }

                    for (int x = 0; x < Size; x++)
                    {
                        _grid[y, x] = _lst[x];
                    }
                }

                break;
            }

            case Direction.Right:
            {
                for (int y = 0; y < Size; y++)
                {
                    _lst.Clear();

                    for (int x = 0; x <= Size - 1; x++)
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

    private void MergingStep(Direction direction, out bool isMerge) // of adjacent pairs of equal values.
    {
        isMerge = false;

        switch(direction)
        {
            case Direction.Up:
            {
                for (int x = 0; x < Size; x++)
                {
                    _lst.Clear();

                    for (int y = 0; y <= Size - 2; y++)
                    {
                        int up   = _grid[y, x];
                        int down = _grid[y + 1, x];

                        if (up == down)
                        {
                            if (up != 0)
                            {
                                _grid[y, x] = up + down;
                                _grid[y + 1, x] = 0;

                                isMerge = true;

                                Score += up + down;
                                Max = Math.Max(Max, up + down);
                            }

                            y++;
                        }
                    }
                }

                break;
            }

            case Direction.Down:
            {
                for (int x = 0; x < Size; x++)
                {
                    _lst.Clear();

                    for (int y = Size - 2; y >= 0; y--)
                    {
                        int up   = _grid[y, x];
                        int down = _grid[y + 1, x];

                        if (up == down)
                        {
                            if (up != 0)
                            {
                                _grid[y, x] = 0;
                                _grid[y + 1, x] = up + down;

                                isMerge = true;

                                Score += up + down;
                                Max = Math.Max(Max, up + down);
                            }

                            y--;
                        }
                    }
                }

                break;
            }

            case Direction.Left:
            {
                for (int y = 0; y < Size; y++)
                {
                    _lst.Clear();

                    for (int x = 0; x <= Size - 2; x++)
                    {
                        int left  = _grid[y, x];
                        int right = _grid[y, x + 1];

                        if (left == right)
                        {
                            if (left != 0)
                            {
                                _grid[y, x] = left + right;
                                _grid[y, x + 1] = 0;

                                isMerge = true;

                                Score += left + right;
                                Max = Math.Max(Max, left + right);
                            }

                            x++;
                        }
                    }
                }

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
                            if (left != 0)
                            {
                                _grid[y, x] = 0;
                                _grid[y, x + 1] = left + right;

                                isMerge = true;

                                Score += left + right;
                                Max = Math.Max(Max, left + right);
                            }

                            x--;
                        }
                    }
                }

                break;
            }
        }
    }

    public void Print(bool init = false)
    {
        const int width = 29;
        const int height = 17;

        int left = (Console.WindowWidth - width) / 2; // Center.
        int top = Console.WindowHeight - height - 1; // Down.

        if (init)
        {
            Program.Write("╔══════╤══════╤══════╤══════╗", left, top + 0,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 1,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 2,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 3,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 4,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 5,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 6,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 7,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 8,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 9,  ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 10, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 11, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("╟──────┼──────┼──────┼──────╢", left, top + 12, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 13, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 14, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("║      │      │      │      ║", left, top + 15, ConsoleColor.Black, ConsoleColor.White);
            Program.Write("╚══════╧══════╧══════╧══════╝", left, top + 16, ConsoleColor.Black, ConsoleColor.White);
        }

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int value = _grid[y, x];

                string strValue = value switch
                {
                    0                 => new string(' ', 4),
                    >= 2   and <= 8   => $" {value}  ",
                    >= 16  and <= 64  => $" {value} ",
                    >= 128 and <= 512 => $"{value} ",
                    _                 => $"{value}"
                };
                Trace.Assert(strValue.Length <= 4);

                ConsoleColor fColor = value switch
                {
                    0                  => ConsoleColor.Black,
                    2 or 4             => ConsoleColor.Black,
                    >= 8   and <= 64   => ConsoleColor.White,
                    >= 128 and <= 2048 => ConsoleColor.Gray,
                    _                  => ConsoleColor.White
                };

                ConsoleColor bColor = value switch
                {
                    0                   => ConsoleColor.White,
                    2                   => ConsoleColor.Gray,
                    4                   => ConsoleColor.DarkGray,
                    8  or 16            => ConsoleColor.Red,
                    32 or 64            => ConsoleColor.DarkRed,
                    >= 128  and <= 512  => ConsoleColor.Yellow,
                    >= 1024 and <= 2048 => ConsoleColor.DarkYellow,
                    _                   => ConsoleColor.Black
                };

                Program.Write(new string(' ', 4), left + (x * 7) + 2, top + (y * 4) + 1, fColor, bColor);
                Program.Write(strValue,           left + (x * 7) + 2, top + (y * 4) + 2, fColor, bColor);
                Program.Write(new string(' ', 4), left + (x * 7) + 2, top + (y * 4) + 3, fColor, bColor);
            }
        }
    }
}