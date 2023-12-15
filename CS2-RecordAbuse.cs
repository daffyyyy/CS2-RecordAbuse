using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API;
using System.Text;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

namespace CS2_RecordAbuse;

[MinimumApiVersion(126)]
public class CS2_RecordAbuse : BasePlugin, IPluginConfig<CS2_RecordAbuseConfig>
{
	public static IStringLocalizer? _localizer;
	public readonly Random _random = new Random();
	public bool _activeDemo = false;
	public string? _nameDemo;
	public bool _enabledDem = true;
	public CS2_RecordAbuseConfig Config { get; set; } = new();
	public override string ModuleName => "CS2-RecordAbuse";
	public override string ModuleDescription => "Allows admins to record abuse";
	public override string ModuleAuthor => "daffyy";
	public override string ModuleVersion => "1.0.0";

	public override void Load(bool hotReload)
	{
		RegisterListener<OnMapStart>(OnMapStart);

		if (!Directory.Exists($"{ModuleDirectory}/demos"))
		{
			Directory.CreateDirectory($"{ModuleDirectory}/demos");
		}
	}

	public void OnConfigParsed(CS2_RecordAbuseConfig config)
	{
		// Maybe for future use
		Config = config;
		_localizer = Localizer;
	}

	private void OnMapStart(string mapName)
	{
		bool? tvStatus = ConVar.Find("tv_enable")!.GetPrimitiveValue<bool>();

		if (tvStatus == null || tvStatus == false)
		{
			_enabledDem = false;
			Logger.LogError("tv_enable must be true!");
			return;
		}

		Server.ExecuteCommand("tv_stoprecord");
		Server.ExecuteCommand("tv_autorecord 0");
		Server.ExecuteCommand("tv_relayvoice 1");
		Server.ExecuteCommand("tv_maxclients 1");
		_activeDemo = false;
		_nameDemo = null;
	}

	[ConsoleCommand("css_record")]
	[CommandHelper(minArgs: 1, usage: "<start/stop/status>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/ban")]
	public void OnRecordCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (_enabledDem == false)
		{
			Logger.LogError("tv_enable must be true!");
			return;

		}
		string action = command.GetArg(1);
		if (action != "start" && action != "stop" && action != "status")
			return;

		StringBuilder sb = new(_localizer!["ra_prefix"]);
		if (action == "status")
		{
			sb.Append(_localizer["ra_status", _activeDemo ? "Recording" : "Not Recording"]);
			caller!.PrintToChat(sb.ToString());
			return;
		}
		if (action == "stop")
		{
			if (_activeDemo)
			{
				_activeDemo = false;

				if (_nameDemo == null || _nameDemo.Length <= 1)
				{
					Server.ExecuteCommand("tv_stoprecord");
					return;
				}

				Server.ExecuteCommand("tv_stoprecord");
				sb.Append(_localizer["ra_stop", $"{_nameDemo.Split("/").Last()}.dem"]);
				caller!.PrintToChat(sb.ToString());
				_nameDemo = null;
				return;
			}
		}

		if (action == "start")
		{
			if (_activeDemo)
			{
				sb.Append(_localizer["ra_status", _activeDemo ? "Recording" : "Not Recording"]);
				caller!.PrintToChat(sb.ToString());
				return;
			}

			_nameDemo = $"{ModuleDirectory}/demos/{RandomString(12)}";
			Server.ExecuteCommand($"tv_record {_nameDemo}");
			_activeDemo = true;
			sb.Append(_localizer["ra_start"]);
			caller!.PrintToChat(sb.ToString());
			return;
		}
	}

	public string RandomString(int size = 8, bool lowerCase = false)
	{
		var builder = new StringBuilder(size);

		char offset = lowerCase ? 'a' : 'A';
		const int lettersOffset = 26;

		for (var i = 0; i < size; i++)
		{
			var @char = (char)_random.Next(offset, offset + lettersOffset);
			builder.Append(@char);
		}

		return lowerCase ? builder.ToString().ToLower() : builder.ToString();
	}

}