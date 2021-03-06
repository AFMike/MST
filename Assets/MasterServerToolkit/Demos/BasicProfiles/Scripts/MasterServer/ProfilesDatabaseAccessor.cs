﻿#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR

using LiteDB;
using MasterServerToolkit.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MasterServerToolkit.MasterServer.Examples.BasicProfile
{
    public class ProfilesDatabaseAccessor : IProfilesDatabaseAccessor
    {
        private readonly ILiteCollection<ProfileInfoData> profiles;
        private readonly ILiteDatabase database;

        public ProfilesDatabaseAccessor(LiteDatabase database)
        {
            this.database = database;

            profiles = this.database.GetCollection<ProfileInfoData>("profiles");
            profiles.EnsureIndex(a => a.Username, true);
        }

        /// <summary>
        /// Get profile info from database
        /// </summary>
        /// <param name="profile"></param>
        public async Task RestoreProfileAsync(ObservableServerProfile profile)
        {
            try
            {
                var data = await FindOrCreateData(profile);
                profile.FromBytes(data.Data);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        /// <summary>
        /// Update profile info in database
        /// </summary>
        /// <param name="profile"></param>
        public async Task UpdateProfileAsync(ObservableServerProfile profile)
        {
            var data = await FindOrCreateData(profile);
            data.Data = profile.ToBytes();

            await Task.Run(() =>
            {
                profiles.Update(data);
            });
        }

        /// <summary>
        /// Find profile data in database or create new data and insert them to database
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        private async Task<ProfileInfoData> FindOrCreateData(ObservableServerProfile profile)
        {
            string username = profile.Username;

            var data = await Task.Run(() => {
                return profiles.FindOne(a => a.Username == username);
            });

            if (data == null)
            {
                data = new ProfileInfoData()
                {
                    Username = profile.Username,
                    Data = profile.ToBytes()
                };

                await Task.Run(() => {
                    profiles.Insert(data);
                });
            }

            return data;
        }

        /// <summary>
        /// LiteDB profile data implementation
        /// </summary>
        private class ProfileInfoData
        {
            [BsonId]
            public string Username { get; set; }
            public byte[] Data { get; set; }
        }
    }
}

#endif