using FileCreator;
using FileSorter;

Console.Write("Create file? (Y/N): ");
string response = Console.ReadLine();
string inputFilePath = "";
if (response.ToUpper() == "Y")
{
    BigFileCreator creator;
    do
    {
        Console.Write("Size of the output file (MB): ");
        response = Console.ReadLine();
        if (int.TryParse(response, out int size))
        {
            creator = new BigFileCreator(size);
            break;
        }
        else
            Console.WriteLine("Invalid integer value");
    } while (true);
    inputFilePath = creator.CreateFile();
}
else
{
    inputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "BigFile.txt");
    Console.Write($"Do you want to type the path to the input file (Y) or use the default path ({inputFilePath}) (N)? ");
    response = Console.ReadLine();
    if (response.ToUpper() == "Y")
    {
        do
        {
            Console.Write("Please type the path to the input file: ");
            inputFilePath = Console.ReadLine();
            if (File.Exists(inputFilePath))
                break;
            Console.WriteLine("Invalid path to the file.");
        } while (true);
    }
}

Console.WriteLine($"Start time: {DateTime.Now}");

BigFileSorter sorter = new BigFileSorter(inputFilePath);
sorter.Sort();
Console.WriteLine($"Finish time: {DateTime.Now}");