using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField] private string testToken = "test-token";
    protected string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    protected string nameSpace = "playground";
    protected string gameID = "SL-BB";

    [Header("References")]
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private GameObject RaycastBlocker;

    private SocketManager socketManager;
    private Socket gameSocket;

    private string authToken;
    private string socketURL;

    internal bool isConnected;
    internal bool isInitialized;
    internal bool initializationFailed;
    internal bool isExiting;   

    
    internal InitData initialData = null;
    internal ServerUIData initUIData = null;
    internal ServerSpinResponse resultData = null;
    internal ServerPlayer playerdata = null;
    internal List<string> bonusdata = null;
    internal List<List<int>> LineData = null;
    internal bool isResultdone = false;
    internal bool isLoaded = false;
    internal bool SetInit = false;

    
    private Coroutine pingCoroutine;
    private float lastPongTime;
    private bool waitingForPong;
    private int missedPongs;
    private const int MAX_MISSED_PONGS = 15;
    private const float PING_INTERVAL = 2f;
    private const float PONG_TIMEOUT = 5f;

    #region Initialization

    private void Awake()
    {
        isInitialized = false;
        isConnected = false;
        isExiting = false;
        initializationFailed = false;
        isLoaded = false;
        SetInit = false;
    }

    private void Start()
    {
        RequestAuthToken();
    }

    private void RequestAuthToken()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("authToken");
        }
        StartCoroutine(WaitForAuthToken());
#else
        authToken = testToken;
        socketURL = testSocketURL;
        InitializeSocket();
#endif
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("[SocketIO] Auth received");

        try
        {
            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);
            authToken = authData.cookie;
            socketURL = authData.socketURL;

            if (!string.IsNullOrEmpty(authData.nameSpace))
            {
                nameSpace = authData.nameSpace;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Auth parse failed: {e.Message}");
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator WaitForAuthToken()
    {
        while (string.IsNullOrEmpty(authToken))
        {
            yield return null;
        }
        while (string.IsNullOrEmpty(socketURL))
        {
            yield return null;
        }
        InitializeSocket();
    }
#endif

    private void InitializeSocket()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(3),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken, gameId = gameID };

