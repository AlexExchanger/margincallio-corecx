
namespace CoreCX
{
    internal enum FuncIds
    {
        CreateAccount = 100,
        SuspendAccount = 200,
        UnsuspendAccount = 300,
        DeleteAccount = 400,
        DepositFunds = 500,
        WithdrawFunds = 600,
        PlaceLimit = 700,
        PlaceMarket = 800,
        CancelOrder = 900,
        SetAccountFee = 1000,

        GetAccountBalance = 2400,
        GetAccountParameters = 2500,
        GetAccountFee = 2600,
        GetOpenOrders = 2700,
        GetOrderInfo = 2800,
        
        CreateCurrencyPair = 5000,
        GetCurrencyPairs = 5100,
        GetDerivedCurrencies = 5200,
        DeleteCurrencyPair = 5300,

        GetTicker = 7000,
        GetDepth = 7100,

        CloseMarket = 8800,
        OpenMarket = 8900,
        BackupCore = 9000,
        RestoreCore = 9100,
        ResetFuncCallId = 9500,        

        ManageMargin = 60000, //replication-only
        ManageConditionalOrders = 61000 //replication-only
    }

    internal enum MarketClosedForbiddenFuncIds
    {
        PlaceLimit = 700,
        PlaceMarket = 800,
        CancelOrder = 900
    }

    internal enum StatusCodes
    {
        Success = 0,
        ErrorAccountAlreadyExists = 1,
        ErrorAccountNotFound = 2,
        ErrorAccountAlreadySuspended = 3,
        ErrorAccountAlreadyUnsuspended = 4,
        ErrorAccountSuspended = 5,
        ErrorCrossUserAccessDenied = 6,
        ErrorInsufficientFunds = 7,
        ErrorIncorrectOrderKind = 8,
        ErrorOrderNotFound = 9,
        ErrorInsufficientMarketVolume = 10,
        ErrorBorrowedFundsUse = 11,
        ErrorNegativeOrZeroValue = 12,
        ErrorNegativeOrZeroId = 13,
        ErrorApiKeyNotPrivileged = 14,
        ErrorIncorrectStopLossRate = 15,
        ErrorIncorrectTakeProfitRate = 16,
        ErrorIncorrectTrailingStopOffset = 17,
        ErrorApiKeysLimitReached = 18,
        ErrorApiKeyNotFound = 19,
        ErrorSignatureDuplicate = 20,
        ErrorNonceLessThanExpected = 21,
        ErrorIncorrectSignature = 22,
        ErrorNegativeOrZeroLimit = 23,
        ErrorInvalidFunctionArguments = 24,
        ErrorFunctionNotFound = 25,
        ErrorInvalidJsonInput = 26,
        ErrorNegativeOrZeroLeverage = 27,
        ErrorIncorrectPercValue = 28,
        ErrorFixAccountsLimitReached = 29,
        ErrorFixRestartFailed = 30,
        ErrorFixAccountAlreadyExists = 31,
        ErrorFixAccountNotFound = 32,
        ErrorFixSymbolNotFound = 33,
        ErrorFixFieldsNotSet = 34,
        ErrorFixInvalidClOrdID = 35,
        ErrorFixUnknownOrderType = 36,
        ErrorFixInvalidOrderId = 37,
        ErrorSnapshotBackupFailed = 38,
        ErrorSnapshotRestoreFailed = 39,
        ErrorMarketClosed = 40,
        ErrorMarketAlreadyClosed = 41,
        ErrorMarketAlreadyOpened = 42,
        ErrorMarketOpened = 43,
        ErrorBackupRestoreInProc = 44,
        ErrorIPDuplicate = 45,
        ErrorInvalidCurrency = 46,
        ErrorInvalidCurrencyPair = 47,        
        ErrorCurrencyNotFound = 48,
        ErrorCurrencyPairNotFound = 49, 
        ErrorCurrencyPairAlreadyExists = 50,
        ErrorStopLossUnavailable = 51,
        ErrorTakeProfitUnavailable = 52,
        ErrorTrailingStopUnavailable = 53,
        ErrorIncorrectDelayValue = 54,

        Unknown = 99
    }

    internal enum MessageTypes
    {
        NewBalance = 0,
        NewMarginInfo = 1,
        NewMarginCall = 2,
        NewTicker = 3,
        NewOrderBookTop = 4,
        NewOrder = 5,
        NewOrderMatch = 6,
        NewTrade = 7,
        NewAccountFee = 8
    }

    internal enum FCSources
    {
        Core = 0,
        WebApp = 1,
        Marketmaker = 2,
        HttpApi = 3,
        FixApi = 4,

    }

    internal enum CancOrdTypes
    {
        Limit = 0,
        StopLoss = 1,
        TakeProfit = 2,
        TrailingStop = 3
    }

    internal enum OrderStatuses
    {
        PartiallyFilled = 0,
        Filled = 1,
        Accepted = 2
    }

    internal enum OrderEvents
    {
        PlaceLimit = 0,
        PlaceMarket = 1,
        ExecSL = 2,
        ExecTP = 3,
        ExecTS = 4,
        AddSL = 5,
        AddTP = 6,
        AddTS = 7,
        Cancel = 8,
        ForcedLiquidation = 9
    }
    
}
