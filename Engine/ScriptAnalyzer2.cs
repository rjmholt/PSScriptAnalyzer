using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer
{
    public interface IScriptAnalyzer
    {
    }

    public interface IAsyncScriptAnalyzer
    {

    }

    public interface IScriptFixer
    {

    }

    public interface IAsyncScriptFixer
    {

    }

    public interface IScriptFormatter
    {

    }

    public interface IAsyncScriptFormatter
    {

    }

    public interface IResettable
    {
        void Reset();
    }

    internal class ScriptAnalyzer2
    {
    }
}
