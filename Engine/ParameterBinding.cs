using System.Management.Automation.Language;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer
{
    public static class ParameterBinding
    {
        public ParameterInvocationInfo BindParameters(CommandAst commandAst)
        {

        }
    }

    public interface ICommandParameterInfo
    {
        bool TryGetPositionalParameter(int position, out string name);

        bool TryGetParameter(string name, out int position);
    }

    public class ParameterBindingResult
    {

    }
}