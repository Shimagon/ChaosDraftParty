using UnrealBuildTool;
using System.Collections.Generic;

public class ChaosDraftPartyUETarget : TargetRules
{
	public ChaosDraftPartyUETarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.V7;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.Add("ChaosDraftPartyUE");
	}
}
