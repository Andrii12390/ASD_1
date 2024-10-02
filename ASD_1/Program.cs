using System.Diagnostics;
using System.IO;
using System.Text;
class MultiwayMergeSort
{
    static void Main(string[] args)
    {
        int sizeMB = 10;
        string inputFilePath = "input.txt";
        CreateEmptyFiles();
        Console.WriteLine($"[Generating] input.txt {sizeMB}MB");
        GenerateFile(inputFilePath, sizeMB);

        Console.WriteLine("[Sorting]: start");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        SplitInputFile(inputFilePath);

        bool sorted = false, mergeToB = true;
        do
        {
            if (mergeToB) sorted = MergeSeries("B", "C");
            else sorted = MergeSeries("C", "B");

            mergeToB = !mergeToB;
        } while (!sorted);

        stopwatch.Stop();
        Console.WriteLine($"[Sorting]: end in {stopwatch.ElapsedMilliseconds / 1000} seconds");
        PrintFirstElements();
        DeleteFiles();
    }
    static void SplitInputFile(string inputFilePath)
    {
        var writers = new StreamWriter[3];
        writers[0] = new StreamWriter("B1.txt", append: false);
        writers[1] = new StreamWriter("B2.txt", append: false);
        writers[2] = new StreamWriter("B3.txt", append: false);

        using (var reader = new StreamReader(inputFilePath))
        {
            int previousValue = int.MinValue;
            int currentFileIndex = 0; 

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int value = int.Parse(line.Trim());

                if (value < previousValue)
                {
                    currentFileIndex = (currentFileIndex + 1) % 3;
                }

                writers[currentFileIndex].WriteLine(value);
                previousValue = value;
            }
        }

        foreach (var writer in writers)
        {
            writer.Close();
        }
    }

    static bool TryReadNextValue(StreamReader reader, out int value)
    {
        string line = reader.ReadLine();
        if (line != null)
        {
            value = int.Parse(line.Trim());
            return true;
        }
        value = int.MaxValue;
        return false; 
    }
    static void CreateEmptyFiles()
    {
        string[] files = { "input.txt", "B1.txt", "B2.txt", "B3.txt", "C1.txt", "C2.txt", "C3.txt" };
        foreach (var path in files)
        {
            using (FileStream fs = File.Create(path)) { }
            Console.WriteLine($"File '{path}' created.");
        }
    }

    static void DeleteFiles()
    {
        string notResultFile = GetFileLineCount("C1.txt") > GetFileLineCount("B1.txt") ? "B1.txt" : "C1.txt";
        string[] files = { notResultFile, "B2.txt", "B3.txt", "C2.txt", "C3.txt" };
        try
        {
            foreach (string path in files)
            {
                File.Delete(path);
                Console.WriteLine($"File '{path}' deleted.");
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    static int GetFileLineCount(string filePath)
    {
        return File.ReadLines(filePath).Count();
    }

    static bool MergeSeries(string inputPrefix, string outputPrefix)
    {
        ClearFile($"{outputPrefix}1.txt");
        ClearFile($"{outputPrefix}2.txt");
        ClearFile($"{outputPrefix}3.txt");

        int[] currentValues = new int[3];
        bool[] hasMoreData = new bool[3];

        using (var reader1 = new StreamReader($"{inputPrefix}1.txt"))
        using (var reader2 = new StreamReader($"{inputPrefix}2.txt"))
        using (var reader3 = new StreamReader($"{inputPrefix}3.txt"))
        {
            hasMoreData[0] = TryReadNextValue(reader1, out currentValues[0]);
            hasMoreData[1] = TryReadNextValue(reader2, out currentValues[1]);
            hasMoreData[2] = TryReadNextValue(reader3, out currentValues[2]);

            int currentIndex = 0;
            bool[] endOfSeries = new bool[3];
            bool hasAnyMoreData = hasMoreData[0] || hasMoreData[1] || hasMoreData[2];
            while (hasAnyMoreData)
            {
                string outputFilePath = $"{outputPrefix}{currentIndex + 1}.txt";
                using (var writer = new StreamWriter(outputFilePath, true))
                {
                    bool seriesOpen = true;

                    while (seriesOpen)
                    {
                        int minIndex = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (hasMoreData[i] && !endOfSeries[i] &&
                                (minIndex == -1 || currentValues[i] < currentValues[minIndex]))
                            {
                                minIndex = i;
                            }
                        }

                        if (minIndex == -1)
                        {
                            seriesOpen = false;
                            break;
                        }

                        writer.WriteLine(currentValues[minIndex]);
                        int previousValue = currentValues[minIndex];

                        hasMoreData[minIndex] = TryReadNextValue(minIndex == 0 ? reader1 : minIndex == 1 ? reader2 : reader3, out currentValues[minIndex]);

                        if (!hasMoreData[minIndex] || currentValues[minIndex] < previousValue)
                        {
                            endOfSeries[minIndex] = true;
                        }
                    }
                }

                currentIndex = (currentIndex + 1) % 3;
                Array.Fill(endOfSeries, false);
                hasAnyMoreData = hasMoreData[0] || hasMoreData[1] || hasMoreData[2];
            }
        }
        return GetFileLineCount($"{outputPrefix}1.txt") >= GetFileLineCount("input.txt");
    }
    static void GenerateFile(string filePath, int sizeMB)
    {
        int targetSizeInBytes = sizeMB * 1024 * 1024;
        long seriesBytes = 100 * 1024 * 1024;
        int currentSize = 0;

        Random random = new Random();
        StringBuilder sb = new StringBuilder();
        using (var writer = new StreamWriter(filePath))
        {
            if (sizeMB == 1024)
            {
                for (int section = 0; section < 10; section++)
                {
                    long bytesInSeries = 0;
                    int currentNumber = 1;

                    while (bytesInSeries < seriesBytes)
                    {
                        string line = currentNumber.ToString() + Environment.NewLine;
                        long lineSize = System.Text.Encoding.UTF8.GetByteCount(line);

                        if (bytesInSeries + lineSize > seriesBytes) break;
                        writer.Write(line);
                        bytesInSeries += lineSize;
                        currentNumber += 5;
                    }
                }
            }
            else
            {
                while (currentSize < targetSizeInBytes)
                {
                    string line = random.Next(int.MinValue, int.MaxValue).ToString() + Environment.NewLine;
                    currentSize += Encoding.UTF8.GetByteCount(line);
                    writer.Write(line);
                }
            }
        }
    }
    static void ClearFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.WriteAllText(filePath, string.Empty);
        }
    }

    static void PrintFirstElements()
    {
        string outputFilePath = GetFileLineCount("C1.txt") > GetFileLineCount("B1.txt") ? "C1.txt" : "B1.txt";
        using (var reader = new StreamReader(outputFilePath))
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(reader.ReadLine());
            }
        }
    }
}
