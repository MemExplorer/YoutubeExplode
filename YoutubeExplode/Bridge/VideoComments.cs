using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Lazy;
using YoutubeExplode.Utils;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

internal partial class VideoComments(JsonElement content)
{
    [Lazy]
    private Dictionary<string, JsonElement>? CommentsRoot =>
        content
            .GetPropertyOrNull("frameworkUpdates")
            ?.GetPropertyOrNull("entityBatchUpdate")
            ?.GetPropertyOrNull("mutations")
            ?.EnumerateArrayOrNull()
            ?.Where(x => x.GetPropertyOrNull("entityKey")?.GetStringOrNull() != null)
            ?.ToDictionary(x => x.GetPropertyOrNull("entityKey")!.Value.GetString()!, y => y);

    [Lazy]
    private IEnumerable<JsonElement>? CommentInfoRoot =>
        content
            .GetPropertyOrNull("onResponseReceivedEndpoints")
            ?.EnumerateArrayOrNull()
            ?.Select(x =>
                x.GetPropertyOrNull("reloadContinuationItemsCommand")
                    ?.GetPropertyOrNull("continuationItems")
                    ?.EnumerateArrayOrNull()
            )
            .SelectMany(x => x.GetValueOrDefault());

    [Lazy]
    private JsonElement? CommentSectionRoot =>
        CommentInfoRoot?.FirstOrNull(x => x.GetPropertyOrNull("commentsHeaderRenderer") != null);

    [Lazy]
    private IEnumerable<JsonElement>? CommentMetadataRoot =>
        CommentInfoRoot?.Where(x => x.GetPropertyOrNull("commentThreadRenderer") != null);

    [Lazy]
    private JsonElement? CommentsContinuationRoot =>
        CommentInfoRoot?.FirstOrNull(x => x.GetPropertyOrNull("continuationItemRenderer") != null);

    [Lazy]
    private IEnumerable<JsonElement?>? RawComments =>
        CommentMetadataRoot?.Select(x =>
            x.GetPropertyOrNull("commentThreadRenderer")
                ?.GetPropertyOrNull("commentViewModel")
                ?.GetPropertyOrNull("commentViewModel")
        );

    [Lazy]
    public int? CommentsCount =>
        CommentSectionRoot
            ?.GetPropertyOrNull("commentsHeaderRenderer")
            ?.GetPropertyOrNull("countText")
            ?.GetPropertyOrNull("runs")
            ?.EnumerateArrayOrNull()
            ?.FirstOrNull(x =>
            {
                var result = x.GetPropertyOrNull("text")?.GetStringOrNull()?.Contains("Comments");
                return result != null && !result.Value;
            })
            ?.GetPropertyOrNull("text")
            ?.GetStringOrNull()
            ?.ParseIntOrNull();

    /// <summary>
    /// User Comments
    /// </summary>
    [Lazy]
    public IEnumerable<CommentsNextResponse>? Comments =>
        CommentsNextResponse.Parse(RawComments!, CommentsRoot!);

    /// <summary>
    /// Continuation Token
    /// </summary>
    [Lazy]
    public string? ContinuationToken =>
        CommentsContinuationRoot
            ?.GetPropertyOrNull("continuationItemRenderer")
            ?.GetPropertyOrNull("continuationEndpoint")
            ?.GetPropertyOrNull("continuationCommand")
            ?.GetPropertyOrNull("token")
            ?.GetStringOrNull();
}

internal partial class VideoComments
{
    public static VideoComments Parse(string raw) => new(Json.Parse(raw));
}
