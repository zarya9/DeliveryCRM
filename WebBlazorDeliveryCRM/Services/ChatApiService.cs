using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace WebBlazorDeliveryCRM.Services;

public class ChatApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ChatApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(int roomId, int skip = 0, int take = 100)
    {
        using var stream = await _http.GetStreamAsync($"/api/Chat/rooms/{roomId}/messages?skip={skip}&take={take}");
        var list = await JsonSerializer.DeserializeAsync<List<ChatMessageDto>>(stream, JsonOptions);
        return list ?? new List<ChatMessageDto>();
    }

    public async Task<List<ChatRoomListItemDto>> GetRoomsListAsync()
    {
        using var stream = await _http.GetStreamAsync("/api/Chat/rooms/list");
        var list = await JsonSerializer.DeserializeAsync<List<ChatRoomListItemDto>>(stream, JsonOptions);
        return list ?? new List<ChatRoomListItemDto>();
    }

    public async Task<(bool ok, int roomId, string? roomName)> EnsureCompanyRoomAsync()
    {
        var res = await _http.PostAsync("/api/Chat/rooms/company", null);
        if (!res.IsSuccessStatusCode) return (false, 0, null);
        using var stream = await res.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<EnsureRoomResponse>(stream, JsonOptions);
        return (payload != null, payload?.RoomId ?? 0, payload?.RoomName);
    }

    public async Task<(bool ok, int roomId, string? roomName)> CreateOrGetDirectRoomAsync(int peerUserId)
    {
        var res = await _http.PostAsync($"/api/Chat/rooms/direct?peerUserId={peerUserId}", null);
        if (!res.IsSuccessStatusCode) return (false, 0, null);
        using var stream = await res.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<EnsureRoomResponse>(stream, JsonOptions);
        return (payload != null, payload?.RoomId ?? 0, payload?.RoomName);
    }

    public async Task<bool> SendMessageAsync(int roomId, string text, string? attachmentUrl = null)
    {
        var res = await _http.PostAsJsonAsync($"/api/Chat/messages?chatRoomId={roomId}", new { messageText = text, attachmentUrl });
        return res.IsSuccessStatusCode;
    }

    public async Task<(bool ok, string? path, string? error)> UploadChatAttachmentAsync(IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(25 * 1024 * 1024);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _http.PostAsync("/api/Files/chat-attachment", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(body))
                    return (false, null, $"Не удалось загрузить файл: {body}");
                return (false, null, "Не удалось загрузить файл.");
            }

            var payload = await JsonSerializer.DeserializeAsync<UploadAttachmentResponse>(
                await response.Content.ReadAsStreamAsync(), JsonOptions);
            return (true, payload?.Path, null);
        }
        catch
        {
            return (false, null, "Ошибка загрузки файла.");
        }
    }

    public async Task<bool> EditMessageAsync(int messageId, string text)
    {
        var res = await _http.PutAsJsonAsync($"/api/Chat/messages/{messageId}", text);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        var res = await _http.DeleteAsync($"/api/Chat/messages/{messageId}");
        return res.IsSuccessStatusCode;
    }

    public async Task<List<QuickReplyTemplateDto>> GetQuickRepliesAsync(string? category = null, string? search = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(category)) q.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
        var qs = q.Count > 0 ? "?" + string.Join("&", q) : string.Empty;
        using var stream = await _http.GetStreamAsync($"/api/Chat/quick-replies{qs}");
        var list = await JsonSerializer.DeserializeAsync<List<QuickReplyTemplateDto>>(stream, JsonOptions);
        return list ?? new List<QuickReplyTemplateDto>();
    }

    public async Task<bool> UpsertQuickReplyAsync(QuickReplyUpsertRequest request)
    {
        var res = await _http.PostAsJsonAsync("/api/Chat/quick-replies", request);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteQuickReplyAsync(int templateId)
    {
        var res = await _http.DeleteAsync($"/api/Chat/quick-replies/{templateId}");
        return res.IsSuccessStatusCode;
    }
}

public class ChatMessageDto
{
    public int ID_ChatMessage { get; set; }
    public int ChatRoom_id { get; set; }
    public int Sender_id { get; set; }
    public string MessageText { get; set; } = "";
    public string? AttachmentUrl { get; set; }
    public DateTime Sent_at { get; set; }
    public DateTime? Edited_at { get; set; }
    public bool Is_deleted { get; set; }
}

public sealed class UploadAttachmentResponse
{
    public string? Path { get; set; }
    public string? FileName { get; set; }
}

public sealed class QuickReplyTemplateDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class QuickReplyUpsertRequest
{
    public int? TemplateId { get; set; }
    public string Category { get; set; } = "other";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ChatRoomListItemDto
{
    public int ChatRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomKind { get; set; } = "company";
    public int? PeerUserId { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public sealed class EnsureRoomResponse
{
    public int RoomId { get; set; }
    public string? RoomName { get; set; }
}

