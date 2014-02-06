
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyChatScript : MonoBehaviour
{


    public bool usingChat = false;	
    public GUISkin skin;						//Skin
    public bool showChat = false;			

    //Private vars used by the script
    private string inputField = "";

    private Vector2 scrollPosition;
    private int width = 500;
    private int height = 180;
    private string playerName;
    private float lastUnfocus = 0;
    private Rect window;

    //Server-only playerlist
    private List<LobbyPlayerNode> playerList = new List<LobbyPlayerNode>();
    public class LobbyPlayerNode
    {
        public string playerName;
        public NetworkPlayer networkPlayer;
    }

    private List<LobbyChatEntry> chatEntries = new List<LobbyChatEntry>();
    public class LobbyChatEntry
    {
        public string name = "";
        public string text = "";
    }
    //Client function
    void OnConnectedToServer()
    {
        ShowChatWindow();
        networkView.RPC("TellServerOurName", RPCMode.Server, playerName);
    }

    //Server function
    void OnServerInitialized()
    {
        ShowChatWindow();
        LobbyPlayerNode newEntry = new LobbyPlayerNode();
        newEntry.playerName = playerName;
        newEntry.networkPlayer = Network.player;
        playerList.Add(newEntry);
        addGameChatMessage(playerName + " joined the chat");
    }

    //Server function
    void OnPlayerDisconnected(NetworkPlayer player)
    {
        addGameChatMessage("Player disconnected from: " + player.ipAddress + ":" + player.port);

        //Remove player from the server list
        foreach (LobbyPlayerNode entry in playerList as List<LobbyPlayerNode>)
        {
            if (entry.networkPlayer == player)
            {
                playerList.Remove(entry);
                break;
            }
        }
    }

    void OnDisconnectedFromServer()
    {
        CloseChatWindow();
    }

    //Server function
    void OnPlayerConnected(NetworkPlayer player)
    {
        addGameChatMessage("Player connected from: " + player.ipAddress + ":" + player.port);
    }

    [RPC]
    //Sent by newly connected clients, recieved by server
    void TellServerOurName(string name, NetworkMessageInfo info)
    {
        LobbyPlayerNode newEntry = new LobbyPlayerNode();
        newEntry.playerName = name;
        newEntry.networkPlayer = info.sender;
        playerList.Add(newEntry);

        addGameChatMessage(name + " joined the chat");
    }
    void Awake()
    {
        window = new Rect(Screen.width / 2 - width / 2, Screen.height - height + 5, width, height);
        chatEntries = new List<LobbyChatEntry>();
    }

    public void CloseChatWindow()
    {
        showChat = false;
        inputField = "";
        chatEntries = new List<LobbyChatEntry>();
    }

    public void ShowChatWindow()
    {
        playerName = PlayerPrefs.GetString("playerName", "");
        if (playerName == "")
        {
            playerName = "RandomName" + Random.Range(1, 999);
        }

        showChat = true;
        inputField = "";
        chatEntries = new List<LobbyChatEntry>();
    }

    void OnGUI()
    {
        if (!showChat)
        {
            return;
        }

        GUI.skin = skin;

        if (Event.current.type == EventType.keyDown && Event.current.character == '\n' && inputField.Length <= 0)
        {
            if (lastUnfocus + 0.25f < Time.time)
            {
                usingChat = true;
                GUI.FocusWindow(5);
                GUI.FocusControl("Chat input field");
            }
        }

        window = GUI.Window(5, window, GlobalChatWindow, "");
    }
    void GlobalChatWindow(int id)
    {

        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.EndVertical();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (LobbyChatEntry entry in chatEntries as List<LobbyChatEntry>)
        {
            GUILayout.BeginHorizontal();
            if (entry.name == "")
            {//Game message
                GUILayout.Label(entry.text);
            }
            else
            {
                GUILayout.Label(entry.name + ": " + entry.text);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

        }
        // End the scrollview we began above.
        GUILayout.EndScrollView();

        if (Event.current.type == EventType.keyDown && Event.current.character == '\n' && inputField.Length > 0)
        {
            HitEnter(inputField);
        }
        GUI.SetNextControlName("Chat input field");
        inputField = GUILayout.TextField(inputField);


        if (Input.GetKeyDown("mouse 0"))
        {
            if (usingChat)
            {
                usingChat = false;
                GUI.UnfocusWindow();//Deselect chat
                lastUnfocus = Time.time;
            }
        }
    }

    void HitEnter(string msg)
    {
        msg = msg.Replace("\n", "");
        networkView.RPC("ApplyGlobalChatText", RPCMode.All, playerName, msg);
        inputField = ""; //Clear line
        GUI.UnfocusWindow();//Deselect chat
        lastUnfocus = Time.time;
        usingChat = false;
    }
    [RPC]
    public void ApplyGlobalChatText(string name, string msg)
    {
        LobbyChatEntry entry = new LobbyChatEntry();
        entry.name = name;
        entry.text = msg;

        chatEntries.Add(entry);

        //Remove old entries
        if (chatEntries.Count > 4)
        {
            chatEntries.RemoveAt(0);
        }
        
        scrollPosition.y = 1000000;
    }
    
    //Add game messages etc
    public void addGameChatMessage(string str)
    {
        ApplyGlobalChatText("", str);
        if (Network.connections.Length > 0)
        {
            networkView.RPC("ApplyGlobalChatText", RPCMode.Others, "", str);
        }
    }
}
