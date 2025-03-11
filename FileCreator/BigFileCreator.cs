namespace FileCreator
{
    public class BigFileCreator
    {
        private const int MB = 1024 * 1024;
        private static Random _rand = new Random(10001);
        private static readonly char[] _digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private static readonly string _symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Size of the file in output file in MB
        /// </summary>
        private long _size;
        private string _path;

        /// <param name="size">The size of the output file in MB</param>
        public BigFileCreator(int size, string folder = null)
        {
            _size = (long)size * MB;
            if (string.IsNullOrEmpty(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            _path = Path.Combine(folder, "BigFile.txt");
        }

        public string CreateFile()
        {
            long realSize = 0;
            char[] buffer = new char[15];
            int rowSize;
            using (StreamWriter writer = new StreamWriter(_path))
            {
                do
                {
                    rowSize = GenerateRow(buffer);
                    realSize += rowSize + 2;
                    writer.WriteLine(buffer, 0, rowSize);
                    if (realSize >= _size)
                        break;
                } while (true);
            }
            return _path;
        }

        private int GenerateRow(Span<char> buffer)
        {
            int length = NumberToSpan(buffer);
            buffer[length] = '.';
            int stringLength = _rand.Next(2, buffer.Length - length - 1);
            int totalLength = length + stringLength + 1;
            for (int i = length + 1; i < totalLength; i++)
                buffer[i] = _symbols[_rand.Next(0, _symbols.Length)];
            return totalLength;
        }

        private int NumberToSpan(Span<char> buffer)
        {
            int intVal = _rand.Next(10000);
            int length = intVal.ToString().Length;

            for (int i = length - 1; i >= 0; i--)
            {
                buffer[i] = _digits[intVal % 10];
                intVal /= 10;
            }
            return length;
        }
    }
}
