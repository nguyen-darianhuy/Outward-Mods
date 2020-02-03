﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
//using SinAPI;
using Photon;

namespace CombatAndDodgeOverhaul
{
    public class RPCManager : Photon.MonoBehaviour
    {
        public static RPCManager Instance;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Update()
        {
            if (this.photonView == null && PhotonNetwork.inRoom)
            {
                this.gameObject.AddComponent<PhotonView>();
                this.photonView.viewID = PhotonNetwork.AllocateViewID();
            }
        }

        public void RequestSettings()
        {
            //Debug.Log("sending settings request to master client. viewID: " + this.photonView.viewID);
            this.photonView.RPC("RequestSettingsRPC", PhotonNetwork.masterClient, new object[0]);
        }

        [PunRPC]
        private void RequestSettingsRPC()
        {
            StartCoroutine(DelayedSendSettingsRPC());
        }

        private IEnumerator DelayedSendSettingsRPC()
        {
            //Debug.Log("Received settings request, waiting for players to be done loading...");
            while (!NetworkLevelLoader.Instance.AllPlayerDoneLoading)
            {
                yield return new WaitForSeconds(0.2f);
            }

            if (!PhotonNetwork.isNonMasterClientInRoom && (EnemyManager.Instance.TimeOfLastSyncSend < 0 || Time.time - EnemyManager.Instance.TimeOfLastSyncSend > 10f))
            {
                EnemyManager.Instance.TimeOfLastSyncSend = Time.time;
                //Debug.Log("Sending settings to all clients. View id: " + this.photonView.viewID);
                this.photonView.RPC("SendSettingsRPC", PhotonTargets.All, new object[]
                {
                    OverhaulGlobal.settings.Enable_Enemy_Mods,
                    OverhaulGlobal.settings.All_Enemies_Allied,
                    OverhaulGlobal.settings.Enemy_Balancing,
                    OverhaulGlobal.settings.Enemy_Health,
                    OverhaulGlobal.settings.Enemy_Damages,
                    OverhaulGlobal.settings.Enemy_ImpactRes,
                    OverhaulGlobal.settings.Enemy_Resistances,
                    OverhaulGlobal.settings.Enemy_ImpactDmg
                });
            }
        }

        [PunRPC]
        private void SendSettingsRPC(bool modsEnabled, bool enemiesAllied, bool customStats, float healthModifier, float damageModifier, float impactRes, float damageRes, float impactDmg)
        {
            //Debug.Log("Received settings RPC.");
            if (PhotonNetwork.isNonMasterClientInRoom)
            {
                //Debug.Log("We are not host, setting to received infos");
                EnemyManager.Instance.SetSyncInfo(modsEnabled, enemiesAllied, customStats, healthModifier, damageModifier, impactRes, damageRes, impactDmg);
            }
        }
    }
}
