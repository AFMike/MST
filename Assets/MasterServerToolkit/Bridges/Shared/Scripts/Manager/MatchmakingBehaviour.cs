﻿using Aevien.UI;
using MasterServerToolkit.Logging;
using MasterServerToolkit.MasterServer;
using MasterServerToolkit.Networking;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MasterServerToolkit.Games
{
    public class MatchmakingBehaviour : BaseClientBehaviour
    {
        #region INSPECTOR

        /// <summary>
        /// Name of the room that will be loaded after a match is successfully created
        /// </summary>
        [SerializeField, Tooltip("Name of the room that will be loaded after a match is successfully created")]
        protected string startRoomScene = "Room";

        /// <summary>
        /// Time to wait before match creation process will be aborted
        /// </summary>
        [SerializeField, Tooltip("Time to wait before match creation process will be aborted")]
        protected uint matchCreationTimeout = 60;

        public UnityEvent OnRoomStartedEvent;
        public UnityEvent OnRoomStartAbortedEvent;

        #endregion

        private static MatchmakingBehaviour _instance;

        public static MatchmakingBehaviour Instance
        {
            get
            {
                if (!_instance) Logs.Error("Instance of MatchmakingBehaviour is not found");
                return _instance;
            }
        }

        protected override void Awake()
        {
            if (_instance)
            {
                Destroy(_instance.gameObject);
                return;
            }

            _instance = this;

            base.Awake();
        }

        protected override void OnInitialize()
        {
            // Set cliet mode
            Mst.Client.Rooms.ForceClientMode = true;

            // Set MSF global options
            Mst.Options.Set(MstDictKeys.autoStartRoomClient, true);
            Mst.Options.Set(MstDictKeys.roomOfflineSceneName, SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Sends request to master server to start new room process
        /// </summary>
        /// <param name="spawnOptions"></param>
        public virtual void CreateNewRoom(string regionName, MstProperties spawnOptions)
        {
            Mst.Events.Invoke(MstEventKeys.showLoadingInfo, "Starting room... Please wait!");

            logger.Debug("Starting room... Please wait!");

            // Custom options that will be given to room directly
            var customSpawnOptions = new MstProperties();
            customSpawnOptions.Add(Mst.Args.Names.StartClientConnection);

            Mst.Client.Spawners.RequestSpawn(spawnOptions, customSpawnOptions, regionName, (controller, error) =>
            {
                if (controller == null)
                {
                    Mst.Events.Invoke(MstEventKeys.hideLoadingInfo);
                    Mst.Events.Invoke(MstEventKeys.showOkDialogBox, new OkDialogBoxEventMessage(error, null));
                    return;
                }

                Mst.Events.Invoke(MstEventKeys.showLoadingInfo, "Room started. Finalizing... Please wait!");

                // Wait for spawning status until it is finished
                MstTimer.WaitWhile(() =>
                {
                    return controller.Status != SpawnStatus.Finalized;
                }, (isSuccess) =>
                {
                    Mst.Events.Invoke(MstEventKeys.hideLoadingInfo);

                    if (!isSuccess)
                    {
                        Mst.Client.Spawners.AbortSpawn(controller.SpawnTaskId);

                        logger.Error("Failed spawn new room. Time is up!");
                        Mst.Events.Invoke(MstEventKeys.showOkDialogBox, new OkDialogBoxEventMessage("Failed spawn new room. Time is up!", null));

                        OnRoomStartAbortedEvent?.Invoke();

                        return;
                    }

                    OnRoomStartedEvent?.Invoke();

                    logger.Info("You have successfully spawned new room");

                }, matchCreationTimeout);
            });
        }

        /// <summary>
        /// Sends request to master server to start new room process
        /// </summary>
        /// <param name="spawnOptions"></param>
        public virtual void CreateNewRoom(MstProperties spawnOptions)
        {
            CreateNewRoom(string.Empty, spawnOptions);
        }


        public virtual void StartMatch(GameInfoPacket gameInfo)
        {
            // Save room Id in buffer
            Mst.Options.Set(MstDictKeys.roomId, gameInfo.Id);
            // Save max players to buffer
            Mst.Options.Set(MstDictKeys.roomMaxPlayers, gameInfo.MaxPlayers);

            if (gameInfo.IsPasswordProtected)
            {
                Mst.Events.Invoke(MstEventKeys.showPasswordDialogBox,
                    new PasswordInputDialoxBoxEventMessage("Room is required the password. Please enter room password below",
                    () =>
                    {
                        StartLoadingGameScene();
                    }));
            }
            else
            {
                StartLoadingGameScene();
            }
        }

        /// <summary>
        /// Starte match scene
        /// </summary>
        protected virtual void StartLoadingGameScene()
        {
            ScenesLoader.LoadSceneByName(startRoomScene, (progressValue) =>
            {
                Mst.Events.Invoke(MstEventKeys.showLoadingInfo, $"Loading scene {Mathf.RoundToInt(progressValue * 100f)}% ... Please wait!");
            }, null);
        }
    }
}