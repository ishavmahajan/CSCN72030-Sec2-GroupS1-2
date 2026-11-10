namespace kitchenOS_Scada.Api.Helpers;

class FileHelper
{
    public static string GetFilePath(string fileName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectory, "SimulatorFiles", fileName);
        Console.WriteLine($"Looking for file '{fileName}' at path: {filePath}");
        
        return filePath;
    }

    public static string GetSimulationData(string fileName)
    {
        var filePath = GetFilePath(fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{fileName}' was not found in the 'SimulatorFiles' directory.");
        }

        return File.ReadAllText(filePath);
    }
}