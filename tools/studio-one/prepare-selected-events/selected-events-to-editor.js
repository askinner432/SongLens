function SelectedEventsToEditor()
{
    this.interfaces = [Host.Interfaces.IEditTask];

    this.prepareEdit = function (context)
    {
        return Host.Results.kResultOk;
    };

    this.performEdit = function (context)
    {
        var commands = Host.GUI.Commands;
        var steps = [
            {
                label: "Select Tracks with Selected Events",
                candidates: [
                    ["Scoring Tools", "Select Tracks with Selected Events"],
                    ["Track", "Select Tracks with Selected Events"]
                ]
            },
            {
                label: "Apply Track Names to Events",
                candidates: [
                    ["Track", "Apply Track Names to Events"]
                ]
            },
            {
                label: "Apply Track Names to Channels",
                candidates: [
                    ["Track", "Apply Track Names to Channels"]
                ]
            },
            {
                label: "Apply Track Names to Instruments",
                candidates: [
                    ["Track", "Apply Track Names to Instruments"]
                ]
            },
            {
                label: "Solo Selected Events",
                candidates: [
                    ["Track", "Solo Selected Events"],
                    ["Macros", "Solo Selected Events"],
                    ["Track", "Solo"]
                ]
            },
            {
                label: "Assign Unique Track Colors",
                candidates: [
                    ["Scoring Tools", "Assign Unique Track Colors"],
                    ["Track", "Assign Unique Track Colors"],
                    ["Macros", "Assign Unique Track Colors"],
                    ["Track", "Colorize Tracks"]
                ]
            },
            {
                label: "Show Editor",
                candidates: [
                    ["View", "Editor"]
                ]
            }
        ];

        var resolved = [];
        var missing = [];
        var stepIndex;
        for (stepIndex = 0; stepIndex < steps.length; stepIndex++)
        {
            var step = steps[stepIndex];
            var match = null;
            var candidateIndex;
            for (candidateIndex = 0; candidateIndex < step.candidates.length; candidateIndex++)
            {
                var candidate = step.candidates[candidateIndex];
                try
                {
                    if (commands.findCommand(candidate[0], candidate[1]))
                    {
                        match = candidate;
                        break;
                    }
                }
                catch (error)
                {
                    // Try the next known category/name combination.
                }
            }

            if (match)
            {
                resolved.push(match);
            }
            else
            {
                missing.push(step.label);
            }
        }

        if (missing.length > 0)
        {
            Host.GUI.alert(
                "Selected Events to Editor could not find:\n\n- " +
                missing.join("\n- ") +
                "\n\nInstall or enable the commands, then restart Studio One.");
            return Host.Results.kResultFailed;
        }

        var commandIndex;
        for (commandIndex = 0; commandIndex < resolved.length; commandIndex++)
        {
            var command = resolved[commandIndex];
            if (typeof commands.deferCommand === "function")
            {
                commands.deferCommand(command[0], command[1]);
            }
            else
            {
                commands.interpretCommand(command[0], command[1]);
            }
        }

        return Host.Results.kResultOk;
    };
}

function createInstance()
{
    return new SelectedEventsToEditor();
}
