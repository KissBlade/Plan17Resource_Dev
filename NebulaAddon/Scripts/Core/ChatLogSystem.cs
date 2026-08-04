
using System.IO;
using System.Text;
using Virial.Helpers;
//本代码来源于凌寒冰歌。

namespace Plana.Core;

[HarmonyPatch]
public static class ChatLogSystemPatch
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat)),HarmonyPostfix]
    public static void AddChatPostfix(PlayerControl sourcePlayer, string chatText, bool __runOriginal)
    {
        if (!__runOriginal) return;
        ChatLogSystem.RecordFull(sourcePlayer, chatText);
    }
}

[NebulaRPCHolder]
public static class ChatLogSystem
{
    public const int ChannelPublic  = 0;
    private sealed class Entry
    {
        public System.DateTime Time;
        public int Round;
        public int ChannelTag;
        public byte SenderId;
        public string SenderName = "";
        public string Text = "";
    }

    private static readonly List<Entry> log = new();
    private sealed class FullEntry
    {
        public System.DateTime Time;
        public int Round;
        public string Name = ""; 
        public string Text = "";
    }
    private static readonly List<FullEntry> fullLog = new();

    private static int currentRound = 0;
    private static System.DateTime gameStartTime;

    private const string LogTag = "Plan17.ChatLog";
    private static string FileName = "Plan17_GameChatLog.txt";
    private static string FullFileName = "Plan17_GameChatLog_Full.txt";
    public static void ResetForNewGame()
    {
        log.Clear();
        fullLog.Clear();
        currentRound = 0;
        gameStartTime = System.DateTime.Now;
        FileName = $"Plan17_GameChatLog_{DateTime.Now:yyyy_MM_dd HH_mm}.txt";
        FullFileName = $"Plan17_GameChatLog_{DateTime.Now:yyyy_MM_dd HH_mm}_Full.txt";
    }

    public static void OnMeetingStarted()
    {
        currentRound++;
    }
    public static void Record(byte senderId, int channelTag, string text)
    {
        if (LobbyBehaviour.Instance.AsBoolFast()) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        log.Add(new Entry
        {
            Time = System.DateTime.Now,
            Round = currentRound,
            ChannelTag = channelTag,
            SenderId = senderId,
            SenderName = ResolveName(senderId),
            Text = text
        });
    }

