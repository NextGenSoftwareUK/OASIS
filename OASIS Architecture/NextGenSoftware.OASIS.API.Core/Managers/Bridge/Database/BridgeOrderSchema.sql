-- =============================================
-- OASIS Cross-Chain Bridge Database Schema
-- Version: 1.0
-- Description: Database schema for tracking cross-chain bridge orders
-- =============================================

-- Table: BridgeOrders
-- Stores all bridge order information and transaction details
CREATE TABLE IF NOT EXISTS BridgeOrders (
    -- Primary Key
    OrderId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Order Details
    UserId UNIQUEIDENTIFIER NOT NULL,
    FromChain VARCHAR(50) NOT NULL,           -- e.g., 'SOL', 'XRD', 'ARB', 'ETH'
    ToChain VARCHAR(50) NOT NULL,             -- e.g., 'SOL', 'XRD', 'ARB', 'ETH'
    
    -- Amount Details
    FromAmount DECIMAL(28, 10) NOT NULL,      -- Amount sent from source chain
    ToAmount DECIMAL(28, 10) NOT NULL,        -- Amount to receive on destination chain
    ExchangeRate DECIMAL(28, 10) NOT NULL,    -- Exchange rate at time of order
    
    -- Account Addresses
    FromAddress VARCHAR(255) NOT NULL,        -- Source chain address
    ToAddress VARCHAR(255) NOT NULL,          -- Destination chain address
    
    -- Transaction Details
    WithdrawTransactionId VARCHAR(255) NULL,  -- Transaction ID on source chain
    DepositTransactionId VARCHAR(255) NULL,   -- Transaction ID on destination chain
    
    -- Status Tracking
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending',  -- Pending, Completed, Failed, Canceled, Expired
    IsCompleted BIT NOT NULL DEFAULT 0,
    IsFailed BIT NOT NULL DEFAULT 0,
    IsRolledBack BIT NOT NULL DEFAULT 0,
    
    -- Error Handling
    ErrorMessage VARCHAR(MAX) NULL,
    FailureReason VARCHAR(MAX) NULL,
    RollbackReason VARCHAR(MAX) NULL,
    
    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    WithdrawCompletedAt DATETIME2 NULL,
    DepositCompletedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    FailedAt DATETIME2 NULL,
    RolledBackAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NULL,
    
    -- Audit Trail
    CreatedBy VARCHAR(255) NULL,
    ModifiedBy VARCHAR(255) NULL,
    
    -- Indexes (defined below)
    CONSTRAINT CK_BridgeOrders_FromAmount CHECK (FromAmount > 0),
    CONSTRAINT CK_BridgeOrders_ToAmount CHECK (ToAmount > 0),
    CONSTRAINT CK_BridgeOrders_ExchangeRate CHECK (ExchangeRate > 0),
    CONSTRAINT CK_BridgeOrders_ValidChains CHECK (FromChain <> ToChain)
);

-- Create indexes for performance
CREATE INDEX IX_BridgeOrders_UserId ON BridgeOrders(UserId);
CREATE INDEX IX_BridgeOrders_Status ON BridgeOrders(Status);
CREATE INDEX IX_BridgeOrders_FromChain ON BridgeOrders(FromChain);
CREATE INDEX IX_BridgeOrders_ToChain ON BridgeOrders(ToChain);
CREATE INDEX IX_BridgeOrders_CreatedAt ON BridgeOrders(CreatedAt DESC);
CREATE INDEX IX_BridgeOrders_WithdrawTransactionId ON BridgeOrders(WithdrawTransactionId);
CREATE INDEX IX_BridgeOrders_DepositTransactionId ON BridgeOrders(DepositTransactionId);

-- Composite index for user order history
CREATE INDEX IX_BridgeOrders_UserHistory ON BridgeOrders(UserId, CreatedAt DESC, Status);

-- =============================================

-- Table: BridgeOrderBalances
-- Stores balance snapshots for bridge orders
CREATE TABLE IF NOT EXISTS BridgeOrderBalances (
    -- Primary Key
    BalanceId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Foreign Key
    OrderId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT FK_BridgeOrderBalances_OrderId 
        FOREIGN KEY (OrderId) REFERENCES BridgeOrders(OrderId) 
        ON DELETE CASCADE,
    
    -- Balance Details
    ChainType VARCHAR(50) NOT NULL,           -- 'Source' or 'Destination'
    Chain VARCHAR(50) NOT NULL,               -- Chain identifier (SOL, XRD, ARB, etc.)
    Address VARCHAR(255) NOT NULL,            -- Account address
    
    -- Balance Information
    BalanceBefore DECIMAL(28, 10) NULL,       -- Balance before transaction
    BalanceAfter DECIMAL(28, 10) NULL,        -- Balance after transaction
    BalanceChange DECIMAL(28, 10) NULL,       -- Change in balance
    
    -- Status
    IsVerified BIT NOT NULL DEFAULT 0,
    IsSufficient BIT NOT NULL DEFAULT 0,
    
    -- Timestamps
    CheckedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    VerifiedAt DATETIME2 NULL,
    
    -- Additional Info
    Notes VARCHAR(MAX) NULL
);

