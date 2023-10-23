using System.Diagnostics;

internal class Matrix
{
    private int[,] _matr;
    public int Rows { get; }
    public int Columns { get; }

    public Matrix(int rows, int columns)
    {
        this.Rows = rows;
        this.Columns = columns;
        this._matr = new int[rows, columns];
        InitializeMatrix();
    }

    private void InitializeMatrix()
    {
        Random rand = new Random();
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                _matr[i, j] = rand.Next(0, 100);
            }
        }
    }

    public int Filter(Predicate<int> func)
    {
        int x = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                if (func(_matr[i, j]))
                {
                    x++;
                }
            }
        }
        return x;
    }
    
    public int FilterByBlocks(Predicate<int> func)
    {
        int x = 0;
        Block[] blocks = Separate();
        foreach (Block block in blocks)
        {
            x += block.Filter(func);
        }
        return x;
    }

    public int FilterMultiThread(Predicate<int> func, int max)
    {
        int x = 0;

        void ThreadFunc(object? obj)
        {
            Block?[] blocks = (Block?[])obj!;
            Interlocked.Add(ref x, blocks.Sum(block => block?.Filter(func) ?? 0));
        }

        Block[] blocks = Separate();
        int threadsNum = Math.Min(blocks.Length, max);
        Thread[] threads = new Thread[threadsNum];
        int blocksPerThread = (int)Math.Ceiling((double)blocks.Length / threadsNum);
        int k = 0;

        for (int i = 0; i < threadsNum; i++)
        {
            Block[] threadBlocks = new Block[blocksPerThread];
            for (int j = k; j < Math.Min(k + blocksPerThread, blocks.Length); j++)
            {
                threadBlocks[j - k] = blocks[j];
            }
            threads[i] = new Thread(ThreadFunc);
            threads[i].Start(threadBlocks);
            k += blocksPerThread;
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        return x;
    }

    public int this[int i, int j]
    {
        get => _matr[i, j];
        set => _matr[i, j] = value;
    }

    private Block[] Separate()
    {
        int num = (int)Math.Ceiling((double)Rows / Block.Rows) * (int)Math.Ceiling((double)Columns / Block.Cols);
        Block[] blocks = new Block[num];
        int m = 0, n = 0;
        for (int i = 0; i < num; i++)
        {
            blocks[i] = new Block(this, (m, n));
            n += Block.Cols;
            if (n >= Columns)
            {
                n = 0;
                m += Block.Rows;
            }
        }
        return blocks;
    }

    public static void Main(string[] args)
    {
        int[] threadCounts = { 1, 2, 4, 7, 10 };
        int[] matrixSizes = { 100, 200, 500, 700, 1000, 3500, 6000, 7777, 10000 }; // Розміри матриць для тестування

        foreach (int size in matrixSizes)
        {
            Console.WriteLine($"\n{size}x{size} elements:");

            Matrix matrix = new Matrix(size, size);

            foreach (int threadCount in threadCounts)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                int result = matrix.FilterMultiThread(x => x > 50, threadCount);
                stopwatch.Stop();
                Console.WriteLine($"{threadCount} threads: {result}");

                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                if (threadCount > 1)
                {
                    long singleThreadTime = stopwatch.ElapsedMilliseconds;
                    double speedup = (double)singleThreadTime / elapsedMilliseconds;
                    Console.WriteLine($"Прискорення: {speedup:F2}");
                }
            }

            Console.WriteLine();
        }
    }
}

internal class Block
{
    private Matrix _matr;
    public const int Rows = 10;
    public const int Cols = 10;
    private (int, int) _index;

    public Block(Matrix matr, (int, int) index)
    {
        this._matr = matr;
        this._index = index;
    }

    public int Filter(Predicate<int> func)
    {
        int x = 0;
        for (int i = _index.Item1; i < Math.Min(_index.Item1 + Rows, _matr.Rows); i++)
        {
            for (int j = _index.Item2; j < Math.Min(_index.Item2 + Cols, _matr.Columns); j++)
            {
                if (func(_matr[i, j]))
                {
                    x++;
                }
            }
        }
        return x;
    }
}
