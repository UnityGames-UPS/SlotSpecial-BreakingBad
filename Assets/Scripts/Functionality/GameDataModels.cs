using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

#region Server Communication Models

[Serializable]
public class InitData
{
    public string id = "initData";
    public ServerGameData gameData;
    public ServerFeatures features;
    public ServerUIData uiData;
    public ServerPlayer player;
}

[Serializable]
public class ServerGameData
{
    public List<List<int>> lines;
    public List<double> bets;
    public int totalLines;
}

[Serializable]
public class ServerFeatures
{
    public FreeSpinFeature freeSpins;
    public BuyFeature buyFeature;
    public int betMultiplier;
    public int maxWinMultiplier;
    public int minWinMultiplier;

    // Breaking Bad specific features
    public Jackpot jackpot;
    public Bonus bonus;
}

[Serializable]
public class FreeSpinFeature
{
    public bool enabled;
    public int initialSpins;
    public bool stickyWilds;
    public bool wildMultiplierPersist;
    public OverlayScatterFeature overlayScatter;
}

[Serializable]
public class OverlayScatterFeature
{
    public bool enabled;
    public List<int> values;
    public ExtraSpinsData extraSpins;
}

[Serializable]
public class ExtraSpinsData
{
    [JsonProperty("2")] public int _2; // For 2 scatters
    [JsonProperty("3")] public int _3; // For 3 scatters
    [JsonProperty("4")] public int _4; // For 4 scatters
    [JsonProperty("5")] public int _5; // For 5 scatters
}

[Serializable]
public class BuyFeature
{
    public bool enabled;
    public double costMultiplier;
}

[Serializable]
public class ServerUIData
{
    public PaylineData paylines;
}

[Serializable]
public class PaylineData
{
    public List<ServerSymbolInfo> symbols;
}

[Serializable]
public class ServerSymbolInfo
{
    public int id;
    public string name;
    public List<double> multiplier; // Note: "multiplier" not "multipliers"
    public string description;      // Breaking Bad symbol description
}

[Serializable]
public class ServerPlayer
{
    public double balance;
}

// ============================================================================
// FIXED: Server Response Models - Must match actual server JSON structure
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id = "ResultData";
    public bool success;
    public ServerPlayerBalance player;
    public ServerPayload payload;
    public ServerFeaturesResult features;
    public List<List<string>> matrix; // Added for Breaking Bad backwards compatibility
}

[Serializable]
public class ServerPlayerBalance
{
    public double? balance; // Nullable because server sends null
}

[Serializable]
public class ServerPayload
{
    public List<List<string>> reels;        // Server sends STRINGS not ints!
    public List<ServerWinLine> winningLines; // Server uses "winningLines"
    public double totalWin;                  // Server uses "totalWin"
    public int scatterCount;
    public bool scatterTriggered;
    public ServerFreeSpinState freeSpinState; // Can be null
    public bool isRoundOver;                 // True when free spin round is over
    public double totalRoundWin;             // Total round win (at payload level when isRoundOver)

    // Breaking Bad specific fields to maintain compatibility
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
public class ServerFreeSpinState
{
    public bool isActive;
    public int spinsRemaining;
    public int spinsUsed;
    public double totalRoundWin;
    public bool isBought;
    public Dictionary<string, int> stickyWilds;
}

[Serializable]
public class ServerWinLine
{
    public int lineIndex;                    // Server uses "lineIndex"
    public List<List<int>> positions;        // Server format: [[row,col], [row,col]]
    public string symbolId;                  // Server sends STRING!
    public int matchCount;
    public double basePayout;
    public double payout;
    public int wildMultiplier;
    public List<WildDetail> wildDetails;
}

[Serializable]
public class WildDetail
{
    public int col;
    public int row;
    public int multiplier;
}

[Serializable]
public class ServerFeaturesResult
{
    public ServerFreeSpinResult freeSpins;
}

[Serializable]
public class ServerFreeSpinResult
{
    public bool triggered;
    public int spinsAwarded;
    public bool isFreeSpin;
    public bool isRoundOver;
    public int spinsRemaining;
    public int spinsUsed;  // Added: Server sends this in features.freeSpins
    public int stickyWildsCount;
    public ServerOverlayScatter overlayScatter;
}

[Serializable]
public class ServerOverlayScatter
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
}

