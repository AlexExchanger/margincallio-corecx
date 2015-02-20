
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
        BaseLimit = 650, //replication-only
        PlaceLimit = 700,
        PlaceMarket = 800,
        PlaceInstant = 900,
        CancelOrder = 1000,
        AddSL = 1100,
        AddTP = 1200,
        AddTS = 1300,
        RemoveSL = 1400,
        RemoveTP = 1500,
        RemoveTS = 1600,

        CreateFixAccount = 1700,
        GenerateNewFixPassword = 1800,
        GetFixAccounts = 1900,
        CancelFixAccount = 2000,

        GenerateApiKey = 2100,
        GetApiKeys = 2200,
        CancelApiKey = 2300,

        GetAccountInfo = 2400,
        GetOrderInfo = 2500,
        GetSLInfo = 2600,
        GetTPInfo = 2700,
        GetTSInfo = 2800,
        GetOpenOrders = 2900,
        GetOpenConditionalOrders = 3000,
        SetAccountFee = 3100,
        GetTicker = 3200,
        GetDepth = 3300,
        GetMarginParameters = 3400,
        SetMaxLeverage = 3500,
        SetMCLevel = 3600,
        SetFLLevel = 3700,

        GetAccountInfo_HTTP = 50000,
        GetOpenOrders_HTTP = 50100,
        GetOpenConditionalOrders_HTTP = 50200,
        PlaceLimit_HTTP = 50300,
        PlaceMarket_HTTP = 50400,
        PlaceInstant_HTTP = 50500,
        CancelOrder_HTTP = 50600,
        AddSL_HTTP = 50700,
        AddTP_HTTP = 50800,
        AddTS_HTTP = 50900,
        RemoveSL_HTTP = 51000,
        RemoveTP_HTTP = 51100,
        RemoveTS_HTTP = 51200,

        ManageMargin = 60000, //replication-only
        ManageSLs = 60100, //replication-only
        ManageTPs = 60200, //replication-only
        ManageTSs = 60300, //replication-only

        Authorize = 65000, //replication-only
        MarkFixAccountsActive = 65100, //replication-only

        CloseMarket = 79000,
        OpenMarket = 79100,
        RestartFix = 79500,

        BackupMasterSnapshot = 80000,
        RestoreMasterSnapshot = 80100,
        RestoreSlaveSnapshot = 80200,

        RestrictWebAppIP = 85000,
        RestrictHttpApiIP = 85100,
        RestrictDaemonIP = 85200
    }

    internal enum MarketClosedForbiddenFuncIds
    {
        DepositFunds = 500,
        WithdrawFunds = 600,
        PlaceLimit = 700,
        PlaceMarket = 800,
        PlaceInstant = 900,
        CancelOrder = 1000,
        AddSL = 1100,
        AddTP = 1200,
        AddTS = 1300,
        RemoveSL = 1400,
        RemoveTP = 1500,
        RemoveTS = 1600,

        CreateFixAccount = 1700,
        GenerateNewFixPassword = 1800,
        CancelFixAccount = 2000,

        GenerateApiKey = 2100,
        CancelApiKey = 2300,

        PlaceLimit_HTTP = 50300,
        PlaceMarket_HTTP = 50400,
        PlaceInstant_HTTP = 50500,
        CancelOrder_HTTP = 50600,
        AddSL_HTTP = 50700,
        AddTP_HTTP = 50800,
        AddTS_HTTP = 50900,
        RemoveSL_HTTP = 51000,
        RemoveTP_HTTP = 51100,
        RemoveTS_HTTP = 51200,
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
        ErrorNegativeOrZeroSum = 12,
        ErrorNegativeOrZeroId = 13,
        ErrorApiKeyNotPrivileged = 14,
        ErrorIncorrectPositionType = 15,
        ErrorIncorrectRate = 16,
        ErrorApiKeysLimitReached = 17,
        ErrorApiKeyNotFound = 18,
        ErrorSignatureDuplicate = 19,
        ErrorNonceLessThanExpected = 20,
        ErrorIncorrectSignature = 21,
        ErrorNegativeOrZeroLimit = 22,
        ErrorInvalidFunctionArguments = 23,
        ErrorFunctionNotFound = 24,
        ErrorInvalidJsonInput = 25,
        ErrorNegativeOrZeroLeverage = 26,
        ErrorIncorrectPercValue = 27,
        ErrorFixAccountsLimitReached = 28,
        ErrorFixRestartFailed = 29,
        ErrorFixAccountAlreadyExists = 30,
        ErrorFixAccountNotFound = 31,
        ErrorFixSymbolNotFound = 32, //TODO
        ErrorFixFieldsNotSet = 33,
        ErrorFixInvalidClOrdID = 34,
        ErrorFixUnknownOrderType = 35,
        ErrorFixInvalidOrderId = 36,
        ErrorSnapshotBackupFailed = 37,
        ErrorSnapshotRestoreFailed = 38,
        ErrorMarketClosed = 39,
        ErrorMarketAlreadyClosed = 40,
        ErrorMarketAlreadyOpened = 41,
        ErrorMarketOpened = 42,
        ErrorBackupRestoreInProc = 43,
        ErrorIPDuplicate = 44,
        ErrorInvalidCurrency = 45,
        ErrorInvalidCurrencyPair = 46,        
        ErrorCurrencyNotFound = 47,
        ErrorCurrencyPairNotFound = 48, 
        ErrorCurrencyPairAlreadyExists = 49,
        

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
        WebApp = 0,
        HttpApi = 1,
        FixApi = 2
    }

    internal enum FCRejCodes
    {
        InvalidFuncArgs = 0,
        FuncNotFound = 1,
        MarketClosed = 2,
        BackupRestoreInProc = 3
    }

    internal enum OrdTypes
    {
        Limit = 0,
        Market = 1,
        StopMarket = 2,
        StopLimit = 3
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
