namespace FileSorter
{
    internal class FileEntryComparer : IComparer<FileEntry>
    {
        public static FileEntryComparer Instance = new FileEntryComparer();

        public int Compare(FileEntry x, FileEntry y)
        {
            (int numberX, string textX) = ParseEntry(x.Line);
            (int numberY, string textY) = ParseEntry(y.Line);

            int textComparison = StringComparer.Ordinal.Compare(textX, textY);
            if (textComparison != 0) return textComparison;

            return numberX.CompareTo(numberY);
        }

        private static (int, string) ParseEntry(string line)
        {
            int dotIndex = line.IndexOf('.');
            if (dotIndex == -1)
                return (int.MaxValue, line);

            ReadOnlySpan<char> numberPart = line.AsSpan(0, dotIndex);
            ReadOnlySpan<char> textPart = line.AsSpan(dotIndex + 1);

            int.TryParse(numberPart, out int number);

            return (number, textPart.ToString());
        }
    }
}
