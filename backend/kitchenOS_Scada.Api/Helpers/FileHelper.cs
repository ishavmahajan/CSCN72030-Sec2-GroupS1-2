namespace kitchenOS_Scada.Api.Helpers;

// Loads simulator files from the application's working directory.
class FileHelper
{
    public static string GetFilePath(string fileName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectory, "SimulatorFiles", fileName);
        if (!File.Exists(filePath))
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, "SimulatorFiles", fileName);
            if (File.Exists(outputPath))
            {
                filePath = outputPath;
            }
        }

        // Log the resolved path to make missing deployment content easy to diagnose.
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
