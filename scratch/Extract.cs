using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static void Main()
    {
        string zipPath = @"e:\Project game pk\minecraft\Docs\tham khảo\mod\mods\epic-fight-20.14.17-mc1.20.1-forge.jar";
        Directory.CreateDirectory(@"scratch\epicfight_jsons");
        
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.StartsWith("assets/epicfight/animmodels/animations/biped/combat/") && entry.FullName.EndsWith(".json"))
                {
                    if (entry.FullName.Contains("fist_"))
                    {
                        string name = Path.GetFileName(entry.FullName);
                        entry.ExtractToFile(Path.Combine(@"scratch\epicfight_jsons", name), true);
                        Console.WriteLine("Extracted " + name);
                    }
                }
            }
        }
    }
}
