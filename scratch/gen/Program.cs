using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

class Program
{
    static void Main()
    {
        string[] files = { "fist_auto1.json", "fist_auto2.json", "fist_auto3.json", "fist_dash.json", "fist_airslash.json" };
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("namespace MineCraftUnity.Player.Combat {");
        sb.AppendLine("public static class EpicFightAnimData {");
        sb.AppendLine("  public static Dictionary<string, AttackAnimationDefinition> GetAnimations() {");
        sb.AppendLine("    var dict = new Dictionary<string, AttackAnimationDefinition>();");
        
        foreach (var file in files)
        {
            string path = Path.Combine(@"scratch\epicfight_jsons", file);
            if (!File.Exists(path)) continue;
            
            string json = File.ReadAllText(path);
            string animId = file.Replace(".json", "");
            
            sb.AppendLine($"    {{ // {animId}");
            sb.AppendLine($"       var def = new AttackAnimationDefinition();");
            sb.AppendLine($"       def.Id = \"{animId}\";");
            sb.AppendLine($"       def.Joints = new Dictionary<string, AttackJointKeyframe>();");
            
            // Very hacky JSON regex parsing since we don't have Newtonsoft here
            var jointMatches = Regex.Matches(json, @"\""name\"":\s*\""(.*?)\"".*?\""time\"":\s*\[(.*?)\].*?\""transform\"":\s*\[(.*?)\]\s*\}", RegexOptions.Singleline);
            
            float maxTime = 0f;
            foreach (Match jm in jointMatches)
            {
                string boneName = jm.Groups[1].Value;
                string[] timesStr = jm.Groups[2].Value.Split(',');
                
                sb.AppendLine($"       {{");
                sb.AppendLine($"           var times = new float[{timesStr.Length}];");
                sb.AppendLine($"           var matrices = new Matrix4x4[{timesStr.Length}];");
                
                for(int i = 0; i < timesStr.Length; i++)
                {
                    float t = float.Parse(timesStr[i].Trim(), CultureInfo.InvariantCulture);
                    if (t > maxTime) maxTime = t;
                    sb.AppendLine($"           times[{i}] = {t.ToString("F4", CultureInfo.InvariantCulture)}f;");
                }
                
                // Extract individual matrix arrays
                var transformStr = jm.Groups[3].Value;
                var matrixMatches = Regex.Matches(transformStr, @"\[(.*?)\]", RegexOptions.Singleline);
                
                for(int i = 0; i < matrixMatches.Count; i++)
                {
                    string[] vals = matrixMatches[i].Groups[1].Value.Split(',');
                    sb.AppendLine($"           matrices[{i}] = new Matrix4x4(");
                    sb.Append("             ");
                    for(int v = 0; v < 16; v++)
                    {
                        float val = float.Parse(vals[v].Trim(), CultureInfo.InvariantCulture);
                        sb.Append($"{val.ToString("F6", CultureInfo.InvariantCulture)}f");
                        if (v < 15) sb.Append(", ");
                    }
                    sb.AppendLine(");");
                }
                
                sb.AppendLine($"           def.Joints[\"{boneName}\"] = new AttackJointKeyframe {{ Times = times, Matrices = matrices }};");
                sb.AppendLine($"       }}");
            }
            sb.AppendLine($"       def.TotalDuration = {maxTime.ToString("F4", CultureInfo.InvariantCulture)}f;");
            sb.AppendLine($"       dict[\"{animId}\"] = def;");
            sb.AppendLine($"    }}");
        }
        
        sb.AppendLine("    return dict;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("}");
        
        File.WriteAllText(@"scratch\EpicFightAnimData.cs", sb.ToString());
        Console.WriteLine("Generated EpicFightAnimData.cs");
    }
}
