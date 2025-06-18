using System.Text;

namespace MMLGB
{
    public class MMLGB
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine("error: Wrong number of arguments.");
                Console.Error.WriteLine("Usage: MMLGB.exe MMLFILE OUTFILE [BANK]");
                Environment.Exit(1);
            }

            string inputPath = args[0];
            string outputPath = args[1];

            // Read file with UTF8 encoding
            string input = File.ReadAllText(inputPath, Encoding.UTF8);

            // Strip BOM if present
            if (input.Length > 0 && input[0] == '\uFEFF')
            {
                input = input.Substring(1);
            }

            // Normalize line endings to LF
            input = input.Replace("\r\n", "\n");

            var tokens = Lexer.Lex(input);
            var parser = new Parser(tokens);
            var song = parser.Parse();

            song.SetFileName(Path.GetFileName(outputPath));

            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false));
            writer.NewLine = "\n";

            if (outputPath.EndsWith(".asm", StringComparison.OrdinalIgnoreCase) ||
                outputPath.EndsWith(".s", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length == 3 && int.TryParse(args[2], out int bank))
                {
                    writer.Write($".area _CODE_{bank}\n");
                }

                // Normalize line endings in output string as well
                var asmOutput = song.EmitASM().Replace("\r\n", "\n");
                writer.Write(asmOutput);
            }
            else if (outputPath.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
            {
                var cOutput = song.EmitC().Replace("\r\n", "\n");
                writer.Write(cOutput);
            }

            writer.Flush();
        }
    }
}
