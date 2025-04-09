using System.Collections.Concurrent;
using System.Net;

namespace ModBot.Server.Services.Twitch;

public class TwitchEmotesService
{
    private readonly HttpClient _http;
    private readonly ILogger<TwitchEmotesService> _logger;

    public TwitchEmotesService(ILogger<TwitchEmotesService> logger)
    {
        _logger = logger;
        _http = new();
    }

    public async Task<Dictionary<string, ThirdPartyEmote>> GetGlobal()
    {
        List<ThirdPartyEmote> list = [];

        BttvEmote[]? bttv = await GetGlobalBttv();
        if (bttv is not null)
            foreach (BttvEmote? emote in bttv)
                list.Add(new(EThirdPartyService.Bttv, emote.id, emote.code, $"https://cdn.betterttv.net/emote/{emote.id}/2x"));

        FfzEmoticon[]? ffz = await GetGlobalFfz();
        if (ffz is not null)
        {
            foreach (FfzEmoticon? emote in ffz)
            {
                string url;
                if (emote.animated is not null && emote.animated.TryGetValue("2", out string? value))
                    url = value;
                else if (emote.urls is not null && emote.urls.TryGetValue("2", out string? emoteUrl))
                    url = emoteUrl;
                else
                    continue;
                list.Add(new(EThirdPartyService.Ffz, emote.id.ToString(), emote.name, url));
            }
        }

        SevenTVEmote[]? sevenTv = await GetGlobal7Tv();
        if (sevenTv is not null)
        {
            foreach (SevenTVEmote? emote in sevenTv)
            {
                IOrderedEnumerable<SevenTVFile> webpFiltered = emote.data.host.files.Where(x => x is { format: "WEBP", height: > 32 }).OrderBy(x => x.height);
                if (!webpFiltered.Any())
                    continue;
                SevenTVFile webp = webpFiltered.First();
                list.Add(new(EThirdPartyService.SevenTV, emote.id, emote.name, "https:" + emote.data.host.url + "/" + webp.name));
            }
        }

        return list.DistinctBy(x => x.code).ToDictionary(x => x.code);
    }

    public async Task<Dictionary<string, ThirdPartyEmote>> GetChannel(string broadcasterId, string channelName)
    {
        List<ThirdPartyEmote> list = [];

        ChannelBttvEmotesResponse? bttv = await GetChannelBttv(broadcasterId);
        if (bttv is not null)
        {
            BttvEmote[] channelBttv = [];
            if (bttv.channelEmotes is not null && bttv.sharedEmotes is not null)
                channelBttv = bttv.channelEmotes.Concat(bttv.sharedEmotes).ToArray();
            else if (bttv.channelEmotes is not null)
                channelBttv = bttv.channelEmotes;
            else if (bttv.sharedEmotes is not null)
                channelBttv = bttv.sharedEmotes;
            foreach (BttvEmote? emote in channelBttv)
                list.Add(new(EThirdPartyService.Bttv, emote.id, emote.code, $"https://cdn.betterttv.net/emote/{emote.id}/2x"));
        }

        FfzEmoticon[]? ffz = await GetChannelFfz(channelName);
        if (ffz is not null)
        {
            foreach (FfzEmoticon? emote in ffz)
            {
                string url;
                if (emote.animated is not null && emote.animated.TryGetValue("2", out string? value))
                    url = value;
                else if (emote.urls is not null && emote.urls.TryGetValue("2", out string? emoteUrl))
                    url = emoteUrl;
                else
                    continue;
                list.Add(new(EThirdPartyService.Ffz, emote.id.ToString(), emote.name, url));
            }
        }

        SevenTVEmote[]? sevenTv = await GetChannel7Tv(broadcasterId);
        if (sevenTv is not null)
        {
            foreach (SevenTVEmote? emote in sevenTv)
            {
                IOrderedEnumerable<SevenTVFile> webpFiltered = emote.data.host.files.Where(x => x is { format: "WEBP", height: > 32 }).OrderBy(x => x.height);
                if (!webpFiltered.Any())
                    continue;
                SevenTVFile webp = webpFiltered.First();
                list.Add(new(EThirdPartyService.SevenTV, emote.id, emote.name, "https:" + emote.data.host.url + "/" + webp.name));
            }
        }

        return list.DistinctBy(x => x.code).ToDictionary(x => x.code);
    }

