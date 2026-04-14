using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField]
  private SlotBehaviour slotManager;
  [SerializeField]
  private UIManager uiManager;
  [SerializeField] internal JSFunctCalls JSManager;
  private Socket gameSocket;
  protected string nameSpace = "playground";
  internal Root initialData = null;
  internal UiData initUIData = null;
  [SerializeField] internal Root resultData = null;
  [SerializeField] internal Player playerdata = null;
  [SerializeField] internal List<string> bonusdata = null;
  internal List<List<int>> LineData = null;
  internal bool isResultdone = false;
  private SocketManager manager;
  protected string SocketURI = null;
  // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
  //protected string TestSocketURI = "https://6f01c04j-5000.inc1.devtunnels.ms/";
  //protected string TestSocketURI = "https://7p68wzhv-5000.inc1.devtunnels.ms/"; //vikings
  //protected string TestSocketURI = "https://916smq0d-5001.inc1.devtunnels.ms/";
  protected string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
  [SerializeField]
  private string testToken;
  protected string gameID = "SL-BB";
  //  protected string gameID = "";
  private bool isConnected = false; //Back2 Start
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private float pongTimeout = 3f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  private Coroutine PingRoutine; //Back2 end
  internal bool isLoaded = false;
  internal bool SetInit = false;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);

  private void Awake()
  {
    isLoaded = false;
    SetInit = false;
  }

  private void Start()
  {
    OpenSocket();
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);

    // Proceed with connecting to the server using myAuth and socketURL
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
  }

  string myAuth = null;

  private void OpenSocket()
  {
    // Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.ReconnectionAttempts = maxReconnectionAttempts;
    options.ReconnectionDelay = reconnectionDelay;
    options.Reconnection = true;
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = testToken,
        gameId = gameID
      };
    };
    options.Auth = authFunction;
    // Proceed with connecting to the server
    SetupSocketManager(options);
