﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SideLoader;
using SideLoader.Helpers;
using UnityEngine;

namespace PvP
{
    public class PvPGUI : MonoBehaviour
    {
        public static PvPGUI Instance;

        private static readonly int WINDOW_ID = 6313531;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public bool ShowGUI
        {
            get => m_showGUI;
            set
            {
                m_showGUI = value;

                if (m_showGUI)
                    ForceUnlockCursor.AddUnlockSource();
                else
                    ForceUnlockCursor.RemoveUnlockSource();
            }
        }
        private bool m_showGUI;

        public int guiPage = 0;
        public bool lastMenuToggle;

        public bool ConfirmingBattleRoyale = false;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            ShowGUI = PvP.Instance.settings.Show_Menu_On_Startup;
        }

        internal void Update()
        {
            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }

            if (NetworkLevelLoader.Instance.IsGameplayPaused || Global.Lobby.PlayersInLobbyCount <= 0)
            {
                return;
            }
        }

        internal void OnGUI()
        {
            var origSkin = GUI.skin;
            GUI.skin = UIStyles.WindowSkin;

            Matrix4x4 orig = GUI.matrix;

            if (PvP.Instance.settings.Enable_Menu_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(50, 50, 500, 300);
            }

            if (!ConfirmingBattleRoyale && ShowGUI)
            {
                m_windowRect = GUI.Window(WINDOW_ID, m_windowRect, DrawWindow, "PvP " + PvP.VERSION);
            }
            else if (ConfirmingBattleRoyale || BattleRoyale.Instance.IsGameplayEnding)
            {
                float x = Screen.width / 2 - 200;
                float y;
                if (Global.Lobby.LocalPlayerCount > 1)
                {
                    y = Screen.height / 4 - 150;
                }
                else
                {
                    y = Screen.height / 2 - 150;
                }

                if (ConfirmingBattleRoyale)
                {
                    GUI.Window(WINDOW_ID, new Rect(x, y, 400, 250), BattleRoyaleConfirmStart, "Are you sure?");
                }
                else if (BattleRoyale.Instance.IsGameplayEnding)
                {
                    GUI.Window(WINDOW_ID, new Rect(x, y, 400, 150), BattleRoyaleGameEnd, "Play again?");
                }
            }

            if (PvP.Instance.CurrentGame != PvP.GameModes.NONE)
            {
                CurrentGameWindow();
            }

            GUI.matrix = orig;
            GUI.skin = origSkin;
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 50, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 50, 0, 45, 20), "X"))
            {
                ShowGUI = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Space(150);
            if (guiPage == 0) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Main", GUILayout.Width(100)))
            {
                guiPage = 0;
            }
            if (guiPage == 1) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Settings", GUILayout.Width(100)))
            {
                guiPage = 1;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUI.skin.box.alignment = TextAnchor.UpperLeft;

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(m_windowRect.height - 70));

            switch (guiPage)
            {
                case 0:
                    MainPage();
                    break;
                case 1:
                    SettingsPage();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void MainPage()
        {
            if (Global.Lobby.PlayersInLobbyCount > 0 && !NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                if (!PhotonNetwork.isNonMasterClientInRoom) // only host do these things
                {
                    if (PvP.Instance.CurrentGame == PvP.GameModes.NONE) // && !BattleRoyale.Instance.IsGameplayEnding)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Begin Deathmatch"))
                        {
                            PvP.Instance.StartGameplay((int)PvP.GameModes.Deathmatch, "A Deathmatch has begun!");
                        }
                        if (GUILayout.Button("Begin Battle Royale"))
                        {
                            ConfirmingBattleRoyale = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        if (GUILayout.Button("End Gameplay"))
                        {
                            PvP.Instance.StopGameplay("The host has ended the game.");
                        }
                    }

                    GUILayout.BeginHorizontal();
                    if (!PvP.Instance.FriendlyFireEnabled)
                    {
                        GUI.color = Color.red;
                        if (GUILayout.Button("Enable Friendly Fire"))
                        {
                            RPCManager.SendFriendyFire(true);
                        }
                    }
                    else
                    {
                        GUI.color = Color.green;
                        if (GUILayout.Button("Disable Friendly Fire"))
                        {
                            RPCManager.SendFriendyFire(false);
                        }
                        if (!PvP.Instance.FriendlyTargetingEnabled)
                        {
                            GUI.color = Color.red;
                            if (GUILayout.Button("Enable Friendly Targeting"))
                            {
                                RPCManager.SendFriendyTargeting(true);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Disable Friendly Targeting"))
                            {
                                RPCManager.SendFriendyTargeting(false);
                            }
                        }
                    }
                    if (!PvP.Instance.EnemiesDisabled)
                    {
                        GUI.color = Color.green;
                        if (GUILayout.Button("Disable Enemies"))
                        {
                            RPCManager.SendSetEnemiesActive(false);
                        }
                    }
                    else
                    {
                        GUI.color = Color.red;
                        if (GUILayout.Button("Enable Enemies"))
                        {
                            RPCManager.SendSetEnemiesActive(true);
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUI.color = Color.white;
                }
                // end host-only block

                GUILayout.Label("Characters: ");

                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
                {
                    if (!ps.ControlledCharacter.Initialized) { continue; }

                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUI.skin.label.wordWrap = false;
                    GUI.color = TeamColors[ps.ControlledCharacter.Faction];

                    string label = ps.ControlledCharacter.Name;
                    if (ps.ControlledCharacter.IsWorldHost) { label += " [Host]"; }
                    else if (ps.ControlledCharacter.IsLocalPlayer) { label += " [Local]"; }
                    else { label += " [Online]"; }
                    GUILayout.Label(label, GUILayout.Width(130));

                    GUILayout.Label("Team: ", GUILayout.Width(40));

                    if (!PhotonNetwork.isNonMasterClientInRoom || ps.ControlledCharacter.IsLocalPlayer)
                    {
                        if (ps.ControlledCharacter.Faction != Character.Factions.NONE && PvP.Instance.CurrentGame == PvP.GameModes.NONE)
                        {
                            if (GUILayout.Button("<", GUILayout.Width(30)))
                            {
                                var newFaction = (Character.Factions)((int)ps.ControlledCharacter.Faction - 1);
                                PlayerManager.Instance.ChangeFactions(ps.ControlledCharacter, newFaction);
                            }
                        }
                        else { GUILayout.Space(35); }
                    }

                    GUILayout.Label(ps.ControlledCharacter.Faction.ToString(), GUILayout.Width(80));

                    if (!PhotonNetwork.isNonMasterClientInRoom || ps.ControlledCharacter.IsLocalPlayer)
                    {
                        if (ps.ControlledCharacter.Faction != Character.Factions.Golden && PvP.Instance.CurrentGame == PvP.GameModes.NONE)
                        {
                            if (GUILayout.Button(">", GUILayout.Width(30)))
                            {
                                var newFaction = (Character.Factions)((int)ps.ControlledCharacter.Faction + 1);
                                PlayerManager.Instance.ChangeFactions(ps.ControlledCharacter, newFaction);
                            }
                        }
                        else { GUILayout.Space(35); }

                        if (ps.ControlledCharacter.IsDead && PvP.Instance.CurrentGame == PvP.GameModes.NONE)
                        {
                            if (GUILayout.Button("Resurrect", GUILayout.Width(75)))
                            {
                                RPCManager.Instance.SendResurrect(ps.ControlledCharacter);
                            }
                        }
                    }

                    GUI.skin.label.wordWrap = true;
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Load up a character to begin...");
            }
        }

        private void SettingsPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            PvP.Instance.settings.Show_Menu_On_Startup = GUILayout.Toggle(PvP.Instance.settings.Show_Menu_On_Startup, "Show Menu On Startup");
            PvP.Instance.settings.Enable_Menu_Scaling = GUILayout.Toggle(PvP.Instance.settings.Enable_Menu_Scaling, "Enable Menu Scaling");
            GUILayout.Space(15);            
            GUILayout.EndVertical();
        }

        private void BattleRoyaleConfirmStart(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 400, 20));

            GUILayout.BeginArea(new Rect(15, 25, 370, 350));

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            string message = "Are you sure you want to start a Battle Royale?";
            if (SceneManagerHelper.ActiveSceneName != "Monsoon") { message += "\r\n\r\nThis will teleport all players to Monsoon."; }
            GUILayout.Label(message, GUILayout.Width(370));

            GUILayout.Label("<b><color=red>WARNING:</color> This will WIPE the character save and you will need to manually restore your save " +
                "from a backup if you wish to restore it for this character.\r\n" +
                "\r\n" +
                "It is highly recommended to use a fresh character for Battle Royale so that you don't care if it gets wiped.</b>");

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("No, go back!"))
            {
                ConfirmingBattleRoyale = false;
            }

            GUILayout.Space(30);

            if (GUILayout.Button("Yes, I'm sure!"))
            {
                ConfirmingBattleRoyale = false;
                ShowGUI = false;

                if (BattleRoyale.Instance.CheckCanStart())
                {
                    BattleRoyale.Instance.StartBattleRoyale(false);
                }
                else
                {
                    RPCManager.Instance.SendUIMessageLocal(CharacterManager.Instance.GetFirstLocalCharacter(), "There are not enough teams to start!");
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void BattleRoyaleGameEnd(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 400, 20));

            GUILayout.BeginArea(new Rect(15, 65, 370, 350));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Again"))
            {
                BattleRoyale.Instance.StartBattleRoyale(true);
                BattleRoyale.Instance.IsGameplayEnding = false;
                ShowGUI = false;
            }

            GUILayout.Space(30);

            if (GUILayout.Button("End Lobby"))
            {
                BattleRoyale.Instance.IsGameplayEnding = false;
                ShowGUI = false;
                RPCManager.Instance.photonView.RPC("EndBattleRoyaleRPC", PhotonTargets.All, new object[0]);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CurrentGameWindow()
        {
            GUILayout.BeginArea(new Rect(15, 15, 240, Screen.height * 0.7f));

            if (PvP.Instance.CurrentGame == PvP.GameModes.BattleRoyale && BattleRoyale.Instance.IsGameplayStarting)
            {
                GUILayout.Label("A Battle Royale is starting...");
            }
            else
            {
                GUI.skin.label.fontSize *= 2;
                TimeSpan t = TimeSpan.FromSeconds(Time.time - PvP.Instance.GameStartTime);
                GUILayout.Label(t.Minutes.ToString("0") + ":" + t.Seconds.ToString("00"), GUILayout.Height(40));
                GUI.skin.label.fontSize /= 2;

                GUILayout.Label("Current Teams:");

                foreach (var entry in PvP.Instance.CurrentPlayers)
                {
                    if (entry.Key == Character.Factions.NONE) continue;

                    GUI.color = TeamColors[entry.Key];
                    GUILayout.Label(entry.Key.ToString() + ":");
                    GUI.color = Color.white;

                    foreach (PlayerSystem player in entry.Value)
                    {
                        if (player.ControlledCharacter.IsDead)
                        {
                            GUI.color = Color.black;
                            GUILayout.Label(" - " + player.ControlledCharacter.Name + " (DEAD)");
                        }
                        else
                        {
                            GUILayout.Label(
                            " - " +
                            player.ControlledCharacter.Name +
                            " (" +
                            Math.Round((decimal)player.ControlledCharacter.Stats.CurrentHealth) +
                            " / " +
                            player.ControlledCharacter.Stats.MaxHealth +
                            ")");
                        }
                    }
                }
            }

            GUILayout.EndArea();
        }

        public Dictionary<Character.Factions, Color> TeamColors = new Dictionary<Character.Factions, Color>()
        {
            { (Character.Factions)0, Color.white },
            { (Character.Factions)1, Color.green },
            { (Character.Factions)2, Color.red * 2.0f },
            { (Character.Factions)3, Color.cyan * Color.grey },
            { (Character.Factions)4, Color.magenta },
            { (Character.Factions)5, new Color(0.8f, 0.3f, 0.3f) },
            { (Character.Factions)6, new Color(0.3f, 0.3f, 1.0f) },
            { (Character.Factions)7, Color.gray },
            { (Character.Factions)8, Color.yellow },
        };
    }

    public class UIStyles
    {
        public static GUISkin WindowSkin
        {
            get
            {
                if (_customSkin == null)
                {
                    try
                    {
                        _customSkin = CreateWindowSkin();
                    }
                    catch
                    {
                        _customSkin = GUI.skin;
                    }
                }

                return _customSkin;
            }
        }

        public static void HorizontalLine(Color color)
        {
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, HorizontalBar);
            GUI.color = c;
        }

        private static GUISkin _customSkin;

        public static Texture2D m_nofocusTex;
        public static Texture2D m_focusTex;

        private static GUIStyle _horizBarStyle;

        private static GUIStyle HorizontalBar
        {
            get
            {
                if (_horizBarStyle == null)
                {
                    _horizBarStyle = new GUIStyle();
                    _horizBarStyle.normal.background = Texture2D.whiteTexture;
                    _horizBarStyle.margin = new RectOffset(0, 0, 4, 4);
                    _horizBarStyle.fixedHeight = 2;
                }

                return _horizBarStyle;
            }
        }

        private static GUISkin CreateWindowSkin()
        {
            var newSkin = UnityEngine.Object.Instantiate(GUI.skin);
            UnityEngine.Object.DontDestroyOnLoad(newSkin);

            m_nofocusTex = MakeTex(550, 700, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            m_focusTex = MakeTex(550, 700, new Color(0.3f, 0.3f, 0.3f, 1f));

            newSkin.window.normal.background = m_nofocusTex;
            newSkin.window.onNormal.background = m_focusTex;

            newSkin.box.normal.textColor = Color.white;
            newSkin.window.normal.textColor = Color.white;
            newSkin.button.normal.textColor = Color.white;
            newSkin.textField.normal.textColor = Color.white;
            newSkin.label.normal.textColor = Color.white;

            return newSkin;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