#if UNITY_EDITOR
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        gameSocket = string.IsNullOrEmpty(nameSpace)
            ? socketManager.Socket
            : socketManager.GetSocket("/" + nameSpace);

        
        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        
        gameSocket.On<string>("game:init", OnInitReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);

        
        gameSocket.On<string>("internalError", (data) => Debug.LogWarning($"[SocketIO] Internal error: {data}"));
        gameSocket.On<string>("alert", (data) => {});

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("[SocketIO] Connected");

        isConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;

        if (popupManager != null)
        {
            popupManager.CloseReconnectionPopup();
        }

        StartPingRoutine();
    }

    private void OnSocketDisconnected()
    {
        Debug.Log("[SocketIO] Disconnected");

        isConnected = false;
        StopPingRoutine();

        if (isExiting)
        {
            
            if (popupManager != null && !popupManager.IsLoadingPopupActive())
            {
                popupManager.ShowLoadingPopup(0f); 
            }
            
        }
        else
        {
            
            if (popupManager != null)
            {
                popupManager.ShowDisconnectionPopup();
            }
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

        if (!isInitialized)
        {
            initializationFailed = true;
        }

        if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
        {
            Debug.LogWarning("[SocketIO] Session expired detected");

            isConnected = false;
            StopPingRoutine();

            if (popupManager != null)
            {
                popupManager.ShowDisconnectionPopup("Your session has expired. Please relaunch the game.");
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null) JSManager.SendCustomMessage("session_expired");
#endif
        }
        else
        {
            if (popupManager != null)
            {
                popupManager.ShowServerError(err.message);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null) JSManager.SendCustomMessage("error");
#endif
        }
    }

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            InitData myData = JsonConvert.DeserializeObject<InitData>(jsonData);

            
            initialData = myData;
            initUIData = myData.uiData;
            playerdata = myData.player;
            LineData = myData.gameData.lines;

            
            slotManager.Initialize(myData);

            isInitialized = true;

            if (!SetInit)
            {
                PopulateSlotSocket();
                SetInit = true;
            }
            else
            {
                RefreshUI();
            }

            if (RaycastBlocker) RaycastBlocker.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
            initializationFailed = true;
            if (popupManager != null)
            {
                popupManager.ShowServerError("Failed to parse game initialization data.");
            }
        }
    }

    private void OnResultReceived(string jsonData)
    {
        if (!jsonData.Contains("\"id\":\"ResultData\""))
        {
            
            HandleNonResultData(jsonData);
            return;
        }

        Debug.Log($"[SocketIO] Result received: {jsonData}");

        try
        {
            ServerSpinResponse myData = JsonConvert.DeserializeObject<ServerSpinResponse>(jsonData);
            resultData = myData;

            if (myData.player != null)
            {
                if (playerdata == null) playerdata = new ServerPlayer();
                playerdata.balance = myData.player.balance ?? playerdata.balance;
            }

            slotManager.UpdateFromSpinResult(myData);
            isResultdone = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Result parse failed: {e.Message}");
        }
    }

    private void HandleNonResultData(string jsonData)
    {
        try
        {
            var jobj = Newtonsoft.Json.Linq.JObject.Parse(jsonData);
            string id = jobj["id"]?.ToString();

            switch (id)
            {
                case "ExitUser":
                    Debug.Log("[SocketIO] ExitUser received");
                    if (gameSocket != null) gameSocket.Disconnect();
                    if (socketManager != null)
                    {
                        socketManager.Close();
                    }
#if UNITY_WEBGL && !UNITY_EDITOR
                    if (JSManager != null) JSManager.SendCustomMessage("onExit");
#endif
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Non-result parse failed: {e.Message}");
        }
    }

    private void OnAnotherDevice(string data)
    {
        Debug.Log("[SocketIO] Another device login");

        if (popupManager != null)
        {
            popupManager.ShowAnotherDeviceError();
        }
    }

    #endregion

    #region Raycast Blocker

    internal void SetRaycastBlocker(bool active)
    {
        if (RaycastBlocker != null) RaycastBlocker.SetActive(active);
    }

    #endregion

    #region Ping/Pong Health Check

    private void StartPingRoutine()
    {
        StopPingRoutine();
        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private void StopPingRoutine()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
    }

    private IEnumerator PingRoutine()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(PING_INTERVAL);

            if (waitingForPong)
            {
                float timeSinceLastPong = Time.time - lastPongTime;

                if (timeSinceLastPong > PONG_TIMEOUT)
                {
                    missedPongs++;

                    Debug.LogWarning($"[SocketIO] Pong missed #{missedPongs}/{MAX_MISSED_PONGS}");

                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogError("[SocketIO] Max pongs missed - disconnecting");
                        isConnected = false;

                        if (popupManager != null)
                        {
                            popupManager.ShowDisconnectionPopup();
                        }
                        yield break;
                    }

                    if (missedPongs >= 1 && popupManager != null)
                    {
                        popupManager.ShowReconnectionPopup(missedPongs, MAX_MISSED_PONGS);
                    }
                }
            }

            SendPing();
            waitingForPong = true;
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("ping");
        }
    }

    private void OnPongReceived(string data)
    {
        waitingForPong = false;
        lastPongTime = Time.time;

        if (missedPongs > 0)
        {
            missedPongs = 0;

            if (popupManager != null)
            {
                popupManager.CloseReconnectionPopup();
            }
        }
    }

    #endregion

    #region Spin Request

    internal void AccumulateResult(double currBet)
    {
        isResultdone = false;

        MessageData message = new MessageData();
        message.payload = new SentDeta();
        message.type = "SPIN";
        message.payload.betIndex = slotManager.BetCounter;

        string json = JsonUtility.ToJson(message);

        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("request", json);
            Debug.Log($"[SocketIO] Spin request sent: {json}");
        }
        else
        {
            Debug.LogWarning("[SocketIO] Cannot send spin - not connected");
        }
    }

    #endregion

    #region UI Helpers

    private void RefreshUI()
    {
        uiManager.InitialiseUIData(initUIData.paylines);
    }

    private void PopulateSlotSocket()
    {
        if (RaycastBlocker != null) RaycastBlocker.SetActive(false);
        slotManager.SetInitialUI();
        isLoaded = true;
    }

    #endregion

    #region Cleanup

    internal IEnumerator CloseSocket()
    {
        
        
        isExiting = true;

        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        StopPingRoutine();

        Debug.Log("[SocketIO] Closing socket");

        if (socketManager != null)
        {
            socketManager.Close();
            socketManager = null;
        }

        isConnected = false;

        
        
        if (popupManager != null && !popupManager.IsLoadingPopupActive())
        {
            popupManager.ShowLoadingPopup(0f);
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[SocketIO] Socket closed");

#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnExit");
        }
#endif
    }

    private void OnDisable()
    {
        StopPingRoutine();
    }

    private void OnDestroy()
    {
        StopPingRoutine();

        if (socketManager != null)
        {
            socketManager.Close();
            socketManager = null;
        }
    }

    #endregion
}