#endif
  }


  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      Debug.Log("My Auth is null");
      yield return null;
    }
    while (SocketURI == null)
    {
      Debug.Log("My Socket is null");
      yield return null;
    }
    Debug.Log("My Auth is not null");

    // Once myAuth is set, configure the authFunction
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
        gameId = gameID
      };
    };
    options.Auth = authFunction;

    Debug.Log("Auth function configured with token: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);
  }

  private void SetupSocketManager(SocketOptions options)
  {
#if UNITY_EDITOR
    // Create and setup SocketManager for Testing
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    // Create and setup SocketManager
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(nameSpace) | string.IsNullOrWhiteSpace(nameSpace))
    {
      gameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("result", OnResult);
    //gameSocket.On<string>("gamble:result", OnGameResult);
    //gameSocket.On<string>("bonus:result", OnBonusResult);
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("pong", OnPongReceived); //Back2 Start
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice); //BackendChanges Finish
    manager.Open();
  }

  // Connected event handler implementation
  void OnBonusResult(string data)
  {
    // Handle the game result here
    Debug.Log("Bonus Result: " + data);

    ParseResponse(data);

  }
  // Connected event handler implementation
  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  } //Back2 end
  private void OnError(Error err)
  {
    Debug.LogError("[ERROR] Socket error: " + err);
    if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
    {
      Debug.LogWarning("Session expired detected");
      OnDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("session_expired");
#endif
    }
    else
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("error");
#endif
    }
  }
  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    uiManager.DisconnectionPopup();
    isConnected = false;
    ResetPingRoutine();
  } //Back2 end
  private void OnPongReceived(string data) //Back2 Start
  {
    // Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    // Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    // Debug.Log($"📦 Pong payload: {data}");
  } //Back2 end

  private void OnError(string response)
  {
    Debug.LogError("Error: " + response);
  }

  private void OnListenEvent(string data)
  {
    Debug.Log("Received some_event with data: " + data);
    ParseResponse(data);
  }
  void OnResult(string data)
  {
    // print(data);
    ParseResponse(data);
  }
  private void OnSocketState(bool state)
  {
    if (state)
    {
      Debug.Log("my state is " + state);
      //InitRequest("AUTH");
    }
  }

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }
  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }
  private void OnSocketAlert(string data)
  {
    Debug.Log("Received alert with data: " + data);
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
    uiManager.ADfunction();
  }

  private void SendPing() //Back2 Start
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }
  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private void AliveRequest()
  {
    SendDataWithNamespace("YES I AM ALIVE");
  }
  private IEnumerator PingCheck()
  {
    while (true)
    {
      // Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          uiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      // Debug.Log("📤 Sending ping...");
      SendDataWithNamespace("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end


  private void SendDataWithNamespace(string eventName, string json = null)
  {
    // Send the message
    if (gameSocket != null && gameSocket.IsOpen)
    {
      if (json != null)
      {
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        gameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }
  internal void closeSocketReactnativeCall()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("onExit");
#endif
  }

  // internal void CloseSocket()
  // {
  //   SendDataWithNamespace("EXIT");
  // }

  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          initialData = myData;
          initUIData = myData.uiData;
          playerdata = myData.player;
          LineData = myData.gameData.lines;

          if (!SetInit)
          {
            PopulateSlotSocket();
            SetInit = true;
          }
          else
          {
            RefreshUI();
          }
          break;
        }
      case "ResultData":
        {
          // Debug.Log(jsonObject);
          resultData = myData;
          playerdata = myData.player;
          // myData.message.GameData.FinalsymbolsToEmit = TransformAndRemoveRecurring(myData.message.GameData.symbolsToEmit);
          // resultData = myData.message.GameData;
          isResultdone = true;
          break;
        }
      case "ExitUser":
        {
          gameSocket.Disconnect();
          if (this.manager != null)
          {
            Debug.Log("Dispose my Socket");
            this.manager.Close();
          }
#if UNITY_WEBGL && !UNITY_EDITOR
                    JSManager.SendCustomMessage("onExit");
#endif
          break;
        }
    }
  }

  private void RefreshUI()
  {
    uiManager.InitialiseUIData(initUIData.paylines);
  }

  private void PopulateSlotSocket()
  {
    uiManager.RaycastBlocker.SetActive(false);
    slotManager.SetInitialUI();

    isLoaded = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("OnEnter");
#endif
  }

  internal void AccumulateResult(double currBet)
  {
    isResultdone = false;
    MessageData message = new MessageData();
    message.payload = new SentDeta();
    message.type = "SPIN";
    Debug.Log(slotManager.BetCounter);
    message.payload.betIndex = slotManager.BetCounter;
    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }
  internal IEnumerator CloseSocket() //Back2 Start
  {
    uiManager.RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  } //Back2 end
  private List<string> RemoveQuotes(List<string> stringList)
  {
    for (int i = 0; i < stringList.Count; i++)
    {
      stringList[i] = stringList[i].Replace("\"", ""); // Remove inverted commas
    }
    return stringList;
  }

  private List<string> ConvertListListIntToListString(List<List<int>> listOfLists)
  {
    List<string> resultList = new List<string>();

    foreach (List<int> innerList in listOfLists)
    {
      // Convert each integer in the inner list to string
      List<string> stringList = new List<string>();
      foreach (int number in innerList)
      {
        stringList.Add(number.ToString());
      }

      // Join the string representation of integers with ","
      string joinedString = string.Join(",", stringList.ToArray()).Trim();
      resultList.Add(joinedString);
    }

    return resultList;
  }

  private List<string> ConvertListOfListsToStrings(List<List<string>> inputList)
  {
    List<string> outputList = new List<string>();

    foreach (List<string> row in inputList)
    {
      string concatenatedString = string.Join(",", row);
      outputList.Add(concatenatedString);
    }

    return outputList;
  }

  private List<string> TransformAndRemoveRecurring(List<List<string>> originalList)
  {
    // Flattened list
    List<string> flattenedList = new List<string>();
    foreach (List<string> sublist in originalList)
    {
      flattenedList.AddRange(sublist);
    }

    // Remove recurring elements
    HashSet<string> uniqueElements = new HashSet<string>(flattenedList);

    // Transformed list
    List<string> transformedList = new List<string>();
    foreach (string element in uniqueElements)
    {
      transformedList.Add(element.Replace(",", ""));
    }

    return transformedList;
  }
}

[Serializable]
public class BetData
{
  public double currentBet;
  public double currentLines;
  public double spins;
}

[Serializable]
public class AuthData
{
  public string GameID;
}

[Serializable]
public class MessageData
{
  public string type;

  public SentDeta payload;

}
[Serializable]
public class SentDeta
{
  public int betIndex;
  public string Event;
  public double lastWinning;
  public int index;
}
[Serializable]
public class ExitData
{
  public string id;
}

[Serializable]
public class InitData
{
  public AuthData Data;
  public string id;
}

[Serializable]
public class AbtLogo
{
  public string logoSprite;
  public string link;
}

// [Serializable]
// public class GameData
// {
//   public List<List<string>> Reel ;
//   public List<List<int>> Lines ;
//   public List<double> Bets ;
//   public bool canSwitchLines ;
//   public List<int> LinesCount ;
//   public List<int> autoSpin ;
//   public List<List<string>> ResultReel ;
//   public List<int> linesToEmit ;
//   public List<List<string>> symbolsToEmit ;
//   public double WinAmout ;
//   public FreeSpins freeSpins ;
//   public WinData winData ;
//   public List<string> FinalsymbolsToEmit ;
//   public List<string> FinalResultReel ;
//   public Jackpot jackpot ;
//   public List<double> Jackpot ;
//   public Bonus bonus ;
//   public bool isBonus ;
//   public double BonusStopIndex ;
//   public bool isCoinCollect ;
// }
// public class Jackpot
// {
//   public bool isTriggered ;
//   public double payout ;
// }

