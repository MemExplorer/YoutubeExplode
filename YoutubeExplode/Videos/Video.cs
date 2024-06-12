using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Bridge;
using YoutubeExplode.Common;
using YoutubeExplode.Utils;

namespace YoutubeExplode.Videos;

/// <summary>
/// Metadata associated with a YouTube video.
/// </summary>
public class Video(
    VideoId id,
    string title,
    string? commentContinuationToken,
    Author author,
    DateTimeOffset uploadDate,
    string description,
    TimeSpan? duration,
    IReadOnlyList<Thumbnail> thumbnails,
    IReadOnlyList<string> keywords,
    Engagement engagement
) : IVideo
{
    /// <inheritdoc />
    public VideoId Id { get; } = id;

    /// <inheritdoc />
    public string Url => $"https://www.youtube.com/watch?v={Id}";

    /// <summary>
    /// For fetching comments
    /// </summary>
    public Stack<string> ContinuationTokenStack { get; set; } =
        new Stack<string>(commentContinuationToken != null ? [commentContinuationToken!] : []);

    /// <inheritdoc />
    public string Title { get; } = title;

    /// <inheritdoc />
    public Author Author { get; } = author;

    /// <summary>
    /// Video upload date.
    /// </summary>
    public DateTimeOffset UploadDate { get; } = uploadDate;

    /// <summary>
    /// Video description.
    /// </summary>
    public string Description { get; } = description;

    /// <inheritdoc />
    public TimeSpan? Duration { get; } = duration;

    /// <inheritdoc />
    public IReadOnlyList<Thumbnail> Thumbnails { get; } = thumbnails;

    /// <summary>
    /// Available search keywords for the video.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; } = keywords;

    /// <summary>
    /// Engagement statistics for the video.
    /// </summary>
    public Engagement Engagement { get; } = engagement;

    private async ValueTask<VideoComments> GetCommentsNextResponseAsync(
        HttpClient http,
        string? continuationToken,
        string? visitorData = null,
        CancellationToken cancellationToken = default
    )
    {
        for (var retriesRemaining = 5; ; retriesRemaining--)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://www.youtube.com/youtubei/v1/next"
            );

            request.Content = new StringContent(
                // lang=json
                $$"""
                {
                    "context": {
                    "client": {
                        "clientName": "WEB",
                        "clientVersion": "2.20210408.08.00",
                        "hl": "en",
                        "gl": "US",
                        "utcOffsetMinutes": 0,
                        "visitorData": {{Json.Serialize(visitorData)}}
                    }
                    },
                    "continuation": {{Json.Serialize(continuationToken)}}
                }
                """
            );

            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var commentsResponse = VideoComments.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

            if (commentsResponse.Comments == null)
            {
                // Retry if this is not the first request, meaning that the previous requests were successful,
                // and that the playlist is probably not actually unavailable.
                if (!string.IsNullOrWhiteSpace(visitorData) && retriesRemaining > 0)
                    continue;

                throw new Exception($"Comments are not available.");
            }

            return commentsResponse;
        }
    }

    /// <summary>
    /// Fetch 20 comments at a time from a video
    /// </summary>
    /// <returns></returns>
    public async ValueTask<List<CommentsNextResponse>> FetchCommentsAsync(HttpClient httpClient)
    {
        var commentsNextResponse = await GetCommentsNextResponseAsync(
            httpClient,
            ContinuationTokenStack.Peek()
        );
        ContinuationTokenStack.Push(commentsNextResponse.ContinuationToken!);
        return commentsNextResponse.Comments!.ToList();
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Video ({Title})";
}