    [NebulaRPC]
    public static void RpcLogChat(byte senderId, int channelTag, string text)
        => Record(senderId, channelTag, text);
    public static void RecordFull(PlayerControl sourcePlayer, string chatText)
    {
        if (LobbyBehaviour.Instance.AsBoolFast()) return;
        if (string.IsNullOrWhiteSpace(chatText)) return;
        string name = "系统";
        try
        {
            if (sourcePlayer != null && !string.IsNullOrEmpty(sourcePlayer.name))
                name = StripRichTags(sourcePlayer.name);
        }
        catch { }
        if (string.IsNullOrEmpty(name)) name = "系统";
        fullLog.Add(new FullEntry
        {
            Time = System.DateTime.Now,
            Round = currentRound,
            Name = name,
            Text = chatText
        });
    }
    private static string StripRichTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        try { return System.Text.RegularExpressions.Regex.Replace(s, "<[^>]*>", "").Trim(); }
        catch { return s; }
    }

    private static string ResolveName(byte senderId)
    {
        try
        {
            var gp = GamePlayer.GetPlayer(senderId);
            if (gp != null && !string.IsNullOrEmpty(gp.Name)) return gp.Name;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return "玩家#" + senderId;
    }

    private static string ChannelLabel(int tag)
    {
        return tag==100?"灵魂":"";
    }
    public static void ExportNow()
    {
        try { WriteReport(FullFileName, BuildFullReport(), "完整版", fullLog.Count); }
        catch (System.Exception e)
        { PDebug.Log("导出完整版失败: " + e.ToString()); }
        try { WriteReport(FileName, BuildReport(), "干净版", log.Count); }
        catch (System.Exception e)
        {  PDebug.Log("导出干净版失败: " + e.ToString()); }
    }
    private static void WriteReport(string fileName, string content, string label, int count)
    {
        var encoding = new UTF8Encoding(true);
        string written;
        try
        {
            string gameDir = Path.GetDirectoryName(UnityEngine.Application.dataPath)!;
            string gamechatlogDir = "GameChatLogs";
            var gamechatlogDirPath = Path.Combine(gameDir, gamechatlogDir);
            if (!Directory.Exists(gamechatlogDirPath))
            {
                Directory.CreateDirectory(gamechatlogDirPath);
            }
            string p = Path.Combine(gamechatlogDirPath, fileName);
            File.WriteAllText(p, content, encoding);
            written = p;
        }
        catch (Exception e)
        {
            PDebug.Log("导出失败");
            PDebug.Log(e);
            return;
        }
        try { NebulaAPI.Logging.NebulaLogger(LogTag).Message("本局聊天记录(" + label + ")已导出: " + written + "（共 " + count + " 条）"); } catch { }
    }

    private static string BuildReport()
    {
        var sb = new StringBuilder(8192);

        sb.AppendLine("========================================");
        sb.AppendLine("  沉浸-最终梦想 · 对局聊天记录（赛后复盘）");
        sb.AppendLine("========================================");
        sb.AppendLine("导出时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("本局开始(估): " + gameStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        try
        {
            var span = System.DateTime.Now - gameStartTime;
            sb.AppendLine("对局时长(估): " + ((int)span.TotalMinutes) + " 分 " + (span.Seconds) + " 秒");
        }
        catch { }
        sb.AppendLine("会议次数: " + currentRound);
        sb.AppendLine("聊天条数: " + log.Count);
        sb.AppendLine();

        AppendRoster(sb);
        AppendChatLog(sb);

        sb.AppendLine();
        sb.AppendLine("———— 记录结束 ————");
        return sb.ToString();
    }

    private static void AppendRoster(StringBuilder sb)
    {
        sb.AppendLine("—— 玩家名册（职业 / 附加职业 / 状态）——");
        try
        {
            var players = GamePlayer.AllPlayers.OrderBy(p => p.PlayerId).ToList();
            foreach (var p in players)
            {
                try
                {
                    string role = SafeRoleName(p);
                    string mods = SafeModifierNames(p);
                    string ghost = SafeGhostRoleName(p);
                    string status = p.IsDisconnected ? "掉线" : (p.IsDead ? "出局" : "存活");
                    string team = SafeTeam(p);

                    sb.Append("  [").Append(p.PlayerId).Append("] ").Append(p.Name);
                    sb.Append("   职业: ").Append(role);
                    if (!string.IsNullOrEmpty(team)) sb.Append(" (").Append(team).Append(")");
                    sb.Append("   附加: ").Append(string.IsNullOrEmpty(mods) ? "无" : mods);
                    if (!string.IsNullOrEmpty(ghost)) sb.Append("   死后职业: ").Append(ghost);
                    sb.Append("   状态: ").Append(status);
                    sb.AppendLine();
                }
                catch (System.Exception e)
                {
                    sb.Append("  [").Append(p.PlayerId).Append("] ").Append("(读取失败: ").Append(e.Message).AppendLine(")");
                }
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine("  (名册读取失败: " + e.Message + ")");
        }
        sb.AppendLine();
    }

    private static string SafeRoleName(GamePlayer p)
    {
        try { return p.Role?.Role?.DisplayName ?? "?"; }
        catch { return "?"; }
    }

    private static string SafeTeam(GamePlayer p)
    {
        try
        {
            var cat = p.Role?.Role?.Category;
            if (cat == null) return "";
            switch (cat.Value)
            {
                case RoleCategory.CrewmateRole: return "船员";
                case RoleCategory.ImpostorRole: return "内鬼";
                case RoleCategory.NeutralRole:  return "中立";
                default: return cat.Value.ToString();
            }
        }
        catch { return ""; }
    }

    private static string SafeModifierNames(GamePlayer p)
    {
        try
        {
            var mods = p.Modifiers;
            if (mods == null) return "";
            var names = new List<string>();
            foreach (var m in mods)
            {
                try
                {
                    var n = m?.Modifier?.DisplayName;
                    if (!string.IsNullOrEmpty(n)) names.Add(n);
                }
                catch { }
            }
            return string.Join(", ", names);
        }
        catch { return ""; }
    }

    private static string SafeGhostRoleName(GamePlayer p)
    {
        try
        {
            var g = p.GhostRole;
            if (g == null) return "";
            return g.Role?.DisplayName ?? "";
        }
        catch { return ""; }
    }

    private static void AppendChatLog(StringBuilder sb)
    {
        sb.AppendLine("========================================");
        sb.AppendLine("  聊天记录（按时间顺序）");
        sb.AppendLine("========================================");

        if (log.Count == 0)
        {
            sb.AppendLine("  （本局没有捕获到任何发言）");
            return;
        }

        int lastRound = int.MinValue;
        foreach (var e in log)
        {
            if (e.Round != lastRound)
            {
                lastRound = e.Round;
                sb.AppendLine();
                sb.AppendLine(e.Round == 0 ? "【开局 / 首次会议前】" : ("【第 " + e.Round + " 次会议】"));
            }
            sb.Append("  ").Append(e.Time.ToString("HH:mm:ss")).Append("  ");
            var label = ChannelLabel(e.ChannelTag);
            if (!string.IsNullOrEmpty(label))
            {
                sb.Append("[").Append(label).Append("]  ");
            }
            sb.Append(e.SenderName).Append(": ").Append(e.Text);
            sb.AppendLine();
        }

        // 末尾再给一个分频道小计，便于快速扫读。
        try
        {
            sb.AppendLine();
            sb.AppendLine("—— 分频道条数小计 ——");
            foreach (var grp in log.GroupBy(x => x.ChannelTag).OrderBy(g => g.Key))
                sb.AppendLine("  [" + ChannelLabel(grp.Key) + "]: " + grp.Count() + " 条");
        }
        catch { }
    }

    // ====================== 完整版报告 ======================
    private static string BuildFullReport()
    {
        var sb = new StringBuilder(8192);
        sb.AppendLine("========================================");
        sb.AppendLine("  沉浸-最终梦想 · 对局聊天记录【完整版 / 本机所见】");
        sb.AppendLine("========================================");
        sb.AppendLine("导出时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("会议次数: " + currentRound);
        sb.AppendLine("消息条数: " + fullLog.Count);
        sb.AppendLine();
        sb.AppendLine("【说明】本文件记录「本机聊天框里实际显示出来的所有消息」——含原版公共/灵魂、");
        sb.AppendLine("  本插件私聊频道，以及 Plan17 阵营/情侣等其它插件频道里 *你这个视角看得到* 的内容，");
        sb.AppendLine("  另含投票/死亡通知等系统提示。因此它的完整度取决于复盘者本人的视角（普通玩家只会");
        sb.AppendLine("  看到自己阵营的私密聊天；以观战/全可见身份复盘才可能看到全部）。要标签准确、我方");
        sb.AppendLine("  频道必定完整的版本，请看同目录的干净版 " + FileName + "。");
        sb.AppendLine();

        AppendRoster(sb);
        AppendFullChatLog(sb);

        sb.AppendLine();
        sb.AppendLine("———— 记录结束 ————");
        return sb.ToString();
    }

    private static void AppendFullChatLog(StringBuilder sb)
    {
        sb.AppendLine("========================================");
        sb.AppendLine("  完整消息（按时间顺序 / 本机所见）");
        sb.AppendLine("========================================");
        if (fullLog.Count == 0)
        {
            sb.AppendLine("  （本局没有捕获到任何显示消息）");
            return;
        }
        int lastRound = int.MinValue;
        foreach (var e in fullLog)
        {
            if (e.Round != lastRound)
            {
                lastRound = e.Round;
                sb.AppendLine();
                sb.AppendLine(e.Round == 0 ? "【开局 / 首次会议前】" : ("【第 " + e.Round + " 次会议】"));
            }
            sb.Append("  ").Append(e.Time.ToString("HH:mm:ss")).Append("  ");
            sb.Append(e.Name).Append(": ").Append(e.Text);
            sb.AppendLine();
        }
    }
}