// [Serializable]
// public class Bonus
// {
//   public bool isBonus ;
//   public List<List<int>> BonusResult ; // Result of bonus symbols
//   public double payout ;
//   public int spinCount ;
//   public bool freeSpinAdded ;
//   public bool isWalterStash ;
//   public double walterStashPayout ;
//   public bool isGrandPrize ;
//   public double grandPrizePayout ;
//   public List<List<int>> freezeIndices ;
//   public List<CoinValues> coins ;
// }

[System.Serializable]
public class WinData
{
  public List<CoinValues> coinValues;
  public List<LosPollos> losPollos;
}

[System.Serializable]
public class LosPollos
{
  public List<int> index;
  public int value;
}

[System.Serializable]
public class CoinValues
{
  public List<int> index;
  public double value;
}

[Serializable]
public class SpecialSymbols
{
  public int count;
  public bool isNewAdded;
}

[Serializable]
public class FreeSpins
{
  public int count;
  public bool isNewAdded;
}

[Serializable]
public class Message
{
  public GameData GameData;
  public UIData UIData;
  public PlayerData PlayerData;
  public List<string> BonusData;
}

// [Serializable]
// public class Root
// {
//   public string id ;
//   public Message message ;
// }

[Serializable]
public class UIData
{
  public Paylines paylines;
  public List<string> spclSymbolTxt;
  public AbtLogo AbtLogo;
  public string ToULink;
  public string PopLink;
}

// [Serializable]
// public class Paylines
// {
//   public List<Symbol> symbols ;
// }



[Serializable]
public class PlayerData
{
  public double Balance;
  public double haveWon;
  public double currentWining;
}
[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace = "";
}


// new V2




public class CollectedCoin
{
  public List<int> position;
  public int coinValue;
  public int symbolId;
  public string symbolName;
}


[Serializable]
public class CashCollectResult
{
  public bool triggered;
  public List<object> collectedCoins;
  public double totalValue;
  public List<int> positions;
}

[Serializable]
public class CoinPosition
{
  public List<int> position;
  public int coinValue;
  public int symbolId;
  public string symbolName;


  public string prizeType;
  public int? prizeTypeIndex;

}


[Serializable]
public class FinalCoin
{
  public List<int> position;
  public int coinValue;
  public int symbolId;
  public string symbolName;
}
[Serializable]
public class InitialCoin
{
  public List<int> position;
  public int coinValue;
  public int symbolId;
  public string symbolName;
}
[Serializable]
public class LineWin
{
  public int lineIndex;
  public List<int> positions;
  public List<int> pattern;
  public string symbolId;
  public string symbolName;
  public double payout;
  public int matchCount;
}
[Serializable]
public class LinkFeatureResult
{
  public bool triggered;
  public string linkType;
  public List<InitialCoin> initialCoins;
  public int respins;
  public List<FinalCoin> finalCoins;
  public double totalValue;
  public List<int> cashCollectPositions;
}
[Serializable]
public class Payload
{
  public double winAmount;
  public List<LineWin> lineWins;
  public List<CoinPosition> coinPositions;
  public CashCollectResult cashCollectResult;
  public LinkFeatureResult linkFeatureResult;
  public int activeLines;
  public int freeSpinsRemaining;
  public bool isFreeSpinActive;
  public bool linkFeatureActive;
  public int linkRespinsRemaining;
  public List<object> lockedCashCollects;

  public bool isFreeSpinTriggered;
  public FreeSpinResult freeSpinResult;


  public bool isLinkTriggered;
  public bool isPrizeCoinTriggered;

}

[Serializable]
public class FreeSpinResult
{
  public bool triggered;
  public int freeSpinCount;
  public List<TriggerCoin> triggerCoins;
  public List<int> lpValues;
  public int freeSpinsRemaining;
}

[Serializable]
public class TriggerCoin
{
  public List<int> position;
  public int coinValue;
  public int symbolId;
  public string symbolName;
}

[Serializable]
public class Player
{
  public double balance;
}
[Serializable]
public class Root
{
  public bool success;
  public List<List<string>> matrix;
  public string id;
  public Payload payload;
  public Features features;
  public Player player;


  //init

  public GameData gameData;

  public UiData uiData;

}





[Serializable]
public class Bonus
{
  public string type;
  public bool isEnabled;
  public int noOfItem;
  public List<int> payOut;
  public List<double> payOutProb;
}
[Serializable]
public class Features
{
  public Bonus bonus;
  public FreeSpin freeSpin;
  public Jackpot jackpot;
}
[Serializable]
public class FreeSpin
{
  public bool isEnabled;
  public List<int> LPValue;
  public List<double> LPValueProbs;
}
[Serializable]
public class GameData
{
  public List<List<int>> lines;
  public List<double> bets;
  public int totalLines;
}
[Serializable]
public class Jackpot
{
  public List<int> payout;
  public List<double> payoutProbs;
}
[Serializable]
public class Paylines
{
  public List<Symbol> symbols;
}



[Serializable]
public class Symbol
{
  public int id;
  public string name;
  public List<int> multiplier;
  public string description;
}
[Serializable]
public class UiData
{
  public Paylines paylines;
}
