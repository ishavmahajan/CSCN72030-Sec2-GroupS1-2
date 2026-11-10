namespace kitchenOS_Scada.Api.Helpers;

class FileHelper
{
    public static string GetFilePath(string fileName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectory, "SimulatorFiles", fileName);
        return filePath;
    }

    public static string GetDeviceFromJsonFile(string fileName)
    {
        var filePath = GetFilePath(fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{fileName}' was not found in the 'SimulatorFiles' directory.");
        }

        return File.ReadAllText(filePath);
    }
}