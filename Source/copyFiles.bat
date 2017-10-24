@echo off
SET "ProjectName=ExpandedRoofing"
SET "SolutionDir=C:\Users\robin\Desktop\Games\RimWorld Modding\Source\ExpandedRoofing\Source"
@echo on

xcopy /S /Y "%SolutionDir%\..\About\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\About\"
xcopy /S /Y "%SolutionDir%\..\Patches\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Patches\"