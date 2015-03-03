
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
        GetWithdrawalLimit = 2600,
        GetOpenOrders = 2700,
        GetOrderInfo = 2800,
        
        CreateCurrencyPair = 5000,
        GetCurrencyPairs = 5100,
        GetDerivedCurrencies = 5200,
        DeleteCurrencyPair = 5300,

        GetTicker = 7000,
        GetDepth = 7100
    }

    internal enum MarketClosedForbiddenFuncIds
    {

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
        ErrorFixSymbolNotFound = 33, //TODO
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
        

        Unknown = 99
    }

    internal enum MessageTypes
    {
        NewBalance = 0, 
        NewTicker = 1, 
        NewActiveBuyTop = 2, 
        NewActiveSellTop = 3, 
        NewPlaceLimit = 4, // + func_call_id
        NewPlaceMarket = 5, // + func_call_id
        NewPlaceInstant = 6, // + func_call_id
        NewTrade = 7, 
        NewCancelOrder = 8, // + func_call_id
        NewAddSL = 9, // + func_call_id
        NewAddTP = 10, // + func_call_id
        NewAddTS = 11, // + func_call_id  
        NewRemoveSL = 12, // + func_call_id
        NewRemoveTP = 13, // + func_call_id
        NewRemoveTS = 14, // + func_call_id
        NewAccountFee = 15, // + func_call_id
        NewMarginInfo = 16,
        NewMarginCall = 17,
        NewForcedLiquidation = 18,
        NewExecSL = 19,
        NewExecTP = 20,
        NewExecTS = 21,
        NewFixRestart = 22,
        NewOrderStatus = 23,
        NewMarketStatus = 24,
        NewSnapshotOperation = 25
    }

    internal enum FCSources
    {
        Core = 0,
        WebApp = 1,
        HttpApi = 2,
        FixApi = 3
    }

    internal enum FCRejCodes
    {
        InvalidFuncArgs = 0,
        FuncNotFound = 1,
        MarketClosed = 2,
        BackupRestoreInProc = 3
    }

    internal enum CancOrdTypes
    {
        Limit = 0,
        StopLoss = 1,
        TakeProfit = 2,
        TrailingStop = 3
    }

    internal enum OrdExecStatuses
    {
        PartiallyFilled = 0,
        Filled = 1
    }

    internal enum MarketStatuses
    {
        Closed = 0,
        Opened = 1
    }

    internal enum SnapshotOperations
    {
        BackupMaster = 0,
        RestoreMaster = 1,
        RestoreSlave = 2
    }
    
}
