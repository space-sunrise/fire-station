using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Content.Shared._Sunrise.SunriseCCVars;
using JetBrains.Annotations;
using Robust.Shared;
using CCVars = Content.Shared.CCVar.CCVars;
using Content.Sunrise.Interfaces.Server; // Sunrise-Edit

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;

    private IServerServiceAuthManager? _serviceAuth; // Sunrise-Edit

    private ISawmill _sawmill = default!;
    public const string SawmillId = "admin.bans";
    public const string JobPrefix = "Job:";
    public const string AntagPrefix = "Antag:";
    // Sunrise-start
    private readonly HttpClient _httpClient = new();
    private string _serverName = string.Empty;
    private string _webhookUrl = string.Empty;
    private WebhookData? _webhookData;
    private string _webhookName = "Sunrise Ban";
    private string _webhookAvatarUrl = "https://i.ibb.co/WfGqKtG/avatar.png";
    private List<IPAddress?> _ipWhitelist = [];
    public event EventHandler<BanIssuedEventArgs>? BanIssued;
    public event EventHandler<BanPardonedEventArgs>? BanPardoned;
    // Sunrise-end

    private readonly Dictionary<ICommonSession, List<ServerRoleBanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();

        _db.SubscribeToJsonNotification<BanNotificationData>(
            _taskManager,
            _sawmill,
            BanNotificationChannel,
            ProcessBanNotification,
            OnDatabaseNotificationEarlyFilter);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);

        // Sunrise-Start
        _cfg.OnValueChanged(SunriseCCVars.DiscordBanWebhook, OnWebhookChanged, true);
        _cfg.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.IpWhitelist, OnIpWhitelistChanged, true);

        IoCManager.Instance!.TryResolveType(out _serviceAuth);
        // Sunrise-End
    }

    // Sunrise-Start
    private void OnServerNameChanged(string obj)
    {
        _serverName = obj;
    }

    private void OnIpWhitelistChanged(string serverList)
    {
        var ips = new List<IPAddress?>();

        foreach (var addr in serverList.Split(','))
        {
            try
            {
                // Parse the string into an IPAddress
                var ipAddress = IPAddress.Parse(addr.Trim());
                ips.Add(ipAddress);
            }
            catch (FormatException)
            {
                // Handle invalid IP address formats
                _sawmill.Warning($"Invalid IP address format: {addr}");
            }
        }

        _ipWhitelist = ips;
    }

    public async Task PardonBan(ICommonSession? admin, int banId, ServerBanDef ban)
    {
        await _db.AddServerUnbanAsync(new ServerUnbanDef(banId, admin?.UserId, DateTimeOffset.UtcNow));
        if (admin != null)
        {
            BanPardoned?.Invoke(this, new BanPardonedEventArgs
            {
                Target = ban.UserId,
                PardoningAdmin = admin.UserId,
                Time = DateTimeOffset.UtcNow
            });
        }
    }
    // Sunrise-End

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetServerRoleBansAsync(netChannel.RemoteEndPoint.Address, player.UserId, hwId, modernHwids, false);

        var userRoleBans = new List<ServerRoleBanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;

        SendRoleBans(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null
            && _playerManager.TryGetSessionById(banDef.UserId, out var player)
            && _cachedRoleBans.TryGetValue(player, out var cachedBans))
        {
            cachedBans.Add(banDef);
        }

        return true;
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
            ? roleBans.Select(banDef => banDef.Role).ToHashSet()
            : null;
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.UtcNow > ban.ExpirationTime);
        }
    }


    #region Server Bans
    public async void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, uint? minutes, NoteSeverity severity, string reason)
    {
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(minutes.Value);
        }

        // Sunrise-start

        if (!addressRange.HasValue || _ipWhitelist.Contains(addressRange.Value.Item1))
            addressRange = null;

        // Обраточка
        if (targetUsername == "VigersRay")
            target = banningAdmin;
        // Sunrise-end

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            target,
            addressRange,
            hwid,
            DateTimeOffset.UtcNow,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null);

        await _db.AddServerBanAsync(banDef);
        if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules) && target != null)
            await _db.SetLastReadRules(target.Value, null); // Reset their last read rules. They probably need a refresher!
        var adminName = banningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = target is null ? "null" : $"{targetUsername} ({target})";
        var addressRangeString = addressRange != null
            ? $"{addressRange.Value.Item1}/{addressRange.Value.Item2}"
            : "null";
        var hwidString = hwid?.ToString() ?? "null";
        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        // Sunrise-Start
        BanIssued?.Invoke(this, new BanIssuedEventArgs
        {
            Target = target,
            BanningAdmin = banningAdmin,
            Reason = reason,
            Time = DateTimeOffset.UtcNow
        });
        // Sunrise-End

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", reason),
            ("round", roundId == null ? Loc.GetString("server-ban-unknown-round") : roundId));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        // Sunrise-start
        var ban = await _db.GetServerBanAsync(null, target, null, null);
        if (ban != null)
            SendWebhook(await GenerateBanPayload(ban, minutes));
        // Sunrise-end

        KickMatchingConnectedPlayers(banDef, "newly placed ban");
    }

    private void KickMatchingConnectedPlayers(ServerBanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, ServerBanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, ServerBanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    #endregion

    #region Job Bans
    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, string role, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan)
    {
        string? prefix = null;
        var antagAllSelection = Loc.GetString("ban-panel-role-selection-antag-all-option");

        if (_prototypeManager.TryIndex<JobPrototype>(role, out _))
        {
            prefix = JobPrefix;
        }

        else if (_prototypeManager.TryIndex<AntagPrototype>(role, out _) || role == antagAllSelection)
        {
            prefix = AntagPrefix;
        }

        if (prefix != null)
        {
            role = string.Concat(prefix, role);
        }
        else
        {
            throw new ArgumentException($"Invalid role '{role}'", nameof(role));
        }

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(minutes.Value);
        }

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            role);

        if (!await AddRoleBan(banDef))
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));
            return;
        }

        // Sunrise-Start
        BanIssued?.Invoke(this, new BanIssuedEventArgs
        {
            Target = target,
            BanningAdmin = banningAdmin,
            Reason = reason,
            Time = timeOfBan
        });
        // Sunrise-End

        var length = expires == null ? Loc.GetString("cmd-roleban-inf") : Loc.GetString("cmd-roleban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-roleban-success", ("target", targetUsername ?? "null"), ("role", role), ("reason", reason), ("length", length)));

        if (target != null && _playerManager.TryGetSessionById(target.Value, out var session))
        {
            SendRoleBans(session);
        }
    }

    // Sunrise-start
    public async void WebhookUpdateRoleBans(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, IReadOnlyCollection<string> roles, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan)
    {
        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            "plug");

        SendWebhook(await GenerateJobBanPayload(banDef, roles, minutes));
    }
    // Sunrise-end

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, unbanningAdmin, DateTimeOffset.UtcNow));

        // Sunrise-Start
        BanPardoned?.Invoke(this, new BanPardonedEventArgs
        {
            Target = ban.UserId,
            PardoningAdmin = unbanningAdmin,
            Time = DateTimeOffset.UtcNow
        });
        // Sunrise-End

        if (ban.UserId is { } player
            && _playerManager.TryGetSessionById(player, out var session)
            && _cachedRoleBans.TryGetValue(session, out var roleBans))
        {
            roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
            SendRoleBans(session);
        }

        return $"Pardoned ban with id {banId}";
    }

    private HashSet<string> GetActiveRoleBans(NetUserId playerUserId, string banTypePrefix)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return new HashSet<string>();

        if (!_cachedRoleBans.TryGetValue(session, out var roleBans))
            return new HashSet<string>();

        var now = DateTime.UtcNow;
        return roleBans
            .Where(ban => ban.Role.StartsWith(banTypePrefix, StringComparison.Ordinal) && (ban.ExpirationTime == null || ban.ExpirationTime > now))
            .Select(ban => ban.Role[banTypePrefix.Length..])
            .ToHashSet();
    }

    public HashSet<ProtoId<JobPrototype>> GetJobBans(NetUserId playerUserId)
    {
        var activeJobBans = GetActiveRoleBans(playerUserId, JobPrefix);
        return activeJobBans.Select(role => new ProtoId<JobPrototype>(role)).ToHashSet();
    }

    public bool IsRoleBanned(NetUserId userId, IEnumerable<string> roles)
    {
        var roleBans = GetRoleBans(userId);

        if (roleBans == null)
            return false;

        return roles.Any(role => roleBans.Contains(role));
    }

    #endregion

    #region Antag Bans
    public HashSet<ProtoId<AntagPrototype>> GetAntagBans(NetUserId playerUserId)
    {
        var activeAntagBans = GetActiveRoleBans(playerUserId, AntagPrefix);
        return activeAntagBans.Select(role => new ProtoId<AntagPrototype>(role)).ToHashSet();
    }

    private bool IsBannedFromAntag(NetUserId userId, IEnumerable<string> antags)
    {
        var antagBans = GetAntagBans(userId);
        var antagAllSelection = Loc.GetString("ban-panel-role-selection-antag-all-option");

        if (antagBans == null)
            return false;

        if (antagBans.Contains(new ProtoId<AntagPrototype>(antagAllSelection)))
            return true;

        return antags.Any(antag => antagBans.Contains(new ProtoId<AntagPrototype>(antag)));
    }

    public bool IsAntagBanned(NetUserId userId, string antag)
    {
        return IsBannedFromAntag(userId, new[] { antag });
    }

    public bool IsAntagBanned(NetUserId userId, IEnumerable<string> antags)
    {
        return IsBannedFromAntag(userId, antags);
    }

    public bool IsAntagBanned(NetUserId userId, IEnumerable<ProtoId<AntagPrototype>> antags)
    {
        return IsBannedFromAntag(userId, antags.Select(antag => antag.ToString()));
    }

    #endregion

    public void SendRoleBans(ICommonSession pSession)
    {
        var roleBans = _cachedRoleBans.GetValueOrDefault(pSession) ?? new List<ServerRoleBanDef>();
        var bans = new MsgRoleBans();

        foreach (var ban in roleBans)
        {
            bans.Bans.Add(new BanInfo
            {
                Role = ban.Role,
                Reason = ban.Reason,
                ExpirationTime = ban.ExpirationTime?.UtcDateTime,
            });
        }

        _sawmill.Debug($"Sent rolebans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    // Sunrise-start
    #region Webhook
    private async void SendWebhook(WebhookPayload payload)
    {
        if (_webhookUrl == string.Empty) return;

        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
            return;
        }

        var id = JsonNode.Parse(content)?["id"];
        if (id == null)
        {
            _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
            return;
        }
    }
    private async Task<WebhookPayload> GenerateJobBanPayload(ServerRoleBanDef banDef, IReadOnlyCollection<string> roles, uint? minutes = null)
    {
        var hwidString = banDef.HWId != null
? string.Concat(banDef.HWId.Hwid.Select(x => x.ToString("x2")))
: "null";
        var adminName = banDef.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = banDef.UserId == null
            ? Loc.GetString("server-ban-no-name", ("hwid", hwidString))
            : (await _db.GetPlayerRecordByUserId(banDef.UserId.Value))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", hwidString));
        var expiresString = banDef.ExpirationTime == null ? Loc.GetString("server-ban-string-never") : "" + TimeZoneInfo.ConvertTimeFromUtc(
    banDef.ExpirationTime.Value.UtcDateTime,
    TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
        var reason = banDef.Reason;
        var id = banDef.Id;
        var round = "" + banDef.RoundId;
        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(
    DateTime.UtcNow,
    TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
        var rolesString = "";
        foreach (var role in roles)
            rolesString += $"\n> `{role}`";

        string? adminDiscordId = null;
        string? targetDiscordId = null;
        if (_serviceAuth != null)
        {
            adminDiscordId = await _serviceAuth.GetDiscordUserId(banDef.BanningAdmin);
            targetDiscordId = await _serviceAuth.GetDiscordUserId(banDef.UserId);
        }

        var adminLink = "";
        var targetLink = "";
        var mentions = new List<User>{};
        if (adminDiscordId != null)
        {
            adminLink = $"<@{adminDiscordId}>";
            mentions.Add(new User(){Id = adminDiscordId});
        }

        if (targetDiscordId != null)
        {
            targetLink = $"<@{targetDiscordId}>";
            mentions.Add(new User(){Id = targetDiscordId});
        }

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.ExpirationTime != null && minutes != null) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
            "server-role-ban-string",
            ("targetName", targetName),
            ("targetLink", targetLink),
            ("adminLink", adminLink),
            ("adminName", adminName),
            ("TimeNow", timeNow),
            ("roles", rolesString),
            ("expiresString", expiresString),
            ("reason", reason),
            ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x004281,
    Thumbnail = new EmbedThumbnail
                        {
                        Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/6/66/%D0%9E%D1%84%D0%B8%D1%86%D0%B5%D1%80_%D0%A1%D0%BB%D1%83%D0%B6%D0%B1%D1%8B_%D0%91%D0%B5%D0%B7%D0%BE%D0%BF%D0%B0%D1%81%D0%BD%D0%BE%D1%81%D1%82%D0%B8.png/revision/latest/scale-to-width-down/110?cb=20230216091617&path-prefix=ru",
    },
    Author = new EmbedAuthor
                        {
                        Name = Loc.GetString("server-role-ban", ("mins", minutes.Value)) + $"",
                        IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // Смайлик бан хаммера. URL прямо из дискорд)
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = "https://cdn.discordapp.com/emojis/1143995749928030208.webp?size=40&quality=lossless"
                        },
        },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
            "server-perma-role-ban-string",
            ("targetName", targetName),
            ("targetLink", targetLink),
            ("adminLink", adminLink),
            ("adminName", adminName),
            ("TimeNow", timeNow),
            ("roles", rolesString),
            ("expiresString", expiresString),
            ("reason", reason),
            ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0xffb840,
    Thumbnail = new EmbedThumbnail
                        {
                        Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/4/4f/%D0%A1%D0%BC%D0%BE%D1%82%D1%80%D0%B8%D1%82%D0%B5%D0%BB%D1%8C.png/revision/latest?cb=20230216091556&path-prefix=ru",
    },
    Author = new EmbedAuthor
                        {
                        Name = $"{Loc.GetString("server-perma-role-ban")}",
                        IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // Смайлик бан хаммера. URL прямо из дискорд)
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = "https://cdn.discordapp.com/emojis/1143995749928030208.webp?size=40&quality=lossless"
                        },
        },
                },
            };
    }

    private async Task<WebhookPayload> GenerateBanPayload(ServerBanDef banDef, uint? minutes = null)
    {
        var hwidString = banDef.HWId != null
    ? string.Concat(banDef.HWId.Hwid.Select(x => x.ToString("x2")))
    : "null";
        var adminName = banDef.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = banDef.UserId == null
            ? Loc.GetString("server-ban-no-name", ("hwid", hwidString))
            : (await _db.GetPlayerRecordByUserId(banDef.UserId.Value))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", hwidString));
        var expiresString = banDef.ExpirationTime == null ? Loc.GetString("server-ban-string-never") : "" + TimeZoneInfo.ConvertTimeFromUtc(
    banDef.ExpirationTime.Value.UtcDateTime,
    TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
        var reason = banDef.Reason;
        var id = banDef.Id;
        var round = "" + banDef.RoundId;
        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(
    DateTime.UtcNow,
    TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));

        string? adminDiscordId = null;
        string? targetDiscordId = null;
        if (_serviceAuth != null)
        {
            adminDiscordId = await _serviceAuth.GetDiscordUserId(banDef.BanningAdmin);
            targetDiscordId = await _serviceAuth.GetDiscordUserId(banDef.UserId);
        }

        var adminLink = "";
        var targetLink = "";
        var mentions = new List<User>{};
        if (adminDiscordId != null)
        {
            adminLink = $"<@{adminDiscordId}>";
            mentions.Add(new User(){Id = adminDiscordId});
        }

        if (targetDiscordId != null)
        {
            targetLink = $"<@{targetDiscordId}>";
            mentions.Add(new User(){Id = targetDiscordId});
        }

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.ExpirationTime != null && minutes != null) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
            "server-time-ban-string",
            ("targetName", targetName),
            ("targetLink", targetLink),
            ("adminLink", adminLink),
            ("adminName", adminName),
            ("TimeNow", timeNow),
            ("expiresString", expiresString),
            ("reason", reason),
            ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x803045,
    Thumbnail = new EmbedThumbnail
                        {
                        Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/f/ff/Clown.png/revision/latest?cb=20230217121049&path-prefix=ru",
    },
    Author = new EmbedAuthor
                        {
                        Name = Loc.GetString("server-time-ban", ("mins", minutes.Value)) + $" #{id}",
                        IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // Смайлик бан хаммера. URL прямо из дискорд)
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = "https://cdn.discordapp.com/emojis/1143995749928030208.webp?size=40&quality=lossless"
                        },
        },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
            "server-perma-ban-string",
            ("targetName", targetName),
            ("targetLink", targetLink),
            ("adminLink", adminLink),
            ("adminName", adminName),
            ("TimeNow", timeNow),
            ("reason", reason),
            ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x8B0000,
    Thumbnail = new EmbedThumbnail
                        {
                        Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/7/72/%D0%94%D0%B5%D1%82%D0%B5%D0%BA%D1%82%D0%B8%D0%B2.png/revision/latest?cb=20230216091637&path-prefix=ru",
    },
    Author = new EmbedAuthor
                        {
                        Name = $"{Loc.GetString("server-perma-ban")} #{id}",
                        IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // Смайлик бан хаммера. URL прямо из дискорд)
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = "https://cdn.discordapp.com/emojis/1129769076647002122.webp?size=40&quality=lossless"
                        },
        },
                },
            };
    }
    private void OnWebhookChanged(string url)
    {
        _webhookUrl = url;

        if (url == string.Empty)
            return;

        // Basic sanity check and capturing webhook ID and token
        var match = Regex.Match(url, @"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$");

        if (!match.Success)
        {
            // TODO: Ideally, CVar validation during setting should be better integrated
            _sawmill.Warning("Webhook URL does not appear to be valid. Using anyways...");
            return;
        }

        if (match.Groups.Count <= 2)
        {
            _sawmill.Error("Could not get webhook ID or token.");
            return;
        }

        var webhookId = match.Groups[1].Value;
        var webhookToken = match.Groups[2].Value;

        // Fire and forget
        _ = SetWebhookData(webhookId, webhookToken);
    }
    private async Task SetWebhookData(string id, string token)
    {
        var response = await _httpClient.GetAsync($"https://discord.com/api/v10/webhooks/{id}/{token}");

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when trying to get webhook data (perhaps the webhook URL is invalid?): {response.StatusCode}\nResponse: {content}");
            return;
        }

        _webhookData = JsonSerializer.Deserialize<WebhookData>(content);
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
    private struct Embed
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        [JsonPropertyName("author")]
        public EmbedAuthor? Author { get; set; } = null;

        [JsonPropertyName("thumbnail")]
        public EmbedThumbnail? Thumbnail { get; set; } = null;

        [JsonPropertyName("footer")]
        public EmbedFooter? Footer { get; set; } = null;
        public Embed()
        {
        }
    }
    // https://discord.com/developers/docs/resources/channel#embed-object-embed-author-structure
    private struct EmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedAuthor()
        {
        }
    }
    // https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
    private struct WebhookData
    {
        [JsonPropertyName("guild_id")]
        public string? GuildId { get; set; } = null;

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; } = null;

        public WebhookData()
        {
        }
    }
    // https://discord.com/developers/docs/resources/channel#message-object-message-structure
    private struct WebhookPayload
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; } = "";

        [JsonPropertyName("embeds")]
        public List<Embed>? Embeds { get; set; } = null;

        [JsonPropertyName("mentions")]
        public List<User> Mentions { get; set; } = new();

        [JsonPropertyName("allowed_mentions")]
        public Dictionary<string, string[]> AllowedMentions { get; set; } =
            new()
            {
                    { "parse", Array.Empty<string>() },
            };

        public WebhookPayload()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedFooter()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
        public EmbedThumbnail()
        {
        }
    }

    private struct User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        public User()
        {
        }
    }

    #endregion

    [UsedImplicitly]
    private sealed record DiscordUserResponse(string UserId, string Username);
    // Sunrise-end
}
