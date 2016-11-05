
ECHO Run Tests
"packages\Microsoft.DotNet.xunit.performance.runner.Windows.1.0.0-alpha-build0041\tools\xunit.performance.run.exe" "VirtoCommerce.SearchApiModule.Test\bin\Debug\VirtoCommerce.SearchApiModule.Test.dll" -runner "packages\xunit.runner.console.2.2.0-beta2-build3300\tools\xunit.console.exe" -trait "category=performance" -parallel none -verbose -diagnostics -runid Performance

ECHO Generate Reports
"packages\Microsoft.DotNet.xunit.performance.analysis.1.0.0-alpha-build0041\tools\xunit.performance.analysis.exe" Performance.xml -html results.html