// ============================================================================
// Client-Side Spin Request
// ============================================================================

[Serializable]
public class SpinRequest
{
    public string type = "SPIN";
    public SpinPayload payload;
}

[Serializable]
public class SpinPayload
{
    public int betIndex;
    public bool isFreeSpin;
}

[Serializable]
public class BuyFeatureRequest
{
    public string type = "BUY_FEATURE";
    public BuyFeaturePayload payload;
}

[Serializable]
public class BuyFeaturePayload
{
    public int betIndex;
}


[Serializable]
public class BetHistoryRequest
{
    public string type = "BET_HISTORY";
    public string userId; // Will be set from server session
    public BetHistoryPayload payload;
}

[Serializable]
public class BetHistoryPayload
{
    public int page = 1;
    public int limit = 10;
}

[Serializable]
public class BetHistoryResponse
{
    public string id = "BetHistory";
    public bool success;
    public BetHistoryData payload;
}

[Serializable]
public class BetHistoryData
{
    public List<BetHistoryItem> betHistory;
    public PaginationInfo pagination;
}

[Serializable]
public class BetHistoryItem
{
    public string betSlipNumber;
    public string gameMode;
    public double startingBalance;
    public double bet;
    public double winLoss;
    public double balance;
    public string date; // ISO format: "2026-04-23T14:29:55.265Z"
}

[Serializable]
public class PaginationInfo
{
    public int page;
    public int limit;
    public int total;
    public double totalBetAmount;
    public double totalWinLoss;
    public int totalPages;
}

#endregion

#region Game Configuration (Client Side Converted)

[Serializable]
public class GameConfig
{
    public int reelCount = 5;
    public int rowCount = 3; // Breaking Bad has 3 rows
    public int symbolCount = 13;
    public int paylineCount = 40;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<SymbolInfo> symbols;

    // Wild configuration
    public int wildSymbolId = 11;      // Base wild (1x)
    public int wild2xSymbolId = 13;     // Wild 2x multiplier
    public int wild3xSymbolId = 14;     // Wild 3x multiplier
    public int wild5xSymbolId = 15;     // Wild 5x multiplier
    public List<int> wildMultipliers = new List<int> { 1, 2, 3, 5 };

    // Scatter configuration
    public int scatterSymbolId = 12;

    // Buy Feature configuration
    public bool buyFeatureEnabled;
    public double buyFeatureCostMultiplier;

    public int betMultiplier = 100;
    public int maxWinMultiplier = 10000;
    public int minWinMultiplier = 10;
    public int initialFreeSpins = 8;
    public ExtraSpinsData extraSpinsData;
}

[Serializable]
public class SymbolInfo
{
    public int id;
    public string name;
    public List<double> multipliers;
    public bool isWild;
    public bool isScatter;
    public int wildMultiplier;
}

#endregion

#region Player & Game State (Client Side)

[Serializable]
public class PlayerData
{
    public double balance;
    public int currentBetIndex;
}

[Serializable]
public class SpinResult
{
    public List<List<int>> resultMatrix;  // Client uses int matrix
    public double winAmount;
    public List<WinLine> winLines;
    public PlayerData playerData;
    public FreeSpinData freeSpinData;
    public ScatterData scatterData;
    public OverlayScatterData overlayScatterData;
    public Dictionary<string, int> stickyWilds;

    // Server-authoritative free spin state
    public int serverSpinsRemaining;
    public int serverSpinsUsed;
    public double serverTotalRoundWin;
    public bool isRoundOver;
}

[Serializable]
public class WinLine
{
    public int lineId;
    public int symbolId;
    public List<int> positions;  // Flat list: [0, 5, 10, 15, 20]
    public double winAmount;
}

[Serializable]
public class FreeSpinData
{
    public bool isTriggered;
    public int spinsAwarded;
    public int remainingSpins;
    public bool isBought;
}

[Serializable]
public class ScatterData
{
    public bool isTriggered;
    public int scatterCount;
    public double winAmount;
}

[Serializable]
public class OverlayScatterData
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
}

#endregion

#region Platform Communication

[Serializable]
public class AuthData
{
    public string token;
    public string socketURL;
    public string nameSpace;
}

