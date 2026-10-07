param([string]$Name, [string]$Id, [string]$Type, [ValidateSet('tree','invoke','select','value','toggle')][string]$Action='tree', [string]$Value, [int]$ProcessId=0)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$hubCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'ProjectHub')
$hubCondition=if($ProcessId){New-Object System.Windows.Automation.AndCondition($hubCondition,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$ProcessId)))}else{$hubCondition}
$hubWindow=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$hubCondition)
if (!$hubWindow) { throw 'ProjectHub window unavailable' }
if ($Action -eq 'tree') {
    $hubWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { [pscustomobject]@{ Name=$_.Current.Name; Id=$_.Current.AutomationId; Type=$_.Current.ControlType.ProgrammaticName; Handle=$_.Current.NativeWindowHandle } } | ConvertTo-Json -Depth 2
    exit
}
$hubProperty=if($Id){[System.Windows.Automation.AutomationElement]::AutomationIdProperty}else{[System.Windows.Automation.AutomationElement]::NameProperty}
$hubText=if($Id){$Id}else{$Name}
$hubMatch=New-Object System.Windows.Automation.PropertyCondition($hubProperty,$hubText)
if($Type){$hubMatch=New-Object System.Windows.Automation.AndCondition($hubMatch,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::$Type)))}
$hubTarget=$hubWindow.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$hubMatch)
if (!$hubTarget) { throw "Control not found: $hubText" }
switch($Action) {
    'invoke' { $hubTarget.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
    'select' { $hubTarget.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() }
    'value' { $hubTarget.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Value) }
    'toggle' { $hubTarget.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
}