    private SevenTVEmote[]? _global7TvEmotes;
    private async Task<SevenTVEmote[]?> GetGlobal7Tv()
    {
        if (_global7TvEmotes is not null)
            return _global7TvEmotes;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://7tv.io/v3/emote-sets/global");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SevenTvGlobalResponse? obj = await response.Content.ReadFromJsonAsync<SevenTvGlobalResponse>();
                List<SevenTVEmote> list = [];
                foreach (SevenTVEmote? emote in obj?.emotes ?? [])
                    list.Add(emote);
                _global7TvEmotes = list.ToArray();
                return _global7TvEmotes;
            }
            else return null;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to fetch global 7tv emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    private readonly ConcurrentDictionary<string, SevenTVEmote[]> _getChannel7TvCache = new();
    private async Task<SevenTVEmote[]?> GetChannel7Tv(string broadcasterId)
    {
        if (_getChannel7TvCache.TryGetValue(broadcasterId, out SevenTVEmote[]? cached))
            return cached;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://7tv.io/v3/users/twitch/{broadcasterId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SevenTvChannelResponse? obj = await response.Content.ReadFromJsonAsync<SevenTvChannelResponse>();
                List<SevenTVEmote> list = [];
                foreach (SevenTVEmote? emote in obj?.emote_set.emotes ?? [])
                    list.Add(emote);
                SevenTVEmote[] result = list.ToArray();
                _getChannel7TvCache.AddOrUpdate(broadcasterId, result, (_, _) => result);
                return result;
            }
            else return null;
        }
        catch(Exception e)
        {
            _logger.LogError("Failed to fetch channel 7tv emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    private FfzEmoticon[]? _globalFfzEmotes;
    private async Task<FfzEmoticon[]?> GetGlobalFfz()
    {
        if (_globalFfzEmotes is not null)
            return _globalFfzEmotes;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://api.frankerfacez.com/v1/set/global");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                FfzGlobalResponse? obj = await response.Content.ReadFromJsonAsync<FfzGlobalResponse>();
                List<FfzEmoticon> list = [];
                foreach (int setId in obj?.default_sets ?? [])
                    if (obj?.sets.TryGetValue(setId.ToString(), out FfzSet? set) ?? false)
                        foreach (FfzEmoticon? emote in set.emoticons)
                            list.Add(emote);
                _globalFfzEmotes = list.ToArray();
                return _globalFfzEmotes;
            }
            else return null;
        }
        catch(Exception e)
        {
            _logger.LogError("Failed to fetch global ffz emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    private readonly ConcurrentDictionary<string, FfzEmoticon[]> _getChannelFfzCache = new();
    private async Task<FfzEmoticon[]?> GetChannelFfz(string channelName)
    {
        if (_getChannelFfzCache.TryGetValue(channelName, out FfzEmoticon[]? cached))
            return cached;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://api.frankerfacez.com/v1/room/{channelName}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                FfzChannelResponse? obj = await response.Content.ReadFromJsonAsync<FfzChannelResponse>();
                List<FfzEmoticon> list = [];
                foreach ((string _, FfzSet? set) in obj?.sets ?? [])
                    foreach (FfzEmoticon? emote in set.emoticons)
                        list.Add(emote);
                FfzEmoticon[] result = list.ToArray();
                _getChannelFfzCache.AddOrUpdate(channelName, result, (_, _) => result);
                return result;
            }
            else return null;
        }
        catch(Exception e)
        {
            _logger.LogError("Failed to fetch channel ffz emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    private BttvEmote[]? _globalBttvEmotes;
    private async Task<BttvEmote[]?> GetGlobalBttv()
    {
        if (_globalBttvEmotes is not null)
            return _globalBttvEmotes;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://api.betterttv.net/3/cached/emotes/global");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _globalBttvEmotes = await response.Content.ReadFromJsonAsync<BttvEmote[]>();
                return _globalBttvEmotes;
            }
            else return null;
        }
        catch(Exception e)
        {
            _logger.LogError("Failed to fetch global bttv emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    private readonly ConcurrentDictionary<string, ChannelBttvEmotesResponse> _getChannelBttvCache = new();
    private async Task<ChannelBttvEmotesResponse?> GetChannelBttv(string broadcasterId)
    {
        if (_getChannelBttvCache.TryGetValue(broadcasterId, out ChannelBttvEmotesResponse? cached))
            return cached;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync($"https://api.betterttv.net/3/cached/users/twitch/{broadcasterId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                ChannelBttvEmotesResponse? result = await response.Content.ReadFromJsonAsync<ChannelBttvEmotesResponse>();
                if (result is not null)
                    _getChannelBttvCache.AddOrUpdate(broadcasterId, result, (_, _) => result);
                return result;
            }
            else return null;
        }
        catch(Exception e)
        {
            _logger.LogError("Failed to fetch channel bttv emotes: {ErrorMessage}", e.Message + (e.InnerException is not null ? $" ({e.InnerException.Message})" : ""));
            return null;
        }
    }

    public void ResetChannelCache(string broadcasterId, string channelName)
    {
        _getChannel7TvCache.TryRemove(broadcasterId, out _);
        _getChannelFfzCache.TryRemove(channelName, out _);
        _getChannelBttvCache.TryRemove(broadcasterId, out _);
    }
}

public record ThirdPartyEmote(EThirdPartyService service, string id, string code, string url);

public record SevenTVFile(string name, string static_name, int width, int height, int frame_count, int size, string format);
public record SevenTVHost(string url, IReadOnlyList<SevenTVFile> files);
public record SevenTVData(string id, string name, bool animated, SevenTVHost host);
public record SevenTVEmote(string id, string name, SevenTVData data);
public record SevenTvGlobalResponse(IReadOnlyList<SevenTVEmote> emotes);
public record SevenTvSet(IReadOnlyList<SevenTVEmote> emotes);
public record SevenTvChannelResponse(SevenTvSet emote_set);

public record FfzEmoticon(int id, string name, Dictionary<string, string> urls, Dictionary<string, string> animated);
public record FfzSet(int id, int _type, object icon, string title, object css, IReadOnlyList<FfzEmoticon> emoticons);
public record FfzGlobalResponse(int[] default_sets, Dictionary<string, FfzSet> sets);
public record FfzChannelResponse(Dictionary<string, FfzSet> sets);

public record BttvEmote(string id, string code, string imageType);
public record ChannelBttvEmotesResponse(BttvEmote[] channelEmotes, BttvEmote[] sharedEmotes);

public enum EThirdPartyService
{
    Bttv, Ffz, SevenTV
}
