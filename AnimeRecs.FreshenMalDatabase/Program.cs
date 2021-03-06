﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnimeRecs.DAL;
using MalApi;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace AnimeRecs.FreshenMalDatabase
{
    enum ExitCode
    {
        Success = 0,
        Failure = 1
    }

    class Program
    {
        static Config config;

        static int Main(string[] args)
        {
            CommandLineArgs commandLine;

            try
            {
                commandLine = new CommandLineArgs(args);
                if (commandLine.ShowHelp)
                {
                    commandLine.DisplayHelp(Console.Out);
                    return (int)ExitCode.Success;
                }

                IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                    .AddXmlFile(commandLine.ConfigFile);

                IConfigurationRoot rawConfig = configBuilder.Build();
                config = rawConfig.Get<Config>();

                if (config.LoggingConfigPath != null)
                {
                    Logging.SetUpLogging(config.LoggingConfigPath);
                }
                else
                {
                    Console.Error.WriteLine("No logging configuration file set. Logging to console.");
                    Logging.SetUpConsoleLogging();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal error: {0}", ex, ex.Message);
                return (int)ExitCode.Failure;
            }

            try
            {
                Logging.Log.Debug($"Command line args parsed. ConfigFile={commandLine.ConfigFile}");

                using (IMyAnimeListApi basicApi = new MyAnimeListApi() { TimeoutInMs = config.MalTimeoutInMs, UserAgent = config.MalApiUserAgentString })
                using (IMyAnimeListApi rateLimitingApi = new RateLimitingMyAnimeListApi(basicApi, TimeSpan.FromMilliseconds(config.DelayBetweenRequestsInMs)))
                using (IMyAnimeListApi malApi = new RetryOnFailureMyAnimeListApi(rateLimitingApi, config.NumMalRequestFailuresBeforeGivingUp, config.DelayAfterMalRequestFailureInMs))
                using (NpgsqlConnection conn = new NpgsqlConnection(config.ConnectionStrings.AnimeRecs))
                {
                    conn.Open();
                    int usersAddedSoFar = 0;
                    while (usersAddedSoFar < config.UsersPerRun)
                    {
                        RecentUsersResults recentMalUsers = malApi.GetRecentOnlineUsers();

                        foreach (string user in recentMalUsers.RecentUsers)
                        {
                            using (var transaction = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                            {
                                if (!UserIsInDatabase(user, conn, transaction))
                                {
                                    MalUserLookupResults userLookup = malApi.GetAnimeListForUser(user);
                                    if (UserMeetsCriteria(userLookup, conn, transaction))
                                    {
                                        InsertUserAndRatingsInDatabase(userLookup, conn, transaction);
                                        usersAddedSoFar++;
                                        Logging.Log.Debug("Committing transaction.");
                                        transaction.Commit();
                                        Logging.Log.Debug("Transaction committed.");

                                        if (usersAddedSoFar == config.UsersPerRun)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        Logging.Log.InfoFormat("{0} does not meet criteria for inclusion, skipping", user);
                                    }
                                }
                                else
                                {
                                    Logging.Log.InfoFormat("{0} is already in the database, skipping.", user);
                                }
                            }
                        }
                    }

                    using (var transaction = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                    {
                        TrimDatabaseToMaxUsers(config.MaxUsersInDatabase, conn, transaction);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log.FatalFormat("Fatal error: {0}", ex, ex.Message);
                return (int)ExitCode.Failure;
            }

            return (int)ExitCode.Success;
        }

        /// <summary>
        /// Not a definitive check. If it returns false, you should still check if the id is in the DB in case the user
        /// changed their username.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        static bool UserIsInDatabase(string username, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            Logging.Log.DebugFormat("Checking if {0} is in the database.", username);
            bool isInDb = mal_user.UserIsInDbCaseSensitive(username, conn, transaction);
            Logging.Log.DebugFormat("{0} in database = {1}", username, isInDb);
            return isInDb;
        }

        static bool UserMeetsCriteria(MalUserLookupResults userLookup, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            // completed, rated >= X, and user is not in DB
            int completedRated = userLookup.AnimeList.Count(anime => anime.Score.HasValue && anime.Status == CompletionStatus.Completed);
            if (completedRated < config.MinimumAnimesCompletedAndRated)
            {
                return false;
            }

            Logging.Log.DebugFormat("Really checking if {0} is in the database by user id.", userLookup.CanonicalUserName);
            bool isInDb = mal_user.UserIsInDb(userLookup.UserId, conn, transaction);
            Logging.Log.DebugFormat("{0} really in database = {1}", userLookup.CanonicalUserName, isInDb);
            return !isInDb;
        }

        // Only insert/update an anime once per run to save on trips to the DB
        static Dictionary<int, mal_anime> AnimesUpserted = new Dictionary<int, mal_anime>();

        static void InsertUserAndRatingsInDatabase(MalUserLookupResults userLookup, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            Logging.Log.InfoFormat("Inserting anime and list entries for {0} ({1} entries).", userLookup.CanonicalUserName, userLookup.AnimeList.Count);

            List<mal_anime> animesToUpsert = new List<mal_anime>();
            Dictionary<int, List<mal_anime_synonym>> synonymsToUpsert = new Dictionary<int, List<mal_anime_synonym>>();
            List<mal_list_entry> entriesToInsert = new List<mal_list_entry>();
            List<mal_list_entry_tag> tagsToInsert = new List<mal_list_entry_tag>();

            // Buffer animes, anime synonyms, list entries, and tags.
            // For animes not upserted this session, upsert animes all at once, clear synonyms, insert synonyms
            // insert user
            // insert list entries all at once
            // insert tags all at once

            foreach (MyAnimeListEntry anime in userLookup.AnimeList)
            {
                if (!AnimesUpserted.ContainsKey(anime.AnimeInfo.AnimeId))
                {
                    mal_anime animeRow = new mal_anime(
                        _mal_anime_id: anime.AnimeInfo.AnimeId,
                        _title: anime.AnimeInfo.Title,
                        _mal_anime_type_id: (int)anime.AnimeInfo.Type,
                        _num_episodes: anime.AnimeInfo.NumEpisodes,
                        _mal_anime_status_id: (int)anime.AnimeInfo.Status,
                        _start_year: (short?)anime.AnimeInfo.StartDate.Year,
                        _start_month: (short?)anime.AnimeInfo.StartDate.Month,
                        _start_day: (short?)anime.AnimeInfo.StartDate.Day,
                        _end_year: (short?)anime.AnimeInfo.EndDate.Year,
                        _end_month: (short?)anime.AnimeInfo.EndDate.Month,
                        _end_day: (short?)anime.AnimeInfo.EndDate.Day,
                        _image_url: anime.AnimeInfo.ImageUrl,
                        _last_updated: DateTime.UtcNow
                    );

                    animesToUpsert.Add(animeRow);

                    List<mal_anime_synonym> synonymRowsForThisAnime = new List<mal_anime_synonym>();
                    foreach (string synonym in anime.AnimeInfo.Synonyms)
                    {
                        mal_anime_synonym synonymRow = new mal_anime_synonym(
                            _mal_anime_id: anime.AnimeInfo.AnimeId,
                            _synonym: synonym
                        );
                        synonymRowsForThisAnime.Add(synonymRow);
                    }

                    synonymsToUpsert[anime.AnimeInfo.AnimeId] = synonymRowsForThisAnime;
                }

                mal_list_entry dbListEntry = new mal_list_entry(
                    _mal_user_id: userLookup.UserId,
                    _mal_anime_id: anime.AnimeInfo.AnimeId,
                    _rating: (short?)anime.Score,
                    _mal_list_entry_status_id: (short)anime.Status,
                    _num_episodes_watched: (short)anime.NumEpisodesWatched,
                    _started_watching_year: (short?)anime.MyStartDate.Year,
                    _started_watching_month: (short?)anime.MyStartDate.Month,
                    _started_watching_day: (short?)anime.MyStartDate.Day,
                    _finished_watching_year: (short?)anime.MyFinishDate.Year,
                    _finished_watching_month: (short?)anime.MyFinishDate.Month,
                    _finished_watching_day: (short?)anime.MyFinishDate.Day,
                    _last_mal_update: anime.MyLastUpdate
                );

                entriesToInsert.Add(dbListEntry);

                foreach (string tag in anime.Tags)
                {
                    mal_list_entry_tag dbTag = new mal_list_entry_tag(
                        _mal_user_id: userLookup.UserId,
                        _mal_anime_id: anime.AnimeInfo.AnimeId,
                        _tag: tag
                    );
                    tagsToInsert.Add(dbTag);
                }
            }

            // For animes not upserted this session, upsert animes, clear synonyms all at once, insert synonyms all at once
            Logging.Log.DebugFormat("Upserting {0} animes.", animesToUpsert.Count);
            foreach (mal_anime animeToUpsert in animesToUpsert)
            {
                Logging.Log.TraceFormat("Checking if anime \"{0}\" is in the database.", animeToUpsert.title);
                bool animeIsInDb = mal_anime.IsInDatabase(animeToUpsert.mal_anime_id, conn, transaction);
                if (!animeIsInDb)
                {
                    // Not worth optimizing this by batching inserts because once there are a couple hundred users in the database,
                    // inserts will be relatively few in number.
                    Logging.Log.Trace("Not in database. Inserting it.");
                    animeToUpsert.Insert(conn, transaction);
                    Logging.Log.TraceFormat("Inserted anime \"{0}\" in database.", animeToUpsert.title);
                    AnimesUpserted[animeToUpsert.mal_anime_id] = animeToUpsert;
                }
                else
                {
                    Logging.Log.TraceFormat("Already in database. Updating it.");
                    animeToUpsert.Update(conn, transaction);
                    Logging.Log.TraceFormat("Updated anime \"{0}\".", animeToUpsert.title);
                    AnimesUpserted[animeToUpsert.mal_anime_id] = animeToUpsert;
                }
            }
            Logging.Log.DebugFormat("Upserted {0} animes.", animesToUpsert.Count);

            if (synonymsToUpsert.Count > 0)
            {
                List<mal_anime_synonym> flattenedSynonyms = synonymsToUpsert.Values.SelectMany(synonyms => synonyms).ToList();

                // clear synonyms for all these animes
                Logging.Log.DebugFormat("Clearing {0} synonyms for this batch.", flattenedSynonyms.Count);
                mal_anime_synonym.Delete(synonymsToUpsert.Keys, conn, transaction);
                Logging.Log.DebugFormat("Cleared {0} synonyms for this batch.", flattenedSynonyms.Count);

                // insert synonyms for all these animes
                Logging.Log.DebugFormat("Inserting {0} synonyms for this batch.", flattenedSynonyms.Count);
                mal_anime_synonym.Insert(flattenedSynonyms, conn, transaction);
                Logging.Log.DebugFormat("Inserted {0} synonyms for this batch.", flattenedSynonyms.Count);
            }
            else
            {
                Logging.Log.Debug("No synonyms in this batch.");
            }

            // Insert user
            mal_user user = new mal_user(
                _mal_user_id: userLookup.UserId,
                _mal_name: userLookup.CanonicalUserName,
                _time_added: DateTime.UtcNow
            );

            Logging.Log.DebugFormat("Inserting {0} into DB.", userLookup.CanonicalUserName);
            user.Insert(conn, transaction);
            Logging.Log.DebugFormat("Inserted {0} into DB.", userLookup.CanonicalUserName);

            // insert list entries all at once
            if (entriesToInsert.Count > 0)
            {
                Logging.Log.DebugFormat("Inserting {0} list entries for user \"{1}\".", entriesToInsert.Count, userLookup.CanonicalUserName);
                mal_list_entry.Insert(entriesToInsert, conn, transaction);
                Logging.Log.DebugFormat("Inserted {0} list entries for user \"{1}\".", entriesToInsert.Count, userLookup.CanonicalUserName);
            }

            // insert tags all at once
            if (tagsToInsert.Count > 0)
            {
                Logging.Log.DebugFormat("Inserting {0} tags by user \"{1}\".", tagsToInsert.Count, userLookup.CanonicalUserName);
                mal_list_entry_tag.Insert(tagsToInsert, conn, transaction);
                Logging.Log.DebugFormat("Inserted {0} tags by user \"{1}\".", tagsToInsert.Count, userLookup.CanonicalUserName);
            }

            Logging.Log.InfoFormat("Done inserting anime and list entries for {0}.", userLookup.CanonicalUserName);
        }

        static void TrimDatabaseToMaxUsers(long maxUsersInDatabase, NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            Logging.Log.InfoFormat("Trimming database to {0} users.", maxUsersInDatabase);
            long numUsers = mal_user.Count(conn, transaction);
            Logging.Log.DebugFormat("{0} users are in the database.", numUsers);

            if (numUsers > maxUsersInDatabase)
            {
                long numUsersToDelete = numUsers - maxUsersInDatabase;

                Logging.Log.DebugFormat("Deleting {0} users.", numUsersToDelete);
                mal_user.DeleteOldestUsers(numUsersToDelete, conn, transaction);
                Logging.Log.InfoFormat("Deleted {0} users.", numUsersToDelete);
            }
            else
            {
                Logging.Log.Info("Don't need to delete any users.");
            }
        }
    }
}
