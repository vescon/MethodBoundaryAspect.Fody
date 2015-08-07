SET NUGET=%~dp0..\..\_CreateNewNuGetPackage\DoNotModify\nuget.exe

"%NUGET%" push "*.nupkg" -Source "http://nuget.vescon.com" -ApiKey "nuget"
pause