#endregion

#region Enums

public enum GameState
{
    Initializing,
    Idle,
    Spinning,
    Stopping,
    ShowingWin,
    FreeSpinMode
}

public enum SpinSpeed
{
    Normal,
    Turbo,
    QuickSpin
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts server data to client GameConfig
/// </summary>
public static class InitDataConverter
{
    internal static GameConfig ConvertToGameConfig(InitData serverData)
    {
        var config = new GameConfig
        {
            reelCount = 5,
            rowCount = 3, // Breaking Bad layout has 3 rows
            symbolCount = serverData.uiData.paylines.symbols.Count,
            paylineCount = serverData.gameData.totalLines,
            paylines = serverData.gameData.lines,
            availableBets = serverData.gameData.bets,
            symbols = new List<SymbolInfo>()
        };

        foreach (var serverSymbol in serverData.uiData.paylines.symbols)
        {
            var symbolInfo = new SymbolInfo
            {
                id = serverSymbol.id,
                name = serverSymbol.name,
                multipliers = serverSymbol.multiplier ?? new List<double>(),
                isWild = serverSymbol.name.ToLower().Contains("wild"),
                isScatter = serverSymbol.name.ToLower().Contains("scatter"),
                wildMultiplier = 1
            };

            config.symbols.Add(symbolInfo);

            if (symbolInfo.isWild)
            {
                config.wildSymbolId = symbolInfo.id;
            }
            if (symbolInfo.isScatter)
            {
                config.scatterSymbolId = symbolInfo.id;
            }
        }

        // Buy Feature config
        if (serverData.features?.buyFeature != null)
        {
            config.buyFeatureEnabled = serverData.features.buyFeature.enabled;
            config.buyFeatureCostMultiplier = serverData.features.buyFeature.costMultiplier;
        }

        if (serverData.features != null)
        {
            config.betMultiplier = serverData.features.betMultiplier > 0 ? serverData.features.betMultiplier : 1;
            config.maxWinMultiplier = serverData.features.maxWinMultiplier;
            config.minWinMultiplier = serverData.features.minWinMultiplier;

            if (serverData.features.freeSpins != null)
            {
                config.initialFreeSpins = serverData.features.freeSpins.initialSpins;
                if (serverData.features.freeSpins.overlayScatter != null)
                {
                    config.extraSpinsData = serverData.features.freeSpins.overlayScatter.extraSpins;
                }
            }
        }

        return config;
    }

    internal static PlayerData ConvertToPlayerData(ServerPlayer serverPlayer, int defaultBetIndex = 0)
    {
        return new PlayerData
        {
            balance = serverPlayer.balance,
            currentBetIndex = defaultBetIndex
        };
    }

