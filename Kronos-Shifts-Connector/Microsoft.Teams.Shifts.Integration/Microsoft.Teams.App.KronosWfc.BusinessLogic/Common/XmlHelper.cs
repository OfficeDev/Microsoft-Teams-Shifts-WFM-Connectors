// <copyright file="XmlHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;

    /// <summary>
    /// Helper methods to helpal with XML requests/responses.
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Process response for an xml string.
        /// </summary>
        /// <param name="response">Response received.</param>
        /// <param name="telemetryClient">The telemetry client for logging.</param>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>Response object.</returns>
        public static T ProcessResponse<T>(this Tuple<string, string> response, TelemetryClient telemetryClient)
            where T : new()
        {
            if (response == null)
            {
                telemetryClient.TrackTrace($"Response of type {new T().GetType().FullName} was unable to be retrieved.");
                return default;
            }

            XDocument xDoc = XDocument.Parse(response.Item1);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(Response, StringComparison.Ordinal));
            return xResponse.ToString().DeserializeObject<T>();
        }

        /// <summary>
        /// Create Kronos comments ensuring we preserve previous notes.
        /// </summary>
        /// <param name="noteMessage">The note to add.</param>
        /// <param name="noteCommentText">The comment text value of the note to add.</param>
        /// <param name="timeStamp">The time stamp in local time to assign to the comments.</param>
        /// <param name="existingNotes">Existing notes.</param>
        /// <returns>Kronos Comments object.</returns>
        public static Comments GenerateKronosComments(string noteMessage, string noteCommentText, string timeStamp, List<Comment> existingNotes = null)
        {
            var comments = new Comments
            {
                Comment = new List<Comment>(),
            };

            if (existingNotes != null)
            {
                comments.Comment.AddRange(existingNotes);
            }

            if (!string.IsNullOrEmpty(noteMessage))
            {
                comments.Comment.Add(new Comment
                {
                    CommentText = noteCommentText,
                    Notes = new Notes
                    {
                        Note = new List<Note>
                        {
                            new Note
                            {
                                Text = noteMessage.Trim(),
                                Timestamp = timeStamp,
                            },
                        },
                    },
                });
            }

            return comments;
        }

        /// <summary>
        /// Create Kronos comments in the event of a shift edit.
        /// </summary>
        /// <param name="noteMessage">The note to .</param>
        /// <param name="noteCommentText">The comment text value of the note to add.</param>
        /// <param name="timeStamp">The timestamp in local time to assign to the comments.</param>
        /// <returns>Kronos Comments object.</returns>
        public static Comments GenerateEditedShiftKronosComments(string noteMessage, string noteCommentText, string timeStamp)
        {
            var comments = new Comments
            {
                Comment = new List<Comment>(),
            };

            if (string.IsNullOrEmpty(noteMessage))
            {
                return comments;
            }

            // We join multiple notes from kronos into a single string in Teams, therefore
            // split the string in to the individual parts.
            var notes = noteMessage.Split('*');
            var noteList = new List<Note>();

            foreach (var note in notes)
            {
                var noteToAdd = new Note
                {
                    Text = note.Trim(),
                    Timestamp = timeStamp,
                };

                noteList.Add(noteToAdd);
            }

            comments.Comment.Add(new Comment
            {
                CommentText = noteCommentText,
                Notes = new Notes
                {
                    Note = noteList,
                },
            });

            return comments;
        }
    }
}