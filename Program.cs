using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class Program
{
    private const int AbsoluteMax = 8192;
    private const int Max = 2048;

    private const string StatsPath = "stats.dat";

    private enum GameOver { None, YouWin, YouLose }

    public static void Main(string[] args)
    {
        Console.Title = $"{Max}";

        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.CursorVisible = false;

        Console.TreatControlCAsInput = true;

        Console.SetWindowPosition(0, 0);

        Console.WindowWidth = 33;
        Console.WindowHeight = 30;

        Console.BufferWidth = Console.WindowWidth;
        Console.BufferHeight = Console.WindowHeight;

        Console.Clear();

        TryLoadStats(out int hiScore, out int hiMax);

        Grid grid = new(hiScore, hiMax);

        grid.TryFill();
        grid.TryFill();

        grid.Print(init: true);

        PrintStats(grid, init: true);

        bool exit = false;
        GameOver gameOver = GameOver.None;

        while (!exit)
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
                        if (grid.Max < AbsoluteMax && grid.TryMove(GetDirectionByConsoleKey(cKI.Key), out bool isMerge))
                        {
                            grid.TryFill();

                            grid.Print();

                            PrintStats(grid, gameOver);

                            Program.BeepAsync(frequencyA: !isMerge ? 800 : 1000, duration: 250);
                        }
                        else
                        {
                            Program.BeepAsync(frequencyA: 600, duration: 250);
                        }

                        if (gameOver == GameOver.None && grid.Max == Max)
                        {
                            gameOver = GameOver.YouWin;

                            PrintStats(grid, gameOver);

                            Thread.Sleep(250);
                            BeepAsync(frequencyA: 1000, frequencyB: 1500, duration: 500, ramp: true);
                        }

                        if (gameOver != GameOver.YouLose && !grid.ThereAreEmptyCells() && !grid.ThereAreEqualAdjacentCells())
                        {
                            gameOver = GameOver.YouLose;

                            PrintStats(grid, gameOver);

                            Thread.Sleep(250);
                            BeepAsync(frequencyA: 1000, frequencyB: 500, duration: 500, ramp: true);
                        }

                        break;
                    }

                    case ConsoleKey.Escape:
                    {
                        exit = true;

                        UpdateStats(grid);

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

    private static Direction GetDirectionByConsoleKey(ConsoleKey consoleKey)
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

    private static void BeepAsync(int frequencyA = 800, int frequencyB = 0, int duration = 200, bool ramp = false)
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

    private static bool TryLoadStats(out int hiScore, out int hiMax)
    {
        try
        {
            hiScore = 0;
            hiMax   = 0;

            if (!File.Exists(StatsPath))
            {
                return false;
            }

            string[] stats = File.ReadAllLines(StatsPath);

            if (stats.Length != 2)
            {
                return false;
            }

            byte[] bytesHiScore = Convert.FromBase64String(stats[0]);
            byte[] bytesHiMax   = Convert.FromBase64String(stats[1]);

            if (bytesHiScore.Length != 4 || bytesHiMax.Length != 4)
            {
                return false;
            }

            hiScore = BitConverter.ToInt32(bytesHiScore);
            hiMax   = BitConverter.ToInt32(bytesHiMax);

            if (hiScore < 0 || hiScore > 99999 || hiMax < 0 || hiMax > 9999)
            {
                hiScore = 0;
                hiMax   = 0;

                return false;
            }

            return true;
        }
        catch
        {
            hiScore = 0;
            hiMax   = 0;

            return false;
        }
    }

    private static bool TrySaveStats(int score, int max)
    {
        try
        {
            byte[] bytesScore = BitConverter.GetBytes(score);
            byte[] bytesMax   = BitConverter.GetBytes(max);

            string[] stats = new String[2];

            stats[0] = Convert.ToBase64String(bytesScore);
            stats[1] = Convert.ToBase64String(bytesMax);

            File.WriteAllLines(StatsPath, stats);

            return true;
        }
        catch
        {
            File.Delete(StatsPath);

            return false;
        }
    }

    private static void UpdateStats(Grid grid)
    {
        if (TryLoadStats(out int hiScore, out int hiMax))
        {
            if (grid.Score > hiScore || grid.Max > hiMax)
            {
                TrySaveStats(grid.Score, grid.Max);
            }
        }
        else
        {
            TrySaveStats(grid.Score, grid.Max);
        }
    }

    private static void PrintStats(Grid grid, GameOver gameOver = GameOver.None, bool init = false)
    {
        const int width = 20;
        //const int height = 10;

        int left = (Console.WindowWidth - width) / 2; // Center.
        int top = 1; // Up.

        if (init)
        {
            Write("┌──────────────────┐", left, top + 0, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Score    │       │", left, top + 1, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Max      │       │", left, top + 2, ConsoleColor.Black, ConsoleColor.White);
            Write("├──────────────────┤", left, top + 3, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Hi-Score │       │", left, top + 4, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Hi-Max   │       │", left, top + 5, ConsoleColor.Black, ConsoleColor.White);
            Write("╞══════════════════╡", left, top + 6, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Arrows   │ Move  │", left, top + 7, ConsoleColor.Black, ConsoleColor.White);
            Write("│ Esc      │ Exit  │", left, top + 8, ConsoleColor.Black, ConsoleColor.White);
            Write("└──────────────────┘", left, top + 9, ConsoleColor.Black, ConsoleColor.White);
        }

        if (gameOver == GameOver.None)
        {
            Write($"{grid.Score}".PadRight(5),   left + 13, top + 1, ConsoleColor.White, ConsoleColor.Black);
            Write($"{grid.Max}".PadRight(5),     left + 13, top + 2, ConsoleColor.White, ConsoleColor.Black);
            Write($"{grid.HiScore}".PadRight(5), left + 13, top + 4, ConsoleColor.White, ConsoleColor.Black);
            Write($"{grid.HiMax}".PadRight(5),   left + 13, top + 5, ConsoleColor.White, ConsoleColor.Black);
        }
        else
        {
            string str7 = "Game Over";
            string str8 = gameOver == GameOver.YouWin ? "You Win!" : "You Lose!";

            Write(str7.PadRight(15), left + 2, top + 7, ConsoleColor.White, ConsoleColor.Black);
            Write(str8.PadRight(15), left + 2, top + 8, ConsoleColor.White, ConsoleColor.Black);
        }
    }
}

public enum Direction { Up, Down, Left, Right };

public class Grid
{
    private const int Size = 4;

    public int Score { get; private set; }
    public int Max { get; private set; }

    public int HiScore { get; }
    public int HiMax { get; }

    private record struct Cell(int Value, bool IsFill);
    private readonly Cell[,] _grid;

    private readonly Random _rnd;
    private readonly List<int> _lst;

    public Grid(int hiScore = 0, int hiMax = 0)
    {
        HiScore = hiScore;
        HiMax = hiMax;

        _grid = new Cell[Size, Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                _grid[y, x].Value = 0;
            }
        }

        _rnd = new();
        _lst = new();
    }

    public bool TryFill()
    {
        if (!ThereAreEmptyCells())
        {
            return false;
        }

        while (true)
        {
            int y = _rnd.Next(0, Size);
            int x = _rnd.Next(0, Size);

            if (_grid[y, x].Value == 0)
            {
                int value = _rnd.Next(0, Size) < (Size * 3) / 4 ? 2 : 4; // 3/4 -> 2, 1/4 -> 4.

                _grid[y, x].Value = value;
                _grid[y, x].IsFill = true;

                break;
            }
        }

        return true;
    }

    public bool TryMove(Direction direction, out bool isMerge)
    {
        SeparationStep(direction, out bool isSeparation);

        MergingStep(direction, out isMerge);
        if (isMerge) SeparationStep(direction, out _);

        return isSeparation || isMerge;
    }

    public bool ThereAreEmptyCells()
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (_grid[y, x].Value == 0)
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
                int left  = _grid[y, x].Value;
                int right = _grid[y, x + 1].Value;

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
                int up   = _grid[y, x].Value;
                int down = _grid[y + 1, x].Value;

                if (up == down && up != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SeparationStep(Direction direction, out bool isSeparation) // of non-zero values, preserving their order, from the others.
    {
        isSeparation = false;

        switch(direction)
        {
            case Direction.Up:
            {
                for (int x = 0; x < Size; x++)
                {
                    _lst.Clear();

                    for (int y = Size - 1; y >= 0; y--)
                    {
                        int value = _grid[y, x].Value;

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
                        if (_grid[y, x].Value != _lst[y])
                        {
                            _grid[y, x].Value = _lst[y];

                            isSeparation = true;
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

                    for (int y = 0; y <= Size - 1; y++)
                    {
                        int value = _grid[y, x].Value;

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
                        if (_grid[y, x].Value != _lst[y])
                        {
                            _grid[y, x].Value = _lst[y];

                            isSeparation = true;
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

                    for (int x = Size - 1; x >= 0; x--)
                    {
                        int value = _grid[y, x].Value;

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
                        if (_grid[y, x].Value != _lst[x])
                        {
                            _grid[y, x].Value = _lst[x];

                            isSeparation = true;
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

                    for (int x = 0; x <= Size - 1; x++)
                    {
                        int value = _grid[y, x].Value;

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
                        if (_grid[y, x].Value != _lst[x])
                        {
                            _grid[y, x].Value = _lst[x];

                            isSeparation = true;
                        }
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
                    for (int y = 0; y <= Size - 2; y++)
                    {
                        int up   = _grid[y, x].Value;
                        int down = _grid[y + 1, x].Value;

                        if (up == down)
                        {
                            if (up != 0)
                            {
                                _grid[y, x].Value = up + down;
                                _grid[y + 1, x].Value = 0;

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
                    for (int y = Size - 2; y >= 0; y--)
                    {
                        int up   = _grid[y, x].Value;
                        int down = _grid[y + 1, x].Value;

                        if (up == down)
                        {
                            if (up != 0)
                            {
                                _grid[y, x].Value = 0;
                                _grid[y + 1, x].Value = up + down;

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
                    for (int x = 0; x <= Size - 2; x++)
                    {
                        int left  = _grid[y, x].Value;
                        int right = _grid[y, x + 1].Value;

                        if (left == right)
                        {
                            if (left != 0)
                            {
                                _grid[y, x].Value = left + right;
                                _grid[y, x + 1].Value = 0;

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
                    for (int x = Size - 2; x >= 0; x--)
                    {
                        int left  = _grid[y, x].Value;
                        int right = _grid[y, x + 1].Value;

                        if (left == right)
                        {
                            if (left != 0)
                            {
                                _grid[y, x].Value = 0;
                                _grid[y, x + 1].Value = left + right;

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
                int value = _grid[y, x].Value;

                string strValue = value switch
                {
                    0                 => new String(' ', 4),
                    >= 2   and <= 8   => $" {value}  ",
                    >= 16  and <= 64  => $" {value} ",
                    >= 128 and <= 512 => $"{value} ",
                    _                 => $"{value}"
                };

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

                Program.Write(new String(' ', 4), left + (x * 7) + 2, top + (y * 4) + 1, fColor, bColor);
                Program.Write(strValue,           left + (x * 7) + 2, top + (y * 4) + 2, fColor, bColor);
                Program.Write(new String(' ', 4), left + (x * 7) + 2, top + (y * 4) + 3, fColor, bColor);

                if (_grid[y, x].IsFill)
                {
                    _grid[y, x].IsFill = false;

                    Program.Write("┌──┐", left + (x * 7) + 2, top + (y * 4) + 1, ConsoleColor.Black, bColor);
                    Program.Write("│",    left + (x * 7) + 2, top + (y * 4) + 2, ConsoleColor.Black, bColor);
                    Program.Write(   "│", left + (x * 7) + 5, top + (y * 4) + 2, ConsoleColor.Black, bColor);
                    Program.Write("└──┘", left + (x * 7) + 2, top + (y * 4) + 3, ConsoleColor.Black, bColor);
                }
            }
        }
    }
}