    /// <summary>
    /// CRITICAL: Converts server response to client SpinResult
    /// Handles string-to-int conversion, matrix transposition, and wild multiplier mapping
    /// Server sends [row][col] (3 rows x 5 cols), Client needs [col][row] (5 cols x 3 rows)
    /// </summary>
    internal static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount, GameConfig gameConfig)
    {
        // Use server balance directly if available
        double newBalance = serverResponse.player?.balance ?? CalculateNewBalance(currentBalance, betAmount, serverResponse.payload.totalWin);

        // Get server free spin state values
        int spinsRemaining = serverResponse.features?.freeSpins?.spinsRemaining ?? serverResponse.payload.freeSpinState?.spinsRemaining ?? 0;
        int spinsUsed = serverResponse.features?.freeSpins?.spinsUsed ?? serverResponse.payload.freeSpinState?.spinsUsed ?? 0;
        double totalRoundWin = serverResponse.payload.totalRoundWin > 0
            ? serverResponse.payload.totalRoundWin
            : (serverResponse.payload.freeSpinState?.totalRoundWin ?? 0);
        bool isRoundOver = serverResponse.features?.freeSpins?.isRoundOver ?? serverResponse.payload.isRoundOver;

        var stickyWilds = serverResponse.payload.freeSpinState?.stickyWilds;

        var result = new SpinResult
        {
            // Convert and transpose reels from server format to client format
            resultMatrix = ConvertReelsToMatrix(serverResponse.payload.reels ?? serverResponse.matrix, serverResponse.payload.winningLines, stickyWilds, gameConfig),

            // Map totalWin to winAmount
            winAmount = serverResponse.payload.totalWin,

            // Convert winningLines to winLines
            winLines = ConvertWinningLines(serverResponse.payload.winningLines, gameConfig),

            // Update player data — use server balance directly
            playerData = new PlayerData
            {
                balance = newBalance,
                currentBetIndex = 0 // Will be set by GameManager
            },

            // Convert free spin data
            freeSpinData = serverResponse.features?.freeSpins != null && serverResponse.features.freeSpins.triggered
                ? new FreeSpinData
                {
                    isTriggered = true,
                    spinsAwarded = serverResponse.features.freeSpins.spinsAwarded,
                    remainingSpins = 0,
                    isBought = serverResponse.payload.freeSpinState?.isBought ?? false
                }
                : null,

            // Convert scatter data
            scatterData = serverResponse.payload.scatterTriggered
                ? new ScatterData
                {
                    isTriggered = true,
                    scatterCount = serverResponse.payload.scatterCount,
                    winAmount = 0 // Calculate if needed
                }
                : null,

            overlayScatterData = serverResponse.features?.freeSpins?.overlayScatter != null && serverResponse.features.freeSpins.overlayScatter.isTriggered
                ? new OverlayScatterData
                {
                    isTriggered = true,
                    count = serverResponse.features.freeSpins.overlayScatter.count,
                    extraSpins = serverResponse.features.freeSpins.overlayScatter.extraSpins,
                    positions = serverResponse.features.freeSpins.overlayScatter.positions
                }
                : null,

            stickyWilds = serverResponse.payload.freeSpinState?.stickyWilds,

            // Server-authoritative free spin state
            serverSpinsRemaining = spinsRemaining,
            serverSpinsUsed = spinsUsed,
            serverTotalRoundWin = totalRoundWin,
            isRoundOver = isRoundOver
        };

        return result;
    }


    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverReels, List<ServerWinLine> winningLines, Dictionary<string, int> stickyWilds, GameConfig gameConfig)
    {
        // Server sends 3 rows x 5 columns: reels[row][col]
        // Client needs 5 columns x 3 rows: matrix[col][row]

        if (serverReels == null || serverReels.Count != 3)
        {
            UnityEngine.Debug.LogError($"Invalid server reels: expected 3 rows, got {serverReels?.Count}");
            return GenerateDefaultMatrix();
        }

        // Build wild multiplier lookup: [col][row] -> multiplier
        var wildMultipliers = new Dictionary<string, int>();

        // 1. Add winning line wild details (format explicit col, row)
        if (winningLines != null)
        {
            foreach (var line in winningLines)
            {
                if (line.wildDetails != null)
                {
                    foreach (var wild in line.wildDetails)
                    {
                        string key = $"{wild.col}_{wild.row}";
                        wildMultipliers[key] = wild.multiplier;
                    }
                }
            }
        }

        // 2. Add sticky wilds (format row_col) - these override winningLines if they overlap
        // to ensure the authoritative sticky multiplier is used (e.g. 3x instead of 1x)
        if (stickyWilds != null)
        {
            foreach (var kvp in stickyWilds)
            {
                string[] parts = kvp.Key.Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int row) &&
                    int.TryParse(parts[1], out int col))
                {
                    // Convert row_col to col_row for lookup
                    string key = $"{col}_{row}";
                    wildMultipliers[key] = kvp.Value;
                }
            }
        }

        var matrix = new List<List<int>>();

        // Transpose: iterate by columns
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();

            // Each column has 3 rows
            for (int row = 0; row < 3; row++)
            {
                if (col >= serverReels[row].Count)
                {
                    UnityEngine.Debug.LogError($"Invalid server data at row {row}, col {col}");
                    column.Add(0);
                    continue;
                }

                string symbolStr = serverReels[row][col];

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    column.Add(0);
                    continue;
                }

                // Check if this is a wild with multiplier
                if (symbolId == gameConfig.wildSymbolId)
                {
                    string key = $"{col}_{row}";
                    if (wildMultipliers.TryGetValue(key, out int multiplier))
                    {
                        // Map wild multiplier to correct symbol ID
                        symbolId = GetWildSymbolIdForMultiplier(multiplier, gameConfig);
                    }
                }

                column.Add(symbolId);
            }

            matrix.Add(column);
        }

        return matrix;
    }

    /// <summary>
    /// Maps wild multiplier to correct symbol ID
    /// 1x → 11 (Wild), 2x → 13 (Wild2x), 3x → 14 (Wild3x), 5x → 15 (Wild5x)
    /// </summary>
    private static int GetWildSymbolIdForMultiplier(int multiplier, GameConfig gameConfig)
    {
        return multiplier switch
        {
            1 => 11,  // Wild (normal)
            2 => 13,  // Wild 2x
            3 => 14,  // Wild 3x
            5 => 15,  // Wild 5x
            _ => 11   // Default to normal wild
        };
    }


    private static List<List<int>> GenerateDefaultMatrix()
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < 3; row++)
            {
                column.Add(0);
            }
            matrix.Add(column);
        }
        return matrix;
    }

    /// <summary>
    /// Converts server winningLines to client winLines.
    /// Uses the server-provided positions directly: each position is [row, col].
    /// Encodes as flat index = col * rowCount + row (rowCount = 3).
    /// </summary>
    private static List<WinLine> ConvertWinningLines(List<ServerWinLine> serverWinLines, GameConfig gameConfig)
    {
        var winLines = new List<WinLine>();

        if (serverWinLines == null) return winLines;

        foreach (var serverLine in serverWinLines)
        {
            // Parse symbolId from string to int
            if (!int.TryParse(serverLine.symbolId, out int symbolId))
            {
                UnityEngine.Debug.LogError($"Failed to parse symbolId: {serverLine.symbolId}");
                continue;
            }

            var flatPositions = new List<int>();

            if (serverLine.positions != null && serverLine.positions.Count > 0)
            {
                foreach (var pos in serverLine.positions)
                {
                    if (pos.Count >= 2)
                    {
                        int row = pos[0];
                        int col = pos[1];
                        int flatIndex = row * 5 + col;
                        flatPositions.Add(flatIndex);
                    }
                }
            }
            else
            {
                // Fallback: derive from payline definition + matchCount if positions missing
                UnityEngine.Debug.LogWarning($"[ConvertWinningLines] No positions from server for lineIndex {serverLine.lineIndex}, falling back to payline table");
                if (gameConfig?.paylines != null &&
                    serverLine.lineIndex >= 0 &&
                    serverLine.lineIndex < gameConfig.paylines.Count)
                {
                    var payline = gameConfig.paylines[serverLine.lineIndex];
                    for (int col = 0; col < serverLine.matchCount && col < payline.Count; col++)
                    {
                        int row = payline[col];
                        flatPositions.Add(row * 5 + col);
                    }
                }
            }

            winLines.Add(new WinLine
            {
                lineId = serverLine.lineIndex,
                symbolId = symbolId,
                positions = flatPositions,
                winAmount = serverLine.payout
            });
        }

        return winLines;
    }

    private static double CalculateNewBalance(double currentBalance, double betAmount, double winAmount)
    {
        return currentBalance - betAmount + winAmount;
    }
}

#endregion

#region Breaking Bad Custom Data Models

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}

[Serializable]
public class AbtLogo
{
    public string logoSprite;
    public string link;
}

[Serializable]
public class Jackpot
{
    public List<double> payout;
    public List<double> payoutProbs;
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
public class CashCollectResult
{
    public bool triggered;
    public List<CollectedCoin> collectedCoins;
    public double totalValue;
    public Newtonsoft.Json.Linq.JToken positions;
}

[Serializable]
public class CollectedCoin
{
    public List<int> position;
    public int coinValue;
    public int symbolId;
    public string symbolName;
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
    public Newtonsoft.Json.Linq.JToken cashCollectPositions;
    public bool isWalterStashGrandPrize;
    public double grandPrizeAmount;
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
public class FinalCoin
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
public class BetData
{
    public double currentBet;
    public double currentLines;
    public double spins;
}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

[Serializable]
public class SlotTransform
{
    public List<Transform> slotTransforms = new List<Transform>();
}

[Serializable]
public class Column
{
    public List<int> index = new();
}

#endregion