-- Create indexes for BridgeOrderBalances
CREATE INDEX IX_BridgeOrderBalances_OrderId ON BridgeOrderBalances(OrderId);
CREATE INDEX IX_BridgeOrderBalances_Chain ON BridgeOrderBalances(Chain);
CREATE INDEX IX_BridgeOrderBalances_Address ON BridgeOrderBalances(Address);

-- =============================================

-- Table: BridgeTransactionLog
-- Detailed transaction log for debugging and audit
CREATE TABLE IF NOT EXISTS BridgeTransactionLog (
    -- Primary Key
    LogId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Foreign Key
    OrderId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT FK_BridgeTransactionLog_OrderId 
        FOREIGN KEY (OrderId) REFERENCES BridgeOrders(OrderId) 
        ON DELETE CASCADE,
    
    -- Log Details
    LogLevel VARCHAR(20) NOT NULL,            -- Info, Warning, Error, Critical
    EventType VARCHAR(50) NOT NULL,           -- OrderCreated, WithdrawStarted, etc.
    Message VARCHAR(MAX) NOT NULL,
    
    -- Transaction Context
    TransactionId VARCHAR(255) NULL,
    Chain VARCHAR(50) NULL,
    Address VARCHAR(255) NULL,
    Amount DECIMAL(28, 10) NULL,
    
    -- Additional Data
    ExceptionDetails VARCHAR(MAX) NULL,
    StackTrace VARCHAR(MAX) NULL,
    AdditionalData VARCHAR(MAX) NULL,        -- JSON for custom data
    
    -- Timestamp
    LoggedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Context
    MachineName VARCHAR(255) NULL,
    ProcessId INT NULL,
    ThreadId INT NULL
);

-- Create indexes for BridgeTransactionLog
CREATE INDEX IX_BridgeTransactionLog_OrderId ON BridgeTransactionLog(OrderId);
CREATE INDEX IX_BridgeTransactionLog_LogLevel ON BridgeTransactionLog(LogLevel);
CREATE INDEX IX_BridgeTransactionLog_EventType ON BridgeTransactionLog(EventType);
CREATE INDEX IX_BridgeTransactionLog_LoggedAt ON BridgeTransactionLog(LoggedAt DESC);

-- =============================================

-- Table: ExchangeRateHistory
-- Historical exchange rates for audit and analytics
CREATE TABLE IF NOT EXISTS ExchangeRateHistory (
    -- Primary Key
    RateId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Rate Details
    FromToken VARCHAR(50) NOT NULL,
    ToToken VARCHAR(50) NOT NULL,
    Rate DECIMAL(28, 10) NOT NULL,
    
    -- Source Information
    Source VARCHAR(100) NOT NULL,             -- e.g., 'CoinGecko', 'Binance'
    IsRealTime BIT NOT NULL DEFAULT 1,
    
    -- Timestamps
    FetchedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ValidFrom DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ValidUntil DATETIME2 NULL,
    
    -- Additional Data
    AdditionalData VARCHAR(MAX) NULL         -- JSON for extended info
);

-- Create indexes for ExchangeRateHistory
CREATE INDEX IX_ExchangeRateHistory_FromToken ON ExchangeRateHistory(FromToken);
CREATE INDEX IX_ExchangeRateHistory_ToToken ON ExchangeRateHistory(ToToken);
CREATE INDEX IX_ExchangeRateHistory_FetchedAt ON ExchangeRateHistory(FetchedAt DESC);
CREATE INDEX IX_ExchangeRateHistory_TokenPair ON ExchangeRateHistory(FromToken, ToToken, FetchedAt DESC);

-- =============================================

-- View: ActiveBridgeOrders
-- Returns all active (non-completed, non-failed) orders
CREATE VIEW ActiveBridgeOrders AS
SELECT 
    OrderId,
    UserId,
    FromChain,
    ToChain,
    FromAmount,
    ToAmount,
    ExchangeRate,
    FromAddress,
    ToAddress,
    Status,
    CreatedAt,
    ExpiresAt,
    DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) AS AgeInMinutes
FROM BridgeOrders
WHERE IsCompleted = 0 
  AND IsFailed = 0 
  AND IsRolledBack = 0
  AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE());

-- =============================================

