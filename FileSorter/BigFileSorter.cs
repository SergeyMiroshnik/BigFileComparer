using System.Text;

namespace FileSorter
{
    public class BigFileSorter
    {
        private const double powerRatio = 0.6; // approximately optimal ratio between size of the whole file and the sizes of files to devide it
        private const int KB = 1024;
        private const int MB = KB * KB;

        private const string BigFileName = "BigFile";
        private const string OutputFileNameSuffix = "Sorted";

        private string sourcePath;
        private string outputPath;
        private int bufferSize;
        private string tempFolderPath;

        public BigFileSorter(string bigFilePath = null)
        {
            Initialize(bigFilePath);
        }

        private void Initialize(string bigFilePath = null)
        {
            string tempDirectory = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            string workDirectory;
            if (string.IsNullOrEmpty(bigFilePath) || !File.Exists(bigFilePath))
            {
                workDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                sourcePath = Path.Combine(workDirectory, $"{BigFileName}.txt");
                outputPath = Path.Combine(workDirectory, $"{BigFileName}{OutputFileNameSuffix}.txt");
            }
            else
            {
                sourcePath = bigFilePath;
                workDirectory = Path.GetDirectoryName(bigFilePath);
                string fileName = Path.GetFileNameWithoutExtension(bigFilePath);
                outputPath = Path.Combine(workDirectory, $"{fileName}{OutputFileNameSuffix}.txt");
            }
            tempFolderPath = Path.Combine(workDirectory, tempDirectory);
        }

        public string Sort()
        {
            Directory.CreateDirectory(tempFolderPath);

            Console.WriteLine($"Sorting start time: {DateTime.Now}");
            SplitAndSortFile();
            Console.WriteLine($"File splitted at: {DateTime.Now}");
            MergeSortedFiles();
            Console.WriteLine($"File sorted at: {DateTime.Now}");

            Directory.Delete(tempFolderPath, true);
            return outputPath;
        }

        private void SplitAndSortFile()
        {
            long inputFileSize = new FileInfo(sourcePath).Length;
            bufferSize = CalculateBufferSize(inputFileSize);
            try
            {
                using FileStream fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
                using StreamReader reader = new StreamReader(new BufferedStream(fileStream, bufferSize), Encoding.UTF8, true, bufferSize);

                char[] buffer = new char[bufferSize];
                StringBuilder restSymbols = new StringBuilder();
                int bytesRead, tempFileIndex = 0;
                SortedDictionary<string, List<int>> lines = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ProcessBuffer(buffer.AsSpan(0, bytesRead), restSymbols, lines);

                    if (lines.Count > 0)
                    {
                        string tempFile = Path.Combine(tempFolderPath, $"part_{tempFileIndex++}.txt");
                        WriteSortedPartial(tempFile, lines);
                        lines.Clear();
                    }
                }

                if (restSymbols.Length > 0)
                {
                    AddToDictionary(restSymbols.ToString(), lines);

                    string tempFile = Path.Combine(tempFolderPath, $"part_{tempFileIndex++}.txt");
                    WriteSortedPartial(tempFile, lines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            int CalculateBufferSize(long fileSize)
            {
                int bufferSize = (int)Math.Pow(fileSize, powerRatio);

                int roundingStep = MB;
                while (roundingStep > KB && bufferSize < roundingStep)
                    roundingStep /= 10;
                bufferSize = bufferSize / roundingStep * roundingStep;
                return Math.Max(KB, bufferSize);
            }
        }

        private void ProcessBuffer(ReadOnlySpan<char> span, StringBuilder restSymbols, SortedDictionary<string, List<int>> lines)
        {
            int start = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\n')
                {
                    int length = i - start;
                    if (length > 0)
                    {
                        if (span[i - 1] == '\r')
                            length--;
                        if (restSymbols.Length > 0)
                        {
                            restSymbols.Append(span.Slice(start, length));
                            AddToDictionary(restSymbols.ToString().AsMemory().Span, lines);
                            restSymbols.Clear();
                        }
                        else
                        {
                            AddToDictionary(span.Slice(start, length), lines);
                        }
                    }

                    start = i + 1;
                }
            }

            if (start < span.Length)
            {
                restSymbols.Append(span.Slice(start));
            }
        }

        private void AddToDictionary(ReadOnlySpan<char> line, SortedDictionary<string, List<int>> groupedLines)
        {
            int dotIndex = line.IndexOf('.');
            if (dotIndex == -1)
                return;

            ReadOnlySpan<char> numberPart = line.Slice(0, dotIndex);
            ReadOnlySpan<char> textPart = line.Slice(dotIndex + 1);

            string text = textPart.ToString();
            if (!int.TryParse(numberPart, out int number))
                return;

            if (!groupedLines.TryGetValue(text, out var numbers))
            {
                numbers = new List<int>();
                groupedLines[text] = numbers;
            }
            numbers.Add(number);
        }

        private void WriteSortedPartial(string filePath, SortedDictionary<string, List<int>> lines)
        {
            using StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8);
            foreach (var line in lines)
            {
                line.Value.Sort();
                foreach (var number in line.Value)
                    writer.WriteLine($"{number}.{line.Key}");
            }
        }

        private void MergeSortedFiles()
        {
            using StreamWriter writer = new(outputPath);
            string[] fileNames = Directory.GetFiles(tempFolderPath);
            StreamReader[] readers = new StreamReader[fileNames.Length];
            int index = 0;
            PriorityQueue<FileEntry, FileEntry> queue = new PriorityQueue<FileEntry, FileEntry>(FileEntryComparer.Instance);

            for (int i = 0; i < fileNames.Length; i++)
            {
                readers[index] = new StreamReader(fileNames[i]);
                if (readers[index].ReadLine() is string line)
                {
                    var entry = new FileEntry(line, readers[index]);
                    queue.Enqueue(entry, entry);
                }
                index++;
            }

            try
            {
                while (queue.Count > 0)
                {
                    var minEntry = queue.Dequeue();
                    writer.WriteLine(minEntry.Line);

                    if (minEntry.Reader.ReadLine() is string nextLine)
                    {
                        var entry = new FileEntry(nextLine, minEntry.Reader);
                        queue.Enqueue(entry, entry);
                    }
                        
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                foreach (var item in readers)
                {
                    item.Dispose();
                }
            }
        }
    }
}