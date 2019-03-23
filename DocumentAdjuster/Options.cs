using CommandLine;

namespace DocumentAdjuster
{
    public class Options
    {
        [Option('n', "name", Required = true, HelpText = "Document file name")]
        public string FileName { get; set; }

        [Option('d', "debug", Required = false, HelpText = "Debug mode")]
        public bool Debug { get; set; }
    }
}