-- View: UserBridgeOrderSummary
-- Summary statistics per user
CREATE VIEW UserBridgeOrderSummary AS
SELECT 
    UserId,
    COUNT(*) AS TotalOrders,
    SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedOrders,
    SUM(CASE WHEN IsFailed = 1 THEN 1 ELSE 0 END) AS FailedOrders,
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingOrders,
    SUM(FromAmount) AS TotalVolume,
    MIN(CreatedAt) AS FirstOrderAt,
    MAX(CreatedAt) AS LastOrderAt
FROM BridgeOrders
GROUP BY UserId;

-- =============================================

-- Stored Procedure: CreateBridgeOrder
CREATE PROCEDURE CreateBridgeOrder
    @UserId UNIQUEIDENTIFIER,
    @FromChain VARCHAR(50),
    @ToChain VARCHAR(50),
    @FromAmount DECIMAL(28, 10),
    @ToAmount DECIMAL(28, 10),
    @ExchangeRate DECIMAL(28, 10),
    @FromAddress VARCHAR(255),
    @ToAddress VARCHAR(255),
    @ExpiresInMinutes INT = 30,
    @CreatedBy VARCHAR(255) = NULL,
    @OrderId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @OrderId = NEWID();
    
    DECLARE @ExpiresAt DATETIME2 = DATEADD(MINUTE, @ExpiresInMinutes, GETUTCDATE());
    
    INSERT INTO BridgeOrders (
        OrderId, UserId, FromChain, ToChain,
        FromAmount, ToAmount, ExchangeRate,
        FromAddress, ToAddress, Status,
        CreatedAt, UpdatedAt, ExpiresAt, CreatedBy
    )
    VALUES (
        @OrderId, @UserId, @FromChain, @ToChain,
        @FromAmount, @ToAmount, @ExchangeRate,
        @FromAddress, @ToAddress, 'Pending',
        GETUTCDATE(), GETUTCDATE(), @ExpiresAt, @CreatedBy
    );
    
    -- Log order creation
    INSERT INTO BridgeTransactionLog (OrderId, LogLevel, EventType, Message, Chain, LoggedAt)
    VALUES (@OrderId, 'Info', 'OrderCreated', 
            CONCAT('Bridge order created: ', @FromAmount, ' ', @FromChain, ' -> ', @ToAmount, ' ', @ToChain),
            @FromChain, GETUTCDATE());
    
    RETURN 0;
END;

-- =============================================

-- Stored Procedure: UpdateBridgeOrderStatus
CREATE PROCEDURE UpdateBridgeOrderStatus
    @OrderId UNIQUEIDENTIFIER,
    @Status VARCHAR(50),
    @ErrorMessage VARCHAR(MAX) = NULL,
    @ModifiedBy VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE BridgeOrders
    SET Status = @Status,
        UpdatedAt = GETUTCDATE(),
        ErrorMessage = @ErrorMessage,
        ModifiedBy = @ModifiedBy,
        IsCompleted = CASE WHEN @Status = 'Completed' THEN 1 ELSE IsCompleted END,
        IsFailed = CASE WHEN @Status = 'Failed' THEN 1 ELSE IsFailed END,
        CompletedAt = CASE WHEN @Status = 'Completed' THEN GETUTCDATE() ELSE CompletedAt END,
        FailedAt = CASE WHEN @Status = 'Failed' THEN GETUTCDATE() ELSE FailedAt END
    WHERE OrderId = @OrderId;
    
    -- Log status change
    INSERT INTO BridgeTransactionLog (OrderId, LogLevel, EventType, Message, LoggedAt)
    VALUES (@OrderId, 'Info', 'StatusChanged', 
            CONCAT('Status changed to: ', @Status), GETUTCDATE());
    
    RETURN @@ROWCOUNT;
END;

-- =============================================

-- Stored Procedure: RecordExchangeRate
CREATE PROCEDURE RecordExchangeRate
    @FromToken VARCHAR(50),
    @ToToken VARCHAR(50),
    @Rate DECIMAL(28, 10),
    @Source VARCHAR(100),
    @ValidForMinutes INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO ExchangeRateHistory (
        FromToken, ToToken, Rate, Source,
        FetchedAt, ValidFrom, ValidUntil
    )
    VALUES (
        @FromToken, @ToToken, @Rate, @Source,
        GETUTCDATE(), GETUTCDATE(), 
        DATEADD(MINUTE, @ValidForMinutes, GETUTCDATE())
    );
    
    RETURN 0;
END;

-- =============================================

-- Stored Procedure: GetBridgeOrdersByUser
CREATE PROCEDURE GetBridgeOrdersByUser
    @UserId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        OrderId, UserId, FromChain, ToChain,
        FromAmount, ToAmount, ExchangeRate,
        FromAddress, ToAddress,
        WithdrawTransactionId, DepositTransactionId,
        Status, IsCompleted, IsFailed,
        ErrorMessage, CreatedAt, UpdatedAt, CompletedAt
    FROM BridgeOrders
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

-- =============================================
-- End of Schema
-- =============================================






