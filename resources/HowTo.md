# How To

## Copy Build

Um die Buildartefakte auf den Raspberry PI zu laden kann `rsync` verwendet werden. Folgendes muss zum `.csproj` hinzugefügt werden:
```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="rsync -avz --delete $(TargetDir) pi@hslu-rpi:netcore/$(ProjectName)" />
</Target>
```

## Remote Debugging

Um das Programm auf dem Raspbarry Pi zu testen muss _.NET Remote Debugger (vsdbg)_ auf dem RPI installieren werden:
```sh
curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l ~/vsdbg
```

Erstelle oder öffne in VS Code die Datei .vscode/launch.json und füge folgende Konfiguration hinzu:
```json
{
    "name": "Remote Debug RPI",
    "type": "coreclr",
    "request": "launch",
    "program": "~/netcore/Zumo.Labyrinth/Zumo.Labyrinth.dll",
    "args": [],
    "cwd": "~/netcore/Zumo.Labyrinth",
    "stopAtEntry": false,
    "console": "internalConsole",
    "pipeTransport": {
        "pipeCwd": "${workspaceFolder}",
        "pipeProgram": "/usr/bin/ssh",
        "pipeArgs": [
            "pi@hslu-rpi"
        ],
        "debuggerPath": "~/vsdbg/vsdbg"
    }
}
```

Passe bei Bedarf das Login `pi@hslu-rpi` und den Pfad zur DLL an.
