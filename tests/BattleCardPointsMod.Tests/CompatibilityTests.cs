using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BattleCardPointsMod.Tests
{
    public class CompatibilityTests
    {
        [Fact]
        public void ModSourcesAvoidKnownMissingHybridClrBcl()
        {
            var root = typeof(CompatibilityTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "ModSourceRoot").Value;
            var dir = Path.GetFullPath(root);
            var files = Directory.GetFiles(dir, "*.cs");
            Assert.True(files.Length >= 4, $"Found {files.Length} source files in {dir}");

            var forbidden = new[]
            {
                "DllImport", "new FileStream", "new FileInfo", "new DirectoryInfo",
                "new StreamReader", "new StreamWriter", "Flush(true)", "Flush(false)", "File.ReadAllLines",
                "AppDomain", "Assembly.Location", "FromUnixTime"
            };

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                foreach (var pattern in forbidden)
                {
                    Assert.DoesNotContain(pattern, content);
                }
            }
        }
    }
}
