using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Lazy;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

/// <summary>
/// Raw Json Response
/// </summary>
/// <param name="commentMetadata"></param>
/// <param name="commentEntity"></param>
/// <param name="toolBarEntity"></param>
public partial class CommentsNextResponse(
    JsonElement? commentMetadata,
    JsonElement? commentEntity,
    JsonElement? toolBarEntity
)
{
    [Lazy]
    private JsonElement? CommentEntityPayload =>
        commentEntity?.GetPropertyOrNull("payload")?.GetPropertyOrNull("commentEntityPayload");

    /// <summary>
    /// Comment content
    /// </summary>
    [Lazy]
    public string? Content =>
        CommentEntityPayload
            ?.GetPropertyOrNull("properties")
            ?.GetPropertyOrNull("content")
            ?.GetPropertyOrNull("content")
            ?.GetStringOrNull();

    /// <summary>
    /// Author of the comment.
    /// </summary>
    [Lazy]
    public string? Author =>
        CommentEntityPayload
            ?.GetPropertyOrNull("author")
            ?.GetPropertyOrNull("displayName")
            ?.GetStringOrNull();

    /// <summary>
    /// User Channel Id
    /// </summary>
    [Lazy]
    public string? ChannelId =>
        CommentEntityPayload
            ?.GetPropertyOrNull("author")
            ?.GetPropertyOrNull("channelId")
            ?.GetStringOrNull();

    /// <summary>
    /// Is comment author the video upload
    /// </summary>
    [Lazy]
    public bool? IsAuthorTheUploader =>
        CommentEntityPayload
            ?.GetPropertyOrNull("author")
            ?.GetPropertyOrNull("isCreator")
            ?.GetBoolean();

    /// <summary>
    /// Like count in string
    /// </summary>
    [Lazy]
    public string? Likes =>
        CommentEntityPayload
            ?.GetPropertyOrNull("toolbar")
            ?.GetPropertyOrNull("likeCountLiked")
            ?.GetStringOrNull();

    /// <summary>
    /// Check whether the comment is favorite by the uploader
    /// </summary>
    [Lazy]
    public bool IsFavorite =>
        toolBarEntity
            ?.GetPropertyOrNull("payload")
            ?.GetPropertyOrNull("engagementToolbarStateEntityPayload")
            ?.GetPropertyOrNull("heartState")
            ?.GetStringOrNull() == "TOOLBAR_HEART_STATE_HEARTED";

    /// <summary>
    /// Comment published time
    /// </summary>
    [Lazy]
    public string? PublishedTime =>
        CommentEntityPayload
            ?.GetPropertyOrNull("properties")
            ?.GetPropertyOrNull("publishedTime")
            ?.GetStringOrNull();

    /// <summary>
    /// Gets reply count of the comment
    /// </summary>
    [Lazy]
    public int? ReplyCount =>
        CommentEntityPayload
            ?.GetPropertyOrNull("toolbar")
            ?.GetPropertyOrNull("replyCount")
            ?.GetStringOrNull()
            ?.ParseIntOrNull();

    /// <summary>
    /// Check whether the comment is pinned or not
    /// </summary>
    [Lazy]
    public bool IsPinned => commentMetadata?.GetPropertyOrNull("pinnedText") != null;

    /// <summary>
    /// Check whether the comment is Highlited by the uploader
    /// </summary>
    [Lazy]
    public bool IsHighlighted =>
        commentMetadata?.GetPropertyOrNull("linkedCommentText")?.GetStringOrNull()
        == "Highlighted comment";

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var tProperties = typeof(CommentsNextResponse).GetProperties();
        var sb = new StringBuilder();
        foreach (var p in tProperties)
        {
            var pValue = p.GetValue(this, null);
            var pName = p.Name;
            sb.Append(pName);
            sb.Append(": ");
            sb.Append((pValue ?? "").ToString());
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public partial class CommentsNextResponse
{
    /// <summary>
    /// Parse Comments Data
    /// </summary>
    /// <param name="content"></param>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static List<CommentsNextResponse> Parse(
        IEnumerable<JsonElement?> content,
        Dictionary<string, JsonElement> entities
    )
    {
        var retList = new List<CommentsNextResponse>();
        foreach (var c in content)
        {
            if (c == null)
                continue;

            var commentEntityKey = c?.GetPropertyOrNull("commentKey")?.GetStringOrNull();
            var toolBarEntityKey = c?.GetPropertyOrNull("toolbarStateKey")?.GetStringOrNull();
            if (commentEntityKey == null || toolBarEntityKey == null)
                continue;
            if (!entities.ContainsKey(commentEntityKey) || !entities.ContainsKey(toolBarEntityKey))
                continue;

            var commentEntity = entities[commentEntityKey];
            var toolBarEntity = entities[toolBarEntityKey];
            var commentResponse = new CommentsNextResponse(c, commentEntity, toolBarEntity);
            retList.Add(commentResponse);
        }

        return retList;
    }
}
