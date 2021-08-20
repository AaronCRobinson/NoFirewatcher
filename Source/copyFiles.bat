@echo off
SET "ProjectName=NoFirewatcher"
SET "SolutionDir=C:\Users\robin\Desktop\Games\RimWorld Modding\Source\NoFirewatcher\Source"
SET "RWModsDir=D:\SteamLibrary\steamapps\common\RimWorld\Mods"
@echo on

xcopy /S /Y "%SolutionDir%\..\About\*" "%RWModsDir%\%ProjectName%\About\"
xcopy /S /Y "%SolutionDir%\..\Assemblies\*" "%RWModsDir%\%ProjectName%\Assemblies\"