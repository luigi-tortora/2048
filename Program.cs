public class Program
{
    public static void Main(string[] args)
    {
        Grid grid = new();

        Console.Title = "2048";

        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.CursorVisible = false;

        Console.TreatControlCAsInput = true;

        Console.SetWindowPosition(0, 0);

        Console.WindowWidth = 1 + Grid.GridWidth + 1;
        Console.WindowHeight = 1 + Grid.GridHeight + 1;

        Console.BufferWidth = Console.WindowWidth;
        Console.BufferHeight = Console.WindowHeight;

        Console.Clear();

        grid.PrintInit(1, 1);

        grid.TryFill();
        grid.TryFill();
        grid.TryFill();

        grid.PrintUpdate(1, 1);

        grid.TryMove(Direction.Right);

        Console.ReadLine();

        Console.OutputEncoding = System.Text.Encoding.Default;
        Console.CursorVisible = true;

        Console.TreatControlCAsInput = false;

        Console.Clear();
        Console.ResetColor();
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
}

public enum Direction {Up, Right, Down, Bottom};

public class Grid
{
    public const int GridWidth = 29;
    public const int GridHeight = 17;
    public const int Size = 4;
    private readonly int[,] _grid;
    private readonly Random _rnd;

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

    public bool TryMove(Direction direction)
    {
        switch(direction)
        {
            case Direction.Right:
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int i = Size - 1; i > 0; i--)
                    {
                        for (int x = i - 1; x >= 0; x--)
                        {
                            Console.Write(x);
                        } 
                        Console.WriteLine();
                    }
                }
                
                break;
            }
        }

        return false;
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

    public void PrintUpdate(int left, int top)
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