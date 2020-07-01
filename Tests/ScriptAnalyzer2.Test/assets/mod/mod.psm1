function Test-HasWriteHost
{
    [Microsoft.PowerShell.ScriptAnalyzer.Rules.Rule('TestHasWriteHost', 'Is friends')]
    param(
      $Ast,
      $Tokens,
      $ScriptPath
    )

    foreach ($result in $Ast.FindAll({ $args[0] -is [System.Management.Automation.Language.CommandAst] }, $true))
    {
        [System.Management.Automation.Language.CommandAst]$result = $result
        if ($result.GetCommandName() -eq 'Write-Host')
        {
            Write-Diagnostic -Extent $result.Extent -Message 'Bad'
        }
    }
}
