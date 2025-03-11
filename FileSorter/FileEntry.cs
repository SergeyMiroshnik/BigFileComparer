namespace FileSorter
{
    internal struct FileEntry
    {
        public string Line { get; set; }
        public StreamReader Reader { get; set; }

        public FileEntry(string line, StreamReader reader)
        {
            Line = line;
            Reader = reader;
        }
    }
}
